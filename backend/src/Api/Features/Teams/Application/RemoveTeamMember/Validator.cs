using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.RemoveTeamMember;

public sealed class RemoveTeamMemberValidator : AbstractValidator<RemoveTeamMemberCommand>
{
    public RemoveTeamMemberValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
