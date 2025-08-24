using Azure;
using Azure.Data.Tables;

namespace ABC_Retail.Models
{
    public class Admin:ITableEntity
    {
        public string PartitionKey { get; set; } = "Admin";
        public string RowKey { get; set; } // Email
        public string FullName { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public DateTime CreatedOn { get; set; }
        public bool IsActive { get; set; }

        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }

    }
}
