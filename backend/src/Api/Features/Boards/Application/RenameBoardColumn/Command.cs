using Kaban.Api.Features.Boards.Application.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.RenameBoardColumn;

public sealed record RenameBoardColumnCommand(int ColumnId, string Name) : IRequest<BoardColumnDto>;
