using Microsoft.AspNetCore.Mvc;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WeightController : ControllerBase
{
    [HttpPost]
    public IActionResult PostWeightEntry([FromBody] WeightRecord entry)
    {
        return Accepted();
    }

    public class WeightRecordExample : ISchemaFilter
    {
        public void Apply(OpenApiSchema schema, SchemaFilterContext context)
        {
            if (context.Type == typeof(WeightRecord))
            {
                schema.Example = new OpenApiObject()
                {
                    ["date"] = new OpenApiDate(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
                    ["weight"] = new OpenApiDouble(75.5)
                };
            }
        }
    }
}