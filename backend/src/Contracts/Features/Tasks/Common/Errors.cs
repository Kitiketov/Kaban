namespace Contracts.Features.Tasks.Common;

public interface ITaskFailed;

public sealed record TaskNotFound(
    int TaskId,
    string Message = "Задача с указанным идентификатором не найдена") : ITaskFailed;

public sealed record TaskColumnNotFound(
    int BoardId,
    int ColumnId,
    string Message = "Указанная колонка не существует или не принадлежит данной доске") : ITaskFailed;

public sealed record TaskUserNotFound(
    int UserId,
    string Message = "Пользователь с указанным идентификатором не найден") : ITaskFailed;

