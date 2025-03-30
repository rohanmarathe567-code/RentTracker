using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace RentTrackerClient.Services;

public class AttachmentService : HttpClientService
{
    public AttachmentService(HttpClient httpClient, ILogger<AttachmentService> logger) 
        : base(httpClient, "api/attachments", logger)
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

    public async Task<Attachment?> UploadAttachmentAsync(int propertyId, Stream fileStream, string fileName)
    {
        _logger.LogInformation($"Uploading attachment for property ID: {propertyId}");
        _logger.LogDebug($"Attachment filename: {fileName}");

        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        
        fileContent.Headers.ContentType = MediaTypeHeaderValue.Parse("multipart/form-data");
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(propertyId.ToString()), "propertyId");

        try
        {
            var response = await _httpClient.PostAsync($"{_baseUrl}", content);
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

    public async Task<List<Attachment>> GetAttachmentsByPropertyAsync(int propertyId)
    {
        _logger.LogInformation($"Fetching attachments for property with ID: {propertyId}");
        
        var attachments = await GetListAsync<Attachment>($"property/{propertyId}");
        
        _logger.LogDebug($"Retrieved {attachments.Count} attachments for property with ID: {propertyId}");
        return attachments;
    }

    public async Task DeleteAttachmentAsync(int id)
    {
        _logger.LogInformation($"Deleting attachment with ID: {id}");
        
        try
        {
            await DeleteAsync($"{id}");
            _logger.LogInformation($"Successfully deleted attachment with ID: {id}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error deleting attachment with ID: {id}");
            throw;
        }
    }

    public async Task<Stream> DownloadAttachmentAsync(int id)
    {
        _logger.LogInformation($"Downloading attachment with ID: {id}");

        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/{id}/download");
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