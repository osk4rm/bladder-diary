namespace SwarmApp.Api.Data;

/// <summary>A child whose diary is kept by a household.</summary>
public sealed class Child
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid HouseholdId { get; set; }

    public Household? Household { get; set; }

    public required string Name { get; set; }

    /// <summary>Nullable because a household may not know or choose to record it.</summary>
    public DateOnly? BirthDate { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
