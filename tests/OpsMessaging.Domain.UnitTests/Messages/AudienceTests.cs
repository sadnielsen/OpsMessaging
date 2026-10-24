using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class AudienceTests
{
    [Fact]
    public void Create_WithOneRoleGroup_PassesBR02()
    {
        var result = Audience.Create(["Driver"], "REGION-NORTH");

        result.ShouldSucceed();
        result.Value.RoleGroups.ShouldBe(["Driver"]);
        result.Value.RegionCode.ShouldBe("REGION-NORTH");
    }

    [Fact]
    public void Create_WithoutRoleGroups_FailsWithBR02()
    {
        var result = Audience.Create([], "REGION-NORTH");

        result.ShouldFailWith("BR-02");
    }

    [Fact]
    public void Create_WithNullRoleGroups_FailsWithBR02()
    {
        var result = Audience.Create(null, null);

        result.ShouldFailWith("BR-02");
    }

    [Fact]
    public void Create_WithBlankRoleGroup_FailsWithValidationError()
    {
        var result = Audience.Create(["Driver", " "], null);

        result.ShouldFailWith(Error.ValidationCode);
    }

    [Fact]
    public void Create_WithBlankRegion_HasNoRegion()
    {
        var result = Audience.Create(["Driver"], "  ");

        result.Value.RegionCode.ShouldBeNull();
    }

    [Fact]
    public void Equals_SameGroupsInDifferentOrderAndCase_AreEqual()
    {
        // CONCEPT (DDD): Value objects are equal when their values are equal, not when they are the
        // same instance in memory.
        var first = Audience.Create(["Driver", "Conductor"], "REGION-NORTH").Value;
        var second = Audience.Create(["conductor", "Driver", "Driver"], "region-north").Value;

        first.ShouldBe(second);
        first.GetHashCode().ShouldBe(second.GetHashCode());
    }

    [Fact]
    public void Equals_DifferentRegion_AreNotEqual()
    {
        var north = Audience.Create(["Driver"], "REGION-NORTH").Value;
        var south = Audience.Create(["Driver"], "REGION-SOUTH").Value;

        north.ShouldNotBe(south);
    }
}
