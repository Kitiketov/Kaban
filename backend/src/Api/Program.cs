using Contracts.Features.Tasks;
using Contracts.Features.Boards;
using Kaban.Api.Common;
using Kaban.Api.Common.Exceptions;
using MassTransit;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId);
builder.Logging.AddConsole();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddProblemDetails();
builder.Services.AddCustomExceptionHandlers();
builder.Services.AddHealthChecks();

builder.Services.AddApplication(); // MediatR + FluentValidation pipeline
builder.Services.AddMassTransit(x =>
{
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
app.MapControllers();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");


app.Run();
