using FluentValidation;

namespace Kaban.Api.Features.Weather.Application.CreateForecast;

public sealed class CreateForecastValidator : AbstractValidator<CreateForecastCommand>
{
    public CreateForecastValidator()
    {
        RuleFor(x => x.Date).NotEmpty();
        RuleFor(x => x.TemperatureC).InclusiveBetween(-100, 100);
        RuleFor(x => x.Summary).NotEmpty().MaximumLength(120);
    }
}
