using AwesomeAssertions;
using CommunityToolkit.Mvvm.Input;
using KnowledgeBase.Desktop.Models;
using KnowledgeBase.Desktop.ViewModels;

namespace KnowledgeBase.DesktopTests;

public sealed class DialogCommandTests
{
    [Fact]
    public async Task TagManager_Commands_Execute_And_Call_Api()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new TagManagerViewModel(api);
        await vm.LoadAsync();
        vm.Tags.Should().NotBeEmpty();

        // Add
        vm.NewTagName = "csharp";
        await ((IAsyncRelayCommand)vm.AddTagCommand).ExecuteAsync(null);
        api.CreateTagCalls.Should().Be(1);

        // Update (requires selection)
        var tag = vm.Tags.First();
        vm.SelectedTag = tag;
        vm.EditName = "renamed";
        await ((IAsyncRelayCommand)vm.UpdateTagCommand).ExecuteAsync(null);
        api.UpdateTagCalls.Should().Be(1);

        // Add a second tag to be a merge target
        vm.NewTagName = "fsharp";
        await ((IAsyncRelayCommand)vm.AddTagCommand).ExecuteAsync(null);

        // Merge (requires selection + target)
        vm.MergeTargetName = vm.Tags.First(t => t.Id != tag.Id).Name;
        await ((IAsyncRelayCommand)vm.MergeTagCommand).ExecuteAsync(null);
        api.MergeTagCalls.Should().Be(1);

        // Delete
        vm.SelectedTag = vm.Tags.First();
        await ((IAsyncRelayCommand)vm.DeleteTagCommand).ExecuteAsync(null);
        api.DeleteTagCalls.Should().Be(1);
    }

    [Fact]
    public async Task TagManager_Merge_Into_Itself_Is_Rejected()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new TagManagerViewModel(api);
        await vm.LoadAsync();

        var tag = vm.Tags.First();
        vm.SelectedTag = tag;
        vm.MergeTargetName = tag.Name;

        await ((IAsyncRelayCommand)vm.MergeTagCommand).ExecuteAsync(null);

        api.MergeTagCalls.Should().Be(0);
        vm.StatusMessage.Should().Contain("itself");
    }

    [Fact]
    public async Task NewWorkspace_Create_Closes_And_Refreshes_On_Success()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var created = false;
        var closed = false;
        var vm = new NewWorkspaceViewModel(api, () => created = true);
        vm.CloseRequested += () => closed = true;

        vm.Name = "Inbox";
        await ((IAsyncRelayCommand)vm.CreateCommand).ExecuteAsync(null);

        api.CreateWorkspaceCalls.Should().Be(1);
        created.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task NewWorkspace_Empty_Name_Shows_Error_Without_Api_Call()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new NewWorkspaceViewModel(api, () => { });

        await ((IAsyncRelayCommand)vm.CreateCommand).ExecuteAsync(null);

        api.CreateWorkspaceCalls.Should().Be(0);
        vm.StatusMessage.Should().Contain("name");
    }

    [Fact]
    public async Task ImportLink_Imports_And_Closes()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var imported = false;
        var closed = false;
        var vm = new ImportLinkViewModel(api, () => Guid.NewGuid(), () => imported = true);
        vm.CloseRequested += () => closed = true;

        vm.Url = "https://example.com/a";
        await ((IAsyncRelayCommand)vm.ImportCommand).ExecuteAsync(null);

        api.ImportLinkCalls.Should().Be(1);
        imported.Should().BeTrue();
        closed.Should().BeTrue();
    }

    [Fact]
    public async Task ImportLink_Invalid_Url_Shows_Error_Without_Api_Call()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var vm = new ImportLinkViewModel(api, () => Guid.NewGuid(), () => { });

        vm.Url = "";
        await ((IAsyncRelayCommand)vm.ImportCommand).ExecuteAsync(null);

        api.ImportLinkCalls.Should().Be(0);
        vm.StatusMessage.Should().Contain("URL");
    }

    [Fact]
    public async Task Search_Commute_With_Debounce()
    {
        var api = new FakeKnowledgeBaseApiClient();
        var opened = null as NoteSearchItem;
        var vm = new SearchViewModel(api, item => opened = item);
        vm.SearchText = "ef core";
        await Task.Delay(400);

        vm.Results.Should().NotBeEmpty();
        vm.HasResults.Should().BeTrue();

        var result = vm.Results.First();
        vm.SelectedItem = result;
        vm.OpenSelectedCommand.Execute(null);

        opened.Should().NotBeNull();
        opened!.Id.Should().Be(result.Id);
    }
}