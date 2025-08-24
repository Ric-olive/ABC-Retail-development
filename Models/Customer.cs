using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Customer:ITableEntity
    {
            public string PartitionKey { get; set; }         // e.g. "Customer"
            public string RowKey { get; set; }               // Unique username or GUID
            public string FullName { get; set; }
            public string Email { get; set; }
            public string PasswordHash { get; set; }         // Hashed password
            public DateTime RegisteredOn { get; set; }
            public bool IsActive { get; set; }               // Soft-delete or suspension control

            // Required for Azure Table binding
            public ETag ETag { get; set; }
            public DateTimeOffset? Timestamp { get; set; }
    }
}
