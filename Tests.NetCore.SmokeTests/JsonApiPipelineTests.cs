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
    }
}
