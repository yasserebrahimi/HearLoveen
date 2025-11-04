using MediatR;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

public static class GetNextPromptElo
{
    // Constants for ELO ratings and thresholds
    private const double DefaultRating = 1000.0;
    private const double MasteryThreshold = 1050.0;
    private const int DefaultDifficulty = 1;
    private const string DefaultFocusPhonemes = "R,S";

    // Phoneme to word mapping - should be loaded from database in production
    private static readonly Dictionary<string, string> PhonemeToWord = new()
    {
        ["R"] = "car",
        ["S"] = "bus",
        ["CH"] = "chair",
        ["L"] = "ball"
    };
    private const string DefaultWord = "mama";

    public record Query(Guid ChildId) : IRequest<Result>;
    public record Result(string[] Words, string[] Phonemes, int Difficulty);

    public class Handler : IRequestHandler<Query, Result>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db) { _db = db; }

        public async Task<Result> Handle(Query request, CancellationToken ct)
        {
            // Load curriculum and ratings in parallel
            var curriculumTask = _db.ChildCurricula
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ChildId == request.ChildId, ct);

            var ratingsTask = _db.PhonemeRatings
                .AsNoTracking()
                .Where(x => x.ChildId == request.ChildId)
                .ToDictionaryAsync(x => x.Phoneme, x => x.Rating, ct);

            await Task.WhenAll(curriculumTask, ratingsTask);

            var cur = curriculumTask.Result;
            var ratingsDict = ratingsTask.Result;

            var pool = (cur?.FocusPhonemesCsv ?? DefaultFocusPhonemes)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct()
                .ToList();

            // Load only prerequisites for phonemes in the pool
            var prerequisites = await _db.PhonemePrerequisites
                .AsNoTracking()
                .Where(p => pool.Contains(p.Phoneme))
                .ToListAsync(ct);

            var prereqLookup = prerequisites.ToLookup(p => p.Phoneme);

            bool IsSatisfied(string phoneme)
            {
                foreach (var edge in prereqLookup[phoneme])
                {
                    var rating = ratingsDict.GetValueOrDefault(edge.Requires, DefaultRating);
                    if (rating < MasteryThreshold)
                        return false;
                }
                return true;
            }

            var candidates = pool.Where(IsSatisfied).ToList();
            if (candidates.Count == 0)
                candidates = pool;

            var ordered = candidates
                .OrderBy(p => ratingsDict.GetValueOrDefault(p, DefaultRating))
                .Take(3)
                .ToArray();

            var words = ordered
                .Select(p => PhonemeToWord.GetValueOrDefault(p, DefaultWord))
                .ToArray();

            return new Result(words, ordered, cur?.Difficulty ?? DefaultDifficulty);
        }
    }
}
