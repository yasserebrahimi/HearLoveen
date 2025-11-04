namespace HearLoveen.Domain.Entities;
public class TherapistAssignment
{
    public Guid Id { get; set; }
    public Guid TherapistUserId { get; set; }
    public Guid ChildId { get; set; }
    public DateTime AssignedAtUtc { get; set; } = DateTime.UtcNow;
}
