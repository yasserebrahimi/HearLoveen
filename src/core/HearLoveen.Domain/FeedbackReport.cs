namespace HearLoveen.Domain.Entities;
public class FeedbackReport
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public int Score0_100 { get; set; }
    public string Weakness { get; set; } = string.Empty;
    public string Recommendation { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
