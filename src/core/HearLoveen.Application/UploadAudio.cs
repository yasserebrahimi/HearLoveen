using FluentValidation;
using HearLoveen.Domain.Entities;
using HearLoveen.Infrastructure.Messaging;
using HearLoveen.Infrastructure.Persistence;
using HearLoveen.Infrastructure.Storage;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace HearLoveen.Application.Audio;

public static class UploadAudio
{
    public record Command(Guid ChildId, string FileName, int DurationSec, string MimeType) : IRequest<Result>;
    public record Result(Guid SubmissionId, string UploadUrl);

    public class Validator : AbstractValidator<Command>
    {
        public Validator()
        {
            RuleFor(x => x.ChildId).NotEmpty();
            RuleFor(x => x.FileName).NotEmpty().MaximumLength(200);
            RuleFor(x => x.DurationSec).GreaterThan(0).LessThanOrEqualTo(300);
            RuleFor(x => x.MimeType).NotEmpty();
        }
    }

    public class Handler : IRequestHandler<Command, Result>
    {
        private readonly AppDbContext _db;
        private readonly IStorageService _storage;
        private readonly IQueuePublisher _publisher;

        public Handler(AppDbContext db, IStorageService storage, IQueuePublisher publisher)
        {
            _db = db; _storage = storage; _publisher = publisher;
        }

        public async Task<Result> Handle(Command request, CancellationToken ct)
        {
            var child = await _db.Children.FirstOrDefaultAsync(c => c.Id == request.ChildId, ct);
            if (child is null) throw new KeyNotFoundException("Child not found");

            var submission = new AudioSubmission
            {
                Id = Guid.NewGuid(),
                ChildId = request.ChildId,
                BlobUrl = $"audio/{request.ChildId}/{DateTime.UtcNow:yyyyMMddHHmmss}-{request.FileName}",
                DurationSec = request.DurationSec,
                MimeType = request.MimeType
            };
            _db.AudioSubmissions.Add(submission);
            await _db.SaveChangesAsync(ct);

            var sas = await _storage.GetUploadSasAsync(submission.BlobUrl, ct);
            await _publisher.PublishAudioSubmittedAsync(submission.Id, submission.BlobUrl, submission.ChildId, ct);

            return new Result(submission.Id, sas);
        }
    }
}
