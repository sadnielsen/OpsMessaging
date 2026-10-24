using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

// CONCEPT (Unit test): Each business rule has a positive and a negative test, and the test name
// carries the rule code. Searching for "BR01" finds the rule in code AND its proof in tests.
public sealed class MessageTitleTests
{
    [Fact]
    public void Create_TitleOfMaximumLength_PassesBR01()
    {
        var result = MessageTitle.Create(new string('a', MessageTitle.MaxLength));

        result.ShouldSucceed();
    }

    [Fact]
    public void Create_TitleWithSurroundingSpaces_IsTrimmed()
    {
        var result = MessageTitle.Create("  Shift change  ");

        result.Value.Value.ShouldBe("Shift change");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyTitle_FailsWithBR01(string? title)
    {
        var result = MessageTitle.Create(title);

        result.ShouldFailWith("BR-01");
        result.Error!.Field.ShouldBe("title");
    }

    [Fact]
    public void Create_TitleLongerThanMaximum_FailsWithBR01()
    {
        var result = MessageTitle.Create(new string('a', MessageTitle.MaxLength + 1));

        result.ShouldFailWith("BR-01");
    }

    [Fact]
    public void ToString_Always_HidesTheContent()
    {
        // Privacy-by-design: an accidental "{title}" in a log template must not leak content.
        var title = TestData.Title("Secret content");

        title.ToString().ShouldNotContain("Secret");
    }
}
