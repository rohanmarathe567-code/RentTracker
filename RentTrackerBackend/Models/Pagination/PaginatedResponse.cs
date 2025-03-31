namespace RentTrackerBackend.Models.Pagination;

public class PaginatedResponse<T>
{
    public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    
    public int TotalCount { get; set; }
    
    public int PageNumber { get; set; }
    
    public int TotalPages { get; set; }
    
    public bool HasNextPage { get; set; }
    
    public bool HasPreviousPage { get; set; }
    
    public static PaginatedResponse<T> Create(
        IEnumerable<T> items, 
        int totalCount, 
        int pageNumber, 
        int pageSize)
    {
        return new PaginatedResponse<T>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            HasNextPage = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize),
            HasPreviousPage = pageNumber > 1
        };
    }
}