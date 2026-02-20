using System.ComponentModel;
using ModelContextProtocol.Server;
using Serilog;
using ILogger = Serilog.ILogger;

namespace MCPForOllama.Server.Tools;

[McpServerToolType]
public static class RandomNumberTool
{
    private static ILogger Logger => Log.ForContext(typeof(RandomNumberTool));

    [McpServerTool, Description("Generates a random integer between min and max (inclusive).")]
    public static int GenerateRandomNumber(
        [Description("The minimum value (inclusive). Defaults to 1.")] int min = 1,
        [Description("The maximum value (inclusive). Defaults to 100.")] int max = 100)
    {
        Logger.Information("GenerateRandomNumber invoked with min={Min}, max={Max}", min, max);

        if (min > max)
        {
            Logger.Warning("Validation failed: min ({Min}) > max ({Max})", min, max);
            throw new ArgumentException($"min ({min}) must be less than or equal to max ({max}).");
        }

        // Random.Shared.Next upper bound is exclusive, so add 1 for inclusive max
        var result = Random.Shared.Next(min, max + 1);

        Logger.Information("GenerateRandomNumber result: {Result}", result);

        return result;
    }
}
