using System.Globalization;
using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess
{
    public class WeightRepository
    {
        private readonly string _connectionString;

        public WeightRepository(string connectionString)
        {
            _connectionString = connectionString;
            EnsureTablesExist();
        }

        private void EnsureTablesExist()
        {
            using var connection = new SqliteConnection(_connectionString);
            connection.Open();
            var command = connection.CreateCommand();
            command.CommandText = """
                CREATE TABLE IF NOT EXISTS Weights (
                    WeightId TEXT NOT NULL PRIMARY KEY,
                    UserId TEXT NOT NULL,
                    Date TEXT NOT NULL,
                    Weight TEXT NOT NULL,
                    Deleted INTEGER NOT NULL DEFAULT 0
                )
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets all undeleted weight entries for a specific user.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted FROM Weights WHERE UserId = $userId AND Deleted = 0";
            command.Parameters.AddWithValue("$userId", userId);

            var results = new List<WeightEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadWeightEntity(reader));
            }
            return results;
        }

        /// <summary>
        /// Gets all weight entries.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted FROM Weights";

            var results = new List<WeightEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(ReadWeightEntity(reader));
            }
            return results;
        }

        public async Task<WeightEntity?> GetByIdAsync(Guid weightId, string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT WeightId, UserId, Date, Weight, Deleted FROM Weights WHERE WeightId = $weightId AND UserId = $userId AND Deleted = 0";
            command.Parameters.AddWithValue("$weightId", weightId.ToString());
            command.Parameters.AddWithValue("$userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return ReadWeightEntity(reader);
            }
            return null;
        }

        public async Task AddAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Weights (WeightId, UserId, Date, Weight, Deleted) VALUES ($weightId, $userId, $date, $weight, $deleted)";
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            command.Parameters.AddWithValue("$userId", entity.UserId);
            command.Parameters.AddWithValue("$date", entity.Date.ToString("O"));
            command.Parameters.AddWithValue("$weight", entity.Weight.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            await command.ExecuteNonQueryAsync();
        }

        public async Task UpdateAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "UPDATE Weights SET Date = $date, Weight = $weight, Deleted = $deleted WHERE WeightId = $weightId";
            command.Parameters.AddWithValue("$date", entity.Date.ToString("O"));
            command.Parameters.AddWithValue("$weight", entity.Weight.ToString(CultureInfo.InvariantCulture));
            command.Parameters.AddWithValue("$deleted", entity.Deleted ? 1 : 0);
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task HardDeleteAsync(WeightEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Weights WHERE WeightId = $weightId";
            command.Parameters.AddWithValue("$weightId", entity.WeightId.ToString());
            await command.ExecuteNonQueryAsync();
        }

        public async Task DeleteAsync(WeightEntity entity)
        {
            entity.Deleted = true;
            await UpdateAsync(entity);
        }

        private static WeightEntity ReadWeightEntity(SqliteDataReader reader)
        {
            return new WeightEntity(
                Guid.Parse(reader.GetString(0)),
                reader.GetString(1),
                DateTime.Parse(reader.GetString(2)),
                decimal.Parse(reader.GetString(3), CultureInfo.InvariantCulture),
                reader.GetInt32(4) != 0
            );
        }
    }
}
