using Kaban.Api.Features.Boards.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.CreateBoard;

public sealed record CreateBoardCommand(int ProjectId, string Name) : IRequest<BoardDto>;