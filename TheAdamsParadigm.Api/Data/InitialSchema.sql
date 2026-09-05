-- ============================================================
-- Yoco Payment Demo - Database Schema Setup
-- ============================================================
-- Database: adamsParadigm_db
-- User: tap_owner
-- Created for .NET 8 / Entity Framework Core application
-- ============================================================

-- Create the services table
CREATE TABLE IF NOT EXISTS services (
    service_id SERIAL PRIMARY KEY,
    icon TEXT NOT NULL,
    title TEXT NOT NULL,
    description TEXT NOT NULL,
    cost_per_hour NUMERIC(18,2) NOT NULL DEFAULT 0.00,
    setup_fee NUMERIC(18,2) NOT NULL DEFAULT 0.00,
    is_bookable BOOLEAN NOT NULL DEFAULT TRUE
);

-- Create the orders table
CREATE TABLE IF NOT EXISTS orders (
    order_id TEXT PRIMARY KEY,
    service_id INTEGER,
    amount NUMERIC(18,2) NOT NULL,
    currency TEXT NOT NULL DEFAULT 'ZAR',
    status TEXT NOT NULL DEFAULT 'Pending',
    checkout_id TEXT,
    payment_id TEXT,
    payment_link TEXT,
    name TEXT NOT NULL DEFAULT '',
    surname TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL DEFAULT '',
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    paid_at TIMESTAMP WITHOUT TIME ZONE,
    CONSTRAINT fk_orders_service_id FOREIGN KEY (service_id) REFERENCES services(service_id)
);

-- Create index on checkout_id for faster lookups during webhook processing
-- This is critical for the webhook handler to quickly find orders by Yoco checkout ID
CREATE INDEX IF NOT EXISTS idx_orders_checkout_id ON orders(checkout_id);

-- Create index on status for filtering orders by status
-- Useful for reporting and admin queries
CREATE INDEX IF NOT EXISTS idx_orders_status ON orders(status);

-- Create index on created_at for time-based queries
-- Enables efficient sorting and filtering by creation date
CREATE INDEX IF NOT EXISTS idx_orders_created_at ON orders(created_at);

-- Create the user_memories table
-- Stores durable facts extracted from AI chatbot conversations, keyed by the
-- client-generated ChatUserId (see X-Chat-User-Id header on /api/ai/chat).
CREATE TABLE IF NOT EXISTS user_memories (
    id SERIAL PRIMARY KEY,
    chat_user_id TEXT NOT NULL,
    category TEXT NOT NULL,
    text TEXT NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create index on chat_user_id for fast lookups when fetching or deleting
-- a visitor's stored memory
CREATE INDEX IF NOT EXISTS idx_user_memories_chat_user_id ON user_memories(chat_user_id);

-- Enable pgvector for the knowledge base's embedding column below
CREATE EXTENSION IF NOT EXISTS vector;

-- Create the knowledge_chunks table
-- One row per business section/project/FAQ chunk from Data/knowledge-base.json, embedded
-- with Voyage AI (voyage-3.5, 1024 dimensions). KnowledgeSearchService does cosine-
-- similarity retrieval over this table; POST /api/knowledge/reseed regenerates it.
CREATE TABLE IF NOT EXISTS knowledge_chunks (
    id SERIAL PRIMARY KEY,
    section TEXT NOT NULL,
    content TEXT NOT NULL,
    embedding VECTOR(1024) NOT NULL,
    created_at TIMESTAMP WITHOUT TIME ZONE NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create the clients table
-- icloud_password is application-level encrypted (ASP.NET Core Data Protection via
-- ClientCredentialProtector) before it ever reaches this column — never plaintext.
CREATE TABLE IF NOT EXISTS clients (
    client_id SERIAL PRIMARY KEY,
    name TEXT NOT NULL,
    website TEXT NOT NULL DEFAULT '',
    email TEXT NOT NULL,
    icloud_email TEXT NOT NULL DEFAULT '',
    icloud_password TEXT NOT NULL DEFAULT '',
    icloud_calendar TEXT NOT NULL DEFAULT '',
    client_api_key TEXT NOT NULL
);

-- Create unique index on client_api_key for fast, unambiguous lookup during
-- per-client request authentication
CREATE UNIQUE INDEX IF NOT EXISTS idx_clients_client_api_key ON clients(client_api_key);

-- Create the DataProtectionKeys table
-- ASP.NET Core Data Protection's key ring, persisted here (via
-- ApplicationDbContext : IDataProtectionKeyContext) instead of local disk so it survives
-- Railway redeploys — losing these keys would permanently break decryption of anything
-- already encrypted with them (e.g. clients.icloud_password).
CREATE TABLE IF NOT EXISTS "DataProtectionKeys" (
    "Id" SERIAL PRIMARY KEY,
    "FriendlyName" TEXT,
    "Xml" TEXT
);

-- Insert seed data into services table
INSERT INTO services (icon, title, description, cost_per_hour, setup_fee, is_bookable)
VALUES
('Globe', 'Basic Website', 'A polished three-page website built to make a strong first impression — clean, focused design with no added integrations.', 100.00, 2000.00, TRUE),
('Layers', 'Full-Stack Web Development', 'Modern responsive applications built around real business requirements.', 100.00, 3500.00, TRUE),
('Wrench', 'Custom Software', 'Purpose-built applications designed around a company''s workflow.', 250.00, 5000.00, TRUE),
('Plug', 'API Development', 'Secure and maintainable APIs and backend systems.', 100.00, 500.00, FALSE),
('Atom', 'React Applications', 'Fast, modern and interactive frontend experiences.', 100.00, 500.00, FALSE),
('Server', 'ASP.NET / C# Development', 'Robust backend systems using modern Microsoft technologies.', 150.00, 500.00, FALSE),
('Database', 'Database Solutions', 'SQL Server and application data architecture.', 100.00, 500.00, TRUE),
('CreditCard', 'Payment Integrations', 'Payment workflows and third-party payment integrations.', 150.00, 1500.00, TRUE),
('CalendarClock', 'Booking & Scheduling', 'Booking systems, availability logic, payments and confirmations.', 200.00, 2500.00, TRUE),
('Sparkles', 'AI-Powered Applications', 'Practical AI integrations and intelligent application features.', 250.00, 5000.00, FALSE),
('CloudCog', 'Cloud & Deployment', 'Taking applications from development into reliable production environments.', 100.00, 500.00, FALSE),
('MessageCircle', 'Consult', 'One-on-one technical consulting to scope a new project, review an existing system, or plan next steps before committing to a build.', 100.00, 500.00, TRUE)

ON CONFLICT DO NOTHING;

-- ============================================================
-- Optional: Verify the tables were created successfully
-- ============================================================
-- SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';
-- SELECT * FROM information_schema.columns WHERE table_name='orders';
