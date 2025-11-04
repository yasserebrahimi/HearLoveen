using MediatR;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

public static class UpdatePhonemeElo
{
    public record Command(Guid ChildId, string[] TargetPhonemes, int Score0_100) : IRequest;
    public class Handler : IRequestHandler<Command>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db) { _db = db; }
        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            double k = 16;
            double perf = (request.Score0_100 - 50) / 50.0; // -1..+1
            if (double.IsNaN(perf)) perf = 0;
            foreach (var p in request.TargetPhonemes.Distinct())
            {
                var pr = await _db.PhonemeRatings.FirstOrDefaultAsync(x => x.ChildId==request.ChildId && x.Phoneme==p, ct);
                if (pr == null) { pr = new HearLoveen.Domain.Entities.PhonemeRating{ Id=Guid.NewGuid(), ChildId=request.ChildId, Phoneme=p, Rating=1000 }; _db.PhonemeRatings.Add(pr); }
                var expected = 1.0 / (1.0 + Math.Pow(10.0, (1000 - pr.Rating)/400.0));
                pr.Rating = pr.Rating + k * ((perf+1)/2 - expected);
                pr.UpdatedAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
