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

    builder.Services
        .AddMcpServer()
        .WithHttpTransport()
        .WithToolsFromAssembly();

    var app = builder.Build();

    app.UseSerilogRequestLogging();

    app.MapMcp("mcp");
    app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MCPForOllama" }));

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
