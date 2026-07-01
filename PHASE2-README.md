# Phase 2: Microservices Architecture

## Overview
This is the evolution of the monolithic e-commerce system into a **production-grade microservices architecture** with polyglot persistence (multiple database types).

```
┌─────────────────────────────────────────────────────────────────┐
│                     NGINX API Gateway (Port 8080)              │
│                   Reverse Proxy & Load Balancer                 │
└──────┬──────────────────┬──────────────────────────┬────────────┘
       │                  │                          │
       ▼                  ▼                          ▼
┌──────────────┐  ┌──────────────────┐  ┌──────────────────┐
│ ORDER        │  │ PRODUCT CATALOG  │  │ INVENTORY        │
│ SERVICE      │  │ SERVICE          │  │ SERVICE          │
│ Port 5001    │  │ Port 5002        │  │ Port 5003        │
│              │  │                  │  │                  │
│ REST APIs:   │  │ REST APIs:       │  │ REST APIs:       │
│ - POST /api/ │  │ - GET /api/      │  │ - GET /api/      │
│   orders     │  │   products       │  │   inventories    │
│ - GET /api/  │  │ - POST /api/     │  │ - POST /api/     │
│   orders/id  │  │   products       │  │   inventories/   │
│              │  │ - PUT /api/      │  │   reserve        │
└──────┬───────┘  │   products/id    │  │ - POST /api/     │
       │          └─────────┬────────┘  │   inventories/   │
       │                    │            │   release        │
       ▼                    ▼            ▼
┌──────────────────┐  ┌──────────────┐  ┌──────────────────┐
│   SQL Server     │  │   MongoDB    │  │   PostgreSQL     │
│   (Order DB)     │  │   (Products) │  │   (Inventory DB) │
│ - Orders         │  │ - Products   │  │ - Inventories    │
│ - OrderItems     │  │   (flexible  │  │   (simple schema)│
│                  │  │   schema)    │  │                  │
└──────────────────┘  └──────────────┘  └──────────────────┘

       │
       ▼
┌──────────────────────────────────────┐
│  NOTIFICATION SERVICE (Port 5004)   │
│  - Stateless (No Database)          │
│  - Sends Emails                     │
│  - REST API: POST /api/notifications│
└──────────────────────────────────────┘
```

## Services

### 1. OrderService (SQL Server - Relational)
**Database**: SQL Server  
**Ports**: 5001 (API), 1433 (DB)  
**Responsibilities**:
- Place orders
- Confirm/Reject orders
- Store order history
- ACID transactions for financial data

**Endpoints**:
```
POST   /api/orders                    → Create order
GET    /api/orders/{id}               → Get order details
GET    /api/orders/user/{userId}      → List user's orders
PUT    /api/orders/{id}/confirm       → Confirm order
PUT    /api/orders/{id}/reject        → Reject order
```

**Why SQL Server?** → Financial data requires ACID, multi-row transactions, strong consistency. See [ADR-001](ADR-001-OrderService-SqlServer.md)

---

### 2. ProductCatalogService (MongoDB - Document Store)
**Database**: MongoDB  
**Ports**: 5002 (API), 27017 (DB)  
**Responsibilities**:
- Manage product catalog
- Support flexible schemas (different attributes per category)
- Browse products by category
- Store product metadata

**Endpoints**:
```
GET    /api/products/{id}             → Get product details
GET    /api/products/category/{name}  → List products by category
POST   /api/products                  → Create new product
PUT    /api/products/{id}             → Update product
```

**Why MongoDB?** → Product catalog has varying attributes per category (books, electronics, clothing). Flexible schema fits document model perfectly. See [ADR-002](ADR-002-ProductCatalog-MongoDB.md)

---

### 3. InventoryService (PostgreSQL - Relational)
**Database**: PostgreSQL  
**Ports**: 5003 (API), 5432 (DB)  
**Responsibilities**:
- Track product quantities
- Reserve inventory when orders are placed
- Release inventory when orders are cancelled
- Check availability

**Endpoints**:
```
GET    /api/inventories/{productId}           → Get inventory
POST   /api/inventories/reserve               → Reserve stock
POST   /api/inventories/release               → Release stock
GET    /api/inventories/check/{productId}/{qty} → Check availability
```

**Why PostgreSQL?** → Lightweight relational database, perfect for simple schema. Different from SQL Server demonstrates polyglot persistence. See [ADR-003](ADR-003-Inventory-PostgreSQL.md)

---

### 4. NotificationService (No Database - Stateless)
**Ports**: 5004 (API)  
**Responsibilities**:
- Send order confirmation emails
- Send order rejection emails
- No database (stateless, horizontally scalable)

**Endpoints**:
```
POST   /api/notifications/order-confirmed     → Send confirmation email
POST   /api/notifications/order-rejected      → Send rejection email
```

**Why Stateless?** → Notifications are ephemeral. Enables unlimited horizontal scaling. See [ADR-004](ADR-004-NotificationService-Stateless.md)

---

## Database-per-Service Pattern

Each service owns its database. **NO cross-service database access**.

| Service | Database | Type | Connection |
|---------|----------|------|-----------|
| OrderService | Order DB (SQL Server) | Relational, ACID | `Server=order-db;Database=OrderDB;...` |
| ProductCatalogService | ProductCatalog (MongoDB) | Document Store | `mongodb://admin:pass@mongo:27017` |
| InventoryService | Inventory DB (PostgreSQL) | Relational | `Host=inventory-db;Database=InventoryDB;...` |
| NotificationService | None | Stateless | N/A |

