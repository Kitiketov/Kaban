using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.DeleteTeam;

public sealed class DeleteTeamValidator : AbstractValidator<DeleteTeamCommand>
{
    public DeleteTeamValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
    }
}
