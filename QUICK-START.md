# 🚀 QUICK START REFERENCE

## ONE COMMAND TO START EVERYTHING

```bash
cd c:\Users\s0583\Desktop\architacture\Architecture-project
docker-compose up --build -d
```

Wait 30 seconds, then verify all services are running:

```bash
docker-compose ps
```

---

## GATEWA Y ENDPOINTS (All via http://localhost:8080)

| Method | Endpoint | Service | Purpose |
|--------|----------|---------|---------|
| POST | `/api/products` | ProductCatalog | Create product |
| GET | `/api/products` | ProductCatalog | List all products |
| GET | `/api/products/{id}` | ProductCatalog | Get single product (CACHED) |
| POST | `/api/orders` | Order | Create order (triggers saga) |
| GET | `/api/orders` | Order | List orders |
| GET | `/api/orders/{id}` | Order | Get order details |
| GET | `/api/bff/order-details/{id}` | ApiGateway | BFF - aggregated order + product |
| GET | `/api/inventories` | Inventory | List inventory |
| POST | `/api/notifications` | Notification | (Internal only) |
| GET | `/health` | All | Service health check |

---

## DASHBOARDS & MONITORING

| Dashboard | URL | Purpose | Credentials |
|-----------|-----|---------|-------------|
| **Seq Logs** | http://localhost:5341 | Log aggregation + search | None |
| **RabbitMQ** | http://localhost:15672 | Message broker management | guest/guest |
| **Swagger** | http://localhost:8080/swagger | API documentation | None |
| **Mongo Express** | (Future) | MongoDB UI | (Optional) |
| **Redis CLI** | `docker exec redis redis-cli` | Redis commands | None |

---

## TEST SCENARIOS

### 1. Happy Path (Order Confirmation)

```bash
# STEP 1: Create a product
curl -X POST http://localhost:8080/api/products \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Microservices Book",
    "category": "Books",
    "price": 59.99,
    "donorId": 1,
    "attributes": {"author": "Morgan Bruce", "pages": "480"}
  }' | jq '.id'

# Use the returned product ID (e.g., 1) in next step

# STEP 2: Create order
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 1,
    "items": [
      {"productId": 1, "quantity": 2, "price": 59.99}
    ]
  }'

# STEP 3: View order
curl http://localhost:8080/api/orders/1

# STEP 4: Get aggregated BFF data
curl http://localhost:8080/api/bff/order-details/1

# STEP 5: Check saga flow in Seq
# Open http://localhost:5341
# Search for: correlationId, OrderPlaced, InventoryReserved, OrderFinalized
```

### 2. Cache Demonstration

```bash
# FIRST CALL - Cache MISS
time curl http://localhost:8080/api/products/1

# SECOND CALL - Cache HIT (should be faster)
time curl http://localhost:8080/api/products/1

# View logs to confirm cache behavior
# Seq: http://localhost:5341
# Search: "Cache MISS" vs "Cache HIT"
```

### 3. Load Balancing Test

```bash
# Call product endpoint repeatedly
for i in {1..5}; do
  echo "=== Request $i ==="
  curl -v http://localhost:8080/api/products 2>&1 | grep "X-Container\|HTTP"
done

# Should see traffic distributed to both replicas
```

### 4. Failure Path (Out of Stock)

```bash
# Try to order more than available inventory
curl -X POST http://localhost:8080/api/orders \
  -H "Content-Type: application/json" \
  -d '{
    "userId": 2,
    "items": [
      {"productId": 1, "quantity": 99999, "price": 59.99}
    ]
  }'

# In Seq logs, you should see:
# [OrderService] Publishing OrderPlaced
# [InventoryService] Reservation failed
# [InventoryService] Publishing InventoryRejected (compensation)
# [OrderService] Status = Rejected
# [NotificationService] Sending rejection email
```

### 5. Service Communication Test

```bash
# Test direct service communication (via gateway)
curl http://localhost:8080/api/orders  # ✅ Works
curl http://localhost:5001/api/orders  # ❌ Should fail (not exposed)

# Only gateway port is exposed for external clients
```

---

## IMPORTANT FILES

| File | Purpose | Edit? |
|------|---------|-------|
| `docker-compose.yml` | Service orchestration | After first run only |
| `appsettings.json` (each service) | Configuration | For production deployments |
| `FINAL-SUBMISSION-CHECKLIST.md` | Complete documentation | Reference only |
| `ADR-*.md` | Architecture decisions | Reference only |
| `Program.cs` (each service) | Startup & DI | Production code |

---

## TROUBLESHOOTING

### Services throw connection errors
```bash
# Services need ~20 seconds to start
# Check individual service logs
docker-compose logs order-service
docker-compose logs rabbitmq

# If still failing, rebuild
docker-compose down -v
docker-compose up --build -d
```

### Cache not working
```bash
# Verify Redis is running
docker-compose exec redis ping
docker-compose logs redis
```

### Can't see logs in Seq
```bash
# Verify Seq is accessible
curl http://localhost:5341/api/health

# Check service is logging
docker-compose logs api-gateway | grep "Seq"
```

### RabbitMQ issues
```bash
# Check brokectivity
docker-compose exec rabbitmq rabbitmqctl status

# View active channels
docker-compose exec rabbitmq rabbitmqctl list_channels
```

### Database connection errors
```bash
# Check SQL Server health
docker-compose exec order-db sqlcmd -S localhost -U sa -P YourStrong!Password -Q "SELECT 1"

# Check MongoDB
docker-compose exec mongo mongosh --eval "db.adminCommand('ping')"
```

---

## CLEANUP

```bash
# Stop all services
docker-compose down

# Remove volumes (WARNING: deletes data)
docker-compose down -v

# Remove all containers, images, networks
docker-compose down -v --rmi all
```

---

## PERFORMANCE NOTES

- **First startup:** 30-45 seconds (database migrations)
- **Subsequent startups:** 15-20 seconds
- **Cache TTL:** 1 hour (configurable)
- **Log retention:** Daily rolling files
- **RabbitMQ connections:** 1 per service (shared with MassTransit)

---

## NEXT STEPS FOR PRODUCTION

1. **Authentication:** Add JWT/OAuth2 at gateway
2. **Authorization:** Implement role-based access control
3. **API Versioning:** Add version routing at gateway
4. **Rate Limiting:** Implement request throttling
5. **Monitoring:** Add Prometheus metrics
6. **Tracing:** Integrate OpenTelemetry for distributed tracing
7. **Database:** Add indexing for critical queries
8. **Caching:** Implement cache invalidation patterns
9. **Security:** Enable TLS/HTTPS everywhere
10. **Scaling:** Kubernetes deployment manifests

---

**Status: ✅ READY FOR SUBMISSION**
