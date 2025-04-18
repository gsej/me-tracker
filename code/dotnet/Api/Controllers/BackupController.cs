using Api.Controllers.Models;
using Api.Filters;
using Azure;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/backup")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class BackupController : ControllerBase
{
    private const string TableName = "Weights";
    private const string PartitionKey = "WeightEntries";
    private readonly TableServiceClient _tableServiceClient;

    public BackupController(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }

    [HttpGet]
    public async Task<WeightsCollection> GetAllWeightRecords()
    {
        var tableClient = _tableServiceClient.GetTableClient(TableName);

        // Ensure the table exists
        await tableClient.CreateIfNotExistsAsync();

        var weightRecords = new List<WeightRecord>();
        var queryResults = tableClient
            .QueryAsync<WeightEntity>($"PartitionKey eq '{PartitionKey}'");

        await foreach (var entity in queryResults)
        {
            weightRecords.Add(new WeightRecord(entity.WeightId, entity.Date, entity.Weight));
        }

        var orderedWeightRecords = weightRecords.OrderBy(record => record.Date);

        return new WeightsCollection(orderedWeightRecords);
    }

    [HttpPost("restore")]
    public async Task<IActionResult> RestoreWeightRecords([FromBody] WeightsCollection? records)
    {
        if (records == null || records.WeightRecords == null)
            return BadRequest("No records provided");

        var tableClient = _tableServiceClient.GetTableClient(TableName);
        
        await tableClient.CreateIfNotExistsAsync();
        
        var existingRecords = tableClient.QueryAsync<WeightEntity>($"PartitionKey eq '{PartitionKey}'");
        await foreach (var entity in existingRecords) await tableClient.DeleteEntityAsync(PartitionKey, entity.RowKey);

        foreach (var weightRecord in records.WeightRecords)
        {
            var entity = new WeightEntity(weightRecord.WeightId, weightRecord.Date, weightRecord.Weight);
            await tableClient.AddEntityAsync(entity);
        }

        return Ok();
    }
}