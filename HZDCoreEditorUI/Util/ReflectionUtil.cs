namespace HZDCoreEditorUI.Util;

using System;

/// <summary>
/// A utility class for reflection-related operations.
/// </summary>
internal static class ReflectionUtil
{
    /// <summary>
    /// Determines if a given type inherits from a specified base type.
    /// </summary>
    /// <param name="objectType">The type to check inheritance for.</param>
    /// <param name="baseType">The base type to check against.</param>
    /// <returns>True if the type inherits from the base type, false otherwise.</returns>
    public static bool Inherits(this Type objectType, Type baseType)
    {
        while (objectType != null)
        {
            if (objectType == baseType)
                return true;

            objectType = objectType.BaseType;
        }

        return false;
    }

    /// <summary>
    /// Determines if a given type inherits from a specified generic type.
    /// </summary>
    /// <param name="objectType">The type to check inheritance for.</param>
    /// <param name="genericType">The generic type to check against.</param>
    /// <returns>True if the type inherits from the generic type, false otherwise.</returns>
    public static bool InheritsGeneric(this Type objectType, Type genericType)
    {
        while (objectType != null)
        {
            if (objectType.IsGenericType && objectType.GetGenericTypeDefinition() == genericType)
                return true;

            objectType = objectType.BaseType;
        }

        return false;
    }
}
