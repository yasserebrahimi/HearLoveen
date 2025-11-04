using System.Security.Claims;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Api.Auth;

public class TherapistAssignedRequirement : IAuthorizationRequirement { }

public class TherapistAssignedHandler : AuthorizationHandler<TherapistAssignedRequirement, Guid>
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public TherapistAssignedHandler(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    protected override async Task HandleRequirementAsync(AuthorizationHandlerContext context, TherapistAssignedRequirement requirement, Guid resourceSubmissionId)
    {
        var userIdClaim = _http.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrWhiteSpace(userIdClaim)) return;

        // Find childId of submission, then check assignment
        var sub = await _db.AudioSubmissions.AsNoTracking().FirstOrDefaultAsync(s => s.Id == resourceSubmissionId);
        if (sub is null) return;

        var therapistUserId = Guid.TryParse(userIdClaim, out var g) ? g : Guid.Empty;
        var ok = await _db.TherapistAssignments.AsNoTracking()
            .AnyAsync(a => a.TherapistUserId == therapistUserId && a.ChildId == sub.ChildId);

        if (ok) context.Succeed(requirement);
    }
}
