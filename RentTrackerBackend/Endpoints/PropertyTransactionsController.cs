using RentTrackerBackend.Models;
using RentTrackerBackend.Models.Pagination;
using RentTrackerBackend.Services;
using Microsoft.AspNetCore.Mvc;

namespace RentTrackerBackend.Endpoints;

public static class PropertyTransactionsController
{
    public static void MapPropertyTransactionEndpoints(this WebApplication app)
    {
        // Get paginated transactions for a specific property
        app.MapGet("/api/properties/{propertyId}/transactions", async (
            string propertyId,
            [AsParameters] PaginationParameters parameters,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService,
            [FromQuery] TransactionType? type = null) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                // Parse the include parameter from query string
                string[]? includes = null;
                if (parameters.Include != null)
                {
                    includes = parameters.Include.Split(',', StringSplitOptions.RemoveEmptyEntries);
                }

                var transactions = await transactionService.GetTransactionsByPropertyAsync(tenantId, propertyId, includeSystem: true, includes: includes);
                
                // Filter by transaction type if specified
                if (type.HasValue)
                {
                    transactions = transactions.Where(t => t.TransactionType == type.Value);
                }
                
                // Manual filtering
                var filteredTransactions = transactions.AsQueryable();
                if (!string.IsNullOrWhiteSpace(parameters.SearchTerm))
                {
                    filteredTransactions = filteredTransactions.Where(t =>
                        (t.PaymentMethod != null && t.PaymentMethod.Name.Contains(parameters.SearchTerm)) ||
                        (t.Category != null && t.Category.Name.Contains(parameters.SearchTerm)) ||
                        (t.Reference != null && t.Reference.Contains(parameters.SearchTerm)) ||
                        (t.Notes != null && t.Notes.Contains(parameters.SearchTerm)));
                }

                // Apply sorting
                if (!string.IsNullOrWhiteSpace(parameters.SortField))
                {
                    filteredTransactions = parameters.SortField.ToLower() switch
                    {
                        "transactiondate" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.TransactionDate)
                            : filteredTransactions.OrderBy(t => t.TransactionDate),
                        "amount" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.Amount)
                            : filteredTransactions.OrderBy(t => t.Amount),
                        "paymentmethod" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.PaymentMethod!.Name)
                            : filteredTransactions.OrderBy(t => t.PaymentMethod!.Name),
                        "category" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.Category!.Name)
                            : filteredTransactions.OrderBy(t => t.Category!.Name),
                        "transactiontype" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.TransactionType)
                            : filteredTransactions.OrderBy(t => t.TransactionType),
                        "reference" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.Reference)
                            : filteredTransactions.OrderBy(t => t.Reference),
                        "notes" => parameters.SortDescending
                            ? filteredTransactions.OrderByDescending(t => t.Notes)
                            : filteredTransactions.OrderBy(t => t.Notes),
                        _ => filteredTransactions.OrderByDescending(t => t.TransactionDate)
                    };
                }
                else
                {
                    // Default sorting by transaction date, newest first
                    filteredTransactions = filteredTransactions.OrderByDescending(t => t.TransactionDate);
                }

                // Manual pagination
                var totalCount = filteredTransactions.Count();
                var items = filteredTransactions
                    .Skip((parameters.PageNumber - 1) * parameters.PageSize)
                    .Take(parameters.PageSize)
                    .ToList();

                var result = PaginatedResponse<PropertyTransaction>.Create(
                    items,
                    totalCount,
                    parameters.PageNumber,
                    parameters.PageSize);

                return Results.Ok(result);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Get a specific transaction by ID for a property
        app.MapGet("/api/properties/{propertyId}/transactions/{transactionId}", async (
            string propertyId,
            string transactionId,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var transaction = await transactionService.GetTransactionByIdAsync(tenantId, transactionId);
                if (transaction == null || transaction.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Transaction with ID {transactionId} not found for property {propertyId}");
                }

                return Results.Ok(transaction);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Create a new transaction for a property
        app.MapPost("/api/properties/{propertyId}/transactions", async (
            string propertyId,
            PropertyTransactionDto transactionDto,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var transaction = PropertyTransactionDto.ToEntity(transactionDto);
                transaction.RentalPropertyId = propertyId;
                transaction.TenantId = tenantId;

                var createdTransaction = await transactionService.CreateTransactionAsync(transaction);
                
                return Results.Created($"/api/properties/{propertyId}/transactions/{createdTransaction.Id}", createdTransaction);
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Update an existing transaction for a property
        app.MapPut("/api/properties/{propertyId}/transactions/{transactionId}", async (
            string propertyId,
            string transactionId,
            PropertyTransactionDto transactionDto,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var existingTransaction = await transactionService.GetTransactionByIdAsync(tenantId, transactionId);
                if (existingTransaction == null || existingTransaction.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Transaction with ID {transactionId} not found for property {propertyId}");
                }

                var transaction = PropertyTransactionDto.ToEntity(transactionDto);
                transaction.RentalPropertyId = propertyId;
                transaction.TenantId = tenantId;

                var updatedTransaction = await transactionService.UpdateTransactionAsync(tenantId, transactionId, transaction);
                return transaction != null
                    ? Results.NoContent()
                    : Results.NotFound($"Transaction with ID {transactionId} not found");
            }
            catch (ArgumentException ex)
            {
                return Results.BadRequest(ex.Message);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Delete a transaction for a property
        app.MapDelete("/api/properties/{propertyId}/transactions/{transactionId}", async (
            string propertyId,
            string transactionId,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var existingTransaction = await transactionService.GetTransactionByIdAsync(tenantId, transactionId);
                if (existingTransaction == null || existingTransaction.RentalPropertyId != propertyId)
                {
                    return Results.NotFound($"Transaction with ID {transactionId} not found for property {propertyId}");
                }

                var deleted = await transactionService.DeleteTransactionAsync(tenantId, transactionId);
                return deleted
                    ? Results.NoContent()
                    : Results.NotFound($"Transaction with ID {transactionId} not found");
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();

        // Get financial summary for a property
        app.MapGet("/api/properties/{propertyId}/financial-summary", async (
            string propertyId,
            IPropertyTransactionService transactionService,
            IPropertyService propertyService,
            IClaimsPrincipalService claimsPrincipalService,
            [FromQuery] DateTime? startDate = null,
            [FromQuery] DateTime? endDate = null) =>
        {
            try
            {
                if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
                {
                    return Results.Unauthorized();
                }

                // Verify property ownership
                var property = await propertyService.GetPropertyByIdAsync(tenantId, propertyId);
                if (property == null)
                    return Results.NotFound("Property not found");

                var totalIncome = await transactionService.GetTotalIncomeByPropertyAsync(tenantId, propertyId, startDate, endDate);
                var totalExpenses = await transactionService.GetTotalExpensesByPropertyAsync(tenantId, propertyId, startDate, endDate);
                var netCashflow = totalIncome - totalExpenses;

                var incomeByCategory = await transactionService.GetTransactionsByCategoryAsync(
                    tenantId, propertyId, TransactionType.Income, startDate, endDate);
                    
                var expensesByCategory = await transactionService.GetTransactionsByCategoryAsync(
                    tenantId, propertyId, TransactionType.Expense, startDate, endDate);

                var summary = new
                {
                    PropertyId = propertyId,
                    PropertyName = property.Address.ToString(),
                    TotalIncome = totalIncome,
                    TotalExpenses = totalExpenses,
                    NetCashflow = netCashflow,
                    DateRange = new 
                    {
                        StartDate = startDate ?? DateTime.MinValue,
                        EndDate = endDate ?? DateTime.UtcNow
                    },
                    IncomeByCategory = incomeByCategory,
                    ExpensesByCategory = expensesByCategory
                };

                return Results.Ok(summary);
            }
            catch (Exception ex)
            {
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .RequireAuthorization();
    }
}
