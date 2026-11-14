using FluentValidation;

namespace Kaban.Api.Features.Projects.Application.DeleteProject;

public sealed class DeleteProjectValidator : AbstractValidator<DeleteProjectCommand>
{
    public DeleteProjectValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}
