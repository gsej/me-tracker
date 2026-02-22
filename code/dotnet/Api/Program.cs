using Api.Controllers;
using Api.Controllers.Report;
using Api.Filters;
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
        builder.Services.AddHttpContextAccessor();
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
        
        builder.Services.AddScoped<DataAccess.WeightRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["SqliteConnectionString"];
            return new DataAccess.WeightRepository(connectionString!);
        });

        builder.Services.AddScoped<DataAccess.UserRepository>(sp =>
        {
            var configuration = sp.GetRequiredService<IConfiguration>();
            var connectionString = configuration["SqliteConnectionString"];
            return new DataAccess.UserRepository(connectionString!);
        });

        builder.Services.AddSingleton(new Settings());
        builder.Services.AddScoped<ReportHandler>();
        
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
