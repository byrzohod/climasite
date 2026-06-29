using ClimaSite.Application.Common.Models;
using FluentAssertions;

namespace ClimaSite.Application.Tests.Common;

public class PaginatedListTests
{
    [Fact]
    public void Constructor_NormalCase_ComputesTotalPages()
    {
        var list = new PaginatedList<int>([1, 2, 3], count: 25, pageNumber: 1, pageSize: 10);

        list.TotalPages.Should().Be(3);
        list.PageNumber.Should().Be(1);
        list.TotalCount.Should().Be(25);
    }

    [Fact]
    public void Constructor_PageSizeZero_DoesNotDivideByZero()
    {
        // B-036 defensive floor: pageSize 0 would make `count / (double)pageSize` produce a NaN/garbage
        // TotalPages. The floor treats it as 1, so the page count stays finite and sane.
        var list = new PaginatedList<int>([1, 2], count: 2, pageNumber: 1, pageSize: 0);

        list.TotalPages.Should().Be(2);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Constructor_PageNumberBelowOne_FlooredToOne(int pageNumber)
    {
        var list = new PaginatedList<int>([], count: 0, pageNumber: pageNumber, pageSize: 10);

        list.PageNumber.Should().Be(1);
        list.HasPreviousPage.Should().BeFalse();
    }
}
