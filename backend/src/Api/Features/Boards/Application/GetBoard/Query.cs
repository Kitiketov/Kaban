using Kaban.Api.Features.Boards.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.GetBoard;

public sealed record GetBoardQuery(int BoardId) : IRequest<BoardDetailsDto>;