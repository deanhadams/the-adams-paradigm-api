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

            // Map entities to specific table names used in SQL script
            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<Service>().ToTable("services");

            // Configure Order entity
            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(e => e.OrderId);
                entity.Property(e => e.OrderId).HasColumnName("order_id").ValueGeneratedNever();
                entity.Property(e => e.ServiceId).HasColumnName("service_id");
                entity.Property(e => e.Amount).HasColumnName("amount").HasPrecision(18, 2);
                entity.Property(e => e.Currency).HasColumnName("currency");
                entity.Property(e => e.Status).HasColumnName("status");
                entity.Property(e => e.CheckoutId).HasColumnName("checkout_id");
                entity.Property(e => e.PaymentId).HasColumnName("payment_id");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone");
                entity.Property(e => e.PaidAt).HasColumnName("paid_at").HasColumnType("timestamp without time zone");
                
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
                entity.Property(e => e.ServiceId).HasColumnName("service_id").UseIdentityColumn();
                entity.Property(e => e.Icon).HasColumnName("icon");
                entity.Property(e => e.Title).HasColumnName("title");
                entity.Property(e => e.Description).HasColumnName("description");
                entity.Property(e => e.CostPerHour).HasColumnName("cost_per_hour").HasPrecision(18, 2);
            });
        }
    }
}
