using ClimaSite.Core.Entities;
using FluentAssertions;

namespace ClimaSite.Core.Tests.Entities;

public class ContactMessageTests
{
    private static ContactMessage CreateValid() =>
        new("Jane", "jane@test.com", "Question", "Hello there");

    [Fact]
    public void Constructor_WithValidData_CreatesNewMessage()
    {
        var message = CreateValid();

        message.Name.Should().Be("Jane");
        message.Email.Should().Be("jane@test.com");
        message.Subject.Should().Be("Question");
        message.Message.Should().Be("Hello there");
        message.Status.Should().Be(ContactMessageStatus.New);
    }

    [Fact]
    public void Constructor_TrimsFields()
    {
        var message = new ContactMessage("  Jane  ", "  jane@test.com  ", "  Subject  ", "  Body  ");

        message.Name.Should().Be("Jane");
        message.Email.Should().Be("jane@test.com");
        message.Subject.Should().Be("Subject");
        message.Message.Should().Be("Body");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyName_ThrowsArgumentException(string name)
    {
        var act = () => new ContactMessage(name, "jane@test.com", "Subject", "Body");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*name is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyEmail_ThrowsArgumentException(string email)
    {
        var act = () => new ContactMessage("Jane", email, "Subject", "Body");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*email is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptySubject_ThrowsArgumentException(string subject)
    {
        var act = () => new ContactMessage("Jane", "jane@test.com", subject, "Body");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*subject is required*");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithEmptyMessage_ThrowsArgumentException(string body)
    {
        var act = () => new ContactMessage("Jane", "jane@test.com", "Subject", body);

        act.Should().Throw<ArgumentException>()
           .WithMessage("*message is required*");
    }

    [Fact]
    public void Constructor_TruncatesFieldsExceedingMaxLength()
    {
        var longName = new string('a', 300);
        var longMessage = new string('b', 6000);

        var message = new ContactMessage(longName, "jane@test.com", "Subject", longMessage);

        message.Name.Should().HaveLength(200);
        message.Message.Should().HaveLength(5000);
    }

    [Fact]
    public void MarkRead_SetsReadStatus()
    {
        var message = CreateValid();

        message.MarkRead();

        message.Status.Should().Be(ContactMessageStatus.Read);
    }
}
