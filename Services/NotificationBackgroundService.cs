using DaycareAPI.Data;
using DaycareAPI.Models;
using Microsoft.EntityFrameworkCore;

namespace DaycareAPI.Services
{
    public class NotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<NotificationBackgroundService> _logger;

        public NotificationBackgroundService(
            IServiceProvider serviceProvider,
            ILogger<NotificationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Notification Background Service started");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        
                        await CheckBirthdayReminders(context);
                        await CheckFeePaymentReminders(context);
                        await UpdateOverdueFees(context);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in Notification Background Service");
                }

                // Run every 6 hours
                await Task.Delay(TimeSpan.FromHours(6), stoppingToken);
            }
        }

        private async Task CheckBirthdayReminders(ApplicationDbContext context)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            var in3Days = today.AddDays(3);

            // Get children with birthdays in next 3 days
            var upcomingBirthdays = await context.Children
                .Include(c => c.Parent)
                .Where(c => c.IsActive)
                .ToListAsync();

            var birthdayChildren = upcomingBirthdays.Where(c =>
            {
                var birthday = new DateTime(today.Year, c.DateOfBirth.Month, c.DateOfBirth.Day);
                return birthday >= today && birthday <= in3Days;
            }).ToList();

            foreach (var child in birthdayChildren)
            {
                var birthday = new DateTime(today.Year, child.DateOfBirth.Month, child.DateOfBirth.Day);
                var daysUntil = (birthday - today).Days;
                var age = today.Year - child.DateOfBirth.Year;

                // Check if notification already sent today
                var existingNotification = await context.Notifications
                    .Where(n => n.Type == "Birthday" && 
                           n.Message.Contains(child.FirstName) && 
                           n.CreatedAt.Date == today)
                    .FirstOrDefaultAsync();

                if (existingNotification == null)
                {
                    var message = daysUntil == 0 
                        ? $"🎂 Today is {child.FirstName} {child.LastName}'s birthday! They are turning {age} years old."
                        : $"🎂 {child.FirstName} {child.LastName}'s birthday is in {daysUntil} day(s) on {birthday:MMM dd}. They will be {age} years old.";

                    // Notify admin and teachers
                    var notification = new Notification
                    {
                        Type = "Birthday",
                        Title = "Birthday Reminder",
                        Message = message,
                        RedirectUrl = $"/children/detail/{child.Id}",
                        CreatedAt = DateTime.UtcNow
                    };

                    context.Notifications.Add(notification);
                    _logger.LogInformation($"Birthday reminder created for {child.FirstName} {child.LastName}");
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task CheckFeePaymentReminders(ApplicationDbContext context)
        {
            var today = DateTime.UtcNow.Date;
            var in3Days = today.AddDays(3);
            var in7Days = today.AddDays(7);

            // Get pending fees due in next 7 days
            var upcomingFees = await context.Fees
                .Include(f => f.Child)
                    .ThenInclude(c => c.Parent)
                .Where(f => f.Status == "pending" && f.DueDate.Date >= today && f.DueDate.Date <= in7Days)
                .ToListAsync();

            foreach (var fee in upcomingFees)
            {
                var daysUntil = (fee.DueDate.Date - today).Days;

                // Send reminder at 7 days, 3 days, and 1 day before due date
                if (daysUntil == 7 || daysUntil == 3 || daysUntil == 1)
                {
                    // Check if reminder already sent today
                    var existingNotification = await context.Notifications
                        .Where(n => n.Type == "FeeReminder" && 
                               n.UserId == fee.Child!.ParentId.ToString() &&
                               n.Message.Contains(fee.Id.ToString()) &&
                               n.CreatedAt.Date == today)
                        .FirstOrDefaultAsync();

                    if (existingNotification == null)
                    {
                        var urgency = daysUntil == 1 ? "⚠️ URGENT: " : "";
                        var message = $"{urgency}Payment reminder: ${fee.Amount} for {fee.Child!.FirstName} {fee.Child.LastName} is due in {daysUntil} day(s) on {fee.DueDate:MMM dd, yyyy}. {fee.Description}";

                        var notification = new Notification
                        {
                            Type = "FeeReminder",
                            Title = "Payment Reminder",
                            Message = message,
                            RedirectUrl = "/fees",
                            UserId = fee.Child.ParentId.ToString(),
                            CreatedAt = DateTime.UtcNow
                        };

                        context.Notifications.Add(notification);
                        _logger.LogInformation($"Fee reminder created for parent {fee.Child.ParentId}, fee {fee.Id}");
                    }
                }
            }

            await context.SaveChangesAsync();
        }

        private async Task UpdateOverdueFees(ApplicationDbContext context)
        {
            var today = DateTime.UtcNow.Date;

            var overdueFees = await context.Fees
                .Include(f => f.Child)
                    .ThenInclude(c => c.Parent)
                .Where(f => f.Status == "pending" && f.DueDate.Date < today)
                .ToListAsync();

            foreach (var fee in overdueFees)
            {
                fee.Status = "overdue";
                fee.UpdatedAt = DateTime.UtcNow;

                // Notify parent about overdue fee
                var notification = new Notification
                {
                    Type = "FeeOverdue",
                    Title = "⚠️ Overdue Payment",
                    Message = $"Payment of ${fee.Amount} for {fee.Child!.FirstName} {fee.Child.LastName} is now overdue. Due date was {fee.DueDate:MMM dd, yyyy}. Please pay as soon as possible.",
                    RedirectUrl = "/fees",
                    UserId = fee.Child.ParentId.ToString(),
                    CreatedAt = DateTime.UtcNow
                };

                context.Notifications.Add(notification);
                _logger.LogInformation($"Overdue notification created for parent {fee.Child.ParentId}, fee {fee.Id}");
            }

            if (overdueFees.Any())
            {
                await context.SaveChangesAsync();
                _logger.LogInformation($"Updated {overdueFees.Count} fees to overdue status");
            }
        }
    }
}
