using ABC_Retail.Models;
using Azure.Data.Tables;
using Newtonsoft.Json;

namespace ABC_Retail.Services
{
    public class OrderService
    {
        private readonly TableClient _orderTable;

        public OrderService(TableServiceClient client)
        {
            _orderTable = client.GetTableClient("Orders");
            _orderTable.CreateIfNotExists();
        }

        public async Task<string> PlaceOrderAsync(string customerId, List<CartItem> cartItems, decimal total)
        {
            try
            {
                var orderId = Guid.NewGuid().ToString();
                Console.WriteLine($"DEBUG: Creating order with ID: {orderId}");
                
                var order = new Order
                {
                    PartitionKey = customerId.ToLower().Trim(),
                    RowKey = orderId,
                    Timestamp = DateTimeOffset.UtcNow,
                    Status = "Placed",
                    CartSnapshotJson = JsonConvert.SerializeObject(cartItems),
                    TotalAmount = total,
                    OrderDate = DateTime.UtcNow,
                    CustomerEmail = customerId,
                    DeliveryAddress = "Default Address", // TODO: Get from customer profile
                    TrackingNumber = string.Empty
                };

                Console.WriteLine($"DEBUG: Adding order to table storage...");
                await _orderTable.AddEntityAsync(order);
                Console.WriteLine($"DEBUG: Order {orderId} added to table storage successfully");
                
                return orderId;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR: Failed to place order - {ex.Message}");
                Console.WriteLine($"ERROR: Stack trace - {ex.StackTrace}");
                throw;
            }
        }

        public async Task<Order> GetOrderByIdAsync(string customerId, string orderId)
        {
            var response = await _orderTable.GetEntityAsync<Order>(customerId, orderId);
            return response.Value;
        }

        public async Task<List<Order>> GetOrdersByCustomerAsync(string customerEmail)
        {
            var normalizedEmail = customerEmail.ToLower().Trim();
            var queryResults = _orderTable.QueryAsync<Order>(order => order.PartitionKey == normalizedEmail);

            var orders = new List<Order>();
            await foreach (var order in queryResults)
            {
                orders.Add(order);
            }

            return orders;
        }

        public async Task<Order?> GetOrderAsync(string orderId)
        {
            // Since we don't know the partition key, we need to query by RowKey
            var queryResults = _orderTable.QueryAsync<Order>(order => order.RowKey == orderId);
            
            await foreach (var order in queryResults)
            {
                return order; // Return the first match
            }
            
            return null;
        }

    }
}
