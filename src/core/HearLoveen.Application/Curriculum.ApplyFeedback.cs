using MediatR;
using HearLoveen.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

public static class ApplyFeedback
{
    public record Command(Guid ChildId, int Score0_100, string[] WeakPhonemes) : IRequest;
    public class Handler : IRequestHandler<Command>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db) { _db = db; }

        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            var cur = await _db.ChildCurricula.FirstOrDefaultAsync(x => x.ChildId == request.ChildId, ct);
            if (cur == null)
            {
                cur = new Domain.Entities.ChildCurriculum
                {
                    Id = Guid.NewGuid(),
                    ChildId = request.ChildId,
                    FocusPhonemesCsv = string.Join(',', request.WeakPhonemes.Distinct()),
                    Difficulty = 1
                };
                _db.ChildCurricula.Add(cur);
            }
            else
            {
                if (request.Score0_100 >= 80) cur.SuccessStreak++;
                else cur.SuccessStreak = 0;

                if (request.Score0_100 >= 90 && cur.Difficulty < 5) cur.Difficulty++;
                if (request.Score0_100 < 70 && cur.Difficulty > 1) cur.Difficulty--;

                if (request.WeakPhonemes?.Length > 0)
                    cur.FocusPhonemesCsv = string.Join(',', request.WeakPhonemes.Distinct());
                cur.UpdatedAtUtc = DateTime.UtcNow;
            }
            await _db.SaveChangesAsync(ct);
            return Unit.Value;
        }
    }
}
