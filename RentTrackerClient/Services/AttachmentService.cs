using System.Net.Http.Json;
using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class AttachmentService : HttpClientService
{
    public AttachmentService(HttpClient httpClient) 
        : base(httpClient, "api/attachment")
    {
    }

    public async Task<List<Attachment>> GetAllAttachmentsAsync()
    {
        return await GetListAsync<Attachment>("");
    }

    public async Task<Attachment?> GetAttachmentAsync(int id)
    {
        return await GetAsync<Attachment>($"{id}");
    }

    public async Task<Attachment?> CreateAttachmentAsync(Attachment attachment, MultipartFormDataContent formData)
    {
        var response = await _httpClient.PostAsync($"{_baseUrl}", formData);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<Attachment>(_jsonOptions);
    }

    public async Task<Attachment?> UpdateAttachmentAsync(int id, Attachment attachment)
    {
        return await PutAsync<Attachment>($"{id}", attachment);
    }

    public async Task DeleteAttachmentAsync(int id)
    {
        await DeleteAsync($"{id}");
    }

    public async Task<Stream> DownloadAttachmentAsync(int id)
    {
        var response = await _httpClient.GetAsync($"{_baseUrl}/{id}/download");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }
}