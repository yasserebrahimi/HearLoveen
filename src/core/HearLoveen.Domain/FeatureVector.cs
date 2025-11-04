namespace HearLoveen.Domain.Entities;
public class FeatureVector
{
    public Guid Id { get; set; }
    public Guid SubmissionId { get; set; }
    public double PitchAvg { get; set; }
    public double VolumeRms { get; set; }
    public double Clarity { get; set; }
    public double Wpm { get; set; }
    public string Extra { get; set; } = "{}";
}
