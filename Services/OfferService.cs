using Microsoft.EntityFrameworkCore;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Services
{
    public interface IOfferService
    {
        Task<List<Offer>> GetActiveOffersAsync(string? keyword = null, int? categoryId = null, string? location = null);
        Task<List<Offer>> GetUserOffersAsync(string userId);
        Task<Offer?> GetOfferByIdAsync(int id);
        Task<Offer> CreateOfferAsync(string userId, string title, string description, int categoryId, OfferType type, string? keywords, string? location, decimal? estimatedHours);
        Task<bool> UpdateOfferAsync(int id, string userId, string title, string description, int categoryId, OfferType type, string? keywords, OfferStatus status);
        Task<bool> DeleteOfferAsync(int id, string userId);
        Task<List<Offer>> FindMatchesForUserAsync(string userId);
        Task<Match> CreateMatchAsync(int offerId, string requesterId);
        Task<List<Match>> GetUserMatchesAsync(string userId);
        Task<bool> UpdateMatchStatusAsync(int matchId, string userId, MatchStatus status);
    }

    public class OfferService : IOfferService
    {
        private readonly ApplicationDbContext _context;

        public OfferService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<Offer>> GetActiveOffersAsync(string? keyword = null, int? categoryId = null, string? location = null)
        {
            var query = _context.Offers
                .Include(o => o.User)
                .Include(o => o.Category)
                .Where(o => o.Status == OfferStatus.Active);

            if (!string.IsNullOrWhiteSpace(keyword))
            {
                var kw = keyword.ToLower();
                query = query.Where(o =>
                    o.Title.ToLower().Contains(kw) ||
                    o.Description.ToLower().Contains(kw) ||
                    (o.Keywords != null && o.Keywords.ToLower().Contains(kw)));
            }

            if (categoryId.HasValue)
                query = query.Where(o => o.CategoryId == categoryId || o.Category.ParentCategoryId == categoryId);

            if (!string.IsNullOrWhiteSpace(location))
            {
                var loc = location.ToLower();
                query = query.Where(o =>
                    (o.User.City != null && o.User.City.ToLower().Contains(loc)) ||
                    (o.User.ZipCode != null && o.User.ZipCode.Contains(location)) ||
                    (o.LocationPreference != null && o.LocationPreference.ToLower().Contains(loc)));
            }

            return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
        }

        public async Task<List<Offer>> GetUserOffersAsync(string userId)
        {
            return await _context.Offers
                .Include(o => o.Category)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .ToListAsync();
        }

        public async Task<Offer?> GetOfferByIdAsync(int id)
        {
            return await _context.Offers
                .Include(o => o.User)
                .Include(o => o.Category)
                .Include(o => o.Matches)
                .FirstOrDefaultAsync(o => o.Id == id);
        }

        public async Task<Offer> CreateOfferAsync(string userId, string title, string description, int categoryId, OfferType type, string? keywords, string? location, decimal? estimatedHours)
        {
            var offer = new Offer
            {
                UserId = userId,
                Title = title,
                Description = description,
                CategoryId = categoryId,
                OfferType = type,
                Keywords = keywords,
                LocationPreference = location,
                EstimatedHours = estimatedHours,
                Status = OfferStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            _context.Offers.Add(offer);
            await _context.SaveChangesAsync();
            return offer;
        }

        public async Task<bool> UpdateOfferAsync(int id, string userId, string title, string description, int categoryId, OfferType type, string? keywords, OfferStatus status)
        {
            var offer = await _context.Offers.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (offer == null) return false;
            offer.Title = title;
            offer.Description = description;
            offer.CategoryId = categoryId;
            offer.OfferType = type;
            offer.Keywords = keywords;
            offer.Status = status;
            offer.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteOfferAsync(int id, string userId)
        {
            var offer = await _context.Offers.FirstOrDefaultAsync(o => o.Id == id && o.UserId == userId);
            if (offer == null) return false;
            offer.Status = OfferStatus.Inactive;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<Offer>> FindMatchesForUserAsync(string userId)
        {
            var userSkills = await _context.UserSkills
                .Include(us => us.Skill)
                .Where(us => us.UserId == userId)
                .Select(us => us.Skill.CategoryId)
                .ToListAsync();

            // Find Requests that match the user's skill categories
            return await _context.Offers
                .Include(o => o.User)
                .Include(o => o.Category)
                .Where(o =>
                    o.Status == OfferStatus.Active &&
                    o.OfferType == OfferType.Request &&
                    o.UserId != userId &&
                    userSkills.Contains(o.CategoryId))
                .OrderByDescending(o => o.CreatedAt)
                .Take(10)
                .ToListAsync();
        }

        public async Task<Match> CreateMatchAsync(int offerId, string requesterId)
        {
            var offer = await _context.Offers.FindAsync(offerId);
            if (offer == null) throw new Exception("Offer not found");

            string providerId, receiverId;
            if (offer.OfferType == OfferType.Offer)
            {
                providerId = offer.UserId;
                receiverId = requesterId;
            }
            else
            {
                providerId = requesterId;
                receiverId = offer.UserId;
            }

            var match = new Match
            {
                OfferId = offerId,
                ProviderId = providerId,
                ReceiverId = receiverId,
                Status = MatchStatus.Pending,
                CreatedAt = DateTime.UtcNow
            };
            _context.Matches.Add(match);
            await _context.SaveChangesAsync();
            return match;
        }

        public async Task<List<Match>> GetUserMatchesAsync(string userId)
        {
            return await _context.Matches
                .Include(m => m.Offer).ThenInclude(o => o.Category)
                .Include(m => m.Provider)
                .Include(m => m.Receiver)
                .Where(m => m.ProviderId == userId || m.ReceiverId == userId)
                .OrderByDescending(m => m.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> UpdateMatchStatusAsync(int matchId, string userId, MatchStatus status)
        {
            var match = await _context.Matches
                .FirstOrDefaultAsync(m => m.Id == matchId && (m.ProviderId == userId || m.ReceiverId == userId));
            if (match == null) return false;
            match.Status = status;
            if (status == MatchStatus.Completed) match.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
