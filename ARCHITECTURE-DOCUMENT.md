# Architecture Document

**Last Updated:** July 7, 2026

## 1. Architecture Summary

This project implements a production-style microservices architecture with:
- API Gateway (YARP) for routing, load balancing, and BFF aggregation.
- 4 microservices: OrderService, ProductCatalogService, InventoryService, NotificationService.
- Polyglot persistence: SQL Server, MongoDB, Redis, and stateless service.
- Async messaging with RabbitMQ + MassTransit.
- Choreography-based saga pattern for order processing and compensation.
- Correlation ID propagation for distributed tracing.
- Centralized logging with Seq.

## 2. Topology and Flow Diagram

```
+----------------------+          +-------------------------+
|  Client / Browser    |          |      Seq Log Store      |
|  (HTTP -> 8080)      |          |  http://localhost:5341  |
+----------+-----------+          +-----------+-------------+
           |                                ^
           |                                |
           v                                |
+----------------------+                    |
|      API Gateway     |                    |
|    (YARP, Port 8080) |                    |
|  - BFF Aggregation   |                    |
|  - Correlation IDs   |                    |
+-------+-------+------+                    |
        |       |                           |
        |       |                           |
        v       v                           |
+-------+--+ +--+------------+         +----+----------------+
| OrderSvc | | ProductSvc    |         | NotificationSvc     |
| 5001     | | 5002 (2x repl)|         | 5004                |
| SQL DB   | | MongoDB       |         | Stateless           |
+----+-----+ +------+--------+         +---------+----------+
     |               |                          ^
     |               |                          |
     |               |                          |
     v               v                          |
+--------------------------------+              |
|          RabbitMQ              |<-------------+
|      (Message Broker)          |
|   - OrderPlaced                |
|   - InventoryReserved          |
|   - InventoryRejected          |
|   - OrderFinalized             |
+----------------+---------------+
                 |
                 v
          +------+-------+
          | InventorySvc | 
          | 5003         |
          | Redis cache  |
          +--------------+
```

## 3. Service Responsibilities

### API Gateway
- Receives all external client traffic on `http://localhost:8080`.
- Routes requests to internal services.
- Implements BFF endpoint `/api/bff/order-details/{id}` to aggregate order and product details.
- Injects and forwards `X-Correlation-ID` for distributed tracing.

### OrderService
- Stores orders in SQL Server.
- Initiates the order saga when a new order is created.
- Publishes `OrderPlaced` events to RabbitMQ.
- Reacts to `InventoryReserved`, `InventoryRejected`, and finalizes order status.

### InventoryService
- Reads/writes inventory state from Redis.
- Consumes `OrderPlaced` events.
- Reserves stock and publishes either `InventoryReserved` or `InventoryRejected`.
- Propagates the correlation ID in message headers.

### ProductCatalogService
- Stores product data in MongoDB.
- Exposes product queries and updates.
- Uses Redis cache for faster read responses and cache hit/miss behavior.

### NotificationService
- Stateless service with no database.
- Sends notification events after order finalization or rejection.
- Subscribed to saga events via RabbitMQ.

## 4. Architecture Decisions and ADRs

This architecture is supported by the following ADRs:
- [ADR-001-OrderService-SqlServer.md](ADR-001-OrderService-SqlServer.md)
- [ADR-002-ProductCatalog-MongoDB.md](ADR-002-ProductCatalog-MongoDB.md)
- [ADR-003-Inventory-Redis.md](ADR-003-Inventory-Redis.md)
- [ADR-004-NotificationService-Stateless.md](ADR-004-NotificationService-Stateless.md)

### Decision Highlights
- SQL Server for transactional order data and business-critical consistency.
- MongoDB for a flexible product catalog that can store varying metadata.
- Redis for inventory state where low latency and fast reads matter.
- Notification service as stateless to allow horizontal scaling without persistence concerns.

## 5. Messaging Technology Comparison

### Chosen stack
- RabbitMQ as the message broker.
- MassTransit for .NET integration and consumer orchestration.
- Choreography-based saga using event messages.

### Why RabbitMQ?
- Proven, reliable message broker with broad industry adoption.
- Easy to run in Docker with management UI on `http://localhost:15672`.
- Good support for publish/subscribe, routing, and at-least-once delivery.
- Lightweight and well-supported by MassTransit.

### Alternatives considered

| Technology | Pros | Cons | Verdict |
|---|---|---|---|
| RabbitMQ | Mature, works well with MassTransit, easy local setup | Not as cloud-native as Kafka | Chosen for simplicity and reliability in course scope |
| Apache Kafka | High throughput, durable event log | More complex setup, heavier for small project | Not chosen because the project favors choreography and lightweight local deployment |
| Azure Service Bus | Cloud-managed, enterprise features | Requires Azure subscription and external environment | Not chosen because project needs a self-contained local demo |
| NATS | Very low latency | Less suited for durable saga events and message persistence | Not chosen for this course-style transactional saga |

### Why MassTransit?
- Simplifies consumer registration and message configuration.
- Automatically propagates headers and supports retry policies.
- Works smoothly with RabbitMQ and C# service code.

## 6. Saga and Compensation

The system implements a distributed saga for order processing:
1. `OrderService` publishes `OrderPlaced`.
2. `InventoryService` consumes `OrderPlaced` and attempts reservation.
3. If success: publishes `InventoryReserved`.
4. If failure: publishes `InventoryRejected`.
5. `OrderService` updates order state accordingly.
6. `NotificationService` sends either confirmation or rejection notification.

### Compensation path
- If inventory cannot be reserved, the saga does not commit the order.
- The order status becomes `Rejected`.
- A rejection notification is sent.

This satisfies the requirement for both happy and compensation paths.

## 7. Correlation ID Tracing

The system propagates `X-Correlation-ID` across:
- API Gateway incoming HTTP requests
- HTTP responses from services
- RabbitMQ message headers between services
- Logs in Seq

This allows one fully-traced correlation ID to be followed from the gateway through the entire saga.

## 8. Demo Evidence Checklist

To prove the system works, capture screenshots of:
1. **Saga happy path**
   - Seq logs showing `OrderPlaced`, `InventoryReserved`, `OrderFinalized`, and `NotificationService` confirmation.
   - Final order status marked as `Confirmed`.
2. **Saga compensation path**
   - Seq logs showing `InventoryRejected`, `OrderService` marking order as `Rejected`, and notification rejection flow.
3. **Cache hit / cache miss**
   - Seq logs showing a `Cache MISS` on first product GET and a `Cache HIT` on second GET for the same product.
4. **Fully traced correlation ID**
   - Seq filtered by the same `CorrelationId` across at least two services.
   - Show request from gateway, message propagation, and final log entries.

## 9. One-command startup

From the repository root, run:

```bash
docker-compose up --build -d
```

Verify with:

```bash
docker-compose ps
```

This is the single command required to start the entire system.

## 10. Related files

- `docker-compose.yml` — full service orchestration
- `README.md` — root project guide and deliverables list
- `QUICK-START.md` — quick run and test commands
- `FINAL-SUBMISSION-CHECKLIST.md` — grading checklist and evidence hints
