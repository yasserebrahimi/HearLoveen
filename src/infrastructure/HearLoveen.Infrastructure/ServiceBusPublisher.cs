using Azure.Messaging.ServiceBus;

namespace HearLoveen.Infrastructure.Messaging;
public class ServiceBusPublisher : IQueuePublisher, IAsyncDisposable
{
    private readonly ServiceBusSender _sender;
    public ServiceBusPublisher(string connectionString, string topicOrQueue)
    {
        var client = new ServiceBusClient(connectionString);
        _sender = client.CreateSender(topicOrQueue);
    }

    public async Task PublishAudioSubmittedAsync(Guid submissionId, string blobUrl, Guid childId, CancellationToken ct = default)
    {
        var payload = System.Text.Json.JsonSerializer.Serialize(new {
            submissionId, blobUrl, childId, type = "AudioSubmitted", ts = DateTime.UtcNow
        });
        var msg = new ServiceBusMessage(payload) { Subject = "AudioSubmitted", MessageId = submissionId.ToString() };
        await _sender.SendMessageAsync(msg, ct);
    }

    public async ValueTask DisposeAsync() => await _sender.DisposeAsync();
}
