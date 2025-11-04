namespace HearLoveen.Infrastructure.Messaging;
public interface IQueuePublisher
{
    Task PublishAudioSubmittedAsync(Guid submissionId, string blobUrl, Guid childId, CancellationToken ct = default);
}
