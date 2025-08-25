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
    private readonly ReportHandler _reportHandler;

    public ReportController(TableServiceClient tableServiceClient, ReportHandler reportHandler)
    {
        _tableServiceClient = tableServiceClient;
        _reportHandler = reportHandler;
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
        
        var report = _reportHandler.GetReport(weightEntities);

        return report;
    }
}