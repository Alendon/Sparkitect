using System.Text.Json;
using System.Text.Json.Serialization;
using Semver;

namespace Sparkitect.Utils;

public class SemVersionJsonConverter : JsonConverter<SemVersion>
{
    public override SemVersion? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        SemVersion.TryParse(reader.GetString(), SemVersionStyles.Any, out var version);
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
        SemVersionRange.TryParse(reader.GetString(), out var range);
        return range;
    }

    public override void Write(Utf8JsonWriter writer, SemVersionRange value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString());
    }
}