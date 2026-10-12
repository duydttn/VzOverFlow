using Microsoft.AspNetCore.Mvc;

namespace VzOverFlow.Controllers
{
    [Route("api/feedback")]
    [ApiController]
    public class FeedbackController : ControllerBase
    {
        [HttpPost]
        public IActionResult Post([FromBody] FeedbackRequest request)
        {
            return Ok(new { success = true, message = "Feedback received" });
        }

        public class FeedbackRequest
        {
            public bool Helpful { get; set; }
        }
    }
}
