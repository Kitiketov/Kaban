using FluentValidation;

namespace Kaban.Api.Features.Tasks.Application.Move;

public sealed class MoveTaskCommandValidator : AbstractValidator<MoveTaskCommand>
{
    public MoveTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("Идентификатор задачи должен быть больше нуля.");

        RuleFor(x => x.ColumnId)
            .GreaterThan(0).WithMessage("Идентификатор колонки должен быть больше нуля.");
    }
}
