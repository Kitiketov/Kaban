using System.Threading.RateLimiting;
using Contracts.Features.Boards;
using Contracts.Features.Tasks;
using Kaban.Api.Common;
using Kaban.Api.Common.Exceptions;
using Kaban.Api.Features.Auth.Infrastructure;
using Kaban.Contracts.Users;
using MassTransit;
using Microsoft.OpenApi;
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
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Description = "Paste the accessToken without the Bearer prefix. For POST /api/auth/tokens/refresh, use the refreshToken instead."
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference("Bearer", document)] = []
    });
    options.OperationFilter<BearerSecurityOperationFilter>();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<MessagingExceptionHandler>();
builder.Services.AddCustomExceptionHandlers();
builder.Services.AddKabanAuth(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0
        }));
});
builder.Services.AddHealthChecks();

builder.Services.AddApplication(); // MediatR + FluentValidation pipeline
builder.Services.AddMassTransit(x =>
{
    var seconds = builder.Configuration.GetValue("Messaging:RequestTimeoutSeconds", 30);
    if (seconds is < 1 or > 120) throw new InvalidOperationException("Messaging timeout must be 1–120 seconds.");
    var timeout = RequestTimeout.After(s: seconds);
    x.AddRequestClient<RegisterUserRequested>(timeout);
    x.AddRequestClient<GetCredentialsRequested>(timeout);
    x.AddRequestClient<GetUserRequested>(timeout);
    x.AddRequestClient<SearchUsersRequested>(timeout);
    x.AddRequestClient<TaskCreateEvent>();
    x.AddRequestClient<CreateBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<ListBoardsRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<GetBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<RenameBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<DeleteBoardRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<AddBoardColumnRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<RenameBoardColumnRequest>(RequestTimeout.After(s: 30));
    x.AddRequestClient<DeleteBoardColumnRequest>(RequestTimeout.After(s: 30));
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
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");


app.Run();
