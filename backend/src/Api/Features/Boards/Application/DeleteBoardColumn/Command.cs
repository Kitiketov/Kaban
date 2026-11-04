using MediatR;

namespace Kaban.Api.Features.Boards.Application.DeleteBoardColumn;

public sealed record DeleteBoardColumnCommand(int ColumnId) : IRequest<bool>;