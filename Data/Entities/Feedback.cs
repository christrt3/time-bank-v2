namespace Time_Bank_V1.Data.Entities
{
    public class Feedback
    {
        public int Id { get; set; }
        public string FromUserId { get; set; } = string.Empty;
        public string ToUserId { get; set; } = string.Empty;
        public int? TransactionId { get; set; }
        public bool IsPositive { get; set; } = true; // Only positive feedback allowed
        public string? Comment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser FromUser { get; set; } = null!;
        public ApplicationUser ToUser { get; set; } = null!;
        public TimeTransaction? Transaction { get; set; }
    }
}
