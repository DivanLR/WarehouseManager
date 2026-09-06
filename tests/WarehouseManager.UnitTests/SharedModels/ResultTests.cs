using Shouldly;
using WarehouseManager.Api.SharedModels;

namespace WarehouseManager.UnitTests.SharedModels;

public class ResultTests
{
    [Fact]
    public void Success_Should_HaveNoError()
    {
        var result = Result.Success();

        result.IsSuccess.ShouldBeTrue();
        result.Error.ShouldBe(Error.None);
    }

    [Fact]
    public void Constructor_Should_ThrowWhenFailureHasNoError()
    {
        Should.Throw<ArgumentException>(() => new Result(false, Error.None));
    }

    [Fact]
    public void Constructor_Should_ThrowWhenSuccessHasAnError()
    {
        Should.Throw<ArgumentException>(() => new Result(true, Error.NullValue));
    }

    [Fact]
    public void Value_Should_ThrowWhenResultIsFailure()
    {
        var result = Result.Failure<int>(Error.NullValue);

        Should.Throw<InvalidOperationException>(() => result.Value);
    }
}
