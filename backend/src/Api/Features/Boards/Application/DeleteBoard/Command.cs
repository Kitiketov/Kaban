using MediatR;

namespace Kaban.Api.Features.Boards.Application.DeleteBoard;

public sealed record DeleteBoardCommand(int BoardId) : IRequest<bool>;