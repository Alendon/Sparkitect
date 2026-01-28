using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace Sparkitect.Utils;

public class SemVersionJsonConverter : JsonConverter<SemVersion>
{
    public override SemVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!SemVersion.TryParse(value, SemVersionStyles.Any, out var version))
            throw new JsonException($"Invalid semantic version: '{value}'");
        return version;
    }

    public override void Write(Utf8JsonWriter writer, SemVersion value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}

public class SemVersionRangeJsonConverter : JsonConverter<SemVersionRange>
{
    public override SemVersionRange? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        if (!SemVersionRange.TryParse(value, out var range))
            throw new JsonException($"Invalid semantic version range: '{value}'");
        return range;
    }

    public override void Write(Utf8JsonWriter writer, SemVersionRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}