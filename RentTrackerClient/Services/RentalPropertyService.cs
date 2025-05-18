using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;
using RentTrackerClient.Models.Pagination;
using System.Net.Http.Json;
using System.Text.Json;
using System.Web;
using System;

namespace RentTrackerClient.Services;

public class RentalPropertyService : HttpClientService
{
    public RentalPropertyService(
        HttpClient httpClient,
        ILogger<RentalPropertyService> logger,
        IAuthenticationService authService)
        : base(httpClient, "api/properties", logger, authService)
    {
        _logger.LogDebug("RentalPropertyService initialized");
    }

    public async Task<List<RentalProperty>> GetAllPropertiesAsync()
    {
        try
        {
            var parameters = new PaginationParameters { PageNumber = 1, PageSize = 50 };
            var paginatedResult = await GetPaginatedPropertiesAsync(parameters);
            return paginatedResult.Items.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all properties");
            return new List<RentalProperty>();
        }
    }
    
    public async Task<PaginatedResponse<RentalProperty>> GetPaginatedPropertiesAsync(PaginationParameters parameters)
    {
        var queryString = BuildQueryString(parameters);
        var result = await GetAsync<PaginatedResponse<RentalProperty>>(queryString);
        if (result == null)
        {
            _logger.LogWarning("Failed to retrieve paginated properties");
            return new PaginatedResponse<RentalProperty>();
        }
        return result;
    }

    public async Task<RentalProperty?> GetPropertyAsync(string id)
    {
        _logger.LogInformation($"Fetching rental property with ID: {id}");
        var property = await GetAsync<RentalProperty>($"{id}");
        
        if (property == null)
        {
            _logger.LogWarning($"No rental property found with ID: {id}");
        }
        else
        {
            _logger.LogDebug($"Retrieved rental property details for ID: {id}");
        }

        return property;
    }

    public async Task<RentalProperty?> CreatePropertyAsync(RentalProperty property)
    {
        try
        {
            // Validate required fields
            if (string.IsNullOrWhiteSpace(property.Address?.Street))
            {
                _logger.LogWarning("Cannot create property: Street address is required");
                return null;
            }

            _logger.LogDebug($"Creating property at {property.Address?.Street}");
            var createdProperty = await PostAsync<RentalProperty>("", property);
            
            if (createdProperty != null)
            {
                _logger.LogDebug($"Created property with ID: {createdProperty.Id}");
            }
            return createdProperty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rental property");
            throw; // Rethrow to allow the UI to handle the error
        }
    }

    public async Task<RentalProperty?> UpdatePropertyAsync(string id, RentalProperty property)
    {
        try
        {
            _logger.LogDebug($"Updating property {id} (Version: {property.Version})");
            var updatedProperty = await PutAsync<RentalProperty>($"{id}", property);
            return updatedProperty;
        }
        catch (HttpRequestException ex) when (ex.Message.Contains("Concurrency conflict"))
        {
            _logger.LogWarning($"Concurrency conflict detected while updating property {id}. Version: {property.Version}");
            throw new InvalidOperationException("This property has been modified by another user. Please refresh and try again.", ex);
        }
    }

    public async Task DeletePropertyAsync(string id)
    {
        try
        {
            _logger.LogDebug($"Deleting property {id}");
            await DeleteAsync($"{id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting rental property with ID: {id}");
            throw;
        }
    }


    public async Task<List<Attachment>> GetPropertyAttachmentsAsync(string id)
    {
        var attachments = await GetListAsync<Attachment>($"{id}/attachments");
        _logger.LogDebug($"Retrieved {attachments.Count} attachments for property {id}");
        return attachments;
    }
    
    private string BuildQueryString(PaginationParameters parameters)
    {
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["pageNumber"] = parameters.PageNumber.ToString();
        query["pageSize"] = parameters.PageSize.ToString();
        
        if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
        {
            query["searchTerm"] = parameters.SearchTerm;
        }

        if (!string.IsNullOrWhiteSpace(parameters.SortField))
        {
            query["sortField"] = parameters.SortField;
            query["sortDescending"] = parameters.SortDescending.ToString().ToLower();
        }
        
        var queryString = query.ToString();
        return string.IsNullOrEmpty(queryString) ? string.Empty : $"?{queryString}";
    }
}