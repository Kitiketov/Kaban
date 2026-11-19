using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.ListTeams;

public sealed class ListTeamsValidator : AbstractValidator<ListTeamsQuery>
{
    public ListTeamsValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
    }
}
