using Api.Controllers.Models;
using Microsoft.Data.Sqlite;

namespace Api.DataAccess
{
    public class UserRepository
    {
        private readonly string _connectionString;

        public UserRepository(string connectionString)
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
                CREATE TABLE IF NOT EXISTS Users (
                    UserId TEXT NOT NULL PRIMARY KEY,
                    HeightInCm INTEGER NOT NULL
                )
                """;
            command.ExecuteNonQuery();
        }

        /// <summary>
        /// Gets all user entries.
        /// </summary>
        public async Task<IEnumerable<UserEntity>> GetAllAsync()
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserId, HeightInCm FROM Users";

            var results = new List<UserEntity>();
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                results.Add(new UserEntity(reader.GetString(0), reader.GetInt32(1)));
            }
            return results;
        }

        public async Task<UserEntity?> GetByIdAsync(string userId)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "SELECT UserId, HeightInCm FROM Users WHERE UserId = $userId";
            command.Parameters.AddWithValue("$userId", userId);

            using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserEntity(reader.GetString(0), reader.GetInt32(1));
            }
            return null;
        }

        public async Task AddAsync(UserEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO Users (UserId, HeightInCm) VALUES ($userId, $heightInCm)";
            command.Parameters.AddWithValue("$userId", entity.UserId);
            command.Parameters.AddWithValue("$heightInCm", entity.HeightInCm);
            await command.ExecuteNonQueryAsync();
        }

        public async Task HardDeleteAsync(UserEntity entity)
        {
            using var connection = new SqliteConnection(_connectionString);
            await connection.OpenAsync();
            var command = connection.CreateCommand();
            command.CommandText = "DELETE FROM Users WHERE UserId = $userId";
            command.Parameters.AddWithValue("$userId", entity.UserId);
            await command.ExecuteNonQueryAsync();
        }
    }
}
