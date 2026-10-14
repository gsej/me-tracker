using Api.Controllers.Models;

namespace Api.Controllers.Report;

public class ReportHandler
{
    private readonly Settings _settings;

    public ReportHandler(Settings settings)
    {
        _settings = settings;
    }

    public WeightReport GetReport(IEnumerable<WeightEntity> records, int heightInCm)
    {
        // stage 1, reduce the records to a single entry for each date, averaging the weights
        var stage1Entries = records
            .GroupBy(record => DateOnly.FromDateTime(record.Date))
            .Select(group => new Stage1ReportEntry(
                group.Key,
                group.Average(record => record.Weight)))
            .OrderBy(record => record.Date)
            .ToList();

        // No records means no date range to walk — return an empty report rather than
        // throwing on Min()/Max() of an empty sequence (e.g. a new user opening the
        // report before logging any weight).
        if (stage1Entries.Count == 0)
        {
            return new WeightReport();
        }

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
                .OrderBy(entry => entry.Date)
                .Select(entry => entry.Weight)
                .ToList();

            decimal? recordedWeight;
            decimal movingAverageWeight;

            if (previousEntries.Count == 0)
            {
                recordedWeight = null;
                movingAverageWeight = Math.Round(stage2Entries.Single(e => e.Date == date.AddDays(-1)).AverageWeight, 2);
            }
            else
            {
                recordedWeight = previousEntries.Last();
                movingAverageWeight = ExponentialMovingAverage(previousEntries, _settings.AverageWeightWindowInDays);
            }

            var heightInMetres = (decimal)heightInCm / 100;

            var bmi = Math.Round(movingAverageWeight / (heightInMetres * heightInMetres), 2);

            // Every N-week change compares the same 7-day moving average at both ends:
            // the value now against the value N weeks ago. Using a longer N*7-day trailing
            // average for the "now" side would mix bases (the stored value N weeks ago is
            // always the 7-day average) and can flip the sign of the reported change.
            var oneWeekChange = CalculateLastNWeekChange(stage2Entries, date, movingAverageWeight, 1);
            var twoWeekChange = CalculateLastNWeekChange(stage2Entries, date, movingAverageWeight, 2);
            var fourWeekChange = CalculateLastNWeekChange(stage2Entries, date, movingAverageWeight, 4);
            var twelveWeekChange = CalculateLastNWeekChange(stage2Entries, date, movingAverageWeight, 12);

            var entry = new Stage2ReportEntry(date, 
                recordedWeight, 
                movingAverageWeight, 
                bmi, 
                oneWeekChange,
                twoWeekChange,
                fourWeekChange,
                twelveWeekChange
                );
            stage2Entries.Add(entry);
        }

        var report = new WeightReport
        {
            Entries = stage2Entries
        };

        return report;
    }

    // Exponentially weighted moving average over an oldest-to-newest ordered list, so recent
    // measurements count for more than older ones. alpha = 2/(N+1) ties the decay to the window
    // length, keeping each report period (1/2/4/12 week) distinctly responsive.
    private static decimal ExponentialMovingAverage(IList<decimal> ordered, int windowInDays)
    {
        var alpha = 2m / (windowInDays + 1);
        var average = ordered[0];
        for (var i = 1; i < ordered.Count; i++)
        {
            average = alpha * ordered[i] + (1 - alpha) * average;
        }
        return average;
    }

    private decimal CalculateLastNWeekChange(List<Stage2ReportEntry> stage2Entries, DateOnly date, decimal movingAverageWeight, int weeks)
    {
        var lastWeeksEntry = stage2Entries.SingleOrDefault(e => e.Date == date.AddDays(-7 * weeks));

        var weeksChange = lastWeeksEntry != null
            ? Math.Round(movingAverageWeight - lastWeeksEntry.AverageWeight, 2)
            : 0;
        return weeksChange;
    }
}