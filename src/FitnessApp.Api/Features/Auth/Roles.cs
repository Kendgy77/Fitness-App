namespace FitnessApp.Api.Features.Auth;

/// <summary>Role names. Use these constants instead of strings in [Authorize(Roles = ...)].</summary>
public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";
}
