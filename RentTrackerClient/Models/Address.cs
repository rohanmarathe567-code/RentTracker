namespace RentTrackerClient.Models;

public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string State { get; set; } = string.Empty;
    public string ZipCode { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{Street}, {City}, {State}";
    }
}