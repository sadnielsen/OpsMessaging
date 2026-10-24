using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.Messages;

/// <summary>
/// Who a message is meant for: one or more role groups (for example <c>Driver</c>, <c>Conductor</c>),
/// optionally limited to one region.
/// </summary>
/// <remarks>
/// CONCEPT (DDD): The audience is a description, not a list of people. Turning it into concrete
/// recipients is a separate step (the IRecipientDirectory port in Application), because that needs
/// data from another system.
/// </remarks>
public sealed record Audience
{
    private Audience(IReadOnlyList<string> roleGroups, string? regionCode)
    {
        RoleGroups = roleGroups;
        RegionCode = regionCode;
    }

    /// <summary>The role groups, without duplicates and sorted, so equal audiences look the same.</summary>
    public IReadOnlyList<string> RoleGroups { get; }

    /// <summary>Optional region, for example <c>REGION-NORTH</c>; <c>null</c> means all regions.</summary>
    public string? RegionCode { get; }

    /// <summary>Validates and creates an audience.</summary>
    public static Result<Audience> Create(IEnumerable<string>? roleGroups, string? regionCode)
    {
        var groups = (roleGroups ?? []).ToList();

        // BR-02: the audience contains at least one role group.
        if (groups.Count == 0)
        {
            return Result.Failure<Audience>(MessageErrors.AudienceWithoutRoleGroup);
        }

        if (groups.Any(string.IsNullOrWhiteSpace))
        {
            return Result.Failure<Audience>(
                Error.Validation("audience.roleGroups", "Role group names must not be empty."));
        }

        var normalized = groups
            .Select(group => group.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var region = string.IsNullOrWhiteSpace(regionCode) ? null : regionCode.Trim();

        return Result.Success(new Audience(normalized, region));
    }

    // A record compares a list property by reference. A value object must compare by value,
    // so equality is written out explicitly here.
    /// <summary>Two audiences are equal when they have the same role groups and region.</summary>
    public bool Equals(Audience? other) =>
        other is not null
        && RoleGroups.SequenceEqual(other.RoleGroups, StringComparer.OrdinalIgnoreCase)
        && string.Equals(RegionCode, other.RegionCode, StringComparison.OrdinalIgnoreCase);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        foreach (var group in RoleGroups)
        {
            hash.Add(group, StringComparer.OrdinalIgnoreCase);
        }

        hash.Add(RegionCode, StringComparer.OrdinalIgnoreCase);
        return hash.ToHashCode();
    }
}
