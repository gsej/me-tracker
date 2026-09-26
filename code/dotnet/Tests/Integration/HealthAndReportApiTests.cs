using System.Net;
using System.Net.Http.Json;
using Api.Controllers.Models;
using Api.Controllers.Report;
using AwesomeAssertions;

namespace Tests.Integration;

public class HealthAndReportApiTests : IDisposable
{
    private readonly ApiTestFactory _factory = new();

    public void Dispose() => _factory.Dispose();

    [Fact]
    public async Task Healthz_RequiresNoApiKey()
    {
        var client = _factory.CreateClientWithKey(null);

        var response = await client.GetAsync("/api/healthz");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Report_ReturnsAnEntryForThePostedWeight()
    {
        var client = _factory.CreateClientWithKey(ApiTestFactory.AliceKey);

        // The report looks up the user's height, so the user row must exist.
        await client.PostAsJsonAsync("/api/backup/users/restore",
            new UsersCollection(new[] { new User(ApiTestFactory.AliceUserId, 180) }));
        await client.PostAsJsonAsync("/api/weight",
            new CreateWeightRecordRequest(new DateTime(2025, 1, 15), 90m));

        var report = await client.GetFromJsonAsync<WeightReport>("/api/report");

        report!.Entries.Should().ContainSingle();
        report.Entries[0].AverageWeight.Should().Be(90m);
    }
}
