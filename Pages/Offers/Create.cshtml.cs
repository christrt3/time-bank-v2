using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;

namespace Time_Bank_V1.Pages.Offers
{
    [Authorize]
    public class CreateModel : PageModel
    {
        private readonly IOfferService _offerService;
        private readonly ApplicationDbContext _context;

        public CreateModel(IOfferService offerService, ApplicationDbContext context)
        {
            _offerService = offerService;
            _context = context;
        }

        public class OfferInputModel
        {
            [Required, MaxLength(200)]
            public string Title { get; set; } = string.Empty;

            [Required, MaxLength(2000)]
            public string Description { get; set; } = string.Empty;

            [Required]
            public int CategoryId { get; set; }

            [Required]
            public string OfferType { get; set; } = "Offer";

            public string? Keywords { get; set; }
            public string? LocationPreference { get; set; }

            [Range(0.25, 100)]
            public decimal? EstimatedHours { get; set; }
        }

        [BindProperty]
        public OfferInputModel Input { get; set; } = new();

        public List<Category> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            await LoadCategoriesAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            await LoadCategoriesAsync();
            if (!ModelState.IsValid) return Page();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _offerService.CreateOfferAsync(
                userId, Input.Title, Input.Description,
                Input.CategoryId, Enum.Parse<OfferType>(Input.OfferType),
                Input.Keywords, Input.LocationPreference, Input.EstimatedHours);

            TempData["SuccessMessage"] = $"Your {Input.OfferType.ToLower()} \"{Input.Title}\" has been posted successfully.";
            return RedirectToPage("/Offers");
        }

        private async Task LoadCategoriesAsync()
        {
            Categories = await _context.Categories
                .Include(c => c.SubCategories)
                .Where(c => c.IsActive)
                .OrderBy(c => c.ParentCategoryId).ThenBy(c => c.Name)
                .ToListAsync();
        }
    }
}
