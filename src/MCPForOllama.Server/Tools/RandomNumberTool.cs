using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCPForOllama.Server.Tools;

[McpServerToolType]
public class RandomNumberTool(ILogger<RandomNumberTool> logger)
{
    [McpServerTool, Description("Generates a random integer between min and max (inclusive).")]
    public string GenerateRandomNumber(
        [Description("The minimum value (inclusive). Defaults to 1.")] int min = 1,
        [Description("The maximum value (inclusive). Defaults to 100.")] int max = 100)
    {
        logger.LogInformation("GenerateRandomNumber invoked with min={Min}, max={Max}", min, max);

        if (min > max)
        {
            logger.LogWarning("Validation failed: min ({Min}) > max ({Max})", min, max);
            throw new ArgumentException($"min ({min}) must be less than or equal to max ({max}).");
        }

        // Random.Shared.Next upper bound is exclusive, so add 1 for inclusive max
        var result = Random.Shared.Next(min, max + 1);

        logger.LogInformation("GenerateRandomNumber result: {Result}", result);

        return result.ToString();
    }
}
