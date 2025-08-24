using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace ABC_Retail.Services
{
    public class BlobImageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName = "product-images";


        public BlobImageService(BlobServiceClient blobServiceClient)
        {
            _blobServiceClient = blobServiceClient;
        }
        private async Task<BlobContainerClient> GetOrCreateContainerAsync()
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            // Create container without public access (account likely disallows public access)
            await containerClient.CreateIfNotExistsAsync(PublicAccessType.None);
            return containerClient;
        }
        public async Task<string> UploadImageAsync(Stream imageStream, string originalFileName, string contentType)
        {

            var containerClient = await GetOrCreateContainerAsync();

            // Generate a unique filename to avoid collisions
            var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(originalFileName)}";
            var blobClient = containerClient.GetBlobClient(uniqueFileName);

            // Upload the image with content type
            await blobClient.UploadAsync(imageStream, new BlobHttpHeaders { ContentType = contentType });

            // Generate a read-only SAS URL so the image can be displayed without public container access
            if (blobClient.CanGenerateSasUri)
            {
                var sasBuilder = new Azure.Storage.Sas.BlobSasBuilder
                {
                    BlobContainerName = containerClient.Name,
                    BlobName = uniqueFileName,
                    Resource = "b",
                    StartsOn = DateTimeOffset.UtcNow.AddMinutes(-5),
                    ExpiresOn = DateTimeOffset.UtcNow.AddYears(1) // adjust if needed
                };
                sasBuilder.SetPermissions(Azure.Storage.Sas.BlobSasPermissions.Read);

                var sasUri = blobClient.GenerateSasUri(sasBuilder);
                return sasUri.ToString();
            }

            // Fallback: return the blob URI (may not be accessible without SAS)
            return blobClient.Uri.ToString();
        }

    }
}
