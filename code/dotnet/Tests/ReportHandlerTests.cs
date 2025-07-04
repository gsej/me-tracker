using Api.Controllers.Models;
using Api.Controllers.Report;
using FluentAssertions;

namespace Tests;

public class ReportHandlerTests
{
    [Fact]
    public void HandleShouldAverageWeightsForEachDay()
    {
        // i.e. if the same date has multiple weights, these will be merged into a single entry with the average weight for that date.
        
        var date1 = new DateTime(2023, 10, 1);
        var date2 = new DateTime(2023, 10, 2);
        
        var weights = new List<WeightEntity>
        {
            new (Guid.NewGuid(), date1, 70),
            new (Guid.NewGuid(), date1, 80),
            new (Guid.NewGuid(), date1, 90),
            new (Guid.NewGuid(), date2, 100),
            new (Guid.NewGuid(), date2, 102),
            new (Guid.NewGuid(), date2, 101)
        };

        var reportHandler = new ReportHandler();
        var report = reportHandler.GetReport(weights);
        
        var date1Entry = report.Entries.SingleOrDefault(entry => entry.Date == DateOnly.FromDateTime(date1));
        var date2Entry = report.Entries.SingleOrDefault(entry => entry.Date == DateOnly.FromDateTime(date2));

        date1Entry.Should().NotBeNull();
        date1Entry!.RecordedWeight.Should().Be(80);
        date1Entry!.AverageWeight.Should().Be(80);
        
        date2Entry.Should().NotBeNull();
        date2Entry!.RecordedWeight.Should().Be(101);
        date2Entry!.AverageWeight.Should().Be(90.5m);
    }
    
    [Fact]
    public void HandleShouldReturnOneEntryForEachDateInRange()
    {
        var firstDate = new DateTime(2023, 10, 1);
        var lastDate = new DateTime(2023, 10, 5);
        
        var weights = new List<WeightEntity>
        {
            new (Guid.NewGuid(), firstDate, 70),
            new (Guid.NewGuid(), lastDate, 100)
        };

        var reportHandler = new ReportHandler();
        var report = reportHandler.GetReport(weights);

        report.Entries.Count.Should().Be(5);
        report.Entries.First().Date.Should().Be(DateOnly.FromDateTime(firstDate));
        report.Entries.Last().Date.Should().Be(DateOnly.FromDateTime(lastDate));
    }
    
    [Fact]
    public void HandleShouldReturnEntriesAveragingThePrevious7Entries()
    {
        var firstDate = new DateTime(2023, 10, 1);
        
        var weights = new List<WeightEntity>
        {
            new (Guid.NewGuid(), firstDate, 100),
            new (Guid.NewGuid(), firstDate.AddDays(1), 99),
            new (Guid.NewGuid(), firstDate.AddDays(2), 98),
            new (Guid.NewGuid(), firstDate.AddDays(3), 97),
            new (Guid.NewGuid(), firstDate.AddDays(4), 96),
            new (Guid.NewGuid(), firstDate.AddDays(5), 95),
            new (Guid.NewGuid(), firstDate.AddDays(6), 94),
            new (Guid.NewGuid(), firstDate.AddDays(7), 93),
            new (Guid.NewGuid(), firstDate.AddDays(8), 92),
            new (Guid.NewGuid(), firstDate.AddDays(9), 91),
        };

        var reportHandler = new ReportHandler();
        var report = reportHandler.GetReport(weights);
        
        report.Entries.Count.Should().Be(10);
        
        report.Entries[0].RecordedWeight.Should().Be(100);
        report.Entries[0].AverageWeight.Should().Be(100);
        
        report.Entries[1].RecordedWeight.Should().Be(99);
        report.Entries[1].AverageWeight.Should().Be(99.5m);
        
        report.Entries[2].RecordedWeight.Should().Be(98);
        report.Entries[2].AverageWeight.Should().Be(99);
        
        report.Entries[3].RecordedWeight.Should().Be(97);
        report.Entries[3].AverageWeight.Should().Be(98.5m);
        
        report.Entries[4].RecordedWeight.Should().Be(96);
        report.Entries[4].AverageWeight.Should().Be(98);
        
        report.Entries[5].RecordedWeight.Should().Be(95);
        report.Entries[5].AverageWeight.Should().Be(97.5m);
        
        report.Entries[6].RecordedWeight.Should().Be(94);
        report.Entries[6].AverageWeight.Should().Be(97);
        
        report.Entries[7].RecordedWeight.Should().Be(93);
        report.Entries[7].AverageWeight.Should().Be(96);
        
        report.Entries[8].RecordedWeight.Should().Be(92);
        report.Entries[8].AverageWeight.Should().Be(95);
        
        report.Entries[9].RecordedWeight.Should().Be(91);
        report.Entries[9].AverageWeight.Should().Be(94);
    }
}