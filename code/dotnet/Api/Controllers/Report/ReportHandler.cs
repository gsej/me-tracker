using Api.Controllers.Models;

namespace Api.Controllers.Report;

public class ReportHandler
{
    private readonly Settings _settings;

    public ReportHandler(Settings settings)
    {
        _settings = settings;
    }
    
    public WeightReport GetReport(IEnumerable<WeightEntity> records)
    {
        // stage 1, reduce the records to a single entry for each date, averaging the weights
        var stage1Entries = records
            .GroupBy(record => DateOnly.FromDateTime(record.Date))
            .Select(group => new Stage1ReportEntry(
                group.Key,
                group.Average(record => record.Weight)))
            .ToList();
        
        // stage two. 
        
        var firstDate = stage1Entries.Min(entry => entry.Date);
        var lastDate = stage1Entries.Max(entry => entry.Date);

        var stage2Entries = new List<Stage2ReportEntry>();
        
        for (var date = firstDate; date <= lastDate; date = date.AddDays(1))
        {
           // create an entry. it will have the current date. 
           // The weight will be the average of the weights of the previous 7 days (i.e entries from date.AddDays(-7) to the current date, taking in account missing days).
            var previousEntries = stage1Entries
                .Where(entry => entry.Date > date.AddDays(-_settings.AverageWeightWindowInDays) && entry.Date <= date)
                .Select(entry => entry.Weight)
                .ToList();

            decimal? recordedWeight;
            decimal movingAverageWeight;

            if (previousEntries.Count == 0)
            {
                recordedWeight = null;
                movingAverageWeight = stage2Entries.Single(e => e.Date == date.AddDays(-1)).AverageWeight;
            }
            else
            {
                recordedWeight = previousEntries.Last();
                movingAverageWeight = previousEntries.Average();
            }
            
            var bmi = Math.Round(movingAverageWeight / (_settings.HeightInMeters * _settings.HeightInMeters), 2);

            var entry = new Stage2ReportEntry(date, recordedWeight, movingAverageWeight, bmi);
            stage2Entries.Add(entry);
        }
        
        var report = new WeightReport
        {
            Entries = stage2Entries
        };

        return report;
    }
}