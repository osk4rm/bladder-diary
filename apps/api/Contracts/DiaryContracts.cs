using System.ComponentModel.DataAnnotations;
using SwarmApp.Api.Data;

namespace SwarmApp.Api.Contracts;

public sealed class CreateChildRequest
{
    [Required, MaxLength(100)]
    public string Name { get; init; } = string.Empty;

    public DateOnly? BirthDate { get; init; }
}

public sealed class CreateEntryRequest
{
    public Guid ChildId { get; init; }

    public EntryKind Kind { get; init; }

    /// <summary>Defaults to now when omitted.</summary>
    public DateTimeOffset? OccurredAt { get; init; }

    [MaxLength(2000)]
    public string? Notes { get; init; }
}

public sealed record ChildResponse(
    Guid Id,
    string Name,
    DateOnly? BirthDate,
    DateTimeOffset CreatedAt);

public sealed record EntryResponse(
    Guid Id,
    Guid ChildId,
    EntryKind Kind,
    DateTimeOffset OccurredAt,
    string? Notes,
    DateTimeOffset CreatedAt);
