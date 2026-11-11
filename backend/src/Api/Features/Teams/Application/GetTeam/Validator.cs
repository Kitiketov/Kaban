using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.GetTeam;

public sealed class GetTeamValidator : AbstractValidator<GetTeamQuery>
{
    public GetTeamValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
    }
}
