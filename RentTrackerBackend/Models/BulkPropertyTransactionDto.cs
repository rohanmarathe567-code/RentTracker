namespace RentTrackerBackend.Models
{
    public class BulkPropertyTransactionDto
    {
        public List<PropertyTransactionDto> Transactions { get; set; } = new();
    }
}