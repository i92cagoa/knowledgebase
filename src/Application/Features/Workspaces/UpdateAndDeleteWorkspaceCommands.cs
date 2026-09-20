namespace KnowledgeBase.Application.Features.Workspaces;

public sealed record UpdateWorkspaceCommand(Guid Id, string Name, string? Description);
public sealed record DeleteWorkspaceCommand(Guid Id);