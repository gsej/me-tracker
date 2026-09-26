using Api.Controllers.Models;
using Api.DataAccess;
using AwesomeAssertions;
using AwesomeAssertions.Execution;

namespace Tests.DataAccess;

public class WeightRepositoryTests : IDisposable
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"weight-repo-tests-{Guid.NewGuid():N}.db");
    private readonly WeightRepository _repository;

    public WeightRepositoryTests()
    {
        _repository = new WeightRepository($"Data Source={_dbPath}");
    }

    public void Dispose()
    {
        if (File.Exists(_dbPath)) File.Delete(_dbPath);
    }

    [Fact]
    public async Task AddThenGetById_RoundTripsAllFields()
    {
        var id = Guid.NewGuid();
        var date = new DateTime(2025, 1, 15, 6, 30, 0, DateTimeKind.Utc);
        await _repository.AddAsync(new WeightEntity(id, "alice", date, 82.5m, false));

        var loaded = await _repository.GetByIdAsync(id, "alice");

        using var _ = new AssertionScope();
        loaded.Should().NotBeNull();
        loaded!.WeightId.Should().Be(id);
        loaded.UserId.Should().Be("alice");
        loaded.Weight.Should().Be(82.5m);
        loaded.Deleted.Should().BeFalse();
        // Guards the "dates as UTC regardless of server timezone" behaviour.
        loaded.Date.Should().Be(date);
        loaded.Date.Kind.Should().Be(DateTimeKind.Utc);
    }

    [Fact]
    public async Task GetByIdAsync_DoesNotReturnAnotherUsersRecord()
    {
        var id = Guid.NewGuid();
        await _repository.AddAsync(new WeightEntity(id, "alice", new DateTime(2025, 1, 15), 82.5m, false));

        var asBob = await _repository.GetByIdAsync(id, "bob");

        asBob.Should().BeNull();
    }

    [Fact]
    public async Task GetAllAsync_FiltersByUserAndExcludesDeleted()
    {
        await _repository.AddAsync(new WeightEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 1), 80m, false));
        await _repository.AddAsync(new WeightEntity(Guid.NewGuid(), "alice", new DateTime(2025, 1, 2), 81m, true));
        await _repository.AddAsync(new WeightEntity(Guid.NewGuid(), "bob", new DateTime(2025, 1, 3), 90m, false));

        var aliceRecords = (await _repository.GetAllAsync("alice")).ToList();

        aliceRecords.Should().ContainSingle();
        aliceRecords[0].Weight.Should().Be(80m);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletes_RowRemainsButIsExcludedFromReads()
    {
        var id = Guid.NewGuid();
        var entity = new WeightEntity(id, "alice", new DateTime(2025, 1, 15), 82.5m, false);
        await _repository.AddAsync(entity);

        await _repository.DeleteAsync(entity);

        using var _ = new AssertionScope();
        (await _repository.GetByIdAsync(id, "alice")).Should().BeNull();
        (await _repository.GetAllAsync("alice")).Should().BeEmpty();
        // Still physically present (backup reads everything, including deleted).
        (await _repository.GetAllAsync()).Should().ContainSingle(e => e.WeightId == id && e.Deleted);
    }

    [Fact]
    public async Task HardDeleteAsync_RemovesTheRowEntirely()
    {
        var id = Guid.NewGuid();
        var entity = new WeightEntity(id, "alice", new DateTime(2025, 1, 15), 82.5m, false);
        await _repository.AddAsync(entity);

        await _repository.HardDeleteAsync(entity);

        (await _repository.GetAllAsync()).Should().BeEmpty();
    }
}
