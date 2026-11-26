using Contracts.Features.Tasks.Common;

namespace Contracts.Features.Boards.Get;

public sealed record GetBoardSuccess(int Id, int ProjectId, string Name, IReadOnlyList<BoardColumnDetails> Columns);

public sealed record BoardColumnDetails(int Id, string Name, int Order, List<TaskSuccess> Tasks);
