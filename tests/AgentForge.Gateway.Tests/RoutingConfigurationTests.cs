using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgentForge.Gateway.Tests
{
    public class RoutingConfigurationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly IConfiguration _config;

        public RoutingConfigurationTests(WebApplicationFactory<Program> factory)
        {
            _config = factory.Services.GetRequiredService<IConfiguration>();
        }

        [Fact]
        public void ApiRoute_RoutesToOrchestratorCluster()
        {
            _config["ReverseProxy:Routes:orchestrator-route:ClusterId"].Should().Be("orchestrator-cluster");
            _config["ReverseProxy:Routes:orchestrator-route:Match:Path"].Should().Be("/api/{**catch-all}");
        }

        [Fact]
        public void HubsRoute_RoutesToOrchestratorCluster()
        {
            _config["ReverseProxy:Routes:hubs-route:ClusterId"].Should().Be("orchestrator-cluster");
            _config["ReverseProxy:Routes:hubs-route:Match:Path"].Should().Be("/hubs/{**catch-all}");
        }

        [Fact]
        public void OrchestratorCluster_HasDestinationAndActiveHealthCheck()
        {
            _config["ReverseProxy:Clusters:orchestrator-cluster:Destinations:node1:Address"]
                .Should().StartWith("http");

            _config.GetValue<bool>("ReverseProxy:Clusters:orchestrator-cluster:HealthCheck:Active:Enabled")
                .Should().BeTrue();
            _config["ReverseProxy:Clusters:orchestrator-cluster:HealthCheck:Active:Path"]
                .Should().Be("/health");
        }
    }
}
