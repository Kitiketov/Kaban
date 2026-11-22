using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.CreateTeamRole;

public sealed class CreateTeamRoleValidator : AbstractValidator<CreateTeamRoleCommand>
{
    public CreateTeamRoleValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
        RuleFor(x => x.Role).NotEmpty().MaximumLength(64);
    }
}
