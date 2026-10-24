using OpsMessaging.Domain.Common;
using OpsMessaging.Domain.Messages;

namespace OpsMessaging.Domain.UnitTests.Messages;

public sealed class IdentifierTests
{
    [Fact]
    public void RecipientIdCreate_ValidId_Succeeds()
    {
        var result = RecipientId.Create(" EMP-0001 ");

        result.Value.Value.ShouldBe("EMP-0001");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("  ")]
    public void RecipientIdCreate_EmptyId_FailsWithValidationError(string? value)
    {
        RecipientId.Create(value).ShouldFailWith(Error.ValidationCode);
    }

    [Fact]
    public void RecipientIdCreate_TooLong_FailsWithValidationError()
    {
        RecipientId.Create(new string('x', RecipientId.MaxLength + 1)).ShouldFailWith(Error.ValidationCode);
    }

    [Fact]
    public void SenderIdCreate_NonEmptyGuid_Succeeds()
    {
        var oid = Guid.NewGuid();

        SenderId.Create(oid).Value.Value.ShouldBe(oid);
    }

    [Fact]
    public void SenderIdCreate_EmptyGuid_FailsWithValidationError()
    {
        SenderId.Create(Guid.Empty).ShouldFailWith(Error.ValidationCode);
    }
}
