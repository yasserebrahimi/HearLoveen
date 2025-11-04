namespace HearLoveen.Domain.Entities;
public class Child
{
    public Guid Id { get; set; }
    public Guid ParentId { get; set; }
    public string FirstName { get; set; } = default!;
    public int AgeYears { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
