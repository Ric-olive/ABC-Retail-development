using Azure.Storage.Queues;
using System.Text.Json;

namespace ABC_Retail.Services
{
    public class OrderProcessingQueueService
    {
        private readonly QueueClient _orderQueueClient;
        private readonly QueueClient _inventoryQueueClient;
        private readonly QueueClient _orderLifecycleQueueClient;

        public OrderProcessingQueueService(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentException("Storage connection string is missing.");

            _orderQueueClient = new QueueClient(connectionString, "order-processing");
            _inventoryQueueClient = new QueueClient(connectionString, "inventory-management");
            _orderLifecycleQueueClient = new QueueClient(connectionString, "order-lifecycle");
            
            // Ensure queues exist
            _orderQueueClient.CreateIfNotExists();
            _inventoryQueueClient.CreateIfNotExists();
            _orderLifecycleQueueClient.CreateIfNotExists();
        }

        // Order Processing Queue Methods
        public async Task EnqueueOrderProcessingAsync(OrderProcessingMessage message)
        {
            try
            {
                Console.WriteLine($"DEBUG: Serializing order processing message for order {message.OrderId}");
                var payload = JsonSerializer.Serialize(message);
                Console.WriteLine($"DEBUG: Sending message to order-processing queue...");
                await _orderQueueClient.SendMessageAsync(payload);
                Console.WriteLine($"DEBUG: Order processing message sent successfully for order {message.OrderId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to enqueue order processing message - {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace - {ex.StackTrace}");
                throw;
            }
        }

        public async Task<OrderProcessingMessage?> DequeueOrderProcessingAsync()
        {
            var response = await _orderQueueClient.ReceiveMessageAsync();
            if (response.Value != null)
            {
                var message = JsonSerializer.Deserialize<OrderProcessingMessage>(response.Value.MessageText);
                await _orderQueueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return message;
            }
            return null;
        }

        // Inventory Management Queue Methods
        public async Task EnqueueInventoryUpdateAsync(InventoryUpdateMessage message)
        {
            var payload = JsonSerializer.Serialize(message);
            await _inventoryQueueClient.SendMessageAsync(payload);
        }

        public async Task<InventoryUpdateMessage?> DequeueInventoryUpdateAsync()
        {
            var response = await _inventoryQueueClient.ReceiveMessageAsync();
            if (response.Value != null)
            {
                var message = JsonSerializer.Deserialize<InventoryUpdateMessage>(response.Value.MessageText);
                await _inventoryQueueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return message;
            }
            return null;
        }

        // Order Lifecycle Queue Methods
        public async Task EnqueueOrderLifecycleAsync(OrderLifecycleMessage message)
        {
            var payload = JsonSerializer.Serialize(message);
            await _orderLifecycleQueueClient.SendMessageAsync(payload);
        }

        public async Task<OrderLifecycleMessage?> DequeueOrderLifecycleAsync()
        {
            var response = await _orderLifecycleQueueClient.ReceiveMessageAsync();
            if (response.Value != null)
            {
                var message = JsonSerializer.Deserialize<OrderLifecycleMessage>(response.Value.MessageText);
                await _orderLifecycleQueueClient.DeleteMessageAsync(response.Value.MessageId, response.Value.PopReceipt);
                return message;
            }
            return null;
        }

        // Get queue statistics
        public async Task<(int orderQueueLength, int inventoryQueueLength, int lifecycleQueueLength)> GetQueueLengthsAsync()
        {
            var orderProperties = await _orderQueueClient.GetPropertiesAsync();
            var inventoryProperties = await _inventoryQueueClient.GetPropertiesAsync();
            var lifecycleProperties = await _orderLifecycleQueueClient.GetPropertiesAsync();
            
            return (orderProperties.Value.ApproximateMessagesCount, 
                   inventoryProperties.Value.ApproximateMessagesCount,
                   lifecycleProperties.Value.ApproximateMessagesCount);
        }
    }

    // Message DTOs for Queue Processing
    public class OrderProcessingMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "ProcessOrder", "CancelOrder", "RefundOrder"
        public decimal TotalAmount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public List<OrderItem> Items { get; set; } = new();
    }

    public class InventoryUpdateMessage
    {
        public string ProductId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty; // "UpdateStock", "ReserveStock", "ReleaseStock"
        public int Quantity { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    public class OrderItem
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal Price { get; set; }
    }

    public class OrderLifecycleMessage
    {
        public string OrderId { get; set; } = string.Empty;
        public string CustomerId { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty; // "Placed", "Confirmed", "Processing", "Shipped", "Delivered", "Cancelled"
        public string PreviousStatus { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string Notes { get; set; } = string.Empty;
    }
}
