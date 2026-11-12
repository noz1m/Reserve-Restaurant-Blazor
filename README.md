# Reserve. Restaurant OS — .NET 10, Blazor, PostgreSQL

## Важно — исправление ошибки `3D000: database "Restaurant_DB" does not exist`

Раньше сайт запускался без создания БД, а все страницы и операции завершались исключением.
Теперь в режиме **Development** включено `Database:AutoMigrate=true`: до приёма запросов приложение вызывает `MigrateAsync()`, который при достаточных правах пользователя PostgreSQL может создать отсутствующую БД и применить существующие миграции.
В production автоматические миграции по умолчанию отключены. **Существующие базы не удаляются.**

### На Mac: настройка с нуля

Открой терминал в корне проекта:

```bash
# Убедись, что PostgreSQL слушает порт 5432
pg_isready -h localhost -p 5432

# Введи собственный пароль PostgreSQL (не публикуй его)
dotnet user-secrets init --project Blazor
dotnet user-secrets set "ConnectionStrings:DefaultConnection" 'Host=localhost;Port=5432;Database=Restaurant_DB;Username=postgres;Password=YOUR_PASSWORD' --project Blazor

# Запусти приложение: миграции в Development применятся перед запуском сайта
dotnet restore
dotnet build Blazor/Blazor.csproj
dotnet run --project Blazor --launch-profile http
```

**Важно:** имя PostgreSQL-базы чувствительно к регистру, когда используются кавычки; `Restaurant_DB` и `restaurant_db` могут быть разными базами. Не создавай пустую базу поверх существующей: сначала проверь список `psql -h localhost -p 5432 -U postgres -l` и при необходимости поправь только параметр `Database=`.

Если пользователь PostgreSQL не имеет права создавать базы, сначала создай пустую БД с правами администратора:

```bash
createdb -h localhost -p 5432 -U postgres 'Restaurant_DB'
```

Затем повтори запуск. Если БД уже существует, команду `createdb` выполнять не нужно.

### Безопасное обновление существующей БД

Перед изменением схемы сделай резервную копию с помощью `pg_dump`. Если в существующей базе важные данные, **не включай автоматические миграции до проверки**: установи `Database:AutoMigrate=false` в `Blazor/appsettings.Development.json`, сравни схему с EF Core миграциями и выполни миграцию вручную:

```bash
dotnet ef migrations list --project Infrastructure --startup-project Blazor --context DataContext
dotnet ef database update --project Infrastructure --startup-project Blazor --context DataContext
```

### Проверка работоспособности

После запуска проверь главную страницу, `/all-tables`, `/create-table`, `/customers/create`, `/reservations` и создание тестового столика, клиента и бронирования. В архив добавлен `scripts/check.sh` для проверки сборки и HTTP-ответов на локальном Mac (нужен запущенный сайт). Поскольку в среде подготовки нет .NET SDK и доступа к локальному PostgreSQL пользователя, полная интеграционная проверка **не подтверждена**.

## Структура

- `Domain` — сущности, DTO и фильтры
- `Infrastructure` — EF Core, PostgreSQL, сервисы, миграции и `DI/DependencyInjection.cs`
- `Blazor` — Blazor Web App (Interactive Server), UI

## Известные ограничения

В проекте по-прежнему используется AutoMapper 13.0.1, для которого NuGet сообщает `NU1903`. До production следует обновить или заменить зависимость с проверкой совместимости. Конкурентные бронирования требуют ограничения в PostgreSQL на уровне БД. Приложение не проверялось на 100% в браузере: браузер и PostgreSQL на Mac пользователя недоступны в этой среде.
