// Отдельный запускаемый сервис. RabbitMQ-консьюмеры и фоновые задания
// команда добавит позже самостоятельно.
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddHealthChecks();

var app = builder.Build();
app.MapHealthChecks("/health/live");
app.MapHealthChecks("/health/ready");
app.Run();
