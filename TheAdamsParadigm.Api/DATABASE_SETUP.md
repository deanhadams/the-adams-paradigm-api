# Database Setup Guide - The Adams Paradigm API

## Overview
This guide covers the setup and configuration of the PostgreSQL database connection to Neon for The Adams Paradigm API (.NET 8 with Entity Framework Core).

## Connection Details
- **Database**: adamsParadigm_db
- **Host**: ep-tiny-silence-za0c6dcp-pooler.c-2.eu-west-2.aws.neon.tech
- **Port**: 5432
- **User**: tap_owner
- **Region**: eu-west-2 (Ireland)
- **Provider**: Neon PostgreSQL

## Project Structure

### Data Layer (`TheAdamsParadigm.Api/Data/`)
- **ApplicationDbContext.cs** - Entity Framework Core DbContext that defines database entities and relationships
  - Configures Order and Service entities
  - Sets up indexes for optimal query performance
  - Handles foreign key relationships

- **InitialSchema.sql** - SQL script for manual database setup (if needed)
  - Creates tables with proper constraints
  - Sets up indexes
  - Includes seed data

### Models (`TheAdamsParadigm.Api/Models/`)
- **Order.cs** - Represents a customer order
  - OrderId (Primary Key)
  - ServiceId (Foreign Key to Service)
  - Amount, Currency, Status
  - CheckoutId, PaymentId (for Yoco integration)
  - CreatedAt, PaidAt timestamps
  - Navigation property to Service

- **Service.cs** - Represents a billable service
  - ServiceId (Primary Key - auto-increment)
  - Icon, Title, Description
  - CostPerHour (decimal 18,2)
  - Navigation collection to Orders

- **YocoWebhookEvent.cs** - Yoco payment webhook event model
- **YocoPaymentPayload.cs** - Yoco payment details
- **YocoPaymentMetadata.cs** - Yoco payment metadata
- **YocoPaymentMethodDetails.cs** - Yoco payment method information

### Services (`TheAdamsParadigm.Api/Services/`)
- **DatabaseSeedService.cs** - Handles database initialization and seeding
  - Ensures database is created
  - Seeds 10 services with pricing information
  - Checks if data already exists to avoid duplicates
  - Comprehensive error logging

## Automatic Setup Process

### When the Application Starts
1. The connection string is read from `appsettings.json`
2. Entity Framework Core applies any pending migrations
3. The database is automatically created if it doesn't exist
4. `DatabaseSeedService` populates initial data (Services)

### Program.cs Flow
```csharp
1. DbContext is registered with PostgreSQL provider
2. DatabaseSeedService is registered as a scoped service
3. On startup:
   - Creates a service scope
   - Applies migrations with `dbContext.Database.MigrateAsync()`
   - Runs `seedService.SeedDatabaseAsync()`
   - Handles and logs any errors
```

## Configuration Files

