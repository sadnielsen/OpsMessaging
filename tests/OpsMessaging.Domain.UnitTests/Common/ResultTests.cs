using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.UnitTests.Common;

public sealed class ResultTests
{
    [Fact]
    public void Success_WithValue_ExposesValue()
    {
        var result = Result.Success(42);

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBeNull();
        result.Value.ShouldBe(42);
    }

    [Fact]
    public void Failure_WithError_ExposesError()
    {
        var error = new Error("BR-99", "Something is wrong.");

        var result = Result.Failure<int>(error);

        result.IsFailure.ShouldBeTrue();
        result.Error.ShouldBe(error);
    }

    [Fact]
    public void Value_OnFailedResult_Throws()
    {
        var result = Result.Failure<int>(new Error("BR-99", "Something is wrong."));

        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
