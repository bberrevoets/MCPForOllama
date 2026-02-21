using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace MCPForOllama.Server.Tests.Services;

public class FileNetatmoTokenStoreTests : IDisposable
{
    private readonly string _tempDir;
    private readonly FileNetatmoTokenStore _store;

    public FileNetatmoTokenStoreTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"netatmo-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var tokenFilePath = Path.Combine(_tempDir, "tokens.json");
        var settings = Options.Create(new NetatmoSettings { TokenFilePath = tokenFilePath });
        _store = new FileNetatmoTokenStore(settings, NullLogger<FileNetatmoTokenStore>.Instance);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public async Task LoadAsync_FileDoesNotExist_ReturnsNull()
    {
        var result = await _store.LoadAsync();

        Assert.Null(result);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_ReturnsSameTokens()
    {
        var tokens = new NetatmoTokens
        {
            AccessToken = "access-123",
            RefreshToken = "refresh-456",
            ExpiresAt = new DateTimeOffset(2026, 6, 15, 12, 0, 0, TimeSpan.Zero)
        };

        await _store.SaveAsync(tokens);
        var loaded = await _store.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("access-123", loaded.AccessToken);
        Assert.Equal("refresh-456", loaded.RefreshToken);
        Assert.Equal(tokens.ExpiresAt, loaded.ExpiresAt);
    }

    [Fact]
    public async Task SaveAsync_OverwritesExistingFile()
    {
        var original = new NetatmoTokens
        {
            AccessToken = "old-access",
            RefreshToken = "old-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        var updated = new NetatmoTokens
        {
            AccessToken = "new-access",
            RefreshToken = "new-refresh",
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(2)
        };

        await _store.SaveAsync(original);
        await _store.SaveAsync(updated);
        var loaded = await _store.LoadAsync();

        Assert.NotNull(loaded);
        Assert.Equal("new-access", loaded.AccessToken);
        Assert.Equal("new-refresh", loaded.RefreshToken);
    }

    [Fact]
    public async Task LoadAsync_CorruptedFile_ReturnsNull()
    {
        var tokenFilePath = Path.Combine(_tempDir, "tokens.json");
        await File.WriteAllTextAsync(tokenFilePath, "not-valid-json{{{");

        var result = await _store.LoadAsync();

        Assert.Null(result);
    }
}
