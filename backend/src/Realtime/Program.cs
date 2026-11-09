using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

// Отдельный запускаемый сервис. Будущая задача: SignalR Hub,
// аутентификация пользователей и подписка на события из RabbitMQ.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
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
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();