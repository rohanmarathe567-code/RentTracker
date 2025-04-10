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