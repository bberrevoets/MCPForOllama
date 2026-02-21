using System.Text.Json.Serialization;

namespace MCPForOllama.Server.Models;

public class NetatmoApiResponse
{
    [JsonPropertyName("body")]
    public NetatmoBody Body { get; set; } = new();
}

public class NetatmoBody
{
    [JsonPropertyName("devices")]
    public List<NetatmoDevice> Devices { get; set; } = [];
}

public class NetatmoDevice
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("station_name")]
    public string StationName { get; set; } = string.Empty;

    [JsonPropertyName("module_name")]
    public string ModuleName { get; set; } = string.Empty;

    [JsonPropertyName("dashboard_data")]
    public NetatmoDashboardData? DashboardData { get; set; }

    [JsonPropertyName("modules")]
    public List<NetatmoModule> Modules { get; set; } = [];
}

public class NetatmoModule
{
    [JsonPropertyName("_id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("module_name")]
    public string ModuleName { get; set; } = string.Empty;

    [JsonPropertyName("dashboard_data")]
    public NetatmoDashboardData? DashboardData { get; set; }
}

public class NetatmoDashboardData
{
    [JsonPropertyName("Temperature")]
    public double? Temperature { get; set; }

    [JsonPropertyName("Humidity")]
    public int? Humidity { get; set; }
}
