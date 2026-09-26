using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.Integration;

public class WeightApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task GetWeights_WithoutApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/weights");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task GetWeights_WithUnknownApiKey_ReturnsUnauthorized()
    {
        var client = _factory.CreateClientWithKey("not-a-real-key");

        var response = await client.GetAsync("/api/weights");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task PostThenGet_RoundTripsTheRecord()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var date = new DateTime(2025, 1, 15, 0, 0, 0, DateTimeKind.Utc);

        var post = await client.PostAsJsonAsync("/api/weight", new CreateWeightRecordRequest(date, 82.5m));

        post.StatusCode.Should().Be(HttpStatusCode.Created);

        var collection = await client.GetFromJsonAsync<WeightsCollection>("/api/weights");

        using var _ = new AssertionScope();
        var records = collection!.WeightRecords!.ToList();
        records.Should().HaveCount(1);
        records[0].Weight.Should().Be(82.5m);
        records[0].Date.Should().Be(date);
        records[0].UserId.Should().Be(ApiTestFactory.AliceUserId);
        records[0].Deleted.Should().BeFalse();
    }

    [Fact]
    public async Task Weights_AreScopedPerUser()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));

        var bobCollection = await bob.GetFromJsonAsync<WeightsCollection>("/api/weights");

        bobCollection!.WeightRecords.Should().BeEmpty();
    }

    [Fact]
    public async Task GetById_ForAnotherUsersRecord_ReturnsNotFound()
    {
        var alice = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        var bob = _factory.CreateClientWithKey(ApiTestFactory.BobKey);

        await alice.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var aliceRecord = (await alice.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var bobResponse = await bob.GetAsync($"/api/weight/{aliceRecord.WeightId}");

        bobResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Delete_SoftDeletes_SoTheRecordDisappearsFromReads()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 82.5m));
        var record = (await client.GetFromJsonAsync<WeightsCollection>("/api/weights"))!
            .WeightRecords!.Single();

        var delete = await client.DeleteAsync($"/api/weight/{record.WeightId}");

        using var _ = new AssertionScope();
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getById = await client.GetAsync($"/api/weight/{record.WeightId}");
        getById.StatusCode.Should().Be(HttpStatusCode.NotFound);

        var collection = await client.GetFromJsonAsync<WeightsCollection>("/api/weights");
        collection!.WeightRecords.Should().BeEmpty();
    }
}
