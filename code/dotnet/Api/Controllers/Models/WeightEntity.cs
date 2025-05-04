using System.Globalization;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace Api.Controllers.Models;

public static class Constants
{
    public const string TableName = "Weights";
    public const string PartitionKey = "WeightEntries";
}

public class WeightEntity : ITableEntity
{
    
    
    public WeightEntity()
    {
    }
    
    public WeightEntity(Guid weightId, DateTime date, decimal weight)
    {
        WeightId = weightId;
        Date = date;
        Weight = weight;
        RowKey = Guid.NewGuid().ToString();
    }
    
    public Guid WeightId { get; init; }
    
    public DateTime Date { get; init; }

    [IgnoreDataMember] 
    public decimal Weight { get; set; }

    [DataMember(Name = "Weight")]
    public string WeightString
    {
        get => Weight.ToString(CultureInfo.InvariantCulture);
        set => Weight = decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public string PartitionKey { get; set; } = Constants.PartitionKey;
    public string RowKey { get; set; } = null!;

    public ETag ETag { get; set; } = ETag.All;
    public DateTimeOffset? Timestamp { get; set; }
}