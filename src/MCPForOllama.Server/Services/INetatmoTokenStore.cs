using MCPForOllama.Server.Models;

namespace MCPForOllama.Server.Services;

public interface INetatmoTokenStore
{
    Task<NetatmoTokens?> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(NetatmoTokens tokens, CancellationToken cancellationToken = default);
}
