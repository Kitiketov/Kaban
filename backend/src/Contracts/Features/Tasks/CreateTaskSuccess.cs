namespace Contracts.Features.Tasks;

public record CreateTaskSuccess
{
    public required int Id { get; init; }
    public required string Name { get; init; }
    public string? Description { get; init; }
    public required int Priority { get; init; }
    public DateTime? Deadline { get; init; }
    public required int Order { get; init; }
    public required int ColumnId { get; init; }
}

public sealed record CreateTaskColumnNotFound(                                                                                                                                
    int BoardId,                                                                                                                                                              
    int ColumnId,                                                                                                                                                             
    string Message = "Указанная колонка не существует или не принадлежит данной доске.");                                                                                     
                                                                                                                                                                                  
public sealed record CreateTaskUserNotFound(                                                                                                                                  
    int UserId,                                                                                                                                                               
    string Message = "Пользователь с указанным идентификатором не найден.");  