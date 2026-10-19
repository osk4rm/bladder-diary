using SwarmApp.Api.Data;

namespace SwarmApp.Api.Infrastructure.Diary;

/// <summary>
/// Creates the tenant row that a newly registered account records into.
/// </summary>
/// <remarks>
/// Deliberately does not save: the caller owns the transaction, so the user, their role and
/// the household either all exist or none of them do.
/// </remarks>
public sealed class HouseholdProvisioner(AppDbContext db)
{
    public const int MaxNameLength = 100;
    public const string DefaultName = "My household";

    public Household CreateFor(AppUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        var name = string.IsNullOrWhiteSpace(user.DisplayName)
            ? DefaultName
            : user.DisplayName.Trim();

        if (name.Length > MaxNameLength)
        {
            name = name[..MaxNameLength];
        }

        var household = new Household
        {
            OwnerUserId = user.Id,
            Name = name,
            CreatedAt = DateTimeOffset.UtcNow,
        };

        db.Households.Add(household);
        return household;
    }
}
