using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using HearLoveen.AudioService.Application.Commands;
using HearLoveen.AudioService.Domain.Entities;
using HearLoveen.AudioService.Domain.Repositories;

namespace AudioService.Tests.Commands;

public class UploadAudioCommandTests
{
    private readonly Mock<IAudioRecordingRepository> _recordingRepoMock;
    private readonly Mock<IChildRepository> _childRepoMock;
    private readonly Mock<IBlobStorageService> _blobStorageMock;
    private readonly Mock<IEventBus> _eventBusMock;
    private readonly Mock<ILogger<UploadAudioCommandHandler>> _loggerMock;
    private readonly UploadAudioCommandHandler _handler;

    public UploadAudioCommandTests()
    {
        _recordingRepoMock = new Mock<IAudioRecordingRepository>();
        _childRepoMock = new Mock<IChildRepository>();
        _blobStorageMock = new Mock<IBlobStorageService>();
        _eventBusMock = new Mock<IEventBus>();
        _loggerMock = new Mock<ILogger<UploadAudioCommandHandler>>();
        
        _handler = new UploadAudioCommandHandler(
            _recordingRepoMock.Object,
            _childRepoMock.Object,
            _blobStorageMock.Object,
            _eventBusMock.Object,
            _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var child = Child.Create(Guid.NewGuid(), "John", "Doe", DateTime.UtcNow.AddYears(-5), 
            HearingLossLevel.Moderate, true);
        
        _childRepoMock.Setup(x => x.GetByIdAsync(childId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(child);
        
        _blobStorageMock.Setup(x => x.UploadAsync(It.IsAny<string>(), It.IsAny<string>(), 
            It.IsAny<byte[]>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("blob-path/audio.wav");

        var command = new UploadAudioCommand(
            childId,
            Guid.NewGuid(),
            new byte[] { 1, 2, 3 },
            "test.wav",
            30,
            "home");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();
        _recordingRepoMock.Verify(x => x.AddAsync(It.IsAny<AudioRecording>(), 
            It.IsAny<CancellationToken>()), Times.Once);
        _eventBusMock.Verify(x => x.PublishAsync(It.IsAny<AudioUploadedIntegrationEvent>(), 
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ChildNotFound_ReturnsFailure()
    {
        // Arrange
        var childId = Guid.NewGuid();
        _childRepoMock.Setup(x => x.GetByIdAsync(childId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Child?)null);

        var command = new UploadAudioCommand(
            childId,
            Guid.NewGuid(),
            new byte[] { 1, 2, 3 },
            "test.wav",
            30,
            "home");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Child not found");
    }

    [Fact]
    public async Task Handle_InactiveChild_ReturnsFailure()
    {
        // Arrange
        var childId = Guid.NewGuid();
        var child = Child.Create(Guid.NewGuid(), "John", "Doe", DateTime.UtcNow.AddYears(-5), 
            HearingLossLevel.Moderate, true);
        
        // Use reflection to set IsActive to false
        typeof(Child).GetProperty("IsActive")!.SetValue(child, false);
        
        _childRepoMock.Setup(x => x.GetByIdAsync(childId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(child);

        var command = new UploadAudioCommand(
            childId,
            Guid.NewGuid(),
            new byte[] { 1, 2, 3 },
            "test.wav",
            30,
            "home");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be("Child account is inactive");
    }
}
