using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using CryptoWallet.Models;

namespace CryptoWallet.Data
{
    public class WalletDbContext : DbContext
    {
        public WalletDbContext(DbContextOptions<WalletDbContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            base.OnConfiguring(optionsBuilder);
            
            // Configure warnings for in-memory database
            optionsBuilder.ConfigureWarnings(warnings =>
                warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Wallet> Wallets { get; set; }
        public DbSet<Transaction> Transactions { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
                entity.Property(e => e.CreatedAt).IsRequired();
                
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Wallet configuration
            modelBuilder.Entity<Wallet>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.UserId).IsRequired();
                entity.Property(e => e.Balance).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.CreatedAt).IsRequired();

                entity.HasOne<User>()
                    .WithMany()
                    .HasForeignKey(e => e.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasMany(e => e.Transactions)
                    .WithOne()
                    .HasForeignKey(t => t.WalletId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            // Transaction configuration
            modelBuilder.Entity<Transaction>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Id).IsRequired();
                entity.Property(e => e.WalletId).IsRequired();
                entity.Property(e => e.Type).IsRequired();
                entity.Property(e => e.Amount).IsRequired().HasColumnType("decimal(18,2)");
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Timestamp).IsRequired();
                entity.Property(e => e.RelatedUserId).HasMaxLength(50);
            });
        }
    }
}
