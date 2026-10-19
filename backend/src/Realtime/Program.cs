// Отдельный запускаемый сервис. Будущая задача: SignalR Hub,
// аутентификация пользователей и подписка на события из RabbitMQ.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();
