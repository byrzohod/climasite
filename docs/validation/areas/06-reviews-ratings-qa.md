# Reviews, Ratings & Q&A - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **Product Reviews** - Create, list, and sort reviews with star ratings (1-5)
- **Review Summary** - Rating distribution and average rating per product
- **Verified Purchases** - Badge for reviews from users who purchased the product
- **Review Moderation** - Admin approval/rejection/flagging of reviews
- **Review Voting** - Mark reviews as helpful/unhelpful (not fully implemented)
- **Admin Response** - Store responses to customer reviews
- **Product Q&A** - Ask questions and provide answers about products
- **Q&A Moderation** - Admin moderation of questions and answers
- **Q&A Voting** - Vote questions and answers as helpful
- **Official Answers** - Mark answers as official store responses

### API Endpoints

#### Reviews API
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/reviews/product/{productId}` | Get paginated reviews for a product | No |
| GET | `/api/reviews/product/{productId}/summary` | Get review summary (ratings distribution) | No |
| POST | `/api/reviews` | Create a new review | Yes |
| POST | `/api/reviews/{reviewId}/helpful` | Mark review as helpful | Yes (Not Implemented) |
| POST | `/api/reviews/{reviewId}/unhelpful` | Mark review as unhelpful | Yes (Not Implemented) |

#### Admin Reviews API
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/admin/reviews/pending` | Get pending reviews for moderation | Admin |
| GET | `/api/admin/reviews` | Get reviews by status | Admin |
| POST | `/api/admin/reviews/{id}/approve` | Approve a review | Admin |
| POST | `/api/admin/reviews/{id}/reject` | Reject a review | Admin |
| POST | `/api/admin/reviews/{id}/flag` | Flag a review for further review | Admin |
| POST | `/api/admin/reviews/bulk-approve` | Bulk approve multiple reviews | Admin |

#### Questions API
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/questions/product/{productId}` | Get questions for a product | No |
| POST | `/api/questions` | Ask a new question | No (Anonymous allowed) |
| POST | `/api/questions/{questionId}/answers` | Submit an answer | No |
| POST | `/api/questions/{questionId}/official-answer` | Submit official answer | Admin |
| POST | `/api/questions/{questionId}/vote` | Vote question as helpful | No |
| POST | `/api/questions/answers/{answerId}/vote` | Vote answer as helpful/unhelpful | No |

#### Admin Questions API
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/admin/questions` | Get questions by status | Admin |
| POST | `/api/admin/questions/{id}/approve` | Approve a question | Admin |
| POST | `/api/admin/questions/{id}/reject` | Reject a question | Admin |
| POST | `/api/admin/questions/{id}/flag` | Flag a question | Admin |
| POST | `/api/admin/answers/{id}/approve` | Approve an answer | Admin |
| POST | `/api/admin/answers/{id}/reject` | Reject an answer | Admin |
| POST | `/api/admin/answers/{id}/flag` | Flag an answer | Admin |
| POST | `/api/admin/questions/bulk-approve` | Bulk approve questions | Admin |
| POST | `/api/admin/answers/bulk-approve` | Bulk approve answers | Admin |

