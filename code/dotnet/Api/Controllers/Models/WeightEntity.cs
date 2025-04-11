using System.Globalization;
using System.Runtime.Serialization;
using Azure;
using Azure.Data.Tables;

namespace Api.Controllers;

public class WeightEntity : ITableEntity
{
    public DateTime Date { get; set; }

    [IgnoreDataMember] public decimal Weight { get; set; }

    [DataMember(Name = "Weight")]
    public string WeightString
    {
        get => Weight.ToString(CultureInfo.InvariantCulture);
        set => Weight = decimal.Parse(value, CultureInfo.InvariantCulture);
    }

    public string PartitionKey { get; set; }
    public string RowKey { get; set; }

    public ETag ETag { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}