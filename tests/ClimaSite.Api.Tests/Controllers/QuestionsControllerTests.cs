using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ClimaSite.Api.Tests.Infrastructure;
using ClimaSite.Core.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ClimaSite.Api.Tests.Controllers;

public class QuestionsControllerTests : IntegrationTestBase
{
    public QuestionsControllerTests(TestWebApplicationFactory factory) : base(factory)
    {
    }

    private async Task<Product> SeedProductAsync(string sku = "QA-001", string slug = "qa-product")
    {
        var product = new Product(sku, "Q&A Product", slug, 599.99m);
        product.SetActive(true);
        DbContext.Products.Add(product);
        await DbContext.SaveChangesAsync();
        return product;
    }

    private async Task<ProductQuestion> SeedApprovedQuestionAsync(Guid productId, string text)
    {
        var question = new ProductQuestion(productId, text);
        question.SetStatus(QuestionStatus.Approved);
        DbContext.ProductQuestions.Add(question);
        await DbContext.SaveChangesAsync();
        return question;
    }

    private async Task<ProductAnswer> SeedApprovedAnswerAsync(Guid questionId, string text)
    {
        var answer = new ProductAnswer(questionId, text);
        answer.SetStatus(AnswerStatus.Approved);
        DbContext.ProductAnswers.Add(answer);
        await DbContext.SaveChangesAsync();
        return answer;
    }

    /// <summary>
    /// Registers a fresh user, grants the Admin role via Identity, then logs in so the
    /// issued JWT carries the Admin role claim. The token is applied to <see cref="Client"/>.
    /// The login handler reads roles from UserManager at token-mint time, so the role must
    /// be assigned BEFORE the final login.
    /// </summary>
    private async Task AuthenticateAsAdminAsync()
    {
        var email = $"admin-{Guid.NewGuid()}@test.com";
        const string password = "Password123!";

        // Register the user through the real endpoint.
        var register = await Client.PostAsJsonAsync("/api/auth/register", new
        {
            email,
            password,
            firstName = "Admin",
            lastName = "User"
        });
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        // Grant the Admin role directly via Identity (self-contained, no test secret needed).
        using (var scope = Factory.Services.CreateScope())
        {
            var roleManager = scope.ServiceProvider
                .GetRequiredService<RoleManager<IdentityRole<Guid>>>();
            if (!await roleManager.RoleExistsAsync("Admin"))
            {
                await roleManager.CreateAsync(new IdentityRole<Guid>
                {
                    Name = "Admin",
                    NormalizedName = "ADMIN"
                });
            }

            var userManager = scope.ServiceProvider
                .GetRequiredService<UserManager<ApplicationUser>>();
            var user = await userManager.FindByEmailAsync(email);
            user.Should().NotBeNull();
            if (!await userManager.IsInRoleAsync(user!, "Admin"))
            {
                (await userManager.AddToRoleAsync(user!, "Admin")).Succeeded.Should().BeTrue();
            }
        }

        // Login AFTER the role grant so the JWT carries the Admin role claim.
        var login = await Client.PostAsJsonAsync("/api/auth/login", new { email, password });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = body.GetProperty("accessToken").GetString();
        SetAuthToken(token!);
    }

    /// <summary>Registers + logs in a fresh user (token applied to <see cref="Client"/>) and returns their id.</summary>
    private async Task<Guid> AuthenticateAndGetUserIdAsync(string? email = null)
    {
        email ??= $"voter-{Guid.NewGuid()}@test.com";
        await AuthenticateAsync(email);

        using var scope = Factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByEmailAsync(email);
        user.Should().NotBeNull();
        return user!.Id;
    }

    private async Task<int> QuestionHelpfulCountAsync(Guid questionId) =>
        await DbContext.ProductQuestions
            .AsNoTracking()
            .Where(q => q.Id == questionId)
            .Select(q => q.HelpfulCount)
            .FirstAsync();

    #region GetProductQuestions

    [Fact]
    public async Task GetProductQuestions_ReturnsApprovedQuestions()
    {
        // Arrange
        var product = await SeedProductAsync();
        await SeedApprovedQuestionAsync(product.Id, "Does this unit support WiFi control?");

        // Act
        var response = await Client.GetAsync($"/api/questions/product/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Does this unit support WiFi control?");
    }

    [Fact]
    public async Task GetProductQuestions_ExcludesPendingQuestions()
    {
        // Arrange
        var product = await SeedProductAsync();
        await SeedApprovedQuestionAsync(product.Id, "Approved visible question?");
        // Pending question (default status) - should not appear
        DbContext.ProductQuestions.Add(new ProductQuestion(product.Id, "Pending hidden question?"));
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/questions/product/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Approved visible question?");
        content.Should().NotContain("Pending hidden question?");
    }

    [Fact]
    public async Task GetProductQuestions_ReturnsEmptyForUnknownProduct()
    {
        // Act
        var response = await Client.GetAsync($"/api/questions/product/{Guid.NewGuid()}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"totalQuestions\":0");
    }

    #endregion

    #region AskQuestion (anonymous)

