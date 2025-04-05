using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Components.Forms;

namespace RentTrackerClient.Services;

public class AttachmentService : HttpClientService
{
    public AttachmentService(HttpClient httpClient, ILogger<AttachmentService> logger)
        : base(httpClient, "/api", logger)  // Use '/api' as base path to align with backend routes
    {
    }

    public async Task<List<Attachment>> GetAllAttachmentsAsync()
    {
        _logger.LogInformation("Fetching all attachments");
        var attachments = await GetListAsync<Attachment>("");
        _logger.LogDebug($"Retrieved {attachments.Count} attachments");
        return attachments;
    }

    public async Task<Attachment?> GetAttachmentAsync(int id)
    {
        _logger.LogInformation($"Fetching attachment with ID: {id}");
        var attachment = await GetAsync<Attachment>($"{id}");
        
        if (attachment == null)
        {
            _logger.LogWarning($"No attachment found with ID: {id}");
        }
        else
        {
            _logger.LogDebug($"Retrieved attachment details for ID: {id}");
        }

        return attachment;
    }

    public async Task<Attachment?> UploadAttachmentAsync(Guid propertyId, IBrowserFile file, string? description = null, string[]? tags = null)
    {
        _logger.LogInformation($"Uploading attachment for property ID: {propertyId}");
        _logger.LogDebug($"Attachment filename: {file.Name}");

        using var content = new MultipartFormDataContent();
        using var fileStream = file.OpenReadStream(maxAllowedSize: 10 * 1024 * 1024); // 10MB limit
        using var fileContent = new StreamContent(fileStream);
        
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        content.Add(fileContent, "file", file.Name);
        
        if (description != null)
            content.Add(new StringContent(description), "description");
            
        if (tags != null && tags.Length > 0)
            content.Add(new StringContent(JsonSerializer.Serialize(tags)), "tags");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}/properties/{propertyId}/attachments", content);
            response.EnsureSuccessStatusCode();

            var attachment = await response.Content.ReadFromJsonAsync<Attachment>();
            
            if (attachment != null)
            {
                _logger.LogInformation($"Successfully uploaded attachment for property ID: {propertyId}. Attachment ID: {attachment.Id}");
            }
            else
            {
                _logger.LogWarning($"Attachment upload for property ID: {propertyId} returned no attachment");
            }

            return attachment;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error uploading attachment for property ID: {propertyId}");
            throw;
        }
    }

    public async Task<List<Attachment>> GetAttachmentsByPropertyAsync(Guid propertyId)
    {
        _logger.LogInformation($"Fetching attachments for property with ID: {propertyId}");
        
        var attachments = await GetListAsync<Attachment>($"properties/{propertyId}/attachments");
        
        _logger.LogDebug($"Retrieved {attachments.Count} attachments for property with ID: {propertyId}");
        return attachments;
    }

    public async Task DeleteAttachmentAsync(Guid id)
    {
        _logger.LogInformation($"Deleting attachment with ID: {id}");
        
        try
        {
            await DeleteAsync($"attachments/{id}");
            _logger.LogInformation($"Successfully deleted attachment with ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting attachment with ID: {id}");
            throw;
        }
    }

    public async Task<Stream> DownloadAttachmentAsync(Guid id)
    {
        _logger.LogInformation($"Downloading attachment with ID: {id}");

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/attachments/{id}/download");
            response.EnsureSuccessStatusCode();

            var stream = await response.Content.ReadAsStreamAsync();
            
            _logger.LogDebug($"Successfully downloaded attachment with ID: {id}");
            return stream;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error downloading attachment with ID: {id}");
            throw;
        }
    }
}