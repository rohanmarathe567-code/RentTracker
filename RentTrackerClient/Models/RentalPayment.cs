namespace RentTrackerClient.Models;

public class RentalPayment
{
    public Guid Id { get; set; }
    
    public Guid RentalPropertyId { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public Guid PaymentMethodId { get; set; }
    public PaymentMethod? PaymentMethod { get; set; }
    
    public string? PaymentReference { get; set; }
    
    public string? Notes { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public RentalProperty? RentalProperty { get; set; }
    
    public List<Attachment> Attachments { get; set; } = new();
}