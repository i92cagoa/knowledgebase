using AwesomeAssertions;
using KnowledgeBase.Domain.Common;
using KnowledgeBase.Domain.Entities;
using Xunit;

namespace KnowledgeBase.UnitTests.Entities;

public class ResultTests
{
    [Fact]
    public void Result_Success_And_Failure_Behave()
    {
        var ok = Result.Success();
        ok.IsSuccess.Should().BeTrue();
        ok.IsFailure.Should().BeFalse();
        ok.Error.Should().Be(Error.None);

        var error = Error.NotFound("missing");
        var fail = Result.Failure(error);
        fail.IsSuccess.Should().BeFalse();
        fail.IsFailure.Should().BeTrue();
        fail.Error.Should().Be(error);
    }

    [Fact]
    public void Result_OfT_Implicit_Conversions()
    {
        Result<string> fromValue = "value";
        fromValue.Value.Should().Be("value");
        fromValue.IsSuccess.Should().BeTrue();

        Result<string> fromError = Error.Invalid("bad");
        fromError.IsFailure.Should().BeTrue();
        fromError.Error.Code.Should().Be("Invalid");
    }
}