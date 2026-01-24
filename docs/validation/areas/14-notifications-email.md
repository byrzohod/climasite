# Notifications & Email - Validation Report

> Generated: 2026-01-24

## 1. Scope Summary

### Features Covered
- **In-App Notifications** - User notification center with CRUD operations
- **Email Service** - Email sending infrastructure (placeholder implementation)
- **Email Templates** - Templated emails for order confirmations, password reset, etc.
- **Notification Types** - Order, payment, review, wishlist, account notification types
- **Notification Management** - Mark as read, mark all as read, delete notifications
- **Notification Summary** - Unread count and recent notifications for header display

### API Endpoints
| Method | Endpoint | Description | Auth Required |
|--------|----------|-------------|---------------|
| GET | `/api/notifications` | Get paginated notifications | Yes |
| GET | `/api/notifications/summary` | Get notification summary (count + recent) | Yes |
| PUT | `/api/notifications/{id}/read` | Mark notification as read | Yes |
| PUT | `/api/notifications/read-all` | Mark all notifications as read | Yes |
| DELETE | `/api/notifications/{id}` | Delete a notification | Yes |

### Email Types Defined
| Email Type | Method | Status |
|------------|--------|--------|
| Welcome Email | `SendWelcomeEmailAsync` | Implemented (no-op) |
| Password Reset | `SendPasswordResetEmailAsync` | Implemented (no-op) |
| Order Confirmation | `SendOrderConfirmationEmailAsync` | Implemented (no-op) |
| Order Shipped | `SendOrderShippedEmailAsync` | Implemented (no-op) |
| Generic Email | `SendEmailAsync` | Implemented (no-op) |
| Template Email | `SendEmailAsync` (with template) | Implemented (no-op) |

### Notification Types Supported
| Type Constant | Description | Trigger |
|---------------|-------------|---------|
| `order_placed` | Order was placed | Order creation |
| `order_shipped` | Order was shipped | Shipping update |
| `order_delivered` | Order was delivered | Delivery confirmation |
| `order_cancelled` | Order was cancelled | Order cancellation |
| `payment_received` | Payment was received | Payment confirmation |
| `payment_failed` | Payment failed | Payment failure |
| `review_posted` | Review was posted | Review submission |
| `wishlist_price_drop` | Wishlist item price dropped | Price change |
| `wishlist_back_in_stock` | Wishlist item back in stock | Stock update |
| `account_update` | Account was updated | Profile change |
| `password_changed` | Password was changed | Password update |
| `promotional` | Promotional notification | Admin broadcast |

---

## 2. Code Path Map

### Backend

| Layer | Files |
|-------|-------|
| **Controllers** | `src/ClimaSite.Api/Controllers/NotificationsController.cs` |
| **Commands** | `src/ClimaSite.Application/Features/Notifications/Commands/CreateNotificationCommand.cs` |
| | `src/ClimaSite.Application/Features/Notifications/Commands/MarkNotificationReadCommand.cs` |
| | `src/ClimaSite.Application/Features/Notifications/Commands/MarkAllNotificationsReadCommand.cs` |
| | `src/ClimaSite.Application/Features/Notifications/Commands/DeleteNotificationCommand.cs` |
| **Queries** | `src/ClimaSite.Application/Features/Notifications/Queries/GetNotificationsQuery.cs` |
| | `src/ClimaSite.Application/Features/Notifications/Queries/GetNotificationSummaryQuery.cs` |
| **DTOs** | `src/ClimaSite.Application/Features/Notifications/DTOs/NotificationDtos.cs` |
| **Entity** | `src/ClimaSite.Core/Entities/Notification.cs` |
| **Configuration** | `src/ClimaSite.Infrastructure/Data/Configurations/NotificationConfiguration.cs` |
| **Email Service** | `src/ClimaSite.Infrastructure/Services/EmailService.cs` |
| **Email Interface** | `src/ClimaSite.Application/Common/Interfaces/IEmailService.cs` |
| **DI Registration** | `src/ClimaSite.Infrastructure/DependencyInjection.cs` (line 69) |
| **Auth Handler** | `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs` (email TODO) |

