using System.Text.Json;
using MCPForOllama.Server.Models;
using Microsoft.Extensions.Options;

namespace MCPForOllama.Server.Services;

public class FileNetatmoTokenStore(
    IOptions<NetatmoSettings> options,
    ILogger<FileNetatmoTokenStore> logger) : INetatmoTokenStore
{
    private readonly string _filePath = options.Value.TokenFilePath;
    private static readonly SemaphoreSlim Lock = new(1, 1);

    public async Task<NetatmoTokens?> LoadAsync(CancellationToken cancellationToken = default)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            if (!File.Exists(_filePath))
            {
                logger.LogDebug("Token file {FilePath} does not exist", _filePath);
                return null;
            }

            var json = await File.ReadAllTextAsync(_filePath, cancellationToken);
            var tokens = JsonSerializer.Deserialize<NetatmoTokens>(json);

            if (tokens is null)
            {
                logger.LogWarning("Token file {FilePath} deserialized to null", _filePath);
                return null;
            }

            logger.LogDebug("Loaded tokens from {FilePath}, expires at {ExpiresAt}", _filePath, tokens.ExpiresAt);
            return tokens;
        }
        catch (JsonException ex)
        {
            logger.LogWarning(ex, "Token file {FilePath} is corrupted, returning null", _filePath);
            return null;
        }
        finally
        {
            Lock.Release();
        }
    }

    public async Task SaveAsync(NetatmoTokens tokens, CancellationToken cancellationToken = default)
    {
        await Lock.WaitAsync(cancellationToken);
        try
        {
            var json = JsonSerializer.Serialize(tokens, new JsonSerializerOptions { WriteIndented = true });
            var tempPath = _filePath + ".tmp";

            await File.WriteAllTextAsync(tempPath, json, cancellationToken);
            File.Move(tempPath, _filePath, overwrite: true);

            logger.LogInformation("Saved tokens to {FilePath}, expires at {ExpiresAt}", _filePath, tokens.ExpiresAt);
        }
        finally
        {
            Lock.Release();
        }
    }
}
