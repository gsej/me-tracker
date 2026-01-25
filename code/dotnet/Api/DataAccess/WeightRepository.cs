using Azure.Data.Tables;
using Api.Controllers.Models;

namespace Api.DataAccess
{
    public class WeightRepository
    {
        private readonly TableClient _tableClient;

        public WeightRepository(string storageConnectionString)
        {
            _tableClient = new TableClient(storageConnectionString, Constants.TableName);
            _tableClient.CreateIfNotExists();
        }

        /// <summary>
        /// Gets all undeleted weight entries for a specific user.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync(string userId)
        {
            
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<WeightEntity>(e =>
                e.PartitionKey == Constants.PartitionKey
                && e.UserId == userId
                && !e.Deleted);
                                                                                                            
            var results = new List<WeightEntity>();
            await foreach (var entity in query)
            {
                results.Add(entity);
            }
            return results;
        }

        /// <summary>
        /// Gets all weight entries.
        /// </summary>
        public async Task<IEnumerable<WeightEntity>> GetAllAsync()
        {
            await _tableClient.CreateIfNotExistsAsync();

            var query = _tableClient.QueryAsync<WeightEntity>(e =>
                e.PartitionKey == Constants.PartitionKey);
                                                                                                            
            var results = new List<WeightEntity>();
            await foreach (var entity in query)
            {
                results.Add(entity);
            }
            return results;
        }
        
        public async Task<WeightEntity?> GetByIdAsync(Guid weightId, string userId)
        {
            await _tableClient.CreateIfNotExistsAsync();
            var query = _tableClient.QueryAsync<WeightEntity>(e => 
                e.PartitionKey == Constants.PartitionKey && 
                e.UserId == userId && e.WeightId == weightId && !e.Deleted);
            await foreach (var entity in query)
            {
                return entity;
            }
            return null;
        }

        public async Task AddAsync(WeightEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.AddEntityAsync(entity);
        }

        public async Task UpdateAsync(WeightEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.UpdateEntityAsync(entity, entity.ETag, TableUpdateMode.Replace);
        }

        public async Task HardDeleteAsync(WeightEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            await _tableClient.DeleteEntityAsync(entity);
        }
        
        public async Task DeleteAsync(WeightEntity entity)
        {
            await _tableClient.CreateIfNotExistsAsync();
            entity.Deleted = true;
            await UpdateAsync(entity);
        }
    }
}
