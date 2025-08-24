namespace ABC_Retail.Models.DTOs
{
    public class ImageUploadQueueMessageDto
    {
        public string BlobUrl { get; set; } = string.Empty;       // Full URL to the uploaded image
        public string FileName { get; set; } = string.Empty;      // Original file name
        public string UploadedByUserId { get; set; } = string.Empty; // Who uploaded it
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;  // Timestamp
        public string? ProductId { get; set; }
    }
}
