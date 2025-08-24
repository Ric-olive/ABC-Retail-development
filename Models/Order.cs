using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Order : ITableEntity
    {
        public string PartitionKey { get; set; } = string.Empty; // CustomerId
        public string RowKey { get; set; } = string.Empty;       // OrderId (Guid or timestamp-based)
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

        public string Status { get; set; } = "Pending";       // "Pending", "Processing", "Completed", etc.
        public string CartSnapshotJson { get; set; } = string.Empty; // Serialized cart items
        public decimal TotalAmount { get; set; }
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public string DeliveryAddress { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;

        // Customer metadata
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
    }
}
