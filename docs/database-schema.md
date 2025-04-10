# Database Schema

The database schema represents a rental property management system with 4 main entities:

1. **RentalProperty**:
   - Core entity storing property details (address, rent amount, lease dates)
   - Has one-to-many relationships with RentalPayment and Attachment

2. **RentalPayment**:
   - Tracks payments made for properties
   - Connected to RentalProperty and PaymentMethod (many-to-one)
   - Can have multiple attachments (one-to-many with Attachment)

3. **PaymentMethod**:
   - Stores different payment methods 
   - Has a one-to-many relationship with RentalPayment

4. **Attachment**:
   - Handles file storage for both properties and payments
   - Contains metadata like fileName, contentType, fileSize
   - Links to either RentalProperty or RentalPayment through their IDs

Key Relationships:
- A RentalProperty can have many RentalPayments
- A PaymentMethod can be used for many RentalPayments
- Both RentalProperty and RentalPayment can have multiple Attachments

All entities include standard audit fields (createdAt, updatedAt) and use UUID primary keys for identification.

```mermaid
erDiagram
    RentalProperty ||--|{ RentalPayment : "has"
    PaymentMethod ||--|{ RentalPayment : "has"
    RentalProperty ||--|{ Attachment : "has"
    RentalPayment ||--|{ Attachment : "has"
    
    RentalPayment {
        uuid id
        uuid rentalPropertyId
        decimal amount
        datetime paymentDate
        uuid paymentMethodId
        string paymentReference
        string notes
        datetime createdAt
        datetime updatedAt
    }
    RentalProperty {
        uuid id
        string address
        string suburb
        string state
        string postCode
        string description
        decimal weeklyRentAmount
        datetime leaseStartDate
        datetime leaseEndDate
        string propertyManager
        string propertyManagerContact
        datetime createdAt
        datetime updatedAt
    }
    PaymentMethod {
        uuid id
        string name
        string description
        datetime createdAt
        datetime updatedAt
    }
    Attachment {
        uuid id
        string fileName
        string contentType
        string storagePath
        bigint fileSize
        string description
        string entityType
        uuid rentalPropertyId
        uuid rentalPaymentId
        datetime uploadDate
        array tags
    }