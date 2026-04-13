using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options) { }

        public DbSet<Category> Categories => Set<Category>();
        public DbSet<Skill> Skills => Set<Skill>();
        public DbSet<UserSkill> UserSkills => Set<UserSkill>();
        public DbSet<Availability> Availabilities => Set<Availability>();
        public DbSet<Offer> Offers => Set<Offer>();
        public DbSet<Match> Matches => Set<Match>();
        public DbSet<TimeTransaction> TimeTransactions => Set<TimeTransaction>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<Message> Messages => Set<Message>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<AdminFlag> AdminFlags => Set<AdminFlag>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // UserSkill composite key
            builder.Entity<UserSkill>()
                .HasKey(us => new { us.UserId, us.SkillId });

            builder.Entity<UserSkill>()
                .HasOne(us => us.User)
                .WithMany(u => u.UserSkills)
                .HasForeignKey(us => us.UserId);

            builder.Entity<UserSkill>()
                .HasOne(us => us.Skill)
                .WithMany(s => s.UserSkills)
                .HasForeignKey(us => us.SkillId);

            // Category self-reference
            builder.Entity<Category>()
                .HasOne(c => c.ParentCategory)
                .WithMany(c => c.SubCategories)
                .HasForeignKey(c => c.ParentCategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            // Match foreign keys (restrict to avoid cycles)
            builder.Entity<Match>()
                .HasOne(m => m.Provider)
                .WithMany()
                .HasForeignKey(m => m.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Match>()
                .HasOne(m => m.Receiver)
                .WithMany()
                .HasForeignKey(m => m.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // TimeTransaction foreign keys
            builder.Entity<TimeTransaction>()
                .HasOne(t => t.Provider)
                .WithMany(u => u.TransactionsAsProvider)
                .HasForeignKey(t => t.ProviderId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<TimeTransaction>()
                .HasOne(t => t.Receiver)
                .WithMany(u => u.TransactionsAsReceiver)
                .HasForeignKey(t => t.ReceiverId)
                .OnDelete(DeleteBehavior.Restrict);

            // Feedback foreign keys
            builder.Entity<Feedback>()
                .HasOne(f => f.FromUser)
                .WithMany(u => u.FeedbackGiven)
                .HasForeignKey(f => f.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Feedback>()
                .HasOne(f => f.ToUser)
                .WithMany(u => u.FeedbackReceived)
                .HasForeignKey(f => f.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Message foreign keys
            builder.Entity<Message>()
                .HasOne(m => m.FromUser)
                .WithMany(u => u.MessagesSent)
                .HasForeignKey(m => m.FromUserId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Message>()
                .HasOne(m => m.ToUser)
                .WithMany(u => u.MessagesReceived)
                .HasForeignKey(m => m.ToUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Decimal precision
            builder.Entity<ApplicationUser>()
                .Property(u => u.TimeBalance)
                .HasColumnType("decimal(18,2)");

            builder.Entity<TimeTransaction>()
                .Property(t => t.Hours)
                .HasColumnType("decimal(18,2)");

            builder.Entity<Offer>()
                .Property(o => o.EstimatedHours)
                .HasColumnType("decimal(18,2)");
        }
    }
}
