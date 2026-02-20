using MCPForOllama.Server.Tools;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MCPForOllama.Server.Tests.Tools;

public class RandomNumberToolTests
{
    private readonly RandomNumberTool _tool = new(NullLogger<RandomNumberTool>.Instance);

    [Fact]
    public void GenerateRandomNumber_DefaultRange_ReturnsBetween1And100()
    {
        var result = _tool.GenerateRandomNumber();

        Assert.InRange(result, 1, 100);
    }

    [Fact]
    public void GenerateRandomNumber_CustomRange_ReturnsBetweenMinAndMax()
    {
        var result = _tool.GenerateRandomNumber(10, 20);

        Assert.InRange(result, 10, 20);
    }

    [Fact]
    public void GenerateRandomNumber_MinEqualsMax_ReturnsThatValue()
    {
        var result = _tool.GenerateRandomNumber(42, 42);

        Assert.Equal(42, result);
    }

    [Fact]
    public void GenerateRandomNumber_MinGreaterThanMax_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            _tool.GenerateRandomNumber(100, 1));

        Assert.Contains("min", ex.Message);
        Assert.Contains("max", ex.Message);
    }

    [Fact]
    public void GenerateRandomNumber_NegativeRange_Works()
    {
        var result = _tool.GenerateRandomNumber(-50, -10);

        Assert.InRange(result, -50, -10);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 1)]
    [InlineData(int.MinValue / 2, int.MinValue / 2)]
    public void GenerateRandomNumber_BoundaryValues_ReturnsBetweenMinAndMax(int min, int max)
    {
        var result = _tool.GenerateRandomNumber(min, max);

        Assert.InRange(result, min, max);
    }

    [Fact]
    public void GenerateRandomNumber_MultipleCallsDefaultRange_AllInRange()
    {
        for (var i = 0; i < 100; i++)
        {
            var result = _tool.GenerateRandomNumber();
            Assert.InRange(result, 1, 100);
        }
    }
}