### Frontend Routes
| Route | Component | Guard |
|-------|-----------|-------|
| `/products/:slug` (Reviews Tab) | ProductDetailComponent + ProductReviewsComponent | None |
| `/products/:slug` (Q&A Tab) | ProductDetailComponent + ProductQaComponent | None |
| `/admin/moderation` | AdminModerationComponent | adminGuard |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Controllers** | `src/ClimaSite.Api/Controllers/ReviewsController.cs` |
| | `src/ClimaSite.Api/Controllers/QuestionsController.cs` |
| | `src/ClimaSite.Api/Controllers/Admin/AdminReviewsController.cs` |
| | `src/ClimaSite.Api/Controllers/Admin/AdminQuestionsController.cs` |
| **Commands** | `src/ClimaSite.Application/Features/Reviews/Commands/CreateReviewCommand.cs` |
| | `src/ClimaSite.Application/Features/Reviews/Commands/ModerateReviewCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/AskQuestionCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/AnswerQuestionCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/VoteQuestionCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/VoteAnswerCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/ModerateQuestionCommand.cs` |
| | `src/ClimaSite.Application/Features/Questions/Commands/ModerateAnswerCommand.cs` |
| **Queries** | `src/ClimaSite.Application/Features/Reviews/Queries/GetProductReviewsQuery.cs` |
| | `src/ClimaSite.Application/Features/Reviews/Queries/GetPendingReviewsQuery.cs` |
| | `src/ClimaSite.Application/Features/Questions/Queries/GetProductQuestionsQuery.cs` |
| **DTOs** | `src/ClimaSite.Application/Features/Reviews/DTOs/ReviewDto.cs` |
| | `src/ClimaSite.Application/Features/Reviews/DTOs/AdminReviewDto.cs` |
| | `src/ClimaSite.Application/Features/Questions/DTOs/QuestionDto.cs` |
| | `src/ClimaSite.Application/Features/Questions/DTOs/AdminQuestionDto.cs` |
| **Entities** | `src/ClimaSite.Core/Entities/Review.cs` |
| | `src/ClimaSite.Core/Entities/ProductQuestion.cs` |
| | `src/ClimaSite.Core/Entities/ProductAnswer.cs` |
| **Interfaces** | `src/ClimaSite.Core/Interfaces/IReviewRepository.cs` |
| **Configurations** | `src/ClimaSite.Infrastructure/Data/Configurations/ReviewConfiguration.cs` |
| | `src/ClimaSite.Infrastructure/Data/Configurations/ProductQuestionConfiguration.cs` |
| | `src/ClimaSite.Infrastructure/Data/Configurations/ProductAnswerConfiguration.cs` |
| **Migrations** | `src/ClimaSite.Infrastructure/Data/Migrations/20260113185306_AddProductQuestionsAndAnswers.cs` |

### Frontend

| Layer | Files |
|-------|-------|
| **Components** | `src/ClimaSite.Web/src/app/shared/components/product-reviews/product-reviews.component.ts` |
| | `src/ClimaSite.Web/src/app/features/products/components/product-qa/product-qa.component.ts` |
| | `src/ClimaSite.Web/src/app/features/admin/moderation/admin-moderation.component.ts` |
| | `src/ClimaSite.Web/src/app/shared/components/energy-rating/energy-rating.component.ts` |
| **Services** | `src/ClimaSite.Web/src/app/core/services/review.service.ts` |
| | `src/ClimaSite.Web/src/app/features/products/services/questions.service.ts` |
| | `src/ClimaSite.Web/src/app/core/services/moderation.service.ts` |
| **Models** | `src/ClimaSite.Web/src/app/core/models/review.model.ts` |

---

## 3. Test Coverage Audit

### Unit Tests (Backend)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `tests/ClimaSite.Core.Tests/Entities/ReviewTests.cs` | `Constructor_WithValidData_CreatesReview` | Review entity creation |
| | `Constructor_WithInvalidRating_ThrowsArgumentException` (0, -1, 6, 10) | Rating validation |
| | `SetRating_WithValidValue_UpdatesRating` (1-5) | Rating update |
| | `SetRating_WithInvalidValue_ThrowsArgumentException` | Rating bounds validation |
| | `SetTitle_WithValidValue_UpdatesTitle` | Title update |
| | `SetTitle_TrimsWhitespace` | Title whitespace handling |
| | `SetTitle_WithTooLongValue_ThrowsArgumentException` | Title length validation |
| | `SetTitle_WithNull_SetsToNull` | Nullable title |
| | `SetContent_WithValidValue_UpdatesContent` | Content update |
| | `SetContent_WithTooLongValue_ThrowsArgumentException` | Content length validation |
| | `SetStatus_UpdatesStatus` | Status transitions |
| | `SetVerifiedPurchase_WithOrderId_SetsVerifiedAndOrderId` | Verified purchase |
| | `SetVerifiedPurchase_WithFalse_SetsNotVerified` | Remove verified status |
| | `AddHelpfulVote_IncrementsHelpfulCount` | Helpful voting |
| | `AddUnhelpfulVote_IncrementsUnhelpfulCount` | Unhelpful voting |
| | `TotalVotes_CalculatesCorrectly` | Total votes calculation |
| | `HelpfulPercentage_CalculatesCorrectly` | Percentage calculation |
| | `HelpfulPercentage_WithNoVotes_ReturnsZero` | Zero division handling |
| | `SetAdminResponse_SetsResponseAndTimestamp` | Admin response |
| | `SetAdminResponse_WithEmptyValue_ThrowsArgumentException` | Empty response validation |
| | `SetAdminResponse_WithWhitespace_ThrowsArgumentException` | Whitespace validation |

