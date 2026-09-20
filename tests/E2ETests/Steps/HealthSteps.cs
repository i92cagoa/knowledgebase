using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using TechTalk.SpecFlow;

namespace KnowledgeBase.E2ETests.Steps;

[Binding]
public sealed class HealthSteps
{
    private WebApplicationFactory<Program> _factory = null!;
    private HttpResponseMessage _response = null!;

    [Given(@"the API is running")]
    public void GivenTheApiIsRunning()
    {
        _factory = new WebApplicationFactory<Program>();
    }

    [When(@"I request the health endpoint")]
    public async Task WhenIRequestTheHealthEndpoint()
    {
        using var client = _factory.CreateClient();
        _response = await client.GetAsync("/health");
    }

    [Then(@"the response has status ""(.*)""")]
    public async Task ThenTheResponseHasStatus(string expectedStatus)
    {
        _response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await _response.Content.ReadFromJsonAsync<HealthBody>();
        body!.Status.Should().BeEquivalentTo(expectedStatus);
    }

    public sealed record HealthBody(string Status, DateTime Timestamp);
}