using HearLoveen.Shared.Domain.Events;
using HearLoveen.Shared.Application.Common;
using MediatR;
using FluentValidation;

namespace HearLoveen.AudioService.Application.Commands;

/// <summary>
/// Command to upload and process audio recording
/// Demonstrates: CQRS, MediatR, FluentValidation, Domain Events
/// </summary>
public record UploadAudioRecordingCommand : IRequest<Result<Guid>>
{
    public Guid ChildId { get; init; }
    public byte[] AudioData { get; init; } = Array.Empty<byte>();
    public string FileName { get; init; } = string.Empty;
    public int DurationSeconds { get; init; }
    public Guid ExerciseId { get; init; }
    public string RecordingEnvironment { get; init; } = "home";
}

/// <summary>
/// Validator ensures business rules before processing
/// </summary>
public class UploadAudioRecordingCommandValidator : AbstractValidator<UploadAudioRecordingCommand>
{
    public UploadAudioRecordingCommandValidator()
    {
        RuleFor(x => x.ChildId)
            .NotEmpty()
            .WithMessage("Child ID is required");

        RuleFor(x => x.AudioData)
            .NotEmpty()
            .WithMessage("Audio data is required")
            .Must(data => data.Length <= 50 * 1024 * 1024) // 50MB max
            .WithMessage("Audio file must be less than 50MB");

        RuleFor(x => x.DurationSeconds)
            .GreaterThan(0)
            .LessThanOrEqualTo(300) // 5 minutes max
            .WithMessage("Duration must be between 1 and 300 seconds");

        RuleFor(x => x.FileName)
            .NotEmpty()
            .Must(name => name.EndsWith(".wav") || name.EndsWith(".flac") || name.EndsWith(".mp3"))
            .WithMessage("Only WAV, FLAC, or MP3 files are supported");
    }
}

/// <summary>
/// Command Handler - implements the business logic
/// Demonstrates: Repository Pattern, Unit of Work, Domain Events, Integration Events
/// </summary>
public class UploadAudioRecordingCommandHandler 
    : IRequestHandler<UploadAudioRecordingCommand, Result<Guid>>
{
    private readonly IAudioRecordingRepository _repository;
    private readonly IChildRepository _childRepository;
    private readonly IBlobStorageService _blobStorage;
    private readonly IEventBus _eventBus;
    private readonly ILogger<UploadAudioRecordingCommandHandler> _logger;
    private readonly IUnitOfWork _unitOfWork;

    public UploadAudioRecordingCommandHandler(
        IAudioRecordingRepository repository,
        IChildRepository childRepository,
        IBlobStorageService blobStorage,
        IEventBus eventBus,
        ILogger<UploadAudioRecordingCommandHandler> logger,
        IUnitOfWork unitOfWork)
    {
        _repository = repository;
        _childRepository = childRepository;
        _blobStorage = blobStorage;
        _eventBus = eventBus;
        _logger = logger;
        _unitOfWork = unitOfWork;
    }

    public async Task<Result<Guid>> Handle(
        UploadAudioRecordingCommand request, 
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation(
                "Processing audio upload for child {ChildId}, duration: {Duration}s",
                request.ChildId, 
                request.DurationSeconds);

            // 1. Verify child exists and is active
            var child = await _childRepository.GetByIdAsync(request.ChildId, cancellationToken);
            if (child == null)
            {
                return Result<Guid>.Failure("Child not found");
            }

            if (!child.IsActive)
            {
                return Result<Guid>.Failure("Child account is inactive");
            }

            // 2. Upload to blob storage
            var blobPath = await _blobStorage.UploadAsync(
                containerName: "audio-recordings",
                fileName: $"{request.ChildId}/{Guid.NewGuid()}/{request.FileName}",
                data: request.AudioData,
                contentType: "audio/wav",
                cancellationToken: cancellationToken);

            _logger.LogInformation("Audio uploaded to blob storage: {BlobPath}", blobPath);

            // 3. Create domain entity
            var recording = AudioRecording.Create(
                childId: request.ChildId,
                exerciseId: request.ExerciseId,
                blobStoragePath: blobPath,
                durationSeconds: request.DurationSeconds,
                audioFormat: Path.GetExtension(request.FileName).TrimStart('.'),
                recordingEnvironment: request.RecordingEnvironment);

            // 4. Save to database
            await _repository.AddAsync(recording, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Audio recording created with ID: {RecordingId}", recording.Id);

            // 5. Publish domain event (for internal handlers)
            var domainEvent = new AudioRecordingUploadedDomainEvent(
                RecordingId: recording.Id,
                ChildId: request.ChildId,
                BlobPath: blobPath,
                DurationSeconds: request.DurationSeconds,
                OccurredOn: DateTime.UtcNow);

            await _eventBus.PublishAsync(domainEvent, cancellationToken);

            // 6. Publish integration event (for other microservices via Kafka/RabbitMQ)
            var integrationEvent = new AudioRecordingUploadedIntegrationEvent(
                EventId: Guid.NewGuid(),
                RecordingId: recording.Id,
                ChildId: request.ChildId,
                BlobPath: blobPath,
                AudioFormat: recording.AudioFormat,
                DurationSeconds: request.DurationSeconds,
                OccurredOn: DateTime.UtcNow);

            await _eventBus.PublishIntegrationEventAsync(integrationEvent, cancellationToken);

            _logger.LogInformation("Events published for recording {RecordingId}", recording.Id);

            return Result<Guid>.Success(recording.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading audio recording for child {ChildId}", request.ChildId);
            return Result<Guid>.Failure($"Failed to upload audio: {ex.Message}");
        }
    }
}

/// <summary>
/// Domain Event - triggers within the same bounded context
/// </summary>
public record AudioRecordingUploadedDomainEvent(
    Guid RecordingId,
    Guid ChildId,
    string BlobPath,
    int DurationSeconds,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Integration Event - sent to other microservices via message bus
/// This triggers the Analysis Service to process the audio
/// </summary>
public record AudioRecordingUploadedIntegrationEvent(
    Guid EventId,
    Guid RecordingId,
    Guid ChildId,
    string BlobPath,
    string AudioFormat,
    int DurationSeconds,
    DateTime OccurredOn) : IIntegrationEvent;

/// <summary>
/// Result Pattern - for better error handling
/// </summary>
public class Result<T>
{
    public bool IsSuccess { get; }
    public bool IsFailure => !IsSuccess;
    public T? Value { get; }
    public string Error { get; } = string.Empty;

    private Result(bool isSuccess, T? value, string error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public static Result<T> Success(T value) => new(true, value, string.Empty);
    public static Result<T> Failure(string error) => new(false, default, error);
}
