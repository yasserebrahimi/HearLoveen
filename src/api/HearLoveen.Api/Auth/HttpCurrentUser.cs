using System.Security.Claims;
using HearLoveen.Infrastructure;

namespace HearLoveen.Api.Auth;

public class HttpCurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _http;
    private readonly HashSet<Guid> _childScope = new();

    public HttpCurrentUser(IHttpContextAccessor http)
    {
        _http = http;
        var user = _http.HttpContext?.User;
        if (user?.Identity?.IsAuthenticated == true)
        {
            IsAuthenticated = true;
            var sub = user.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user.FindFirst("sub")?.Value;
            if (Guid.TryParse(sub, out var g)) UserId = g;

            var roles = user.FindAll(ClaimTypes.Role).Select(c => c.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            IsAdmin = roles.Contains("Admin");
            IsTherapist = roles.Contains("Therapist");

            var childCsv = user.FindFirst("child_scope")?.Value;
            if (!string.IsNullOrWhiteSpace(childCsv))
            {
                foreach (var part in childCsv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                    if (Guid.TryParse(part, out var cid)) _childScope.Add(cid);
            }
        }
    }

    public bool IsAuthenticated { get; }
    public bool IsAdmin { get; }
    public bool IsTherapist { get; }
    public Guid? UserId { get; }
    public IReadOnlyCollection<Guid> ChildScope => _childScope;
}
