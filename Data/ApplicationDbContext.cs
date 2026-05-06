// ============================================================
// ApplicationDbContext.cs — Entity Framework Core Database Context
// ============================================================
// This class is the bridge between the application and the PostgreSQL
// database. It inherits from IdentityDbContext<ApplicationUser>, which
// automatically adds ASP.NET Identity tables (AspNetUsers, AspNetRoles, etc.)
// to the database schema.
//
// Every DbSet<T> property below represents a table in the database.
// The OnModelCreating method configures relationships, indexes, and
// column precision using the Fluent API instead of data annotations.
// ============================================================

using FitTrack.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace FitTrack.Data;

/// <summary>
/// The main EF Core database context for FitTrack.
/// Inherits Identity tables from <see cref="IdentityDbContext{TUser}"/>
/// and adds FitTrack-specific tables via DbSet properties.
/// </summary>
public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    /// <summary>
    /// Initialises the context with the options (connection string, provider, etc.)
    /// provided via Dependency Injection in Program.cs.
    /// </summary>
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    // ── DbSet Tables ──────────────────────────────────────────────────────────
    // Each property maps to a table in the PostgreSQL database.
    // EF Core uses Set<T>() so the table is created lazily on first access.

    /// <summary>Membership payment transactions submitted by members.</summary>
    public DbSet<Transaction> Transactions => Set<Transaction>();

    /// <summary>Active, expired, or pending membership records for each user.</summary>
    public DbSet<Membership> Memberships => Set<Membership>();

    /// <summary>Digital wallets — one per user — that hold a peso balance.</summary>
    public DbSet<Wallet> Wallets => Set<Wallet>();

    /// <summary>Individual credit/debit movements recorded against a wallet.</summary>
    public DbSet<WalletTransaction> WalletTransactions => Set<WalletTransaction>();

    /// <summary>GCash / Maya / bank payment accounts managed by the admin for QR display.</summary>
    public DbSet<PaymentAccount> PaymentAccounts => Set<PaymentAccount>();

    /// <summary>Audit log of every QR code scan attempt at the gym entrance.</summary>
    public DbSet<CheckInLog> CheckInLogs => Set<CheckInLog>();

    // ── Model Configuration (Fluent API) ─────────────────────────────────────
    /// <summary>
    /// Configures table relationships, foreign key behaviours, column precision,
    /// and database indexes that cannot be expressed with data annotations alone.
    /// This method is called by EF Core before any migrations are applied.
    /// </summary>
    protected override void OnModelCreating(ModelBuilder builder)
    {
        // Always call base first so Identity tables are set up correctly.
        base.OnModelCreating(builder);

        // ── Transaction ───────────────────────────────────────────────────────
        // A Transaction belongs to one ApplicationUser.
        // When a user is deleted, all their transactions are deleted too (Cascade).
        builder.Entity<Transaction>()
            .HasOne(t => t.User)
            .WithMany(u => u.Transactions)
            .HasForeignKey(t => t.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Store monetary amounts with up to 18 digits total, 2 after the decimal (₱).
        builder.Entity<Transaction>()
            .Property(t => t.Amount)
            .HasPrecision(18, 2);

        // ── Membership ────────────────────────────────────────────────────────
        // A Membership belongs to one user; cascade delete removes memberships
        // when the user account is deleted.
        builder.Entity<Membership>()
            .HasOne(m => m.User)
            .WithMany(u => u.Memberships)
            .HasForeignKey(m => m.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Precision for the monthly fee column.
        builder.Entity<Membership>()
            .Property(m => m.MonthlyFee)
            .HasPrecision(18, 2);

        // ── Wallet ────────────────────────────────────────────────────────────
        // One-to-One relationship: each user has at most one wallet.
        builder.Entity<Wallet>()
            .HasOne(w => w.User)
            .WithOne(u => u.Wallet)
            .HasForeignKey<Wallet>(w => w.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Enforce uniqueness at the DB level — a user cannot have two wallets.
        builder.Entity<Wallet>()
            .HasIndex(w => w.UserId)
            .IsUnique();

        // Precision for the wallet balance column.
        builder.Entity<Wallet>()
            .Property(w => w.Balance)
            .HasPrecision(18, 2);

        // ── WalletTransaction ─────────────────────────────────────────────────
        // Each WalletTransaction is linked to exactly one Wallet.
        // Cascade delete removes wallet history when the wallet is deleted.
        builder.Entity<WalletTransaction>()
            .HasOne(wt => wt.Wallet)
            .WithMany(w => w.WalletTransactions)
            .HasForeignKey(wt => wt.WalletId)
            .OnDelete(DeleteBehavior.Cascade);

        // Precision for transaction amounts.
        builder.Entity<WalletTransaction>()
            .Property(wt => wt.Amount)
            .HasPrecision(18, 2);

        // ── PaymentAccount ────────────────────────────────────────────────────
        // Index on PaymentType to speed up "group by payment type" queries
        // used when listing accounts on the admin Payment Accounts page.
        builder.Entity<PaymentAccount>()
            .HasIndex(p => p.PaymentType);

        // ── ApplicationUser (QR Code Token) ───────────────────────────────────
        // QrCodeToken must be unique across all users — this ensures that
        // a scanned QR code can only ever match one member account.
        builder.Entity<ApplicationUser>()
            .HasIndex(u => u.QrCodeToken)
            .IsUnique();

        // ── CheckInLog ────────────────────────────────────────────────────────
        // Each log entry optionally references a member. Using SetNull means
        // the log row is preserved even if the member account is deleted —
        // important for historical audit trails.
        builder.Entity<CheckInLog>()
            .HasOne(c => c.Member)
            .WithMany(u => u.CheckInLogs)
            .HasForeignKey(c => c.MemberId)
            .OnDelete(DeleteBehavior.SetNull);

        // Index on MemberId speeds up "show all check-ins for member X" queries.
        builder.Entity<CheckInLog>()
            .HasIndex(c => c.MemberId);

        // Index on CheckInTime speeds up ordering and recent-scan lookups.
        builder.Entity<CheckInLog>()
            .HasIndex(c => c.CheckInTime);
    }
}
