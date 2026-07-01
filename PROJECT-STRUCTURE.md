# Project Structure - Phase 2: Microservices

```
WebApiProject/
├── 📁 OrderService/                     # Microservice 1: Order Management (SQL Server)
│   ├── Controllers/
│   │   └── OrdersController.cs          # REST endpoints for orders
│   ├── Models/
│   │   ├── Order.cs                     # Domain model
│   │   └── DTOs/
│   │       └── OrderDTO.cs              # Data transfer objects
│   ├── BLL/
│   │   ├── OrderBLLService.cs           # Business logic
│   │   └── Interfaces/
│   │       └── IOrderBLLService.cs
│   ├── DAL/
│   │   ├── OrderDbContext.cs            # EF Core DbContext (SQL Server)
│   │   ├── OrderDAL.cs                  # Data access layer
│   │   ├── Interfaces/
│   │   │   └── IOrderDAL.cs
│   │   └── Migrations/
│   │       ├── 20260630_InitialCreate.cs
│   │       └── OrderDbContextModelSnapshot.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile                       # Docker build config
│   ├── Program.cs                       # Service startup & DI
│   ├── appsettings.json                 # Configuration
│   └── OrderService.csproj              # NuGet dependencies
│
├── 📁 ProductCatalogService/            # Microservice 2: Product Catalog (MongoDB)
│   ├── Controllers/
│   │   └── ProductsController.cs        # REST endpoints for products
│   ├── Models/
│   │   ├── Product.cs                   # Domain model (MongoDB document)
│   │   └── DTOs/
│   │       └── ProductDTO.cs            # Data transfer objects
│   ├── BLL/
│   │   ├── ProductBLLService.cs         # Business logic
│   │   └── Interfaces/
│   │       └── IProductBLLService.cs
│   ├── DAL/
│   │   ├── ProductDAL.cs                # MongoDB data access
│   │   └── Interfaces/
│   │       └── IProductDAL.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile
│   ├── Program.cs
│   ├── appsettings.json
│   └── ProductCatalogService.csproj
│
├── 📁 InventoryService/                 # Microservice 3: Inventory Management (PostgreSQL)
│   ├── Controllers/
│   │   └── InventoriesController.cs     # REST endpoints for inventory
│   ├── Models/
│   │   ├── Inventory.cs                 # Domain model
│   │   └── DTOs/
│   │       └── InventoryDTO.cs          # Data transfer objects
│   ├── BLL/
│   │   ├── InventoryBLLService.cs       # Business logic
│   │   └── Interfaces/
│   │       └── IInventoryBLLService.cs
│   ├── DAL/
│   │   ├── InventoryDbContext.cs        # EF Core DbContext (PostgreSQL)
│   │   ├── InventoryDAL.cs              # Data access layer
│   │   ├── Interfaces/
│   │   │   └── IInventoryDAL.cs
│   │   └── Migrations/
│   │       ├── 20260630_InitialCreate.cs
│   │       └── InventoryDbContextModelSnapshot.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile
│   ├── Program.cs
│   ├── appsettings.json
│   └── InventoryService.csproj
│
├── 📁 NotificationService/              # Microservice 4: Notifications (Stateless)
│   ├── Controllers/
│   │   └── NotificationsController.cs   # REST endpoints for notifications
│   ├── Models/
│   │   └── Notification.cs              # Email notification model
│   ├── BLL/
│   │   ├── NotificationBLLService.cs    # Business logic (email sending)
│   │   └── Interfaces/
│   │       └── INotificationBLLService.cs
│   ├── Properties/
│   │   └── launchSettings.json
│   ├── Dockerfile
│   ├── Program.cs
│   ├── appsettings.json
│   └── NotificationService.csproj
│
├── 📄 docker-compose.yml                # Orchestration (4 services + 3 databases + gateway)
├── 📄 nginx.conf                        # API Gateway configuration
│
├── 📄 Microservices.sln                 # Visual Studio solution file
│
├── 📄 PHASE2-README.md                  # Phase 2 architecture overview
├── 📄 DEPLOYMENT-GUIDE.md               # How to run & test the system
├── 📄 PROJECT-STRUCTURE.md              # This file
│
├── 📄 ADR-001-OrderService-SqlServer.md           # Database decision for OrderService
├── 📄 ADR-002-ProductCatalog-MongoDB.md          # Database decision for ProductCatalogService
├── 📄 ADR-003-Inventory-PostgreSQL.md            # Database decision for InventoryService
├── 📄 ADR-004-NotificationService-Stateless.md   # Database decision for NotificationService
│
├── 📄 .gitignore                        # Git ignore patterns
├── 📄 .env.example                      # Environment variables template
│
└── 📁 bin, obj/                         # Build outputs (ignored by git)
```

