using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Authorization;
using HearLoveen.Api.Auth;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using HearLoveen.Domain.Entities;
using HearLoveen.Tests.Testing;
using HearLoveen.Infrastructure;

namespace HearLoveen.Tests;

public class AuthorizationTests
{
    [Fact]
    public async Task TherapistAssigned_allows_assigned_child()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IAuthorizationHandler, TherapistAssignedHandler>();
        services.AddAuthorization();
        var cu = new TestCurrentUser { IsTherapist = true };
        services.AddSingleton<ICurrentUser>(cu);

        var dbOpts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        var db = new AppDbContext(dbOpts, cu);
        services.AddSingleton(db);
        var sp = services.BuildServiceProvider();

        var child = Guid.NewGuid();
        var therapist = cu.UserId ?? Guid.NewGuid();
        db.TherapistAssignments.Add(new TherapistAssignment { Id = Guid.NewGuid(), TherapistUserId = therapist, ChildId = child });
        var sub = new AudioSubmission { Id = Guid.NewGuid(), ChildId = child };
        db.AudioSubmissions.Add(sub);
        db.SaveChanges();

        var handler = sp.GetRequiredService<IAuthorizationHandler>() as TherapistAssignedHandler;
        var req = new TherapistAssignedRequirement();
        var ctx = new AuthorizationHandlerContext(new[] { req }, new System.Security.Claims.ClaimsPrincipal(), sub.Id);
        await handler!.HandleAsync(ctx);

        Assert.True(ctx.HasSucceeded);
    }
}
