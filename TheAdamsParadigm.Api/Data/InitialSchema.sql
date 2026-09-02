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
    setup_fee NUMERIC(18,2) NOT NULL DEFAULT 0.00
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

-- Insert seed data into services table
INSERT INTO services (icon, title, description, cost_per_hour, setup_fee)
VALUES
('Globe', 'Basic Website', '3 pages, no integrations.', 100.00, 2500.00),
('Layers', 'Full-Stack Web Development', 'Modern responsive applications built around real business requirements.', 100.00, 500.00),
('Wrench', 'Custom Software', 'Purpose-built applications designed around a company''s workflow.', 150.00, 500.00),
('Plug', 'API Development', 'Secure and maintainable APIs and backend systems.', 100.00, 500.00),
('Atom', 'React Applications', 'Fast, modern and interactive frontend experiences.', 100.00, 500.00),
('Server', 'ASP.NET / C# Development', 'Robust backend systems using modern Microsoft technologies.', 130.00, 500.00),
('Database', 'Database Solutions', 'SQL Server and application data architecture.', 100.00, 500.00),
('CreditCard', 'Payment Integrations', 'Payment workflows and third-party payment integrations.', 150.00, 1500.00),
('CalendarClock', 'Booking & Scheduling', 'Booking systems, availability logic, payments and confirmations.', 100.00, 500.00),
('Sparkles', 'AI-Powered Applications', 'Practical AI integrations and intelligent application features.', 150.00, 500.00),
('CloudCog', 'Cloud & Deployment', 'Taking applications from development into reliable production environments.', 100.00, 500.00)

ON CONFLICT DO NOTHING;

-- ============================================================
-- Optional: Verify the tables were created successfully
-- ============================================================
-- SELECT table_name FROM information_schema.tables WHERE table_schema='public' AND table_type='BASE TABLE';
-- SELECT * FROM information_schema.columns WHERE table_name='orders';
