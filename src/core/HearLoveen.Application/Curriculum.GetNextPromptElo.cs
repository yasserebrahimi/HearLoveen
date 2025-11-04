using MediatR;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

public static class GetNextPromptElo
{
    public record Query(Guid ChildId) : IRequest<Result>;
    public record Result(string[] Words, string[] Phonemes, int Difficulty);

    public class Handler : IRequestHandler<Query, Result>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db) { _db = db; }

        public async Task<Result> Handle(Query request, CancellationToken ct)
        {
            var cur = await _db.ChildCurricula.AsNoTracking().FirstOrDefaultAsync(x => x.ChildId == request.ChildId, ct);
            var pre = await _db.PhonemePrerequisites.AsNoTracking().ToListAsync(ct);
            var ratings = await _db.PhonemeRatings.AsNoTracking().Where(x => x.ChildId==request.ChildId).ToListAsync(ct);

            var pool = (cur?.FocusPhonemesCsv ?? "R,S").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).Distinct().ToList();

            bool Satisfied(string p) {
                foreach (var edge in pre.Where(e => e.Phoneme==p)) {
                    var r = ratings.FirstOrDefault(x => x.Phoneme==edge.Requires)?.Rating ?? 1000;
                    if (r < 1050) return false;
                }
                return true;
            }

            var cand = pool.Where(Satisfied).ToList();
            if (cand.Count==0) cand = pool;
            var ordered = cand.OrderBy(p => ratings.FirstOrDefault(x => x.Phoneme==p)?.Rating ?? 1000).Take(3).ToArray();
            var words = ordered.Select(p => p switch { "R"=>"car","S"=>"bus","CH"=>"chair","L"=>"ball", _=>"mama"}).ToArray();
            return new Result(words, ordered, cur?.Difficulty ?? 1);
        }
    }
}
