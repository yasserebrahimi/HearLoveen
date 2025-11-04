namespace HearLoveen.Domain.Entities;
public class User
{
    public Guid Id { get; set; }
    public string Email { get; set; } = default!;
    public string Role { get; set; } = "Parent"; // Parent|Therapist|Admin
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
