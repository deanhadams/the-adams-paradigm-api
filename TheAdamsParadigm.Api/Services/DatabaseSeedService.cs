using TheAdamsParadigm.Api.Data;
using TheAdamsParadigm.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace TheAdamsParadigm.Api.Services
{
    public class DatabaseSeedService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeedService> _logger;

        public DatabaseSeedService(ApplicationDbContext context, ILogger<DatabaseSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedDatabaseAsync()
        {
            try
            {
                // Ensure migrations are applied (preferred over EnsureCreated in apps using migrations)
                await _context.Database.MigrateAsync();

                // Seed services if they don't exist
                if (!await _context.Services.AnyAsync())
                {
                    _logger.LogInformation("Seeding services into the database...");
                    var services = GetServiceSeedData();
                    await _context.Services.AddRangeAsync(services);
                    await _context.SaveChangesAsync();
                    _logger.LogInformation($"Successfully seeded {services.Count} services");
                }
                else
                {
                    _logger.LogInformation("Services already exist in database, skipping seed");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while seeding the database");
                throw;
            }
        }

        private static List<Service> GetServiceSeedData()
        {
            return new List<Service>
            {
                new Service
                {
                    Icon = "Layers",
                    Title = "Full-Stack Web Development",
                    Description = "Modern responsive applications built around real business requirements.",
                    CostPerHour = 120.00m
                },
                new Service
                {
                    Icon = "Wrench",
                    Title = "Custom Software",
                    Description = "Purpose-built applications designed around a company's workflow.",
                    CostPerHour = 150.00m
                },
                new Service
                {
                    Icon = "Plug",
                    Title = "API Development",
                    Description = "Secure and maintainable APIs and backend systems.",
                    CostPerHour = 110.00m
                },
                new Service
                {
                    Icon = "Atom",
                    Title = "React Applications",
                    Description = "Fast, modern and interactive frontend experiences.",
                    CostPerHour = 100.00m
                },
                new Service
                {
                    Icon = "Server",
                    Title = "ASP.NET / C# Development",
                    Description = "Robust backend systems using modern Microsoft technologies.",
                    CostPerHour = 130.00m
                },
                new Service
                {
                    Icon = "Database",
                    Title = "Database Solutions",
                    Description = "SQL Server and application data architecture.",
                    CostPerHour = 140.00m
                },
                new Service
                {
                    Icon = "CreditCard",
                    Title = "Payment Integrations",
                    Description = "Payment workflows and third-party payment integrations.",
                    CostPerHour = 125.00m
                },
                new Service
                {
                    Icon = "CalendarClock",
                    Title = "Booking & Scheduling",
                    Description = "Booking systems, availability logic, payments and confirmations.",
                    CostPerHour = 95.00m
                },
                new Service
                {
                    Icon = "Sparkles",
                    Title = "AI-Powered Applications",
                    Description = "Practical AI integrations and intelligent application features.",
                    CostPerHour = 160.00m
                },
                new Service
                {
                    Icon = "CloudCog",
                    Title = "Cloud & Deployment",
                    Description = "Taking applications from development into reliable production environments.",
                    CostPerHour = 135.00m
                }
            };
        }
    }
}
