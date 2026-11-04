using Kaban.Api.Features.Boards.DTO;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.AddBoardColumn;

public sealed record AddBoardColumnCommand(int BoardId, string Name) : IRequest<BoardColumnDto>;