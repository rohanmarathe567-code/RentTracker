namespace RentTrackerBackend.Models
{
    public class PaymentMethod : BaseDocument
    {
        public string Name { get; set; } = string.Empty;
        
        public string? Description { get; set; }
        
        public bool IsSystemDefault { get; set; }
        
        public string? UserId { get; set; }
    }
}