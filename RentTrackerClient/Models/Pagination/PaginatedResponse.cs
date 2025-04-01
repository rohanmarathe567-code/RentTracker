namespace RentTrackerClient.Models.Pagination;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int TotalPages { get; set; }
    
    public bool HasNextPage { get; set; }
    
    public bool HasPreviousPage { get; set; }
}