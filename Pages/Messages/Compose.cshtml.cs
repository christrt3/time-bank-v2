using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Pages.Messages
{
    [Authorize]
    public class ComposeModel : PageModel
    {
        private readonly ApplicationDbContext _context;

        public ComposeModel(ApplicationDbContext context)
        {
            _context = context;
        }

        public class MessageInputModel
        {
            public string? ToUserId { get; set; }
            public int? ToCategoryId { get; set; }

            [Required, MaxLength(200)]
            public string Subject { get; set; } = string.Empty;

            [Required, MaxLength(5000)]
            public string Body { get; set; } = string.Empty;

            public bool IsAnonymous { get; set; } = true;
            public bool SendToCategory { get; set; } = false;
        }

        [BindProperty]
        public MessageInputModel Input { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public string? ToUserId { get; set; }

        public List<SelectListItem> CategoryOptions { get; set; } = new();

        public async Task OnGetAsync()
        {
            if (!string.IsNullOrEmpty(ToUserId))
                Input.ToUserId = ToUserId;

            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategoriesAsync();
            if (!ModelState.IsValid) return Page();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;

            if (Input.SendToCategory && Input.ToCategoryId.HasValue)
            {
                // Send to all members in a category - find users who offer/request in that category
                var userIds = await _context.Offers
                    .Where(o => o.CategoryId == Input.ToCategoryId && o.Status == OfferStatus.Active)
                    .Select(o => o.UserId)
                    .Distinct()
                    .ToListAsync();

                foreach (var uid in userIds.Where(id => id != userId))
                {
                    _context.Messages.Add(new Message
                    {
                        FromUserId = userId,
                        ToUserId = uid,
                        ToCategoryId = Input.ToCategoryId,
                        Subject = Input.Subject,
                        Body = Input.Body,
                        IsAnonymous = Input.IsAnonymous,
                        CreatedAt = DateTime.UtcNow
                    });
                }
                TempData["SuccessMessage"] = $"Message sent to {userIds.Count} members in this category.";
            }
            else if (!string.IsNullOrEmpty(Input.ToUserId))
            {
                _context.Messages.Add(new Message
                {
                    FromUserId = userId,
                    ToUserId = Input.ToUserId,
                    Subject = Input.Subject,
                    Body = Input.Body,
                    IsAnonymous = Input.IsAnonymous,
                    CreatedAt = DateTime.UtcNow
                });
                TempData["SuccessMessage"] = "Message sent successfully.";
            }
            else
            {
                ModelState.AddModelError("", "Please select a recipient or a category.");
                return Page();
            }

            await _context.SaveChangesAsync();
            return RedirectToPage("/Messages/Index");
        }

        private async Task LoadCategoriesAsync()
        {
            CategoryOptions = await _context.Categories
                .Where(c => c.IsActive && c.ParentCategoryId == null)
                .OrderBy(c => c.Name)
                .Select(c => new SelectListItem { Value = c.Id.ToString(), Text = c.Name })
                .ToListAsync();
        }
    }
}
