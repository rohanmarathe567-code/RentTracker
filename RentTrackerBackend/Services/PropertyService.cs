using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Extensions;

namespace RentTrackerBackend.Services
{
    public interface IPropertyService
    {
        Task<RentalProperty?> GetPropertyByIdAsync(string tenantId, string propertyId);
        Task<PaginatedResponse<RentalProperty>> GetPropertiesAsync(string tenantId, PaginationParameters parameters);
        Task<RentalProperty> CreatePropertyAsync(RentalProperty property);
        Task<RentalProperty?> UpdatePropertyAsync(string tenantId, string propertyId, RentalProperty updatedProperty);
        Task<bool> DeletePropertyAsync(string tenantId, string propertyId);
    }

    public class PropertyService : IPropertyService
    {
        private readonly IMongoRepository<RentalProperty> _propertyRepository;
        private readonly ILogger<PropertyService> _logger;

        public PropertyService(IMongoRepository<RentalProperty> propertyRepository, ILogger<PropertyService> logger)
        {
            _propertyRepository = propertyRepository;
            _logger = logger;
        }

        public async Task<RentalProperty?> GetPropertyByIdAsync(string tenantId, string propertyId)
        {
            return await _propertyRepository.GetByIdAsync(tenantId, propertyId);
        }

        public async Task<PaginatedResponse<RentalProperty>> GetPropertiesAsync(string tenantId, PaginationParameters parameters)
        {
            parameters.Validate();

            var properties = await _propertyRepository.GetAllAsync(tenantId);
            var query = properties.AsQueryable();

            // Apply search filtering if needed
            if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
            {
                var searchTerm = parameters.SearchTerm.ToLower();
                query = query.Where(p =>
                    p.Address.Street.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Address.City.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Address.State.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Address.ZipCode.Contains(searchTerm) ||
                    p.PropertyManager.Name.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    p.PropertyManager.Contact.Contains(searchTerm, StringComparison.CurrentCultureIgnoreCase) ||
                    p.Description.ToLower().Contains(searchTerm));
            }

            // Apply sorting if specified
            if (!string.IsNullOrWhiteSpace(parameters.SortField))
            {
                query = parameters.SortField.ToLower() switch
                {
                    "address" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.Street)
                        : query.OrderBy(p => p.Address.Street),
                    "city" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.City)
                        : query.OrderBy(p => p.Address.City),
                    "state" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.Address.State)
                        : query.OrderBy(p => p.Address.State),
                    "rentamount" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.RentAmount)
                        : query.OrderBy(p => p.RentAmount),
                    "leasestartdate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseDates.StartDate)
                        : query.OrderBy(p => p.LeaseDates.StartDate),
                    "leaseenddate" => parameters.SortDescending
                        ? query.OrderByDescending(p => p.LeaseDates.EndDate)
                        : query.OrderBy(p => p.LeaseDates.EndDate),
                    _ => query.OrderBy(p => p.Address.Street) // Default sort
                };
            }

            return query.ToPaginatedList(parameters);
        }

        public async Task<RentalProperty> CreatePropertyAsync(RentalProperty property)
        {
            if (string.IsNullOrWhiteSpace(property.Address.Street))
            {
                throw new ArgumentException("Address is required");
            }

            _logger.LogDebug("Creating new property");
            var createdProperty = await _propertyRepository.CreateAsync(property);
            _logger.LogDebug("Created property {Id}", createdProperty.FormattedId);
            return createdProperty;
        }

        public async Task<RentalProperty?> UpdatePropertyAsync(string tenantId, string propertyId, RentalProperty updatedProperty)
        {
            var existingProperty = await _propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (existingProperty == null)
            {
                return null;
            }

            // Create new property instance with existing ID and preserved fields
            var propertyToUpdate = new RentalProperty
            {
                Id = existingProperty.Id,
                TenantId = tenantId,
                CreatedAt = existingProperty.CreatedAt,
                UpdatedAt = DateTime.UtcNow,
                Version = existingProperty.Version,
                Address = updatedProperty.Address,
                Description = updatedProperty.Description,
                RentAmount = updatedProperty.RentAmount,
                LeaseDates = updatedProperty.LeaseDates,
                PropertyManager = updatedProperty.PropertyManager,
                TransactionIds = existingProperty.TransactionIds,
                AttachmentIds = existingProperty.AttachmentIds
            };

            await _propertyRepository.UpdateAsync(tenantId, propertyId, propertyToUpdate);
            return propertyToUpdate;
        }

        public async Task<bool> DeletePropertyAsync(string tenantId, string propertyId)
        {
            var property = await _propertyRepository.GetByIdAsync(tenantId, propertyId);
            if (property == null)
            {
                return false;
            }

            await _propertyRepository.DeleteAsync(tenantId, propertyId);
            return true;
        }
    }
}