**Note:** No unit tests found for:
- `ProductQuestion` entity
- `ProductAnswer` entity
- Any CQRS command handlers
- Review/Question validators

### Unit Tests (Frontend)

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `src/ClimaSite.Web/src/app/features/products/components/product-qa/product-qa.component.spec.ts` | `should create` | Component creation |
| | `should load questions on init` | Initial data load |
| | `should display questions in template` | Question rendering |
| | `should display answers for questions` | Answer rendering |
| | `should toggle ask question form` | Form toggle |
| | `should validate question form` | Form validation |
| | `should submit question` | Question submission |
| | `should toggle answer form for a question` | Answer form toggle |
| | `should vote on a question` | Question voting |
| | `should not allow double voting on question` | Duplicate vote prevention |
| | `should vote on an answer` | Answer voting |
| | `should display empty state when no questions` | Empty state UI |
| | `should calculate total pages correctly` | Pagination math |
| | `should navigate pages` | Page navigation |
| | `should show loading state` | Loading indicator |
| | `should track questions by id` | TrackBy function |
| `src/ClimaSite.Web/src/app/features/products/services/questions.service.spec.ts` | `should be created` | Service creation |
| | `should fetch product questions with default params` | GET questions |
| | `should fetch product questions with custom params` | GET with pagination |
| | `should submit a new question` | POST question |
| | `should submit an answer to a question` | POST answer |
| | `should vote a question as helpful` | POST vote question |
| | `should vote an answer as helpful` | POST vote answer helpful |
| | `should vote an answer as unhelpful` | POST vote answer unhelpful |
| `src/ClimaSite.Web/src/app/shared/components/energy-rating/energy-rating.component.spec.ts` | (Energy rating tests) | Energy rating component |

**Note:** No unit tests found for:
- `ProductReviewsComponent`
- `ReviewService`
- `AdminModerationComponent`
- `ModerationService`

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No API integration tests for Reviews or Q&A |

**Note:** `tests/ClimaSite.Api.Tests/` contains only `ProductsControllerTests.cs`. No `ReviewsControllerTests.cs` or `QuestionsControllerTests.cs`.

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No E2E tests for Reviews or Q&A flows |

**Note:** E2E test folder contains tests for authentication, cart, checkout, orders, navigation, products, settings, and journeys, but no dedicated review/Q&A tests.

---

## 4. Manual Verification Steps

### Review Creation Flow
1. Login as a registered user
2. Navigate to any product detail page (`/products/:slug`)
3. Click "Reviews" tab
4. Click "Write Review" button
5. Select star rating (1-5)
6. Enter review title and content
7. Click "Submit Review"
8. Verify success message appears
9. Verify review appears in list (after admin approval)

### Review Summary Display
1. Navigate to product with existing reviews
2. Verify average rating displayed
3. Verify rating distribution bars (5-star to 1-star)
4. Verify total review count

### Review Sorting
1. Navigate to product with multiple reviews
2. Test sort options: Newest, Oldest, Helpful, Rating High, Rating Low
3. Verify reviews reorder correctly

### Admin Review Moderation
1. Login as admin user
2. Navigate to `/admin/moderation`
3. Click "Reviews" tab
4. Verify pending reviews display with star rating, title, content
5. Test "Approve" button - review should move to approved
6. Test "Reject" button - review should be removed from queue
7. Verify bulk approve functionality

