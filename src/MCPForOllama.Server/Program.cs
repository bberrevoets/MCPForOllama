var builder = WebApplication.CreateBuilder(args);

builder.Services
    .AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssembly();

var app = builder.Build();

app.MapMcp("mcp");
app.MapGet("/health", () => Results.Ok(new { status = "healthy", service = "MCPForOllama" }));

app.Run();
