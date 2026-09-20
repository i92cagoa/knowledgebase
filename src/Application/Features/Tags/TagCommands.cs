namespace KnowledgeBase.Application.Features.Tags;

public sealed record CreateTagCommand(string Name, string Color);
public sealed record DeleteTagCommand(Guid Id);