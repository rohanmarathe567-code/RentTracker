using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using RentTrackerClient.Models;
using RentTrackerClient.Models.Pagination;

namespace RentTrackerClient.Services
{
    public class PropertyTransactionService : HttpClientService
    {
        public PropertyTransactionService(
            HttpClient httpClient, 
            ILogger<PropertyTransactionService> logger,
            IAuthenticationService authService)
            : base(httpClient, "api/properties", logger, authService)
        {
            _logger.LogInformation("PropertyTransactionService initialized");
        }

        public async Task<PaginatedResponse<PropertyTransaction>?> GetTransactionsByPropertyAsync(
            string propertyId, 
            int pageNumber = 1,
            int pageSize = 10,
            TransactionType? type = null,
            string? sortField = null,
            bool sortDescending = true)
        {
            try
            {
                // Build query string
                var queryParams = new List<string>
                {
                    $"pageNumber={pageNumber}",
                    $"pageSize={pageSize}",
                    // Backend expects comma-separated list in a single 'include' parameter
                    "include=Category,PaymentMethod"
                };

                if (type.HasValue)
                {
                    queryParams.Add($"type={type.Value}");
                }

                if (!string.IsNullOrEmpty(sortField))
                {
                    queryParams.Add($"sortField={sortField}");
                    queryParams.Add($"sortDescending={sortDescending}");
                }

                var queryString = string.Join("&", queryParams);
                var path = $"{propertyId}/transactions?{queryString}";
                _logger.LogDebug($"Fetching transactions for property {propertyId}");

                var response = await GetAsync<PaginatedResponse<PropertyTransaction>>(path);
                if (response?.Items != null)
                {
                    _logger.LogDebug($"Retrieved {response.Items.Count()} transactions");
                }
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transactions for property {PropertyId}", propertyId);
                return null;
            }
        }

        public async Task<PropertyTransaction?> GetTransactionByIdAsync(string propertyId, string transactionId)
        {
            try
            {
                _logger.LogDebug($"Fetching transaction {transactionId}");
                var path = $"{propertyId}/transactions/{transactionId}?include=Category,PaymentMethod";
                return await GetAsync<PropertyTransaction>(path);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction {TransactionId} for property {PropertyId}", 
                    transactionId, propertyId);
                return null;
            }
        }

        public async Task<PropertyTransaction?> CreateTransactionAsync(string propertyId, PropertyTransaction transaction)
        {
            try
            {
                return await PostAsync<PropertyTransaction>($"{propertyId}/transactions", transaction);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction for property {PropertyId}", propertyId);
                throw; // Re-throw to preserve the original error message
            }
        }

        public async Task<bool> UpdateTransactionAsync(
            string propertyId, string transactionId, PropertyTransaction transaction)
        {
            try
            {
                var response = await PutAsync<PropertyTransaction>($"{propertyId}/transactions/{transactionId}", transaction);
                // For 204 NoContent responses, response will be null but update was successful
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating transaction {TransactionId} for property {PropertyId}", 
                    transactionId, propertyId);
                return false;
            }
        }

        public async Task<bool> DeleteTransactionAsync(string propertyId, string transactionId)
        {
            try
            {
                await DeleteAsync($"{propertyId}/transactions/{transactionId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting transaction {TransactionId} for property {PropertyId}", 
                    transactionId, propertyId);
                return false;
            }
        }

        private async Task<bool> VerifyCategoryExistsAsync(string categoryId)
        {
            try
            {
                var category = await GetAsync<PropertyTransactionCategory>($"categories/{categoryId}");
                var exists = category != null;
                _logger.LogDebug($"Category {categoryId} exists: {exists}");
                return exists;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error verifying category {CategoryId}", categoryId);
                return false;
            }
        }

        public async Task<FinancialSummary?> GetFinancialSummaryAsync(
            string propertyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            try
            {
                var queryParams = new List<string>();

                if (startDate.HasValue)
                {
                    queryParams.Add($"startDate={startDate.Value.ToString("yyyy-MM-dd")}");
                }

                if (endDate.HasValue)
                {
                    queryParams.Add($"endDate={endDate.Value.ToString("yyyy-MM-dd")}");
                }

                var queryString = queryParams.Any() ? "?" + string.Join("&", queryParams) : "";
                return await GetAsync<FinancialSummary>($"{propertyId}/financial-summary{queryString}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting financial summary for property {PropertyId}", propertyId);
                return null;
            }
        }
    }
}
