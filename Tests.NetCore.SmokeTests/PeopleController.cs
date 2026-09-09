using Microsoft.AspNetCore.Mvc;
using Saule.Http;

namespace Tests.NetCore.SmokeTests
{
    [ApiController]
    [Route("people")]
    public class PeopleController : ControllerBase
    {
        [HttpGet("{id}")]
        [ReturnsResourceAttribute(typeof(PersonResource))]
        public Person Get(int id)
        {
            return new Person { Id = id, Name = "Alice" };
        }

        [HttpGet("notfound")]
        public IActionResult NotFoundAction()
        {
            // PR review finding: NotFound() (no body) returns NotFoundResult, not ObjectResult - it
            // never reaches JsonApiResultFilter's `is ObjectResult` guard at all, so the corresponding
            // test passed even with the 4xx short-circuit branch deleted entirely (false confidence).
            // NotFound(body) returns NotFoundObjectResult, which IS an ObjectResult, and genuinely
            // exercises that guard.
            return NotFound(new { message = "not found" });
        }

        // No [ReturnsResourceAttribute] - a plain, non-JSON:API endpoint living in the same app as
        // ConfigureJsonApi()'s globally-registered JsonApiResultFilter. Confirms the requiresMediaType
        // Accept-header gate (PR review fix) actually protects endpoints that were never meant to be
        // JSON:API - without it, this would 500 with "You must add a [ReturnsResourceAttribute]".
        [HttpGet("plain")]
        [Produces("application/json")]
        public Person GetPlain()
        {
            return new Person { Id = 99, Name = "Plain" };
        }

        // Confirms unhandled exceptions produce a JSON:API errors document (JsonApiExceptionFilter,
        // PR review fix) rather than the developer exception page/a bare 500.
        [HttpGet("throws")]
        [ReturnsResourceAttribute(typeof(PersonResource))]
        public Person Throws()
        {
            throw new System.InvalidOperationException("boom");
        }
    }
}
