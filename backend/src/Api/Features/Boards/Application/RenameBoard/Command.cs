using Kaban.Api.Features.Boards.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.RenameBoard;

public sealed record RenameBoardCommand(int BoardId, string Name) : IRequest<BoardDto>;