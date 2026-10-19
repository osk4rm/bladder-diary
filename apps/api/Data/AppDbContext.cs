using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace SwarmApp.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options)
    : IdentityDbContext<AppUser, IdentityRole<Guid>, Guid>(options)
{
    public const string Schema = "app";

    public DbSet<Subscription> Subscriptions => Set<Subscription>();

    public DbSet<Household> Households => Set<Household>();

    public DbSet<Child> Children => Set<Child>();

    public DbSet<Entry> Entries => Set<Entry>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);

        builder.Entity<Subscription>(entity =>
        {
            // One row per provider subscription: a redelivered webhook updates it rather
            // than creating a second, which is the whole idempotency story in one index.
            entity.HasIndex(s => s.ProviderSubscriptionId).IsUnique();
            entity.HasIndex(s => s.UserId);
            entity.HasOne(s => s.User)
                .WithMany()
                .HasForeignKey(s => s.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Household>(entity =>
        {
            entity.Property(h => h.Name).HasMaxLength(100).IsRequired();
            // One household per owner for now. The unique index is the rule, not a
            // convention: a second row for the same owner is a database error.
            entity.HasIndex(h => h.OwnerUserId).IsUnique();
            entity.HasOne(h => h.Owner)
                .WithMany()
                .HasForeignKey(h => h.OwnerUserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Child>(entity =>
        {
            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.HasIndex(c => c.HouseholdId);
            entity.HasOne(c => c.Household)
                .WithMany()
                .HasForeignKey(c => c.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.Entity<Entry>(entity =>
        {
            entity.Property(e => e.Notes).HasMaxLength(2000);
            entity.HasIndex(e => new { e.HouseholdId, e.OccurredAt });
            entity.HasIndex(e => new { e.ChildId, e.OccurredAt });
            entity.HasOne(e => e.Household)
                .WithMany()
                .HasForeignKey(e => e.HouseholdId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Child)
                .WithMany()
                .HasForeignKey(e => e.ChildId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        builder.HasDefaultSchema(Schema);

        builder.Entity<AppUser>(user =>
        {
            user.Property(entity => entity.DisplayName).HasMaxLength(200);
        });
    }
}
