using System.Security.Claims;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace HearLoveen.Api.Auth;

public class ScopeClaimsTransformer : IClaimsTransformation
{
    private readonly AppDbContext _db;
    private readonly IHttpContextAccessor _http;
    private readonly IMemoryCache _cache;
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

    public ScopeClaimsTransformer(AppDbContext db, IHttpContextAccessor http, IMemoryCache cache)
    {
        _db = db;
        _http = http;
        _cache = cache;
    }

    public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
    {
        if (principal.Identity?.IsAuthenticated != true)
            return principal;

        var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!Guid.TryParse(id, out var therapistId))
            return principal;

        // Check if child_scope claim already exists (avoid re-adding on each call)
        if (principal.HasClaim(c => c.Type == "child_scope"))
            return principal;

        var cacheKey = $"therapist_scope:{therapistId}";
        var childIds = await _cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return await _db.TherapistAssignments
                .AsNoTracking()
                .Where(a => a.TherapistUserId == therapistId)
                .Select(a => a.ChildId)
                .ToListAsync();
        });

        if (childIds != null && childIds.Count > 0)
        {
            var csv = string.Join(',', childIds);
            var identity = (ClaimsIdentity)principal.Identity!;
            identity.AddClaim(new Claim("child_scope", csv));
        }

        return principal;
    }
}
