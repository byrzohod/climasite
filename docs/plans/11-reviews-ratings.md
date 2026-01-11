# Reviews & Ratings Plan

## 1. Overview

The Reviews & Ratings system enables customers to share their experiences with HVAC products on ClimaSite. This feature builds trust, aids purchasing decisions, and provides valuable feedback for product improvement.

### Key Features

- **Star Ratings**: 1-5 star rating system for products
- **Verified Purchase Badges**: Visual indicator for reviews from confirmed buyers
- **Review Moderation**: Admin approval workflow for quality control
- **Helpfulness Voting**: Community-driven review quality signals
- **Rating Analytics**: Aggregate statistics and distribution charts
- **Review Responses**: Seller/admin responses to customer reviews

### Business Value

- Increases conversion rates through social proof
- Reduces return rates via informed purchasing
- Provides product improvement insights
- Builds customer trust and engagement

---

## 2. Database Schema

### 2.1 Reviews Table

```sql
CREATE TABLE reviews (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    product_id UUID NOT NULL REFERENCES products(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    order_id UUID REFERENCES orders(id) ON DELETE SET NULL,
    order_item_id UUID REFERENCES order_items(id) ON DELETE SET NULL,
    rating INT NOT NULL CHECK (rating >= 1 AND rating <= 5),
    title VARCHAR(200),
    content TEXT NOT NULL CHECK (char_length(content) >= 20),
    pros TEXT,
    cons TEXT,
    is_verified_purchase BOOLEAN NOT NULL DEFAULT FALSE,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    moderation_notes TEXT,
    moderated_by UUID REFERENCES users(id),
    moderated_at TIMESTAMP WITH TIME ZONE,
    helpful_count INT NOT NULL DEFAULT 0,
    not_helpful_count INT NOT NULL DEFAULT 0,
    report_count INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_reviews_product_user UNIQUE (product_id, user_id),
    CONSTRAINT chk_reviews_status CHECK (status IN ('pending', 'approved', 'rejected', 'flagged'))
);

-- Indexes for common queries
CREATE INDEX idx_reviews_product_id ON reviews(product_id);
CREATE INDEX idx_reviews_user_id ON reviews(user_id);
CREATE INDEX idx_reviews_status ON reviews(status);
CREATE INDEX idx_reviews_rating ON reviews(rating);
CREATE INDEX idx_reviews_created_at ON reviews(created_at DESC);
CREATE INDEX idx_reviews_product_status_rating ON reviews(product_id, status, rating);
CREATE INDEX idx_reviews_verified ON reviews(product_id, is_verified_purchase) WHERE status = 'approved';

-- Full-text search on review content
CREATE INDEX idx_reviews_content_fts ON reviews USING gin(to_tsvector('english', coalesce(title, '') || ' ' || content));
```

### 2.2 Review Helpfulness Table

```sql
CREATE TABLE review_helpfulness (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    is_helpful BOOLEAN NOT NULL,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_review_helpfulness_review_user UNIQUE (review_id, user_id)
);

CREATE INDEX idx_review_helpfulness_review_id ON review_helpfulness(review_id);
CREATE INDEX idx_review_helpfulness_user_id ON review_helpfulness(user_id);
```

### 2.3 Review Reports Table

```sql
CREATE TABLE review_reports (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    reason VARCHAR(100) NOT NULL,
    description TEXT,
    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    resolved_by UUID REFERENCES users(id),
    resolved_at TIMESTAMP WITH TIME ZONE,
    resolution_notes TEXT,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_review_reports_review_user UNIQUE (review_id, user_id),
    CONSTRAINT chk_review_reports_status CHECK (status IN ('pending', 'reviewed', 'dismissed')),
    CONSTRAINT chk_review_reports_reason CHECK (reason IN ('spam', 'inappropriate', 'fake', 'off_topic', 'other'))
);

CREATE INDEX idx_review_reports_review_id ON review_reports(review_id);
CREATE INDEX idx_review_reports_status ON review_reports(status);
```

### 2.4 Review Responses Table (Admin/Seller Responses)

```sql
CREATE TABLE review_responses (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    content TEXT NOT NULL CHECK (char_length(content) >= 10),
    is_official BOOLEAN NOT NULL DEFAULT FALSE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_review_responses_official UNIQUE (review_id) WHERE is_official = TRUE
);

CREATE INDEX idx_review_responses_review_id ON review_responses(review_id);
```

### 2.5 Review Images Table

```sql
CREATE TABLE review_images (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    review_id UUID NOT NULL REFERENCES reviews(id) ON DELETE CASCADE,
    image_url VARCHAR(500) NOT NULL,
    thumbnail_url VARCHAR(500),
    alt_text VARCHAR(200),
    sort_order INT NOT NULL DEFAULT 0,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_review_images_max CHECK (sort_order < 5)
);

CREATE INDEX idx_review_images_review_id ON review_images(review_id);
```

### 2.6 Product Rating Cache (Alter Products Table)

```sql
ALTER TABLE products
ADD COLUMN average_rating DECIMAL(3, 2) NOT NULL DEFAULT 0.00,
ADD COLUMN review_count INT NOT NULL DEFAULT 0,
ADD COLUMN rating_1_count INT NOT NULL DEFAULT 0,
ADD COLUMN rating_2_count INT NOT NULL DEFAULT 0,
ADD COLUMN rating_3_count INT NOT NULL DEFAULT 0,
ADD COLUMN rating_4_count INT NOT NULL DEFAULT 0,
ADD COLUMN rating_5_count INT NOT NULL DEFAULT 0,
ADD COLUMN verified_review_count INT NOT NULL DEFAULT 0;

CREATE INDEX idx_products_average_rating ON products(average_rating DESC);
CREATE INDEX idx_products_review_count ON products(review_count DESC);
```

### 2.7 Database Functions

```sql
-- Function to update product rating statistics
CREATE OR REPLACE FUNCTION update_product_rating_stats()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        UPDATE products SET
            average_rating = COALESCE((
                SELECT ROUND(AVG(rating)::numeric, 2)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved'
            ), 0),
            review_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved'
            ),
            rating_1_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND rating = 1
            ),
            rating_2_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND rating = 2
            ),
            rating_3_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND rating = 3
            ),
            rating_4_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND rating = 4
            ),
            rating_5_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND rating = 5
            ),
            verified_review_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = NEW.product_id AND status = 'approved' AND is_verified_purchase = TRUE
            ),
            updated_at = NOW()
        WHERE id = NEW.product_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE products SET
            average_rating = COALESCE((
                SELECT ROUND(AVG(rating)::numeric, 2)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved'
            ), 0),
            review_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved'
            ),
            rating_1_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND rating = 1
            ),
            rating_2_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND rating = 2
            ),
            rating_3_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND rating = 3
            ),
            rating_4_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND rating = 4
            ),
            rating_5_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND rating = 5
            ),
            verified_review_count = (
                SELECT COUNT(*)
                FROM reviews
                WHERE product_id = OLD.product_id AND status = 'approved' AND is_verified_purchase = TRUE
            ),
            updated_at = NOW()
        WHERE id = OLD.product_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_product_rating_stats
AFTER INSERT OR UPDATE OR DELETE ON reviews
FOR EACH ROW EXECUTE FUNCTION update_product_rating_stats();

-- Function to update helpfulness counts
CREATE OR REPLACE FUNCTION update_review_helpfulness_counts()
RETURNS TRIGGER AS $$
BEGIN
    IF TG_OP = 'INSERT' OR TG_OP = 'UPDATE' THEN
        UPDATE reviews SET
            helpful_count = (
                SELECT COUNT(*) FROM review_helpfulness
                WHERE review_id = NEW.review_id AND is_helpful = TRUE
            ),
            not_helpful_count = (
                SELECT COUNT(*) FROM review_helpfulness
                WHERE review_id = NEW.review_id AND is_helpful = FALSE
            ),
            updated_at = NOW()
        WHERE id = NEW.review_id;
        RETURN NEW;
    ELSIF TG_OP = 'DELETE' THEN
        UPDATE reviews SET
            helpful_count = (
                SELECT COUNT(*) FROM review_helpfulness
                WHERE review_id = OLD.review_id AND is_helpful = TRUE
            ),
            not_helpful_count = (
                SELECT COUNT(*) FROM review_helpfulness
                WHERE review_id = OLD.review_id AND is_helpful = FALSE
            ),
            updated_at = NOW()
        WHERE id = OLD.review_id;
        RETURN OLD;
    END IF;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER trg_update_review_helpfulness_counts
AFTER INSERT OR UPDATE OR DELETE ON review_helpfulness
FOR EACH ROW EXECUTE FUNCTION update_review_helpfulness_counts();
```

---

## 3. API Endpoints

### 3.1 Public Review Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/products/{productId}/reviews` | Get paginated product reviews | No |
| GET | `/api/v1/products/{productId}/reviews/stats` | Get rating statistics | No |
| GET | `/api/v1/reviews/{reviewId}` | Get single review details | No |

