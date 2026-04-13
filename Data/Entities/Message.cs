namespace Time_Bank_V1.Data.Entities
{
    public class Message
    {
        public int Id { get; set; }
        public string FromUserId { get; set; } = string.Empty;
        public string? ToUserId { get; set; }     // null = category broadcast
        public int? ToCategoryId { get; set; }    // Set when broadcasting to a category
        public string Subject { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public bool IsAnonymous { get; set; } = true; // Anonymous by default
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public ApplicationUser FromUser { get; set; } = null!;
        public ApplicationUser? ToUser { get; set; }
        public Category? ToCategory { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public NotificationType Type { get; set; }
        public bool IsRead { get; set; } = false;
        public string? LinkUrl { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ApplicationUser User { get; set; } = null!;
    }

    public enum NotificationType
    {
        General,
        MatchRequest,
        ConfirmationNeeded,
        TransactionConfirmed,
        FeedbackReceived,
        MessageReceived,
        AdminAlert,
        BalanceAlert
    }
}
