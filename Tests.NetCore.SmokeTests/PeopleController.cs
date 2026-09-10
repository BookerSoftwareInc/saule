using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Saule.Http;
using Saule.Queries;

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

        // Confirms JsonApiQueryValueProvider's bracket-to-PascalCase key normalization actually
        // reaches [ApiController] model binding (#2/#12 PR review fix) - without
        // IBindingSourceValueProvider, CompositeValueProvider.Filter drops the provider entirely
        // under [ApiController], so filter[min-age] would never bind to PersonFilter.MinAge.
        [HttpGet("filtered")]
        public IActionResult GetFiltered([FromQuery] PersonFilter filter)
        {
            return Ok(new { filter.MinAge });
        }

        // Confirms a [HandlesQuery] action with a QueryContext parameter does not require a request
        // body/Content-Type under [ApiController] (#13 PR review fix) - without a non-Body binding
        // source for QueryContext, UnsupportedContentTypeFilter/ModelStateInvalidFilter would
        // short-circuit this GET request before HandlesQueryAttribute ever runs.
        [HttpGet("handled")]
        [HandlesQueryAttribute]
        public IActionResult GetHandled(QueryContext queryContext)
        {
            var hasNameFilter = queryContext.Filter?.Properties.Any(p => p.Name == "Name") ?? false;
            return Ok(new { hasNameFilter });
        }

        // Confirms a malformed JSON:API request body produces a 400, not a 500 (#7 PR review fix) -
        // JsonApiInputFormatter throws BadHttpRequestException for invalid JSON, which
        // JsonApiExceptionFilter must surface via BadHttpRequestException.StatusCode rather than
        // forcing every exception to Status500InternalServerError.
        [HttpPost]
        public IActionResult Post([FromBody] Person person)
        {
            return Ok(person);
        }
    }
}
