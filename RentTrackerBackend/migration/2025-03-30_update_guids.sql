-- Migration Script for GUID Update
-- Date: 2025-03-30
-- Description: Update GUIDs across RentalProperty, RentalPayment, and Attachment tables

-- Begin Transaction
BEGIN;

-- Backup existing data
CREATE TABLE rental_property_guid_backup AS
SELECT "Id", "Address" FROM "RentalProperties";

CREATE TABLE rental_payment_guid_backup AS
SELECT "Id", "RentalPropertyId", "Amount", "PaymentDate" FROM "RentalPayments";

CREATE TABLE attachment_guid_backup AS
SELECT "Id", "RentalPropertyId", "RentalPaymentId", "FileName" FROM "Attachments";

-- Update GUIDs for RentalProperties
UPDATE "RentalProperties"
SET "Id" = gen_random_uuid();

-- Update GUIDs for RentalPayments and their foreign key references
UPDATE "RentalPayments"
SET
    "Id" = gen_random_uuid(),
    "RentalPropertyId" = (
        SELECT "Id" FROM "RentalProperties"
        WHERE "RentalProperties"."Address" = (
            SELECT "Address" FROM rental_property_guid_backup
            WHERE "Id" = "RentalPayments"."RentalPropertyId"
        )
    );

-- Update GUIDs for Attachments and their foreign key references
UPDATE "Attachments"
SET
    "Id" = gen_random_uuid(),
    "RentalPropertyId" = (
        SELECT "Id" FROM "RentalProperties"
        WHERE "RentalProperties"."Address" = (
            SELECT "Address" FROM rental_property_guid_backup
            WHERE "Id" = "Attachments"."RentalPropertyId"
        )
    ),
    "RentalPaymentId" = (
        SELECT "Id" FROM "RentalPayments"
        WHERE "RentalPayments"."Amount" = (
            SELECT "Amount" FROM rental_payment_guid_backup
            WHERE "Id" = "Attachments"."RentalPaymentId"
        )
    );

-- Verify the updates
SELECT
    (SELECT COUNT(*) FROM "RentalProperties") AS rental_properties_count,
    (SELECT COUNT(*) FROM "RentalPayments") AS rental_payments_count,
    (SELECT COUNT(*) FROM "Attachments") AS attachments_count;

-- Optional: Drop backup tables after successful migration
DROP TABLE rental_property_guid_backup;
DROP TABLE rental_payment_guid_backup;
DROP TABLE attachment_guid_backup;

-- Commit the transaction
COMMIT;

-- Notes:
-- 1. This script assumes PostgreSQL database
-- 2. Verify the script in a test environment first
-- 3. Ensure database backups are in place before running
-- 4. Adjust table and column names if they differ in your actual schema