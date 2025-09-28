using Azure;
using Azure.Data.Tables;

namespace Api.Controllers.Models;

public class UserEntity : ITableEntity
{
    public static class Constants
    {
        public const string TableName = "Users";
        public const string PartitionKey = "Users";
    }
    
    public UserEntity()
    {
    }
    
    public UserEntity(string userId, int heightInCm)
    {
        UserId = userId;
        HeightInCm = heightInCm;

        RowKey = userId;
    }
    
    public string UserId { get; init; }

    public int HeightInCm { get; set; }

    public string PartitionKey { get; set; } = Constants.PartitionKey;
    public string RowKey { get; set; } = null!;

    public ETag ETag { get; set; } = ETag.All;
    public DateTimeOffset? Timestamp { get; set; }
}