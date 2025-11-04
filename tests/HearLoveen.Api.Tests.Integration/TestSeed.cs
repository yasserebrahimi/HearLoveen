using System;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Domain.Entities;
namespace HearLoveen.Api.Tests.Integration;
public static class TestSeed
{
    public static void SeedBasic(AppDbContext db)
    {
        var child = Guid.NewGuid();
        db.AudioSubmissions.Add(new AudioSubmission { Id=Guid.NewGuid(), ChildId=child });
        db.SaveChanges();
    }
}