### 3.2 Authenticated Review Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| POST | `/api/v1/products/{productId}/reviews` | Create a review | User |
| PUT | `/api/v1/reviews/{reviewId}` | Update own review | User |
| DELETE | `/api/v1/reviews/{reviewId}` | Delete own review | User |
| POST | `/api/v1/reviews/{reviewId}/helpful` | Mark review as helpful/not helpful | User |
| DELETE | `/api/v1/reviews/{reviewId}/helpful` | Remove helpfulness vote | User |
| POST | `/api/v1/reviews/{reviewId}/report` | Report a review | User |
| GET | `/api/v1/users/me/reviews` | Get current user's reviews | User |
| GET | `/api/v1/users/me/can-review/{productId}` | Check if user can review product | User |

### 3.3 Review Response Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/reviews/{reviewId}/responses` | Get review responses | No |
| POST | `/api/v1/reviews/{reviewId}/responses` | Add response to review | User |
| PUT | `/api/v1/reviews/{reviewId}/responses/{responseId}` | Update own response | User |
| DELETE | `/api/v1/reviews/{reviewId}/responses/{responseId}` | Delete own response | User |

### 3.4 Admin Review Endpoints

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/admin/reviews` | Get all reviews (paginated, filterable) | Admin |
| GET | `/api/v1/admin/reviews/pending` | Get pending reviews | Admin |
| PUT | `/api/v1/admin/reviews/{reviewId}/status` | Approve/reject review | Admin |
| PUT | `/api/v1/admin/reviews/{reviewId}/status/bulk` | Bulk approve/reject | Admin |
| POST | `/api/v1/admin/reviews/{reviewId}/responses` | Add official response | Admin |
| GET | `/api/v1/admin/reviews/reports` | Get reported reviews | Admin |
| PUT | `/api/v1/admin/reviews/reports/{reportId}` | Resolve report | Admin |
| GET | `/api/v1/admin/reviews/analytics` | Get review analytics | Admin |

### 3.5 Request/Response DTOs

#### CreateReviewRequest
```json
{
  "rating": 5,
  "title": "Excellent Air Conditioner",
  "content": "This AC unit is incredibly quiet and efficient. Cools down my living room in minutes.",
  "pros": "Quiet operation, energy efficient, easy installation",
  "cons": "Remote could be more intuitive"
}
```

#### ReviewResponse
```json
{
  "id": "uuid",
  "productId": "uuid",
  "productName": "Samsung WindFree AC 12000 BTU",
  "productSlug": "samsung-windfree-ac-12000-btu",
  "userId": "uuid",
  "userName": "John D.",
  "userAvatar": "https://...",
  "rating": 5,
  "title": "Excellent Air Conditioner",
  "content": "This AC unit is incredibly quiet...",
  "pros": "Quiet operation, energy efficient",
  "cons": "Remote could be more intuitive",
  "isVerifiedPurchase": true,
  "status": "approved",
  "helpfulCount": 15,
  "notHelpfulCount": 2,
  "currentUserVote": "helpful",
  "images": [
    {
      "id": "uuid",
      "imageUrl": "https://...",
      "thumbnailUrl": "https://...",
      "altText": "AC installed"
    }
  ],
  "officialResponse": {
    "id": "uuid",
    "content": "Thank you for your review!",
    "createdAt": "2024-01-15T10:30:00Z"
  },
  "createdAt": "2024-01-10T14:22:00Z",
  "updatedAt": "2024-01-10T14:22:00Z"
}
```

#### ProductReviewStatsResponse
```json
{
  "productId": "uuid",
  "averageRating": 4.35,
  "totalReviews": 127,
  "verifiedReviews": 98,
  "ratingDistribution": {
    "5": 67,
    "4": 32,
    "3": 15,
    "2": 8,
    "1": 5
  },
  "ratingPercentages": {
    "5": 52.8,
    "4": 25.2,
    "3": 11.8,
    "2": 6.3,
    "1": 3.9
  }
}
```

#### UpdateReviewStatusRequest (Admin)
```json
{
  "status": "approved",
  "moderationNotes": "Review meets guidelines"
}
```

---

## 4. Backend Implementation Tasks

### Task REV-001: Database Schema & Migrations

**Priority**: High
**Estimated Time**: 4 hours
**Dependencies**: None

**Description**: Create Entity Framework Core migrations for all review-related tables.

**Acceptance Criteria**:
- [ ] Create `Review` entity with all properties
- [ ] Create `ReviewHelpfulness` entity
- [ ] Create `ReviewReport` entity
- [ ] Create `ReviewResponse` entity
- [ ] Create `ReviewImage` entity
- [ ] Add rating cache columns to `Product` entity
- [ ] Create migration with proper indexes
- [ ] Add CHECK constraints for rating (1-5) and status values
- [ ] Configure unique constraints (one review per user per product)
- [ ] Seed sample review data for development
- [ ] Test migration up and down

**Technical Notes**:
```csharp
public class Review : BaseEntity
{
    public Guid ProductId { get; set; }
    public Guid UserId { get; set; }
    public Guid? OrderId { get; set; }
    public Guid? OrderItemId { get; set; }
    public int Rating { get; set; }
    public string? Title { get; set; }
    public string Content { get; set; } = string.Empty;
    public string? Pros { get; set; }
    public string? Cons { get; set; }
    public bool IsVerifiedPurchase { get; set; }
    public ReviewStatus Status { get; set; }
    public string? ModerationNotes { get; set; }
    public Guid? ModeratedById { get; set; }
    public DateTime? ModeratedAt { get; set; }
    public int HelpfulCount { get; set; }
    public int NotHelpfulCount { get; set; }
    public int ReportCount { get; set; }

    // Navigation properties
    public Product Product { get; set; } = null!;
    public User User { get; set; } = null!;
    public Order? Order { get; set; }
    public User? ModeratedBy { get; set; }
    public ICollection<ReviewHelpfulness> HelpfulnessVotes { get; set; } = new List<ReviewHelpfulness>();
    public ICollection<ReviewReport> Reports { get; set; } = new List<ReviewReport>();
    public ICollection<ReviewResponse> Responses { get; set; } = new List<ReviewResponse>();
    public ICollection<ReviewImage> Images { get; set; } = new List<ReviewImage>();
}
```

---

### Task REV-002: Review Repository & Service Layer

**Priority**: High
**Estimated Time**: 8 hours
**Dependencies**: REV-001

**Description**: Implement repository pattern and business logic service for reviews.

**Acceptance Criteria**:
- [ ] Create `IReviewRepository` interface
- [ ] Implement `ReviewRepository` with EF Core
- [ ] Create `IReviewService` interface
- [ ] Implement `ReviewService` with all business logic
- [ ] Implement verified purchase detection logic
- [ ] Implement product rating recalculation
- [ ] Add validation using FluentValidation
- [ ] Handle concurrency for helpfulness voting
- [ ] Unit tests for service layer (90%+ coverage)

**Verified Purchase Logic**:
```csharp
public async Task<bool> IsVerifiedPurchaseAsync(Guid userId, Guid productId)
{
    return await _orderRepository.AnyAsync(o =>
        o.UserId == userId &&
        o.Status == OrderStatus.Delivered &&
        o.Items.Any(i => i.ProductId == productId));
}
```

---

### Task REV-003: Verified Purchase Detection Service

**Priority**: High
**Estimated Time**: 4 hours
**Dependencies**: REV-001, REV-002

**Description**: Create dedicated service for verifying purchase history.

**Acceptance Criteria**:
- [ ] Implement `IVerifiedPurchaseService` interface
- [ ] Check if user has ordered the specific product
- [ ] Verify order status is `Delivered` or `Completed`
- [ ] Return order reference for linking to review
- [ ] Handle multiple orders of same product
- [ ] Cache purchase verification results (short TTL)
- [ ] Unit tests for all scenarios

**Service Implementation**:
```csharp
public interface IVerifiedPurchaseService
{
    Task<VerifiedPurchaseResult> CheckPurchaseAsync(Guid userId, Guid productId);
}

