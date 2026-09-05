using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using TheAdamsParadigm.Api.Models;

namespace TheAdamsParadigm.Api.Data
{
    // IDataProtectionKeyContext lets ASP.NET Core's Data Protection API persist its key
    // ring to this same Postgres database (via the DataProtectionKeys table below) instead
    // of local disk, which would be wiped on every Railway redeploy — losing the keys would
    // permanently break decryption of anything already encrypted (e.g. Client.ICloudPassword).
    public class ApplicationDbContext : DbContext, IDataProtectionKeyContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Order> Orders { get; set; }
        public DbSet<Service> Services { get; set; }
        public DbSet<UserMemory> UserMemories { get; set; }
        public DbSet<KnowledgeChunk> KnowledgeChunks { get; set; }
        public DbSet<Client> Clients { get; set; }
        public DbSet<DataProtectionKey> DataProtectionKeys { get; set; }

        // Npgsql rejects Kind=Utc DateTimes against "timestamp without time zone" columns;
        // strip the Kind on write and re-tag reads as UTC since that's what we always store.
        private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter = new(
            v => DateTime.SpecifyKind(v, DateTimeKind.Unspecified),
            v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter = new(
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Unspecified) : v,
            v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : v);

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasPostgresExtension("vector");

            // Map entities to specific table names used in SQL script
            modelBuilder.Entity<Order>().ToTable("orders");
            modelBuilder.Entity<Service>().ToTable("services");
            modelBuilder.Entity<UserMemory>().ToTable("user_memories");
            modelBuilder.Entity<KnowledgeChunk>().ToTable("knowledge_chunks");
            modelBuilder.Entity<Client>().ToTable("clients");

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
                entity.Property(e => e.PaymentLink).HasColumnName("payment_link");
                entity.Property(e => e.Name).HasColumnName("name");
                entity.Property(e => e.Surname).HasColumnName("surname");
                entity.Property(e => e.Email).HasColumnName("email");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone")
                    .HasConversion(UtcDateTimeConverter);
                entity.Property(e => e.PaidAt).HasColumnName("paid_at").HasColumnType("timestamp without time zone")
                    .HasConversion(UtcNullableDateTimeConverter);
                
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
                entity.Property(e => e.SetupFee).HasColumnName("setup_fee").HasPrecision(18, 2);
                entity.Property(e => e.IsBookable).HasColumnName("is_bookable");
            });

            // Configure UserMemory entity
            modelBuilder.Entity<UserMemory>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
                entity.Property(e => e.ChatUserId).HasColumnName("chat_user_id").IsRequired();
                entity.Property(e => e.Category).HasColumnName("category").IsRequired();
                entity.Property(e => e.Text).HasColumnName("text").IsRequired();
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone")
                    .HasConversion(UtcDateTimeConverter);
                entity.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("timestamp without time zone")
                    .HasConversion(UtcDateTimeConverter);

                entity.HasIndex(e => e.ChatUserId).HasDatabaseName("idx_user_memories_chat_user_id");
            });

            // Configure KnowledgeChunk entity
            modelBuilder.Entity<KnowledgeChunk>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).HasColumnName("id").UseIdentityColumn();
                entity.Property(e => e.Section).HasColumnName("section").IsRequired();
                entity.Property(e => e.Content).HasColumnName("content").IsRequired();
                entity.Property(e => e.Embedding).HasColumnName("embedding").HasColumnType("vector(1024)");
                entity.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("timestamp without time zone")
                    .HasConversion(UtcDateTimeConverter);
            });

            // Configure Client entity
            modelBuilder.Entity<Client>(entity =>
            {
                entity.HasKey(e => e.ClientId);
                entity.Property(e => e.ClientId).HasColumnName("client_id").UseIdentityColumn();
                entity.Property(e => e.Name).HasColumnName("name").IsRequired();
                entity.Property(e => e.Website).HasColumnName("website");
                entity.Property(e => e.Email).HasColumnName("email").IsRequired();
                entity.Property(e => e.ICloudEmail).HasColumnName("icloud_email");
                entity.Property(e => e.ICloudPassword).HasColumnName("icloud_password");
                entity.Property(e => e.ClientApiKey).HasColumnName("client_api_key").IsRequired();

                entity.HasIndex(e => e.ClientApiKey).IsUnique().HasDatabaseName("idx_clients_client_api_key");
            });
        }
    }
}
