using RentTrackerClient.Models;

namespace RentTrackerClient.Services
{
    public class TransactionCategoryService : HttpClientService
    {
        public TransactionCategoryService(
            HttpClient httpClient,
            ILogger<TransactionCategoryService> logger,
            IAuthenticationService authService)
            : base(httpClient, "api/categories", logger, authService)
        {
            _logger.LogDebug("TransactionCategoryService initialized");
        }

        public async Task<List<PropertyTransactionCategory>> GetAllCategoriesAsync(TransactionType? type = null)
        {
            try
            {
                var queryString = type.HasValue ? $"?type={type.Value}" : "";
                var result = await GetListAsync<PropertyTransactionCategory>(queryString);
                
                if (!result.Any())
                {
                    _logger.LogDebug($"No categories found for type: {type}");
                    return new List<PropertyTransactionCategory>();
                }

                result = result
                    .Where(c => !type.HasValue || c.TransactionType == type.Value)
                    .OrderBy(c => c.Name)
                    .ToList();

                _logger.LogDebug($"Retrieved {result.Count} categories");
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting transaction categories");
                return new List<PropertyTransactionCategory>();
            }
        }

        public async Task<PropertyTransactionCategory?> GetCategoryByIdAsync(string categoryId)
        {
            try
            {
                _logger.LogDebug($"Getting category {categoryId}");
                var result = await GetAsync<PropertyTransactionCategory>($"{categoryId}");
                
                if (result == null)
                {
                    _logger.LogDebug($"Category not found: {categoryId}");
                }
                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting category {CategoryId}", categoryId);
                return null;
            }
        }

        public async Task<PropertyTransactionCategory?> CreateCategoryAsync(PropertyTransactionCategory category)
        {
            try
            {
                return await PostAsync<PropertyTransactionCategory>("", category);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating transaction category");
                return null;
            }
        }

        public async Task<bool> UpdateCategoryAsync(string categoryId, PropertyTransactionCategory category)
        {
            try
            {
                var result = await PutAsync<PropertyTransactionCategory>($"{categoryId}", category);
                return result != null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating category {CategoryId}", categoryId);
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(string categoryId)
        {
            try
            {
                await DeleteAsync($"{categoryId}");
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting category {CategoryId}", categoryId);
                return false;
            }
        }
    }
}
