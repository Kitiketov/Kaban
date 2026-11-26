using MediatR;

namespace Kaban.Api.Features.Tasks.Application.Delete;

public sealed record DeleteTaskCommand(int TaskId) : IRequest;