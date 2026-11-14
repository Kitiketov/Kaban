using Kaban.Api.Features.Tasks.Application.Common.Dto;

namespace Kaban.Api.Features.Boards.DTO;

public sealed record BoardColumnDetailsDto(int Id, string Name, int Order, TaskResponseDto[] Tasks);