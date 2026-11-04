using FluentValidation;

namespace Kaban.Api.Features.Boards.Application.RenameBoardColumn;

public sealed class RenameBoardColumnValidator : AbstractValidator<RenameBoardColumnCommand>
{
    public RenameBoardColumnValidator()
    {
        RuleFor(x => x.ColumnId).GreaterThan(0);
        RuleFor(x => x.Name).NotEmpty().MaximumLength(64);
    }
}