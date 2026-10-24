using System.Text.Json;

namespace AvitoNet;

/// <summary>Shared serializer options. camelCase read/write, case-insensitive — the Avito JSON convention.</summary>
internal static class AvitoJson
{
    public static readonly JsonSerializerOptions Web = new(JsonSerializerDefaults.Web);
}
