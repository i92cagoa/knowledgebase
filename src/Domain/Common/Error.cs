namespace KnowledgeBase.Domain.Common;

public sealed record Error(string Code, string Description)
{
    public static readonly Error None = new(string.Empty, string.Empty);

    public static Error NotFound(string description) => new("NotFound", description);
    public static Error Conflict(string description) => new("Conflict", description);
    public static Error Invalid(string description) => new("Invalid", description);
}