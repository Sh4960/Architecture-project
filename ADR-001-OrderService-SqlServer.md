# ADR-001: OrderService Database Choice - SQL Server (Relational)

## Context
OrderService manages the core business transactions - placing and confirming orders. This involves monetary transactions that require strong consistency guarantees, multi-row transactions, and compliance with ACID principles.

## Decision
**Use SQL Server (Relational Database) for OrderService**

## Rationale

### Why ACID is Critical for Orders
- **Atomicity**: An order must be created with ALL its items together, or NOT at all. Partial orders are not acceptable.
- **Consistency**: The total price must always match the sum of items. Double-charging or losing money due to inconsistency is unacceptable.
- **Isolation**: Two concurrent order placements must not interfere with each other.
- **Durability**: Once an order is confirmed, it must persist permanently.

### Why NOT NoSQL for Orders
| Aspect | SQL | NoSQL (MongoDB/Cassandra) |
|--------|-----|--------------------------|
| **Transactions** | Multi-row, multi-document ACID | Limited or eventual consistency |
| **Financial Data** | ✅ Gold standard | ❌ Risky |
| **Joins** | Natural (Order → Items → Products) | Document bloat or denormalization |
| **Consistency Model** | Strong ACID | BASE (eventual consistency) |
| **Compliance** | ✅ Audit trails, reconciliation | ⚠️ Harder to audit |

## CAP Theorem Analysis
- OrderService prioritizes **Consistency** and **Availability**, tolerating minimal Partition tolerance
- SQL Server guarantees CP (Consistency, Partition tolerance)
- This is correct for financial systems

## Consequences
- ✅ Guaranteed data integrity
- ✅ Multi-row transactions
- ✅ Strong audit trail
- ❌ Horizontal scaling is more complex
- ❌ Not ideal for real-time analytics at massive scale

## Related ADRs
- ADR-002: ProductCatalogService - MongoDB (Document Model, AP)
- ADR-003: InventoryService - Redis (Key-Value, AP)
- ADR-004: NotificationService - No Database (Stateless, AP)
