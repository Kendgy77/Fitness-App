namespace FitnessApp.Api.Common.Errors;

/// <summary>
/// A business rule was broken by the request (400). The message is shown to the user as it is.
/// When <paramref name="field"/> is set (the JSON name, e.g. "birthDate"), the response carries it in
/// <c>errors</c> like attribute validation does, so the UI can show the message next to that input.
/// </summary>
public sealed class DomainValidationException(string message, string? field = null) : Exception(message)
{
    public string? Field { get; } = field;
}

/// <summary>The request conflicts with the current state, e.g. editing an archived plan (409).</summary>
public sealed class ConflictException(string message) : Exception(message);
