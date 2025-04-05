-- Create attachments table
CREATE TABLE IF NOT EXISTS attachments (
    id UUID PRIMARY KEY,
    file_name VARCHAR(255) NOT NULL,
    content_type VARCHAR(100) NOT NULL,
    storage_path VARCHAR(1000) NOT NULL,
    file_size BIGINT NOT NULL,
    description VARCHAR(500),
    upload_date TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
    entity_type VARCHAR(50) NOT NULL,
    rental_property_id UUID REFERENCES rental_properties(id),
    rental_payment_id UUID REFERENCES rental_payments(id),
    tags VARCHAR(255)[],
    CONSTRAINT entity_reference CHECK (
        (rental_property_id IS NOT NULL AND rental_payment_id IS NULL AND entity_type = 'Property') OR
        (rental_payment_id IS NOT NULL AND rental_property_id IS NULL AND entity_type = 'Payment')
    )
);

-- Create indexes
CREATE INDEX IF NOT EXISTS idx_attachments_property ON attachments(rental_property_id) WHERE rental_property_id IS NOT NULL;
CREATE INDEX IF NOT EXISTS idx_attachments_payment ON attachments(rental_payment_id) WHERE rental_payment_id IS NOT NULL;