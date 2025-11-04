using HearLoveen.Shared.Domain.Abstractions;

namespace HearLoveen.AudioService.Domain.Entities;

public class AudioRecording : Entity<Guid>, IAggregateRoot
{
    public Guid ChildId { get; private set; }
    public Guid ExerciseId { get; private set; }
    public string BlobStoragePath { get; private set; } = string.Empty;
    public int DurationSeconds { get; private set; }
    public string AudioFormat { get; private set; } = string.Empty;
    public string RecordingEnvironment { get; private set; } = string.Empty;
    public AudioRecordingStatus Status { get; private set; }
    public DateTime RecordedAt { get; private set; }
    
    private AudioRecording() { } // EF Core
    
    public static AudioRecording Create(
        Guid childId,
        Guid exerciseId,
        string blobStoragePath,
        int durationSeconds,
        string audioFormat,
        string recordingEnvironment)
    {
        var recording = new AudioRecording
        {
            Id = Guid.NewGuid(),
            ChildId = childId,
            ExerciseId = exerciseId,
            BlobStoragePath = blobStoragePath,
            DurationSeconds = durationSeconds,
            AudioFormat = audioFormat,
            RecordingEnvironment = recordingEnvironment,
            Status = AudioRecordingStatus.Uploaded,
            RecordedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
        
        recording.RaiseDomainEvent(new AudioRecordingCreatedDomainEvent(recording.Id, childId));
        
        return recording;
    }
    
    public void MarkAsProcessing()
    {
        Status = AudioRecordingStatus.Processing;
        UpdatedAt = DateTime.UtcNow;
    }
    
    public void MarkAsCompleted()
    {
        Status = AudioRecordingStatus.Completed;
        UpdatedAt = DateTime.UtcNow;
        RaiseDomainEvent(new AudioRecordingCompletedDomainEvent(Id, ChildId));
    }
    
    public void MarkAsFailed(string errorMessage)
    {
        Status = AudioRecordingStatus.Failed;
        UpdatedAt = DateTime.UtcNow;
    }
}

public enum AudioRecordingStatus
{
    Uploaded,
    Processing,
    Completed,
    Failed
}

public record AudioRecordingCreatedDomainEvent(Guid RecordingId, Guid ChildId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}

public record AudioRecordingCompletedDomainEvent(Guid RecordingId, Guid ChildId) : IDomainEvent
{
    public Guid EventId { get; } = Guid.NewGuid();
    public DateTime OccurredOn { get; } = DateTime.UtcNow;
}
