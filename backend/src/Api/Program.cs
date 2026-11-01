using Kaban.Api.Common;
using Kaban.Api.Common.Exceptions;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId);
builder.Logging.AddConsole(options => options.IncludeScopes = true);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ValidationExceptionHandler>();
builder.Services.AddHealthChecks();

builder.Services.AddApplication(); // MediatR + FluentValidation pipeline

builder.Services.AddMassTransit(x =>
{
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var rabbitConfig = configuration.GetSection("RabbitMq");
        
        cfg.Host(rabbitConfig["Host"] ?? "localhost", h =>
        {
            h.Username(rabbitConfig["Username"] ?? "kaban");
            h.Password(rabbitConfig["Password"] ?? "local_dev_password");
        });
    });
});

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

app.Run();