### Q&A Question Flow
1. Navigate to any product detail page
2. Click "Q&A" tab
3. Click "Ask a Question" button
4. Enter question text (min 10 characters)
5. Optionally enter name and email
6. Click "Submit Question"
7. Verify success message "Question submitted for review"

### Q&A Answer Flow
1. Navigate to product with existing questions
2. Click "Answer" button on a question
3. Enter answer text
4. Click "Submit"
5. Verify answer appears (after admin approval)

### Q&A Voting
1. Navigate to product with Q&A
2. Click "Helpful" on a question
3. Verify count increments
4. Try clicking again - should be blocked (double-vote prevention)
5. Test answer voting (thumbs up/down)

### Admin Q&A Moderation
1. Login as admin user
2. Navigate to `/admin/moderation`
3. Click "Questions" tab
4. Verify pending questions display
5. Test approve/reject buttons
6. Click "Answers" tab
7. Test approve/reject/flag for answers
8. Test "Approve as Official" option

### Verified Purchase Badge
1. Complete a purchase for a product
2. Leave a review for that product
3. Verify "Verified Purchase" badge appears on review

---

## 5. Gaps & Risks

### Critical Gaps

- [ ] **Review voting NOT implemented** - `POST /api/reviews/{id}/helpful` and `/unhelpful` endpoints return 501 Not Implemented (see `ReviewsController.cs:91-107`)
- [ ] **No E2E tests for Reviews** - Review creation, display, sorting are untested end-to-end
- [ ] **No E2E tests for Q&A** - Question asking, answering, voting flows are untested
- [ ] **No API integration tests** - ReviewsController and QuestionsController have zero integration test coverage
- [ ] **No unit tests for command handlers** - CreateReviewCommandHandler, ModerateReviewCommand handlers untested
- [ ] **No unit tests for ProductQuestion/ProductAnswer entities** - Domain logic untested

### Medium Gaps

- [ ] **No ReviewService unit tests** - Frontend service lacks test coverage
- [ ] **No ProductReviewsComponent unit tests** - Review display/form component untested
- [ ] **No AdminModerationComponent unit tests** - Admin UI untested
- [ ] **No rate limiting on Q&A endpoints** - Anonymous question submission vulnerable to spam (noted as TODO in QuestionsController.cs:47-48)
- [ ] **Bulk operations have N+1 query problem** - Noted as TODO in AdminReviewsController.cs:132-135

### Security Risks

- [ ] **Anonymous Q&A submission** - No authentication required to ask questions or submit answers, spam risk
- [ ] **No CAPTCHA** - No bot protection on public submission forms
- [ ] **No content filtering** - No profanity/spam detection before moderation queue

### Technical Debt

- [ ] **Incomplete voting implementation** - Review voting endpoints exist but return 501
- [ ] **Missing user tracking for votes** - Current implementation doesn't prevent duplicate votes server-side
- [ ] **Frontend vote tracking via localStorage** - Can be bypassed by clearing storage

---

## 6. Recommended Fixes & Tests

| Priority | Issue | Recommendation |
|----------|-------|----------------|
| **Critical** | No Review E2E tests | Create `tests/ClimaSite.E2E/Tests/Reviews/ReviewTests.cs` with tests for: create review, display reviews, sort reviews, verified purchase badge |
| **Critical** | No Q&A E2E tests | Create `tests/ClimaSite.E2E/Tests/QA/QuestionsAnswersTests.cs` with tests for: ask question, answer question, vote question/answer |
| **Critical** | Review voting not implemented | Implement `VoteReviewCommand` with user tracking (store votes in new `ReviewVote` table), update `ReviewsController.cs` endpoints |
| **Critical** | No API integration tests | Create `tests/ClimaSite.Api.Tests/Controllers/ReviewsControllerTests.cs` and `QuestionsControllerTests.cs` |
| **High** | No ProductQuestion/ProductAnswer entity tests | Create `tests/ClimaSite.Core.Tests/Entities/ProductQuestionTests.cs` and `ProductAnswerTests.cs` |
| **High** | No command handler unit tests | Create tests for `CreateReviewCommandHandler`, `ModerateReviewCommandHandler`, `AskQuestionCommandHandler`, etc. |
| **High** | No ReviewService unit tests | Create `src/ClimaSite.Web/src/app/core/services/review.service.spec.ts` |
| **High** | No ProductReviewsComponent tests | Create `product-reviews.component.spec.ts` |
| **Medium** | Anonymous Q&A spam risk | Add rate limiting middleware to `/api/questions` POST endpoint |
| **Medium** | N+1 bulk operations | Implement `BulkModerateReviewsCommand` using EF Core `ExecuteUpdateAsync` |
| **Medium** | No Admin moderation tests | Create `tests/ClimaSite.E2E/Tests/Admin/ModerationTests.cs` for review/Q&A moderation flows |
| **Low** | LocalStorage vote tracking | Implement server-side vote tracking with `QuestionVote` and `AnswerVote` tables |
| **Low** | Content filtering | Integrate spam/profanity filter for review/Q&A content |

