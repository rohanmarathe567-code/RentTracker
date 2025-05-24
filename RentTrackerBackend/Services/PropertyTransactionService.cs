using RentTrackerBackend.Data;
using RentTrackerBackend.Models;

namespace RentTrackerBackend.Services
{
    public interface IPropertyTransactionService
    {
        Task<PropertyTransaction?> GetTransactionByIdAsync(string tenantId, string transactionId);
        Task<IEnumerable<PropertyTransaction>> GetTransactionsByPropertyAsync(string tenantId, string propertyId, bool includeSystem = true, string[]? includes = null);
        Task<PropertyTransaction?> UpdateTransactionAsync(string tenantId, string transactionId, PropertyTransaction updatedTransaction);
        Task<bool> DeleteTransactionAsync(string tenantId, string transactionId);
        Task<PropertyTransaction> CreateTransactionAsync(PropertyTransaction transaction);
        Task<List<PropertyTransaction>> CreateTransactionAsync(List<PropertyTransaction> transactions);
        Task<List<PropertyTransaction>> CreateTransactionAsync(string tenantId, string propertyId, List<PropertyTransactionDto> transactionDtos);
        Task<decimal> GetTotalIncomeByPropertyAsync(string tenantId, string propertyId, DateTime? startDate = null, DateTime? endDate = null);
        Task<decimal> GetTotalExpensesByPropertyAsync(string tenantId, string propertyId, DateTime? startDate = null, DateTime? endDate = null);
        Task<IEnumerable<PropertyTransaction>> GetTransactionsByTypeAsync(string tenantId, string propertyId, TransactionType type, DateTime? startDate = null, DateTime? endDate = null);
        Task<Dictionary<string, decimal>> GetTransactionsByCategoryAsync(string tenantId, string propertyId, TransactionType type, DateTime? startDate = null, DateTime? endDate = null);
    }

    public class PropertyTransactionService : IPropertyTransactionService
    {
        private readonly IPropertyTransactionRepository _transactionRepository;
        private readonly IMongoRepository<RentalProperty> _propertyRepository;
        private readonly ITransactionCategoryRepository _categoryRepository;

        public PropertyTransactionService(
            IPropertyTransactionRepository transactionRepository, 
            IMongoRepository<RentalProperty> propertyRepository,
            ITransactionCategoryRepository categoryRepository)
        {
            _transactionRepository = transactionRepository;
            _propertyRepository = propertyRepository;
            _categoryRepository = categoryRepository;
        }

        public async Task<PropertyTransaction?> GetTransactionByIdAsync(string tenantId, string transactionId)
        {
            return await _transactionRepository.GetByIdAsync(tenantId, transactionId);
        }

        private async Task ValidatePropertyExistsAsync(string tenantId, string propertyId)
        {
            var property = await _propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
            {
                throw new ArgumentException($"Property with ID {propertyId} not found.");
            }
        }

        public async Task<IEnumerable<PropertyTransaction>> GetTransactionsByPropertyAsync(string tenantId, string propertyId, bool includeSystem = true, string[]? includes = null)
        {
            await ValidatePropertyExistsAsync(tenantId, propertyId);
            var transactions = await _transactionRepository.GetAllAsync(tenantId, includeSystem, includes);
            return transactions.Where(t => t.RentalPropertyId == propertyId);
        }

        public async Task<PropertyTransaction?> UpdateTransactionAsync(string tenantId, string transactionId, PropertyTransaction updatedTransaction)
        {
            var existingTransaction = await _transactionRepository.GetByIdAsync(tenantId, transactionId);
            if (existingTransaction == null)
            {
                return null;
            }

            // Update only the modifiable fields
            existingTransaction.Amount = updatedTransaction.Amount;
            existingTransaction.TransactionDate = updatedTransaction.TransactionDate.Kind == DateTimeKind.Utc
                ? updatedTransaction.TransactionDate
                : DateTime.SpecifyKind(updatedTransaction.TransactionDate, DateTimeKind.Utc);
            existingTransaction.TransactionType = updatedTransaction.TransactionType;
            existingTransaction.CategoryId = updatedTransaction.CategoryId;
            existingTransaction.PaymentMethodId = updatedTransaction.PaymentMethodId;
            existingTransaction.Reference = updatedTransaction.Reference;
            existingTransaction.Notes = updatedTransaction.Notes;
            existingTransaction.AttachmentIds = updatedTransaction.AttachmentIds ?? new List<string>();
            existingTransaction.UpdatedAt = DateTime.UtcNow;

            await _transactionRepository.UpdateAsync(tenantId, transactionId, existingTransaction);
            return existingTransaction;
        }

