namespace RentTrackerClient.Models
{
    public class PropertyTransaction
    {
        public string Id { get; set; } = string.Empty;
        public string TenantId { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int Version { get; set; }
        public string RentalPropertyId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public PropertyTransactionCategory? Category { get; set; }
        public string? PaymentMethodId { get; set; }
        public PaymentMethod? PaymentMethod { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public List<string> AttachmentIds { get; set; } = new List<string>();
    }

    public enum TransactionType
    {
        Income,
        Expense
    }

    public class PropertyTransactionCategory
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public TransactionType TransactionType { get; set; }
        public bool IsSystemDefault { get; set; }
        public int Order { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

}
