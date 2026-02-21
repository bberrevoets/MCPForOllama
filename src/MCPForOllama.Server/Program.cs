using MCPForOllama.Server.Models;
using MCPForOllama.Server.Services;
using MCPForOllama.Server.Tools;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services));

    builder.Services.Configure<NetatmoSettings>(
        builder.Configuration.GetSection(NetatmoSettings.SectionName));

    builder.Services.AddSingleton<INetatmoTokenStore, FileNetatmoTokenStore>();

    builder.Services.AddHttpClient<INetatmoApiService, NetatmoApiService>(client =>
    {
        client.DefaultRequestHeaders.Accept.Add(
            new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
    });

    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithTools<RandomNumberTool>()
        .WithTools<NetatmoWeatherTool>();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapMcp("mcp");
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MCPForOllama" }));

    app.MapGet("/netatmo/auth", (INetatmoApiService netatmo) =>
    {
        var state = Guid.NewGuid().ToString("N");
        var url = netatmo.GetAuthorizationUrl(state);
        return Results.Redirect(url);
    });

    app.MapGet("/netatmo/callback", async (
        string code,
        string state,
        INetatmoApiService netatmo,
        ILogger<Program> callbackLogger) =>
    {
        try
        {
            await netatmo.ExchangeCodeForTokensAsync(code);
            return Results.Ok(new { status = "authenticated", message = "Netatmo tokens stored successfully. You can close this page." });
        }
        catch (Exception ex)
        {
            callbackLogger.LogError(ex, "Netatmo OAuth callback failed");
            return Results.Problem($"Authentication failed: {ex.Message}");
        }
    });

    Log.Information("MCPForOllama server starting on {Url}", "http://0.0.0.0:5000");

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MCPForOllama server terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}
