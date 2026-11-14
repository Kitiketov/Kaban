using FluentValidation;

namespace Kaban.Api.Features.Tasks.Application.Delete;

public sealed class DeleteTaskCommandValidator : AbstractValidator<DeleteTaskCommand>
{
    public DeleteTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("Идентификатор задачи должен быть больше нуля.");
    }
}