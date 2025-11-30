using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace QuanLiKhachSan.Controllers
{
    [ApiController]
    [Route("api/debug")]
    public class DebugController : ControllerBase
    {
        private readonly ILogger<DebugController> _logger;

        public DebugController(ILogger<DebugController> logger)
        {
            _logger = logger;
        }

        // POST api/debug/accept
        // Mục đích: kiểm tra giá trị AcceptTerms được gửi trong FormData và binding
        [HttpPost("accept")]
        public IActionResult Accept([FromForm] string? AcceptTerms)
        {
            string? raw = null;
            var hasKey = false;
            if (Request != null && Request.Form != null && Request.Form.ContainsKey("AcceptTerms"))
            {
                hasKey = true;
                raw = Request.Form["AcceptTerms"].ToString();
            }

            var parsed = false;
            if (!string.IsNullOrEmpty(AcceptTerms)) bool.TryParse(AcceptTerms, out parsed);

            _logger.LogInformation("[Debug] Request.Form[AcceptTerms]={Raw}, ParamAcceptTerms={Param}, HasKey={HasKey}, Parsed={Parsed}", raw, AcceptTerms, hasKey, parsed);

            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToArray();

            return Ok(new
            {
                Raw = raw,
                Param = AcceptTerms,
                HasKey = hasKey,
                Parsed = parsed,
                ModelStateErrors = errors
            });
        }
    }
}
