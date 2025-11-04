using HearLoveen.Shared.Domain.Abstractions;

namespace HearLoveen.AudioService.Domain.Entities;

public class Child : Entity<Guid>, IAggregateRoot
{
    public Guid UserId { get; private set; }
    public string FirstName { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public DateTime DateOfBirth { get; private set; }
    public HearingLossLevel HearingLossLevel { get; private set; }
    public bool HasCochlearImplant { get; private set; }
    public bool IsActive { get; private set; }
    
    private readonly List<AudioRecording> _recordings = new();
    public IReadOnlyCollection<AudioRecording> Recordings => _recordings.AsReadOnly();
    
    private Child() { } // EF Core
    
    public static Child Create(Guid userId, string firstName, string lastName, DateTime dateOfBirth, HearingLossLevel hearingLossLevel, bool hasCochlearImplant)
    {
        var child = new Child
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            FirstName = firstName,
            LastName = lastName,
            DateOfBirth = dateOfBirth,
            HearingLossLevel = hearingLossLevel,
            HasCochlearImplant = hasCochlearImplant,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
        
        return child;
    }
    
    public void AddRecording(AudioRecording recording)
    {
        _recordings.Add(recording);
    }
    
    public int GetAge()
    {
        var today = DateTime.Today;
        var age = today.Year - DateOfBirth.Year;
        if (DateOfBirth.Date > today.AddYears(-age)) age--;
        return age;
    }
}

public enum HearingLossLevel
{
    Mild,
    Moderate,
    Severe,
    Profound
}
