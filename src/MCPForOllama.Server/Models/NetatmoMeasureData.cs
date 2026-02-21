using System.Text.Json.Serialization;

namespace MCPForOllama.Server.Models;

public class NetatmoMeasureApiResponse
{
    [JsonPropertyName("body")]
    public List<NetatmoMeasureBody> Body { get; set; } = [];

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class NetatmoMeasureBody
{
    [JsonPropertyName("beg_time")]
    public long BegTime { get; set; }

    [JsonPropertyName("step_time")]
    public long StepTime { get; set; }

    [JsonPropertyName("value")]
    public List<List<double?>> Value { get; set; } = [];
}
