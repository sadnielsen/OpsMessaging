namespace OpsMessaging.Domain.Common;

/// <summary>
/// Describes an expected failure, such as a broken business rule.
/// </summary>
/// <remarks>
/// The <see cref="Code"/> is stable and machine-readable (for example <c>BR-03</c>): it shows up in
/// tests, in HTTP responses and in the Gherkin scenarios. The Api layer maps codes to HTTP status codes.
/// </remarks>
/// <param name="Code">Stable error code, for example <c>BR-03</c>.</param>
/// <param name="Description">Human-readable explanation, safe to show to the caller.</param>
/// <param name="Field">Optional name of the input field the error refers to.</param>
[System.Diagnostics.CodeAnalysis.SuppressMessage(
    "Naming", "CA1716:Identifiers should not match keywords",
    Justification = "'Error' is only a keyword in Visual Basic; this C#-only codebase uses Result/Error by convention.")]
public sealed record Error(string Code, string Description, string? Field = null)
{
    /// <summary>Code for input errors that are not covered by a numbered business rule.</summary>
    public const string ValidationCode = "VALIDATION";

    /// <summary>Creates an input error that is not tied to a numbered business rule.</summary>
    public static Error Validation(string field, string description) =>
        new(ValidationCode, description, field);
}
