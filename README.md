# BLACKProcesses

## UserService (.NET 10)

Реализован микросервис `UserService` на ASP.NET Core Minimal API.

### Что делает сервис
- Хранит профили пользователей (без паролей).
- Дает админские API:
  - `GET /admin/users`
  - `POST /admin/users`
  - `PATCH /admin/users/{userId}/status`
- Проверяет JWT локально по конфигурации.
- Интегрируется с внешними сервисами через HTTP:
  - `AuthService` — создание учетных данных (`POST /internal/users`).
  - `CoreService` — синхронизация статуса пользователя (`PATCH /internal/users/status`).

### Структура
- `src/UserService/Program.cs` — DI, auth, http-клиенты, middleware.
- `src/UserService/Endpoints` — маршруты API.
- `src/UserService/Services` — бизнес-логика и HTTP интеграции.
- `src/UserService/Data` — EF Core DbContext.
- `src/UserService/Domain` — модель пользователя и enum-ы.
- `src/UserService/Contracts` — DTO запросов и ответов.

### Конфигурация
`src/UserService/appsettings.json`:
- `Services:AuthServiceUrl`
- `Services:CoreServiceUrl`
- `Jwt:Issuer`
- `Jwt:Audience`
- `Jwt:SigningKey`

### Запуск
```bash
cd src/UserService
dotnet restore
dotnet run
```
