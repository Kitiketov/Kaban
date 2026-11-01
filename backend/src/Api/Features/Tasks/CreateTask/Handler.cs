using Contracts.Tasks;
using Kaban.Api.Features.Tasks.Domain;
using Kaban.Api.Features.Tasks.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Tasks.CreateTask;

public sealed class CreateForecastHandler(
    IRequestClient<TaskCreateEvent> requestClient) : IRequestHandler<CreateTaskCommand, TaskCreateResponseDto?>
{
    public async Task<TaskCreateResponseDto?> Handle(CreateTaskCommand command, CancellationToken ct)
    {
        var boardTask = BoardTask.Create(command.Name,         
            command.Description,  
            command.Priority,     
            command.Deadline,   
            command.BoardId,
            command.UserId,       
            command.ColumnId,     
            command.Order,        
            command.TagIds);

        var taskEvent = new TaskCreateEvent(
            boardTask.Name,         
            boardTask.Description,  
            boardTask.Priority,     
            boardTask.Deadline, 
            boardTask.BoardId,
            boardTask.UserId,       
            boardTask.ColumnId,     
            boardTask.Order,        
            boardTask.TagIds
        );
        
        try
        {
            var response = await requestClient.GetResponse<TaskCreateSuccess>(
                taskEvent, 
                ct);
            var message = response.Message;
            return new TaskCreateResponseDto(
                message.TaskId,
                boardTask.Name,
                boardTask.Description,
                boardTask.Priority,
                boardTask.Deadline,
                boardTask.BoardId,
                boardTask.UserId,
                boardTask.ColumnId,
                boardTask.Order,
                boardTask.TagIds);
        }
        catch (RequestTimeoutException ex)
        {
            // TODO log + 5**
            Console.WriteLine($"Превышено время ожидания ответа: {ex.Message}");
        }
        catch (RequestFaultException ex)
        {
            // TODO log + 4**
            Console.WriteLine($"Ошибка при обработке запроса на стороне сервера.");
        }
        return null;
    }
}
