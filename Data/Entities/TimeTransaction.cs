namespace Time_Bank_V1.Data.Entities
{
    public class TimeTransaction
    {
        public int Id { get; set; }
        public string ProviderId { get; set; } = string.Empty; // Who gave the service (earns hours)
        public string ReceiverId { get; set; } = string.Empty; // Who received the service (spends hours)
        public int? MatchId { get; set; }
        public decimal Hours { get; set; }
        public TransactionType TransactionType { get; set; }
        public TransactionStatus Status { get; set; } = TransactionStatus.Pending;
        public DateTime ServiceDate { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Mutual confirmation handshake
        public bool ConfirmedByProvider { get; set; } = false;
        public bool ConfirmedByReceiver { get; set; } = false;
        public DateTime? ConfirmedAt { get; set; }

        // For donations: DonationToUserId holds the recipient's ID
        public string? DonationToUserId { get; set; }

        // Description shown in ledger
        public string Description { get; set; } = string.Empty;
        public int? CategoryId { get; set; }

        // Navigation
        public ApplicationUser Provider { get; set; } = null!;
        public ApplicationUser Receiver { get; set; } = null!;
        public Match? Match { get; set; }
        public Category? Category { get; set; }
    }

    public enum TransactionType
    {
        Earned,
        Spent,
        Donated,
        Received,  // Hours received from a donation
        Grant      // Admin-granted hours
    }

    public enum TransactionStatus
    {
        Pending,
        Confirmed,
        Disputed,
        Cancelled
    }
}
