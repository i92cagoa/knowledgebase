namespace KnowledgeBase.Application.Features.Tags;

public sealed record CreateTagCommand(string Name, string Color);
public sealed record UpdateTagCommand(Guid Id, string Name, string Color);
public sealed record MergeTagCommand(Guid SourceTagId, Guid TargetTagId);
public sealed record DeleteTagCommand(Guid Id);