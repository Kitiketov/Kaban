using FluentValidation;
using Kaban.Api.Features.Tasks.Application.Common.Validation;

namespace Kaban.Api.Features.Tasks.Application.Update;

public sealed class UpdateTaskCommandValidator : AbstractValidator<UpdateTaskCommand>
{
    public UpdateTaskCommandValidator()
    {
        RuleFor(x => x.TaskId)
            .GreaterThan(0).WithMessage("Идентификатор задачи должен быть больше нуля.");

        this.ApplyTaskPayloadRules();
    }
}