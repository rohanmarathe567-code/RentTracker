namespace RentTrackerClient.Models;

public class RentalPayment
{
    public string Id { get; set; } = string.Empty;
    
    public string RentalPropertyId { get; set; } = string.Empty;
    
    public RentalProperty? RentalProperty { get; set; }
    
    public decimal Amount { get; set; }
    
    public DateTime PaymentDate { get; set; }
    
    public string? PaymentMethodId { get; set; }
    
    public PaymentMethod? PaymentMethod { get; set; }
    
    public string? PaymentReference { get; set; }
    
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
}