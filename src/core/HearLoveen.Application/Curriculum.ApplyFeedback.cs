using MediatR;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Curriculum;

/// <summary>
/// Applies feedback to update a child's curriculum based on their performance.
/// Updates difficulty, success streak, and focus phonemes based on assessment scores.
/// </summary>
public static class ApplyFeedback
{
    // Constants for score thresholds
    private const int SuccessThreshold = 80;
    private const int AdvanceDifficultyThreshold = 90;
    private const int ReduceDifficultyThreshold = 70;
    private const int MinDifficulty = 1;
    private const int MaxDifficulty = 5;

    // Valid phoneme set - should match PHONEME_SET from AI worker
    private static readonly HashSet<string> ValidPhonemes = new()
    {
        "AA","AE","AH","AO","AW","AY","B","CH","D","DH","EH","ER","EY","F","G",
        "HH","IH","IY","JH","K","L","M","N","NG","OW","OY","P","R","S","SH",
        "T","TH","UH","UW","V","W","Y","Z","ZH"
    };

    public record Command(Guid ChildId, int Score0_100, string[] WeakPhonemes) : IRequest;

    public class Handler : IRequestHandler<Command>
    {
        private readonly AppDbContext _db;
        private readonly ICurrentUser _currentUser;

        public Handler(AppDbContext db, ICurrentUser currentUser)
        {
            _db = db;
            _currentUser = currentUser;
        }

        public async Task<Unit> Handle(Command request, CancellationToken ct)
        {
            // Authorization: ensure current user has access to this child
            if (_currentUser.IsTherapist && !_currentUser.IsAdmin)
            {
                if (!_currentUser.ChildScope.Contains(request.ChildId))
                {
                    throw new UnauthorizedAccessException($"User does not have access to child {request.ChildId}");
                }
            }

            // Validate phonemes
            var validatedPhonemes = request.WeakPhonemes?
                .Where(p => ValidPhonemes.Contains(p))
                .Distinct()
                .ToArray() ?? Array.Empty<string>();

            if (request.WeakPhonemes?.Length > 0 && validatedPhonemes.Length == 0)
            {
                throw new ArgumentException("No valid phonemes provided");
            }

            // Use retry logic for concurrency conflicts
            const int maxRetries = 3;
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                try
                {
                    var cur = await _db.ChildCurricula
                        .FirstOrDefaultAsync(x => x.ChildId == request.ChildId, ct);

                    if (cur == null)
                    {
                        cur = new Domain.Entities.ChildCurriculum
                        {
                            Id = Guid.NewGuid(),
                            ChildId = request.ChildId,
                            FocusPhonemesCsv = string.Join(',', validatedPhonemes),
                            Difficulty = MinDifficulty,
                            SuccessStreak = 0,
                            UpdatedAtUtc = DateTime.UtcNow
                        };
                        _db.ChildCurricula.Add(cur);
                    }
                    else
                    {
                        // Update success streak
                        if (request.Score0_100 >= SuccessThreshold)
                            cur.SuccessStreak++;
                        else
                            cur.SuccessStreak = 0;

                        // Adjust difficulty
                        if (request.Score0_100 >= AdvanceDifficultyThreshold && cur.Difficulty < MaxDifficulty)
                            cur.Difficulty++;
                        else if (request.Score0_100 < ReduceDifficultyThreshold && cur.Difficulty > MinDifficulty)
                            cur.Difficulty--;

                        // Update focus phonemes
                        if (validatedPhonemes.Length > 0)
                            cur.FocusPhonemesCsv = string.Join(',', validatedPhonemes);

                        cur.UpdatedAtUtc = DateTime.UtcNow;
                    }

                    await _db.SaveChangesAsync(ct);
                    return Unit.Value;
                }
                catch (DbUpdateConcurrencyException) when (attempt < maxRetries - 1)
                {
                    // Retry on concurrency conflict
                    await Task.Delay(TimeSpan.FromMilliseconds(100 * (attempt + 1)), ct);
                    continue;
                }
            }

            throw new Exception("Failed to update curriculum after multiple retries");
        }
    }
}
