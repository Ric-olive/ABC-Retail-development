using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.ComponentModel.DataAnnotations.Schema;
using System.Runtime.Serialization;

namespace ABC_Retail.Models
{
    public class Product:ITableEntity
    {
        public string PartitionKey { get; set; } = "Retail";           // Logical grouping

        public string RowKey { get; set; } = string.Empty;             // Unique ID (SKU or Guid) - Auto-generated if empty
        public string Name { get; set; }                               // Product name
        public string Category { get; set; }                           // e.g. Electronics, Apparel
        public double Price { get; set; }                              // Unit price
        public int StockQty { get; set; }                              // Inventory count
        public string? ImageUrl { get; set; }                           // Blob URI of image
        public string Description { get; set; }                        // Optional product info

        [IgnoreDataMember]
        // ✅ Not saved to Table Storage
        public IFormFile? ImageFile { get; set; }

        public DateTimeOffset? Timestamp { get; set; }                 // Required for ITableEntity
        public ETag ETag { get; set; }
    }
}