public record VerifiedPurchaseResult(
    bool IsVerified,
    Guid? OrderId,
    Guid? OrderItemId,
    DateTime? PurchaseDate
);
```

---

### Task REV-004: Review API Controllers

**Priority**: High
**Estimated Time**: 6 hours
**Dependencies**: REV-002, REV-003

**Description**: Implement all review-related API endpoints.

**Acceptance Criteria**:
- [ ] Create `ReviewsController` for public endpoints
- [ ] Create `UserReviewsController` for authenticated user endpoints
- [ ] Create `AdminReviewsController` for admin endpoints
- [ ] Implement proper authorization policies
- [ ] Add request validation with FluentValidation
- [ ] Return proper HTTP status codes
- [ ] Implement pagination with cursor-based or offset pagination
- [ ] Add rate limiting for review creation
- [ ] Document endpoints with OpenAPI/Swagger

**Controller Structure**:
```csharp
[ApiController]
[Route("api/v1/products/{productId}/reviews")]
public class ProductReviewsController : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), 200)]
    public async Task<IActionResult> GetProductReviews(
        Guid productId,
        [FromQuery] ReviewFilterRequest filter) { }

    [HttpPost]
    [Authorize]
    [ProducesResponseType(typeof(ReviewDto), 201)]
    [ProducesResponseType(typeof(ProblemDetails), 400)]
    [ProducesResponseType(typeof(ProblemDetails), 409)] // Already reviewed
    public async Task<IActionResult> CreateReview(
        Guid productId,
        [FromBody] CreateReviewRequest request) { }
}
```

---

### Task REV-005: Review Moderation Service

**Priority**: High
**Estimated Time**: 6 hours
**Dependencies**: REV-002

**Description**: Implement review moderation workflow for administrators.

**Acceptance Criteria**:
- [ ] Implement status transition logic (pending -> approved/rejected)
- [ ] Add moderation notes and moderator tracking
- [ ] Implement bulk moderation operations
- [ ] Send email notifications on status change (optional)
- [ ] Create moderation queue queries
- [ ] Filter reviews by status, date, rating
- [ ] Log moderation actions for audit trail
- [ ] Unit tests for moderation workflows

**Moderation Flow**:
```
New Review -> Pending -> [Admin Reviews] -> Approved/Rejected
                              |
                              v
                          Flagged (via reports) -> [Admin Reviews] -> Decision
```

---

### Task REV-006: Helpfulness Voting Service

**Priority**: Medium
**Estimated Time**: 4 hours
**Dependencies**: REV-002

**Description**: Implement helpful/not helpful voting functionality.

**Acceptance Criteria**:
- [ ] Allow users to vote once per review
- [ ] Toggle vote if already voted
- [ ] Update review helpfulness counts
- [ ] Prevent self-voting on own reviews
- [ ] Handle concurrent voting (optimistic concurrency)
- [ ] Return updated counts after voting
- [ ] Unit tests for voting scenarios

---

### Task REV-007: Review Reporting Service

**Priority**: Medium
**Estimated Time**: 4 hours
**Dependencies**: REV-002

**Description**: Implement review reporting and flagging system.

**Acceptance Criteria**:
- [ ] Allow users to report inappropriate reviews
- [ ] Require report reason selection
- [ ] Auto-flag reviews with multiple reports (threshold: 3)
- [ ] Prevent duplicate reports from same user
- [ ] Admin interface for reviewing reports
- [ ] Resolution workflow for reports
- [ ] Email notification to admins for flagged reviews

---

### Task REV-008: Review Response Service

**Priority**: Medium
**Estimated Time**: 3 hours
**Dependencies**: REV-002

**Description**: Implement review response/reply functionality.

**Acceptance Criteria**:
- [ ] Allow one official response per review (admin/seller)
- [ ] Allow multiple user responses
- [ ] Edit and delete own responses
- [ ] Include response in review listing
- [ ] Notify reviewer when official response added

---

### Task REV-009: Review Image Upload Service

**Priority**: Low
**Estimated Time**: 6 hours
**Dependencies**: REV-002

**Description**: Implement image upload functionality for reviews.

**Acceptance Criteria**:
- [ ] Accept up to 5 images per review
- [ ] Validate image types (JPEG, PNG, WebP)
- [ ] Resize and compress images
- [ ] Generate thumbnails
- [ ] Store images in cloud storage (Azure Blob/S3)
- [ ] Delete images when review is deleted
- [ ] Moderate images (optional: AI-based)

---

### Task REV-010: Review Analytics Service

**Priority**: Low
**Estimated Time**: 4 hours
**Dependencies**: REV-002

**Description**: Implement analytics and insights for reviews.

**Acceptance Criteria**:
- [ ] Calculate average ratings over time
- [ ] Track review volume trends
- [ ] Identify products with declining ratings
- [ ] Most helpful reviewers
- [ ] Common keywords in positive/negative reviews
- [ ] Export analytics data

---

## 5. Frontend Implementation Tasks

### Task REV-011: Star Rating Component

**Priority**: High
**Estimated Time**: 3 hours
**Dependencies**: None

**Description**: Create reusable star rating component for display and input.

**Acceptance Criteria**:
- [ ] Display mode: Show filled/half/empty stars based on rating
- [ ] Input mode: Clickable stars for rating selection
- [ ] Hover preview in input mode
- [ ] Keyboard navigation support (arrow keys)
- [ ] Accessible with ARIA labels
- [ ] Support different sizes (sm, md, lg)
- [ ] Animation on selection
- [ ] Unit tests with Angular Testing Library

**Component API**:
```typescript
@Component({
  selector: 'app-star-rating',
  standalone: true,
  template: `...`
})
export class StarRatingComponent {
  @Input() rating: number = 0;
  @Input() maxRating: number = 5;
  @Input() readonly: boolean = true;
  @Input() size: 'sm' | 'md' | 'lg' = 'md';
  @Input() showValue: boolean = false;
  @Output() ratingChange = new EventEmitter<number>();
}
```

---

### Task REV-012: Review Stats Component

**Priority**: High
**Estimated Time**: 4 hours
**Dependencies**: REV-011

**Description**: Create component showing rating distribution and summary.

**Acceptance Criteria**:
- [ ] Display average rating with star visualization
- [ ] Show total review count
- [ ] Display verified review percentage
- [ ] Show rating distribution bars (5 to 1 stars)
- [ ] Clickable bars to filter by rating
- [ ] Responsive layout
- [ ] Loading skeleton state

**Layout**:
```
┌─────────────────────────────────────┐
│  ★★★★☆  4.3 out of 5              │
│  127 reviews (98 verified)          │
├─────────────────────────────────────┤
│  5 ★ ████████████████████  67 (53%)│
│  4 ★ ██████████            32 (25%)│
│  3 ★ █████                 15 (12%)│
│  2 ★ ███                    8  (6%)│
│  1 ★ ██                     5  (4%)│
└─────────────────────────────────────┘
```

---

### Task REV-013: Review List Component

**Priority**: High
**Estimated Time**: 6 hours
**Dependencies**: REV-011, REV-012

**Description**: Create paginated list of reviews with filtering and sorting.

**Acceptance Criteria**:
- [ ] Display reviews with all details
- [ ] Show verified purchase badge
- [ ] Sort options: newest, oldest, highest rating, lowest rating, most helpful
- [ ] Filter by rating (1-5 stars)
- [ ] Filter by verified purchase only
- [ ] Infinite scroll or pagination
- [ ] Empty state for no reviews
- [ ] Loading skeleton states
- [ ] Responsive design

---

### Task REV-014: Review Card Component

**Priority**: High
**Estimated Time**: 4 hours
**Dependencies**: REV-011

**Description**: Create individual review display card.

**Acceptance Criteria**:
- [ ] Display user avatar and name
- [ ] Show star rating and date
- [ ] Display verified purchase badge (checkmark icon)
- [ ] Show review title and content
- [ ] Display pros/cons if provided
- [ ] Show review images with lightbox
- [ ] Display helpfulness count and voting buttons
- [ ] Show official response if exists
- [ ] Report button
- [ ] Edit/delete buttons for own reviews
- [ ] Expandable for long content

**Layout**:
```
┌─────────────────────────────────────────────────────┐
│ [Avatar] John D.  ★★★★★  5 stars  ✓ Verified       │
│          January 10, 2024                           │
├─────────────────────────────────────────────────────┤
│ Excellent Air Conditioner                           │
│                                                     │
│ This AC unit is incredibly quiet and efficient.     │
│ Cools down my living room in minutes. The energy    │
│ savings are noticeable on my electricity bill.      │
│                                                     │
│ ✓ Pros: Quiet operation, energy efficient           │
│ ✗ Cons: Remote could be more intuitive              │
│                                                     │
│ [Image1] [Image2]                                   │
├─────────────────────────────────────────────────────┤
│ 15 people found this helpful                        │
│ [👍 Helpful] [👎 Not helpful]  [Report]            │
├─────────────────────────────────────────────────────┤
│ 🏢 ClimaSite Response:                             │
│ Thank you for your feedback! We're glad you're      │
│ enjoying the quiet operation.                       │
└─────────────────────────────────────────────────────┘
```

---

### Task REV-015: Review Form Component

**Priority**: High
**Estimated Time**: 6 hours
**Dependencies**: REV-011

**Description**: Create review submission form with validation.

**Acceptance Criteria**:
- [ ] Star rating input (required)
- [ ] Title field (optional, max 200 chars)
- [ ] Content textarea (required, min 20 chars)
- [ ] Pros field (optional)
- [ ] Cons field (optional)
- [ ] Image upload (up to 5 images)
- [ ] Real-time validation feedback
- [ ] Character count display
- [ ] Submit button with loading state
- [ ] Indicate if will be verified purchase
- [ ] Success message on submission
- [ ] Error handling and display

---

### Task REV-016: Review Form Dialog

**Priority**: High
**Estimated Time**: 3 hours
**Dependencies**: REV-015

**Description**: Create modal/dialog wrapper for review form.

**Acceptance Criteria**:
- [ ] Open dialog from "Write Review" button
- [ ] Show product info in dialog header
- [ ] Include review form component
- [ ] Close on successful submission
- [ ] Confirm before closing with unsaved changes
- [ ] Accessible focus management

---

### Task REV-017: Helpfulness Voting Component

**Priority**: Medium
**Estimated Time**: 3 hours
**Dependencies**: None

**Description**: Create helpful/not helpful voting buttons.

**Acceptance Criteria**:
- [ ] Display current helpful count
- [ ] Helpful and Not Helpful buttons
- [ ] Visual feedback for user's vote
- [ ] Optimistic UI updates
- [ ] Handle unauthenticated users (prompt login)
- [ ] Prevent voting on own reviews

---

### Task REV-018: Report Review Dialog

**Priority**: Medium
**Estimated Time**: 3 hours
**Dependencies**: None

**Description**: Create dialog for reporting inappropriate reviews.

**Acceptance Criteria**:
- [ ] Radio buttons for report reason
- [ ] Optional description textarea
- [ ] Submit and cancel buttons
- [ ] Loading state during submission
- [ ] Success confirmation message
- [ ] Prevent duplicate reports

**Report Reasons**:
- Spam or advertisement
- Inappropriate content
- Fake or misleading review
- Off-topic
- Other

---

### Task REV-019: User Reviews Page

**Priority**: Medium
**Estimated Time**: 4 hours
**Dependencies**: REV-013, REV-014

**Description**: Create page showing current user's reviews.

**Acceptance Criteria**:
- [ ] List all user's reviews
- [ ] Show review status (pending, approved, rejected)
- [ ] Edit and delete options
- [ ] Link to product page
- [ ] Filter by status
- [ ] Empty state if no reviews

---

### Task REV-020: Admin Review Moderation Component

**Priority**: High
**Estimated Time**: 8 hours
**Dependencies**: REV-013, REV-014

**Description**: Create admin interface for review moderation.

**Acceptance Criteria**:
- [ ] List pending reviews with pagination
- [ ] Filter by status, date, rating, product
- [ ] Search reviews by content
- [ ] Approve/reject individual reviews
- [ ] Bulk selection and bulk actions
- [ ] Add moderation notes
- [ ] View review in context (link to product)
- [ ] View user's review history
- [ ] Reported reviews queue
- [ ] Add official response

**Layout**:
```
┌─────────────────────────────────────────────────────────┐
│ Review Moderation                                       │
├─────────────────────────────────────────────────────────┤
│ Filters: [Status ▼] [Date Range] [Rating ▼] [Search]   │
├─────────────────────────────────────────────────────────┤
│ □ Select All    Selected: 0   [Approve] [Reject]       │
├─────────────────────────────────────────────────────────┤
│ □ Review #1                                             │
│   Product: Samsung AC | User: john@... | Rating: ★★★★★ │
│   "Great product!" - Posted 2 hours ago                 │
│   [View] [Approve] [Reject] [Add Note]                  │
├─────────────────────────────────────────────────────────┤
│ □ Review #2 ⚠️ Flagged (3 reports)                      │
│   Product: LG Heater | User: jane@... | Rating: ★      │
│   "Terrible product, don't buy" - Posted 1 day ago      │
│   [View] [Approve] [Reject] [View Reports]              │
└─────────────────────────────────────────────────────────┘
```

---

### Task REV-021: Review Service (Angular)

**Priority**: High
**Estimated Time**: 4 hours
**Dependencies**: None

**Description**: Create Angular service for review API interactions.

**Acceptance Criteria**:
- [ ] Implement all API calls
- [ ] Type-safe request/response interfaces
- [ ] Error handling and transformation
- [ ] Caching for review stats
- [ ] Optimistic updates for voting

**Service Interface**:
```typescript
@Injectable({ providedIn: 'root' })
export class ReviewService {
  getProductReviews(productId: string, params: ReviewFilterParams): Observable<PagedResult<Review>>;
  getProductReviewStats(productId: string): Observable<ReviewStats>;
  createReview(productId: string, review: CreateReviewRequest): Observable<Review>;
  updateReview(reviewId: string, review: UpdateReviewRequest): Observable<Review>;
  deleteReview(reviewId: string): Observable<void>;
  voteHelpful(reviewId: string, isHelpful: boolean): Observable<HelpfulnessVote>;
  removeVote(reviewId: string): Observable<void>;
  reportReview(reviewId: string, report: ReportReviewRequest): Observable<void>;
  canReview(productId: string): Observable<CanReviewResponse>;
  getUserReviews(): Observable<Review[]>;
}
```

---

### Task REV-022: Review State Management (Signals)

**Priority**: Medium
**Estimated Time**: 4 hours
**Dependencies**: REV-021

**Description**: Implement state management for reviews using Angular signals.

**Acceptance Criteria**:
- [ ] Create ReviewStore with signals
- [ ] Manage review list state
- [ ] Manage review stats state
- [ ] Handle loading and error states
- [ ] Optimistic updates for voting
- [ ] Cache invalidation on new review

**Store Structure**:
```typescript
@Injectable({ providedIn: 'root' })
export class ReviewStore {
  private readonly _reviews = signal<Review[]>([]);
  private readonly _stats = signal<ReviewStats | null>(null);
  private readonly _loading = signal(false);
  private readonly _error = signal<string | null>(null);

