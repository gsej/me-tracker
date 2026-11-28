using Api.Controllers.Models;

namespace Api.Controllers.Report;

/// <summary>
/// TEMPORARY (see "Temporary: scale-change fudge" in CLAUDE.md — remove after 26 Feb 2027).
/// New Withings scales were first used on 27/11/2026 and read lighter than the old ones.
/// Treating the new scales as accurate, readings taken before the switch are reduced by a
/// fixed per-user offset for reporting only. Stored data is never changed.
/// </summary>
public static class ScaleChangeAdjustment
{
    // Readings strictly before this instant came from the old scales.
    public static readonly DateTime Cutoff = new(2026, 11, 27, 0, 0, 0, DateTimeKind.Utc);

    // How much heavier the old scales read, in kg.
    private static readonly Dictionary<string, decimal> OffsetsByUserId = new()
    {
        ["gsej"] = 0.6m,
        ["shej"] = 0.4m,
    };

    public static IReadOnlyList<WeightEntity> Apply(string userId, IEnumerable<WeightEntity> weights)
    {
        if (!OffsetsByUserId.TryGetValue(userId, out var offset))
        {
            return weights.ToList();
        }

        // Build copies rather than mutating the loaded entities, so nothing can be written back.
        return weights
            .Select(w => w.Date < Cutoff
                ? new WeightEntity(w.WeightId, w.UserId, w.Date, w.Weight - offset, w.Deleted, w.Comment, w.Important)
                : w)
            .ToList();
    }
}
