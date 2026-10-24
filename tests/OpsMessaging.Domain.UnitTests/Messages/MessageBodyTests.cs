using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class MessageBodyTests
{
    [Fact]
    public void Create_BodyOfMaximumLength_PassesBR01()
    {
        var result = MessageBody.Create(new string('a', MessageBody.MaxLength));

        result.ShouldSucceed();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_EmptyBody_FailsWithBR01(string? body)
    {
        var result = MessageBody.Create(body);

        result.ShouldFailWith("BR-01");
        result.Error!.Field.ShouldBe("body");
    }

    [Fact]
    public void Create_BodyLongerThanMaximum_FailsWithBR01()
    {
        var result = MessageBody.Create(new string('a', MessageBody.MaxLength + 1));

        result.ShouldFailWith("BR-01");
    }

    [Fact]
    public void ToString_Always_HidesTheContent()
    {
        var body = TestData.Body("Secret content");

        body.ToString().ShouldNotContain("Secret");
    }
}
