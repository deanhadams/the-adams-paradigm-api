using Microsoft.EntityFrameworkCore;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Service> Services { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderId).ValueGeneratedNever();
                entity.Property(e => e.Amount).HasPrecision(18, 2);
                entity.Property(e => e.CreatedAt).HasColumnType("timestamp without time zone");
                entity.Property(e => e.PaidAt).HasColumnType("timestamp without time zone");
                
                entity.HasOne(e => e.Service)
                    .WithMany(s => s.Orders)
                    .HasForeignKey(e => e.ServiceId)
                    .IsRequired(false);

                entity.HasIndex(e => e.CheckoutId).HasDatabaseName("idx_orders_checkout_id");
                entity.HasIndex(e => e.Status).HasDatabaseName("idx_orders_status");
                entity.HasIndex(e => e.CreatedAt).HasDatabaseName("idx_orders_created_at");
            });

            // Configure Service entity
            modelBuilder.Entity<Service>(entity =>
            {
                entity.HasKey(e => e.ServiceId);
                entity.Property(e => e.ServiceId).UseIdentityColumn();
                entity.Property(e => e.CostPerHour).HasPrecision(18, 2);
            });
        }
    }
}
