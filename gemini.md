# OmniOrder Implementation & Change Report

**Project:** OmniOrder — High-Concurrency Distributed Order Management, Inventory Locking & Payment Webhook Engine  
**Execution Environment:** .NET 8 / C# 12 | React 18 + TypeScript + Vite + Tailwind CSS | SQLite WAL  
**Status:** All Backend, Infrastructure, API, Integration Tests, and Frontend components implemented and verified.

---

## 1. Summary of Changes (+1608 / -34)

This update represents the complete, production-grade realization of OmniOrder from ground up, adhering strictly to Clean Architecture, SOLID principles, zero placeholders, and 100% local execution.

| Layer / Component | Files Created / Modified | Key Additions / Refactors |
| :--- | :--- | :--- |
| **Solution & Configuration** | `OmniOrder.slnx`, `docker-compose.yml`, `Dockerfile`, `.gitignore` | Multi-project .NET 8 solution, multi-stage Dockerfile, local SQLite volume mapping with optional PostgreSQL / MSSQL toggles, standard ignore rules. |
| **Domain Layer** | 7 Entities, 3 Enums, 5 Domain Exceptions | `Product` (Optimistic concurrency via `RowVersion`, computed `AvailableStock`), `Order`, `OrderItem`, `InventoryReservation`, `IdempotencyRecord`, `WebhookEventLog`, `OutboxMessage`. |
| **Application Layer** | 7 DTO Sets, 7 Interfaces, Event Models | Service contracts for Orders, Inventory, Webhooks, Analytics, Idempotency, Tokens, and Concurrency Tracking. In-memory `OrderAuditEvent`. |
| **Infrastructure Layer** | `OmniOrderDbContext`, 7 Services, 2 Background Workers, Seeder | SQLite WAL initialization, dynamic `RowVersion` generation on save, Dapper composite queries with parameterized SQL, bounded `Channel<OrderAuditEvent>` (1000 capacity), `ExpiredReservationReaperService` (15s sweep), `OutboxProcessorService` (SignalR broadcast), 8 seed products + 5 historical orders. |
| **API Layer** | 6 Controllers, 2 Middlewares, SignalR `OrderHub`, `Program.cs` | `OrdersController`, `SimulatorController` (50 concurrent race condition), `WebhooksController` (HMAC-SHA256 signature verification), `AnalyticsController`, `AuthController` (JWT tokens), `IdempotentAttribute` (HTTP filter with response caching), `GlobalExceptionHandler` (RFC 7807 `ProblemDetails`). |
| **Integration Tests** | `tests/OmniOrder.IntegrationTests/` (5 Tests) | Flash sale concurrency test (50 parallel tasks, exactly 5 succeed, 45 fail with 409), Idempotency enforcement test, Webhook 5x duplicate replay test, Dapper vs EF Core benchmark test, Demo token test. |
| **Frontend Console** | `client/` (React 18 + TS + Vite + Tailwind) | Sleek Dark Mode dashboard (`#090D16` bg, `#111827` cards), real-time SignalR order stream, Flash-Sale Race Condition Simulator, Webhook & Idempotency tester, dual ORM latency comparison. |

---

## 2. Core Architectural Features Implemented

### A. Optimistic Concurrency Inventory Reservation (Anti-Overselling Engine)
- **Entities:** `Product` with `TotalStock`, `ReservedStock`, computed `AvailableStock => TotalStock - ReservedStock`, and `RowVersion` byte array.
- **Concurrency Strategy:** Configured via `IsConcurrencyToken()` in EF Core with an automatic byte rotation on `SaveChangesAsync`.
- **Race Condition Simulation (`POST /api/v1/simulator/flash-sale`):**
  - Concurrently invokes 50 asynchronous tasks via `Task.WhenAll` against a product with 5 units left.
  - Provably achieves **exactly 5 orders confirmed (201 Created)** and **45 gracefully rejected with 409 Conflict (Concurrency Conflict or Insufficient Stock)**.
  - Zero negative stock or phantom reservations.

