using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;

namespace Tests.Integration;

public class BackupApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Restore_WithNullBody_ReturnsBadRequest()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var response = await client.PostAsJsonAsync<WeightsCollection?>("/api/backup/weights/restore", null);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Restore_ReplacesAllExistingRecords()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        var original = new WeightsCollection(new[]
        {
            new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 1), 80m, false),
            new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 1, 2), 81m, false),
        });
        await client.PostAsJsonAsync("/api/backup/weights/restore", original);

        var replacement = new WeightsCollection(new[]
        {
            new WeightRecord(Guid.NewGuid(), ApiTestFactory.AliceUserId, new DateTime(2025, 2, 1), 79m, false),
        });
        await client.PostAsJsonAsync("/api/backup/weights/restore", replacement);

        var collection = await client.GetFromJsonAsync<WeightsCollection>("/api/backup/weights");

        var records = collection!.WeightRecords!.ToList();
        records.Should().ContainSingle();
        records[0].Weight.Should().Be(79m);
    }
}
