using DaycareAPI.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DaycareAPI.Data
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;

        public DatabaseSeeder(
            ApplicationDbContext context,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager)
        {
            _context = context;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        public async Task SeedAsync()
        {
            // Check if data already exists
            if (await _context.Parents.AnyAsync())
            {
                Console.WriteLine("Database already seeded. Skipping...");
                return;
            }

            Console.WriteLine("Starting database seeding...");

            // Seed Roles
            await SeedRolesAsync();

            // Seed Admin User
            await SeedAdminUserAsync();

            // Seed Parents and Children
            await SeedParentsAndChildrenAsync();

            // Seed Attendance Records
            await SeedAttendanceAsync();

            // Seed Daily Activities
            await SeedDailyActivitiesAsync();

            // Seed Notifications
            await SeedNotificationsAsync();

            await _context.SaveChangesAsync();

            Console.WriteLine("Database seeding completed successfully!");
        }

        private async Task SeedRolesAsync()
        {
            string[] roles = { "Admin", "Parent", "Staff" };

            foreach (var role in roles)
            {
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole(role));
                    Console.WriteLine($"Role '{role}' created.");
                }
            }
        }

        private async Task SeedAdminUserAsync()
        {
            var adminEmail = "admin@daycare.com";
            var adminUser = await _userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FirstName = "Admin",
                    LastName = "User"
                };

                var result = await _userManager.CreateAsync(adminUser, "Admin@123");
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(adminUser, "Admin");
                    Console.WriteLine("Admin user created successfully.");
                }
            }
        }

        private async Task SeedParentsAndChildrenAsync()
        {
            // Sample profile pictures (small Base64 encoded images)
            var profilePictures = GetSampleProfilePictures();

            var parents = new List<Parent>
            {
                new Parent
                {
                    FirstName = "John",
                    LastName = "Smith",
                    Email = "john.smith@email.com",
                    PhoneNumber = "555-0101",
                    Address = "123 Oak Street, Springfield",
                    ProfilePicture = profilePictures[0],
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new Parent
                {
                    FirstName = "Sarah",
                    LastName = "Johnson",
                    Email = "sarah.johnson@email.com",
                    PhoneNumber = "555-0102",
                    Address = "456 Maple Avenue, Springfield",
                    ProfilePicture = profilePictures[1],
                    CreatedAt = DateTime.Now.AddMonths(-5)
                },
                new Parent
                {
                    FirstName = "Michael",
                    LastName = "Williams",
                    Email = "michael.williams@email.com",
                    PhoneNumber = "555-0103",
                    Address = "789 Pine Road, Springfield",
                    ProfilePicture = profilePictures[2],
                    CreatedAt = DateTime.Now.AddMonths(-4)
                },
                new Parent
                {
                    FirstName = "Emily",
                    LastName = "Brown",
                    Email = "emily.brown@email.com",
                    PhoneNumber = "555-0104",
                    Address = "321 Elm Street, Springfield",
                    ProfilePicture = profilePictures[3],
                    CreatedAt = DateTime.Now.AddMonths(-7)
                },
                new Parent
                {
                    FirstName = "David",
                    LastName = "Davis",
                    Email = "david.davis@email.com",
                    PhoneNumber = "555-0105",
                    Address = "654 Birch Lane, Springfield",
                    ProfilePicture = profilePictures[4],
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new Parent
                {
                    FirstName = "Jessica",
                    LastName = "Miller",
                    Email = "jessica.miller@email.com",
                    PhoneNumber = "555-0106",
                    Address = "987 Cedar Court, Springfield",
                    ProfilePicture = profilePictures[5],
                    CreatedAt = DateTime.Now.AddMonths(-8)
                },
                new Parent
                {
                    FirstName = "Robert",
                    LastName = "Wilson",
                    Email = "robert.wilson@email.com",
                    PhoneNumber = "555-0107",
                    Address = "147 Willow Way, Springfield",
                    ProfilePicture = profilePictures[6],
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new Parent
                {
                    FirstName = "Amanda",
                    LastName = "Moore",
                    Email = "amanda.moore@email.com",
                    PhoneNumber = "555-0108",
                    Address = "258 Spruce Street, Springfield",
                    ProfilePicture = profilePictures[7],
                    CreatedAt = DateTime.Now.AddMonths(-9)
                },
                new Parent
                {
                    FirstName = "Christopher",
                    LastName = "Taylor",
                    Email = "chris.taylor@email.com",
                    PhoneNumber = "555-0109",
                    Address = "369 Ash Avenue, Springfield",
                    ProfilePicture = profilePictures[8],
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                new Parent
                {
                    FirstName = "Jennifer",
                    LastName = "Anderson",
                    Email = "jennifer.anderson@email.com",
                    PhoneNumber = "555-0110",
                    Address = "741 Poplar Place, Springfield",
                    ProfilePicture = profilePictures[9],
                    CreatedAt = DateTime.Now.AddMonths(-10)
                }
            };

            await _context.Parents.AddRangeAsync(parents);
            await _context.SaveChangesAsync();

            Console.WriteLine($"{parents.Count} parents seeded.");

            // Seed Children
            var childProfilePictures = GetSampleChildProfilePictures();
            var allergiesList = new[] { null, "Peanuts", "Dairy", "Eggs", null, "Shellfish", null, "Gluten", null, "Soy", null, "Tree nuts" };
            var genders = new[] { "Male", "Female" };

            var children = new List<Child>
            {
                new Child
                {
                    FirstName = "Emma",
                    LastName = "Smith",
                    DateOfBirth = DateTime.Now.AddYears(-3).AddMonths(-2),
                    Gender = "Female",
                    Allergies = allergiesList[0],
                    ParentId = parents[0].Id,
                    ProfilePicture = childProfilePictures[0],
                    EnrollmentDate = DateTime.Now.AddMonths(-6),
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new Child
                {
                    FirstName = "Liam",
                    LastName = "Smith",
                    DateOfBirth = DateTime.Now.AddYears(-4).AddMonths(-6),
                    Gender = "Male",
                    Allergies = allergiesList[1],
                    ParentId = parents[0].Id,
                    ProfilePicture = childProfilePictures[1],
                    EnrollmentDate = DateTime.Now.AddMonths(-6),
                    CreatedAt = DateTime.Now.AddMonths(-6)
                },
                new Child
                {
                    FirstName = "Olivia",
                    LastName = "Johnson",
                    DateOfBirth = DateTime.Now.AddYears(-2).AddMonths(-8),
                    Gender = "Female",
                    Allergies = allergiesList[2],
                    ParentId = parents[1].Id,
                    ProfilePicture = childProfilePictures[2],
                    EnrollmentDate = DateTime.Now.AddMonths(-5),
                    CreatedAt = DateTime.Now.AddMonths(-5)
                },
                new Child
                {
                    FirstName = "Noah",
                    LastName = "Williams",
                    DateOfBirth = DateTime.Now.AddYears(-3).AddMonths(-10),
                    Gender = "Male",
                    Allergies = allergiesList[3],
                    ParentId = parents[2].Id,
                    ProfilePicture = childProfilePictures[3],
                    EnrollmentDate = DateTime.Now.AddMonths(-4),
                    CreatedAt = DateTime.Now.AddMonths(-4)
                },
                new Child
                {
                    FirstName = "Ava",
                    LastName = "Brown",
                    DateOfBirth = DateTime.Now.AddYears(-4).AddMonths(-1),
                    Gender = "Female",
                    Allergies = allergiesList[4],
                    ParentId = parents[3].Id,
                    ProfilePicture = childProfilePictures[4],
                    EnrollmentDate = DateTime.Now.AddMonths(-7),
                    CreatedAt = DateTime.Now.AddMonths(-7)
                },
                new Child
                {
                    FirstName = "Sophia",
                    LastName = "Brown",
                    DateOfBirth = DateTime.Now.AddYears(-2).AddMonths(-3),
                    Gender = "Female",
                    Allergies = allergiesList[5],
                    ParentId = parents[3].Id,
                    ProfilePicture = childProfilePictures[5],
                    EnrollmentDate = DateTime.Now.AddMonths(-7),
                    CreatedAt = DateTime.Now.AddMonths(-7)
                },
                new Child
                {
                    FirstName = "Jackson",
                    LastName = "Davis",
                    DateOfBirth = DateTime.Now.AddYears(-3).AddMonths(-5),
                    Gender = "Male",
                    Allergies = allergiesList[6],
                    ParentId = parents[4].Id,
                    ProfilePicture = childProfilePictures[6],
                    EnrollmentDate = DateTime.Now.AddMonths(-3),
                    CreatedAt = DateTime.Now.AddMonths(-3)
                },
                new Child
                {
                    FirstName = "Isabella",
                    LastName = "Miller",
                    DateOfBirth = DateTime.Now.AddYears(-2).AddMonths(-11),
                    Gender = "Female",
                    Allergies = allergiesList[7],
                    ParentId = parents[5].Id,
                    ProfilePicture = childProfilePictures[7],
                    EnrollmentDate = DateTime.Now.AddMonths(-8),
                    CreatedAt = DateTime.Now.AddMonths(-8)
                },
                new Child
                {
                    FirstName = "Mason",
                    LastName = "Wilson",
                    DateOfBirth = DateTime.Now.AddYears(-4).AddMonths(-4),
                    Gender = "Male",
                    Allergies = allergiesList[8],
                    ParentId = parents[6].Id,
                    ProfilePicture = childProfilePictures[8],
                    EnrollmentDate = DateTime.Now.AddMonths(-2),
                    CreatedAt = DateTime.Now.AddMonths(-2)
                },
                new Child
                {
                    FirstName = "Mia",
                    LastName = "Moore",
                    DateOfBirth = DateTime.Now.AddYears(-3).AddMonths(-7),
                    Gender = "Female",
                    Allergies = allergiesList[9],
                    ParentId = parents[7].Id,
                    ProfilePicture = childProfilePictures[9],
                    EnrollmentDate = DateTime.Now.AddMonths(-9),
                    CreatedAt = DateTime.Now.AddMonths(-9)
                },
                new Child
                {
                    FirstName = "Ethan",
                    LastName = "Taylor",
                    DateOfBirth = DateTime.Now.AddYears(-2).AddMonths(-6),
                    Gender = "Male",
                    Allergies = allergiesList[10],
                    ParentId = parents[8].Id,
                    ProfilePicture = childProfilePictures[10],
                    EnrollmentDate = DateTime.Now.AddMonths(-1),
                    CreatedAt = DateTime.Now.AddMonths(-1)
                },
                new Child
                {
                    FirstName = "Charlotte",
                    LastName = "Anderson",
                    DateOfBirth = DateTime.Now.AddYears(-4).AddMonths(-9),
                    Gender = "Female",
                    Allergies = allergiesList[11],
                    ParentId = parents[9].Id,
                    ProfilePicture = childProfilePictures[11],
                    EnrollmentDate = DateTime.Now.AddMonths(-10),
                    CreatedAt = DateTime.Now.AddMonths(-10)
                }
            };

            await _context.Children.AddRangeAsync(children);
            await _context.SaveChangesAsync();

            Console.WriteLine($"{children.Count} children seeded.");
        }

        private async Task SeedAttendanceAsync()
        {
            var children = await _context.Children.ToListAsync();
            var attendances = new List<Attendance>();

            // Create attendance records for the last 30 days
            for (int i = 0; i < 30; i++)
            {
                var date = DateTime.Now.AddDays(-i).Date;

                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                foreach (var child in children)
                {
                    // 90% attendance rate
                    if (new Random().Next(100) < 90)
                    {
                        var checkInTime = date.AddHours(7 + new Random().Next(3)).AddMinutes(new Random().Next(60));
                        var checkOutTime = date.AddHours(15 + new Random().Next(3)).AddMinutes(new Random().Next(60));

                        attendances.Add(new Attendance
                        {
                            ChildId = child.Id,
                            Date = date,
                            CheckInTime = checkInTime,
                            CheckOutTime = checkOutTime,
                            CheckInNotes = i % 5 == 0 ? "Had a great day!" : null,
                            CreatedAt = checkInTime
                        });
                    }
                }
            }

            await _context.Attendances.AddRangeAsync(attendances);
            await _context.SaveChangesAsync();

            Console.WriteLine($"{attendances.Count} attendance records seeded.");
        }

        private async Task SeedDailyActivitiesAsync()
        {
            var children = await _context.Children.ToListAsync();
            var activities = new List<DailyActivity>();

            var activityTypes = new[] { "Meal", "Nap", "Play", "Learning", "Outdoor" };
            var mealDescriptions = new[] { "Breakfast - Oatmeal and fruit", "Lunch - Chicken and vegetables", "Snack - Crackers and cheese", "Dinner - Pasta with sauce" };
            var napDescriptions = new[] { "Nap time - 2 hours", "Short nap - 1 hour", "Rested well" };
            var playDescriptions = new[] { "Building blocks", "Arts and crafts", "Pretend play", "Music time" };
            var learningDescriptions = new[] { "ABC practice", "Counting games", "Story time", "Color recognition" };
            var outdoorDescriptions = new[] { "Playground time", "Nature walk", "Outdoor games", "Sandbox play" };

            // Create activities for the last 7 days
            for (int i = 0; i < 7; i++)
            {
                var date = DateTime.Now.AddDays(-i).Date;

                // Skip weekends
                if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday)
                    continue;

                foreach (var child in children)
                {
                    // Add 3-5 activities per child per day
                    var activityCount = new Random().Next(3, 6);

                    for (int j = 0; j < activityCount; j++)
                    {
                        var activityType = activityTypes[new Random().Next(activityTypes.Length)];
                        var description = activityType switch
                        {
                            "Meal" => mealDescriptions[new Random().Next(mealDescriptions.Length)],
                            "Nap" => napDescriptions[new Random().Next(napDescriptions.Length)],
                            "Play" => playDescriptions[new Random().Next(playDescriptions.Length)],
                            "Learning" => learningDescriptions[new Random().Next(learningDescriptions.Length)],
                            "Outdoor" => outdoorDescriptions[new Random().Next(outdoorDescriptions.Length)],
                            _ => "Activity"
                        };

                        activities.Add(new DailyActivity
                        {
                            ChildId = child.Id,
                            ActivityType = activityType,
                            Notes = description,
                            ActivityTime = date.AddHours(8 + j * 2).AddMinutes(new Random().Next(60)),
                            CreatedAt = date.AddHours(8 + j * 2)
                        });
                    }
                }
            }

            await _context.DailyActivities.AddRangeAsync(activities);
            await _context.SaveChangesAsync();

            Console.WriteLine($"{activities.Count} daily activities seeded.");
        }

        private async Task SeedNotificationsAsync()
        {
            var parents = await _context.Parents.Include(p => p.Children).ToListAsync();
            var notifications = new List<Notification>();

            var notificationMessages = new[]
            {
                "Reminder: Parent-teacher conference next week",
                "School will be closed on Monday for holiday",
                "Please bring extra clothes for outdoor activities",
                "Photo day is coming up this Friday",
                "Monthly newsletter is now available",
                "Reminder: Tuition payment due soon",
                "Special event: Summer picnic next month",
                "Health screening scheduled for next week"
            };

            foreach (var parent in parents)
            {
                // Create 2-4 notifications per parent
                var notificationCount = new Random().Next(2, 5);

                for (int i = 0; i < notificationCount; i++)
                {
                    notifications.Add(new Notification
                    {
                        UserId = parent.Id.ToString(),
                        Type = "General",
                        Title = "Daycare Notification",
                        Message = notificationMessages[new Random().Next(notificationMessages.Length)],
                        IsRead = new Random().Next(100) < 60, // 60% read rate
                        CreatedAt = DateTime.Now.AddDays(-new Random().Next(1, 15))
                    });
                }
            }

            await _context.Notifications.AddRangeAsync(notifications);
            await _context.SaveChangesAsync();

            Console.WriteLine($"{notifications.Count} notifications seeded.");
        }

        private List<string> GetSampleProfilePictures()
        {
            // Simple colored circle SVGs as Base64 for parent profile pictures
            var colors = new[] { "#FF6B6B", "#4ECDC4", "#45B7D1", "#FFA07A", "#98D8C8", "#F7DC6F", "#BB8FCE", "#85C1E2", "#F8B739", "#52B788" };
            var pictures = new List<string>();

            foreach (var color in colors)
            {
                var svg = $@"<svg width=""200"" height=""200"" xmlns=""http://www.w3.org/2000/svg"">
  <circle cx=""100"" cy=""100"" r=""80"" fill=""{color}""/>
  <circle cx=""100"" cy=""80"" r=""30"" fill=""white"" opacity=""0.8""/>
  <ellipse cx=""100"" cy=""140"" rx=""50"" ry=""40"" fill=""white"" opacity=""0.8""/>
</svg>";
                var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
                pictures.Add($"data:image/svg+xml;base64,{base64}");
            }

            return pictures;
        }

        private List<string> GetSampleChildProfilePictures()
        {
            // Simple colored circle SVGs with different colors for children
            var colors = new[] { "#FFB6C1", "#87CEEB", "#98FB98", "#DDA0DD", "#F0E68C", "#FFE4B5", "#B0E0E6", "#FFDAB9", "#E0BBE4", "#FFDFD3", "#C7CEEA", "#B4F8C8" };
            var pictures = new List<string>();

            foreach (var color in colors)
            {
                var svg = $@"<svg width=""200"" height=""200"" xmlns=""http://www.w3.org/2000/svg"">
  <circle cx=""100"" cy=""100"" r=""80"" fill=""{color}""/>
  <circle cx=""100"" cy=""85"" r=""25"" fill=""white"" opacity=""0.9""/>
  <ellipse cx=""100"" cy=""135"" rx=""45"" ry=""35"" fill=""white"" opacity=""0.9""/>
  <circle cx=""85"" cy=""80"" r=""5"" fill=""#333""/>
  <circle cx=""115"" cy=""80"" r=""5"" fill=""#333""/>
  <path d=""M 85 95 Q 100 105 115 95"" stroke=""#333"" stroke-width=""2"" fill=""none""/>
</svg>";
                var base64 = Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(svg));
                pictures.Add($"data:image/svg+xml;base64,{base64}");
            }

            return pictures;
        }
    }
}