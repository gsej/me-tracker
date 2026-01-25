using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/backup/weights")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class BackupWeightsController : ControllerBase
{
    private readonly WeightRepository _weightRepository;

    public BackupWeightsController(WeightRepository weightRepository)
    {
        _weightRepository = weightRepository;
    }

    [HttpGet]
    public async Task<WeightsCollection> GetAllWeightRecords()
    {
        var entities = await _weightRepository.GetAllAsync();
        var weightRecords = entities.Select(entity => new WeightRecord(
            entity.WeightId, 
            entity.UserId, 
            entity.Date, 
            entity.Weight,
            entity.Deleted));
        var orderedWeightRecords = weightRecords.OrderBy(record => record.Date);
        return new WeightsCollection(orderedWeightRecords);
    }
    
    [HttpPost("restore")]
    public async Task<IActionResult> RestoreWeightRecords([FromBody] WeightsCollection? records)
    {
        if (records == null || records.WeightRecords == null)
            return BadRequest("No records provided");

        var existingRecords = await _weightRepository.GetAllAsync();
        foreach (var entity in existingRecords)
        {
            await _weightRepository.HardDeleteAsync(entity);
        }

        foreach (var weightRecord in records.WeightRecords)
        {
            var entity = new WeightEntity(
                weightRecord.WeightId == Guid.Empty ? Guid.NewGuid() : weightRecord.WeightId,
                weightRecord.UserId,
                weightRecord.Date, 
                weightRecord.Weight,
                weightRecord.Deleted);
            
            await _weightRepository.AddAsync(entity);
        }

        return Ok();
    }
}