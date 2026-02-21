using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using MCPForOllama.Server.Models;
using Microsoft.Extensions.Options;

namespace MCPForOllama.Server.Services;

public class NetatmoApiService(
    HttpClient httpClient,
    INetatmoTokenStore tokenStore,
    IOptions<NetatmoSettings> options,
    ILogger<NetatmoApiService> logger) : INetatmoApiService
{
    private const string AuthorizeUrl = "https://api.netatmo.com/oauth2/authorize";
    private const string TokenUrl = "https://api.netatmo.com/oauth2/token";
    private const string StationDataUrl = "https://api.netatmo.com/api/getstationsdata";
    private const string MeasureUrl = "https://api.netatmo.com/api/getmeasure";

    private readonly NetatmoSettings _settings = options.Value;

    public string GetAuthorizationUrl(string state)
    {
        var parameters = new Dictionary<string, string>
        {
            ["client_id"] = _settings.ClientId,
            ["redirect_uri"] = _settings.RedirectUri,
            ["scope"] = "read_station",
            ["state"] = state,
            ["response_type"] = "code"
        };

        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        return $"{AuthorizeUrl}?{query}";
    }

    public async Task<NetatmoTokens> ExchangeCodeForTokensAsync(
        string authorizationCode, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("Exchanging authorization code for tokens");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["code"] = authorizationCode,
            ["redirect_uri"] = _settings.RedirectUri,
            ["scope"] = "read_station"
        });

        var response = await httpClient.PostAsync(TokenUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokens = await ParseTokenResponseAsync(response, cancellationToken);
        await tokenStore.SaveAsync(tokens, cancellationToken);

        logger.LogInformation("Successfully obtained and stored Netatmo tokens");
        return tokens;
    }

    public async Task<List<NetatmoDevice>> GetStationDataAsync(CancellationToken cancellationToken = default)
    {
        var tokens = await tokenStore.LoadAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Netatmo is not authenticated. Please visit http://localhost:5000/netatmo/auth to connect your Netatmo account.");

        if (tokens.IsExpired)
        {
            logger.LogInformation("Access token expired, refreshing");
            tokens = await RefreshTokensAsync(tokens.RefreshToken, cancellationToken);
        }

        var response = await SendStationDataRequestAsync(tokens.AccessToken, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Got 401 from Netatmo API, attempting token refresh");
            tokens = await RefreshTokensAsync(tokens.RefreshToken, cancellationToken);
            response = await SendStationDataRequestAsync(tokens.AccessToken, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<NetatmoApiResponse>(json);

        return data?.Body.Devices ?? [];
    }

    public async Task<List<NetatmoMeasureBody>> GetMeasureAsync(
        string deviceId, string? moduleId = null,
        string scale = "30min", string type = "Temperature,Humidity",
        long? dateBegin = null, long? dateEnd = null,
        int? limit = null, CancellationToken cancellationToken = default)
    {
        var tokens = await tokenStore.LoadAsync(cancellationToken)
            ?? throw new InvalidOperationException(
                "Netatmo is not authenticated. Please visit http://localhost:5000/netatmo/auth to connect your Netatmo account.");

        if (tokens.IsExpired)
        {
            logger.LogInformation("Access token expired, refreshing");
            tokens = await RefreshTokensAsync(tokens.RefreshToken, cancellationToken);
        }

        var response = await SendMeasureRequestAsync(
            tokens.AccessToken, deviceId, moduleId, scale, type, dateBegin, dateEnd, limit, cancellationToken);

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            logger.LogWarning("Got 401 from Netatmo API, attempting token refresh");
            tokens = await RefreshTokensAsync(tokens.RefreshToken, cancellationToken);
            response = await SendMeasureRequestAsync(
                tokens.AccessToken, deviceId, moduleId, scale, type, dateBegin, dateEnd, limit, cancellationToken);
        }

        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        var data = JsonSerializer.Deserialize<NetatmoMeasureApiResponse>(json);

        return data?.Body ?? [];
    }

    private async Task<HttpResponseMessage> SendMeasureRequestAsync(
        string accessToken, string deviceId, string? moduleId,
        string scale, string type, long? dateBegin, long? dateEnd,
        int? limit, CancellationToken cancellationToken)
    {
        var parameters = new Dictionary<string, string>
        {
            ["device_id"] = deviceId,
            ["scale"] = scale,
            ["type"] = type
        };

        if (moduleId is not null)
            parameters["module_id"] = moduleId;
        if (dateBegin.HasValue)
            parameters["date_begin"] = dateBegin.Value.ToString();
        if (dateEnd.HasValue)
            parameters["date_end"] = dateEnd.Value.ToString();
        if (limit.HasValue)
            parameters["limit"] = limit.Value.ToString();

        var query = string.Join("&", parameters.Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value)}"));
        var request = new HttpRequestMessage(HttpMethod.Get, $"{MeasureUrl}?{query}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<HttpResponseMessage> SendStationDataRequestAsync(
        string accessToken, CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, StationDataUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return await httpClient.SendAsync(request, cancellationToken);
    }

    private async Task<NetatmoTokens> RefreshTokensAsync(
        string refreshToken, CancellationToken cancellationToken)
    {
        logger.LogInformation("Refreshing Netatmo tokens");

        var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token",
            ["client_id"] = _settings.ClientId,
            ["client_secret"] = _settings.ClientSecret,
            ["refresh_token"] = refreshToken
        });

        var response = await httpClient.PostAsync(TokenUrl, content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var tokens = await ParseTokenResponseAsync(response, cancellationToken);
        await tokenStore.SaveAsync(tokens, cancellationToken);

        logger.LogInformation("Successfully refreshed and stored Netatmo tokens");
        return tokens;
    }

    private static async Task<NetatmoTokens> ParseTokenResponseAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        var json = await response.Content.ReadAsStringAsync(cancellationToken);
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        return new NetatmoTokens
        {
            AccessToken = root.GetProperty("access_token").GetString() ?? string.Empty,
            RefreshToken = root.GetProperty("refresh_token").GetString() ?? string.Empty,
            ExpiresAt = DateTimeOffset.UtcNow.AddSeconds(root.GetProperty("expires_in").GetInt32())
        };
    }
}
