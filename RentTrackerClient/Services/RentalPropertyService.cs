using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;

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
        var properties = await GetListAsync<RentalProperty>("");
        _logger.LogDebug($"Retrieved {properties.Count} rental properties");
        return properties;
    }

    public async Task<RentalProperty?> GetPropertyAsync(int id)
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
        _logger.LogInformation("Creating new rental property");
        _logger.LogDebug($"Property details: {System.Text.Json.JsonSerializer.Serialize(property)}");
        
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

    public async Task<RentalProperty?> UpdatePropertyAsync(int id, RentalProperty property)
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

    public async Task DeletePropertyAsync(int id)
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

    public async Task<List<RentalPayment>> GetPropertyPaymentsAsync(int id)
    {
        _logger.LogInformation($"Fetching payments for rental property with ID: {id}");
        
        var payments = await GetListAsync<RentalPayment>($"{id}/payments");
        
        _logger.LogDebug($"Retrieved {payments.Count} payments for rental property with ID: {id}");
        return payments;
    }

    public async Task<List<Attachment>> GetPropertyAttachmentsAsync(int id)
    {
        _logger.LogInformation($"Fetching attachments for rental property with ID: {id}");
        
        var attachments = await GetListAsync<Attachment>($"{id}/attachments");
        
        _logger.LogDebug($"Retrieved {attachments.Count} attachments for rental property with ID: {id}");
        return attachments;
    }
}