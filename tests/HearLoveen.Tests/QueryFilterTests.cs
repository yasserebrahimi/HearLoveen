using System;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Xunit;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Domain.Entities;
using HearLoveen.Tests.Testing;
using HearLoveen.Infrastructure;

namespace HearLoveen.Tests;

public class QueryFilterTests
{
    private AppDbContext Make(TestCurrentUser cu)
    {
        var opts = new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options;
        return new AppDbContext(opts, cu);
    }

    [Fact]
    public void Therapist_sees_only_scoped_children()
    {
        var childA = Guid.NewGuid();
        var childB = Guid.NewGuid();
        var cu = new TestCurrentUser { IsTherapist = true, ChildScope = new []{ childA } };

        using var db = Make(cu);
        db.AudioSubmissions.Add(new AudioSubmission { Id = Guid.NewGuid(), ChildId = childA });
        db.AudioSubmissions.Add(new AudioSubmission { Id = Guid.NewGuid(), ChildId = childB });
        db.SaveChanges();

        var list = db.AudioSubmissions.AsNoTracking().ToList();
        Assert.Single(list);
        Assert.Equal(childA, list[0].ChildId);
    }
}
