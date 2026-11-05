using FluentValidation;

namespace Kaban.Api.Features.Tasks.Application.CreateTask;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название задачи обязательно для заполнения.")
            .MaximumLength(64).WithMessage("Длина названия задачи не должна превышать 64 символа.");

        RuleFor(x => x.KanbanId)
            .GreaterThan(0).WithMessage("Идентификатор доски должен быть больше нуля.");

        RuleFor(x => x.ColumnId)
            .GreaterThan(0).WithMessage("Идентификатор колонки должен быть больше нуля.");

        RuleFor(x => x.Priority)
            .InclusiveBetween(0, 4).WithMessage("Приоритет должен быть в диапазоне от 0 до 4.");
    }
}
