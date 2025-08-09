using Microsoft.AspNetCore.Mvc;

namespace FOC_Blazor.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CamerasController : ControllerBase
{
    private readonly IConfiguration _cfg;
    public CamerasController(IConfiguration cfg) => _cfg = cfg;

    [HttpGet]
    public IActionResult Get()
    {
        var section = _cfg.GetSection("CameraConfig");
        if (!section.Exists()) return NotFound("CameraConfig not found in appsettings.json");

        // Return the section as-is so your predefined camera URLs are preserved
        var obj = section.Get<object>()!;
        return new JsonResult(obj);
    }
}