  readonly reviews = this._reviews.asReadonly();
  readonly stats = this._stats.asReadonly();
  readonly loading = this._loading.asReadonly();
  readonly error = this._error.asReadonly();

  readonly averageRating = computed(() => this._stats()?.averageRating ?? 0);
  readonly totalReviews = computed(() => this._stats()?.totalReviews ?? 0);
}
```

---

## 6. E2E Tests (Playwright - NO MOCKING)

All E2E tests must run against a real database and API with no mocking. Tests use factory functions to create test data directly in the database.

### Test Setup

```typescript
// tests/e2e/fixtures/review.fixture.ts
import { test as base, expect } from '@playwright/test';
import { TestFactory } from './test-factory';
import { loginAs } from './auth-helpers';

type ReviewFixtures = {
  factory: TestFactory;
  loginAs: typeof loginAs;
};

export const test = base.extend<ReviewFixtures>({
  factory: async ({ request }, use) => {
    const factory = new TestFactory(request);
    await use(factory);
    await factory.cleanup();
  },
  loginAs: async ({ page }, use) => {
    await use((email: string, password: string) => loginAs(page, email, password));
  }
});

export { expect };
```

```typescript
// tests/e2e/fixtures/test-factory.ts
export class TestFactory {
  private createdUsers: string[] = [];
  private createdProducts: string[] = [];
  private createdOrders: string[] = [];
  private createdReviews: string[] = [];

  constructor(private request: APIRequestContext) {}

  async createUser(overrides?: Partial<CreateUserRequest>): Promise<User> {
    const userData = {
      email: `test-${Date.now()}-${Math.random().toString(36).substring(7)}@example.com`,
      password: 'TestPass123!',
      firstName: 'Test',
      lastName: 'User',
      ...overrides
    };

    const response = await this.request.post('/api/v1/test/users', { data: userData });
    const user = await response.json();
    this.createdUsers.push(user.id);
    return { ...user, password: userData.password };
  }

  async createProduct(overrides?: Partial<CreateProductRequest>): Promise<Product> {
    const productData = {
      name: `Test Product ${Date.now()}`,
      slug: `test-product-${Date.now()}`,
      description: 'A test product for E2E testing',
      price: 999.99,
      stock: 100,
      categoryId: await this.getDefaultCategoryId(),
      ...overrides
    };

    const response = await this.request.post('/api/v1/test/products', { data: productData });
    const product = await response.json();
    this.createdProducts.push(product.id);
    return product;
  }

  async createOrder(userId: string, items: OrderItem[]): Promise<Order> {
    const orderData = {
      userId,
      items,
      shippingAddress: {
        street: '123 Test St',
        city: 'Test City',
        state: 'TS',
        postalCode: '12345',
        country: 'US'
      }
    };

    const response = await this.request.post('/api/v1/test/orders', { data: orderData });
    const order = await response.json();
    this.createdOrders.push(order.id);
    return order;
  }

  async createReview(userId: string, productId: string, overrides?: Partial<CreateReviewRequest>): Promise<Review> {
    const reviewData = {
      userId,
      productId,
      rating: 5,
      title: 'Test Review',
      content: 'This is a test review with enough content to pass validation.',
      ...overrides
    };

    const response = await this.request.post('/api/v1/test/reviews', { data: reviewData });
    const review = await response.json();
    this.createdReviews.push(review.id);
    return review;
  }

