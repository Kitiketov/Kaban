using Kaban.Api.Features.Boards.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.ListBoards;

public sealed record ListBoardsQuery(int ProjectId) : IRequest<BoardListDto>;