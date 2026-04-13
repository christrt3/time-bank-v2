using Microsoft.EntityFrameworkCore;
using Time_Bank_V1.Data;
using Time_Bank_V1.Data.Entities;

namespace Time_Bank_V1.Services
{
    public interface ITransactionService
    {
        Task<(bool Success, string Error)> LogTransactionAsync(string providerId, string receiverId, decimal hours, string description, int? categoryId, int? matchId, DateTime serviceDate, string? notes);
        Task<(bool Success, string Error)> ConfirmTransactionAsync(int transactionId, string userId);
        Task<(bool Success, string Error)> DonateHoursAsync(string fromUserId, string toUserId, decimal hours, string? note);
        Task<(bool Success, string Error)> GrantHoursAsync(string adminId, string toUserId, decimal hours, string reason);
        Task<List<TimeTransaction>> GetUserTransactionsAsync(string userId);
        Task<decimal> GetUserBalanceAsync(string userId);
        Task ProcessMonthlyGrantsAsync();
    }

    public class TransactionService : ITransactionService
    {
        private readonly ApplicationDbContext _context;
        private const decimal MaxOverdraft = -3m;

        public TransactionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<(bool Success, string Error)> LogTransactionAsync(
            string providerId, string receiverId, decimal hours, string description,
            int? categoryId, int? matchId, DateTime serviceDate, string? notes)
        {
            var receiver = await _context.Users.FindAsync(receiverId);
            if (receiver == null) return (false, "Receiver not found.");

            // Check overdraft protection on receiver
            if (receiver.IsBalanceLocked)
                return (false, "Your account is locked due to overdraft. Please earn hours before spending more.");

            if (receiver.TimeBalance - hours < MaxOverdraft)
                return (false, $"This transaction would exceed the maximum overdraft limit of {Math.Abs(MaxOverdraft)} hours.");

            var tx = new TimeTransaction
            {
                ProviderId = providerId,
                ReceiverId = receiverId,
                Hours = hours,
                TransactionType = TransactionType.Earned,
                Status = TransactionStatus.Pending,
                ServiceDate = serviceDate,
                Notes = notes,
                Description = description,
                CategoryId = categoryId,
                MatchId = matchId,
                CreatedAt = DateTime.UtcNow
            };
            _context.TimeTransactions.Add(tx);
            await _context.SaveChangesAsync();

            // Send notifications
            await AddNotificationAsync(providerId, $"You've logged {hours}h for \"{description}\". Awaiting confirmation.", NotificationType.ConfirmationNeeded, $"/Confirmations");
            await AddNotificationAsync(receiverId, $"Service \"{description}\" has been logged. Please confirm.", NotificationType.ConfirmationNeeded, $"/Confirmations");

            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> ConfirmTransactionAsync(int transactionId, string userId)
        {
            var tx = await _context.TimeTransactions
                .Include(t => t.Provider)
                .Include(t => t.Receiver)
                .FirstOrDefaultAsync(t => t.Id == transactionId);

            if (tx == null) return (false, "Transaction not found.");
            if (tx.Status == TransactionStatus.Confirmed) return (false, "Already confirmed.");

            bool isProvider = tx.ProviderId == userId;
            bool isReceiver = tx.ReceiverId == userId;
            if (!isProvider && !isReceiver) return (false, "You are not part of this transaction.");

            if (isProvider) tx.ConfirmedByProvider = true;
            if (isReceiver) tx.ConfirmedByReceiver = true;

            // Both confirmed → finalize
            if (tx.ConfirmedByProvider && tx.ConfirmedByReceiver)
            {
                tx.Status = TransactionStatus.Confirmed;
                tx.ConfirmedAt = DateTime.UtcNow;

                // Credit provider, debit receiver
                tx.Provider.TimeBalance += tx.Hours;
                tx.Receiver.TimeBalance -= tx.Hours;

                // Check overdraft lock
                if (tx.Receiver.TimeBalance <= MaxOverdraft)
                    tx.Receiver.IsBalanceLocked = true;
                else
                    tx.Receiver.IsBalanceLocked = false;

                await AddNotificationAsync(tx.ProviderId, $"Transaction confirmed! +{tx.Hours}h added to your balance.", NotificationType.TransactionConfirmed);
                await AddNotificationAsync(tx.ReceiverId, $"Transaction confirmed! -{tx.Hours}h deducted from your balance.", NotificationType.TransactionConfirmed);
            }

            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> DonateHoursAsync(string fromUserId, string toUserId, decimal hours, string? note)
        {
            var donor = await _context.Users.FindAsync(fromUserId);
            var recipient = await _context.Users.FindAsync(toUserId);
            if (donor == null || recipient == null) return (false, "User not found.");
            if (donor.TimeBalance < hours) return (false, "Insufficient balance to donate.");

            donor.TimeBalance -= hours;
            recipient.TimeBalance += hours;

            var tx = new TimeTransaction
            {
                ProviderId = fromUserId,
                ReceiverId = toUserId,
                Hours = hours,
                TransactionType = TransactionType.Donated,
                Status = TransactionStatus.Confirmed,
                ServiceDate = DateTime.UtcNow,
                Description = $"Donation from {donor.FullName}",
                Notes = note,
                ConfirmedByProvider = true,
                ConfirmedByReceiver = true,
                ConfirmedAt = DateTime.UtcNow
            };
            _context.TimeTransactions.Add(tx);

            await AddNotificationAsync(toUserId, $"You received a donation of {hours}h from {donor.FirstName}!", NotificationType.TransactionConfirmed);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<(bool Success, string Error)> GrantHoursAsync(string adminId, string toUserId, decimal hours, string reason)
        {
            var user = await _context.Users.FindAsync(toUserId);
            if (user == null) return (false, "User not found.");

            user.TimeBalance += hours;
            if (user.TimeBalance > MaxOverdraft) user.IsBalanceLocked = false;

            var tx = new TimeTransaction
            {
                ProviderId = adminId,
                ReceiverId = toUserId,
                Hours = hours,
                TransactionType = TransactionType.Grant,
                Status = TransactionStatus.Confirmed,
                ServiceDate = DateTime.UtcNow,
                Description = $"Admin grant: {reason}",
                ConfirmedByProvider = true,
                ConfirmedByReceiver = true,
                ConfirmedAt = DateTime.UtcNow
            };
            _context.TimeTransactions.Add(tx);
            await AddNotificationAsync(toUserId, $"You received {hours}h grant from Admin. Reason: {reason}", NotificationType.TransactionConfirmed);
            await _context.SaveChangesAsync();
            return (true, string.Empty);
        }

        public async Task<List<TimeTransaction>> GetUserTransactionsAsync(string userId)
        {
            return await _context.TimeTransactions
                .Include(t => t.Category)
                .Where(t => t.ProviderId == userId || t.ReceiverId == userId)
                .OrderByDescending(t => t.ServiceDate)
                .ToListAsync();
        }

        public async Task<decimal> GetUserBalanceAsync(string userId)
        {
            var user = await _context.Users.FindAsync(userId);
            return user?.TimeBalance ?? 0m;
        }

        public async Task ProcessMonthlyGrantsAsync()
        {
            var eligibleUsers = await _context.Users
                .Where(u => u.IsEligibleForMonthlyGrant && u.MonthlyGrantHours > 0)
                .ToListAsync();

            var now = DateTime.UtcNow;
            foreach (var user in eligibleUsers)
            {
                if (user.LastGrantDate == null || (now - user.LastGrantDate.Value).TotalDays >= 30)
                {
                    user.TimeBalance += user.MonthlyGrantHours;
                    user.LastGrantDate = now;
                    if (user.TimeBalance > -3m) user.IsBalanceLocked = false;

                    _context.TimeTransactions.Add(new TimeTransaction
                    {
                        ProviderId = user.Id, // Self-grant marker
                        ReceiverId = user.Id,
                        Hours = user.MonthlyGrantHours,
                        TransactionType = TransactionType.Grant,
                        Status = TransactionStatus.Confirmed,
                        ServiceDate = now,
                        Description = "Monthly accessibility grant",
                        ConfirmedByProvider = true,
                        ConfirmedByReceiver = true,
                        ConfirmedAt = now
                    });
                }
            }
            await _context.SaveChangesAsync();
        }

        private async Task AddNotificationAsync(string userId, string message, NotificationType type, string? link = null)
        {
            _context.Notifications.Add(new Notification
            {
                UserId = userId,
                Message = message,
                Type = type,
                LinkUrl = link,
                CreatedAt = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();
        }
    }
}
