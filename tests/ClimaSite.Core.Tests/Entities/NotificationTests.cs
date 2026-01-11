using ClimaSite.Core.Entities;
using FluentAssertions;
using Xunit;

namespace ClimaSite.Core.Tests.Entities;

public class NotificationTests
{
    [Fact]
    public void Constructor_WithValidData_ShouldCreateNotification()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var type = NotificationTypes.OrderPlaced;
        var title = "Order Confirmed";
        var message = "Your order has been placed successfully.";

        // Act
        var notification = new Notification(userId, type, title, message);

        // Assert
        notification.UserId.Should().Be(userId);
        notification.Type.Should().Be(type);
        notification.Title.Should().Be(title);
        notification.Message.Should().Be(message);
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void SetType_WithEmptyValue_ShouldThrowArgumentException(string type)
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");

        // Act & Assert
        var action = () => notification.SetType(type);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*type cannot be empty*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void SetTitle_WithEmptyValue_ShouldThrowArgumentException(string title)
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");

        // Act & Assert
        var action = () => notification.SetTitle(title);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*title cannot be empty*");
    }

    [Fact]
    public void SetTitle_WithTooLongValue_ShouldThrowArgumentException()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        var longTitle = new string('a', 201);

        // Act & Assert
        var action = () => notification.SetTitle(longTitle);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 200 characters*");
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void SetMessage_WithEmptyValue_ShouldThrowArgumentException(string message)
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");

        // Act & Assert
        var action = () => notification.SetMessage(message);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*message cannot be empty*");
    }

    [Fact]
    public void SetMessage_WithTooLongValue_ShouldThrowArgumentException()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        var longMessage = new string('a', 1001);

        // Act & Assert
        var action = () => notification.SetMessage(longMessage);
        action.Should().Throw<ArgumentException>()
            .WithMessage("*cannot exceed 1000 characters*");
    }

    [Fact]
    public void MarkAsRead_WhenUnread_ShouldSetIsReadAndReadAt()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");

        // Act
        notification.MarkAsRead();

        // Assert
        notification.IsRead.Should().BeTrue();
        notification.ReadAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MarkAsRead_WhenAlreadyRead_ShouldNotUpdateReadAt()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        notification.MarkAsRead();
        var firstReadAt = notification.ReadAt;

        // Wait a bit
        Thread.Sleep(10);

        // Act
        notification.MarkAsRead();

        // Assert
        notification.ReadAt.Should().Be(firstReadAt);
    }

    [Fact]
    public void MarkAsUnread_WhenRead_ShouldClearIsReadAndReadAt()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        notification.MarkAsRead();

        // Act
        notification.MarkAsUnread();

        // Assert
        notification.IsRead.Should().BeFalse();
        notification.ReadAt.Should().BeNull();
    }

    [Fact]
    public void SetLink_WithValidLink_ShouldSetLink()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        var link = "/orders/12345";

        // Act
        notification.SetLink(link);

        // Assert
        notification.Link.Should().Be(link);
    }

    [Fact]
    public void SetData_WithValidData_ShouldSetData()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");
        var data = new Dictionary<string, object>
        {
            ["orderId"] = Guid.NewGuid().ToString(),
            ["amount"] = 99.99
        };

        // Act
        notification.SetData(data);

        // Assert
        notification.Data.Should().ContainKey("orderId");
        notification.Data.Should().ContainKey("amount");
    }

    [Fact]
    public void SetData_WithNull_ShouldSetEmptyDictionary()
    {
        // Arrange
        var notification = new Notification(Guid.NewGuid(), "test", "title", "message");

        // Act
        notification.SetData(null);

        // Assert
        notification.Data.Should().NotBeNull();
        notification.Data.Should().BeEmpty();
    }
}
