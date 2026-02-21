using MCPForOllama.Server.Models;

namespace MCPForOllama.Server.Services;

public interface INetatmoApiService
{
    Task<List<NetatmoDevice>> GetStationDataAsync(CancellationToken cancellationToken = default);
    Task<List<NetatmoMeasureBody>> GetMeasureAsync(
        string deviceId, string? moduleId = null,
        string scale = "30min", string type = "Temperature,Humidity",
        long? dateBegin = null, long? dateEnd = null,
        int? limit = null, CancellationToken cancellationToken = default);
    Task<NetatmoTokens> ExchangeCodeForTokensAsync(string authorizationCode, CancellationToken cancellationToken = default);
    string GetAuthorizationUrl(string state);
}
