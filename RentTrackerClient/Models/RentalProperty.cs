namespace RentTrackerClient.Models;

public class RentalProperty
{
    public string Id { get; set; } = string.Empty;
    
    public Address Address { get; set; } = new();
    
    public string Description { get; set; } = string.Empty;
    
    public decimal RentAmount { get; set; }
    
    public LeaseDates LeaseDates { get; set; } = new();
    
    public PropertyManager PropertyManager { get; set; } = new();
    
    public DateTime CreatedAt { get; set; }
    
    public DateTime UpdatedAt { get; set; }
    
    public List<PropertyTransaction> Transactions { get; set; } = new();
    
    public List<Attachment> Attachments { get; set; } = new();

    public Dictionary<string, object> Attributes { get; set; } = new();
    
    public int Version { get; set; }
}

public class LeaseDates
{
    public DateTime StartDate { get; set; } = DateTime.UtcNow;
    public DateTime EndDate { get; set; } = DateTime.UtcNow.AddYears(1);
}