using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.CreateTeam;

public sealed class CreateTeamValidator : AbstractValidator<CreateTeamCommand>
{
    public CreateTeamValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}
