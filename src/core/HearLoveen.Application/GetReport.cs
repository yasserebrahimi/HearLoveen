using HearLoveen.Infrastructure.Persistence;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Reports;

public static class GetReport
{
    public record Query(Guid SubmissionId) : IRequest<Result?>;
    public record Result(Guid SubmissionId, int Score, string Recommendation, string Weakness);

    public class Handler : IRequestHandler<Query, Result?>
    {
        private readonly AppDbContext _db;
        public Handler(AppDbContext db) { _db = db; }
        public async Task<Result?> Handle(Query request, CancellationToken ct)
        {
            var report = await _db.FeedbackReports.FirstOrDefaultAsync(r => r.SubmissionId == request.SubmissionId, ct);
            if (report is null) return null;
            return new Result(report.SubmissionId, report.Score0_100, report.Recommendation, report.Weakness);
        }
    }
}
