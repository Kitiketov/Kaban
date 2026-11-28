using FluentValidation;

namespace Kaban.Api.Features.Tasks.Application.Common.Validation;

public static class TaskValidationRules
{
    public static void ApplyTaskPayloadRules<T>(this AbstractValidator<T> validator)
        where T : ITaskPayload
    {
        validator.RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Название задачи обязательно для заполнения.")
            .MaximumLength(64).WithMessage("Длина названия задачи не должна превышать 64 символа.");

        validator.RuleFor(x => x.Priority)
            .InclusiveBetween(0, 4).WithMessage("Приоритет должен быть в диапазоне от 0 до 4.");

        validator.RuleFor(x => x.ColumnId)
            .GreaterThan(0).WithMessage("Идентификатор колонки должен быть больше нуля.");

        validator.When(x => x.UserId.HasValue, () =>
        {
            validator.RuleFor(x => x.UserId)
                .GreaterThan(0).WithMessage("Идентификатор пользователя должен быть больше нуля.");
        });

        validator.When(x => x.TagIds != null, () =>
        {
            validator.RuleForEach(x => x.TagIds!)
                .GreaterThan(0).WithMessage("Идентификатор тега должен быть больше нуля.");
        });
    }
}
