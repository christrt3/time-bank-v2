using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Pages.Messages
{
    [Authorize]
    public class IndexModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public IndexModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public List<Message> Inbox { get; set; } = new();
        public List<Message> Sent { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string Tab { get; set; } = "inbox";

        public async Task OnGetAsync()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            Inbox = await _context.Messages
                .Include(m => m.FromUser)
                .Where(m => m.ToUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            Sent = await _context.Messages
                .Include(m => m.ToUser)
                .Include(m => m.ToCategory)
                .Where(m => m.FromUserId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();

            // Mark inbox as read
            var unread = Inbox.Where(m => !m.IsRead).ToList();
            foreach (var m in unread) m.IsRead = true;
            await _context.SaveChangesAsync();
        }

        public async Task<IActionResult> OnPostDeleteAsync(int messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var msg = await _context.Messages.FirstOrDefaultAsync(m => m.Id == messageId && m.ToUserId == userId);
            if (msg != null) _context.Messages.Remove(msg);
            await _context.SaveChangesAsync();
            return RedirectToPage();
        }
    }
}