### Frontend

| Layer | Files |
|-------|-------|
| **Components** | None implemented yet |
| **Services** | None implemented yet |
| **Models** | None implemented yet |

**Note:** Frontend notification components (bell, dropdown, center) are planned in `docs/plans/12-notifications-system.md` (Tasks NOT-012 to NOT-016) but not yet implemented.

### Database

| Table | Purpose |
|-------|---------|
| `notifications` | In-app notification storage |
| `notification_preferences` | User notification preferences (NOT IMPLEMENTED) |
| `email_logs` | Email sending audit trail (NOT IMPLEMENTED) |
| `email_templates` | Admin-editable email templates (NOT IMPLEMENTED) |

---

## 3. Test Coverage Audit

### Unit Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| `tests/ClimaSite.Core.Tests/Entities/NotificationTests.cs` | `Constructor_WithValidData_ShouldCreateNotification` | Entity creation |
| | `SetType_WithEmptyValue_ShouldThrowArgumentException` | Type validation |
| | `SetTitle_WithEmptyValue_ShouldThrowArgumentException` | Title validation |
| | `SetTitle_WithTooLongValue_ShouldThrowArgumentException` | Title length limit |
| | `SetMessage_WithEmptyValue_ShouldThrowArgumentException` | Message validation |
| | `SetMessage_WithTooLongValue_ShouldThrowArgumentException` | Message length limit |
| | `MarkAsRead_WhenUnread_ShouldSetIsReadAndReadAt` | Read status |
| | `MarkAsRead_WhenAlreadyRead_ShouldNotUpdateReadAt` | Idempotency |
| | `MarkAsUnread_WhenRead_ShouldClearIsReadAndReadAt` | Unread status |
| | `SetLink_WithValidLink_ShouldSetLink` | Link property |
| | `SetData_WithValidData_ShouldSetData` | Data dictionary |
| | `SetData_WithNull_ShouldSetEmptyDictionary` | Null handling |

### Integration Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No API integration tests for notifications |

### E2E Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No E2E tests for notification flows |

### Email Service Tests

| Test File | Test Names | Coverage |
|-----------|------------|----------|
| **NONE FOUND** | - | No tests for EmailService |

---

## 4. Manual Verification Steps

### Notification Creation (via API)
1. Authenticate as a user
2. Create an order to trigger order notification
3. Navigate to `/api/notifications` 
4. Verify notification appears in list
5. Verify notification has correct type, title, message

### Mark Notification as Read
1. Get notification list via API
2. Note an unread notification ID
3. Call PUT `/api/notifications/{id}/read`
4. Verify response is 204 No Content
5. Get notification list again
6. Verify notification now has `isRead: true` and `readAt` timestamp

### Mark All Notifications as Read
1. Ensure user has multiple unread notifications
2. Call PUT `/api/notifications/read-all`
3. Verify response includes count of marked notifications
4. Get notification list
5. Verify all notifications have `isRead: true`

### Delete Notification
1. Get notification list via API
2. Note a notification ID
3. Call DELETE `/api/notifications/{id}`
4. Verify response is 204 No Content
5. Get notification list again
6. Verify notification no longer appears

### Notification Summary
1. Call GET `/api/notifications/summary?recentCount=5`
2. Verify response includes:
   - `totalCount` - total notifications
   - `unreadCount` - unread notifications
   - `recentItems` - array of recent notifications (max 5)

### Email Service Verification (Logs)
1. Trigger password reset via `/api/auth/forgot-password`
2. Check application logs for:
   - `Email service not configured - email to {email} with subject 'Reset Your Password' was NOT sent`
3. Verify no actual email is sent (placeholder implementation)

### Email Template Content Verification
1. Review `EmailService.cs` for template methods
2. Verify HTML content includes:
   - Proper styling
   - Dynamic placeholders (name, order ID, etc.)
   - Action buttons/links
   - Branding elements

