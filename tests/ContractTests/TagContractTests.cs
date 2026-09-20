using System.Net;
using System.Net.Http.Json;
using AwesomeAssertions;
using Xunit;

namespace KnowledgeBase.ContractTests.Features;

public sealed class TagContractTests : IClassFixture<ContractTestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public TagContractTests(ContractTestWebApplicationFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task List_Create_Delete_Tags()
    {
        var list = await (await _client.GetAsync("/api/tags")).Content.ReadFromJsonAsync<List<object>>();
        list.Should().BeEmpty();

        var create = await _client.PostAsJsonAsync("/api/tags", new { name = "dotnet", color = "#512BD4" });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var id = await create.Content.ReadFromJsonAsync<Guid>();

        await _client.GetAsync("/api/tags");

        var delete = await _client.DeleteAsync($"/api/tags/{id}");
        delete.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task Create_Tag_With_Invalid_Color_Returns_BadRequest()
    {
        var response = await _client.PostAsJsonAsync("/api/tags", new { name = "x", color = "red" });
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}