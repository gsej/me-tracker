using Azure.Data.Tables;
using Api.Controllers.Models;

namespace Api.DataAccess
{
    public class UserRepository
    {
        private readonly TableClient _tableClient;

        public UserRepository(string storageConnectionString)
        {
            _tableClient = new TableClient(storageConnectionString, UserEntity.Constants.TableName);
            _tableClient.CreateIfNotExists();
        }

        /// <summary>
        /// Gets all user entries.
        /// </summary>
        public async Task<IEnumerable<UserEntity>> GetAllAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<UserEntity>(e =>
                e.PartitionKey == UserEntity.Constants.PartitionKey);
                                                                                                            
            var results = new List<UserEntity>();
            await foreach (var entity in query)
            {
                results.Add(entity);
            }
            return results;
        }
        
        public async Task<UserEntity> GetByIdAsync(string userId)
        {
            await _tableClient.CreateIfNotExistsAsync();
            var query = _tableClient.QueryAsync<UserEntity>(e =>
                e.PartitionKey == UserEntity.Constants.PartitionKey &&
                e.UserId == userId);
            
            await foreach (var entity in query)
            {
                return entity;
            }
            return null;
        }

        public async Task AddAsync(UserEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.AddEntityAsync(entity);
        }

        public async Task HardDeleteAsync(UserEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.DeleteEntityAsync(entity);
        }
    }
}
