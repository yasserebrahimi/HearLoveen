namespace HearLoveen.Domain.Entities;
public class AudioSubmission
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string BlobUrl { get; set; } = default!;
    public int DurationSec { get; set; }
    public string MimeType { get; set; } = "audio/wav";
    public DateTime SubmittedAtUtc { get; set; } = DateTime.UtcNow;
    public string ModelVersion { get; set; } = "v1";
}
