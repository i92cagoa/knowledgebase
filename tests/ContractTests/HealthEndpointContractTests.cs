using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace KnowledgeBase.ContractTests;

public sealed class HealthEndpointContractTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HealthEndpointContractTests(WebApplicationFactory<Program> factory)
    {
        _client = factory
            .WithWebHostBuilder(_ => { })
            .CreateClient();
    }

    [Fact]
    public async Task Get_Health_Returns_Ok_With_Status_Healthy()
    {
        var response = await _client.GetAsync("/health");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var body = await response.Content.ReadFromJsonAsync<HealthResponse>();
        body.Should().NotBeNull();
        body!.Status.Should().Be("healthy");
    }

    [Fact]
    public async Task OpenApi_Document_Is_Served()
    {
        var response = await _client.GetAsync("/openapi/v1.json");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("openapi");
    }

    public sealed record HealthResponse(string Status, DateTime Timestamp);
}