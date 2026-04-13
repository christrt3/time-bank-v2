using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Confirmations
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ITransactionService _txService;
        private readonly ApplicationDbContext _context;

        public IndexModel(ITransactionService txService, ApplicationDbContext context)
        {
            _txService = txService;
            _context = context;
        }

        public List<TimeTransaction> PendingTransactions { get; set; } = new();
        public List<TimeTransaction> ConfirmedTransactions { get; set; } = new();

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            var all = await _context.TimeTransactions
                .Include(t => t.Provider)
                .Include(t => t.Receiver)
                .Include(t => t.Category)
                .Where(t => t.ProviderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            PendingTransactions = all
                .Where(t => t.Status == TransactionStatus.Pending)
                .ToList();

            ConfirmedTransactions = all
                .Where(t => t.Status == TransactionStatus.Confirmed)
                .Take(10)
                .ToList();
        }

        public async Task<IActionResult> OnPostConfirmAsync(int transactionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var (success, error) = await _txService.ConfirmTransactionAsync(transactionId, userId);

            if (success)
                TempData["SuccessMessage"] = "Transaction confirmed! Hours have been updated.";
            else
                TempData["ErrorMessage"] = error;

            return RedirectToPage();
        }

        public async Task<IActionResult> OnPostLeavesFeedbackAsync(int transactionId, string toUserId, string? comment)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var tx = await _context.TimeTransactions.FindAsync(transactionId);
            if (tx == null) return RedirectToPage();

            // Check if feedback already given
            var existing = await _context.Feedbacks.AnyAsync(f => f.FromUserId == userId && f.TransactionId == transactionId);
            if (!existing)
            {
                _context.Feedbacks.Add(new Feedback
                {
                    FromUserId = userId,
                    ToUserId = toUserId,
                    TransactionId = transactionId,
                    IsPositive = true,
                    Comment = comment,
                    CreatedAt = DateTime.UtcNow
                });
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Thank you for your feedback!";
            }
            return RedirectToPage();
        }
    }
}
