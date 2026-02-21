using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using MCPForOllama.Server.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MCPForOllama.Server.Tests.Tools;

public class NetatmoWeatherToolTests
{
    [Fact]
    public async Task GetTemperatures_WithDevices_ReturnsFormattedReadings()
    {
        var devices = new List<NetatmoDevice>
        {
            new()
            {
                StationName = "Home",
                ModuleName = "Living Room",
                DashboardData = new NetatmoDashboardData { Temperature = 21.3, Humidity = 45 },
                Modules =
                [
                    new NetatmoModule
                    {
                        ModuleName = "Outdoor",
                        DashboardData = new NetatmoDashboardData { Temperature = 8.1, Humidity = 78 }
                    }
                ]
            }
        };

        var tool = CreateTool(new MockNetatmoApiService(devices));
        var result = await tool.GetTemperatures();

        Assert.Contains("Living Room", result);
        Assert.Contains("21.3", result);
        Assert.Contains("45% humidity", result);
        Assert.Contains("Outdoor", result);
        Assert.Contains("8.1", result);
        Assert.Contains("78% humidity", result);
    }

    [Fact]
    public async Task GetTemperatures_NoDevices_ReturnsNoStationsMessage()
    {
        var tool = CreateTool(new MockNetatmoApiService([]));
        var result = await tool.GetTemperatures();

        Assert.Equal("No Netatmo weather stations found.", result);
    }

    [Fact]
    public async Task GetTemperatures_NotAuthenticated_ReturnsAuthMessage()
    {
        var service = new MockNetatmoApiService(
            exception: new InvalidOperationException("Netatmo is not authenticated. Please visit http://localhost:5000/netatmo/auth to connect your Netatmo account."));

        var tool = CreateTool(service);
        var result = await tool.GetTemperatures();

        Assert.Contains("authenticate", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/netatmo/auth", result);
    }

    [Fact]
    public async Task GetTemperatures_HttpError_ReturnsErrorMessage()
    {
        var service = new MockNetatmoApiService(
            exception: new HttpRequestException("Connection refused"));

        var tool = CreateTool(service);
        var result = await tool.GetTemperatures();

        Assert.Contains("Failed to fetch Netatmo data", result);
        Assert.Contains("Connection refused", result);
    }

    [Fact]
    public async Task GetTemperatures_ModuleWithNullDashboardData_SkipsModule()
    {
        var devices = new List<NetatmoDevice>
        {
            new()
            {
                StationName = "Home",
                ModuleName = "Indoor",
                DashboardData = new NetatmoDashboardData { Temperature = 20.0, Humidity = 50 },
                Modules =
                [
                    new NetatmoModule
                    {
                        ModuleName = "Offline Sensor",
                        DashboardData = null
                    }
                ]
            }
        };

        var tool = CreateTool(new MockNetatmoApiService(devices));
        var result = await tool.GetTemperatures();

        Assert.Contains("Indoor", result);
        Assert.DoesNotContain("Offline Sensor", result);
    }

    [Fact]
    public async Task GetTemperatures_MultipleStations_ReturnsAll()
    {
        var devices = new List<NetatmoDevice>
        {
            new()
            {
                StationName = "Home",
                ModuleName = "Living Room",
                DashboardData = new NetatmoDashboardData { Temperature = 21.0, Humidity = 45 },
                Modules = []
            },
            new()
            {
                StationName = "Office",
                ModuleName = "Desk Area",
                DashboardData = new NetatmoDashboardData { Temperature = 23.5, Humidity = 40 },
                Modules = []
            }
        };

        var tool = CreateTool(new MockNetatmoApiService(devices));
        var result = await tool.GetTemperatures();

        Assert.Contains("Home", result);
        Assert.Contains("Living Room", result);
        Assert.Contains("Office", result);
        Assert.Contains("Desk Area", result);
    }

    private static NetatmoWeatherTool CreateTool(INetatmoApiService service)
        => new(service, NullLogger<NetatmoWeatherTool>.Instance);

    #region Test helpers

    private class MockNetatmoApiService(
        List<NetatmoDevice>? devices = null,
        Exception? exception = null) : INetatmoApiService
    {
        public Task<List<NetatmoDevice>> GetStationDataAsync(CancellationToken cancellationToken = default)
        {
            if (exception is not null)
                throw exception;
            return Task.FromResult(devices ?? []);
        }

        public Task<NetatmoTokens> ExchangeCodeForTokensAsync(string authorizationCode, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public string GetAuthorizationUrl(string state)
            => throw new NotImplementedException();
    }

    #endregion
}
