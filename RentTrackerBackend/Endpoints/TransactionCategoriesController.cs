using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using RentTrackerBackend.Data;
using RentTrackerBackend.Models;
using RentTrackerBackend.Services;
using MongoDB.Driver;

namespace RentTrackerBackend.Endpoints;

public static class TransactionCategoriesController
{
    public static void MapTransactionCategoryEndpoints(this WebApplication app)
    {
        // Get all transaction categories
        app.MapGet("/api/categories", async (
            ITransactionCategoryRepository repository,
            IClaimsPrincipalService claimsPrincipalService,
            ILogger<PropertyTransactionCategory> logger,
            [FromQuery] TransactionType? type = null) =>
        {
            try 
            {
                logger.LogInformation("GET request received for transaction categories");
                
                // Validate tenant ID but always return system categories
                bool hasTenantId = claimsPrincipalService.ValidateTenantId(out string tenantId);
                if (!hasTenantId)
                {
                    logger.LogWarning("No valid tenant ID found in request, returning only system categories");
                    // Even without tenant authentication, we can still provide system categories
                    var onlySystemDefaults = await repository.GetAllAsync("system");
                    var systemFilteredCategories = type.HasValue 
                        ? onlySystemDefaults.Where(c => c.TransactionType == type.Value) 
                        : onlySystemDefaults;
                    return Results.Ok(systemFilteredCategories.ToList());
                }
                
                logger.LogInformation($"Fetching all transaction categories for authenticated user");

                // Get all categories since user is authenticated
                var allCategories = await repository.GetAllSharedAsync();
                
                // Apply type filter if provided
                var filteredCategories = type.HasValue 
                    ? allCategories.Where(c => c.TransactionType == type.Value) 
                    : allCategories;
                
                // Order by Order field, then by Name
                var orderedCategories = filteredCategories
                    .OrderBy(c => c.Order)
                    .ThenBy(c => c.Name)
                    .ToList();
                
                logger.LogInformation($"Returning {orderedCategories.Count} transaction categories");
                return Results.Ok(orderedCategories);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error processing transaction categories request");
                return Results.Problem($"An error occurred: {ex.Message}", statusCode: 500);
            }
        })
        .WithName("GetTransactionCategories")
        .Produces<IEnumerable<PropertyTransactionCategory>>(StatusCodes.Status200OK)
        .RequireAuthorization();  // Require authentication

        // Create a new transaction category
        app.MapPost("/api/categories", async (
            ITransactionCategoryRepository repository,
            IClaimsPrincipalService claimsPrincipalService,
            PropertyTransactionCategory category) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            // Only admins can create system-wide categories
            if (category.IsSystemDefault && !claimsPrincipalService.IsInRole("Admin"))
                return Results.Forbid();

            // Set the appropriate tenant ID based on whether it's a system default
            category.TenantId = category.IsSystemDefault ? "system" : tenantId;
            category.UserId = tenantId;

            var createdCategory = await repository.CreateAsync(category);

            return Results.CreatedAtRoute(
                "GetTransactionCategories",
                new { id = createdCategory.Id.ToString() },
                createdCategory
            );
        })
        .WithName("CreateTransactionCategory")
        .Produces<PropertyTransactionCategory>(StatusCodes.Status201Created)
        .Produces(StatusCodes.Status400BadRequest)
        .RequireAuthorization();  // Require authentication
        
        // Get a specific transaction category by ID
        app.MapGet("/api/categories/{id}", async (
            string id,
            ITransactionCategoryRepository repository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string _))
            {
                return Results.Unauthorized();
            }

            var category = await repository.GetSharedByIdAsync(id);
            
            if (category == null)
            {
                return Results.NotFound($"Category with ID {id} not found");
            }

            return Results.Ok(category);
        })
        .WithName("GetTransactionCategoryById")
        .Produces<PropertyTransactionCategory>(StatusCodes.Status200OK)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();  // Require authentication
        
        // Update an existing transaction category
        app.MapPut("/api/categories/{id}", async (
            string id,
            PropertyTransactionCategory updatedCategory,
            ITransactionCategoryRepository repository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var existingCategory = await repository.GetSharedByIdAsync(id);
            if (existingCategory == null)
            {
                return Results.NotFound($"Category with ID {id} not found");
            }

            // Only admins can update system categories
            if (existingCategory.IsSystemDefault && !claimsPrincipalService.IsInRole("Admin"))
                return Results.Forbid();

            // Preserve tenant ID and system default status
            updatedCategory.TenantId = existingCategory.TenantId;
            updatedCategory.IsSystemDefault = existingCategory.IsSystemDefault;
            
            // Update existing category's properties
            existingCategory.Name = updatedCategory.Name;
            existingCategory.Description = updatedCategory.Description;
            existingCategory.TransactionType = updatedCategory.TransactionType;
            existingCategory.Order = updatedCategory.Order;
            existingCategory.UserId = updatedCategory.UserId;
            existingCategory.UpdatedAt = DateTime.UtcNow;
            
            // Use the existing category with updated properties
            updatedCategory = existingCategory;
            
            await repository.UpdateAsync(tenantId, id, updatedCategory);
            
            return Results.NoContent();
        })
        .WithName("UpdateTransactionCategory")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();  // Require authentication
        
        // Delete a transaction category
        app.MapDelete("/api/categories/{id}", async (
            string id,
            ITransactionCategoryRepository repository,
            IClaimsPrincipalService claimsPrincipalService) =>
        {
            if (!claimsPrincipalService.ValidateTenantId(out string tenantId))
            {
                return Results.Unauthorized();
            }

            var existingCategory = await repository.GetSharedByIdAsync(id);
            if (existingCategory == null)
            {
                return Results.NotFound($"Category with ID {id} not found");
            }

            // Only admins can delete system categories
            if (existingCategory.IsSystemDefault && !claimsPrincipalService.IsInRole("Admin"))
                return Results.Forbid();
                
            await repository.DeleteAsync(tenantId, id);
            
            return Results.NoContent();
        })
        .WithName("DeleteTransactionCategory")
        .Produces(StatusCodes.Status204NoContent)
        .Produces(StatusCodes.Status404NotFound)
        .RequireAuthorization();  // Require authentication
    }
}