---

## 5. Gaps & Risks

### Critical Gaps

| ID | Gap | Impact | Risk Level |
|----|-----|--------|------------|
| EMAIL-001 | **Email service is a no-op placeholder** | No emails are actually sent in any environment | Critical |
| EMAIL-002 | **No SMTP/SendGrid integration** | Cannot send transactional emails | Critical |
| EMAIL-003 | **ForgotPasswordCommandHandler has email commented out** | Password reset emails not sent | Critical |
| EMAIL-004 | **No email queue/retry mechanism** | Email failures not handled gracefully | High |
| NOT-001 | **No frontend notification UI** | Users cannot see notifications in browser | Critical |
| NOT-002 | **No notification preferences table/API** | Users cannot control notification settings | High |
| NOT-003 | **No email_logs table** | Cannot audit/troubleshoot email delivery | High |

### Medium Gaps

| ID | Gap | Impact | Risk Level |
|----|-----|--------|------------|
| EMAIL-005 | **No email template engine** | Template changes require code deployment | Medium |
| EMAIL-006 | **No email validation/sanitization** | Potential injection in email content | Medium |
| NOT-004 | **No real-time notification updates** | Users must refresh to see new notifications | Medium |
| NOT-005 | **No notification triggers implemented** | Events don't create notifications automatically | Medium |
| NOT-006 | **No admin notification broadcast** | Admin cannot send announcements to users | Medium |
| TEST-001 | **No EmailService unit tests** | Email logic untested | Medium |
| TEST-002 | **No NotificationsController integration tests** | API endpoints untested | Medium |
| TEST-003 | **No E2E tests for notification flows** | User flows unverified | Medium |

### Low Gaps

| ID | Gap | Impact | Risk Level |
|----|-----|--------|------------|
| EMAIL-007 | **Hardcoded email content in service** | Not translatable, difficult to maintain | Low |
| EMAIL-008 | **No email preview/test send feature** | Difficult to verify template changes | Low |
| NOT-007 | **No notification grouping** | Multiple similar notifications not grouped | Low |
| NOT-008 | **No notification expiry/auto-delete** | Old notifications accumulate | Low |

### Security Risks

| ID | Risk | Description |
|----|------|-------------|
| SEC-001 | **No rate limiting on notifications API** | Potential DoS via notification spam |
| SEC-002 | **No email enumeration protection in logs** | Logs reveal which emails exist |
| SEC-003 | **HTML injection in email templates** | User-provided content not sanitized |

### Error Handling Gaps

| Scenario | Current Behavior | Expected Behavior |
|----------|------------------|-------------------|
| Email send failure | Logged as warning, silently ignored | Retry with exponential backoff, alert on failure |
| Invalid notification ID | Returns generic "not found" | Returns specific error with details |
| Unauthorized notification access | Returns "not found" | Returns 403 Forbidden |
| Database failure during notification create | Uncaught exception | Graceful error with rollback |

---

## 6. Recommended Fixes & Tests

### Critical Priority

| Priority | Issue | Recommendation | Effort |
|----------|-------|----------------|--------|
| P0 | Email service placeholder | Implement actual SMTP or SendGrid integration | 8h |
| P0 | No frontend notification UI | Implement notification bell and dropdown components | 16h |
| P0 | ForgotPassword email commented | Uncomment and integrate email service | 2h |
| P0 | No notification triggers | Implement event handlers for order/auth events | 8h |

### High Priority

| Priority | Issue | Recommendation | Effort |
|----------|-------|----------------|--------|
| P1 | No email retry mechanism | Implement background job queue with retries | 8h |
| P1 | No notification preferences | Create preferences table, API, and UI | 12h |
| P1 | No email_logs table | Implement email audit logging | 4h |
| P1 | No NotificationsController tests | Create integration tests for all endpoints | 6h |
| P1 | No EmailService tests | Create unit tests with mocked SMTP client | 4h |

### Medium Priority