        public async Task<bool> DeleteTransactionAsync(string tenantId, string transactionId)
        {
            try
            {
                await _transactionRepository.DeleteAsync(tenantId, transactionId);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void ValidateTransactions(List<PropertyTransaction> transactions)
        {
            if (transactions == null || !transactions.Any())
            {
                throw new ArgumentException("At least one transaction is required.");
            }

            foreach (var transaction in transactions)
            {
                if (transaction.Amount <= 0)
                {
                    throw new ArgumentException("Amount must be positive.");
                }

                if (transaction.TransactionDate == DateTime.MinValue ||
                    transaction.TransactionDate == DateTime.MaxValue)
                {
                    throw new ArgumentException("Transaction date is required and must be valid.");
                }

                if (string.IsNullOrEmpty(transaction.CategoryId))
                {
                    throw new ArgumentException("Category is required.");
                }
            }
        }

        public async Task<List<PropertyTransaction>> CreateTransactionAsync(string tenantId, string propertyId, List<PropertyTransactionDto> transactionDtos)
        {
            if (transactionDtos == null || !transactionDtos.Any())
            {
                throw new ArgumentException("At least one transaction is required.");
            }

            var transactions = transactionDtos.Select(dto =>
            {
                var t = PropertyTransactionDto.ToEntity(dto);
                t.RentalPropertyId = propertyId;
                t.TenantId = tenantId;
                return t;
            }).ToList();

            ValidateTransactions(transactions);
            return await CreateTransactionAsync(transactions);
        }

        public async Task<PropertyTransaction> CreateTransactionAsync(PropertyTransaction transaction)
        {
            var result = await CreateTransactionAsync(new List<PropertyTransaction> { transaction });
            return result.First();
        }

        public async Task<List<PropertyTransaction>> CreateTransactionAsync(List<PropertyTransaction> transactions)
        {
            ValidateTransactions(transactions);
            var createdTransactions = new List<PropertyTransaction>();

            foreach (var transaction in transactions)
            {
                await ValidatePropertyExistsAsync(transaction.TenantId, transaction.RentalPropertyId);
                
                // Try to get category from tenant's categories first, then from shared categories
                var category = await _categoryRepository.GetSharedByIdAsync(transaction.CategoryId);
                
                if (category == null)
                {
                    throw new ArgumentException($"Category with ID {transaction.CategoryId} not found.");
                }
                
                // Ensure the transaction type matches the category type
                if (transaction.TransactionType != category.TransactionType)
                {
                    throw new ArgumentException($"Transaction type must match category type.");
                }

                // Ensure TransactionDate is in UTC
                transaction.TransactionDate = transaction.TransactionDate.Kind == DateTimeKind.Utc
                    ? transaction.TransactionDate
                    : DateTime.SpecifyKind(transaction.TransactionDate, DateTimeKind.Utc);

                var createdTransaction = await _transactionRepository.CreateAsync(transaction);
                createdTransactions.Add(createdTransaction);
            }

            return createdTransactions;
        }
        
        public async Task<decimal> GetTotalIncomeByPropertyAsync(string tenantId, string propertyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetTransactionsByPropertyAsync(tenantId, propertyId, true, null);
            
            var query = transactions.Where(t => t.TransactionType == TransactionType.Income);
            
            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);
                
            return query.Sum(t => t.Amount);
        }
        
        public async Task<decimal> GetTotalExpensesByPropertyAsync(string tenantId, string propertyId, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetTransactionsByPropertyAsync(tenantId, propertyId, true, null);
            
            var query = transactions.Where(t => t.TransactionType == TransactionType.Expense);
            
            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);
                
            return query.Sum(t => t.Amount);
        }
        
        public async Task<IEnumerable<PropertyTransaction>> GetTransactionsByTypeAsync(string tenantId, string propertyId, TransactionType type, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetTransactionsByPropertyAsync(tenantId, propertyId, true, new[] { "Category", "PaymentMethod" });
            
            var query = transactions.Where(t => t.TransactionType == type);
            
            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);
                
            return query.OrderByDescending(t => t.TransactionDate);
        }
        
        public async Task<Dictionary<string, decimal>> GetTransactionsByCategoryAsync(string tenantId, string propertyId, TransactionType type, DateTime? startDate = null, DateTime? endDate = null)
        {
            var transactions = await GetTransactionsByPropertyAsync(tenantId, propertyId, true, new[] { "Category" });
            
            var query = transactions.Where(t => t.TransactionType == type);
            
            if (startDate.HasValue)
                query = query.Where(t => t.TransactionDate >= startDate.Value);
                
            if (endDate.HasValue)
                query = query.Where(t => t.TransactionDate <= endDate.Value);
                
            var result = new Dictionary<string, decimal>();
            
            foreach (var transaction in query)
            {
                var categoryName = transaction.Category?.Name ?? "Uncategorized";
                
                if (!result.ContainsKey(categoryName))
                {
                    result.Add(categoryName, 0);
                }
                
                result[categoryName] += transaction.Amount;
            }
            
            return result;
        }
    }
}
