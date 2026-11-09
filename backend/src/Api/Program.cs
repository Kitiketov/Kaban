<<<<<<< HEAD
using Contracts.Features.Tasks.Create;
=======
using Contracts.Features.Tasks;
using Contracts.Features.Boards;
>>>>>>> origin/init-api
using Kaban.Api.Common;
using Kaban.Api.Common.Exceptions;
using MassTransit;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
                                      | ActivityTrackingOptions.SpanId
                                      | ActivityTrackingOptions.ParentId);
builder.Logging.AddJsonConsole(options => options.IncludeScopes = true);
builder.Logging.AddOpenTelemetry(options =>
{
    options.IncludeScopes = true;
    options.IncludeFormattedMessage = true;
    options.ParseStateValues = true;
    options.AddOtlpExporter();
});

builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(builder.Environment.ApplicationName))
    .WithTracing(tracing => tracing
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddSource("MassTransit")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MassTransit")
        .AddOtlpExporter());

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddCustomExceptionHandlers();
builder.Services.AddHealthChecks();

builder.Services.AddApplication(); // MediatR + FluentValidation pipeline
builder.Services.AddMassTransit(x =>
{
<<<<<<< HEAD
=======
    x.AddRequestClient<TaskCreateEvent>();
    x.AddRequestClient<CreateBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<ListBoardsRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<GetBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<RenameBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<DeleteBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<AddBoardColumnRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<RenameBoardColumnRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<DeleteBoardColumnRequest>(RequestTimeout.After(s: 30));
>>>>>>> origin/init-api
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