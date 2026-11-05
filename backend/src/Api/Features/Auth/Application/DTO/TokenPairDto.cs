namespace Kaban.Api.Features.Auth.Application.DTO;

public sealed record TokenPairDto(string AccessToken, string RefreshToken);

