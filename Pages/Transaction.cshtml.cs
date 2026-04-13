using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using System.Text;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages
{
    [Authorize]
    public class TransactionModel : PageModel
    {
        private readonly ITransactionService _txService;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionModel(ITransactionService txService, ApplicationDbContext context, UserManager<ApplicationUser> userManager)
        {
            _txService = txService;
            _context = context;
            _userManager = userManager;
        }

        public ApplicationUser? CurrentUser { get; set; }
        public decimal CurrentBalance { get; set; }
        public decimal TotalEarned { get; set; }
        public decimal TotalSpent { get; set; }
        public decimal TotalDonated { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? TypeFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? StatusFilter { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DateFrom { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? DateTo { get; set; }

        public List<TimeTransaction> Transactions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            CurrentUser = await _userManager.FindByIdAsync(userId);
            CurrentBalance = CurrentUser?.TimeBalance ?? 0;

            var all = await _txService.GetUserTransactionsAsync(userId);

            TotalEarned = all.Where(t => t.TransactionType == TransactionType.Earned && t.Status == TransactionStatus.Confirmed && t.ProviderId == userId).Sum(t => t.Hours);
            TotalSpent = all.Where(t => t.TransactionType != TransactionType.Earned && t.Status == TransactionStatus.Confirmed && t.ReceiverId == userId).Sum(t => t.Hours);
            TotalDonated = all.Where(t => t.TransactionType == TransactionType.Donated && t.Status == TransactionStatus.Confirmed && t.ProviderId == userId).Sum(t => t.Hours);

            // Apply filters
            IEnumerable<TimeTransaction> filtered = all;

            if (!string.IsNullOrEmpty(TypeFilter) && TypeFilter != "All")
                filtered = filtered.Where(t => t.TransactionType.ToString() == TypeFilter);

            if (!string.IsNullOrEmpty(StatusFilter) && StatusFilter != "All")
                filtered = filtered.Where(t => t.Status.ToString() == StatusFilter);

            if (DateTime.TryParse(DateFrom, out var df))
                filtered = filtered.Where(t => t.ServiceDate >= df);

            if (DateTime.TryParse(DateTo, out var dt))
                filtered = filtered.Where(t => t.ServiceDate <= dt);

            Transactions = filtered.ToList();
        }

        public async Task<IActionResult> OnGetDownloadAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var all = await _txService.GetUserTransactionsAsync(userId);

            var sb = new StringBuilder();
            sb.AppendLine("Date,Description,Category,Type,Status,Hours");
            foreach (var tx in all)
            {
                sb.AppendLine($"{tx.ServiceDate:yyyy-MM-dd},\"{tx.Description}\",\"{tx.Category?.Name ?? ""}\",{tx.TransactionType},{tx.Status},{tx.Hours}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", $"ACC_Statement_{DateTime.Now:yyyyMMdd}.csv");
        }
    }
}
