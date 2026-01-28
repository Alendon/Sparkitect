using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace Sparkitect.Sdk.TaskImpl.Utils;

/// <summary>
/// JSON converter for Semantic Version objects
/// </summary>
public class SemVersionJsonConverter : JsonConverter<SemVersion>
{
    /// <inheritdoc />
    public override SemVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!SemVersion.TryParse(value, SemVersionStyles.Any, out var version))
            throw new JsonException($"Invalid semantic version: '{value}'");
        return version;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

/// <summary>
/// JSON converter for Semantic Version Range objects
/// </summary>
public class SemVersionRangeJsonConverter : JsonConverter<SemVersionRange>
{
    /// <inheritdoc />
    public override SemVersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!SemVersionRange.TryParse(value, out var range))
            throw new JsonException($"Invalid semantic version range: '{value}'");
        return range;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, SemVersionRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}