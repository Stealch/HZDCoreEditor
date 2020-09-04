﻿using BinaryStreamExtensions;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Decima
{
    static partial class RTTI
    {
        private static readonly Dictionary<ulong, Type> TypeIdLookupMap;
        private static readonly Dictionary<Type, OrderedFieldInfo> TypeFieldInfoCache;

        public class OrderedFieldInfo
        {
            public struct Entry
            {
                public readonly FieldInfo MIBase;
                public readonly FieldInfo Field;
                public readonly bool IgnoreBinarySerialization;

                public Entry(FieldInfo miBase, FieldInfo field, bool ignoreBinarySerialization)
                {
                    MIBase = miBase;
                    Field = field;
                    IgnoreBinarySerialization = ignoreBinarySerialization;
                }
            }

            public readonly FieldInfo[] MIBases;
            public readonly Entry[] Members;

            public OrderedFieldInfo(FieldInfo[] bases, Entry[] members)
            {
                MIBases = bases;
                Members = members;
            }
        }

        static RTTI()
        {
            // Build a cache of the 64-bit type IDs to actual C# types
            TypeIdLookupMap = new Dictionary<ulong, Type>();
            TypeFieldInfoCache = new Dictionary<Type, OrderedFieldInfo>();

            foreach (var classType in typeof(SerializableAttribute).Assembly.GetTypes())
            {
                var attribute = classType.GetCustomAttribute<SerializableAttribute>();

                if (attribute != null)
                    TypeIdLookupMap.Add(attribute.BinaryTypeId, classType);
            }
        }

        public static Type GetTypeById(ulong typeId)
        {
            if (TypeIdLookupMap.TryGetValue(typeId, out Type objectType))
                return objectType;

            return null;
        }

        public static OrderedFieldInfo GetOrderedFieldsForClass(Type type)
        {
            if (TypeFieldInfoCache.TryGetValue(type, out OrderedFieldInfo info))
                return info;

            if (!type.IsDefined(typeof(SerializableAttribute)))
                return null;

            //
            // This insane code tries to replicate HZD's sorting mechanism. Rebuild the class member hierarchy because of a few reasons:
            //
            // - C# doesn't allow multiple inheritance / multiple base classes.
            // - C# doesn't expose private fields from base classes with ParentType.GetFields().
            // - Properties are declared at offset 0. Serialization order is undefined (???) when multiple are declared at offset 0.
            // - Parent class members can overlap base class members.
            //
            // All [RTTI.Member()] fields are enumerated and sorted by offset, order, and most complex class type. Multiple inheritance
            // offsets are handled by the dumping code ([RTTI.BaseClass()]) so it doesn't need to be taken into account.
            //
            // Test: AIDynamicObstacleRectangleResource members are out of order and overlap the base (AIDynamicObstacleResource)
            // Test: CubemapZone emulated MI (Shape2DExtrusion)
            //
            var allFields = new List<(MemberAttribute Attr, uint Offset, uint ClassOrder, FieldInfo MIBase, FieldInfo Field)>();
            uint classIndex = 0;

            void addFieldsRecursively(Type classType, FieldInfo miBase = null, uint offset = 0)
            {
                // Drill down until System.Object is hit
                for (; classType != null; classType = classType.BaseType)
                {
                    if (!classType.IsDefined(typeof(SerializableAttribute)))
                        continue;

                    classIndex++;
                    var fields = classType.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);

                    foreach (var field in fields)
                    {
                        var baseClassAttr = field.GetCustomAttribute<BaseClassAttribute>();
                        var reflectionAttr = field.GetCustomAttribute<MemberAttribute>();

                        if (baseClassAttr != null)
                            addFieldsRecursively(field.FieldType, field, offset + baseClassAttr.RuntimeOffset);
                        else if (reflectionAttr != null)
                            allFields.Add((reflectionAttr, offset + reflectionAttr.RuntimeOffset, classIndex, miBase, field));
                    }
                }
            }

            // Sort: member offset, member index, class index
            addFieldsRecursively(type);

            var sortedHierarchy = allFields
                .OrderBy(x => x.Offset)
                .ThenBy(x => x.Attr.Order)
                .ThenByDescending(x => x.ClassOrder);

            // Unique base classes
            var miBases = sortedHierarchy
                .Where(x => x.MIBase != null)
                .Select(x => x.MIBase)
                .Distinct()
                .ToArray();

            // All members
            var members = sortedHierarchy
                .Select(x => new OrderedFieldInfo.Entry(x.MIBase, x.Field, x.Attr.IgnoreBinarySerialization))
                .ToArray();

            info = new OrderedFieldInfo(miBases, members);
            TypeFieldInfoCache.Add(type, info);

            return info;
        }

        public static T DeserializeType<T>(BinaryReader reader)
        {
            return (T)DeserializeType(reader, typeof(T));
        }

        public static object DeserializeType(BinaryReader reader, Type type)
        {
            // Enums and trivial types
            if (DeserializeTrivialType(reader, type, out object trivialValue))
                return trivialValue;

            // Classes and structs
            if (DeserializeObjectType(reader, type, out object objectValue))
                return objectValue;

            throw new NotImplementedException($"Unhandled object type '{type.FullName}'");
        }

        private static void DeserializeTypeFromField(BinaryReader reader, object instance, FieldInfo field)
        {
            field.SetValue(instance, DeserializeType(reader, field.FieldType));
        }

        private static bool DeserializeObjectType(BinaryReader reader, Type type, out object objectInstance)
        {
            if (!type.IsClass && !type.IsValueType)
            {
                objectInstance = null;
                return false;
            }

            objectInstance = Activator.CreateInstance(type);

            if (objectInstance is ISerializable asSerializable)
            {
                // Custom deserialization function implemented. Let the interface do the work.
                asSerializable.Deserialize(reader);
            }
            else
            {
                var info = GetOrderedFieldsForClass(type);

                // Instantiate bases
                foreach (var baseClass in info.MIBases)
                    baseClass.SetValue(objectInstance, Activator.CreateInstance(baseClass.FieldType));

                // Read members
                foreach (var member in info.Members)
                {
                    if (member.IgnoreBinarySerialization)
                        continue;

                    if (member.MIBase != null)
                        DeserializeTypeFromField(reader, member.MIBase.GetValue(objectInstance), member.Field);
                    else
                        DeserializeTypeFromField(reader, objectInstance, member.Field);
                }
            }

            // Done reading. Now copy what the engine does and notify MsgReadBinary subscribers.
            if (objectInstance is IExtraBinaryDataCallback asExtraBinaryDataCallback)
                asExtraBinaryDataCallback.DeserializeExtraData(reader);

            return true;
        }

        private static bool DeserializeTrivialType(BinaryReader reader, Type type, out object value)
        {
            bool valid = true;

            // The game always does a direct memory copy for these
            value = Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => reader.ReadBooleanStrict(),
                TypeCode.SByte => reader.ReadSByte(),
                TypeCode.Byte => reader.ReadByte(),
                TypeCode.Int16 => reader.ReadInt16(),
                TypeCode.UInt16 => reader.ReadUInt16(),
                TypeCode.Int32 => reader.ReadInt32(),
                TypeCode.UInt32 => reader.ReadUInt32(),
                TypeCode.Int64 => reader.ReadInt64(),
                TypeCode.UInt64 => reader.ReadUInt64(),
                TypeCode.Single => reader.ReadSingle(),
                TypeCode.Double => reader.ReadDouble(),
                _ => valid = false,
            };

            return valid;
        }
    }
}