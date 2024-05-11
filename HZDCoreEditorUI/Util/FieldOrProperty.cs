namespace HZDCoreEditorUI.Util;

using System;
using System.Reflection;

/// <summary>
/// A struct that represents a field or property in a class.
/// </summary>
public struct FieldOrProperty
{
    private readonly MemberInfo _info;

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldOrProperty"/> struct.
    /// </summary>
    /// <param name="info">The field or property to represent.</param>
    public FieldOrProperty(FieldInfo info)
    {
        _info = info;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldOrProperty"/> struct.
    /// </summary>
    /// <param name="info">The property to represent.</param>
    public FieldOrProperty(PropertyInfo info)
    {
        _info = info;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="FieldOrProperty"/> struct.
    /// </summary>
    /// <param name="type">The type of the class.</param>
    /// <param name="memberName">The name of the field or property.</param>
    /// <exception cref="ArgumentException">Thrown if the memberName is not a valid class member type.</exception>
    public FieldOrProperty(Type type, string memberName)
    {
        _info = type.GetProperty(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_info == null)
            _info = type.GetField(memberName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (_info == null || (_info.MemberType != MemberTypes.Property && _info.MemberType != MemberTypes.Field))
            throw new ArgumentException("Invalid class member type", nameof(memberName));
    }

    /// <summary>
    /// Sets the value of the field or property.
    /// </summary>
    /// <param name="obj">The object whose field or property to set.</param>
    /// <param name="value">The value to set.</param>
    public void SetValue(object obj, object value)
    {
        switch (_info.MemberType)
        {
            case MemberTypes.Property:
                ((PropertyInfo)_info).SetValue(obj, value);
                break;
            case MemberTypes.Field:
                ((FieldInfo)_info).SetValue(obj, value);
                break;
        }
    }

    /// <summary>
    /// Gets the value of the field or property.
    /// </summary>
    /// <param name="obj">The object whose field or property to get.</param>
    /// <returns>The value of the field or property.</returns>
    public object GetValue(object obj)
    {
        return _info.MemberType switch
        {
            MemberTypes.Property => ((PropertyInfo)_info).GetValue(obj),
            MemberTypes.Field => ((FieldInfo)_info).GetValue(obj),
            _ => null,
        };
    }

    /// <summary>
    /// Gets the value of the field or property as a specific type.
    /// </summary>
    /// <typeparam name="T">The type to get the value as.</typeparam>
    /// <param name="obj">The object whose field or property to get.</param>
    /// <returns>The value of the field or property as the specified type.</returns>
    public T GetValue<T>(object obj)
    {
        return (T)GetValue(obj);
    }

    /// <summary>
    /// Gets the type of the field or property.
    /// </summary>
    /// <returns>The type of the field or property.</returns>
    public Type GetMemberType()
    {
        return _info.MemberType switch
        {
            MemberTypes.Property => ((PropertyInfo)_info).PropertyType,
            MemberTypes.Field => ((FieldInfo)_info).FieldType,
            _ => null,
        };
    }

    /// <summary>
    /// Gets the name of the field or property.
    /// </summary>
    /// <returns>The name of the field or property.</returns>
    public string GetName()
    {
        return _info.Name;
    }

    /// <summary>
    /// Gets the category of the member.
    /// </summary>
    /// <returns>The category of the member.</returns>
    public string GetCategory()
    {
        // Create a new RttiField based on the MemberType of _info
        var field = _info.MemberType switch
        {
            MemberTypes.Property => new Decima.RTTI.RttiField((PropertyInfo)_info),
            MemberTypes.Field => new Decima.RTTI.RttiField((FieldInfo)_info),
            _ => null,
        };

        // Get the category of the field using the Rtti.GetFieldCategory method
        return Decima.RTTI.GetFieldCategory(field);
    }
}
