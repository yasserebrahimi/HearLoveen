namespace HearLoveen.Infrastructure.Storage;
public interface IStorageService
{
    Task<string> GetUploadSasAsync(string blobName, CancellationToken ct = default);
}
