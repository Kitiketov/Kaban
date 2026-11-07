namespace Contracts.Features.Tasks.Common;

public sealed record TaskNotFound(
    int TaskId,
    string Message = "Задача с указанным идентификатором не найдена.");

public sealed record CreateTaskColumnNotFound(
    int BoardId,
    int ColumnId,
    string Message = "Указанная колонка не существует или не принадлежит данной доске.");

public sealed record CreateTaskUserNotFound(
    int UserId,
    string Message = "Пользователь с указанным идентификатором не найден.");

public sealed record CreateUserNotFound(
    int UserId,
    string Message = "Пользователь с указанным идентификатором не найден.");