---

## 7. Evidence & Notes

### Review Entity Domain Logic

From `Review.cs`, the entity enforces business rules:
- Rating must be 1-5
- Title max 200 characters
- Content max 5000 characters
- Admin response cannot be empty
- Status workflow: Pending -> Approved/Rejected/Flagged

```csharp
public void SetRating(int rating)
{
    if (rating < 1 || rating > 5)
        throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));
    Rating = rating;
}
```

### Review Voting NOT Implemented

From `ReviewsController.cs:78-108`:
```csharp
/// <remarks>
/// TODO: Implement review voting functionality. This endpoint currently returns 501 Not Implemented.
/// Implementation should track user votes to prevent duplicate voting and update review helpfulness counts.
/// </remarks>
[Authorize]
[HttpPost("{reviewId:guid}/helpful")]
[ProducesResponseType(StatusCodes.Status501NotImplemented)]
public async Task<IActionResult> MarkReviewHelpful(Guid reviewId)
{
    // TODO: Implement vote helpful - requires VoteReviewCommand with user tracking
    return StatusCode(StatusCodes.Status501NotImplemented, new { message = "Review voting not yet implemented" });
}
```

### Q&A Rate Limiting TODO

From `QuestionsController.cs:42-48`:
```csharp
/// <remarks>
/// This endpoint allows anonymous users to submit questions.
/// TODO: Add rate limiting middleware to prevent spam (e.g., max 5 questions per IP per hour).
/// Consider implementing: app.UseRateLimiter() with a sliding window policy for this endpoint.
/// </remarks>
```

### Q&A Frontend Vote Prevention

From `product-qa.component.spec.ts`, double-vote prevention is tested:
```typescript
it('should not allow double voting on question', fakeAsync(() => {
    // ... first vote ...
    // Try to vote again - should be blocked
    httpMock.expectNone(`${environment.apiUrl}/questions/q1/vote`);
}));
```

This is implemented via localStorage tracking (`climasite_voted_questions`, `climasite_voted_answers`) but NOT enforced server-side.

### Test Data Available

Q&A tests use mock data factory pattern:
```typescript
const mockQuestions: ProductQuestions = {
    productId: '123e4567-e89b-12d3-a456-426614174000',
    totalQuestions: 2,
    answeredQuestions: 1,
    questions: [
        {
            id: 'q1',
            questionText: 'Is this product energy efficient?',
            answers: [{ answerText: 'Yes, it has an A++ energy rating.', isOfficial: true }]
        }
    ]
};
```

### Admin Moderation UI

The `AdminModerationComponent` provides a unified interface for moderating:
- Questions (pending, flagged)
- Answers (pending, flagged, mark as official)
- Reviews (pending, flagged)

Supports bulk approval but has N+1 query performance issue (documented as TODO).

### Comprehensive Q&A Frontend Tests

The `product-qa.component.spec.ts` has 16 tests covering:
- Initial load and display
- Form validation
- Question/answer submission
- Voting with double-vote prevention
- Pagination
- Empty states
- Loading states
