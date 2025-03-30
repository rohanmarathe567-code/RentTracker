using RentTrackerClient.Models;

namespace RentTrackerClient.Services;

public class RentalPaymentService : HttpClientService
{
    public RentalPaymentService(HttpClient httpClient) 
        : base(httpClient, "api/rentalpayment")
    {
    }

    public async Task<List<RentalPayment>> GetAllPaymentsAsync()
    {
        return await GetListAsync<RentalPayment>("");
    }

    public async Task<RentalPayment?> GetPaymentAsync(int id)
    {
        return await GetAsync<RentalPayment>($"{id}");
    }

    public async Task<RentalPayment?> CreatePaymentAsync(RentalPayment payment)
    {
        return await PostAsync<RentalPayment>("", payment);
    }

    public async Task<RentalPayment?> UpdatePaymentAsync(int id, RentalPayment payment)
    {
        return await PutAsync<RentalPayment>($"{id}", payment);
    }

    public async Task DeletePaymentAsync(int id)
    {
        await DeleteAsync($"{id}");
    }

    public async Task<List<Attachment>> GetPaymentAttachmentsAsync(int id)
    {
        return await GetListAsync<Attachment>($"{id}/attachments");
    }
}