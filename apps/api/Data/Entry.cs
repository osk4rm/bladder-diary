namespace SwarmApp.Api.Data;

/// <summary>A single diary entry.</summary>
/// <remarks>
/// <see cref="HouseholdId"/> is denormalised on purpose: every read is "entries for my
/// household", and carrying the tenant on the row makes that one indexed predicate instead
/// of a join a later query can forget.
/// </remarks>
public sealed class Entry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public Guid ChildId { get; set; }

    public Child? Child { get; set; }

    public EntryKind Kind { get; set; }

    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;

    public string? Notes { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