### appsettings.json
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=ep-tiny-silence-za0c6dcp-pooler.c-2.eu-west-2.aws.neon.tech;Port=5432;Database=adamsParadigm_db;Username=tap_owner;Password=npg_l5amuCfSoQ4P;SSL Mode=Require;Include Error Detail=True;Channel Binding=Require;"
  }
}
```

## NuGet Dependencies Added
- `Microsoft.EntityFrameworkCore` (v8.0.0)
- `Microsoft.EntityFrameworkCore.Tools` (v8.0.0)
- `Npgsql.EntityFrameworkCore.PostgreSQL` (v8.0.0)

## Seed Data

The application automatically seeds 10 services on first run:

| Icon | Title | Description | Cost/Hour |
|------|-------|-------------|-----------|
| Layers | Full-Stack Web Development | Modern responsive applications | $120.00 |
| Wrench | Custom Software | Purpose-built applications | $150.00 |
| Plug | API Development | Secure and maintainable APIs | $110.00 |
| Atom | React Applications | Fast, modern frontend experiences | $100.00 |
| Server | ASP.NET / C# Development | Robust backend systems | $130.00 |
| Database | Database Solutions | SQL Server and data architecture | $140.00 |
| CreditCard | Payment Integrations | Payment workflows and integrations | $125.00 |
| CalendarClock | Booking & Scheduling | Booking systems and availability | $95.00 |
| Sparkles | AI-Powered Applications | AI integrations and features | $160.00 |
| CloudCog | Cloud & Deployment | Production deployment solutions | $135.00 |

## Database Indexes

For optimal performance, the following indexes are created:
- `idx_orders_checkout_id` - Fast lookup during webhook processing
- `idx_orders_status` - Efficient filtering by order status
- `idx_orders_created_at` - Time-based queries and sorting

## Error Handling

### Automatic Startup
- Any database errors during startup are caught and logged
- The application will log the error but may still start (depending on configuration)
- Check the application logs for details

### Manual Database Setup (if needed)
If automatic setup fails, you can manually run the SQL script:
1. Execute `TheAdamsParadigm.Api/Data/InitialSchema.sql` against the Neon database
2. Restart the application

## Connection String Format

```
Host={hostname};Port={port};Database={database};Username={user};Password={password};SSL Mode=Require;Include Error Detail=True;Channel Binding=Require;
```

## Troubleshooting

### Connection Issues
1. Verify the connection string in `appsettings.json`
2. Check that SSL Mode is set to "Require"
3. Ensure Channel Binding is set to "Require" (Neon requirement)
4. Test connectivity using a PostgreSQL client (psql, pgAdmin, etc.)

### Migration Issues
1. Check application logs for migration errors
2. Ensure EntityFrameworkCore tools are installed
3. Verify database user has proper permissions

### Seeding Issues
1. Check if services table already has data
2. Verify foreign key constraints are properly set
3. Check application logs for detailed error messages

## Development Notes

- The connection uses SSL with channel binding (Neon requirement)
- Timestamps are stored as `TIMESTAMP WITHOUT TIME ZONE`
- Decimal precision is (18,2) for monetary amounts
- Service IDs are auto-incrementing (SERIAL)
- Order IDs are text/string based (not auto-generated)

## Knowledge Base (RAG)

The AI chatbot answers questions using retrieval over `knowledge_chunks`, a Postgres table
of Voyage AI (`voyage-3.5`) embeddings — one row per business section, project, and FAQ
defined in `Data/knowledge-base.json`. `KnowledgeSearchService` embeds the visitor's
question and finds the closest chunks by cosine similarity (pgvector `<=>` operator).

### Reseeding after `knowledge-base.json` changes

Whenever the knowledge base content changes (new project, updated pricing, new FAQ, etc.),
regenerate the embeddings by calling:

```
POST /api/knowledge/reseed
```

This is a full **clear-and-reseed**: it re-embeds every chunk from the current
`knowledge-base.json`, and only deletes the old rows once every new embedding has
succeeded — so a failed Voyage AI call (down, rate-limited) leaves the existing,
still-working chunks untouched rather than wiping the table out. Response:

```json
{ "success": true, "chunksInserted": 27 }
```

There is no authentication on this endpoint (matching the rest of this project's admin
actions, e.g. `register-webhook`) — it's meant to be triggered manually after a content
change, not exposed to end users.

### Tuning retrieval

`KnowledgeSearchService`'s similarity cutoff is configurable, not hardcoded:

```json
"KnowledgeSearch": { "MinCosineSimilarity": 0.5 }
```

Raise it if irrelevant chunks are getting pulled in; lower it if relevant questions are
falling back to "no info found" too often. No code change or redeploy needed to retune —
just the config value (0.5-0.6 is a reasonable starting range).

## Next Steps

1. Run the application - database will be automatically created and seeded
2. Create API endpoints to interact with Orders and Services
3. Implement Yoco payment webhook handlers
4. Add additional business logic as needed
