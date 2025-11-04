namespace HearLoveen.Domain.Entities;
public class PhonemeRating
{
    public Guid Id { get; set; }
    public Guid ChildId { get; set; }
    public string Phoneme { get; set; } = default!;
    public double Rating { get; set; } = 1000;
    public double Volatility { get; set; } = 0.06;
    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
public class PhonemePrerequisite
{
    public Guid Id { get; set; }
    public string Phoneme { get; set; } = default!;
    public string Requires { get; set; } = default!;
}
