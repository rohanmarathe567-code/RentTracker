using System.Linq;
using RentTrackerBackend.Models.Pagination;

namespace RentTrackerBackend.Extensions;

public static class PaginationExtensions
{
    public static PaginatedResponse<T> ToPaginatedList<T>(
        this IEnumerable<T> source, 
        PaginationParameters parameters)
    {
        parameters.Validate();
        
        var enumerable = source.ToList();
        var totalCount = enumerable.Count;
        
        var items = enumerable
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToList();
        
        return PaginatedResponse<T>.Create(
            items, 
            totalCount, 
            parameters.PageNumber, 
            parameters.PageSize);
    }
    
    public static IEnumerable<T> ApplyPagination<T>(
        this IEnumerable<T> source, 
        PaginationParameters parameters)
    {
        parameters.Validate();
        
        return source
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize);
    }
}