### B. Idempotent Payment Webhook Engine & Transactional Outbox
- **Idempotency Filter (`[Idempotent]`):** Enforces an `X-Idempotency-Key` header on critical write endpoints. Caches responses in SQLite with SHA-256 request payload hashing. Identical subsequent requests return cached responses with `X-Cache: HIT`.
- **HMAC-SHA256 Webhook Verification:** Validates webhook payloads (`PAYMENT_CAPTURED`, `PAYMENT_FAILED`) against an HMAC secret.
- **At-Least-Once Delivery & Deduplication:** Webhook events are logged in `WebhookEventLogs`. Replaying the same webhook 5 times processes state transitions (Pending $\to$ Paid, or Pending $\to$ Cancelled with stock release) exactly once.
- **Transactional Outbox:** State changes atomically persist `OutboxMessage` records inside the same EF Core database transaction.

### C. High-Throughput Asynchronous Workers & Channels
- **`OrderAuditChannel`:** A high-throughput bounded channel (`Channel<OrderAuditEvent>`) decoupling API request paths from audit logging.
- **`ExpiredReservationReaperService`:** Background worker polling every 15 seconds. Scans for orders exceeding their 5-minute TTL, releases `ReservedStock`, marks reservations `Expired`, and orders `Cancelled`.
- **`OutboxProcessorService`:** Drains pending `OutboxMessage` rows every 5 seconds and pushes real-time events over SignalR (`/hubs/orders`).

### D. Dual ORM Strategy (EF Core 8 Writes + Dapper Reads)
- **Writes:** EF Core handles ACID transactions, domain entities, foreign keys, and optimistic concurrency locks.
- **Reads:** Dapper executes raw parameterized SQL queries with composite indexes on `GET /api/v1/analytics/dashboard-metrics`.
- **Benchmarking:** Calculates real-time query latency comparing Dapper versus EF Core side-by-side, tracking GMV, order volume, and lock contention rate.

### E. Enterprise Security & Observability
- **JWT Bearer Authentication:** Pre-configured roles (`Admin`, `Customer`) with `[Authorize]`. Includes `POST /api/v1/auth/demo-token` for 1-click token generation.
- **Global RFC 7807 Exception Handler:** Catches all domain and infrastructure exceptions, mapping them to standard `ProblemDetails` with status codes (400, 404, 409, 422, 500).
- **Swagger / OpenAPI:** Configured with JWT Bearer scheme.

---

## 3. SQLite Concurrency & Reliability Engineering
During the scaffolding and testing phases, multiple critical database edge cases were encountered and permanently resolved:
1. **RowVersion Byte Constraint:** SQLite does not natively support SQL Server `ROWVERSION` / `TIMESTAMP` triggers. Switched from `IsRowVersion()` to `IsConcurrencyToken()` and rotated a 16-byte cryptographically secure GUID byte array inside `OmniOrderDbContext.SaveChangesAsync()`.
2. **Decimal Aggregations in SQLite:** SQLite stores decimals as text/real; `SumAsync(o => o.TotalAmount)` in EF Core threw a translation error. Fixed by projecting `.Select(o => o.TotalAmount).ToListAsync()` and computing `.Sum()` in memory.
3. **Dapper DTO Materialization:** Dapper C# positional records without parameterless constructors failed materialization on SQLite's typeless engine. Refactored analytics DTOs into standard classes with parameterless constructors and compatible primitive types (`long`, `double`, `string`).
4. **Idempotency JSON Serialization:** Synchronized JSON serialization casing between cached responses and direct controller responses using `JsonNamingPolicy.CamelCase`.

---

## 4. Verification & Test Results
- **`dotnet build`:** Clean build across all 5 projects (0 errors).
- **`dotnet test`:** 5/5 Integration Tests Passing:
  - `FlashSaleSimulation_50ConcurrentRequests_OnlyAvailableStockPurchased` ✅
  - `CreateOrder_WithSameIdempotencyKey_ReturnsSameResponse` ✅
  - `PaymentWebhook_DuplicateEventFired5Times_ProcessedExactlyOnce` ✅
  - `GetDashboardMetrics_ReturnsValidDualOrmMetrics` ✅
  - `GetDemoToken_ReturnsValidJwt` ✅
- **Client Build (`npm run build`):** Vite production build compiled without errors.