## Polyglot Persistence

We use **3 different database families** in Phase 2:

1. **Relational Databases**
   - SQL Server (OrderService) - Strong ACID for financial data
   - PostgreSQL (InventoryService) - Lightweight relational
   
2. **Document Store**
   - MongoDB (ProductCatalogService) - Flexible schema for varying product types

3. **No Database**
   - NotificationService - Stateless, horizontally scalable

**Why this mix?**
- **Right tool for the job** vs one-size-fits-all monolith
- **Scalability**: Each service optimized for its workload
- **Independent scaling**: Scale ProductCatalogService for reads, OrderService for transactions
- **Team learning**: Exposure to multiple database paradigms

---

## Running the System

### Prerequisites
- Docker & Docker Compose installed
- Git (to clone the repository)

### Start All Services
```bash
docker-compose up -d
```

This will:
1. Create SQL Server instance for OrderService
2. Create MongoDB instance for ProductCatalogService
3. Create PostgreSQL instance for InventoryService
4. Start all 4 microservices
5. Start Nginx API Gateway

### Verify Services are Running
```bash
# API Gateway
curl http://localhost:8080/api/products

# Direct service calls
curl http://localhost:5001/api/orders          # OrderService
curl http://localhost:5002/api/products        # ProductCatalogService
curl http://localhost:5003/api/inventories     # InventoryService
curl http://localhost:5004/api/notifications   # NotificationService
```

### View Logs
```bash
docker-compose logs -f order-service
docker-compose logs -f product-catalog-service
docker-compose logs -f inventory-service
docker-compose logs -f notification-service
```

### Stop Everything
```bash
docker-compose down
```

---

## API Workflow Example

### 1. Create a Product (ProductCatalogService)
```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "C# Book",
    "category": "Books",
    "price": 49.99,
    "donorId": 1,
    "attributes": {"author": "Richter", "pages": 1400}
  }'
```

### 2. Check Inventory (InventoryService)
```bash
curl http://localhost:8080/api/inventories/1
# Response: {"id": 1, "productId": 1, "quantity": 100}
```

### 3. Place an Order (OrderService)
```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "items": [{"productId": 1, "quantity": 2, "price": 49.99}]
  }'
# Response: {"id": 1, "userId": 1, "status": "Pending", "totalPrice": 99.98}
```

### 4. Reserve Inventory (InventoryService)
```bash
curl -X POST http://localhost:8080/api/inventories/reserve \
  -H "Content-Type: application/json" \
  -d '{"productId": 1, "quantity": 2}'
```

### 5. Confirm Order & Send Email (OrderService + NotificationService)
```bash
curl -X PUT http://localhost:8080/api/orders/1/confirm

# Trigger notification manually (Phase 3: will be async via message queue)
curl -X POST http://localhost:8080/api/notifications/order-confirmed \
  -H "Content-Type: application/json" \
  -d '{"userId": 1, "orderId": 1, "email": "customer@example.com"}'
```

---

## Architecture Decision Records (ADRs)

- [ADR-001: OrderService - SQL Server](ADR-001-OrderService-SqlServer.md)
- [ADR-002: ProductCatalogService - MongoDB](ADR-002-ProductCatalog-MongoDB.md)
- [ADR-003: InventoryService - PostgreSQL](ADR-003-Inventory-PostgreSQL.md)
- [ADR-004: NotificationService - Stateless](ADR-004-NotificationService-Stateless.md)

---

## Phase 2 Checklist

- [x] **Task 2.1**: Split monolith into 4 services (OrderService, ProductCatalogService, InventoryService, NotificationService)
- [x] **Task 2.2**: Database-per-service pattern implemented
- [x] **Task 2.3**: Polyglot persistence with 3 database families + documentation
- [x] **Task 2.4**: ADRs written for each database choice

---

## Next Steps (Phase 3)

- [ ] Add async messaging (RabbitMQ/Kafka) for inter-service communication
- [ ] Implement Saga pattern for distributed transactions
- [ ] Add service discovery & health checks
- [ ] Implement circuit breakers for resilience
- [ ] Add Redis caching layer
- [ ] Enhance API Gateway with rate limiting

---

## Troubleshooting

### Service won't start?
```bash
docker-compose logs service-name
# Check connection strings in appsettings.json
```

### Database connection errors?
```bash
# Verify databases are running
docker-compose ps

# Check database credentials
# SQL Server: sa / YourStrong!Password
# MongoDB: admin / adminpassword
# PostgreSQL: postgres / postgres123
```

### API Gateway not routing?
```bash
# Verify nginx.conf syntax
docker exec nginx nginx -t

# Check service names match docker-compose service names
```

---

## Metrics & Monitoring

**Current (Phase 2)**:
- Serilog logs to console and files
- Swagger documentation on each service

**Phase 3 additions**:
- Centralized logging (ELK Stack)
- Distributed tracing (Jaeger)
- Metrics collection (Prometheus)
- Health check endpoints

---

## Credits & References

- [ADR Format](https://adr.github.io/)
- [CAP Theorem](https://en.wikipedia.org/wiki/CAP_theorem)
- [Database-per-Service Pattern](https://microservices.io/patterns/data/database-per-service.html)
- [Polyglot Persistence](https://martinfowler.com/bliki/PolyglotPersistence.html)
