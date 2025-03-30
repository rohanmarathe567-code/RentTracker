namespace RentTrackerClient.Models;

public class RentalProperty
{
    public int Id { get; set; }
    
    public string Address { get; set; } = string.Empty;
    
    public string? Suburb { get; set; }
    
    public string? State { get; set; }
    
    public string? PostCode { get; set; }
    
    public string? Description { get; set; }
    
    public decimal? WeeklyRentAmount { get; set; }
    
    public DateTime? LeaseStartDate { get; set; }
    
    public DateTime? LeaseEndDate { get; set; }
    
    public string? PropertyManager { get; set; }
    
    public string? PropertyManagerContact { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<RentalPayment> RentalPayments { get; set; } = new();
    
    public List<Attachment> Attachments { get; set; } = new();
}