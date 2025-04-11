using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeightController : ControllerBase
{
    private readonly TableServiceClient _tableServiceClient;

    public WeightController(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }
    [HttpPost]
    public async Task<IActionResult> PostWeightEntry([FromBody] WeightRecord entry)
    {
        var tableName = "Weights";
        var tableClient = _tableServiceClient.GetTableClient(tableName);

        // Ensure the table exists
        await tableClient.CreateIfNotExistsAsync();

        var entity = new WeightEntity
        {
            Date = entry.Date,
            Weight = entry.Weight,
            PartitionKey = "WeightEntries",
            RowKey = Guid.NewGuid().ToString(),
            ETag = ETag.All
        };

        // Add the record to the table
        await tableClient.AddEntityAsync(entity);
        
        
        return Accepted();
    }

    public class WeightRecordExample : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(WeightRecord))
            {
                schema.Example = new OpenApiObject()
                {
                    ["date"] = new OpenApiDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    ["weight"] = new OpenApiDouble(75.5)
                };
            }
        }
    }
}