namespace Time_Bank_V1.Data.Entities
{
    public class Category
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int? ParentCategoryId { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public Category? ParentCategory { get; set; }
        public ICollection<Category> SubCategories { get; set; } = new List<Category>();
        public ICollection<Offer> Offers { get; set; } = new List<Offer>();
        public ICollection<Skill> Skills { get; set; } = new List<Skill>();
    }

    public class Skill
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int CategoryId { get; set; }
        public Category Category { get; set; } = null!;
        public ICollection<UserSkill> UserSkills { get; set; } = new List<UserSkill>();
    }

    public class UserSkill
    {
        public string UserId { get; set; } = string.Empty;
        public int SkillId { get; set; }
        public ApplicationUser User { get; set; } = null!;
        public Skill Skill { get; set; } = null!;
    }

    public class Availability
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public DayOfWeek DayOfWeek { get; set; }
        public TimeSlot TimeSlot { get; set; }
        public ApplicationUser User { get; set; } = null!;
    }

    public enum TimeSlot
    {
        Morning,
        Afternoon,
        Evening
    }
}
