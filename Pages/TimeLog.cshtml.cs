using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages
{
    [Authorize]
    public class TimeLogModel : PageModel
    {
        private readonly ITransactionService _txService;
        private readonly ApplicationDbContext _context;

        public TimeLogModel(ITransactionService txService, ApplicationDbContext context)
        {
            _txService = txService;
            _context = context;
        }

        public class TimeLogInputModel
        {
            [Required(ErrorMessage = "Please select a match.")]
            public int? MatchId { get; set; }

            [Required]
            [Range(0.25, 24, ErrorMessage = "Hours must be between 0.25 and 24.")]
            public decimal Hours { get; set; } = 1m;

            [Required]
            public string TransactionType { get; set; } = "Earned";

            [Required]
            [DataType(DataType.Date)]
            public DateTime ServiceDate { get; set; } = DateTime.Today;

            [Required]
            [MaxLength(200)]
            public string Description { get; set; } = string.Empty;

            [MaxLength(500)]
            public string? Notes { get; set; }

            public int? CategoryId { get; set; }
        }

        [BindProperty]
        public TimeLogInputModel LogEntry { get; set; } = new();

        public List<SelectListItem> MatchOptions { get; set; } = new();
        public List<SelectListItem> CategoryOptions { get; set; } = new();
        public List<TimeTransaction> RecentEntries { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadDataAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await LoadDataAsync();

            if (!ModelState.IsValid) return Page();

            var match = await _context.Matches
                .Include(m => m.Provider)
                .Include(m => m.Receiver)
                .FirstOrDefaultAsync(m => m.Id == LogEntry.MatchId);

            if (match == null)
            {
                ModelState.AddModelError("", "Selected match not found.");
                return Page();
            }

            string receiverId = match.ProviderId == userId ? match.ReceiverId : match.ProviderId;

            var (success, error) = await _txService.LogTransactionAsync(
                userId, receiverId, LogEntry.Hours,
                LogEntry.Description, LogEntry.CategoryId,
                LogEntry.MatchId, LogEntry.ServiceDate, LogEntry.Notes);

            if (!success)
            {
                ModelState.AddModelError("", error);
                return Page();
            }

            TempData["SuccessMessage"] = $"Successfully logged {LogEntry.Hours}h. Awaiting confirmation from the other party.";
            return RedirectToPage("/Dashboard");
        }

        private async Task LoadDataAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var matches = await _context.Matches
                .Include(m => m.Offer)
                .Include(m => m.Provider)
                .Include(m => m.Receiver)
                .Where(m => (m.ProviderId == userId || m.ReceiverId == userId) && m.Status == MatchStatus.Active)
                .ToListAsync();

            MatchOptions = matches.Select(m => new SelectListItem
            {
                Value = m.Id.ToString(),
                Text = $"{m.Offer.Title} (with {(m.ProviderId == userId ? m.Receiver.FirstName : m.Provider.FirstName)})"
            }).ToList();

            CategoryOptions = await _context.Categories
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentCategoryId).ThenBy(c => c.Name)
                .Select(c => new SelectListItem
                {
                    Value = c.Id.ToString(),
                    Text = c.ParentCategoryId == null ? c.Name : $"  └ {c.Name}"
                })
                .ToListAsync();

            RecentEntries = await _context.TimeTransactions
                .Include(t => t.Category)
                .Where(t => t.ProviderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .Take(5)
                .ToListAsync();
        }
    }
}
