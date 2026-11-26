using System.Globalization;

namespace Api.Controllers.Models;

public class WeightEntity
{
    public WeightEntity()
    {
    }

    public WeightEntity(Guid weightId, string userId, DateTime date, decimal weight, bool deleted, string? comment = null, bool important = false)
    {
        WeightId = weightId;
        UserId = userId;
        Date = date;
        Weight = weight;
        Deleted = deleted;
        Comment = comment;
        Important = important;
    }

    public Guid WeightId { get; init; }

    public string UserId { get; init; } = null!;

    public DateTime Date { get; init; }

    public bool Deleted { get; set; } = false;

    public decimal Weight { get; set; }

    public string? Comment { get; set; }

    /// <summary>Marks the comment as important (shown in red on the chart). Always false without a comment.</summary>
    public bool Important { get; set; }
}
