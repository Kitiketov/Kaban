# Worker

Самостоятельный .NET host. Уже использует **MassTransit + RabbitMQ**: в `Program.cs` зарегистрирован `ForecastCreatedConsumer`, который получает `ForecastCreatedEvent` из `backend/Contracts` через автоматически созданный receive endpoint и **только пишет лог**. Реальная фоновая работа, idempotency, retry policy, собственное состояние и результат ещё не реализованы.

См. `AGENTS.md`, `../../../docs/MESSAGING.md` и `.agents/skills/kaban-rabbitmq-workflow/SKILL.md` в корне репозитория. Health endpoints пока не проверяют соединение с RabbitMQ.
