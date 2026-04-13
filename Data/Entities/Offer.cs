namespace Time_Bank_V1.Data.Entities
{
    public class Offer
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public OfferType OfferType { get; set; } = OfferType.Offer; // Offer or Request
        public OfferStatus Status { get; set; } = OfferStatus.Active;
        public string? Keywords { get; set; } // Comma-separated for matching
        public string? LocationPreference { get; set; }
        public decimal? EstimatedHours { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ApplicationUser User { get; set; } = null!;
        public Category Category { get; set; } = null!;
        public ICollection<Match> Matches { get; set; } = new List<Match>();
    }

    public enum OfferType
    {
        Offer,
        Request
    }

    public enum OfferStatus
    {
        Active,
        Fulfilled,
        Inactive,
        Flagged
    }
}
