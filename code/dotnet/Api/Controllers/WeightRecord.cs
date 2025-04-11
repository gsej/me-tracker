using Azure;
using Azure.Data.Tables;

namespace Api.Controllers;

public record WeightRecord(DateTime Date, decimal Weight);

public class WeightEntity : ITableEntity
{
    public string PartitionKey { get; set; }
    public string RowKey { get; set; }
    public DateTime Date { get; set; }
    public decimal Weight { get; set; }
    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
    
}