# Food Delivery Microservices (.NET 10)

Practical cloud-native food delivery sample built with **.NET 10**, **.NET Aspire 13**, **Microservices**, **Vertical Slice**, **CQRS**, **DDD**, and **event-driven messaging** (RabbitMQ + Wolverine).

## Architecture

- **ApiGateway** — YARP reverse proxy (`https://localhost:3001`)
- **Catalogs** — products write model (Postgres) + read model (MongoDB)
- **Customers** — created from `UserRegisteredIntegrationEvent`
- **Identity** — register/login + JWT (data-centric)
- **Orders** — create order, publish `OrderCreatedIntegrationEvent`
- **AppHost** — Aspire orchestration for Postgres, MongoDB, RabbitMQ, Redis, and all APIs

## Run with Aspire (recommended)

```powershell
dotnet run --project src/Aspire/FoodDelivery.AppHost
```

Open the Aspire dashboard URL printed in the console. Gateway HTTPS profile: `https://localhost:3001`.

## Run infrastructure only

```powershell
docker compose -f deployments/docker-compose/docker-compose.infrastructure.yaml up -d
```

Local development uses the machine PostgreSQL on port `5432` (`Username=postgres`, `Password=s@123456`) and MongoDB on `27017`. Docker Compose still provides RabbitMQ (`5672` / UI `15672`) and optional Redis.

Demo dashboard: [http://localhost:5100/demo](http://localhost:5100/demo)

Then run each API (or use Aspire). Local connection strings are in each service `appsettings.json`.

```powershell
dotnet run --project src/Services/Catalogs/FoodDelivery.Services.Catalogs.Api --launch-profile http
dotnet run --project src/Services/Customers/FoodDelivery.Services.Customers.Api --launch-profile http
dotnet run --project src/Services/Identity/FoodDelivery.Services.Identity.Api --launch-profile http
dotnet run --project src/Services/Orders/FoodDelivery.Services.Orders.Api --launch-profile http
```

| Service | URL |
|---------|-----|
| Catalogs | http://localhost:5100 |
| Customers | http://localhost:5200 |
| Orders | http://localhost:5300 |
| Identity | http://localhost:5400 |
| Gateway | https://localhost:3001 |

## Sample flow

1. `POST /api/v1/identity/register` → JWT + publishes user-registered event
2. Customers service consumes event and creates customer read/write models
3. `GET /api/v1/products` → seeded catalog
4. `POST /api/v1/orders` → order + integration event

## Tech

| Area | Choice |
|------|--------|
| Runtime | .NET 10 |
| Orchestration | Aspire 13.6 |
| Messaging | Wolverine + RabbitMQ |
| Write DB | PostgreSQL + EF Core |
| Read DB | MongoDB |
| Gateway | YARP |
| CQRS | MediatR + FluentValidation |
| Auth | JWT Bearer |

