using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;
using RentTrackerClient.Models.Pagination;
using System.Web;

namespace RentTrackerClient.Services;

public class RentalPropertyService : HttpClientService
{
    public RentalPropertyService(HttpClient httpClient, ILogger<RentalPropertyService> logger)
        : base(httpClient, "api/properties", logger)
    {
    }

    public async Task<List<RentalProperty>> GetAllPropertiesAsync()
    {
        _logger.LogInformation("Fetching all rental properties");
        try
        {
            // Use pagination parameters to ensure we get a valid response
            var parameters = new PaginationParameters { PageNumber = 1, PageSize = 50 };
            var paginatedResult = await GetPaginatedPropertiesAsync(parameters);
            var properties = paginatedResult.Items.ToList();
            _logger.LogDebug($"Retrieved {properties.Count} rental properties");
            return properties;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error fetching all properties");
            return new List<RentalProperty>();
        }
    }
    
    public async Task<PaginatedResponse<RentalProperty>> GetPaginatedPropertiesAsync(PaginationParameters parameters)
    {
        _logger.LogInformation($"Fetching paginated rental properties. Page: {parameters.PageNumber}, Size: {parameters.PageSize}");
        
        var queryString = BuildQueryString(parameters);
        var result = await GetAsync<PaginatedResponse<RentalProperty>>(queryString);
        
        if (result != null)
        {
            _logger.LogDebug($"Retrieved {result.Items.Count()} properties (page {result.PageNumber} of {result.TotalPages})");
        }
        else
        {
            _logger.LogWarning("Failed to retrieve paginated properties");
            // Return empty response to avoid null reference exceptions
            result = new PaginatedResponse<RentalProperty>();
        }
        
        return result;
    }

    public async Task<RentalProperty?> GetPropertyAsync(Guid id)
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
            _logger.LogInformation("Creating new rental property");
            
            // Enhanced JSON serialization logging
            var serializedProperty = System.Text.Json.JsonSerializer.Serialize(property, new System.Text.Json.JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            });
            
            _logger.LogDebug($"Serialized Property JSON: {serializedProperty}");
            
            // Log individual property values for more detailed debugging
            _logger.LogDebug($"Property Validation: " +
                $"Id={property.Id}, " +
                $"Address={property.Address}, " +
                $"WeeklyRentAmount={property.WeeklyRentAmount}, " +
                $"CreatedAt={property.CreatedAt}, " +
                $"UpdatedAt={property.UpdatedAt}");
            
            // Ensure required fields are set
            if (string.IsNullOrWhiteSpace(property.Address))
            {
                _logger.LogWarning("Cannot create property: Address is required");
                return null;
            }
            
            var createdProperty = await PostAsync<RentalProperty>("", property);
            
            if (createdProperty != null)
            {
                _logger.LogInformation($"Successfully created rental property with ID: {createdProperty.Id}");
            }
            else
            {
                _logger.LogWarning("Failed to create rental property");
            }

            return createdProperty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating rental property");
            throw; // Rethrow to allow the UI to handle the error
        }
    }

    public async Task<RentalProperty?> UpdatePropertyAsync(Guid id, RentalProperty property)
    {
        _logger.LogInformation($"Updating rental property with ID: {id}");
        
        // Enhanced JSON serialization logging
        var serializedProperty = System.Text.Json.JsonSerializer.Serialize(property, new System.Text.Json.JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNameCaseInsensitive = true
        });
        
        _logger.LogDebug($"Serialized Property JSON: {serializedProperty}");
        
        // Log individual property values for more detailed debugging
        _logger.LogDebug($"Property Validation: " +
            $"Id={property.Id}, " +
            $"Address={property.Address}, " +
            $"WeeklyRentAmount={property.WeeklyRentAmount}, " +
            $"CreatedAt={property.CreatedAt}, " +
            $"UpdatedAt={property.UpdatedAt}");
        
        var updatedProperty = await PutAsync<RentalProperty>($"{id}", property);
        
        if (updatedProperty != null)
        {
            _logger.LogInformation($"Successfully updated rental property with ID: {id}");
        }
        else
        {
            _logger.LogWarning($"Failed to update rental property with ID: {id}");
        }

        return updatedProperty;
    }

    public async Task DeletePropertyAsync(Guid id)
    {
        _logger.LogInformation($"Deleting rental property with ID: {id}");
        
        try
        {
            await DeleteAsync($"{id}");
            _logger.LogInformation($"Successfully deleted rental property with ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting rental property with ID: {id}");
            throw;
        }
    }

    public async Task<List<RentalPayment>> GetPropertyPaymentsAsync(Guid id)
    {
        _logger.LogInformation($"Fetching payments for rental property with ID: {id}");
        
        var payments = await GetListAsync<RentalPayment>($"{id}/payments");
        
        _logger.LogDebug($"Retrieved {payments.Count} payments for rental property with ID: {id}");
        return payments;
    }

    public async Task<List<Attachment>> GetPropertyAttachmentsAsync(Guid id)
    {
        _logger.LogInformation($"Fetching attachments for rental property with ID: {id}");
        
        var attachments = await GetListAsync<Attachment>($"{id}/attachments");
        
        _logger.LogDebug($"Retrieved {attachments.Count} attachments for rental property with ID: {id}");
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
        
        return $"?{query}";
    }
}