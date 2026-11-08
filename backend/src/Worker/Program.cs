using Kaban.Worker.Features.Tasks.Application;
using Kaban.Worker.Infrastructure.Database;
using MassTransit;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();
builder.Logging.Configure(options =>
    options.ActivityTrackingOptions = ActivityTrackingOptions.TraceId
        | ActivityTrackingOptions.SpanId
        | ActivityTrackingOptions.ParentId);
builder.Logging.AddConsole(options => options.IncludeScopes = true);
builder.Services.AddScoped<ITaskService, TaskService>();
builder.Services.AddDbContext<KabanDatabaseContext>(
    opt => opt.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));
builder.Services.AddMassTransit(x =>
    {
        x.AddConsumers(typeof(Program).Assembly);     
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
