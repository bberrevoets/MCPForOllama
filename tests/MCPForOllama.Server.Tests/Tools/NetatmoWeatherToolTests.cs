using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using MCPForOllama.Server.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MCPForOllama.Server.Tests.Tools;

public class NetatmoWeatherToolTests
{
    private static readonly List<NetatmoDevice> DefaultDevices =
    [
        new()
        {
            Id = "70:ee:50:aa:bb:cc",
            StationName = "Home",
            ModuleName = "Living Room",
            DashboardData = new NetatmoDashboardData { Temperature = 21.3, Humidity = 45 },
            Modules =
            [
                new NetatmoModule
                {
                    Id = "02:00:00:aa:bb:dd",
                    ModuleName = "Outdoor",
                    DashboardData = new NetatmoDashboardData { Temperature = 8.1, Humidity = 78 }
                }
            ]
        }
    ];

    #region GetTemperatures tests

    [Fact]
    public async Task GetTemperatures_WithDevices_ReturnsFormattedReadings()
    {
        var tool = CreateTool(new MockNetatmoApiService(DefaultDevices));
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
            stationException: new InvalidOperationException("Netatmo is not authenticated. Please visit http://localhost:5000/netatmo/auth to connect your Netatmo account."));

        var tool = CreateTool(service);
        var result = await tool.GetTemperatures();

        Assert.Contains("authenticate", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("/netatmo/auth", result);
    }

