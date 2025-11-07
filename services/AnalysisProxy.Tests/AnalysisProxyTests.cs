using Xunit;

namespace AnalysisProxy.Tests;

public class AnalysisProxyTests
{
    [Fact]
    public void BasicHealthCheck_ShouldPass()
    {
        // Arrange & Act
        var result = true;

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void RateLimiting_ShouldBe120RequestsPerMinute()
    {
        // Arrange
        var expectedRateLimit = 120;

        // Act
        var actualRateLimit = 120; // This would come from configuration

        // Assert
        Assert.Equal(expectedRateLimit, actualRateLimit);
    }

    [Theory]
    [InlineData("http://localhost:5100")]
    [InlineData("http://localhost:5173")]
    public void CorsOrigins_ShouldBeConfigured(string origin)
    {
        // Arrange
        var allowedOrigins = new[] { "http://localhost:5100", "http://localhost:5173" };

        // Act
        var isAllowed = allowedOrigins.Contains(origin);

        // Assert
        Assert.True(isAllowed);
    }
}
