using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class AdminActivityQueueService
    {
        private readonly QueueClient _adminActivityQueueClient;

        public AdminActivityQueueService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Storage connection string is missing.");

            _adminActivityQueueClient = new QueueClient(connectionString, "admin-activity");
            
            // Ensure queue exists
            _adminActivityQueueClient.CreateIfNotExists();
        }

        public async Task EnqueueAdminActivityAsync(AdminActivityMessage message)
        {
            var payload = JsonSerializer.Serialize(message);
            await _adminActivityQueueClient.SendMessageAsync(payload);
        }

        public async Task<AdminActivityMessage?> DequeueAdminActivityAsync()
        {
            var response = await _adminActivityQueueClient.ReceiveMessageAsync();
            if (response.Value != null)
            {
                var message = JsonSerializer.Deserialize<AdminActivityMessage>(response.Value.MessageText);
                await _adminActivityQueueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return message;
            }
            return null;
        }

        public async Task<int> GetQueueLengthAsync()
        {
            var properties = await _adminActivityQueueClient.GetPropertiesAsync();
            return properties.Value.ApproximateMessagesCount;
        }
    }

    public class AdminActivityMessage
    {
        public string ActivityId { get; set; } = Guid.NewGuid().ToString();
        public string AdminId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "CreateProduct", "UpdateProduct", "DeleteProduct", "UpdateInventory"
        public string EntityType { get; set; } = string.Empty; // "Product", "Customer", "Order", "Inventory"
        public string EntityId { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public Dictionary<string, object> Metadata { get; set; } = new();
    }
}
