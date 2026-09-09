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
            return NotFound();
        }
    }
}
