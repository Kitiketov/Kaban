using Kaban.Api.Features.Tasks.Application.DTO;

namespace Kaban.Api.Features.Boards.DTO;

public sealed record BoardColumnDetailsDto(int Id, string Name, int Order, TaskDto[] Tasks);