namespace SwarmApp.Api.Data;

/// <summary>
/// The tenant boundary. Everything a household records hangs off this row.
/// </summary>
/// <remarks>
/// One household per owner for now, enforced by a unique index on <see cref="OwnerUserId"/>.
/// Keeping the owner on the household rather than on every child and entry means the caller's
/// tenant is resolved once per request and then used as the filter, instead of being inferred
/// from a user id spread across tables a new query can forget to join.
/// </remarks>
public sealed class Household
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid OwnerUserId { get; set; }

    public AppUser? Owner { get; set; }

    public required string Name { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
}