| Priority | Issue | Recommendation | Effort |
|----------|-------|----------------|--------|
| P2 | No E2E notification tests | Add Playwright tests for notification flows | 8h |
| P2 | No real-time updates | Implement SignalR for push notifications | 8h |
| P2 | No email template engine | Integrate Handlebars or Razor for templates | 6h |
| P2 | No admin broadcast | Add admin API for sending notifications to users | 4h |

### Low Priority

| Priority | Issue | Recommendation | Effort |
|----------|-------|----------------|--------|
| P3 | Hardcoded email content | Move templates to database or files | 4h |
| P3 | No notification grouping | Implement notification aggregation logic | 4h |
| P3 | No notification expiry | Add TTL and cleanup job for old notifications | 2h |

### Recommended Test Coverage

#### Unit Tests to Add

```csharp
// tests/ClimaSite.Infrastructure.Tests/Services/EmailServiceTests.cs
public class EmailServiceTests
{
    [Fact] public Task SendEmailAsync_WithValidParams_LogsWarning() { }
    [Fact] public Task SendWelcomeEmailAsync_FormatsCorrectSubject() { }
    [Fact] public Task SendPasswordResetEmailAsync_IncludesResetUrl() { }
    [Fact] public Task SendOrderConfirmationEmailAsync_IncludesOrderId() { }
    [Fact] public Task SendOrderShippedEmailAsync_IncludesTrackingNumber() { }
}

// tests/ClimaSite.Application.Tests/Features/Notifications/Commands/
public class CreateNotificationCommandTests
{
    [Fact] public Task Handle_WithValidRequest_CreatesNotification() { }
    [Fact] public Task Handle_WithInvalidUserId_ReturnsFailure() { }
    [Fact] public Task Validator_WithEmptyTitle_ReturnsError() { }
    [Fact] public Task Validator_WithTooLongMessage_ReturnsError() { }
}

public class MarkNotificationReadCommandTests
{
    [Fact] public Task Handle_WithValidId_MarksAsRead() { }
    [Fact] public Task Handle_WithNonExistentId_ReturnsFailure() { }
    [Fact] public Task Handle_WithOtherUsersNotification_ReturnsFailure() { }
}

public class DeleteNotificationCommandTests
{
    [Fact] public Task Handle_WithValidId_DeletesNotification() { }
    [Fact] public Task Handle_WithNonExistentId_ReturnsFailure() { }
}
```

#### Integration Tests to Add

```csharp
// tests/ClimaSite.Api.Tests/Controllers/NotificationsControllerTests.cs
public class NotificationsControllerTests
{
    [Fact] public Task GetNotifications_ReturnsUserNotifications() { }
    [Fact] public Task GetNotifications_WithFilters_FiltersCorrectly() { }
    [Fact] public Task GetNotifications_Unauthorized_Returns401() { }
    [Fact] public Task GetNotificationSummary_ReturnsCorrectCounts() { }
    [Fact] public Task MarkAsRead_ValidId_Returns204() { }
    [Fact] public Task MarkAsRead_InvalidId_Returns400() { }
    [Fact] public Task MarkAllAsRead_MarksAllUserNotifications() { }
    [Fact] public Task DeleteNotification_ValidId_Returns204() { }
    [Fact] public Task DeleteNotification_OtherUsersNotification_Returns400() { }
}
```

#### E2E Tests to Add

```csharp
// tests/ClimaSite.E2E/Tests/Notifications/NotificationBellTests.cs
public class NotificationBellTests
{
    [Fact] public Task NotificationBell_ShowsUnreadCount() { }
    [Fact] public Task NotificationBell_ClickOpensDropdown() { }
    [Fact] public Task NotificationDropdown_ShowsRecentNotifications() { }
    [Fact] public Task NotificationDropdown_ClickNotification_NavigatesToLink() { }
    [Fact] public Task NotificationDropdown_MarkAllRead_ClearsBadge() { }
}

// tests/ClimaSite.E2E/Tests/Notifications/NotificationCenterTests.cs
public class NotificationCenterTests
{
    [Fact] public Task NotificationCenter_ShowsAllNotifications() { }
    [Fact] public Task NotificationCenter_FilterByType_FiltersCorrectly() { }
    [Fact] public Task NotificationCenter_DeleteNotification_RemovesFromList() { }
    [Fact] public Task NotificationCenter_EmptyState_ShowsMessage() { }
}
```