  async markOrderDelivered(orderId: string): Promise<void> {
    await this.request.put(`/api/v1/test/orders/${orderId}/status`, {
      data: { status: 'delivered' }
    });
  }

  async cleanup(): Promise<void> {
    // Cleanup in reverse dependency order
    for (const id of this.createdReviews) {
      await this.request.delete(`/api/v1/test/reviews/${id}`).catch(() => {});
    }
    for (const id of this.createdOrders) {
      await this.request.delete(`/api/v1/test/orders/${id}`).catch(() => {});
    }
    for (const id of this.createdProducts) {
      await this.request.delete(`/api/v1/test/products/${id}`).catch(() => {});
    }
    for (const id of this.createdUsers) {
      await this.request.delete(`/api/v1/test/users/${id}`).catch(() => {});
    }
  }
}
```

---

### Test REV-E2E-001: Verified Purchaser Leaves Review with Badge

```typescript
// tests/e2e/reviews/verified-purchase-review.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Verified Purchase Reviews', () => {
  test('verified purchaser can leave review with verified badge', async ({ page, factory, loginAs }) => {
    // Arrange: Create user, product, and completed order
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Samsung WindFree AC 12000 BTU',
      slug: 'samsung-windfree-ac-12000-btu'
    });
    const order = await factory.createOrder(user.id, [{
      productId: product.id,
      quantity: 1,
      price: product.price
    }]);

    // Mark order as delivered (required for verified purchase)
    await factory.markOrderDelivered(order.id);

    // Act: Login and navigate to product
    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    // Click write review button
    await page.click('[data-testid="write-review-button"]');

    // Fill review form
    await page.click('[data-testid="star-5"]'); // 5 stars
    await page.fill('[data-testid="review-title"]', 'Excellent Air Conditioner!');
    await page.fill('[data-testid="review-content"]',
      'This AC unit is incredibly quiet and efficient. ' +
      'It cools down my living room in just a few minutes. ' +
      'The energy savings are already noticeable on my electricity bill.'
    );
    await page.fill('[data-testid="review-pros"]', 'Quiet operation, energy efficient, easy installation');
    await page.fill('[data-testid="review-cons"]', 'Remote could be more intuitive');

    // Submit review
    await page.click('[data-testid="submit-review"]');

    // Assert: Wait for success and verify review appears
    await expect(page.getByText('Review submitted successfully')).toBeVisible();
    await expect(page.getByText('Excellent Air Conditioner!')).toBeVisible();

    // Assert: Verified badge is displayed
    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Excellent Air Conditioner!'
    });
    await expect(reviewCard.getByTestId('verified-badge')).toBeVisible();
    await expect(reviewCard.getByTestId('verified-badge')).toHaveText(/Verified Purchase/);

    // Assert: Review content is displayed correctly
    await expect(reviewCard.getByTestId('star-rating')).toHaveAttribute('data-rating', '5');
    await expect(reviewCard.getByText('Quiet operation, energy efficient')).toBeVisible();
  });

  test('verified badge shows checkmark icon and proper styling', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();
    const order = await factory.createOrder(user.id, [{ productId: product.id, quantity: 1, price: product.price }]);
    await factory.markOrderDelivered(order.id);

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    await page.click('[data-testid="star-4"]');
    await page.fill('[data-testid="review-title"]', 'Good product');
    await page.fill('[data-testid="review-content"]', 'Works as expected. Delivery was fast and installation was straightforward.');
    await page.click('[data-testid="submit-review"]');

    const verifiedBadge = page.getByTestId('verified-badge');
    await expect(verifiedBadge).toBeVisible();
    await expect(verifiedBadge.locator('svg, .icon-check')).toBeVisible();

    // Verify badge has appropriate styling (green color typically)
    const badgeColor = await verifiedBadge.evaluate(el =>
      window.getComputedStyle(el).color
    );
    expect(badgeColor).toMatch(/rgb\((0|22), (128|163), (0|74)\)/); // Green-ish color
  });
});
```

---

### Test REV-E2E-002: Non-Purchaser Review Without Badge

```typescript
// tests/e2e/reviews/non-purchaser-review.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Non-Purchaser Reviews', () => {
  test('non-purchaser review does not have verified badge', async ({ page, factory, loginAs }) => {
    // Arrange: Create user and product (NO order)
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'LG Dual Inverter AC',
      slug: 'lg-dual-inverter-ac'
    });

    // Act: Login and navigate to product
    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    // Click write review button
    await page.click('[data-testid="write-review-button"]');

    // Fill review form
    await page.click('[data-testid="star-4"]'); // 4 stars
    await page.fill('[data-testid="review-title"]', 'Looks promising');
    await page.fill('[data-testid="review-content"]',
      'I have seen this AC at my friend\'s place and it seems to work really well. ' +
      'Planning to buy one soon based on their recommendation.'
    );

    // Submit review
    await page.click('[data-testid="submit-review"]');

    // Assert: Review appears without verified badge
    await expect(page.getByText('Review submitted successfully')).toBeVisible();
    await expect(page.getByText('Looks promising')).toBeVisible();

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Looks promising'
    });
    await expect(reviewCard.getByTestId('verified-badge')).not.toBeVisible();

    // Assert: Rating and content still displayed
    await expect(reviewCard.getByTestId('star-rating')).toHaveAttribute('data-rating', '4');
  });

  test('pending order does not grant verified status', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();
    // Create order but don't mark as delivered (stays pending)
    await factory.createOrder(user.id, [{ productId: product.id, quantity: 1, price: product.price }]);

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    await page.click('[data-testid="star-3"]');
    await page.fill('[data-testid="review-title"]', 'Still waiting');
    await page.fill('[data-testid="review-content"]',
      'Ordered this product and still waiting for delivery. Will update review once received.'
    );
    await page.click('[data-testid="submit-review"]');

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Still waiting'
    });
    await expect(reviewCard.getByTestId('verified-badge')).not.toBeVisible();
  });
});
```

---

### Test REV-E2E-003: One Review Per Product Per User

```typescript
// tests/e2e/reviews/single-review-constraint.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Constraints', () => {
  test('user cannot submit multiple reviews for same product', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    // Create first review directly via API
    await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'First Review',
      content: 'This is my first review for this amazing product.'
    });

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    // Write Review button should not be visible or should show "Edit Review"
    await expect(page.getByTestId('write-review-button')).not.toBeVisible();
    await expect(page.getByTestId('edit-review-button')).toBeVisible();
  });

  test('user can edit their existing review', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();
    const order = await factory.createOrder(user.id, [{ productId: product.id, quantity: 1, price: product.price }]);
    await factory.markOrderDelivered(order.id);

    // Create existing review
    await factory.createReview(user.id, product.id, {
      rating: 4,
      title: 'Good but could be better',
      content: 'Initial thoughts on this product after first week of use.'
    });

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    // Find user's review and click edit
    await page.click('[data-testid="edit-review-button"]');

    // Update the review
    await page.click('[data-testid="star-5"]'); // Change to 5 stars
    await page.fill('[data-testid="review-title"]', 'Updated: Now I love it!');
    await page.fill('[data-testid="review-content"]',
      'After using this for a month, I have updated my review. ' +
      'This product exceeded all my expectations!'
    );
    await page.click('[data-testid="submit-review"]');

    // Verify update
    await expect(page.getByText('Review updated successfully')).toBeVisible();
    await expect(page.getByText('Updated: Now I love it!')).toBeVisible();
    await expect(page.getByText('Good but could be better')).not.toBeVisible();
  });
});
```

---

### Test REV-E2E-004: Star Rating Selection

```typescript
// tests/e2e/reviews/star-rating.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Star Rating Component', () => {
  test('star rating allows selection from 1 to 5 stars', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    // Test each star rating
    for (let rating = 1; rating <= 5; rating++) {
      await page.click(`[data-testid="star-${rating}"]`);

      // Verify correct number of stars are filled
      const filledStars = await page.locator('[data-testid="star-filled"]').count();
      expect(filledStars).toBe(rating);

      // Verify rating value is set
      const ratingInput = page.getByTestId('rating-input');
      await expect(ratingInput).toHaveValue(rating.toString());
    }
  });

  test('star rating shows hover preview', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    // Hover over 3rd star
    await page.hover('[data-testid="star-3"]');

    // Should show preview of 3 stars highlighted
    const highlightedStars = await page.locator('[data-testid="star-hover"]').count();
    expect(highlightedStars).toBeGreaterThanOrEqual(3);
  });

  test('star rating is required for submission', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    // Fill content but not rating
    await page.fill('[data-testid="review-title"]', 'Test Title');
    await page.fill('[data-testid="review-content"]', 'This is test content with enough characters.');

    // Try to submit
    await page.click('[data-testid="submit-review"]');

    // Should show validation error
    await expect(page.getByText(/rating is required/i)).toBeVisible();

    // Form should not be submitted
    await expect(page.getByTestId('review-form')).toBeVisible();
  });
});
```

---

### Test REV-E2E-005: Review Content Validation

```typescript
// tests/e2e/reviews/review-validation.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Form Validation', () => {
  test('review content must be at least 20 characters', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    await page.click('[data-testid="star-5"]');
    await page.fill('[data-testid="review-content"]', 'Too short');
    await page.click('[data-testid="submit-review"]');

    await expect(page.getByText(/at least 20 characters/i)).toBeVisible();
  });

  test('review title has max 200 characters', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    const longTitle = 'A'.repeat(250);
    await page.fill('[data-testid="review-title"]', longTitle);

    // Should be truncated or show error
    const titleValue = await page.getByTestId('review-title').inputValue();
    expect(titleValue.length).toBeLessThanOrEqual(200);
  });

  test('displays character count for content', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="write-review-button"]');

    await page.fill('[data-testid="review-content"]', 'This is my review');

    await expect(page.getByTestId('content-char-count')).toHaveText(/17.*characters/i);
  });
});
```

---

### Test REV-E2E-006: Review Helpfulness Voting

```typescript
// tests/e2e/reviews/helpfulness-voting.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Helpfulness Voting', () => {
  test('user can mark review as helpful', async ({ page, factory, loginAs }) => {
    const reviewer = await factory.createUser();
    const voter = await factory.createUser();
    const product = await factory.createProduct();

    // Create a review
    const review = await factory.createReview(reviewer.id, product.id, {
      rating: 5,
      title: 'Helpful Review',
      content: 'This is a very detailed and helpful review about this product.',
      status: 'approved'
    });

    await loginAs(voter.email, voter.password);
    await page.goto(`/products/${product.slug}`);

    // Find the review and click helpful
    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Helpful Review'
    });

    await reviewCard.getByTestId('helpful-button').click();

    // Verify vote was recorded
    await expect(reviewCard.getByTestId('helpful-count')).toHaveText(/1.*helpful/i);
    await expect(reviewCard.getByTestId('helpful-button')).toHaveClass(/voted/);
  });

  test('user can toggle helpfulness vote', async ({ page, factory, loginAs }) => {
    const reviewer = await factory.createUser();
    const voter = await factory.createUser();
    const product = await factory.createProduct();

    const review = await factory.createReview(reviewer.id, product.id, {
      rating: 4,
      title: 'Toggle Test',
      content: 'Testing the toggle functionality for helpful votes.',
      status: 'approved'
    });

    await loginAs(voter.email, voter.password);
    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Toggle Test'
    });

    // Click helpful
    await reviewCard.getByTestId('helpful-button').click();
    await expect(reviewCard.getByTestId('helpful-count')).toHaveText(/1/);

    // Click not helpful (should toggle)
    await reviewCard.getByTestId('not-helpful-button').click();
    await expect(reviewCard.getByTestId('helpful-count')).toHaveText(/0/);
    await expect(reviewCard.getByTestId('not-helpful-button')).toHaveClass(/voted/);
  });

  test('user cannot vote on own review', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    const review = await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'My Own Review',
      content: 'I should not be able to vote on my own review.',
      status: 'approved'
    });

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'My Own Review'
    });

    // Voting buttons should be disabled or hidden
    await expect(reviewCard.getByTestId('helpful-button')).toBeDisabled();
  });

  test('unauthenticated user is prompted to login to vote', async ({ page, factory }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'Public Review',
      content: 'This review is visible to everyone including guests.',
      status: 'approved'
    });

    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Public Review'
    });

    await reviewCard.getByTestId('helpful-button').click();

    // Should redirect to login or show login prompt
    await expect(page.getByText(/sign in|log in/i)).toBeVisible();
  });
});
```

---

### Test REV-E2E-007: Admin Review Moderation

```typescript
// tests/e2e/reviews/admin-moderation.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Admin Review Moderation', () => {
  test('admin can view pending reviews', async ({ page, factory, loginAs }) => {
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();
    const product = await factory.createProduct();

    // Create pending reviews
    await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'Pending Review 1',
      content: 'This review is waiting for moderation approval.',
      status: 'pending'
    });

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews');

    // Filter by pending
    await page.selectOption('[data-testid="status-filter"]', 'pending');

    await expect(page.getByText('Pending Review 1')).toBeVisible();
  });

  test('admin can approve a review', async ({ page, factory, loginAs }) => {
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();
    const product = await factory.createProduct();

    const review = await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'Needs Approval',
      content: 'This is a legitimate review that should be approved.',
      status: 'pending'
    });

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews');

    const reviewRow = page.locator('[data-testid="review-row"]').filter({
      hasText: 'Needs Approval'
    });

    await reviewRow.getByTestId('approve-button').click();

    // Confirm approval
    await page.getByTestId('confirm-approve').click();

    // Verify status changed
    await expect(page.getByText('Review approved successfully')).toBeVisible();
    await expect(reviewRow.getByTestId('status-badge')).toHaveText('Approved');
  });

  test('admin can reject a review with notes', async ({ page, factory, loginAs }) => {
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(user.id, product.id, {
      rating: 1,
      title: 'Spam Review',
      content: 'Buy cheap products at spam-site.com! Best deals!',
      status: 'pending'
    });

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews');

    const reviewRow = page.locator('[data-testid="review-row"]').filter({
      hasText: 'Spam Review'
    });

    await reviewRow.getByTestId('reject-button').click();

    // Add moderation notes
    await page.fill('[data-testid="moderation-notes"]', 'Contains spam links');
    await page.getByTestId('confirm-reject').click();

    await expect(page.getByText('Review rejected')).toBeVisible();
    await expect(reviewRow.getByTestId('status-badge')).toHaveText('Rejected');
  });

  test('admin can bulk approve reviews', async ({ page, factory, loginAs }) => {
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();

    // Create multiple products and reviews
    for (let i = 1; i <= 3; i++) {
      const product = await factory.createProduct({ name: `Product ${i}` });
      await factory.createReview(user.id, product.id, {
        rating: 4,
        title: `Bulk Review ${i}`,
        content: `This is bulk review number ${i} for testing.`,
        status: 'pending'
      });
    }

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews');

    // Select all pending reviews
    await page.click('[data-testid="select-all"]');

    // Bulk approve
    await page.click('[data-testid="bulk-approve"]');
    await page.click('[data-testid="confirm-bulk-approve"]');

    await expect(page.getByText('3 reviews approved')).toBeVisible();
  });

  test('admin can add official response to review', async ({ page, factory, loginAs }) => {
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();
    const product = await factory.createProduct();

    const review = await factory.createReview(user.id, product.id, {
      rating: 2,
      title: 'Disappointed Customer',
      content: 'The product arrived damaged and customer service was unhelpful.',
      status: 'approved'
    });

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews');

    const reviewRow = page.locator('[data-testid="review-row"]').filter({
      hasText: 'Disappointed Customer'
    });

    await reviewRow.getByTestId('respond-button').click();

    await page.fill('[data-testid="official-response"]',
      'We sincerely apologize for your experience. ' +
      'Please contact us at support@climasite.com with your order number ' +
      'and we will arrange a replacement immediately.'
    );
    await page.click('[data-testid="submit-response"]');

    await expect(page.getByText('Response added successfully')).toBeVisible();

    // Verify response appears on product page
    await page.goto(`/products/${product.slug}`);
    await expect(page.getByText('ClimaSite Response')).toBeVisible();
    await expect(page.getByText('arrange a replacement immediately')).toBeVisible();
  });
});
```

---

### Test REV-E2E-008: Review Filtering and Sorting

```typescript
// tests/e2e/reviews/filtering-sorting.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Filtering and Sorting', () => {
  test('can filter reviews by star rating', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create reviews with different ratings
    for (let rating = 1; rating <= 5; rating++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating,
        title: `${rating} Star Review`,
        content: `This is a ${rating} star review for filtering test.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Filter by 5 stars
    await page.click('[data-testid="filter-5-stars"]');

    await expect(page.getByText('5 Star Review')).toBeVisible();
    await expect(page.getByText('4 Star Review')).not.toBeVisible();
    await expect(page.getByText('1 Star Review')).not.toBeVisible();

    // Filter by 1 star
    await page.click('[data-testid="filter-1-star"]');

    await expect(page.getByText('1 Star Review')).toBeVisible();
    await expect(page.getByText('5 Star Review')).not.toBeVisible();
  });

  test('can filter to show only verified purchases', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create verified and non-verified reviews
    const verifiedUser = await factory.createUser();
    const order = await factory.createOrder(verifiedUser.id, [{
      productId: product.id, quantity: 1, price: product.price
    }]);
    await factory.markOrderDelivered(order.id);
    await factory.createReview(verifiedUser.id, product.id, {
      rating: 5,
      title: 'Verified Review',
      content: 'I actually bought this product and it is great!',
      status: 'approved',
      isVerifiedPurchase: true
    });

    const nonVerifiedUser = await factory.createUser();
    await factory.createReview(nonVerifiedUser.id, product.id, {
      rating: 4,
      title: 'Non-Verified Review',
      content: 'I have seen this product somewhere and it looks nice.',
      status: 'approved',
      isVerifiedPurchase: false
    });

    await page.goto(`/products/${product.slug}`);

    // Toggle verified only filter
    await page.click('[data-testid="verified-only-toggle"]');

    await expect(page.getByText('Verified Review')).toBeVisible();
    await expect(page.getByText('Non-Verified Review')).not.toBeVisible();
  });

  test('can sort reviews by most helpful', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create reviews with different helpfulness
    const user1 = await factory.createUser();
    await factory.createReview(user1.id, product.id, {
      rating: 5,
      title: 'Most Helpful Review',
      content: 'This review has the most helpful votes.',
      status: 'approved',
      helpfulCount: 50
    });

    const user2 = await factory.createUser();
    await factory.createReview(user2.id, product.id, {
      rating: 4,
      title: 'Less Helpful Review',
      content: 'This review has fewer helpful votes.',
      status: 'approved',
      helpfulCount: 10
    });

    await page.goto(`/products/${product.slug}`);

    // Sort by most helpful
    await page.selectOption('[data-testid="review-sort"]', 'most_helpful');

    const reviewCards = page.locator('[data-testid="review-card"]');
    const firstReviewTitle = await reviewCards.first().getByTestId('review-title').textContent();
    expect(firstReviewTitle).toBe('Most Helpful Review');
  });

  test('can sort reviews by newest first', async ({ page, factory }) => {
    const product = await factory.createProduct();

    const user1 = await factory.createUser();
    await factory.createReview(user1.id, product.id, {
      rating: 3,
      title: 'Older Review',
      content: 'This review was created first but should appear second.',
      status: 'approved'
    });

    // Small delay to ensure different timestamps
    await new Promise(resolve => setTimeout(resolve, 100));

    const user2 = await factory.createUser();
    await factory.createReview(user2.id, product.id, {
      rating: 5,
      title: 'Newest Review',
      content: 'This review was created last and should appear first.',
      status: 'approved'
    });

    await page.goto(`/products/${product.slug}`);
    await page.selectOption('[data-testid="review-sort"]', 'newest');

    const reviewCards = page.locator('[data-testid="review-card"]');
    const firstReviewTitle = await reviewCards.first().getByTestId('review-title').textContent();
    expect(firstReviewTitle).toBe('Newest Review');
  });
});
```

---

### Test REV-E2E-009: Review Stats Display

```typescript
// tests/e2e/reviews/review-stats.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Statistics Display', () => {
  test('displays correct average rating and review count', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create reviews: 2x 5-star, 1x 4-star, 1x 3-star = average 4.25
    const ratings = [5, 5, 4, 3];
    for (const rating of ratings) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating,
        title: `${rating} Star Review`,
        content: `This is a ${rating} star review for stats testing.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Check average rating
    const avgRating = page.getByTestId('average-rating');
    await expect(avgRating).toHaveText(/4\.2|4\.3/); // Allow for rounding

    // Check total count
    const totalReviews = page.getByTestId('total-reviews');
    await expect(totalReviews).toHaveText(/4 reviews/i);
  });

  test('displays rating distribution bars correctly', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create specific distribution: 5 five-stars, 3 four-stars, 1 three-star, 1 one-star
    const distribution = { 5: 5, 4: 3, 3: 1, 2: 0, 1: 1 };

    for (const [rating, count] of Object.entries(distribution)) {
      for (let i = 0; i < count; i++) {
        const user = await factory.createUser();
        await factory.createReview(user.id, product.id, {
          rating: parseInt(rating),
          title: `Review ${rating}-${i}`,
          content: `This is review ${i} with ${rating} stars for distribution test.`,
          status: 'approved'
        });
      }
    }

    await page.goto(`/products/${product.slug}`);

    // Verify distribution bars
    for (const [rating, count] of Object.entries(distribution)) {
      const bar = page.getByTestId(`rating-bar-${rating}`);
      const countText = await bar.getByTestId('rating-count').textContent();
      expect(countText).toContain(count.toString());
    }
  });

  test('clicking rating bar filters reviews by that rating', async ({ page, factory }) => {
    const product = await factory.createProduct();

    for (let rating = 1; rating <= 5; rating++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating,
        title: `Rating ${rating} Review`,
        content: `This review has a ${rating} star rating for click filter test.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Click on 5-star bar
    await page.click('[data-testid="rating-bar-5"]');

    // Only 5-star reviews should be visible
    await expect(page.getByText('Rating 5 Review')).toBeVisible();
    await expect(page.getByText('Rating 4 Review')).not.toBeVisible();
    await expect(page.getByText('Rating 1 Review')).not.toBeVisible();
  });
});
```

---

### Test REV-E2E-010: Review Report Flow

```typescript
// tests/e2e/reviews/report-review.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Reporting', () => {
  test('user can report inappropriate review', async ({ page, factory, loginAs }) => {
    const reviewer = await factory.createUser();
    const reporter = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(reviewer.id, product.id, {
      rating: 1,
      title: 'Inappropriate Content',
      content: 'This review contains content that violates community guidelines.',
      status: 'approved'
    });

    await loginAs(reporter.email, reporter.password);
    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Inappropriate Content'
    });

    await reviewCard.getByTestId('report-button').click();

    // Select reason and submit
    await page.click('[data-testid="report-reason-inappropriate"]');
    await page.fill('[data-testid="report-description"]', 'This review is offensive');
    await page.click('[data-testid="submit-report"]');

    await expect(page.getByText('Report submitted')).toBeVisible();

    // Report button should now indicate reported
    await expect(reviewCard.getByTestId('report-button')).toHaveText(/reported/i);
  });

  test('reported reviews appear in admin queue', async ({ page, factory, loginAs }) => {
    const reviewer = await factory.createUser();
    const reporter = await factory.createUser();
    const admin = await factory.createUser({ role: 'admin' });
    const product = await factory.createProduct();

    const review = await factory.createReview(reviewer.id, product.id, {
      rating: 1,
      title: 'Reported Review',
      content: 'This review has been reported and needs admin attention.',
      status: 'approved',
      reportCount: 3 // Flagged threshold
    });

    await loginAs(admin.email, admin.password);
    await page.goto('/admin/reviews/reports');

    await expect(page.getByText('Reported Review')).toBeVisible();
    await expect(page.getByTestId('report-count')).toHaveText('3 reports');
  });
});
```

---

### Test REV-E2E-011: Review Delete Flow

```typescript
// tests/e2e/reviews/delete-review.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Delete Review', () => {
  test('user can delete their own review', async ({ page, factory, loginAs }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'Review To Delete',
      content: 'This review will be deleted by the user who created it.',
      status: 'approved'
    });

    await loginAs(user.email, user.password);
    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Review To Delete'
    });

    await reviewCard.getByTestId('delete-button').click();

    // Confirm deletion
    await page.click('[data-testid="confirm-delete"]');

    await expect(page.getByText('Review deleted successfully')).toBeVisible();
    await expect(page.getByText('Review To Delete')).not.toBeVisible();
  });

  test('user cannot delete another user\'s review', async ({ page, factory, loginAs }) => {
    const owner = await factory.createUser();
    const other = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(owner.id, product.id, {
      rating: 5,
      title: 'Someone Else Review',
      content: 'This review belongs to another user and cannot be deleted.',
      status: 'approved'
    });

    await loginAs(other.email, other.password);
    await page.goto(`/products/${product.slug}`);

    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Someone Else Review'
    });

    // Delete button should not be visible for other users
    await expect(reviewCard.getByTestId('delete-button')).not.toBeVisible();
  });

  test('deleting review updates product average rating', async ({ page, factory, loginAs, request }) => {
    const user1 = await factory.createUser();
    const user2 = await factory.createUser();
    const product = await factory.createProduct();

    // Create two reviews: 5-star and 3-star (average = 4)
    await factory.createReview(user1.id, product.id, {
      rating: 5,
      title: 'Five Star',
      content: 'This is a five star review that will remain.',
      status: 'approved'
    });

    await factory.createReview(user2.id, product.id, {
      rating: 3,
      title: 'Three Star',
      content: 'This is a three star review that will be deleted.',
      status: 'approved'
    });

    await loginAs(user2.email, user2.password);
    await page.goto(`/products/${product.slug}`);

    // Initial average should be 4.0
    await expect(page.getByTestId('average-rating')).toHaveText(/4\.0/);

    // Delete the 3-star review
    const reviewCard = page.locator('[data-testid="review-card"]').filter({
      hasText: 'Three Star'
    });
    await reviewCard.getByTestId('delete-button').click();
    await page.click('[data-testid="confirm-delete"]');

    // Average should now be 5.0
    await expect(page.getByTestId('average-rating')).toHaveText(/5\.0/);
  });
});
```

---

### Test REV-E2E-012: Review Pagination

```typescript
// tests/e2e/reviews/pagination.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Review Pagination', () => {
  test('displays paginated reviews with load more button', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create 15 reviews (assuming page size is 10)
    for (let i = 1; i <= 15; i++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating: (i % 5) + 1,
        title: `Review Number ${i}`,
        content: `This is review number ${i} for testing pagination functionality.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Should show first 10 reviews
    const initialReviews = await page.locator('[data-testid="review-card"]').count();
    expect(initialReviews).toBe(10);

    // Load more button should be visible
    await expect(page.getByTestId('load-more-reviews')).toBeVisible();

    // Click load more
    await page.click('[data-testid="load-more-reviews"]');

    // Should now show all 15 reviews
    const allReviews = await page.locator('[data-testid="review-card"]').count();
    expect(allReviews).toBe(15);

    // Load more button should be hidden
    await expect(page.getByTestId('load-more-reviews')).not.toBeVisible();
  });

  test('maintains filter state during pagination', async ({ page, factory }) => {
    const product = await factory.createProduct();

    // Create 15 five-star reviews and 5 one-star reviews
    for (let i = 1; i <= 15; i++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating: 5,
        title: `Five Star ${i}`,
        content: `This is five star review ${i} for pagination filter test.`,
        status: 'approved'
      });
    }

    for (let i = 1; i <= 5; i++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating: 1,
        title: `One Star ${i}`,
        content: `This is one star review ${i} for pagination filter test.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Filter by 5 stars
    await page.click('[data-testid="filter-5-stars"]');

    // Load more
    await page.click('[data-testid="load-more-reviews"]');

    // All visible reviews should still be 5-star
    const reviewCards = page.locator('[data-testid="review-card"]');
    const count = await reviewCards.count();

    for (let i = 0; i < count; i++) {
      const card = reviewCards.nth(i);
      const rating = await card.getByTestId('star-rating').getAttribute('data-rating');
      expect(rating).toBe('5');
    }
  });
});
```

---

### Test REV-E2E-013: Guest User Review Experience

```typescript
// tests/e2e/reviews/guest-experience.spec.ts
import { test, expect } from '../fixtures/review.fixture';

test.describe('Guest User Experience', () => {
  test('guest can view reviews but cannot write one', async ({ page, factory }) => {
    const user = await factory.createUser();
    const product = await factory.createProduct();

    await factory.createReview(user.id, product.id, {
      rating: 5,
      title: 'Visible to Guests',
      content: 'This review should be visible to users who are not logged in.',
      status: 'approved'
    });

    await page.goto(`/products/${product.slug}`);

    // Review should be visible
    await expect(page.getByText('Visible to Guests')).toBeVisible();

    // Write review button should prompt login
    await page.click('[data-testid="write-review-button"]');

    // Should redirect to login or show login modal
    await expect(page).toHaveURL(/login|signin/);
  });

  test('guest can view review statistics', async ({ page, factory }) => {
    const product = await factory.createProduct();

    for (let i = 0; i < 5; i++) {
      const user = await factory.createUser();
      await factory.createReview(user.id, product.id, {
        rating: 5 - i % 3,
        title: `Guest View Review ${i}`,
        content: `This review is for testing guest view of statistics.`,
        status: 'approved'
      });
    }

    await page.goto(`/products/${product.slug}`);

    // Stats should be visible
    await expect(page.getByTestId('average-rating')).toBeVisible();
    await expect(page.getByTestId('total-reviews')).toBeVisible();
    await expect(page.getByTestId('rating-distribution')).toBeVisible();
  });
});
```

---

## 7. Integration Tests

### Task REV-INT-001: Review Service Integration Tests

```csharp
// tests/ClimaSite.Api.Tests/Integration/Reviews/ReviewServiceTests.cs
public class ReviewServiceTests : IntegrationTestBase
{
    [Fact]
    public async Task CreateReview_WithVerifiedPurchase_SetsVerifiedFlag()
    {
        // Arrange
        var user = await CreateTestUser();
        var product = await CreateTestProduct();
        var order = await CreateDeliveredOrder(user.Id, product.Id);

        var request = new CreateReviewRequest
        {
            Rating = 5,
            Title = "Great product!",
            Content = "This product exceeded my expectations in every way."
        };

        // Act
        var review = await _reviewService.CreateReviewAsync(user.Id, product.Id, request);

        // Assert
        Assert.True(review.IsVerifiedPurchase);
        Assert.Equal(order.Id, review.OrderId);
    }

    [Fact]
    public async Task CreateReview_UpdatesProductAverageRating()
    {
        // Arrange
        var product = await CreateTestProduct();
        var user1 = await CreateTestUser();
        var user2 = await CreateTestUser();

        // Act
        await _reviewService.CreateReviewAsync(user1.Id, product.Id,
            new CreateReviewRequest { Rating = 5, Content = "Five star review content here." });
        await _reviewService.CreateReviewAsync(user2.Id, product.Id,
            new CreateReviewRequest { Rating = 3, Content = "Three star review content here." });

        // Assert
        var updatedProduct = await _productRepository.GetByIdAsync(product.Id);
        Assert.Equal(4.0m, updatedProduct.AverageRating);
        Assert.Equal(2, updatedProduct.ReviewCount);
    }
}
```

---

## 8. Performance Considerations

### Caching Strategy

- Cache product review stats with 5-minute TTL
- Cache individual reviews with invalidation on update
- Use distributed cache (Redis) for multi-instance deployment

### Query Optimization

- Use projections for listing (don't load full entities)
- Implement cursor-based pagination for large datasets
- Pre-calculate rating distributions

### Database Indexes

- Composite index on (product_id, status, created_at)
- Index on (user_id, product_id) for unique constraint
- Full-text search index on title and content

---

## 9. Security Considerations

### Input Validation

- Sanitize HTML in review content
- Rate limit review submissions (1 per product per user)
- Validate image uploads (type, size, dimensions)

### Authorization

- Users can only edit/delete own reviews
- Admin routes protected by role-based auth
- API endpoints require authentication where needed

### Data Protection

- Don't expose user emails in public API
- Anonymize user names (John D. instead of John Doe)
- Log moderation actions for audit trail

---

## 10. Accessibility Requirements

- Star rating navigable via keyboard
- ARIA labels for star ratings ("4 out of 5 stars")
- Screen reader announcements for vote changes
- Focus management in modals and forms
- Sufficient color contrast for verified badges

---

## 11. Definition of Done

### Feature Complete When

- [ ] All database tables and indexes created
- [ ] All API endpoints implemented and documented
- [ ] All frontend components built with accessibility
- [ ] All E2E tests passing (no mocking)
- [ ] Admin moderation workflow operational
- [ ] Verified purchase detection working
- [ ] Product rating cache updating correctly
- [ ] Performance benchmarks met (<200ms response time)
- [ ] Security review completed
- [ ] Code review approved
- [ ] Deployed to staging environment

---

## 12. Task Summary

| Task ID | Title | Priority | Est. Hours | Dependencies |
|---------|-------|----------|------------|--------------|
| REV-001 | Database Schema & Migrations | High | 4 | - |
| REV-002 | Review Repository & Service | High | 8 | REV-001 |
| REV-003 | Verified Purchase Service | High | 4 | REV-001, REV-002 |
| REV-004 | Review API Controllers | High | 6 | REV-002, REV-003 |
| REV-005 | Review Moderation Service | High | 6 | REV-002 |
| REV-006 | Helpfulness Voting Service | Medium | 4 | REV-002 |
| REV-007 | Review Reporting Service | Medium | 4 | REV-002 |
| REV-008 | Review Response Service | Medium | 3 | REV-002 |
| REV-009 | Review Image Upload | Low | 6 | REV-002 |
| REV-010 | Review Analytics Service | Low | 4 | REV-002 |
| REV-011 | Star Rating Component | High | 3 | - |
| REV-012 | Review Stats Component | High | 4 | REV-011 |
| REV-013 | Review List Component | High | 6 | REV-011, REV-012 |
| REV-014 | Review Card Component | High | 4 | REV-011 |
| REV-015 | Review Form Component | High | 6 | REV-011 |
| REV-016 | Review Form Dialog | High | 3 | REV-015 |
| REV-017 | Helpfulness Voting Component | Medium | 3 | - |
| REV-018 | Report Review Dialog | Medium | 3 | - |
| REV-019 | User Reviews Page | Medium | 4 | REV-013, REV-014 |
| REV-020 | Admin Moderation Component | High | 8 | REV-013, REV-014 |
| REV-021 | Review Service (Angular) | High | 4 | - |
| REV-022 | Review State Management | Medium | 4 | REV-021 |

**Total Estimated Hours: 101 hours**

---

## 13. Future Enhancements

- AI-powered review sentiment analysis
- Review incentive program (discounts for reviews)
- Photo recognition for product verification
- Multi-language review support
- Review aggregation from external sources
- Video reviews support
- Q&A section linked to reviews
