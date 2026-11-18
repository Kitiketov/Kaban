namespace Contracts.Features.Users;

public sealed record UserResult(int Id, string Name, string Email);
public sealed record UserFailure(string Code);
public sealed record GetUserRequested(int UserId);
public sealed record SearchUsersRequested(int ActorId, string Query, int Limit);
public sealed record UsersFound(UserResult[] Users);

// Internal credential exchange only: never serialize these replies into HTTP or logs.
public sealed record GetCredentialsRequested(string Email);
public sealed record CredentialsFound(int UserId, string PasswordHash);
public sealed record RegisterUserRequested(
    string Email, string Name, string PasswordHash);
public sealed record UserRegistered(int UserId);
