# dmp.job.server

Background job server of the DMP marketplace (digital goods & data marketplace with crypto payments via Bitcart).
It runs [Hangfire](https://www.hangfire.io/) with PostgreSQL storage and executes three recurring jobs every minute:
it delivers queued e-mails from the `Mail` table (rendering Razor templates), rebuilds the RedisJSON product
documents that the web API searches, and forwards system notifications from a Redis queue to a Telegram chat.
The work queues are filled by the other DMP services, `dmp.api.web`, `dmp.job.trxworker` and `dmp.job.invoiceworker`.

## Tech stack

- .NET 10 / ASP.NET Core 10 (minimal hosting), C# latest, nullable reference types
- Hangfire 1.8 (`Hangfire.AspNetCore`) + `Hangfire.PostgreSql` 1.21 storage and dashboard
- Entity Framework Core 10 with `Npgsql.EntityFrameworkCore.PostgreSQL` 10
- StackExchange.Redis 3.3 + NRedisStack 1.8 (RedisJSON)
- RazorLight 2.3 (runtime Razor rendering of e-mail templates)
- `System.Net.Mail.SmtpClient` for SMTP delivery, Telegram Bot API over `HttpClient`
- Central Package Management (`Directory.Packages.props`), `.slnx` solution

## Recurring jobs

| Job ID              | Schedule            | Service                 | What it does                                                                                                                         |
|---------------------|---------------------|-------------------------|--------------------------------------------------------------------------------------------------------------------------------------|
| `mail-sender-job`   | `* * * * *` (1 min) | `IMailService`          | Sends `Mail` rows with status below `Sent`, oldest `UpdatedAt` first; after more than 10 failed attempts a mail is set to `Error`.     |
| `product-cache-job` | `* * * * *` (1 min) | `IProductCacheService`  | Takes `JobProductCacheTask` rows in batches of 10 and writes each product to RedisJSON key `product:{rootCategoryId}:{productId}`, then deletes the rows. |
| `notifications-job` | `* * * * *` (1 min) | `INotificationsService` | Pops messages from the Redis list `sys_notify_queue` (`BRPOP`, 2 s timeout) and posts them to Telegram (or just logs them when disabled). |

The job IDs, schedules and the `DMP.BL.Services.I*Service` interface names are stored in the Hangfire database
and must stay stable. A global `DisableConcurrentExecutionFilter` skips a run while the previous run of the
same job is still in progress (in-process guard, so it assumes a single server instance).

The product cache job needs the category list that `dmp.api.web` caches under the Redis key
`dal:catalog:menu:categories`; the job fails until that key exists.

## Project structure

```
dmp.job.server/
├── Job.Server/                 ASP.NET Core host: Hangfire server, dashboard and recurring job registration
│   ├── Program.cs              Composition root and job schedules
│   ├── ServiceExtensions.cs    DI registration for PostgreSQL, Redis and job services
│   ├── DisableConcurrentExecutionFilter.cs  Global filter that skips overlapping runs of a job
│   └── HangfireBasicAuthFilter.cs           Optional Basic auth for the dashboard
├── DMP.BL/                     Job logic (mail, product cache, notifications) and Redis access
├── DMP.DataAccess/             EF Core DbContext and entities (copy of the shared DMP data model)
├── Directory.Build.props       Common build settings (net10.0, nullable, analyzers)
├── Directory.Packages.props    Central package versions
├── Dockerfile                  Multi-stage image build (runs as non-root)
└── .env.example                Documented environment variables
```

## Configuration

Settings come from `appsettings*.json`, user secrets (Development) and environment variables
(`Section__Key` for nested keys). No real values are committed; see `.env.example`.

| Setting / env var                         | Description                                                                            |
|-------------------------------------------|----------------------------------------------------------------------------------------|
| `ConnectionStrings__DmpConnection`        | PostgreSQL connection string for the DMP database. Hangfire stores its data there too (`hangfire` schema). Required. |
| `REDIS_HOST`, `REDIS_PORT`                | Redis Stack host and port (RedisJSON module required).                                 |
| `REDIS_PASSWORD`                          | Redis password.                                                                        |
| `SmtpSettings__Host`, `SmtpSettings__Port` | SMTP server for outgoing mail.                                                         |
| `SmtpSettings__UserName`, `SmtpSettings__Password` | SMTP credentials.                                                             |
| `SmtpSettings__Ssl`                       | `true` to use SSL/TLS.                                                                 |
| `TelegramSettings__Enabled`               | `true` to send system notifications to Telegram; otherwise they are only logged.        |
| `TelegramSettings__BotToken`              | Telegram bot token.                                                                    |
| `TelegramSettings__SysChatId`             | Target chat ID for system notifications.                                               |
| `HangfireDashboard__Username`, `HangfireDashboard__Password` | Basic auth credentials for `/hangfire`. If either is empty, the dashboard has no authentication and a warning is logged at startup. |
| `ASPNETCORE_ENVIRONMENT`                  | `Development` (Docker dev: Kestrel on `:80`, `postgres-dmp`, `mailhog-dmp`), `Local` (host run against published ports), `Production`. |
| `ASPNETCORE_HTTP_PORTS`                   | HTTP port inside the container (`80` in `dmp.docker`).                                  |

## Getting started

### Prerequisites

- .NET SDK 10.0 (see `global.json`)
- PostgreSQL with the DMP database
- Redis Stack (RedisJSON)
- An SMTP server (MailHog works for development)

The complete environment is easiest to start with [dmp.docker](https://github.com/denis-susha/dmp.docker).

### Run locally

1. Put the real connection string and passwords into `Job.Server/appsettings.Local.json` or, better,
   user secrets (`dotnet user-secrets set "ConnectionStrings:DmpConnection" "..." --project Job.Server`).
2. Adjust the Redis variables in `Job.Server/Properties/launchSettings.json`.
3. Run:

   ```bash
   dotnet run --project Job.Server --launch-profile http
   ```

4. Open the dashboard at http://localhost:5154/hangfire.

### Run with Docker

```bash
docker build -t image-dmp-job-server .
docker run --rm --env-file .env -e ASPNETCORE_HTTP_PORTS=80 -p 9050:80 image-dmp-job-server
```

In the full system the container is built and started by `dmp.docker` (`job.server-dmp` service).

## Commands

| Command                                           | Purpose                        |
|---------------------------------------------------|--------------------------------|
| `dotnet build -c Release`                         | Build the solution             |
| `dotnet format`                                   | Apply code style (`.editorconfig`) |
| `dotnet format --verify-no-changes`               | Check formatting (CI-friendly) |
| `dotnet list package --vulnerable --include-transitive` | Check dependencies for known vulnerabilities |

## Related repositories

- [dmp](https://github.com/denis-susha/dmp): umbrella repository and system overview
- [dmp.api.web](https://github.com/denis-susha/dmp.api.web): web API; queues e-mails, product-cache tasks and
  system notifications, caches the category list and searches the Redis product documents
- [dmp.job.trxworker](https://github.com/denis-susha/dmp.job.trxworker): transaction worker; queues e-mails and product-cache tasks
- [dmp.job.invoiceworker](https://github.com/denis-susha/dmp.job.invoiceworker): invoice worker; queues e-mails
- [dmp.docker](https://github.com/denis-susha/dmp.docker): Docker Compose setup that runs this service
