namespace Contracts.Features.Users.Search;

public sealed record SearchUsersRequest(int ActorId, string Query, int Limit);
