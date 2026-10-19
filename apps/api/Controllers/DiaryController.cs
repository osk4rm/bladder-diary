using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SwarmApp.Api.Contracts;
using SwarmApp.Api.Data;
using SwarmApp.Api.Infrastructure.Diary;

namespace SwarmApp.Api.Controllers;

/// <summary>
/// Diary endpoints. Every read and write is scoped to the caller's household.
/// </summary>
[ApiController]
[Route("api/diary")]
[Authorize]
public sealed class DiaryController(DiaryService diary) : ControllerBase
{
    [HttpGet("children")]
    public async Task<ActionResult<IReadOnlyList<ChildResponse>>> Children(
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var household = await CallerHouseholdAsync(cancellationToken);
        if (household is null) return NoHousehold();

        var pageSize = Math.Clamp(take ?? DiaryService.DefaultTake, 1, DiaryService.MaxTake);
        var children = await diary.ListChildrenAsync(household.Id, pageSize, cancellationToken);
        return Ok(children.Select(ToResponse).ToArray());
    }

    [HttpPost("children")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<ChildResponse>> CreateChild(
        CreateChildRequest request,
        CancellationToken cancellationToken)
    {
        var household = await CallerHouseholdAsync(cancellationToken);
        if (household is null) return NoHousehold();

        var child = await diary.CreateChildAsync(household.Id, request.Name, request.BirthDate, cancellationToken);
        return Created($"/api/diary/children/{child.Id}", ToResponse(child));
    }

    [HttpGet("entries")]
    public async Task<ActionResult<IReadOnlyList<EntryResponse>>> Entries(
        [FromQuery] Guid? childId,
        [FromQuery] int? take,
        CancellationToken cancellationToken)
    {
        var household = await CallerHouseholdAsync(cancellationToken);
        if (household is null) return NoHousehold();

        var pageSize = Math.Clamp(take ?? DiaryService.DefaultTake, 1, DiaryService.MaxTake);
        var entries = await diary.ListEntriesAsync(household.Id, childId, pageSize, cancellationToken);
        return Ok(entries.Select(ToResponse).ToArray());
    }

    [HttpPost("entries")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<EntryResponse>> CreateEntry(
        CreateEntryRequest request,
        CancellationToken cancellationToken)
    {
        var household = await CallerHouseholdAsync(cancellationToken);
        if (household is null) return NoHousehold();

        var entry = await diary.CreateEntryAsync(
            household.Id,
            request.ChildId,
            request.Kind,
            request.OccurredAt,
            request.Notes,
            cancellationToken);

        // A child from another household is answered exactly like one that does not exist:
        // a 403 here would confirm the id is real.
        if (entry is null) return NotFound(new { error = "child_not_found" });

        return Created($"/api/diary/entries/{entry.Id}", ToResponse(entry));
    }

    private async Task<Household?> CallerHouseholdAsync(CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId))
        {
            return null;
        }

        return await diary.FindHouseholdAsync(userId, cancellationToken);
    }

    private NotFoundObjectResult NoHousehold() => NotFound(new { error = "no_household" });

    private static ChildResponse ToResponse(Child child) =>
        new(child.Id, child.Name, child.BirthDate, child.CreatedAt);

    private static EntryResponse ToResponse(Entry entry) =>
        new(entry.Id, entry.ChildId, entry.Kind, entry.OccurredAt, entry.Notes, entry.CreatedAt);
}
