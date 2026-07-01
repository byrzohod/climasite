using ClimaSite.Application.Common.Interfaces;
using ClimaSite.Application.Features.Gdpr.Commands;
using ClimaSite.Application.Tests.TestHelpers;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace ClimaSite.Application.Tests.Features.Gdpr.Commands;

public class DeleteUserDataCommandHandlerTests
{
    private const string ValidPassword = "Password123!";

    private readonly Mock<ICurrentUserService> _currentUserServiceMock = new();
    private readonly Mock<UserManager<ApplicationUser>> _userManagerMock;
    private readonly Mock<IEmailService> _emailServiceMock = new();
    private readonly MockDbContext _context = new();

    public DeleteUserDataCommandHandlerTests()
    {
        _userManagerMock = new Mock<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
    }

    private DeleteUserDataCommandHandler CreateHandler() =>
        new(
            _context,
            _currentUserServiceMock.Object,
            _userManagerMock.Object,
            _emailServiceMock.Object,
            Mock.Of<ILogger<DeleteUserDataCommandHandler>>());

    private static DeleteUserDataCommand ValidCommand() => new()
    {
        Password = ValidPassword,
        Reason = "No longer needed",
        ConfirmDeletion = true
    };

    private ApplicationUser SeedAuthenticatedUser(
        Guid? id = null,
        bool passwordValid = true)
    {
        var userId = id ?? Guid.NewGuid();
        var user = new ApplicationUser
        {
            Id = userId,
            Email = "delete-me@test.com",
            UserName = "delete-me@test.com",
            FirstName = "Delete",
            LastName = "Me",
            PhoneNumber = "+359888123456",
            PasswordHash = "hash",
            IsActive = true
        };

        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync(user);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, ValidPassword)).ReturnsAsync(passwordValid);
        _userManagerMock.Setup(x => x.CheckPasswordAsync(user, It.Is<string>(p => p != ValidPassword)))
            .ReturnsAsync(false);
        _userManagerMock.Setup(x => x.UpdateAsync(user)).ReturnsAsync(IdentityResult.Success);

        return user;
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ReturnsFailure()
    {
        _currentUserServiceMock.Setup(x => x.UserId).Returns((Guid?)null);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("not authenticated");
        _userManagerMock.Verify(x => x.FindByIdAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDeletionNotConfirmed_ReturnsFailure()
    {
        SeedAuthenticatedUser();

        var result = await CreateHandler().Handle(
            ValidCommand() with { ConfirmDeletion = false }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("confirm");
    }

    [Fact]
    public async Task Handle_WhenPasswordMissing_ReturnsFailure()
    {
        SeedAuthenticatedUser();

        var result = await CreateHandler().Handle(
            ValidCommand() with { Password = "  " }, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Password is required");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        var userId = Guid.NewGuid();
        _currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
        _userManagerMock.Setup(x => x.FindByIdAsync(userId.ToString())).ReturnsAsync((ApplicationUser?)null);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("User not found");
    }

    [Fact]
    public async Task Handle_WhenPasswordInvalid_ReturnsFailure_AndDoesNotDeactivate()
    {
        var user = SeedAuthenticatedUser(passwordValid: false);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Error.Should().Contain("Invalid password");
        user.IsActive.Should().BeTrue();
        _userManagerMock.Verify(x => x.UpdateAsync(It.IsAny<ApplicationUser>()), Times.Never);
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_WithValidRequest_AnonymizesAndDeactivatesUser()
    {
        var userId = Guid.NewGuid();
        var user = SeedAuthenticatedUser(userId);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeTrue();

        // Account is soft-deleted: deactivated, credentials wiped, PII anonymised.
        user.IsActive.Should().BeFalse();
        user.PasswordHash.Should().BeNull();
        user.RefreshToken.Should().BeNull();
        user.PhoneNumber.Should().BeNull();
        user.FirstName.Should().Be("Deleted");
        user.LastName.Should().Be("User");
        user.Email.Should().Be($"deleted_{userId}@deleted.local");
        user.UserName.Should().Be($"deleted_{userId}");

        _userManagerMock.Verify(x => x.UpdateAsync(user), Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_SendsConfirmationEmailToOriginalAddress()
    {
        var user = SeedAuthenticatedUser();

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        // Email goes to the ORIGINAL address before anonymisation overwrote it.
        _emailServiceMock.Verify(
            x => x.SendEmailAsync(
                "delete-me@test.com",
                It.Is<string>(s => s.Contains("Account Deletion")),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WithValidRequest_HardDeletesNotificationsCartAddressesAndReviewVotes()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SeedAuthenticatedUser(userId);

        _context.Notifications.Add(
            new Notification(userId, NotificationTypes.OrderPlaced, "Title", "Message"));
        _context.Notifications.Add(
            new Notification(otherUserId, NotificationTypes.OrderPlaced, "Other", "Keep me"));

        _context.Addresses.Add(
            new Address(userId, "Delete Me", "1 St", "Sofia", "1000", "Bulgaria", "BG"));

        var cart = new Cart(userId, null);
        _context.AddCart(cart);

        var review = new Review(Guid.NewGuid(), userId, 5);
        _context.Reviews.Add(review);
        _context.ReviewVotes.Add(new ReviewVote(review.Id, userId, true));
        _context.ReviewVotes.Add(new ReviewVote(review.Id, otherUserId, true));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // The user's own personal data is removed; other users' data is untouched.
        (await _context.Notifications.ToListAsync()).Should().ContainSingle()
            .Which.UserId.Should().Be(otherUserId);
        (await _context.Addresses.ToListAsync()).Should().BeEmpty();
        (await _context.Carts.ToListAsync()).Should().BeEmpty();
        (await _context.ReviewVotes.ToListAsync()).Should().ContainSingle()
            .Which.UserId.Should().Be(otherUserId);
    }

    [Fact]
    public async Task Handle_WithValidRequest_HardDeletesTheUsersQaVotes_LeavingOthers()
    {
        var userId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        SeedAuthenticatedUser(userId);

        var questionId = Guid.NewGuid();
        var answerId = Guid.NewGuid();
        _context.ProductQuestionVotes.Add(new ProductQuestionVote(questionId, userId));
        _context.ProductQuestionVotes.Add(new ProductQuestionVote(questionId, otherUserId));
        _context.ProductAnswerVotes.Add(new ProductAnswerVote(answerId, userId, isHelpful: true));
        _context.ProductAnswerVotes.Add(new ProductAnswerVote(answerId, otherUserId, isHelpful: false));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // The user's own Q&A vote ledger rows are hard-deleted; other users' rows are untouched.
        (await _context.ProductQuestionVotes.ToListAsync()).Should().ContainSingle()
            .Which.UserId.Should().Be(otherUserId);
        (await _context.ProductAnswerVotes.ToListAsync()).Should().ContainSingle()
            .Which.UserId.Should().Be(otherUserId);
    }

    [Fact]
    public async Task Handle_WithValidRequest_RemovesWishlistAndItsItems()
    {
        var userId = Guid.NewGuid();
        SeedAuthenticatedUser(userId);

        var wishlist = new Core.Entities.Wishlist(userId);
        wishlist.AddItem(Guid.NewGuid());
        wishlist.AddItem(Guid.NewGuid());
        _context.AddWishlist(wishlist);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        (await _context.Wishlists.ToListAsync()).Should().BeEmpty();
        (await _context.WishlistItems.ToListAsync()).Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_WithValidRequest_AnonymizesReviews_WithoutDeletingThem()
    {
        var userId = Guid.NewGuid();
        SeedAuthenticatedUser(userId);

        var review = new Review(Guid.NewGuid(), userId, 5);
        review.SetVerifiedPurchase(true, Guid.NewGuid());
        review.SetContent("Great unit, keeps the legal-retention copy.");
        _context.Reviews.Add(review);

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();

        // Reviews are retained for product integrity but stripped of the verified-purchase link.
        var stored = (await _context.Reviews.ToListAsync()).Should().ContainSingle().Subject;
        stored.IsVerifiedPurchase.Should().BeFalse();
        stored.OrderId.Should().BeNull();
        stored.Content.Should().Be("Great unit, keeps the legal-retention copy.");
    }

    [Fact]
    public async Task Handle_WhenEmailSendThrows_StillSucceeds()
    {
        SeedAuthenticatedUser();
        _emailServiceMock
            .Setup(x => x.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("SMTP down"));

        var result = await CreateHandler().Handle(ValidCommand(), CancellationToken.None);

        // Deletion is irreversible work already committed; a failed confirmation email must not fail it.
        result.IsSuccess.Should().BeTrue();
    }
}
