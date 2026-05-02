using FitTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<Transaction> Transactions => Set<Transaction>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<Wallet> Wallets => Set<Wallet>();
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();
    public DbSet<CheckInLog> CheckInLogs => Set<CheckInLog>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        builder.Entity<Membership>()
            .HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Membership>()
            .Property(m => m.MonthlyFee)
            .HasPrecision(18, 2);

        builder.Entity<Wallet>()
            .HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Wallet>()
            .HasIndex(w => w.UserId)
            .IsUnique();

        builder.Entity<Wallet>()
            .Property(w => w.Balance)
            .HasPrecision(18, 2);

        builder.Entity<WalletTransaction>()
            .HasOne(wt => wt.Wallet)
            .WithMany(w => w.WalletTransactions)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<WalletTransaction>()
            .Property(wt => wt.Amount)
            .HasPrecision(18, 2);

        builder.Entity<PaymentAccount>()
            .HasIndex(p => p.PaymentType);

        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.QrCodeToken)
            .IsUnique();

        builder.Entity<CheckInLog>()
            .HasOne(c => c.Member)
            .WithMany(u => u.CheckInLogs)
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<CheckInLog>()
            .HasIndex(c => c.MemberId);

        builder.Entity<CheckInLog>()
            .HasIndex(c => c.CheckInTime);
    }
}
