namespace HearLoveen.Domain.Entities;
public class ChildCurriculum
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string FocusPhonemesCsv { get; set; } = string.Empty; // e.g., "R,S,CH"
    public int Difficulty { get; set; } = 1; // 1..5
    public int SuccessStreak { get; set; } = 0;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
