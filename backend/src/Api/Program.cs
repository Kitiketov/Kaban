using Kaban.Api.Common;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Weather.Infrastructure;
using Kaban.Api.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication(); // MediatR + FluentValidation pipeline
builder.Services.AddPersistence(builder.Configuration); // EF Core + UnitOfWork
builder.Services.AddWeatherFeature(); // IWeatherRepository -> WeatherRepository

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseExceptionHandler();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");

// Только учебный Weather и только Development. Позже — EF Core migrations.
if (app.Environment.IsDevelopment())
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<KabanDbContext>();
    await db.Database.EnsureCreatedAsync();
}

app.Run();
