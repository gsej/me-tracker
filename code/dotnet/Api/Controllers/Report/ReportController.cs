using Api.Controllers.Models;
using Api.DataAccess;
using Api.Filters;
using Microsoft.AspNetCore.Mvc;

namespace Api.Controllers.Report;

[ApiController]
[Route("api/report")]
[ServiceFilter(typeof(ApiKeyAuthFilter))]
public class ReportController : ControllerBase
{
    private readonly WeightRepository _weightRepository;
    private readonly UserRepository _userRepository;
    private readonly ReportHandler _reportHandler;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ReportController( 
        ReportHandler reportHandler, 
        IHttpContextAccessor httpContextAccessor, 
        WeightRepository weightRepository,
        UserRepository userRepository)
    {
        _reportHandler = reportHandler;
        _httpContextAccessor = httpContextAccessor;
        _weightRepository = weightRepository;
        _userRepository = userRepository;
    }
     
    [HttpGet]
    public async Task<WeightReport> Get()
    {
        var userId = (string)_httpContextAccessor.HttpContext!.Items[ApiKeyAuthFilter.UserIdKeyname];
        var weights = await _weightRepository.GetAllAsync(userId);
        var user = await _userRepository.GetByIdAsync(userId);
        var report = _reportHandler.GetReport(weights, user.HeightInCm);
        return report;
    }
}