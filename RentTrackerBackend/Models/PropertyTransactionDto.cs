namespace RentTrackerBackend.Models
{
    public class PropertyTransactionDto
    {
        // Required fields for creation/update
        public string RentalPropertyId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType TransactionType { get; set; }
        public string CategoryId { get; set; } = string.Empty;
        public string? PaymentMethodId { get; set; }
        public string? Reference { get; set; }
        public string? Notes { get; set; }
        public List<string> AttachmentIds { get; set; } = new List<string>();

        public static PropertyTransaction ToEntity(PropertyTransactionDto dto)
        {
            return new PropertyTransaction
            {
                RentalPropertyId = dto.RentalPropertyId,
                Amount = dto.Amount,
                TransactionDate = dto.TransactionDate,
                TransactionType = dto.TransactionType,
                CategoryId = dto.CategoryId,
                PaymentMethodId = dto.PaymentMethodId,
                Reference = dto.Reference,
                Notes = dto.Notes,
                AttachmentIds = dto.AttachmentIds
            };
        }
    }
}