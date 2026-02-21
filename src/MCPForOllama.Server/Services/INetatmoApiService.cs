using MCPForOllama.Server.Models;

namespace MCPForOllama.Server.Services;

public interface INetatmoApiService
{
    Task<List<NetatmoDevice>> GetStationDataAsync(CancellationToken cancellationToken = default);
    Task<NetatmoTokens> ExchangeCodeForTokensAsync(string authorizationCode, CancellationToken cancellationToken = default);
    string GetAuthorizationUrl(string state);
}
