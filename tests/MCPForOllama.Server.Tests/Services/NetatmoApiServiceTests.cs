using System.Net;
using System.Text.Json;
using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MCPForOllama.Server.Tests.Services;

public class NetatmoApiServiceTests
{
    private static readonly NetatmoSettings DefaultSettings = new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-client-secret",
        RedirectUri = "http://localhost:5000/netatmo/callback",
        TokenFilePath = "test-tokens.json"
    };

    private static NetatmoApiService CreateService(
        MockHttpMessageHandler handler,
        INetatmoTokenStore? tokenStore = null)
    {
        var httpClient = new HttpClient(handler);
        var options = Options.Create(DefaultSettings);
        return new NetatmoApiService(
            httpClient,
            tokenStore ?? new InMemoryTokenStore(),
            options,
            NullLogger<NetatmoApiService>.Instance);
    }

    [Fact]
    public void GetAuthorizationUrl_IncludesRequiredParameters()
    {
        var handler = new MockHttpMessageHandler();
        var service = CreateService(handler);

        var url = service.GetAuthorizationUrl("test-state");

        Assert.Contains("client_id=test-client-id", url);
        Assert.Contains("redirect_uri=", url);
        Assert.Contains("scope=read_station", url);
        Assert.Contains("state=test-state", url);
        Assert.Contains("response_type=code", url);
        Assert.StartsWith("https://api.netatmo.com/oauth2/authorize?", url);
    }

    [Fact]
    public async Task ExchangeCodeForTokensAsync_SavesTokens()
    {
        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "new-access",
            refresh_token = "new-refresh",
            expires_in = 3600
        });

        var handler = new MockHttpMessageHandler(tokenResponse);
        var tokenStore = new InMemoryTokenStore();
        var service = CreateService(handler, tokenStore);

        var tokens = await service.ExchangeCodeForTokensAsync("auth-code-123");

        Assert.Equal("new-access", tokens.AccessToken);
        Assert.Equal("new-refresh", tokens.RefreshToken);
        Assert.False(tokens.IsExpired);

        var stored = await tokenStore.LoadAsync();
        Assert.NotNull(stored);
        Assert.Equal("new-access", stored.AccessToken);
    }

    [Fact]
    public async Task GetStationDataAsync_NoTokens_ThrowsInvalidOperationException()
    {
        var handler = new MockHttpMessageHandler();
        var emptyStore = new InMemoryTokenStore();
        var service = CreateService(handler, emptyStore);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetStationDataAsync());

        Assert.Contains("authenticate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetStationDataAsync_ValidTokens_ReturnsDevices()
    {
        var stationResponse = JsonSerializer.Serialize(new
        {
            body = new
            {
                devices = new[]
                {
                    new
                    {
                        station_name = "Home",
                        module_name = "Living Room",
                        dashboard_data = new { Temperature = 21.5, Humidity = 45 },
                        modules = new[]
                        {
                            new
                            {
                                module_name = "Outdoor",
                                dashboard_data = new { Temperature = 8.2, Humidity = 78 }
                            }
                        }
                    }
                }
            }
        });

        var handler = new MockHttpMessageHandler(stationResponse);
        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "valid-token",
            RefreshToken = "valid-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        var service = CreateService(handler, tokenStore);
        var devices = await service.GetStationDataAsync();

        Assert.Single(devices);
        Assert.Equal("Home", devices[0].StationName);
        Assert.Equal("Living Room", devices[0].ModuleName);
        Assert.Equal(21.5, devices[0].DashboardData?.Temperature);
        Assert.Single(devices[0].Modules);
        Assert.Equal("Outdoor", devices[0].Modules[0].ModuleName);
        Assert.Equal(8.2, devices[0].Modules[0].DashboardData?.Temperature);
    }

    [Fact]
    public async Task GetStationDataAsync_ExpiredToken_RefreshesAndRetries()
    {
        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "refreshed-access",
            refresh_token = "refreshed-refresh",
            expires_in = 3600
        });

        var stationResponse = JsonSerializer.Serialize(new
        {
            body = new { devices = Array.Empty<object>() }
        });

        var callCount = 0;
        var handler = new MockHttpMessageHandler(request =>
        {
            callCount++;
            if (request.RequestUri!.AbsolutePath.Contains("token"))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenResponse)
                };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(stationResponse)
            };
        });

        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "expired-token",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1) // expired
        });

        var service = CreateService(handler, tokenStore);
        var devices = await service.GetStationDataAsync();

        Assert.NotNull(devices);
        var stored = await tokenStore.LoadAsync();
        Assert.Equal("refreshed-access", stored!.AccessToken);
    }

    #region GetMeasureAsync tests

    [Fact]
    public async Task GetMeasureAsync_NoTokens_ThrowsInvalidOperationException()
    {
        var handler = new MockHttpMessageHandler();
        var emptyStore = new InMemoryTokenStore();
        var service = CreateService(handler, emptyStore);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.GetMeasureAsync("70:ee:50:aa:bb:cc"));

        Assert.Contains("authenticate", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task GetMeasureAsync_ValidTokens_ReturnsMeasureData()
    {
        var measureResponse = JsonSerializer.Serialize(new
        {
            body = new[]
            {
                new
                {
                    beg_time = 1700000000L,
                    step_time = 1800L,
                    value = new[] { new double?[] { 21.5, 45 }, new double?[] { 21.3, 46 } }
                }
            },
            status = "ok"
        });

        var handler = new MockHttpMessageHandler(measureResponse);
        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "valid-token",
            RefreshToken = "valid-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        var service = CreateService(handler, tokenStore);
        var result = await service.GetMeasureAsync("70:ee:50:aa:bb:cc");

        Assert.Single(result);
        Assert.Equal(1700000000L, result[0].BegTime);
        Assert.Equal(1800L, result[0].StepTime);
        Assert.Equal(2, result[0].Value.Count);
    }

    [Fact]
    public async Task GetMeasureAsync_BuildsCorrectQueryString()
    {
        var measureResponse = JsonSerializer.Serialize(new
        {
            body = Array.Empty<object>(),
            status = "ok"
        });

        string? capturedUri = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            capturedUri = request.RequestUri!.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(measureResponse)
            };
        });

        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "valid-token",
            RefreshToken = "valid-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        var service = CreateService(handler, tokenStore);
        await service.GetMeasureAsync("70:ee:50:aa:bb:cc", "02:00:00:aa:bb:dd",
            "1hour", "Temperature,Humidity", 1700000000, 1700003600);

        Assert.NotNull(capturedUri);
        Assert.Contains("device_id=70%3Aee%3A50%3Aaa%3Abb%3Acc", capturedUri);
        Assert.Contains("module_id=02%3A00%3A00%3Aaa%3Abb%3Add", capturedUri);
        Assert.Contains("scale=1hour", capturedUri);
        Assert.Contains("type=Temperature%2CHumidity", capturedUri);
        Assert.Contains("date_begin=1700000000", capturedUri);
        Assert.Contains("date_end=1700003600", capturedUri);
        Assert.DoesNotContain("optimize", capturedUri);
    }

    [Fact]
    public async Task GetMeasureAsync_ExpiredToken_RefreshesAndRetries()
    {
        var tokenResponse = JsonSerializer.Serialize(new
        {
            access_token = "refreshed-access",
            refresh_token = "refreshed-refresh",
            expires_in = 3600
        });

        var measureResponse = JsonSerializer.Serialize(new
        {
            body = Array.Empty<object>(),
            status = "ok"
        });

        var handler = new MockHttpMessageHandler(request =>
        {
            if (request.RequestUri!.AbsolutePath.Contains("token"))
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(tokenResponse)
                };

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(measureResponse)
            };
        });

        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "expired-token",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        });

        var service = CreateService(handler, tokenStore);
        var result = await service.GetMeasureAsync("70:ee:50:aa:bb:cc");

        Assert.NotNull(result);
        var stored = await tokenStore.LoadAsync();
        Assert.Equal("refreshed-access", stored!.AccessToken);
    }

    [Fact]
    public async Task GetMeasureAsync_WithoutModuleId_OmitsFromQuery()
    {
        var measureResponse = JsonSerializer.Serialize(new
        {
            body = Array.Empty<object>(),
            status = "ok"
        });

        string? capturedUri = null;
        var handler = new MockHttpMessageHandler(request =>
        {
            capturedUri = request.RequestUri!.ToString();
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(measureResponse)
            };
        });

        var tokenStore = new InMemoryTokenStore(new NetatmoTokens
        {
            AccessToken = "valid-token",
            RefreshToken = "valid-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        });

        var service = CreateService(handler, tokenStore);
        await service.GetMeasureAsync("70:ee:50:aa:bb:cc");

        Assert.NotNull(capturedUri);
        Assert.DoesNotContain("module_id", capturedUri);
    }

    #endregion

    #region Test helpers

    private class InMemoryTokenStore(NetatmoTokens? initialTokens = null) : INetatmoTokenStore
    {
        private NetatmoTokens? _tokens = initialTokens;

        public Task<NetatmoTokens?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult(_tokens);

        public Task SaveAsync(NetatmoTokens tokens, CancellationToken cancellationToken = default)
        {
            _tokens = tokens;
            return Task.CompletedTask;
        }
    }

    private class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage>? _handler;
        private readonly string? _defaultResponse;

        public MockHttpMessageHandler(string? defaultResponse = null)
        {
            _defaultResponse = defaultResponse;
        }

        public MockHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> handler)
        {
            _handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (_handler is not null)
                return Task.FromResult(_handler(request));

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(_defaultResponse ?? "{}")
            });
        }
    }

    #endregion
}
