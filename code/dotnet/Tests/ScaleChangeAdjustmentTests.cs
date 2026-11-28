using Api.Controllers.Models;
using Api.Controllers.Report;
using AwesomeAssertions;

namespace Tests;

// TEMPORARY: delete along with ScaleChangeAdjustment (see CLAUDE.md).
public class ScaleChangeAdjustmentTests
{
    private static readonly DateTime Before = new(2026, 11, 26, 23, 59, 0, DateTimeKind.Utc);
    private static readonly DateTime AtCutoff = ScaleChangeAdjustment.Cutoff;
    private static readonly DateTime After = new(2026, 11, 28, 7, 0, 0, DateTimeKind.Utc);

    private static WeightEntity Entity(string userId, DateTime date, decimal weight) =>
        new(Guid.NewGuid(), userId, date, weight, false, "note", true);

    [Theory]
    [InlineData("gsej", 0.6)]
    [InlineData("shej", 0.4)]
    public void ReadingsBeforeCutoffAreReducedByUserOffset(string userId, double offset)
    {
        var original = Entity(userId, Before, 80m);

        var result = ScaleChangeAdjustment.Apply(userId, [original]).Single();

        result.Weight.Should().Be(80m - (decimal)offset);
        result.WeightId.Should().Be(original.WeightId);
        result.Date.Should().Be(original.Date);
        result.Comment.Should().Be("note");
        result.Important.Should().BeTrue();
    }

    [Theory]
    [InlineData("gsej")]
    [InlineData("shej")]
    public void ReadingsOnOrAfterCutoffAreUnchanged(string userId)
    {
        var result = ScaleChangeAdjustment.Apply(userId, [Entity(userId, AtCutoff, 80m), Entity(userId, After, 81m)]);

        result.Select(w => w.Weight).Should().Equal(80m, 81m);
    }

    [Fact]
    public void OtherUsersAreUnchanged()
    {
        var result = ScaleChangeAdjustment.Apply("alice", [Entity("alice", Before, 80m)]);

        result.Single().Weight.Should().Be(80m);
    }

    [Fact]
    public void InputEntitiesAreNotMutated()
    {
        var original = Entity("gsej", Before, 80m);

        ScaleChangeAdjustment.Apply("gsej", [original]);

        original.Weight.Should().Be(80m);
    }
}
