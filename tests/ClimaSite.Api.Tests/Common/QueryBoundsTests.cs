using ClimaSite.Api.Common;
using FluentAssertions;

namespace ClimaSite.Api.Tests.Common;

/// <summary>
/// B-036: pins the exact clamp bounds applied to untrusted pagination/count query params at the API edge.
/// </summary>
public class QueryBoundsTests
{
    [Theory]
    [InlineData(0, 1)]        // below floor → 1
    [InlineData(-5, 1)]
    [InlineData(1, 1)]
    [InlineData(50, 50)]      // in range → unchanged
    [InlineData(100, 100)]
    [InlineData(101, 100)]    // above ceiling → 100
    [InlineData(100_000, 100)]
    public void PageSize_ClampsTo_1_to_100(int input, int expected)
        => QueryBounds.PageSize(input).Should().Be(expected);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]
    [InlineData(5, 5)]
    [InlineData(24, 24)]
    [InlineData(25, 24)]      // above ceiling → 24
    [InlineData(100_000, 24)]
    public void Count_ClampsTo_1_to_24(int input, int expected)
        => QueryBounds.Count(input).Should().Be(expected);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-9, 1)]
    [InlineData(1, 1)]
    [InlineData(500, 500)]
    [InlineData(100_000, 100_000)]
    [InlineData(int.MaxValue, 100_000)] // capped so (PageNumber-1)*PageSize can't overflow Int32
    public void PageNumber_ClampsTo_1_to_100000(int input, int expected)
        => QueryBounds.PageNumber(input).Should().Be(expected);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-30, 1)]
    [InlineData(90, 90)]
    [InlineData(730, 730)]
    [InlineData(731, 730)]      // above ceiling → 730
    [InlineData(int.MaxValue, 730)]
    public void Days_ClampsTo_1_to_730(int input, int expected)
        => QueryBounds.Days(input).Should().Be(expected);

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-1, 1)]      // negative would crash Take(-1)
    [InlineData(10, 10)]
    [InlineData(100, 100)]
    [InlineData(101, 100)]   // above ceiling → 100
    [InlineData(100_000, 100)]
    public void DashboardCount_ClampsTo_1_to_100(int input, int expected)
        => QueryBounds.DashboardCount(input).Should().Be(expected);

    [Fact]
    public void PageNumber_TimesMaxPageSize_StaysWithinInt32()
    {
        // The whole point of the upper cap: the downstream Skip((page-1)*size) must not overflow.
        ((long)QueryBounds.PageNumber(int.MaxValue) * QueryBounds.MaxPageSize)
            .Should().BeLessThan(int.MaxValue);
    }
}
