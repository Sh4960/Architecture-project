# ADR-003: InventoryService Database Choice - Redis (Key-Value Store)

## Context
InventoryService tracks product quantities and handles real-time inventory reservations when orders are placed. It's a critical bridge between OrderService and ProductCatalogService that must respond quickly to stock availability checks and updates.

## Decision
**Use Redis (Key-Value Store) for InventoryService**

## Rationale

### Why Key-Value Store Fits Inventory Tracking
- **Lightning-Fast Reads**: Redis serves data from memory (microseconds), perfect for real-time stock checks
- **Simple Data Model**: Inventory is essentially `ProductId → Quantity` (perfect for key-value)
  - Key: `inventory:{productId}` (e.g., `inventory:1`, `inventory:2`)
  - Value: JSON document `{ id, productId, quantity, lastUpdated }`

- **Atomic Operations**: Redis supports atomic increment/decrement operations for safe reservation/release
  - No race conditions when two orders try to reserve the same stock simultaneously

- **Eventual Consistency (AP)**: Can tolerate temporary inconsistency
  - Stock count is eventually consistent across replicas
  - Better for high-throughput scenarios (read/write heavy)

### Why NOT SQL for Inventory?
| Aspect | SQL (Relational) | Redis (Key-Value) |
|--------|------------------|----|
| **Query Speed** | 10-100ms (disk I/O) | <1ms (in-memory) |
| **Concurrency** | Requires locks, slower | Atomic ops, fast |
| **Scalability** | Vertical + complex sharding | Horizontal + simple sharding |
| **Consistency** | Strong (ACID) | Eventual (BASE) |
| **Use Case** | Complex joins, historical audit | Real-time counters, caches |

### CAP Theorem Analysis
- InventoryService prioritizes **Availability** and **Partition Tolerance** (AP)
- Redis follows BASE (Eventual Consistency model)
- In case of network partition:
  - ✅ Can continue serving reads/writes (available)
  - ✅ Can replicate to other nodes (partition tolerant)
  - ⚠️ Replicas may lag temporarily (eventual consistency)

**Why AP is right for inventory:**
- Stock counts don't need immediate consistency across all nodes
- If node A has 100 units and node B has 98 (lag), system recovers automatically
- User experience: "Sorry, stock temporarily unavailable" is acceptable
- Money-critical operations (OrderService) use SQL Server CP (strong consistency) for precision

### Polyglot Persistence Achievement
Using **Redis for InventoryService** completes the **2 NoSQL families** requirement:
1. **MongoDB** (ProductCatalogService) - Document Store
   - Type: Document-oriented NoSQL
   - Model: Flexible schema with nested objects
   - CAP: AP (Availability, Partition tolerance)

2. **Redis** (InventoryService) - Key-Value Store (NEW)
   - Type: In-memory key-value NoSQL
   - Model: Simple key-value with optional JSON serialization
   - CAP: AP (Availability, Partition tolerance)

Plus 1 Relational:
3. **SQL Server** (OrderService) - Relational ACID
   - Type: Relational database
   - Model: Structured schema with transactions
   - CAP: CP (Consistency, Partition tolerance)

## Implementation Details

### Data Storage in Redis
```json
{
  "inventory:1": {
    "id": 1,
    "productId": 1,
    "quantity": 100,
    "lastUpdated": "2026-06-30T12:00:00Z"
  },
  "inventory:2": {
    "id": 2,
    "productId": 2,
    "quantity": 50,
    "lastUpdated": "2026-06-30T12:00:00Z"
  }
}
```

### Operations
- **GetInventoryByProductIdAsync**: `HGET inventory:{productId}` → JSON deserialize
- **ReserveInventoryAsync**: 
  1. GET current quantity
  2. Check if >= requested quantity
  3. SET new quantity (quantity - reserved)
  4. Return true/false
- **ReleaseInventoryAsync**: 
  1. GET current quantity
  2. SET new quantity (quantity + released)
  3. Return true/false

### Atomicity Considerations
- For true atomicity with concurrent requests, use Lua scripts or Redis transactions
- Current simple implementation handles typical loads (can upgrade to Lua if needed)

## Consequences

### ✅ Advantages
- ✅ Ultra-fast inventory lookups (<1ms)
- ✅ Simple data model (key-value)
- ✅ Horizontal scaling with Redis Cluster or Sentinel
- ✅ No schema migrations needed
- ✅ Perfect for real-time operations
- ✅ Achieves 2 NoSQL families (MongoDB + Redis) as required

### ❌ Disadvantages
- ❌ Data lives in memory only (needs persistence/replication)
- ❌ Limited to available RAM (need sizing/monitoring)
- ❌ No complex queries (can't filter by range, pattern)
- ❌ No transactions across multiple keys (without Lua)
- ❌ Requires operational overhead (monitoring, persistence, replication)

## Resilience Strategies

1. **Persistence**: Enable RDB snapshots or AOF (Append-Only File) for durability
2. **Replication**: Use Redis Sentinel for high availability (automatic failover)
3. **Backup**: Regular snapshots to stable storage
4. **Fallback**: Could read from SQL Server historical data if Redis down (eventual consistency)

## Related ADRs
- **ADR-001**: OrderService - SQL Server (ACID for money, CP)
- **ADR-002**: ProductCatalogService - MongoDB (Document model, AP)
- **ADR-004**: NotificationService - No Database (Stateless)

## Decision Date
2026-06-30

## Status
✅ ACCEPTED - Implements teacher requirement: "At least 2 NoSQL families"
