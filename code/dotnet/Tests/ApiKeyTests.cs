using Api.Filters;
using FluentAssertions;

namespace Tests;

public class ApiKeyTests
{
    [Fact]
    public void TrySerialize()
    {
        var keys = new List<ApiKey>
        {
            new ("user1", "key1"), 
            new ("user2", "key2"),
        };
        
        var json = System.Text.Json.JsonSerializer.Serialize(keys);

        json.Should().Be("[{\"UserId\":\"user1\",\"Key\":\"key1\"},{\"UserId\":\"user2\",\"Key\":\"key2\"}]");
    }
}