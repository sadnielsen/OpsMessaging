using OpsMessaging.Domain.Common;

namespace OpsMessaging.Domain.UnitTests;

/// <summary>Readable asserts for <see cref="Result"/>: <c>result.ShouldFailWith("BR-03")</c>.</summary>
internal static class ResultAssertions
{
    public static void ShouldSucceed(this Result result) =>
        result.IsSuccess.ShouldBeTrue($"Expected success, but failed with {result.Error?.Code}: {result.Error?.Description}");

    public static void ShouldFailWith(this Result result, string expectedCode)
    {
        result.IsFailure.ShouldBeTrue($"Expected failure with {expectedCode}, but the operation succeeded.");
        result.Error!.Code.ShouldBe(expectedCode);
    }
}
