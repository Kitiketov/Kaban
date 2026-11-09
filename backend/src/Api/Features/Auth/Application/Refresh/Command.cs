using Kaban.Api.Features.Auth.Application.DTO;
using MediatR;
namespace Kaban.Api.Features.Auth.Application.Refresh;
public sealed record RefreshCommand(int UserId) : IRequest<TokenPairDto>;

