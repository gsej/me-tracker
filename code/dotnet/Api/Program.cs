using Api.Controllers;
using Api.Filters;
using Azure.Data.Tables;
using Microsoft.OpenApi.Models;

namespace Api;

public static class Program
{
    public static void Main(params string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Configuration
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables();

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("AllowAllOrigins",
                policy =>
                {
                    policy.AllowAnyOrigin();
                    policy.AllowAnyMethod();
                    policy.AllowAnyHeader();
                });
        });
        
        
        
       
        builder.Services.AddControllers();
        builder.Services.AddScoped<ApiKeyAuthFilter>();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(s =>
        {
            s.SchemaFilter<WeightController.WeightRecordExample>();
            s.CustomSchemaIds(x => x.FullName);
            
            s.AddSecurityDefinition("ApiKey", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.ApiKey,
                In = ParameterLocation.Header,
                Name = "X-API-Key",
                Description = "API Key Authentication"
            });

            s.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "ApiKey"
                        }
                    },
                    Array.Empty<string>()
                }
            });
        });
        
        builder.Services.AddSingleton(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["Azure:TableStorage:ConnectionString"];
            return new TableServiceClient(connectionString);
        });
        
        var app = builder.Build();

        app.UseSwagger();
        app.UseSwaggerUI();

        app.UseHttpsRedirection();

        app.UseCors("AllowAllOrigins");

        app.MapGet("/", http =>
        {
            http.Response.Redirect("/swagger/index.html", false);
            return Task.CompletedTask;
        });

        app.MapControllers();

        app.Run();
    }
}