---

## File Descriptions

### Services

#### OrderService (SQL Server)
**Purpose**: Core order management with ACID guarantees
**Database**: SQL Server (relational)
**Key Files**:
- `OrdersController.cs` - POST/GET/PUT endpoints
- `OrderBLLService.cs` - Confirm, reject, create orders
- `OrderDbContext.cs` - SQL Server database context
- `OrderDAL.cs` - Database queries

#### ProductCatalogService (MongoDB)
**Purpose**: Flexible product catalog with varying attributes
**Database**: MongoDB (document store)
**Key Files**:
- `ProductsController.cs` - Product CRUD endpoints
- `ProductBLLService.cs` - Product business logic
- `ProductDAL.cs` - MongoDB queries
- Models use `Dictionary<string, object>` for flexible attributes

#### InventoryService (PostgreSQL)
**Purpose**: Stock tracking and reservation
**Database**: PostgreSQL (relational)
**Key Files**:
- `InventoriesController.cs` - Inventory endpoints
- `InventoryBLLService.cs` - Reserve/release logic
- `InventoryDbContext.cs` - PostgreSQL database context
- `InventoryDAL.cs` - Inventory queries

#### NotificationService (Stateless)
**Purpose**: Email notifications for order events
**Database**: None (stateless service)
**Key Files**:
- `NotificationsController.cs` - Notification endpoints
- `NotificationBLLService.cs` - Email sending logic
- Logs to Serilog, no persistent state

### Configuration Files

**docker-compose.yml**
- Defines 4 services + 3 databases + API Gateway
- Networks: microservices-network for inter-service communication
- Volumes: Persistent storage for databases
- Health checks: Ensures databases are ready before services start

**nginx.conf**
- Routes API requests to appropriate services
- Endpoints:
  - `/api/orders` → OrderService
  - `/api/products` → ProductCatalogService
  - `/api/inventories` → InventoryService
  - `/api/notifications` → NotificationService

**Microservices.sln**
- Visual Studio solution file
- Includes all 4 projects

**Program.cs** (each service)
- Configures logging (Serilog)
- Registers dependencies (BLL, DAL)
- Sets up database context
- Configures Swagger

**appsettings.json** (each service)
- Connection strings
- Logging levels
- Service-specific configuration

### Documentation

**PHASE2-README.md**
- Complete architecture overview
- Database-per-service pattern explanation
- Polyglot persistence rationale
- API workflow examples

**DEPLOYMENT-GUIDE.md**
- Quick start instructions
- Testing workflow (create product → place order → confirm)
- Service endpoints reference
- Troubleshooting guide

**ADR-*.md** (4 files)
- Architecture Decision Records
- Why each database was chosen
- CAP Theorem analysis
- Comparison tables

---

## Database Schema

### OrderService (SQL Server)
```
Orders
├── Id (PK)
├── UserId (FK to external User service)
├── CreatedAt (timestamp)
├── Status (Pending, Confirmed, Rejected)
├── TotalPrice (decimal)
└── Items (1-to-many)

OrderItems
├── Id (PK)
├── OrderId (FK)
├── ProductId (from ProductCatalogService)
├── Quantity
└── Price
```

### ProductCatalogService (MongoDB)
```
Products (collection)
{
  "_id": ObjectId,
  "name": string,
  "category": string,
  "price": decimal,
  "donorId": int,
  "imageUrl": string,
  "isRaffled": boolean,
  "createdAt": DateTime,
  "attributes": { ... } // Flexible per category
}
```