    [Fact]
    public async Task GetTemperatures_HttpError_ReturnsErrorMessage()
    {
        var service = new MockNetatmoApiService(
            stationException: new HttpRequestException("Connection refused"));

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

    #endregion

    #region GetHistoricalData tests

    [Fact]
    public async Task GetHistoricalData_ValidModule_ReturnsFormattedTable()
    {
        var measureData = new List<NetatmoMeasureBody>
        {
            new()
            {
                BegTime = 1700000000,
                StepTime = 1800,
                Value = [[21.5, 45], [21.3, 46], [21.0, 47]]
            }
        };

        var service = new MockNetatmoApiService(DefaultDevices, measureData);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Living Room");

        Assert.Contains("Historical data for 'Living Room'", result);
        Assert.Contains("21.5", result);
        Assert.Contains("21.0", result);
        Assert.Contains("min:", result);
        Assert.Contains("max:", result);
        Assert.Contains("avg:", result);
    }

    [Fact]
    public async Task GetHistoricalData_OutdoorModule_UsesModuleId()
    {
        var measureData = new List<NetatmoMeasureBody>
        {
            new()
            {
                BegTime = 1700000000,
                StepTime = 3600,
                Value = [[8.1, 78]]
            }
        };

        var service = new MockNetatmoApiService(DefaultDevices, measureData);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Outdoor", 6);

        Assert.Contains("Historical data for 'Outdoor'", result);
        Assert.Equal("70:ee:50:aa:bb:cc", service.LastMeasureDeviceId);
        Assert.Equal("02:00:00:aa:bb:dd", service.LastMeasureModuleId);
    }

    [Fact]
    public async Task GetHistoricalData_ModuleNotFound_ReturnsAvailableNames()
    {
        var service = new MockNetatmoApiService(DefaultDevices);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Bedroom");

        Assert.Contains("Module 'Bedroom' not found", result);
        Assert.Contains("Living Room", result);
        Assert.Contains("Outdoor", result);
    }

    [Fact]
    public async Task GetHistoricalData_CaseInsensitiveMatch()
    {
        var measureData = new List<NetatmoMeasureBody>
        {
            new() { BegTime = 1700000000, StepTime = 1800, Value = [[21.5, 45]] }
        };

        var service = new MockNetatmoApiService(DefaultDevices, measureData);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("living room");

        Assert.Contains("Historical data for 'Living Room'", result);
    }

    [Fact]
    public async Task GetHistoricalData_HoursBackTooLow_ReturnsValidationError()
    {
        var tool = CreateTool(new MockNetatmoApiService(DefaultDevices));
        var result = await tool.GetHistoricalData("Living Room", 0);

        Assert.Contains("hoursBack must be between 1 and 720", result);
    }

    [Fact]
    public async Task GetHistoricalData_HoursBackTooHigh_ReturnsValidationError()
    {
        var tool = CreateTool(new MockNetatmoApiService(DefaultDevices));
        var result = await tool.GetHistoricalData("Living Room", 721);

        Assert.Contains("hoursBack must be between 1 and 720", result);
    }

    [Fact]
    public async Task GetHistoricalData_EmptyMeasurements_ReturnsNoDataMessage()
    {
        var service = new MockNetatmoApiService(DefaultDevices, []);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Living Room");

        Assert.Contains("No historical data available", result);
    }

    [Fact]
    public async Task GetHistoricalData_NotAuthenticated_ReturnsAuthMessage()
    {
        var service = new MockNetatmoApiService(
            stationException: new InvalidOperationException("Netatmo is not authenticated. Please visit http://localhost:5000/netatmo/auth to connect your Netatmo account."));

        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Living Room");

        Assert.Contains("authenticate", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetHistoricalData_HttpError_ReturnsErrorMessage()
    {
        var service = new MockNetatmoApiService(
            DefaultDevices,
            measureException: new HttpRequestException("API error"));

        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Living Room");

        Assert.Contains("Failed to fetch Netatmo data", result);
        Assert.Contains("API error", result);
    }

    [Fact]
    public async Task GetHistoricalData_NullValuesInMeasurement_HandledGracefully()
    {
        var measureData = new List<NetatmoMeasureBody>
        {
            new()
            {
                BegTime = 1700000000,
                StepTime = 1800,
                Value = [[21.5, 45], [null, null], [20.0, 50]]
            }
        };

        var service = new MockNetatmoApiService(DefaultDevices, measureData);
        var tool = CreateTool(service);
        var result = await tool.GetHistoricalData("Living Room");

        Assert.Contains("--", result);
        Assert.Contains("21.5", result);
        Assert.Contains("20.0", result);
    }

    #endregion

    #region SelectScale tests

    [Theory]
    [InlineData(1, "30min")]
    [InlineData(6, "30min")]
    [InlineData(7, "1hour")]
    [InlineData(48, "1hour")]
    [InlineData(49, "3hours")]
    [InlineData(168, "3hours")]
    [InlineData(169, "1day")]
    [InlineData(720, "1day")]
    public void SelectScale_ReturnsExpectedScale(int hoursBack, string expectedScale)
    {
        Assert.Equal(expectedScale, NetatmoWeatherTool.SelectScale(hoursBack));
    }

    #endregion

    #region ResolveModule tests

    [Fact]
    public void ResolveModule_MatchesDevice()
    {
        var (deviceId, moduleId, name) = NetatmoWeatherTool.ResolveModule(DefaultDevices, "Living Room");

        Assert.Equal("70:ee:50:aa:bb:cc", deviceId);
        Assert.Null(moduleId);
        Assert.Equal("Living Room", name);
    }

    [Fact]
    public void ResolveModule_MatchesModule()
    {
        var (deviceId, moduleId, name) = NetatmoWeatherTool.ResolveModule(DefaultDevices, "Outdoor");

        Assert.Equal("70:ee:50:aa:bb:cc", deviceId);
        Assert.Equal("02:00:00:aa:bb:dd", moduleId);
        Assert.Equal("Outdoor", name);
    }

    [Fact]
    public void ResolveModule_NotFound_ReturnsNulls()
    {
        var (deviceId, moduleId, name) = NetatmoWeatherTool.ResolveModule(DefaultDevices, "Nonexistent");

        Assert.Null(deviceId);
        Assert.Null(moduleId);
        Assert.Null(name);
    }

    #endregion

    private static NetatmoWeatherTool CreateTool(INetatmoApiService service)
        => new(service, NullLogger<NetatmoWeatherTool>.Instance);

    #region Test helpers

    private class MockNetatmoApiService : INetatmoApiService
    {
        private readonly List<NetatmoDevice>? _devices;
        private readonly List<NetatmoMeasureBody>? _measureData;
        private readonly Exception? _stationException;
        private readonly Exception? _measureException;

        public string? LastMeasureDeviceId { get; private set; }
        public string? LastMeasureModuleId { get; private set; }

        public MockNetatmoApiService(
            List<NetatmoDevice>? devices = null,
            List<NetatmoMeasureBody>? measureData = null,
            Exception? stationException = null,
            Exception? measureException = null)
        {
            _devices = devices;
            _measureData = measureData;
            _stationException = stationException;
            _measureException = measureException;
        }

        public Task<List<NetatmoDevice>> GetStationDataAsync(CancellationToken cancellationToken = default)
        {
            if (_stationException is not null)
                throw _stationException;
            return Task.FromResult(_devices ?? []);
        }

        public Task<List<NetatmoMeasureBody>> GetMeasureAsync(
            string deviceId, string? moduleId = null,
            string scale = "30min", string type = "Temperature,Humidity",
            long? dateBegin = null, long? dateEnd = null,
            int? limit = null, CancellationToken cancellationToken = default)
        {
            LastMeasureDeviceId = deviceId;
            LastMeasureModuleId = moduleId;

            if (_measureException is not null)
                throw _measureException;
            return Task.FromResult(_measureData ?? []);
        }

        public Task<NetatmoTokens> ExchangeCodeForTokensAsync(string authorizationCode, CancellationToken cancellationToken = default)
            => throw new NotImplementedException();

        public string GetAuthorizationUrl(string state)
            => throw new NotImplementedException();
    }

    #endregion
}
