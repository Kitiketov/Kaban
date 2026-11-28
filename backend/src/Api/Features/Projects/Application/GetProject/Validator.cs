using FluentValidation;

namespace Kaban.Api.Features.Projects.Application.GetProject;

public sealed class GetProjectValidator : AbstractValidator<GetProjectQuery>
{
    public GetProjectValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.ProjectId).GreaterThan(0);
    }
}
