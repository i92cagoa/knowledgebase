namespace KnowledgeBase.Application.Features.Workspaces;

public sealed record CreateWorkspaceCommand(string Name, string? Description);