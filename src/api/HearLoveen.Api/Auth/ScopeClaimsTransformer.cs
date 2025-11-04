using System.Security.Claims;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Api.Auth;

public class ScopeClaimsTransformer : IClaimsTransformation
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    public ScopeClaimsTransformer(AppDbContext db, IHttpContextAccessor http) { _db = db; _http = http; }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true) return principal;
        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(id, out var therapistId)) return principal;

        var childIds = await _db.TherapistAssignments.AsNoTracking()
            .Where(a => a.TherapistUserId == therapistId)
            .Select(a => a.ChildId).ToListAsync();

        if (childIds.Count > 0)
        {
            var csv = string.Join(',', childIds);
            var identity = (ClaimsIdentity)principal.Identity!;
            identity.AddClaim(new Claim("child_scope", csv));
        }
        return principal;
    }
}
