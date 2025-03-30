using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class RentalPropertyService : HttpClientService
{
    public RentalPropertyService(HttpClient httpClient) 
        : base(httpClient, "api/properties")
    {
    }

    public async Task<List<RentalProperty>> GetAllPropertiesAsync()
    {
        return await GetListAsync<RentalProperty>("");
    }

    public async Task<RentalProperty?> GetPropertyAsync(int id)
    {
        return await GetAsync<RentalProperty>($"{id}");
    }

    public async Task<RentalProperty?> CreatePropertyAsync(RentalProperty property)
    {
        return await PostAsync<RentalProperty>("", property);
    }

    public async Task<RentalProperty?> UpdatePropertyAsync(int id, RentalProperty property)
    {
        return await PutAsync<RentalProperty>($"{id}", property);
    }

    public async Task DeletePropertyAsync(int id)
    {
        await DeleteAsync($"{id}");
    }

    public async Task<List<RentalPayment>> GetPropertyPaymentsAsync(int id)
    {
        return await GetListAsync<RentalPayment>($"{id}/payments");
    }

    public async Task<List<Attachment>> GetPropertyAttachmentsAsync(int id)
    {
        return await GetListAsync<Attachment>($"{id}/attachments");
    }
}