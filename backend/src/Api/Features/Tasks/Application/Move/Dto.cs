namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed record TaskMoveRequestDto(
    int ColumnId,
    int Order,
    int? PrevTaskId,                                                                                                                                                          
    int? NextTaskId 
);