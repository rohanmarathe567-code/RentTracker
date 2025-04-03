namespace RentTrackerClient.Models.Pagination;

public class PaginationParameters
{
    private const int MaxPageSize = 50;
    private const int DefaultPageSize = 10;
    
    private int _pageSize = DefaultPageSize;
    
    public int PageNumber { get; set; } = 1;
    
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value > 0 ? Math.Min(value, MaxPageSize) : DefaultPageSize;
    }
    
    public string? SearchTerm { get; set; }
    
    public string? SortField { get; set; }
    
    public bool SortDescending { get; set; }
    
    public void Validate()
    {
        PageNumber = Math.Max(1, PageNumber);
        // Additional validation can be added here if needed
    }
}