### InventoryService (PostgreSQL)
```
Inventories
├── Id (PK)
├── ProductId (unique, FK to ProductCatalogService)
├── Quantity (int)
└── LastUpdated (timestamp)
```

---

## Dependencies (NuGet Packages)

### All Services
- `Microsoft.AspNetCore.OpenApi` (8.0.0)
- `Swashbuckle.AspNetCore` (6.4.6)
- `AutoMapper.Extensions.Microsoft.DependencyInjection` (12.0.1)
- `Serilog` (3.0.1)
- `Serilog.AspNetCore` (8.0.0)
- `Serilog.Sinks.Console` (5.0.0)
- `Serilog.Sinks.File` (5.0.0)

### OrderService
- `Microsoft.EntityFrameworkCore` (8.0.0)
- `Microsoft.EntityFrameworkCore.SqlServer` (8.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (8.0.0)

### ProductCatalogService
- `MongoDB.Driver` (2.25.0)

### InventoryService
- `Npgsql.EntityFrameworkCore.PostgreSQL` (8.0.0)
- `Microsoft.EntityFrameworkCore` (8.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (8.0.0)

### NotificationService
- `MailKit` (4.5.0) - For future email integration

---

## How Services Communicate

**Phase 2 (Current)**:
- HTTP REST calls (synchronous)
- Services call each other directly
- Example: OrderService calls InventoryService to reserve stock

**Phase 3 (Planned)**:
- Add message queue (RabbitMQ/Kafka)
- Async communication for reliability
- Event-driven architecture
- Saga pattern for distributed transactions

---

## Docker Network

Services communicate via internal Docker network `microservices-network`:

```
order-service → order-db (SQL Server)
product-catalog-service → mongo (MongoDB)
inventory-service → inventory-db (PostgreSQL)
notification-service (no database)

All services ← api-gateway (Nginx, port 8080)
```

Host machine accesses via API Gateway on `localhost:8080`.

---

## Build & Deployment

### Build Docker Images
```bash
docker-compose build
```

### Deploy
```bash
docker-compose up -d
```

### View Status
```bash
docker-compose ps
```

### Logs
```bash
docker-compose logs -f [service-name]
```

---

## Development Workflow

### Local Development (without Docker)
1. Install .NET 8 SDK
2. Start databases separately (or use Docker for just databases)
3. Open `Microservices.sln` in Visual Studio
4. Set startup projects or run individually
5. Debug with breakpoints

### Docker Development
1. `docker-compose up --build`
2. Access services via localhost ports
3. View logs: `docker-compose logs -f`

---

## Checklist for Phase 2 Completion

- [x] OrderService created (SQL Server)
- [x] ProductCatalogService created (MongoDB)
- [x] InventoryService created (PostgreSQL)
- [x] NotificationService created (Stateless)
- [x] Database-per-service implemented
- [x] Polyglot persistence with 3 database types
- [x] All services containerized (Dockerfile)
- [x] docker-compose.yml with orchestration
- [x] API Gateway (Nginx) configured
- [x] ADRs written for each database choice
- [x] Documentation (README, Deployment Guide)
- [x] Migrations for SQL/PostgreSQL
- [x] Swagger endpoints on each service
- [x] Logging configured (Serilog)

---

## Next Steps (Phase 3)

1. **Async Messaging**: Add RabbitMQ or Kafka
2. **Saga Pattern**: Distributed transactions for order workflow
3. **Service Discovery**: Consul or Eureka
4. **Resilience**: Circuit breakers (Polly), retries
5. **Caching**: Redis for catalog optimization
6. **Monitoring**: ELK Stack, Prometheus, Jaeger

---

## File Count Summary

- **Services**: 4 microservices
- **Controllers**: 4 (one per service)
- **BLL Services**: 4 (one per service)
- **DAL/Repositories**: 4 (one per service)
- **Models**: 8+ (domain + DTOs)
- **Database Contexts**: 2 (SQL Server, PostgreSQL)
- **Migrations**: 4 (2 initial migrations)
- **Configuration Files**: 8+ (docker-compose, nginx, appsettings, launchSettings)
- **Documentation**: 6 (README, Guide, ADRs, Structure)

**Total**: 100+ files created for Phase 2 implementation

