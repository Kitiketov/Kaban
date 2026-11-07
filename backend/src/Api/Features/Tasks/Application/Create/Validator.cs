using FluentValidation;
using Kaban.Api.Features.Tasks.Application.Common;
using Kaban.Api.Features.Tasks.Application.Common.Validation;

namespace Kaban.Api.Features.Tasks.Application.Create;

public sealed class CreateTaskCommandValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskCommandValidator()
    {
        this.ApplyTaskPayloadRules();

        RuleFor(x => x.KanbanId)
            .GreaterThan(0).WithMessage("Идентификатор доски должен быть больше нуля.");
    }
}
