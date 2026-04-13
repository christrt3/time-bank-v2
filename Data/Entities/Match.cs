namespace Time_Bank_V1.Data.Entities
{
    public class Match
    {
        public int Id { get; set; }
        public int OfferId { get; set; }
        public string ProviderId { get; set; } = string.Empty;  // Person providing the service
        public string ReceiverId { get; set; } = string.Empty;  // Person receiving the service
        public MatchStatus Status { get; set; } = MatchStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }
        public string? Notes { get; set; }

        // Navigation
        public Offer Offer { get; set; } = null!;
        public ApplicationUser Provider { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
        public ICollection<TimeTransaction> Transactions { get; set; } = new List<TimeTransaction>();
    }

    public enum MatchStatus
    {
        Pending,
        Active,
        Completed,
        Cancelled
    }
}
