using FluentValidation;

namespace KnowledgeBase.Application.Features.Tags;

public sealed class CreateTagCommandValidator : AbstractValidator<CreateTagCommand>
{
    public CreateTagCommandValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("Color must be a hex code like #512BD4.");
    }
}

public sealed class UpdateTagCommandValidator : AbstractValidator<UpdateTagCommand>
{
    public UpdateTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(x => x.Color)
            .NotEmpty()
            .Matches("^#[0-9a-fA-F]{6}$")
            .WithMessage("Color must be a hex code like #512BD4.");
    }
}

public sealed class MergeTagCommandValidator : AbstractValidator<MergeTagCommand>
{
    public MergeTagCommandValidator()
    {
        RuleFor(x => x.SourceTagId).NotEmpty();
        RuleFor(x => x.TargetTagId).NotEmpty();
        RuleFor(x => x).Must(x => x.SourceTagId != x.TargetTagId)
            .WithMessage("Can't merge a tag into itself.");
    }
}

public sealed class DeleteTagCommandValidator : AbstractValidator<DeleteTagCommand>
{
    public DeleteTagCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
    }
}