using System.ComponentModel;
using System.Globalization;
using System.Text;
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
}
