using System.ComponentModel;
using ModelContextProtocol.Server;

namespace MCPForOllama.Server.Tools;

[McpServerToolType]
public static class RandomNumberTool
{
    [McpServerTool, Description("Generates a random integer between min and max (inclusive).")]
    public static int GenerateRandomNumber(
        [Description("The minimum value (inclusive). Defaults to 1.")] int min = 1,
        [Description("The maximum value (inclusive). Defaults to 100.")] int max = 100)
    {
        if (min > max)
        {
            throw new ArgumentException($"min ({min}) must be less than or equal to max ({max}).");
        }

        // Random.Shared.Next upper bound is exclusive, so add 1 for inclusive max
        return Random.Shared.Next(min, max + 1);
    }
}
