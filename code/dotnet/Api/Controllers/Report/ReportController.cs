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
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportController(TableServiceClient tableServiceClient, ReportHandler reportHandler, IHttpContextAccessor httpContextAccessor)
    {
        _tableServiceClient = tableServiceClient;
        _reportHandler = reportHandler;
        _httpContextAccessor = httpContextAccessor;
    }
     
    [HttpGet]
    public async Task<WeightReport> Get()
    {
        var tableClient = _tableServiceClient.GetTableClient(Constants.TableName);
        await tableClient.CreateIfNotExistsAsync();
        
        var userId = _httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname];
      
        var queryResults = tableClient
            .QueryAsync<WeightEntity>($"PartitionKey eq '{Constants.PartitionKey}' and UserId eq '{userId}' and Deleted eq false");
        
        var weightEntities = new List<WeightEntity>();
        await foreach (var entity in queryResults)
        {
            weightEntities.Add(entity);
        }
        
        var report = _reportHandler.GetReport(weightEntities);

        return report;
    }
}