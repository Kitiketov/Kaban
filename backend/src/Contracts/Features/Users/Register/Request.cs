namespace Contracts.Features.Users.Register;

// Internal credential exchange only: never serialize into HTTP or logs.
public sealed record RegisterUserRequest(string Email, string Name, string PasswordHash);
