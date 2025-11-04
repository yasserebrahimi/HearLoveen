using HearLoveen.Shared.Application.Abstractions;
using HearLoveen.Shared.Application.Common;
using HearLoveen.AudioService.Domain.Entities;
using HearLoveen.AudioService.Domain.Repositories;
using MediatR;
using Microsoft.Extensions.Logging;
using FluentValidation;

namespace HearLoveen.AudioService.Application.Commands;

public record UploadAudioCommand(
    Guid ChildId,
    Guid ExerciseId,
    byte[] AudioData,
    string FileName,
    int DurationSeconds,
    string RecordingEnvironment) : ICommand<Guid>;

public class UploadAudioCommandValidator : AbstractValidator<UploadAudioCommand>
{
    public UploadAudioCommandValidator()
    {
        RuleFor(x => x.ChildId).NotEmpty();
        RuleFor(x => x.ExerciseId).NotEmpty();
        RuleFor(x => x.AudioData).NotEmpty().Must(data => data.Length <= 50 * 1024 * 1024);
        RuleFor(x => x.DurationSeconds).GreaterThan(0).LessThanOrEqualTo(300);
        RuleFor(x => x.FileName).NotEmpty().Must(name => 
            name.EndsWith(".wav") || name.EndsWith(".flac") || name.EndsWith(".mp3"));
    }
}

public class UploadAudioCommandHandler : IRequestHandler<UploadAudioCommand, Result<Guid>>
{
    private readonly IAudioRecordingRepository _recordingRepository;
    private readonly IChildRepository _childRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UploadAudioCommandHandler> _logger;
    
    public UploadAudioCommandHandler(
        IAudioRecordingRepository recordingRepository,
        IChildRepository childRepository,
        IBlobStorageService blobStorage,
        IEventBus eventBus,
        ILogger<UploadAudioCommandHandler> logger)
    {
        _recordingRepository = recordingRepository;
        _childRepository = childRepository;
        _blobStorage = blobStorage;
        _eventBus = eventBus;
        _logger = logger;
    }
    
    public async Task<Result<Guid>> Handle(UploadAudioCommand request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Uploading audio for child {ChildId}", request.ChildId);
            
            var child = await _childRepository.GetByIdAsync(request.ChildId, cancellationToken);
            if (child == null) return Result<Guid>.Failure("Child not found");
            if (!child.IsActive) return Result<Guid>.Failure("Child account is inactive");
            
            var blobPath = await _blobStorage.UploadAsync(
                "audio-recordings",
                $"{request.ChildId}/{Guid.NewGuid()}/{request.FileName}",
                request.AudioData,
                "audio/wav",
                cancellationToken);
            
            var recording = AudioRecording.Create(
                request.ChildId,
                request.ExerciseId,
                blobPath,
                request.DurationSeconds,
                Path.GetExtension(request.FileName).TrimStart('.'),
                request.RecordingEnvironment);
            
            await _recordingRepository.AddAsync(recording, cancellationToken);
            
            await _eventBus.PublishAsync(new AudioUploadedIntegrationEvent(
                recording.Id, request.ChildId, blobPath, recording.AudioFormat, request.DurationSeconds),
                cancellationToken);
            
            _logger.LogInformation("Audio uploaded successfully: {RecordingId}", recording.Id);
            
            return Result<Guid>.Success(recording.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio");
            return Result<Guid>.Failure($"Upload failed: {ex.Message}");
        }
    }
}

public record AudioUploadedIntegrationEvent(
    Guid RecordingId,
    Guid ChildId,
    string BlobPath,
    string AudioFormat,
    int DurationSeconds);

public interface IBlobStorageService
{
    Task<string> UploadAsync(string container, string fileName, byte[] data, string contentType, CancellationToken cancellationToken);
}

public interface IEventBus
{
    Task PublishAsync<T>(T @event, CancellationToken cancellationToken);
}
