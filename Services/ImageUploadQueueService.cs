using ABC_Retail.Models.DTOs;
using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class ImageUploadQueueService
    {
        private readonly QueueClient _queueClient;

        public ImageUploadQueueService(string? connectionString, string queueName)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Storage connection string is missing.");

            _queueClient = new QueueClient(connectionString, queueName);
            _queueClient.CreateIfNotExists(); // Ensures queue exists
        }

        public async Task EnqueueImageUploadAsync(ImageUploadQueueMessageDto message)
        {
            var payload = JsonSerializer.Serialize(message);
            await _queueClient.SendMessageAsync(payload);
        }

    }
}
