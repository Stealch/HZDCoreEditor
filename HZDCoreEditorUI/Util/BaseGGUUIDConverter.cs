namespace HZDCoreEditorUI.Util;

using System;
using System.Diagnostics.CodeAnalysis;
using Decima;
using Newtonsoft.Json;

/// <summary>
/// Converts BaseGGUUIDs to strings and vice versa during JSON serialization and deserialization.
/// </summary>
public class BaseGGUUIDConverter : JsonConverter<BaseGGUUID>
{
    /// <summary>
    /// Converts a JSON string to a BaseGGUUID object.
    /// </summary>
    /// <param name="reader">The JSON reader that reads the input.</param>
    /// <param name="objectType">The type of the object being converted.</param>
    /// <param name="existingValue">The existing value of the object being converted. Not used in this method.</param>
    /// <param name="hasExistingValue">Indicates whether the current value is the default value for the type. Not used in this method.</param>
    /// <param name="serializer">The JSON serializer.</param>
    /// <returns>A BaseGGUUID object if the JSON string is not null, otherwise null.</returns>
    public override BaseGGUUID ReadJson(JsonReader reader, Type objectType, [AllowNull] BaseGGUUID existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        if (reader.TokenType == JsonToken.Null)
            return null;

        var data = reader.Value as string;
        return data;
    }

    /// <summary>
    /// Converts a BaseGGUUID object to a JSON string.
    /// </summary>
    /// <param name="writer">The JSON writer.</param>
    /// <param name="value">The BaseGGUUID object to convert.</param>
    /// <param name="serializer">The JSON serializer.</param>
    public override void WriteJson(JsonWriter writer, [AllowNull] BaseGGUUID value, JsonSerializer serializer)
    {
        if (value != null)
        {
            writer.WriteValue(value.ToString());
        }
        else
        {
            writer.WriteNull();
        }
    }
}
