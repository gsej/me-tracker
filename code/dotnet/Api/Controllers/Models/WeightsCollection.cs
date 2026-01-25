namespace Api.Controllers.Models;

public record WeightsCollection(IEnumerable<WeightRecord>? WeightRecords);
public record UsersCollection(IEnumerable<User>? Users);