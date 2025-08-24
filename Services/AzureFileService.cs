using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;

namespace ABC_Retail.Services
{
    public class AzureFileService
    {
        private readonly ShareServiceClient _shareServiceClient;
        private readonly string _shareName = "application-logs";

        public AzureFileService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Storage connection string is missing.");

            _shareServiceClient = new ShareServiceClient(connectionString);
        }

        private async Task<ShareClient> GetOrCreateShareAsync()
        {
            var shareClient = _shareServiceClient.GetShareClient(_shareName);
            await shareClient.CreateIfNotExistsAsync();
            return shareClient;
        }

        private async Task<ShareDirectoryClient> GetOrCreateDirectoryAsync(string directoryName)
        {
            var shareClient = await GetOrCreateShareAsync();
            var directoryClient = shareClient.GetDirectoryClient(directoryName);
            await directoryClient.CreateIfNotExistsAsync();
            return directoryClient;
        }

        public async Task<string> UploadLogFileAsync(string fileName, string content, string logType = "general")
        {
            try
            {
                Console.WriteLine($"DEBUG: Starting file upload - fileName: {fileName}, logType: {logType}");
                
                var directoryClient = await GetOrCreateDirectoryAsync(logType);
                Console.WriteLine($"DEBUG: Directory client created for logType: {logType}");
                
                // Create unique filename with timestamp
                var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
                var uniqueFileName = $"{timestamp}_{fileName}";
                Console.WriteLine($"DEBUG: Unique filename: {uniqueFileName}");
                
                var fileClient = directoryClient.GetFileClient(uniqueFileName);
                
                // Convert content to stream
                using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
                Console.WriteLine($"DEBUG: Content stream created, length: {stream.Length}");
                
                // Create and upload file
                await fileClient.CreateAsync(stream.Length);
                Console.WriteLine($"DEBUG: File created in Azure Files");
                
                stream.Position = 0; // Reset stream position
                await fileClient.UploadAsync(stream);
                Console.WriteLine($"DEBUG: File uploaded successfully to Azure Files: {logType}/{uniqueFileName}");
                
                return $"{logType}/{uniqueFileName}";
            }
            catch (Exception ex)
            {
                Console.WriteLine($"DEBUG: File upload failed - {ex.Message}");
                Console.WriteLine($"DEBUG: Stack trace: {ex.StackTrace}");
                throw new Exception($"Failed to upload log file: {ex.Message}", ex);
            }
        }

        public async Task<string> DownloadLogFileAsync(string filePath)
        {
            try
            {
                var parts = filePath.Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid file path format. Expected: logType/fileName");

                var logType = parts[0];
                var fileName = parts[1];

                var directoryClient = await GetOrCreateDirectoryAsync(logType);
                var fileClient = directoryClient.GetFileClient(fileName);

                var response = await fileClient.DownloadAsync();
                using var reader = new StreamReader(response.Value.Content);
                return await reader.ReadToEndAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to download log file: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> ListLogFilesAsync(string logType = "general")
        {
            try
            {
                var files = new List<string>();
                var directoryClient = await GetOrCreateDirectoryAsync(logType);

                await foreach (ShareFileItem item in directoryClient.GetFilesAndDirectoriesAsync())
                {
                    if (!item.IsDirectory)
                    {
                        files.Add($"{logType}/{item.Name}");
                    }
                }

                return files;
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to list log files: {ex.Message}", ex);
            }
        }

        public async Task DeleteLogFileAsync(string filePath)
        {
            try
            {
                var parts = filePath.Split('/');
                if (parts.Length != 2)
                    throw new ArgumentException("Invalid file path format. Expected: logType/fileName");

                var logType = parts[0];
                var fileName = parts[1];

                var directoryClient = await GetOrCreateDirectoryAsync(logType);
                var fileClient = directoryClient.GetFileClient(fileName);

                await fileClient.DeleteIfExistsAsync();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to delete log file: {ex.Message}", ex);
            }
        }
    }
}
