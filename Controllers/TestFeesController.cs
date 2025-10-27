using Microsoft.AspNetCore.Mvc;

namespace DaycareAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestFeesController : ControllerBase
    {
        [HttpGet]
        public ActionResult<object> Get()
        {
            return Ok(new { message = "Fees API is working", timestamp = DateTime.UtcNow });
        }
    }
}