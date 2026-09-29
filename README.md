# Stock Replenishment Request System

A small end-to-end system for managing stock replenishment requests: create as draft, submit for review, asynchronously validate stock availability against a slow external service, approve/reject, and fulfill.

## Requirements

- .NET 10 SDK

## Run

```powershell
dotnet restore
dotnet build
dotnet run --project src/StockReplenishment.Web
```

The app hosts both the REST API and the Blazor Server UI on a single process/port. Sample data is seeded automatically into an in-memory database on startup.

## Test

```powershell
dotnet test
```

## Project layout

```
src/
  StockReplenishment.Domain          Entities and enums, no external dependencies
  StockReplenishment.Application     DTOs, interfaces, exceptions
  StockReplenishment.Infrastructure  EF Core DbContext, seeding, background worker, service implementation
  StockReplenishment.Web             ASP.NET Core host: API controllers + Blazor (MudBlazor) UI
tests/
  StockReplenishment.Tests           NUnit + NSubstitute service tests
```

## Features

- Create replenishment requests as drafts, edit before submission
- Submit requests (Draft -> Submitted)
- Approve / reject submitted requests (with mandatory rejection reason)
- Fulfill approved requests with per-line fulfilled quantities
- Filter requests by status, priority, and location, with pagination
- Seed data for stock locations and sample requests

## Key design decision: asynchronous stock validation

The external stock-availability check is slow (simulated 3-7s latency). Submitting a request does **not** synchronously wait for it:

1. `POST /api/replenishment-requests/{id}/submit` persists the request as `Submitted`, creates a pending `StockValidation` record, enqueues a background job, and immediately returns `202 Accepted`.
2. A `BackgroundService` (`StockValidationWorker`) drains an in-process channel (`IStockValidationQueue`) and calls the external service out of the request/response cycle.
3. The Blazor client polls `GET /api/replenishment-requests/{id}/validation` every 2 seconds and updates the UI (spinner -> success/warning alert) once validation completes.

This keeps the API responsive under slow external dependencies and gives the user clear feedback without blocking the UI thread.

## API

| Method | Endpoint | Purpose |
|---|---|---|
| GET | `/api/replenishment-requests` | Browse/filter/paginate |
| GET | `/api/replenishment-requests/{id}` | Details |
| GET | `/api/replenishment-requests/locations` | Stock locations |
| POST | `/api/replenishment-requests` | Create draft |
| PUT | `/api/replenishment-requests/{id}` | Update draft |
| POST | `/api/replenishment-requests/{id}/submit` | Submit (202 Accepted) |
| POST | `/api/replenishment-requests/{id}/approve` | Approve |
| POST | `/api/replenishment-requests/{id}/reject` | Reject (requires reason) |
| POST | `/api/replenishment-requests/{id}/fulfill` | Fulfill |
| GET | `/api/replenishment-requests/{id}/validation` | Stock validation result |
