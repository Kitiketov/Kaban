using FluentValidation;

namespace Kaban.Api.Features.Users.Application.Search;

public sealed class SearchUsersValidator : AbstractValidator<SearchUsersQuery>
{
    public SearchUsersValidator()
    {
        RuleFor(x => x.ActorId).GreaterThan(0);
        RuleFor(x => x.Query).NotEmpty().MaximumLength(320);
        RuleFor(x => x.Limit).InclusiveBetween(1, 100);
    }
}