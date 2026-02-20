using MCPForOllama.Server.Tools;
using Xunit;

namespace MCPForOllama.Server.Tests.Tools;

public class RandomNumberToolTests
{
    [Fact]
    public void GenerateRandomNumber_DefaultRange_ReturnsBetween1And100()
    {
        var result = RandomNumberTool.GenerateRandomNumber();

        Assert.InRange(result, 1, 100);
    }

    [Fact]
    public void GenerateRandomNumber_CustomRange_ReturnsBetweenMinAndMax()
    {
        var result = RandomNumberTool.GenerateRandomNumber(10, 20);

        Assert.InRange(result, 10, 20);
    }

    [Fact]
    public void GenerateRandomNumber_MinEqualsMax_ReturnsThatValue()
    {
        var result = RandomNumberTool.GenerateRandomNumber(42, 42);

        Assert.Equal(42, result);
    }

    [Fact]
    public void GenerateRandomNumber_MinGreaterThanMax_ThrowsArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() =>
            RandomNumberTool.GenerateRandomNumber(100, 1));

        Assert.Contains("min", ex.Message);
        Assert.Contains("max", ex.Message);
    }

    [Fact]
    public void GenerateRandomNumber_NegativeRange_Works()
    {
        var result = RandomNumberTool.GenerateRandomNumber(-50, -10);

        Assert.InRange(result, -50, -10);
    }

    [Theory]
    [InlineData(0, 0)]
    [InlineData(-1, 1)]
    [InlineData(int.MinValue / 2, int.MinValue / 2)]
    public void GenerateRandomNumber_BoundaryValues_ReturnsBetweenMinAndMax(int min, int max)
    {
        var result = RandomNumberTool.GenerateRandomNumber(min, max);

        Assert.InRange(result, min, max);
    }

    [Fact]
    public void GenerateRandomNumber_MultipleCallsDefaultRange_AllInRange()
    {
        for (var i = 0; i < 100; i++)
        {
            var result = RandomNumberTool.GenerateRandomNumber();
            Assert.InRange(result, 1, 100);
        }
    }
}
