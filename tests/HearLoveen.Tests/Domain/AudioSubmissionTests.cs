using FluentAssertions;
using HearLoveen.Domain.Entities;
using Xunit;

namespace HearLoveen.Tests.Domain;

public class AudioSubmissionTests
{
    [Fact]
    public void AudioSubmission_ShouldBeCreated_WithValidData()
    {
        // Arrange
        var id = Guid.NewGuid();
        var childId = Guid.NewGuid();
        var blobUrl = "https://storage.blob.core.windows.net/audio/test.wav";
        var uploadedAt = DateTime.UtcNow;

        // Act
        var submission = new AudioSubmission
        {
            Id = id,
            ChildId = childId,
            BlobUrl = blobUrl,
            UploadedAt = uploadedAt
        };

        // Assert
        submission.Should().NotBeNull();
        submission.Id.Should().Be(id);
        submission.ChildId.Should().Be(childId);
        submission.BlobUrl.Should().Be(blobUrl);
        submission.UploadedAt.Should().Be(uploadedAt);
    }

    [Fact]
    public void AudioSubmission_ShouldTrack_ProcessingStatus()
    {
        // Arrange
        var submission = new AudioSubmission
        {
            Id = Guid.NewGuid(),
            ChildId = Guid.NewGuid(),
            BlobUrl = "https://test.com/audio.wav",
            UploadedAt = DateTime.UtcNow,
            ProcessedAt = null
        };

        // Act
        submission.ProcessedAt = DateTime.UtcNow;

        // Assert
        submission.ProcessedAt.Should().NotBeNull();
        submission.ProcessedAt.Should().BeAfter(submission.UploadedAt);
    }
}
