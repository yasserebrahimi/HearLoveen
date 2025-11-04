namespace HearLoveen.Infrastructure;
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsTherapist { get; }
    System.Guid? UserId { get; }
    System.Collections.Generic.IReadOnlyCollection<System.Guid> ChildScope { get; }
}
