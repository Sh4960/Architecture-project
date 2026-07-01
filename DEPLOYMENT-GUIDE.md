# Phase 2 - Deployment & Testing Guide

## ✅ Checkpoint: Phase 2 Complete

All components of Phase 2 have been created:
- [x] **Task 2.1**: 4 Microservices (OrderService, ProductCatalogService, InventoryService, NotificationService)
- [x] **Task 2.2**: Database-per-Service pattern fully implemented
- [x] **Task 2.3**: Polyglot Persistence (SQL Server, MongoDB, PostgreSQL, Stateless)
- [x] **Task 2.4**: 4 Architecture Decision Records (ADRs)

---

## Quick Start

### Prerequisites
- Docker & Docker Compose installed
- .NET 8 SDK (for local development)
- Git

### Start the System

```bash
# Navigate to project directory
cd WebApiProject

# Build and start all services
docker-compose up --build -d

# Verify all services are running
docker-compose ps
```

Expected output:
```
NAME                        STATUS         PORTS
order-db                    healthy        1433/tcp
order-service               running        0.0.0.0:5001→80/tcp
mongo                       running        0.0.0.0:27017→27017/tcp
product-catalog-service     running        0.0.0.0:5002→80/tcp
inventory-db                healthy        5432/tcp
inventory-service           running        0.0.0.0:5003→80/tcp
notification-service        running        0.0.0.0:5004→80/tcp
api-gateway                 running        0.0.0.0:8080→80/tcp
```

---

## Testing the System

### 1. Health Check (API Gateway)

```bash
curl -v http://localhost:8080/api/products
```

Should return `200 OK` (even if empty).

---

### 2. Full Order Workflow

#### Step A: Create a Product

```bash
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Microservices in Action",
    "category": "Books",
    "price": 59.99,
    "donorId": 1,
    "attributes": {
      "author": "Morgan Bruce",
      "isbn": "1617294543",
      "pages": 480
    }
  }'
```

**Response:**
```json
{
  "id": "507f1f77bcf86cd799439011",
  "name": "Microservices in Action",
  "category": "Books",
  "price": 59.99,
  "donorId": 1,
  "isRaffled": false,
  "createdAt": "2026-06-30T10:00:00Z",
  "attributes": {
    "author": "Morgan Bruce",
    "isbn": "1617294543",
    "pages": 480
  }
}
```

**Store the Product ID** from response (e.g., `507f1f77bcf86cd799439011`)

---

#### Step B: Check Inventory

```bash
curl http://localhost:8080/api/inventories/1
```

**Response:**
```json
{
  "id": 1,
  "productId": 1,
  "quantity": 100,
  "lastUpdated": "2026-06-30T00:00:00Z"
}
```

---

#### Step C: Place an Order

```bash
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 123,
    "items": [
      {
        "productId": 1,
        "quantity": 2,
        "price": 59.99
      }
    ]
  }'
```

**Response:**
```json
{
  "id": 1,
  "userId": 123,
  "createdAt": "2026-06-30T10:05:00Z",
  "status": "Pending",
  "totalPrice": 119.98,
  "items": [
    {
      "id": 1,
      "orderId": 1,
      "productId": 1,
      "quantity": 2,
      "price": 59.99
    }
  ]
}
```

**Store the Order ID** from response (e.g., `1`)

---

#### Step D: Reserve Inventory

```bash
curl -X POST http://localhost:8080/api/inventories/reserve \
  -H "Content-Type: application/json" \
  -d '{
    "productId": 1,
    "quantity": 2
  }'
```

**Response:**
```
true
```

Verify inventory decreased:
```bash
curl http://localhost:8080/api/inventories/1
# Quantity should now be 98
```

---

#### Step E: Confirm Order & Send Notification

Confirm order:
```bash
curl -X PUT http://localhost:8080/api/orders/1/confirm
```

Send confirmation email notification:
```bash
curl -X POST http://localhost:8080/api/notifications/order-confirmed \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 123,
    "orderId": 1,
    "email": "customer@example.com"
  }'
```

**Response:**
```json
"Notification sent"
```

Check logs to see email simulation:
```bash
docker-compose logs notification-service | grep "Email would be sent"
```

---

### 3. Get All Orders for a User

```bash
curl http://localhost:8080/api/orders/user/123
```

---

### 4. Test Product Catalog by Category

List all Books:
```bash
curl http://localhost:8080/api/products/category/Books
```

---

## Service Endpoints Reference

### OrderService (Port 5001)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/orders` | Create new order |
| GET | `/api/orders/{id}` | Get order by ID |
| GET | `/api/orders/user/{userId}` | Get user's orders |
| PUT | `/api/orders/{id}/confirm` | Confirm order |
| PUT | `/api/orders/{id}/reject` | Reject order |

