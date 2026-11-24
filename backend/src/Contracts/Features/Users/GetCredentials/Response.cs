namespace Contracts.Features.Users.GetCredentials;

// Internal credential exchange only: never serialize into HTTP or logs.
public sealed record GetCredentialsSuccess(int UserId, string PasswordHash);
