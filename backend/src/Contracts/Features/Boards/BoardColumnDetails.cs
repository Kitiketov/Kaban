using Contracts.Features.Tasks.Common;

namespace Contracts.Features.Boards;

public sealed record BoardColumnDetails(int Id, string Name, int Order, List<TaskSuccess> Tasks);