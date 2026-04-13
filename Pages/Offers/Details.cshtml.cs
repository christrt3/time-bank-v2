using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;
using Time_Bank_V1.Services;
using Microsoft.EntityFrameworkCore;

namespace Time_Bank_V1.Pages.Offers
{
    [Authorize]
    public class DetailsModel : PageModel
    {
        private readonly IOfferService _offerService;
        private readonly ApplicationDbContext _context;

        public DetailsModel(IOfferService offerService, ApplicationDbContext context)
        {
            _offerService = offerService;
            _context = context;
        }

        public Offer? Offer { get; set; }
        public bool AlreadyMatched { get; set; }
        public bool IsOwner { get; set; }

        public async Task<IActionResult> OnGetAsync(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            Offer = await _offerService.GetOfferByIdAsync(id);
            if (Offer == null) return NotFound();

            IsOwner = Offer.UserId == userId;
            AlreadyMatched = await _context.Matches
                .AnyAsync(m => m.OfferId == id && (m.ProviderId == userId || m.ReceiverId == userId));

            return Page();
        }

        public async Task<IActionResult> OnPostRequestMatchAsync(int offerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _offerService.CreateMatchAsync(offerId, userId);
                TempData["SuccessMessage"] = "Match request sent! Check your dashboard for updates.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }
            return RedirectToPage("/Dashboard");
        }

        public async Task<IActionResult> OnPostDeactivateAsync(int offerId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            await _offerService.DeleteOfferAsync(offerId, userId);
            TempData["SuccessMessage"] = "Offer has been deactivated.";
            return RedirectToPage("/Offers");
        }
    }
}
