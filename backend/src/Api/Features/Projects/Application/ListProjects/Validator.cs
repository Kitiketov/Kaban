using FluentValidation;

namespace Kaban.Api.Features.Projects.Application.ListProjects;

public sealed class ListProjectsValidator : AbstractValidator<ListProjectsQuery>
{
    public ListProjectsValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
    }
}
