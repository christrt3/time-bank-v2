using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;
using Microsoft.EntityFrameworkCore;

namespace Time_Bank_V1.Pages
{
    [Authorize]
    public class OffersModel : PageModel
    {
        private readonly IOfferService _offerService;
        private readonly ApplicationDbContext _context;

        public OffersModel(IOfferService offerService, ApplicationDbContext context)
        {
            _offerService = offerService;
            _context = context;
        }

        [BindProperty(SupportsGet = true)]
        public string? Keyword { get; set; }

        [BindProperty(SupportsGet = true)]
        public int? CategoryId { get; set; }

        [BindProperty(SupportsGet = true)]
        public string? OfferTypeFilter { get; set; } // "Offer" or "Request" or null (all)

        public List<Offer> OffersList { get; set; } = new();
        public List<Category> Categories { get; set; } = new();

        public async Task OnGetAsync()
        {
            OfferType? typeFilter = OfferTypeFilter switch
            {
                "Offer" => OfferType.Offer,
                "Request" => OfferType.Request,
                _ => null
            };

            var all = await _offerService.GetActiveOffersAsync(Keyword, CategoryId);
            OffersList = typeFilter.HasValue
                ? all.Where(o => o.OfferType == typeFilter.Value).ToList()
                : all;

            Categories = await _context.Categories
                .Where(c => c.ParentCategoryId == null && c.IsActive)
                .OrderBy(c => c.Name)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostRequestMatchAsync(int offerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _offerService.CreateMatchAsync(offerId, userId);
                TempData["SuccessMessage"] = "Match request sent! The other party will be notified.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage();
        }
    }
}
