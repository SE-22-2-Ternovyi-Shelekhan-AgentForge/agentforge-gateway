using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AgentForge.Gateway.Tests
{
    public class GatewayEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;

        public GatewayEndpointTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
        }

        [Fact]
        public async Task Health_ReturnsHealthy()
        {
            var client = _factory.CreateClient();

            var response = await client.GetAsync("/health");

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            (await response.Content.ReadAsStringAsync()).Should().Be("Healthy");
        }

        [Fact]
        public async Task ProxyRoute_RateLimited_ReturnsTooManyRequests()
        {
            // Tighten the limit so the test triggers it deterministically.
            var factory = _factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureAppConfiguration((_, config) =>
                {
                    config.AddInMemoryCollection(new Dictionary<string, string?>
                    {
                        ["RateLimiting:PermitLimit"] = "2",
                        ["RateLimiting:WindowSeconds"] = "30",
                    });
                });
            });

            var client = factory.CreateClient();

            var statuses = new List<HttpStatusCode>();
            for (var i = 0; i < 6; i++)
            {
                var response = await client.GetAsync("/api/ping");
                statuses.Add(response.StatusCode);
            }

            // Requests beyond the permit limit must be rejected by the rate limiter,
            // regardless of whether the (unreachable) orchestrator answers the first few.
            statuses.Should().Contain(HttpStatusCode.TooManyRequests);
        }
    }
}
