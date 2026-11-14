using FluentValidation;

namespace Kaban.Api.Features.Teams.Application.AddTeamMember;

public sealed class AddTeamMemberValidator : AbstractValidator<AddTeamMemberCommand>
{
    public AddTeamMemberValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.TeamId).GreaterThan(0);
        RuleFor(x => x.UserId).GreaterThan(0);
    }
}
