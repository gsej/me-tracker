namespace Api.Controllers.Report;

/// <summary>A comment on one of the day's weighings.</summary>
public record ReportComment(string Text, bool Important);

public record Stage1ReportEntry(DateOnly Date, decimal Weight, IReadOnlyList<ReportComment> Comments);

public record Stage2ReportEntry(DateOnly Date, 
    decimal? RecordedWeight, 
    decimal AverageWeight, 
    decimal Bmi, 
    decimal OneWeekChange,
    decimal TwoWeekChange,
    decimal FourWeekChange,
    decimal TwelveWeekChange,
    IReadOnlyList<ReportComment> Comments
    );