---

## 7. Evidence & Notes

### Current Email Service Implementation

From `src/ClimaSite.Infrastructure/Services/EmailService.cs`:

```csharp
public async Task SendEmailAsync(string to, string subject, string body, CancellationToken cancellationToken = default)
{
    // TODO: Implement actual email sending using SMTP or a service like SendGrid
    _logger.LogWarning("Email service not configured - email to {To} with subject '{Subject}' was NOT sent. Configure SMTP settings in production.", to, subject);

    // Placeholder - in production, implement with actual email service
    await Task.CompletedTask;
}
```

**Analysis:** This is a complete placeholder implementation. All email methods log warnings but perform no actual email delivery.

### ForgotPassword Email Integration

From `src/ClimaSite.Application/Auth/Handlers/ForgotPasswordCommandHandler.cs`:

```csharp
// TODO: Send email with reset link
_logger.LogInformation("Password reset token generated for user: {UserId}. Token: {Token}", user.Id, token);

// In production, send email instead of logging token
// await _emailService.SendPasswordResetEmailAsync(user.Email!, token);
```

**Analysis:** The email service is not injected, and the call is commented out. Password reset tokens are logged (security risk in production).

### Notification Entity Validation

The `Notification` entity has proper validation:
- Type: cannot be empty
- Title: cannot be empty, max 200 characters
- Message: cannot be empty, max 1000 characters
- Link: optional, max 500 characters
- Data: optional dictionary, stored as JSONB

### Database Indexes

From `NotificationConfiguration.cs`:
```csharp
builder.HasIndex(n => n.UserId);
builder.HasIndex(n => new { n.UserId, n.IsRead });
builder.HasIndex(n => n.CreatedAt);
builder.HasIndex(n => n.Type);
```

**Analysis:** Good indexing for common query patterns.

### Missing Plan Implementation

From `docs/plans/12-notifications-system.md`:
- **NOT-001** (DB Schema) - Partially complete (Notification entity exists, but preferences/logs missing)
- **NOT-002** (Email Service) - Placeholder only
- **NOT-003** (Template Engine) - Not implemented
- **NOT-004** (Notification Service) - Partial (basic CRUD, no event integration)
- **NOT-005** (Background Processing) - Not implemented
- **NOT-006 to NOT-008** (Event Handlers) - Not implemented
- **NOT-009** (API Controller) - Implemented
- **NOT-010** (Preferences API) - Not implemented
- **NOT-011** (Admin API) - Not implemented
- **NOT-012 to NOT-016** (Frontend) - Not implemented
- **NOT-017** (Unsubscribe) - Not implemented
- **NOT-018-019** (Admin UI) - Not implemented
- **NOT-020** (Integration Tests) - Not implemented

### Configuration Requirements

Expected in `appsettings.json`:
```json
{
  "Email": {
    "Provider": "SendGrid",
    "FromAddress": "noreply@climasite.com",
    "FromName": "ClimaSite",
    "Smtp": {
      "Host": "smtp.example.com",
      "Port": 587,
      "Username": "",
      "Password": "",
      "EnableSsl": true
    },
    "SendGrid": {
      "ApiKey": ""
    }
  }
}
```

**Note:** This configuration structure is defined in the plan but not implemented in code.

### Test Data Factory Integration

The `TestDataFactory` does not have notification creation methods. Recommend adding:

```typescript
async createNotificationAsync(userId: string, options?: {
  type?: string;
  title?: string;
  message?: string;
  link?: string;
  isRead?: boolean;
}): Promise<TestNotification>
```
