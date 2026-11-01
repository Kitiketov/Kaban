// Отдельный запускаемый сервис. RabbitMQ-консьюмеры и фоновые задания
// команда добавит позже самостоятельно.

using MassTransit;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId);
builder.Logging.AddConsole(options => options.IncludeScopes = true);
builder.Services.AddMassTransit(x =>
    {
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