### ProductCatalogService (Port 5002)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/products/{id}` | Get product by ID |
| GET | `/api/products/category/{category}` | Get products by category |
| POST | `/api/products` | Create new product |
| PUT | `/api/products/{id}` | Update product |

### InventoryService (Port 5003)
| Method | Endpoint | Description |
|--------|----------|-------------|
| GET | `/api/inventories/{productId}` | Get inventory |
| POST | `/api/inventories/reserve` | Reserve stock |
| POST | `/api/inventories/release` | Release stock |
| GET | `/api/inventories/check/{productId}/{quantity}` | Check availability |

### NotificationService (Port 5004)
| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/api/notifications/order-confirmed` | Send confirmation email |
| POST | `/api/notifications/order-rejected` | Send rejection email |

---

## Direct Service Access (Bypass API Gateway)

Instead of using the gateway on `localhost:8080`, you can call services directly:

```bash
# OrderService directly
curl http://localhost:5001/api/orders

# ProductCatalogService directly
curl http://localhost:5002/api/products

# InventoryService directly
curl http://localhost:5003/api/inventories

# NotificationService directly
curl http://localhost:5004/api/notifications
```

---

## Swagger Documentation

Each service exposes Swagger UI:

- **OrderService**: http://localhost:5001/swagger
- **ProductCatalogService**: http://localhost:5002/swagger
- **InventoryService**: http://localhost:5003/swagger
- **NotificationService**: http://localhost:5004/swagger

---

## Viewing Logs

```bash
# All services
docker-compose logs -f

# Specific service
docker-compose logs -f order-service
docker-compose logs -f product-catalog-service
docker-compose logs -f inventory-service
docker-compose logs -f notification-service

# Follow new logs only
docker-compose logs -f --tail=50
```

---

## Database Access

### SQL Server (OrderService)
```bash
# Connect with SQL Server Management Studio or sqlcmd
sqlcmd -S localhost,1433 -U sa -P "YourStrong!Password"

# Query orders
SELECT * FROM Orders;
SELECT * FROM OrderItems;
```

### MongoDB (ProductCatalogService)
```bash
# Access Mongo Shell
docker-compose exec mongo mongosh -u admin -p adminpassword

# Commands
use ProductCatalogDB
db.Products.find()
db.Products.count()
```

### PostgreSQL (InventoryService)
```bash
# Access PostgreSQL
docker-compose exec inventory-db psql -U postgres -d InventoryDB

# Queries
SELECT * FROM "Inventories";
```

---

## Stopping & Cleaning Up

```bash
# Stop all services (keep volumes)
docker-compose stop

# Stop and remove containers
docker-compose down

# Full cleanup (remove volumes too)
docker-compose down -v
```

---

## Common Issues & Fixes

### Error: "Service 'order-db' failed to start"
```
→ Docker not running or insufficient resources
→ Run: docker-compose down && docker-compose up --build
```

### Error: "MongoDB connection refused"
```
→ Mongo service not ready yet
→ Retry after 10 seconds, or check: docker-compose logs mongo
```

### Error: "PostgreSQction refused"
```
→ Wait 15 seconds for PostgreSQL to initialize
→ Check: docker-compose logs inventory-db
```

### Order creation returns 500 error
```
→ Check OrderService logs: docker-compose logs order-service
→ Verify SQL Server is healthy: docker-compose logs order-db
```

### Products query returns empty
```
→ Create a product first (Step A above)
→ Verify MongoDB is running: docker-compose ps mongo
```

---

## Performance Notes

- **First request takes ~5 seconds**: EF Core migrations running on startup
- **MongoDB indexes**: Not created yet (Phase 3 optimization)
- **Connection pooling**: Enabled by default
- **Caching**: Not implemented (Phase 4)

---

## Monitoring & Health

For Phase 2, logging is done via:
1. **Serilog** → Console output
2. **Serilog** → Log files in `Logs/` directory
3. **Docker logs** → `docker-compose logs`

In Phase 3, we'll add:
- ELK Stack (Elasticsearch, Logstash, Kibana)
- Prometheus metrics
- Jaeger distributed tracing

---

## Architecture Diagrams

See [PHASE2-README.md](PHASE2-README.md) for system architecture, database schema diagrams, and ADRs.

---

## Next Phase

Ready for **Phase 3: Async Messaging & Saga Pattern**?
- Introduce RabbitMQ or Kafka
- Implement Saga pattern for distributed transactions
- Add circuit breakers for resilience
- Service discovery (Consul or Eureka)

---

## Support

For issues or questions, check:
1. [PHASE2-README.md](PHASE2-README.md) - Architecture overview
2. [ADR-001 through ADR-004](.) - Database decisions
3. Service logs: `docker-compose logs SERVICE_NAME`
