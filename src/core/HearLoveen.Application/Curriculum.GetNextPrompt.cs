using MediatR;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

public static class GetNextPrompt
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
            var phonemes = (cur?.FocusPhonemesCsv ?? "R,S").Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var words = phonemes.Select(p => p switch
            {
                "R" => "car",
                "S" => "bus",
                "CH" => "chair",
                "L" => "ball",
                _ => "mama"
            }).Distinct().Take(3).ToArray();
            return new Result(words, phonemes, cur?.Difficulty ?? 1);
        }
    }
}
