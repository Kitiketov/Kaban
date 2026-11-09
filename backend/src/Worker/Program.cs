using Kaban.Worker.Features.Tasks.Application;
using Kaban.Worker.Features.Boards;
using Kaban.Worker.Features.Tasks.Presentation;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

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
        .AddSource("MassTransit")
        .AddOtlpExporter())
    .WithMetrics(metrics => metrics
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddMeter("MassTransit")
        .AddOtlpExporter());
builder.Services.AddDbContext<KabanDatabaseContext>(opt =>
    opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddMassTransit(x =>
{
    x.AddConsumer<TaskCreateConsumer>();
    x.AddConsumer<TaskDeleteConsumer>();
    x.AddConsumer<TasksMoveConsumer>();
    x.AddConsumer<TasksUpdateConsumer>();
    x.AddConsumer<CreateBoardConsumer>();
    x.AddConsumer<ListBoardsConsumer>();
    x.AddConsumer<GetBoardConsumer>();
    x.AddConsumer<RenameBoardConsumer>();
    x.AddConsumer<DeleteBoardConsumer>();
    x.AddConsumer<AddBoardColumnConsumer>();
    x.AddConsumer<RenameBoardColumnConsumer>();
    x.AddConsumer<DeleteBoardColumnConsumer>();
    x.UsingRabbitMq((context, cfg) =>
    {
        var configuration = context.GetRequiredService<IConfiguration>();
        var rabbitConfig = configuration.GetSection("RabbitMq");

        var host = rabbitConfig["Host"] ?? "localhost";
        var username = rabbitConfig["Username"] ?? "kaban";
        var password = rabbitConfig["Password"] ?? "local_dev_password";

        cfg.Host(host, h =>
        {
            h.Username(username);
            h.Password(password);
        });

        cfg.ConfigureEndpoints(context);
    });
});

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();