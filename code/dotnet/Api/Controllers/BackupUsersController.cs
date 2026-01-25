using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers;

[ApiController]
[Route("api/backup/users")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class BackupUsersController : ControllerBase
{
    private readonly UserRepository _userRepository;

    public BackupUsersController(UserRepository userRepository)
    {
        _userRepository = userRepository;
    }

    [HttpGet]
    public async Task<UsersCollection> GetAllUsers()
    {
        var entities = await _userRepository.GetAllAsync();
        var userRecords = entities.Select(entity => new User(
            entity.UserId, 
            entity.HeightInCm));
        return new UsersCollection(userRecords);
    }
    
    [HttpPost("restore")]
    public async Task<IActionResult> RestoreUsersRecords([FromBody] UsersCollection? records)
    {
        if (records == null || records.Users == null)
            return BadRequest("No records provided");

        var existingRecords = await _userRepository.GetAllAsync();
        foreach (var entity in existingRecords)
        {
            await _userRepository.HardDeleteAsync(entity);
        }

        foreach (var userRecord in records.Users)
        {
            var entity = new UserEntity(
                userRecord.UserId,
                userRecord.heightInCm);
            
            await _userRepository.AddAsync(entity);
        }

        return Ok();
    }
}