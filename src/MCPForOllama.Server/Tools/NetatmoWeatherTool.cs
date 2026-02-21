using System.ComponentModel;
using System.Globalization;
using System.Text;
using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using ModelContextProtocol.Server;

namespace MCPForOllama.Server.Tools;

[McpServerToolType]
public class NetatmoWeatherTool(
    INetatmoApiService netatmoApi,
    ILogger<NetatmoWeatherTool> logger)
{
    [McpServerTool, Description("Gets current temperature and humidity readings from all Netatmo weather stations and modules in the home.")]
    public async Task<string> GetTemperatures()
    {
        logger.LogInformation("GetTemperatures invoked");

        try
        {
            var devices = await netatmoApi.GetStationDataAsync();

            if (devices.Count == 0)
                return "No Netatmo weather stations found.";

            var sb = new StringBuilder();
            sb.AppendLine("Current readings:");

            foreach (var device in devices)
            {
                if (device.DashboardData is { } indoor)
                {
                    sb.Append($"  {device.StationName} - {device.ModuleName}:");
                    if (indoor.Temperature.HasValue)
                        sb.Append(CultureInfo.InvariantCulture, $" {indoor.Temperature:F1}C");
                    if (indoor.Humidity.HasValue)
                        sb.Append($", {indoor.Humidity}% humidity");
                    sb.AppendLine();
                }

                foreach (var module in device.Modules)
                {
                    if (module.DashboardData is { } data)
                    {
                        sb.Append($"  {device.StationName} - {module.ModuleName}:");
                        if (data.Temperature.HasValue)
                            sb.Append(CultureInfo.InvariantCulture, $" {data.Temperature:F1}C");
                        if (data.Humidity.HasValue)
                            sb.Append($", {data.Humidity}% humidity");
                        sb.AppendLine();
                    }
                }
            }

            var result = sb.ToString().TrimEnd();
            logger.LogInformation("GetTemperatures result: {Result}", result);
            return result;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("authenticate", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Netatmo not authenticated: {Message}", ex.Message);
            return ex.Message;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch Netatmo station data");
            return $"Failed to fetch Netatmo data: {ex.Message}";
        }
    }

    [McpServerTool, Description("Gets historical temperature and humidity data for a specific Netatmo module/room over a configurable time period. Use module names like 'Living Room', 'Outdoor', etc.")]
    public async Task<string> GetHistoricalData(
        [Description("Name of the module/room to query (e.g. 'Living Room', 'Outdoor')")] string moduleName,
        [Description("Number of hours to look back (default 24, max 720 = 30 days)")] int hoursBack = 24,
        [Description("Time scale for data points: '30min', '1hour', '3hours', '1day'. Auto-selected if omitted.")] string? scale = null)
    {
        logger.LogInformation("GetHistoricalData invoked for module '{ModuleName}', hoursBack={HoursBack}, scale={Scale}",
            moduleName, hoursBack, scale);

        if (hoursBack < 1 || hoursBack > 720)
            return "hoursBack must be between 1 and 720 (30 days).";

        try
        {
            var devices = await netatmoApi.GetStationDataAsync();

            if (devices.Count == 0)
                return "No Netatmo weather stations found.";

            var (deviceId, moduleId, resolvedName) = ResolveModule(devices, moduleName);

            if (deviceId is null)
            {
                var available = GetAvailableModuleNames(devices);
                return $"Module '{moduleName}' not found. Available modules: {string.Join(", ", available)}";
            }

            scale ??= SelectScale(hoursBack);

            var dateEnd = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
            var dateBegin = dateEnd - (hoursBack * 3600L);

            var measurements = await netatmoApi.GetMeasureAsync(
                deviceId, moduleId, scale, "Temperature,Humidity", dateBegin, dateEnd);

            if (measurements.Count == 0 || measurements.All(m => m.Value.Count == 0))
                return $"No historical data available for '{resolvedName}' in the requested time range.";

            return FormatMeasurements(measurements, resolvedName!, hoursBack, scale);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("authenticate", StringComparison.OrdinalIgnoreCase))
        {
            logger.LogWarning("Netatmo not authenticated: {Message}", ex.Message);
            return ex.Message;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Failed to fetch Netatmo historical data");
            return $"Failed to fetch Netatmo data: {ex.Message}";
        }
    }

    public static string SelectScale(int hoursBack) => hoursBack switch
    {
        <= 6 => "30min",
        <= 48 => "1hour",
        <= 168 => "3hours",
        _ => "1day"
    };

    public static (string? DeviceId, string? ModuleId, string? Name) ResolveModule(
        List<NetatmoDevice> devices, string moduleName)
    {
        foreach (var device in devices)
        {
            if (device.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                return (device.Id, null, device.ModuleName);

            foreach (var module in device.Modules)
            {
                if (module.ModuleName.Equals(moduleName, StringComparison.OrdinalIgnoreCase))
                    return (device.Id, module.Id, module.ModuleName);
            }
        }

        return (null, null, null);
    }

    public static List<string> GetAvailableModuleNames(List<NetatmoDevice> devices)
    {
        var names = new List<string>();
        foreach (var device in devices)
        {
            names.Add(device.ModuleName);
            names.AddRange(device.Modules.Select(m => m.ModuleName));
        }
        return names;
    }

    public static string FormatMeasurements(
        List<NetatmoMeasureBody> measurements, string moduleName, int hoursBack, string scale)
    {
        var sb = new StringBuilder();
        sb.AppendLine(CultureInfo.InvariantCulture, $"Historical data for '{moduleName}' (last {hoursBack}h, scale: {scale}):");
        sb.AppendLine("Timestamp                Temp (C)   Humidity (%)");
        sb.AppendLine(new string('-', 52));

        var allTemps = new List<double>();
        var allHumidity = new List<double>();

        foreach (var body in measurements)
        {
            var timestamp = body.BegTime;
            foreach (var values in body.Value)
            {
                var time = DateTimeOffset.FromUnixTimeSeconds(timestamp).LocalDateTime;
                var temp = values.Count > 0 ? values[0] : null;
                var humidity = values.Count > 1 ? values[1] : null;

                var tempStr = temp.HasValue
                    ? temp.Value.ToString("F1", CultureInfo.InvariantCulture)
                    : "  --";
                var humStr = humidity.HasValue
                    ? humidity.Value.ToString("F0", CultureInfo.InvariantCulture)
                    : "--";

                sb.AppendLine($"{time:yyyy-MM-dd HH:mm}       {tempStr,7}      {humStr,5}");

                if (temp.HasValue) allTemps.Add(temp.Value);
                if (humidity.HasValue) allHumidity.Add(humidity.Value);

                timestamp += body.StepTime;
            }
        }

        sb.AppendLine(new string('-', 52));

        if (allTemps.Count > 0)
        {
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"Temperature  — min: {allTemps.Min():F1}C, max: {allTemps.Max():F1}C, avg: {allTemps.Average():F1}C"));
        }

        if (allHumidity.Count > 0)
        {
            sb.AppendLine(string.Create(CultureInfo.InvariantCulture,
                $"Humidity     — min: {allHumidity.Min():F0}%, max: {allHumidity.Max():F0}%, avg: {allHumidity.Average():F0}%"));
        }

        return sb.ToString().TrimEnd();
    }
}
