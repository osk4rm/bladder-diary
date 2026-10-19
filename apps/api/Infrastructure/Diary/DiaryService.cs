using Microsoft.EntityFrameworkCore;
using SwarmApp.Api.Data;

namespace SwarmApp.Api.Infrastructure.Diary;

/// <summary>
/// Reads and writes diary rows for one household at a time.
/// </summary>
/// <remarks>
/// Every operation takes a <c>householdId</c> resolved from the authenticated caller and puts
/// it in the query. There is no method that looks an entry up by an id from the request, and
/// the only client-supplied id — the child on a new entry — is checked against the household
/// before it is used. A foreign id is indistinguishable from a missing one.
/// </remarks>
public sealed class DiaryService(AppDbContext db)
{
    public const int DefaultTake = 50;
    public const int MaxTake = 200;

    public Task<Household?> FindHouseholdAsync(Guid ownerUserId, CancellationToken cancellationToken = default) =>
        db.Households
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.OwnerUserId == ownerUserId, cancellationToken);

    /// <summary>Oldest child first; children with an unknown birth date follow, by creation.</summary>
    public async Task<IReadOnlyList<Child>> ListChildrenAsync(
        Guid householdId,
        int take,
        CancellationToken cancellationToken = default) =>
        await db.Children
            .AsNoTracking()
            .Where(c => c.HouseholdId == householdId)
            .OrderBy(c => c.BirthDate == null)
            .ThenBy(c => c.BirthDate)
            .ThenBy(c => c.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);

    public async Task<Child> CreateChildAsync(
        Guid householdId,
        string name,
        DateOnly? birthDate,
        CancellationToken cancellationToken = default)
    {
        var child = new Child
        {
            HouseholdId = householdId,
            Name = name,
            BirthDate = birthDate,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Children.Add(child);
        await db.SaveChangesAsync(cancellationToken);
        return child;
    }

    public async Task<IReadOnlyList<Entry>> ListEntriesAsync(
        Guid householdId,
        Guid? childId,
        int take,
        CancellationToken cancellationToken = default)
    {
        var query = db.Entries
            .AsNoTracking()
            .Where(e => e.HouseholdId == householdId);

        if (childId is not null)
        {
            // Still filtered by household: a foreign child id simply matches no rows.
            query = query.Where(e => e.ChildId == childId);
        }

        return await query
            .OrderByDescending(e => e.OccurredAt)
            .ThenByDescending(e => e.CreatedAt)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <summary>Returns null when the child is not in the caller's household.</summary>
    public async Task<Entry?> CreateEntryAsync(
        Guid householdId,
        Guid childId,
        EntryKind kind,
        DateTimeOffset? occurredAt,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var belongs = await db.Children
            .AnyAsync(c => c.Id == childId && c.HouseholdId == householdId, cancellationToken);

        if (!belongs)
        {
            return null;
        }

        var entry = new Entry
        {
            HouseholdId = householdId,
            ChildId = childId,
            Kind = kind,
            OccurredAt = occurredAt ?? DateTimeOffset.UtcNow,
            Notes = notes,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Entries.Add(entry);
        await db.SaveChangesAsync(cancellationToken);
        return entry;
    }
}
