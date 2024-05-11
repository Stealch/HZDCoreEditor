namespace HZDCoreTools.Util;

using System;
using System.Diagnostics.CodeAnalysis;
using Decima;
using Newtonsoft.Json;

/// <summary>
/// Converts BaseGGUUIDs to and from strings.
/// </summary>
public class BaseGGUUIDConverter : JsonConverter<BaseGGUUID>
{
    /// <summary>
    /// Reads the JSON representation of the object.
    /// </summary>
    /// <param name="reader">The JsonReader to read from.</param>
    /// <param name="objectType">Type of the object.</param>
    /// <param name="existingValue">The existing value of object being read.</param>
    /// <param name="hasExistingValue">A boolean indicating whether existingValue contains a valid value.</param>
    /// <param name="serializer">The JsonSerializer instance to use for deserialization.</param>
    /// <returns>The object value.</returns>
    public override BaseGGUUID ReadJson(JsonReader reader, Type objectType, [AllowNull] BaseGGUUID existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        // If the token is null, return null
        if (reader.TokenType == JsonToken.Null)
            return null;

        // If the token is a string, return the string as a BaseGGUUID
        return reader.Value as string;
    }

    /// <summary>
    /// Writes the JSON representation of the object.
    /// </summary>
    /// <param name="writer">The JsonWriter to write to.</param>
    /// <param name="value">The value to write.</param>
    /// <param name="serializer">The JsonSerializer instance to use for serialization.</param>
    public override void WriteJson(JsonWriter writer, [AllowNull] BaseGGUUID value, JsonSerializer serializer)
    {
        // If the value is not null, write the string representation of the value
        if (value != null)
            writer.WriteValue(value.ToString());

        // Otherwise, write null
        else
            writer.WriteNull();
    }
}