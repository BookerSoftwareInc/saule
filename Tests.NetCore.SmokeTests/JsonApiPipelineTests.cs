using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace Tests.NetCore.SmokeTests
{
    /// <summary>
    /// Real, in-memory end-to-end verification of the net10.0 ASP.NET Core JSON:API pipeline
    /// (Standard-2.0-Migration-Plan.md Section 7.1: JsonApiResultFilter, JsonApiOutputFormatter,
    /// ReturnsResourceAttribute, ConfigureJsonApi DI registration). Promoted from a throwaway
    /// console app used during the original migration session - same checks, now permanent so
    /// net10.0 regressions are caught going forward (Action item 13).
    /// </summary>
    public class JsonApiPipelineTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public JsonApiPipelineTests(WebApplicationFactory<Program> factory)
        {
            _client = factory.CreateClient();
        }

        [Fact]
        public async Task Get_ReturnsJsonApiResponse_WithCorrectStatusAndContentType()
        {
            var response = await _client.GetAsync("/people/42");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/vnd.api+json", response.Content.Headers.ContentType?.MediaType);
        }

        [Fact]
        public async Task Get_ReturnsJsonApiResponse_WithCorrectShape()
        {
            var response = await _client.GetAsync("/people/42");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Contains("\"type\":\"person\"", body);
            Assert.Contains("\"id\":\"42\"", body);
            Assert.Contains("\"name\":\"Alice\"", body);
        }

        [Fact]
        public async Task NotFoundRoute_Returns404Untouched()
        {
            // JsonApiResultFilter must not touch 4xx results, matching net47's JsonApiProcessor
            // short-circuit (Standard-2.0-Migration-Plan.md Section 7.1).
            var response = await _client.GetAsync("/people/notfound");

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }

        [Fact]
        public async Task PlainEndpoint_WithoutReturnsResource_IsNotForcedIntoJsonApi()
        {
            // PR review finding: without the requiresMediaType Accept-header gate, the globally
            // registered JsonApiResultFilter forced every ObjectResult in the app through JSON:API
            // processing, regardless of Accept header or [ReturnsResourceAttribute] presence - a
            // plain endpoint like this one would 500 with "You must add a [ReturnsResourceAttribute]".
            var request = new HttpRequestMessage(HttpMethod.Get, "/people/plain");
            request.Headers.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

            var response = await _client.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal("application/json", response.Content.Headers.ContentType?.MediaType);
            Assert.DoesNotContain("ReturnsResourceAttribute", body);
        }

        [Fact]
        public async Task UnhandledException_ProducesJsonApiErrorsDocument()
        {
            // PR review finding: on net47, WebApi's own HttpError-on-exception flows through the
            // same pipeline Saule's formatter already understands. ASP.NET Core has no equivalent
            // unless something (JsonApiExceptionFilter) converts the exception into a result first.
            var response = await _client.GetAsync("/people/throws");
            var body = await response.Content.ReadAsStringAsync();

            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("application/vnd.api+json", response.Content.Headers.ContentType?.MediaType);
            Assert.Contains("\"errors\"", body);
            Assert.Contains("boom", body);
        }
    }
}
