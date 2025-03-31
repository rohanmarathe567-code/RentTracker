using Microsoft.EntityFrameworkCore;
using RentTrackerBackend.Models.Pagination;

namespace RentTrackerBackend.Extensions;

public static class PaginationExtensions
{
    public static async Task<PaginatedResponse<T>> ToPaginatedListAsync<T>(
        this IQueryable<T> query, 
        PaginationParameters parameters)
    {
        parameters.Validate();
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .ToListAsync();
        
        return PaginatedResponse<T>.Create(
            items, 
            totalCount, 
            parameters.PageNumber, 
            parameters.PageSize);
    }
    
    public static IQueryable<T> ApplyPagination<T>(
        this IQueryable<T> query, 
        PaginationParameters parameters)
    {
        parameters.Validate();
        
        return query
            .Skip((parameters.PageNumber - 1) * parameters.PageSize)
            .Take(parameters.PageSize);
    }
}