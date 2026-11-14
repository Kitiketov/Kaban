using Contracts.Features.Boards;
using Kaban.Api.Features.Boards.DTO;
using MassTransit;
using MediatR;

namespace Kaban.Api.Features.Boards.Application.CreateBoard;

public sealed class CreateBoardHandler(IRequestClient<CreateBoardRequest> client)
    : IRequestHandler<CreateBoardCommand, BoardDto>
{
    public async Task<BoardDto> Handle(CreateBoardCommand command, CancellationToken ct)
    {
        var response =
            await client.GetResponse<BoardCreated>(new CreateBoardRequest(command.ProjectId, command.Name), ct);
        return new BoardDto(response.Message.Id, response.Message.ProjectId, response.Message.Name);
    }
}