namespace MCPForOllama.Server.Models;

public class NetatmoSettings
{
    public const string SectionName = "Netatmo";

    public string ClientId { get; set; } = string.Empty;
    public string ClientSecret { get; set; } = string.Empty;
    public string RedirectUri { get; set; } = "http://localhost:5000/netatmo/callback";
    public string TokenFilePath { get; set; } = "netatmo-tokens.json";
}
