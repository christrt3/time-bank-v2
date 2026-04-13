namespace Time_Bank_V1.Data.Entities
{
    public class AdminFlag
    {
        public int Id { get; set; }
        public string FlaggedUserId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public string Reason { get; set; } = string.Empty;
        public FlagType FlagType { get; set; }
        public FlagStatus Status { get; set; } = FlagStatus.Pending;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ResolvedAt { get; set; }
        public string? Resolution { get; set; }

        public ApplicationUser FlaggedUser { get; set; } = null!;
    }

    public enum FlagType
    {
        ProfilePhoto,
        Avatar,
        ProfileContent,
        OfferContent,
        Other
    }

    public enum FlagStatus
    {
        Pending,
        Resolved,
        Dismissed
    }
}
