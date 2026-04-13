using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages
{
    [Authorize]
    public class DashboardModel : PageModel
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IOfferService _offerService;

        public DashboardModel(UserManager<ApplicationUser> userManager, ApplicationDbContext context, IOfferService offerService)
        {
            _userManager = userManager;
            _context = context;
            _offerService = offerService;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public decimal CurrentBalance { get; set; }
        public int ActiveMatchesCount { get; set; }
        public int PendingConfirmationsCount { get; set; }
        public int OpenOffersCount { get; set; }
        public List<TimeTransaction> RecentTransactions { get; set; } = new();
        public List<Offer> MatchingRequests { get; set; } = new();
        public List<Notification> UnreadNotifications { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            CurrentUser = await _userManager.FindByIdAsync(userId);

            if (CurrentUser == null) return;

            CurrentBalance = CurrentUser.TimeBalance;

            ActiveMatchesCount = await _context.Matches
                .CountAsync(m => (m.ProviderId == userId || m.ReceiverId == userId)
                                 && m.Status == MatchStatus.Active);

            PendingConfirmationsCount = await _context.TimeTransactions
                .CountAsync(t => (t.ProviderId == userId || t.ReceiverId == userId)
                                 && t.Status == TransactionStatus.Pending
                                 && ((t.ProviderId == userId && !t.ConfirmedByProvider)
                                     || (t.ReceiverId == userId && !t.ConfirmedByReceiver)));

            OpenOffersCount = await _context.Offers
                .CountAsync(o => o.UserId == userId && o.Status == OfferStatus.Active);

            RecentTransactions = await _context.TimeTransactions
                .Include(t => t.Category)
                .Where(t => t.ProviderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.ServiceDate)
                .Take(5)
                .ToListAsync();

            MatchingRequests = await _offerService.FindMatchesForUserAsync(userId);

            UnreadNotifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .OrderByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}
