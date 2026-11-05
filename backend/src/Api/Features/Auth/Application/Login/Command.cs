using Kaban.Api.Features.Auth.Application.DTO;
using MediatR;
namespace Kaban.Api.Features.Auth.Application.Login;
public sealed record LoginCommand(string Email, string Password) : IRequest<TokenPairDto>;

