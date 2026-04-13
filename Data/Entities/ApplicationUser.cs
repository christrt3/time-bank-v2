using Microsoft.AspNetCore.Identity;

namespace Time_Bank_V1.Data.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? PhoneNumber2 { get; set; }
        public string AccountType { get; set; } = "Member"; // Member, Organization, Admin
        public string? City { get; set; }
        public string? ZipCode { get; set; }
        public string? Bio { get; set; }
        public string? Licensure { get; set; }
        public string? ProfilePicturePath { get; set; }
        public string PreferredLanguage { get; set; } = "English";
        public bool IsGuardian { get; set; }
        public DateTime MemberSince { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Time Balance
        public decimal TimeBalance { get; set; } = 0m;

        // Overdraft: max -3 hours. When reached, locked from spending.
        public bool IsBalanceLocked { get; set; } = false;

        // Accessibility / Monthly Grant
        public bool IsEligibleForMonthlyGrant { get; set; } = false;
        public decimal MonthlyGrantHours { get; set; } = 0m;
        public DateTime? LastGrantDate { get; set; }

        // Notification preferences
        public bool NotifyInApp { get; set; } = true;
        public bool NotifyEmail { get; set; } = true;
        public bool NotifySms { get; set; } = false;
        public bool NotifyMatchAlerts { get; set; } = true;

        // Public profile visibility settings (comma-separated field names)
        public string? PublicFieldVisibility { get; set; } = "Name,City,Skills,Availability,Bio";

        // Show licensure on public profile?
        public bool ShowLicensure { get; set; } = false;

        // Organization name (if AccountType == Organization)
        public string? OrganizationName { get; set; }

        public string FullName => $"{FirstName} {LastName}".Trim();
        public string Initials => $"{(FirstName.Length > 0 ? FirstName[0] : ' ')}{(LastName.Length > 0 ? LastName[0] : ' ')}".ToUpper().Trim();

        // Navigation
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
        public ICollection<Availability> Availabilities { get; set; } = new List<Availability>();
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public ICollection<TimeTransaction> TransactionsAsProvider { get; set; } = new List<TimeTransaction>();
        public ICollection<TimeTransaction> TransactionsAsReceiver { get; set; } = new List<TimeTransaction>();
        public ICollection<Feedback> FeedbackGiven { get; set; } = new List<Feedback>();
        public ICollection<Feedback> FeedbackReceived { get; set; } = new List<Feedback>();
        public ICollection<Message> MessagesSent { get; set; } = new List<Message>();
        public ICollection<Message> MessagesReceived { get; set; } = new List<Message>();
        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
