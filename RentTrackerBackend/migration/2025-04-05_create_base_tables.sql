-- Create base tables for properties and payments
CREATE TABLE IF NOT EXISTS rental_properties (
    id UUID PRIMARY KEY,
    address VARCHAR(500) NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE IF NOT EXISTS rental_payments (
    id UUID PRIMARY KEY,
    rental_property_id UUID NOT NULL REFERENCES rental_properties(id),
    amount DECIMAL(10, 2) NOT NULL,
    payment_date DATE NOT NULL,
    description VARCHAR(500),
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    updated_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_rental_payments_property ON rental_payments(rental_property_id);
CREATE INDEX IF NOT EXISTS idx_rental_payments_date ON rental_payments(payment_date);