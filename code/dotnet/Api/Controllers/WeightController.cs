using Api.Controllers.Models;
using Api.Filters;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Controllers;

[ApiController]
[Route("api/weight")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class WeightController : ControllerBase
{
    private readonly TableServiceClient _tableServiceClient;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public WeightController(TableServiceClient tableServiceClient, IHttpContextAccessor httpContextAccessor)
    {
        _tableServiceClient = tableServiceClient;
        _httpContextAccessor = httpContextAccessor;
    }
    
    [HttpGet("/api/weights")]
    public async Task<WeightsCollection> GetAll()
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);
        
        var userId = _httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname];

        // Ensure the table exists
        await tableClient.CreateIfNotExistsAsync();

        var weightRecords = new List<WeightRecord>();
        var queryResults = tableClient
            .QueryAsync<WeightEntity>($"PartitionKey eq '{Constants.PartitionKey}' and UserId eq '{userId}' and Deleted eq false");

        await foreach (var entity in queryResults)
        {
            weightRecords.Add(new WeightRecord(entity.WeightId, entity.UserId, entity.Date, entity.Weight));
        }

        var orderedWeightRecords = weightRecords.OrderBy(record => record.Date);

        return new WeightsCollection(orderedWeightRecords);
    }
    
    [HttpGet("{weightId:guid}")]
    public async Task<IActionResult> GetWeightRecord(Guid weightId)
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);

        await tableClient.CreateIfNotExistsAsync();
        
        var queryResults = tableClient.QueryAsync<WeightEntity>($"PartitionKey eq '{Constants.PartitionKey}' and WeightId eq guid'{weightId}' and Deleted eq false");

        WeightEntity? entity = null;
        await foreach (var result in queryResults)
        {
            entity = result;
            break; // Stop after finding the first match
        }

        if (entity == null)
        {
            return NotFound($"Weight record with ID {weightId} not found.");
        }

        return Ok(new WeightRecord(entity.WeightId, entity.UserId, entity.Date, entity.Weight));
    }

    [HttpPost]
    public async Task<IActionResult> PostWeightRecord([FromBody] CreateWeightRecordRequest request)
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);
        
        await tableClient.CreateIfNotExistsAsync();

        var weightId = Guid.NewGuid();
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname]!;
        
        var entity = new WeightEntity(weightId, userId, request.Date, request.Weight);
        
        await tableClient.AddEntityAsync(entity);
            
        Response.Headers.Append("Location", $"/api/weight/{weightId}");

        return CreatedAtAction(nameof(GetWeightRecord), new { weightId }, null);
    }
    
    [HttpDelete("{weightId:guid}")]
    public async Task<IActionResult> DeleteWeightRecord(Guid weightId)
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);

        await tableClient.CreateIfNotExistsAsync();

        var queryResults = tableClient.QueryAsync<WeightEntity>($"PartitionKey eq '{Constants.PartitionKey}' and WeightId eq guid'{weightId}'");

        WeightEntity? entityToDelete = null;
        await foreach (var entity in queryResults)
        {
            entityToDelete = entity;
            break; // Stop after finding the first match
        }

        if (entityToDelete != null)
        {
            entityToDelete.Deleted = true;
            await tableClient.UpdateEntityAsync(entityToDelete, ETag.All);
        }

        return NoContent();
    }

    public class WeightRecordExample : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(WeightRecord))
                schema.Example = new OpenApiObject
                {
                    ["date"] = new OpenApiDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)), ["weight"] = new OpenApiDouble(75.5)
                };
        }
    }
}