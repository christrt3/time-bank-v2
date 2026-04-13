using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Services
{
    public interface IAdminService
    {
        Task<List<ApplicationUser>> GetAllUsersAsync();
        Task<AdminReportData> GenerateReportAsync();
        Task<bool> FlagUserAsync(string adminId, string userId, FlagType flagType, string reason);
        Task<bool> SetMonthlyGrantAsync(string userId, bool eligible, decimal hours);
        Task<Category> AddCategoryAsync(string name, string? description, int? parentId);
        Task<bool> UpdateCategoryAsync(int id, string name, string? description, bool isActive);
        Task<bool> DeleteCategoryAsync(int id);
        Task<List<Category>> GetAllCategoriesAsync();
    }

    public class AdminReportData
    {
        public int TotalUsers { get; set; }
        public int TotalOrganizations { get; set; }
        public int ActiveOffers { get; set; }
        public int ActiveRequests { get; set; }
        public int TotalTransactions { get; set; }
        public decimal TotalHoursExchanged { get; set; }
        public int PendingTransactions { get; set; }
        public List<ApplicationUser> RecentUsers { get; set; } = new();
        public List<TimeTransaction> RecentTransactions { get; set; } = new();
        public List<ApplicationUser> LockedAccounts { get; set; } = new();
    }

    public class AdminService : IAdminService
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public AdminService(ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<List<ApplicationUser>> GetAllUsersAsync()
        {
            return await _context.Users
                .Include(u => u.UserSkills).ThenInclude(us => us.Skill)
                .OrderBy(u => u.LastName)
                .ToListAsync();
        }

        public async Task<AdminReportData> GenerateReportAsync()
        {
            var report = new AdminReportData
            {
                TotalUsers = await _context.Users.CountAsync(u => u.AccountType == "Member"),
                TotalOrganizations = await _context.Users.CountAsync(u => u.AccountType == "Organization"),
                ActiveOffers = await _context.Offers.CountAsync(o => o.Status == OfferStatus.Active && o.OfferType == OfferType.Offer),
                ActiveRequests = await _context.Offers.CountAsync(o => o.Status == OfferStatus.Active && o.OfferType == OfferType.Request),
                TotalTransactions = await _context.TimeTransactions.CountAsync(),
                TotalHoursExchanged = await _context.TimeTransactions
                    .Where(t => t.Status == TransactionStatus.Confirmed && t.TransactionType == TransactionType.Earned)
                    .SumAsync(t => t.Hours),
                PendingTransactions = await _context.TimeTransactions.CountAsync(t => t.Status == TransactionStatus.Pending),
                RecentUsers = await _context.Users.OrderByDescending(u => u.MemberSince).Take(5).ToListAsync(),
                RecentTransactions = await _context.TimeTransactions
                    .Include(t => t.Provider)
                    .Include(t => t.Receiver)
                    .OrderByDescending(t => t.CreatedAt)
                    .Take(10)
                    .ToListAsync(),
                LockedAccounts = await _context.Users.Where(u => u.IsBalanceLocked).ToListAsync()
            };
            return report;
        }

        public async Task<bool> FlagUserAsync(string adminId, string userId, FlagType flagType, string reason)
        {
            _context.AdminFlags.Add(new AdminFlag
            {
                AdminId = adminId,
                FlaggedUserId = userId,
                FlagType = flagType,
                Reason = reason,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> SetMonthlyGrantAsync(string userId, bool eligible, decimal hours)
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return false;
            user.IsEligibleForMonthlyGrant = eligible;
            user.MonthlyGrantHours = hours;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Category> AddCategoryAsync(string name, string? description, int? parentId)
        {
            var cat = new Category { Name = name, Description = description, ParentCategoryId = parentId, IsActive = true };
            _context.Categories.Add(cat);
            await _context.SaveChangesAsync();
            return cat;
        }

        public async Task<bool> UpdateCategoryAsync(int id, string name, string? description, bool isActive)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return false;
            cat.Name = name;
            cat.Description = description;
            cat.IsActive = isActive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat == null) return false;
            cat.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Category>> GetAllCategoriesAsync()
        {
            return await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }
    }
}