    [Fact]
    public async Task AskQuestion_ReturnsCreated_ForValidAnonymousRequest()
    {
        // Arrange
        var product = await SeedProductAsync();

        // Act
        var response = await Client.PostAsJsonAsync("/api/questions", new
        {
            productId = product.Id,
            questionText = "What is the noise level of this air conditioner?",
            askerName = "Curious Buyer",
            askerEmail = "buyer@example.com"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Created);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("submitted for review");

        // Persisted as Pending
        DbContext.ProductQuestions.Should().ContainSingle(q => q.ProductId == product.Id);
    }

    [Fact]
    public async Task AskQuestion_ReturnsBadRequest_WhenQuestionTextTooShort()
    {
        // Arrange
        var product = await SeedProductAsync();

        // Act - text below the 10-char minimum trips the FluentValidation rule
        var response = await Client.PostAsJsonAsync("/api/questions", new
        {
            productId = product.Id,
            questionText = "short"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("at least 10 characters");
    }

    [Fact]
    public async Task AskQuestion_ReturnsBadRequest_WhenProductIdMissing()
    {
        // Act - empty product id trips the NotEmpty validation rule
        var response = await Client.PostAsJsonAsync("/api/questions", new
        {
            productId = Guid.Empty,
            questionText = "This is a perfectly valid question length."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region AnswerQuestion / votes

    [Fact]
    public async Task AnswerQuestion_ReturnsOk_ForValidAnswer()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Is professional installation required?");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/{question.Id}/answers", new
        {
            answerText = "Yes, professional installation is strongly recommended for warranty.",
            answererName = "Helpful User"
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("submitted for review");
    }

    [Fact]
    public async Task AnswerQuestion_Returns500_WhenQuestionDoesNotExist()
    {
        // Act - handler throws InvalidOperationException, mapped to 500 by middleware
        var response = await Client.PostAsJsonAsync($"/api/questions/{Guid.NewGuid()}/answers", new
        {
            answerText = "An answer to a question that does not exist."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    [Fact]
    public async Task VoteQuestion_Returns401_WhenUnauthenticated()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Is anonymous voting allowed anymore?");
        ClearAuthToken();

        // Act
        var response = await Client.PostAsync($"/api/questions/{question.Id}/vote", null);

        // Assert - voting now requires authentication (B-039)
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VoteQuestion_RecordsVote_ForAuthenticatedUser()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "How energy efficient is this unit?");
        await AuthenticateAndGetUserIdAsync();

        // Act
        var response = await Client.PostAsync($"/api/questions/{question.Id}/vote", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"helpfulCount\":1");
        content.Should().Contain("\"hasVotedHelpful\":true");
        (await QuestionHelpfulCountAsync(question.Id)).Should().Be(1);
    }

    [Fact]
    public async Task VoteQuestion_Repeat_TogglesOff_WithoutInflating()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Does a second click toggle the vote off?");
        await AuthenticateAndGetUserIdAsync();

        // Act - vote, then vote again
        await Client.PostAsync($"/api/questions/{question.Id}/vote", null);
        var second = await Client.PostAsync($"/api/questions/{question.Id}/vote", null);

        // Assert - the second vote toggles off rather than inflating to 2
        second.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await second.Content.ReadAsStringAsync();
        content.Should().Contain("\"helpfulCount\":0");
        content.Should().Contain("\"hasVotedHelpful\":false");
        (await QuestionHelpfulCountAsync(question.Id)).Should().Be(0);
        (await DbContext.ProductQuestionVotes.CountAsync(v => v.QuestionId == question.Id)).Should().Be(0);
    }

    [Fact]
    public async Task VoteQuestion_Returns404_WhenQuestionNotApproved()
    {
        // Arrange - a pending question is not publicly votable
        var product = await SeedProductAsync();
        var pending = new ProductQuestion(product.Id, "A pending, non-votable question here?");
        DbContext.ProductQuestions.Add(pending);
        await DbContext.SaveChangesAsync();
        await AuthenticateAndGetUserIdAsync();

        // Act
        var response = await Client.PostAsync($"/api/questions/{pending.Id}/vote", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task VoteQuestion_Returns400_WhenVotingOnOwnQuestion()
    {
        // Arrange - the authenticated user authors the (approved) question, then tries to vote it
        var product = await SeedProductAsync();
        var userId = await AuthenticateAndGetUserIdAsync();
        var ownQuestion = new ProductQuestion(product.Id, "Can I upvote my very own question?");
        ownQuestion.SetUser(userId);
        ownQuestion.SetStatus(QuestionStatus.Approved);
        DbContext.ProductQuestions.Add(ownQuestion);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.PostAsync($"/api/questions/{ownQuestion.Id}/vote", null);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task VoteQuestion_ConcurrentDoublePost_AppliesDeltaAtMostOnce()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Do parallel clicks double the count?");
        await AuthenticateAndGetUserIdAsync();

        // Act - fire two votes for the same (user, question) in parallel
        var responses = await Task.WhenAll(
            Client.PostAsync($"/api/questions/{question.Id}/vote", null),
            Client.PostAsync($"/api/questions/{question.Id}/vote", null));

        // Assert - no 500s, and the ledger + count stay consistent and are never inflated past 1.
        responses.Should().OnlyContain(r => r.StatusCode == HttpStatusCode.OK);
        var ledgerRows = await DbContext.ProductQuestionVotes.CountAsync(v => v.QuestionId == question.Id);
        var helpfulCount = await QuestionHelpfulCountAsync(question.Id);
        ledgerRows.Should().BeLessThanOrEqualTo(1, "the unique constraint permits at most one row per (user, question)");
        helpfulCount.Should().Be(ledgerRows, "the denormalised count must match the ledger — no lost/double update");
    }

    [Fact]
    public async Task VoteAnswer_Returns401_WhenUnauthenticated()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Can it be wall mounted?");
        var answer = await SeedApprovedAnswerAsync(question.Id, "Yes, it ships with a wall-mount bracket.");
        ClearAuthToken();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/answers/{answer.Id}/vote", new { isHelpful = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task VoteAnswer_RecordsVote_ForAuthenticatedUser()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Can it be wall mounted?");
        var answer = await SeedApprovedAnswerAsync(question.Id, "Yes, it ships with a wall-mount bracket.");
        await AuthenticateAndGetUserIdAsync();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/answers/{answer.Id}/vote", new { isHelpful = true });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"helpfulCount\":1");
        content.Should().Contain("\"userVoteHelpful\":true");
    }

    [Fact]
    public async Task VoteAnswer_FlipHelpfulToUnhelpful_MovesTallyOnRealDatabase()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Does flipping a vote move the tally?");
        var answer = await SeedApprovedAnswerAsync(question.Id, "Yes — a single ledger row flips direction.");
        await AuthenticateAndGetUserIdAsync();

        // Act - vote helpful, then flip to unhelpful
        await Client.PostAsJsonAsync($"/api/questions/answers/{answer.Id}/vote", new { isHelpful = true });
        var flip = await Client.PostAsJsonAsync($"/api/questions/answers/{answer.Id}/vote", new { isHelpful = false });

        // Assert - helpful moved to unhelpful, one ledger row (flipped, not duplicated)
        flip.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await flip.Content.ReadAsStringAsync();
        content.Should().Contain("\"helpfulCount\":0");
        content.Should().Contain("\"unhelpfulCount\":1");
        content.Should().Contain("\"userVoteHelpful\":false");
        (await DbContext.ProductAnswerVotes.CountAsync(v => v.AnswerId == answer.Id)).Should().Be(1);
    }

    [Fact]
    public async Task GetProductQuestions_PopulatesVoteState_ForAuthedCaller()
    {
        // Arrange - the caller votes on a question and its answer, then reads the list back.
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Does the caller's own vote surface in the list?");
        var answer = await SeedApprovedAnswerAsync(question.Id, "Yes — the API returns your own vote state.");
        await AuthenticateAndGetUserIdAsync();

        await Client.PostAsync($"/api/questions/{question.Id}/vote", null);
        await Client.PostAsJsonAsync($"/api/questions/answers/{answer.Id}/vote", new { isHelpful = false });

        // Act - the GET carries the same bearer token, so the query resolves the current user.
        var response = await Client.GetAsync($"/api/questions/product/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"hasVotedHelpful\":true");
        content.Should().Contain("\"userVoteHelpful\":false");
    }

    [Fact]
    public async Task GetProductQuestions_LeavesVoteStateUnset_ForAnonymousCaller()
    {
        // Arrange
        var product = await SeedProductAsync();
        await SeedApprovedQuestionAsync(product.Id, "Anonymous callers should see no vote state?");
        ClearAuthToken();

        // Act
        var response = await Client.GetAsync($"/api/questions/product/{product.Id}");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("\"hasVotedHelpful\":false");
    }

    #endregion

    #region SubmitOfficialAnswer (Admin only)

    [Fact]
    public async Task SubmitOfficialAnswer_Returns401_WhenUnauthenticated()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Does the warranty cover compressor failure?");
        ClearAuthToken();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/{question.Id}/official-answer", new
        {
            answerText = "Yes, the compressor is covered for 5 years."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task SubmitOfficialAnswer_Returns403_WhenAuthenticatedAsNonAdmin()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "Is there a mobile app?");
        await AuthenticateAsync($"user-{Guid.NewGuid()}@test.com");

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/{question.Id}/official-answer", new
        {
            answerText = "Yes, there is a companion mobile app available."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task SubmitOfficialAnswer_ReturnsOk_WhenAuthenticatedAsAdmin()
    {
        // Arrange
        var product = await SeedProductAsync();
        var question = await SeedApprovedQuestionAsync(product.Id, "What refrigerant does this use?");
        await AuthenticateAsAdminAsync();

        // Act
        var response = await Client.PostAsJsonAsync($"/api/questions/{question.Id}/official-answer", new
        {
            answerText = "This unit uses the eco-friendly R32 refrigerant."
        });

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var content = await response.Content.ReadAsStringAsync();
        content.Should().Contain("Official answer submitted");
    }

    #endregion
}
