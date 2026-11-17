namespace FitnessApp.Api.Common.Errors;

/// <summary>A business rule was broken by the request (400). The message is shown to the user, in Polish.</summary>
public sealed class DomainValidationException(string message) : Exception(message);

/// <summary>The request conflicts with the current state, e.g. editing an archived plan (409).</summary>
public sealed class ConflictException(string message) : Exception(message);
