using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Sas;

namespace HearLoveen.Infrastructure.Storage;
public class AzureBlobStorageService : IStorageService
{
    private readonly BlobServiceClient _svc;
    private readonly string _container;
    public AzureBlobStorageService(string accountUrl, string container)
    {
        _svc = new BlobServiceClient(new Uri(accountUrl), new DefaultAzureCredential());
        _container = container;
    }
    public async Task<string> GetUploadSasAsync(string blobName, CancellationToken ct = default)
    {
        var container = _svc.GetBlobContainerClient(_container);
        await container.CreateIfNotExistsAsync();
        var blob = container.GetBlobClient(blobName);
        var sas = blob.GenerateSasUri(BlobSasPermissions.Create | BlobSasPermissions.Write, DateTimeOffset.UtcNow.AddMinutes(30));
        return sas.ToString();
    }
}
