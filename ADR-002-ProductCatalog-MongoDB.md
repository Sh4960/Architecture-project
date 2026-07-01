# ADR-002: ProductCatalogService Database Choice - MongoDB (Document Store)

## Context
ProductCatalogService manages the product catalog. Products (Gifts) have varying attributes by category - some products are books (ISBN, author, pages), others are electronics (specs, warranty), others are clothing (size, color, material). The schema is not fixed.

## Decision
**Use MongoDB (Document Store) for ProductCatalogService**

## Rationale

### Why Document Model Fits Product Catalog
- **Flexible Schema**: Each product category has different attributes without migrations
  - Book: `{ name, price, isbn, author, pages, publishDate }`
  - Electronics: `{ name, price, specs, warranty, model }`
  - Clothing: `{ name, price, size, color, material }`
  - All in ONE collection with natural JSON document model

- **No Complex Joins**: Products are self-contained documents. No need to join with multiple tables.

- **Nested Data**: Related data (images, reviews, ratings) nest naturally in a document.

### Comparison: Relational vs Document
| Scenario | Relational (SQL) | Document (MongoDB) |
|----------|------------------|--------------------|
| **New attribute for category** | ALTER TABLE, migration | Add to document, done |
| **10 attributes per product** | 10 columns | 10 fields in one doc |
| **30 attributes per product** | 30 columns (bloat) | 30 fields (natural) |
| **Reading a product** | 1-2 joins | 1 document query |
| **Querying by attribute** | Fixed schema index | Dynamic field index |

### CAP Theorem Analysis
- ProductCatalogService prioritizes **Availability** and **Partition tolerance**, can tolerate eventual consistency
- MongoDB follows AP (Availability, Partition tolerance) → eventual consistency (BASE)
- This is acceptable for product catalogs (reads >> writes)

## Consequences
- ✅ Flexible schema for varying products
- ✅ Fast queries (single document read)
- ✅ Scalable for reads
- ❌ No ACID transactions across documents
- ❌ Requires denormalization (data duplication)
- ❌ Backup/restore is different from relational

## Implementation Notes
- Use `Attributes` field as `Dictionary<string, object>` to store category-specific data
- Create indexes on frequently queried fields (Category, DonorId, CreatedAt)
- Cache catalog in Redis for performance (Phase 4)

## Related ADRs
- ADR-001: OrderService - SQL Server (ACID for money, CP)
- ADR-003: InventoryService - Redis (Key-Value, AP)
- ADR-004: NotificationService - No Database (Stateless, AP)
