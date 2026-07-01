# ADR-004: NotificationService - No Database (Stateless)

## Context
NotificationService sends emails when orders are confirmed or rejected. It doesn't need to store persistent state for its core function - it processes notifications and sends them.

## Decision
**NotificationService has NO persistent database. It is stateless.**

## Rationale

### Why Stateless is Better for Notifications
1. **Stateless = Scalable**: Can spin up 1, 10, or 100 instances without coordination
2. **No Database Locks**: No bottlenecks, horizontal scaling is truly horizontal
3. **Simple Failure Recovery**: If a notification fails, retry from the source (OrderService)
4. **Immutable Data**: Notification parameters come from OrderService request, don't need to be stored

### Alternative Considered: Notification History DB
- **Rejected**: Logging/auditing can be centralized in ELK/Observability stack (Phase 6)
- **Rejected**: Notification retry logic belongs in Message Queue (Phase 3), not this service
- **Rejected**: Data bloat - notifications are ephemeral

### CAP Theorem Analysis
- NotificationService is **AP** (Availability, Partition tolerance)
- If OrderService can't reach NotificationService, OrderService retries or queues for later (Phase 3)
- No data consistency concerns because no shared data

## Comparison: Stateless vs Stateful Services
| Aspect | Stateless (No DB) | Stateful (With DB) |
|--------|-------------------|--------------------|
| **Instances** | 100 instances, no coordination | Replication, failover complexity |
| **Scaling** | Add container | Add container + database |
| **Failure** | Retry from OrderService | Complex recovery, transaction logs |
| **Testing** | Unit tests only | Mocking DB required |

## Polyglot Persistence Achievement
This ADR completes the **polyglot persistence goal**:
- **OrderService** → SQL Server (ACID, relational, monetary) - CP
- **ProductCatalogService** → MongoDB (document, NoSQL) - AP
- **InventoryService** → Redis (key-value, NoSQL) - AP
- **NotificationService** → No DB (stateless, scalable) - AP

We've now used **2 NoSQL families** (MongoDB + Redis) + 1 Relational (SQL Server) + 1 Stateless:
✅ **Polyglot Persistence Complete**

## Consequences
- ✅ Horizontally scalable (true cloud-native design)
- ✅ No database bottleneck
- ✅ Simpler to test and debug
- ✅ Lower infrastructure cost
- ❌ Can't query notification history (design for auditing elsewhere)
- ❌ Requires async messaging in Phase 3 for reliability

## Implementation Notes
- Log all notifications to Serilog (files + console)
- Centralize notification templates in ConfigMap or configuration service
- In Phase 3, integrate with RabbitMQ/Kafka for retry logic
- Implement circuit breaker for email service integration

## Related ADRs
- ADR-001: OrderService - SQL Server (ACID for money, CP)
- ADR-002: ProductCatalogService - MongoDB (Document Model, AP)
- ADR-003: InventoryService - Redis (Key-Value, AP)
