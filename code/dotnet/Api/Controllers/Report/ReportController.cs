using Api.Controllers.Models;
using Api.Filters;
using Azure.Data.Tables;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Report;

[ApiController]
[Route("api/report")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class ReportController : ControllerBase
{
    private readonly TableServiceClient _tableServiceClient;

    public ReportController(TableServiceClient tableServiceClient)
    {
        _tableServiceClient = tableServiceClient;
    }
    
    [HttpGet]
    public async Task<WeightReport> Get()
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);
        await tableClient.CreateIfNotExistsAsync();
      
        var queryResults = tableClient
            .QueryAsync<WeightEntity>($"PartitionKey eq '{Constants.PartitionKey}'");
        
        var weightEntities = new List<WeightEntity>();
        await foreach (var entity in queryResults)
        {
            weightEntities.Add(entity);
        }
        
        var reportHandler = new ReportHandler();
        
        var report = reportHandler.GetReport(weightEntities);

        return report;
    }
}