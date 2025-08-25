namespace Api;

public record Settings
{
    public int AverageWeightWindowInDays { get; init; } = 7;
    public decimal HeightInMeters { get; init; } = 1.78m;
}