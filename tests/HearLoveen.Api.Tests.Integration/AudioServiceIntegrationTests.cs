using System.Net;
using System.Net.Http.Headers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HearLoveen.Api.Tests.Integration;

public class AudioServiceIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AudioServiceIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task HealthCheck_ReturnsHealthy()
    {
        // Act
        var response = await _client.GetAsync("/health");

        // Assert
        response.IsSuccessStatusCode.Should().BeTrue();
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Healthy");
    }

    [Fact]
    public async Task UploadAudio_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        using var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(new byte[100]);
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("audio/wav");
        content.Add(fileContent, "audio", "test.wav");
        content.Add(new StringContent(Guid.NewGuid().ToString()), "childId");

        // Act
        var response = await _client.PostAsync("/api/v1/audio/upload", content);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetAudioStatus_InvalidId_ReturnsUnauthorized()
    {
        // Arrange
        var submissionId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/audio/{submissionId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task ListAudioSubmissions_WithoutAuth_ReturnsUnauthorized()
    {
        // Arrange
        var childId = Guid.NewGuid();

        // Act
        var response = await _client.GetAsync($"/api/v1/audio/child/{childId}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
