using Microsoft.OpenApi.Models;
using System.Reflection;
using RentTrackerBackend.Filters;

namespace RentTrackerBackend.Extensions
{
    public static class SwaggerExtensions
    {
        public static IServiceCollection AddSwaggerConfiguration(this IServiceCollection services)
        {
            services.AddSwaggerGen(c =>
            {
                // Add JWT token operation filter to automatically add "Bearer" prefix
                c.OperationFilter<AuthorizationHeaderOperationFilter>();

                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "RentTracker API",
                    Version = "v1",
                    Description = "API for managing rental properties and transactions"
                });

                // Configure JWT authentication
                c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
                {
                    Description = "JWT Authorization header using the Bearer scheme. Enter your token ONLY (without 'Bearer' prefix)",
                    Name = "Authorization",
                    In = ParameterLocation.Header,
                    Type = SecuritySchemeType.Http,
                    Scheme = "Bearer",
                    BearerFormat = "JWT"
                });

                c.AddSecurityRequirement(new OpenApiSecurityRequirement
                {
                    {
                        new OpenApiSecurityScheme
                        {
                            Reference = new OpenApiReference
                            {
                                Type = ReferenceType.SecurityScheme,
                                Id = "Bearer"
                            }
                        },
                        Array.Empty<string>()
                    }
                });

                // Add XML comments if they exist
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });

            return services;
        }
    }
}