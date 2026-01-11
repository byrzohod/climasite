# Notifications System Plan

## 1. Overview

The Notifications System provides comprehensive email and in-app notification capabilities for the ClimaSite HVAC e-commerce platform. This system ensures customers stay informed about their orders, account activities, and promotional offers while respecting their communication preferences.

### Goals
- Deliver timely order status updates via email and in-app notifications
- Provide a notification center for users to view all alerts
- Allow users to manage their notification preferences
- Support admin notifications for inventory and system alerts
- Maintain comprehensive logging for audit and troubleshooting

### Architecture Overview
```
┌─────────────────────────────────────────────────────────────────┐
│                      Event Sources                               │
│  (Order Service, Payment Service, Shipping Service, etc.)       │
└─────────────────────┬───────────────────────────────────────────┘
                      │
                      ▼
┌─────────────────────────────────────────────────────────────────┐
│                  Notification Service                            │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────────────┐  │
│  │ Event        │  │ Preference   │  │ Template             │  │
│  │ Handlers     │──│ Checker      │──│ Renderer             │  │
│  └──────────────┘  └──────────────┘  └──────────────────────┘  │
└─────────────────────┬───────────────────────────────────────────┘
                      │
          ┌───────────┼───────────┐
          ▼           ▼           ▼
┌─────────────┐ ┌───────────┐ ┌─────────────┐
│ In-App      │ │ Email     │ │ Email       │
│ Notification│ │ Queue     │ │ Log         │
│ (DB)        │ │ (Background)│ │ (DB)       │
└─────────────┘ └─────┬─────┘ └─────────────┘
                      │
                      ▼
              ┌───────────────┐
              │ SMTP/SendGrid │
              └───────────────┘
```

---

## 2. Database Schema

### 2.1 Notifications Table
```sql
CREATE TABLE notifications (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,
    type VARCHAR(50) NOT NULL,
    title VARCHAR(200) NOT NULL,
    message TEXT NOT NULL,
    link VARCHAR(500),
    metadata JSONB,
    is_read BOOLEAN NOT NULL DEFAULT FALSE,
    read_at TIMESTAMP WITH TIME ZONE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_notification_type CHECK (type IN (
        'order_confirmed', 'order_paid', 'order_shipped',
        'order_delivered', 'order_cancelled', 'welcome',
        'password_reset', 'low_stock', 'review_approved',
        'price_drop', 'back_in_stock', 'promotion'
    ))
);

CREATE INDEX idx_notifications_user_id ON notifications(user_id);
CREATE INDEX idx_notifications_user_unread ON notifications(user_id, is_read) WHERE is_read = FALSE;
CREATE INDEX idx_notifications_created_at ON notifications(created_at DESC);
CREATE INDEX idx_notifications_type ON notifications(type);
```

### 2.2 Notification Preferences Table
```sql
CREATE TABLE notification_preferences (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID NOT NULL REFERENCES users(id) ON DELETE CASCADE,

    -- Email preferences
    email_order_updates BOOLEAN NOT NULL DEFAULT TRUE,
    email_shipping_updates BOOLEAN NOT NULL DEFAULT TRUE,
    email_promotions BOOLEAN NOT NULL DEFAULT FALSE,
    email_newsletter BOOLEAN NOT NULL DEFAULT FALSE,
    email_price_drops BOOLEAN NOT NULL DEFAULT FALSE,
    email_back_in_stock BOOLEAN NOT NULL DEFAULT FALSE,

    -- In-app preferences
    in_app_enabled BOOLEAN NOT NULL DEFAULT TRUE,
    in_app_order_updates BOOLEAN NOT NULL DEFAULT TRUE,
    in_app_promotions BOOLEAN NOT NULL DEFAULT TRUE,

    -- Global settings
    email_frequency VARCHAR(20) NOT NULL DEFAULT 'immediate',
    quiet_hours_enabled BOOLEAN NOT NULL DEFAULT FALSE,
    quiet_hours_start TIME,
    quiet_hours_end TIME,

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT uq_notification_preferences_user UNIQUE(user_id),
    CONSTRAINT chk_email_frequency CHECK (email_frequency IN ('immediate', 'daily_digest', 'weekly_digest'))
);

CREATE INDEX idx_notification_preferences_user_id ON notification_preferences(user_id);
```

### 2.3 Email Logs Table
```sql
CREATE TABLE email_logs (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    user_id UUID REFERENCES users(id) ON DELETE SET NULL,
    notification_id UUID REFERENCES notifications(id) ON DELETE SET NULL,

    email_type VARCHAR(50) NOT NULL,
    recipient VARCHAR(255) NOT NULL,
    subject VARCHAR(500) NOT NULL,
    body_preview VARCHAR(500),

    status VARCHAR(50) NOT NULL DEFAULT 'pending',
    provider VARCHAR(50),
    provider_message_id VARCHAR(255),

    error_message TEXT,
    retry_count INTEGER NOT NULL DEFAULT 0,
    max_retries INTEGER NOT NULL DEFAULT 3,

    sent_at TIMESTAMP WITH TIME ZONE,
    delivered_at TIMESTAMP WITH TIME ZONE,
    opened_at TIMESTAMP WITH TIME ZONE,
    clicked_at TIMESTAMP WITH TIME ZONE,
    bounced_at TIMESTAMP WITH TIME ZONE,

    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),

    CONSTRAINT chk_email_status CHECK (status IN (
        'pending', 'queued', 'sending', 'sent',
        'delivered', 'opened', 'clicked',
        'failed', 'bounced', 'spam_reported'
    ))
);

CREATE INDEX idx_email_logs_user_id ON email_logs(user_id);
CREATE INDEX idx_email_logs_status ON email_logs(status);
CREATE INDEX idx_email_logs_created_at ON email_logs(created_at DESC);
CREATE INDEX idx_email_logs_email_type ON email_logs(email_type);
CREATE INDEX idx_email_logs_recipient ON email_logs(recipient);
```

### 2.4 Email Templates Table (for admin-editable templates)
```sql
CREATE TABLE email_templates (
    id UUID PRIMARY KEY DEFAULT gen_random_uuid(),
    name VARCHAR(100) NOT NULL UNIQUE,
    subject VARCHAR(500) NOT NULL,
    html_body TEXT NOT NULL,
    text_body TEXT,
    variables JSONB NOT NULL DEFAULT '[]',
    is_active BOOLEAN NOT NULL DEFAULT TRUE,
    created_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW(),
    updated_at TIMESTAMP WITH TIME ZONE NOT NULL DEFAULT NOW()
);

CREATE INDEX idx_email_templates_name ON email_templates(name);
```

---

## 3. Notification Types

| Type | Email | In-App | Recipient | Description | Priority |
|------|-------|--------|-----------|-------------|----------|
| `order_confirmed` | Yes | Yes | Customer | Order placed successfully | High |
| `order_paid` | Yes | Yes | Customer | Payment received confirmation | High |
| `order_processing` | No | Yes | Customer | Order being prepared | Medium |
| `order_shipped` | Yes | Yes | Customer | Order shipped with tracking | High |
| `order_out_for_delivery` | Yes | Yes | Customer | Order out for delivery | High |
| `order_delivered` | Yes | Yes | Customer | Order delivered | High |
| `order_cancelled` | Yes | Yes | Customer | Order was cancelled | High |
| `order_refunded` | Yes | Yes | Customer | Refund processed | High |
| `password_reset` | Yes | No | Customer | Password reset link | Critical |
| `password_changed` | Yes | No | Customer | Password was changed | Critical |
| `welcome` | Yes | Yes | Customer | New user welcome | Medium |
| `email_verified` | Yes | No | Customer | Email verification success | Medium |
| `low_stock` | No | Yes | Admin | Product low stock alert | High |
| `out_of_stock` | Yes | Yes | Admin | Product out of stock | Critical |
| `review_submitted` | No | Yes | Admin | New review pending approval | Low |
| `review_approved` | No | Yes | Customer | Your review was approved | Low |
| `price_drop` | Yes | Yes | Customer | Wishlist item price dropped | Medium |
| `back_in_stock` | Yes | Yes | Customer | Wishlist item back in stock | Medium |
| `promotion` | Yes | Yes | Customer | Promotional offer | Low |
| `abandoned_cart` | Yes | No | Customer | Cart reminder | Low |

---

## 4. API Endpoints

### 4.1 Notifications API

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/notifications` | Get paginated notifications | User |
| GET | `/api/v1/notifications/unread-count` | Get unread count | User |
| GET | `/api/v1/notifications/{id}` | Get notification details | User |
| PUT | `/api/v1/notifications/{id}/read` | Mark as read | User |
| PUT | `/api/v1/notifications/read-all` | Mark all as read | User |
| DELETE | `/api/v1/notifications/{id}` | Delete notification | User |
| DELETE | `/api/v1/notifications` | Delete all read notifications | User |

### 4.2 Notification Preferences API

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/users/me/notification-preferences` | Get preferences | User |
| PUT | `/api/v1/users/me/notification-preferences` | Update preferences | User |
| POST | `/api/v1/users/me/notification-preferences/reset` | Reset to defaults | User |

### 4.3 Admin Notifications API

| Method | Endpoint | Description | Auth |
|--------|----------|-------------|------|
| GET | `/api/v1/admin/notifications` | Get all notifications | Admin |
| POST | `/api/v1/admin/notifications/broadcast` | Send to all users | Admin |
| GET | `/api/v1/admin/email-logs` | Get email logs | Admin |
| GET | `/api/v1/admin/email-templates` | Get email templates | Admin |
| PUT | `/api/v1/admin/email-templates/{id}` | Update template | Admin |

### 4.4 Request/Response Models

#### Get Notifications Request
```
GET /api/v1/notifications?page=1&pageSize=20&unreadOnly=false&type=order_confirmed
```

#### Get Notifications Response
```json
{
  "items": [
    {
      "id": "550e8400-e29b-41d4-a716-446655440000",
      "type": "order_confirmed",
      "title": "Order Confirmed",
      "message": "Your order #ORD-2024-001234 has been confirmed.",
      "link": "/orders/ORD-2024-001234",
      "metadata": {
        "orderId": "550e8400-e29b-41d4-a716-446655440001",
        "orderNumber": "ORD-2024-001234"
      },
      "isRead": false,
      "createdAt": "2024-01-15T10:30:00Z"
    }
  ],
  "totalCount": 45,
  "page": 1,
  "pageSize": 20,
  "unreadCount": 3
}
```

#### Notification Preferences Response
```json
{
  "emailOrderUpdates": true,
  "emailShippingUpdates": true,
  "emailPromotions": false,
  "emailNewsletter": false,
  "emailPriceDrops": false,
  "emailBackInStock": false,
  "inAppEnabled": true,
  "inAppOrderUpdates": true,
  "inAppPromotions": true,
  "emailFrequency": "immediate",
  "quietHoursEnabled": false,
  "quietHoursStart": null,
  "quietHoursEnd": null
}
```

#### Update Notification Preferences Request
```json
{
  "emailOrderUpdates": true,
  "emailShippingUpdates": true,
  "emailPromotions": true,
  "emailNewsletter": false,
  "emailPriceDrops": true,
  "emailBackInStock": true,
  "inAppEnabled": true,
  "inAppOrderUpdates": true,
  "inAppPromotions": true,
  "emailFrequency": "immediate",
  "quietHoursEnabled": true,
  "quietHoursStart": "22:00",
  "quietHoursEnd": "08:00"
}
```

---

## 5. Tasks

### Task NOT-001: Database Schema and Migrations
**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** None

**Description:**
Create EF Core entities and migrations for the notifications system database schema.

**Acceptance Criteria:**
- [ ] Create `Notification` entity with all fields
- [ ] Create `NotificationPreference` entity with all fields
- [ ] Create `EmailLog` entity with all fields
- [ ] Create `EmailTemplate` entity with all fields
- [ ] Create EF Core migration for all tables
- [ ] Configure proper indexes in migration
- [ ] Add seed data for default email templates
- [ ] Add enum for notification types
- [ ] Add enum for email status
- [ ] Unit tests for entity validation

**Files to Create/Modify:**
- `src/ClimaSite.Core/Entities/Notification.cs`
- `src/ClimaSite.Core/Entities/NotificationPreference.cs`
- `src/ClimaSite.Core/Entities/EmailLog.cs`
- `src/ClimaSite.Core/Entities/EmailTemplate.cs`
- `src/ClimaSite.Core/Enums/NotificationType.cs`
- `src/ClimaSite.Core/Enums/EmailStatus.cs`
- `src/ClimaSite.Infrastructure/Data/Configurations/NotificationConfiguration.cs`
- `src/ClimaSite.Infrastructure/Data/Migrations/XXXXXX_AddNotificationsSystem.cs`

---

### Task NOT-002: Email Service Infrastructure
**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** NOT-001

**Description:**
Implement the email sending infrastructure with support for both SMTP and SendGrid providers.

**Acceptance Criteria:**
- [ ] Create `IEmailService` interface
- [ ] Implement `SmtpEmailService` for SMTP
- [ ] Implement `SendGridEmailService` for SendGrid
- [ ] Create email service factory for provider selection
- [ ] Implement retry logic with exponential backoff
- [ ] Log all email attempts to `email_logs` table
- [ ] Support HTML and plain text emails
- [ ] Handle email bounces and failures
- [ ] Configuration via appsettings.json
- [ ] Unit tests for email services

**Configuration:**
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
    },
    "RetryAttempts": 3,
    "RetryDelaySeconds": 30
  }
}
```

**Files to Create/Modify:**
- `src/ClimaSite.Core/Interfaces/IEmailService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/SmtpEmailService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/SendGridEmailService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/EmailServiceFactory.cs`
- `src/ClimaSite.Core/Models/Email/EmailMessage.cs`
- `src/ClimaSite.Core/Models/Email/EmailResult.cs`

---

### Task NOT-003: Email Template Engine
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-001

**Description:**
Implement a template rendering engine for dynamic email content generation.

**Acceptance Criteria:**
- [ ] Create `IEmailTemplateService` interface
- [ ] Implement template rendering with variable substitution
- [ ] Support Handlebars-style syntax ({{variable}})
- [ ] Support conditional blocks ({{#if}}, {{#each}})
- [ ] Load templates from database or file system
- [ ] Cache compiled templates for performance
- [ ] Generate plain text version from HTML
- [ ] Validate template variables before rendering
- [ ] Unit tests for template rendering

**Files to Create/Modify:**
- `src/ClimaSite.Core/Interfaces/IEmailTemplateService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/EmailTemplateService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/TemplateCache.cs`

---

### Task NOT-004: Notification Service Core
**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** NOT-001, NOT-002, NOT-003

**Description:**
Implement the core notification service that handles creating notifications and dispatching emails.

**Acceptance Criteria:**
- [ ] Create `INotificationService` interface
- [ ] Implement `NotificationService`
- [ ] Create in-app notification in database
- [ ] Check user preferences before sending
- [ ] Queue emails for background processing
- [ ] Support notification batching for digests
- [ ] Respect quiet hours settings
- [ ] Unit tests for notification service

**Files to Create/Modify:**
- `src/ClimaSite.Core/Interfaces/INotificationService.cs`
- `src/ClimaSite.Infrastructure/Services/NotificationService.cs`
- `src/ClimaSite.Core/Models/Notifications/CreateNotificationRequest.cs`

---

### Task NOT-005: Background Email Processing
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-002, NOT-004

**Description:**
Implement background job processing for sending emails asynchronously.

**Acceptance Criteria:**
- [ ] Create background service for email processing
- [ ] Process email queue in batches
- [ ] Handle failed emails with retry logic
- [ ] Support scheduled emails (for digests)
- [ ] Implement dead letter queue for permanent failures
- [ ] Log processing metrics
- [ ] Unit tests for background processor

**Files to Create/Modify:**
- `src/ClimaSite.Infrastructure/BackgroundServices/EmailProcessingService.cs`
- `src/ClimaSite.Infrastructure/Services/Email/EmailQueue.cs`
- `src/ClimaSite.Core/Interfaces/IEmailQueue.cs`

---

### Task NOT-006: Order Event Handlers
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-004

**Description:**
Implement event handlers that create notifications when order events occur.

**Acceptance Criteria:**
- [ ] Handle `OrderCreatedEvent` -> order_confirmed notification
- [ ] Handle `OrderPaidEvent` -> order_paid notification
- [ ] Handle `OrderShippedEvent` -> order_shipped notification
- [ ] Handle `OrderDeliveredEvent` -> order_delivered notification
- [ ] Handle `OrderCancelledEvent` -> order_cancelled notification
- [ ] Handle `OrderRefundedEvent` -> order_refunded notification
- [ ] Include relevant order data in notifications
- [ ] Unit tests for each event handler

**Files to Create/Modify:**
- `src/ClimaSite.Infrastructure/EventHandlers/OrderCreatedNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OrderPaidNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OrderShippedNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OrderDeliveredNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OrderCancelledNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OrderRefundedNotificationHandler.cs`

---

### Task NOT-007: User Event Handlers
**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** NOT-004

**Description:**
Implement event handlers for user-related notifications.

**Acceptance Criteria:**
- [ ] Handle `UserRegisteredEvent` -> welcome notification
- [ ] Handle `PasswordResetRequestedEvent` -> password_reset email
- [ ] Handle `PasswordChangedEvent` -> password_changed email
- [ ] Handle `EmailVerifiedEvent` -> email_verified notification
- [ ] Unit tests for each event handler

**Files to Create/Modify:**
- `src/ClimaSite.Infrastructure/EventHandlers/UserRegisteredNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/PasswordResetNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/PasswordChangedNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/EmailVerifiedNotificationHandler.cs`

---

### Task NOT-008: Inventory Event Handlers
**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** NOT-004

**Description:**
Implement event handlers for inventory-related notifications.

**Acceptance Criteria:**
- [ ] Handle `LowStockEvent` -> low_stock notification (admin)
- [ ] Handle `OutOfStockEvent` -> out_of_stock notification (admin)
- [ ] Handle `BackInStockEvent` -> back_in_stock notification (customers with wishlist)
- [ ] Handle `PriceDropEvent` -> price_drop notification (customers with wishlist)
- [ ] Unit tests for each event handler

**Files to Create/Modify:**
- `src/ClimaSite.Infrastructure/EventHandlers/LowStockNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/OutOfStockNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/BackInStockNotificationHandler.cs`
- `src/ClimaSite.Infrastructure/EventHandlers/PriceDropNotificationHandler.cs`

---

### Task NOT-009: Notifications API Controller
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-004

**Description:**
Implement the notifications REST API endpoints.

**Acceptance Criteria:**
- [ ] GET `/api/v1/notifications` - paginated list with filtering
- [ ] GET `/api/v1/notifications/unread-count` - unread count
- [ ] GET `/api/v1/notifications/{id}` - single notification
- [ ] PUT `/api/v1/notifications/{id}/read` - mark as read
- [ ] PUT `/api/v1/notifications/read-all` - mark all as read
- [ ] DELETE `/api/v1/notifications/{id}` - delete notification
- [ ] DELETE `/api/v1/notifications` - delete all read
- [ ] Input validation with FluentValidation
- [ ] Proper error responses with ProblemDetails
- [ ] OpenAPI documentation
- [ ] Unit tests for controller

**Files to Create/Modify:**
- `src/ClimaSite.Api/Controllers/NotificationsController.cs`
- `src/ClimaSite.Api/Validators/GetNotificationsRequestValidator.cs`
- `src/ClimaSite.Core/Models/Notifications/GetNotificationsRequest.cs`
- `src/ClimaSite.Core/Models/Notifications/NotificationDto.cs`
- `src/ClimaSite.Core/Models/Notifications/NotificationsListResponse.cs`

---

### Task NOT-010: Notification Preferences API
**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** NOT-001

**Description:**
Implement the notification preferences API endpoints.

**Acceptance Criteria:**
- [ ] GET `/api/v1/users/me/notification-preferences` - get preferences
- [ ] PUT `/api/v1/users/me/notification-preferences` - update preferences
- [ ] POST `/api/v1/users/me/notification-preferences/reset` - reset to defaults
- [ ] Create default preferences on user registration
- [ ] Input validation with FluentValidation
- [ ] Unit tests for controller

**Files to Create/Modify:**
- `src/ClimaSite.Api/Controllers/NotificationPreferencesController.cs`
- `src/ClimaSite.Api/Validators/UpdateNotificationPreferencesValidator.cs`
- `src/ClimaSite.Core/Models/Notifications/NotificationPreferencesDto.cs`
- `src/ClimaSite.Core/Models/Notifications/UpdateNotificationPreferencesRequest.cs`

---

### Task NOT-011: Admin Notifications API
**Priority:** Medium
**Estimated Time:** 6 hours
**Dependencies:** NOT-004, NOT-009

**Description:**
Implement admin-specific notification management endpoints.

**Acceptance Criteria:**
- [ ] GET `/api/v1/admin/notifications` - all notifications with filters
- [ ] POST `/api/v1/admin/notifications/broadcast` - send to all/segment
- [ ] GET `/api/v1/admin/email-logs` - email sending logs
- [ ] GET `/api/v1/admin/email-logs/{id}` - email log details
- [ ] GET `/api/v1/admin/email-templates` - list templates
- [ ] PUT `/api/v1/admin/email-templates/{id}` - update template
- [ ] POST `/api/v1/admin/email-templates/{id}/preview` - preview template
- [ ] Admin authorization required
- [ ] Unit tests for controller

**Files to Create/Modify:**
- `src/ClimaSite.Api/Controllers/Admin/AdminNotificationsController.cs`
- `src/ClimaSite.Api/Controllers/Admin/AdminEmailLogsController.cs`
- `src/ClimaSite.Api/Controllers/Admin/AdminEmailTemplatesController.cs`

---

### Task NOT-012: Angular Notification Service
**Priority:** High
**Estimated Time:** 4 hours
**Dependencies:** NOT-009, NOT-010

**Description:**
Create Angular service for notification operations.

**Acceptance Criteria:**
- [ ] Create `NotificationService` with all API methods
- [ ] Implement reactive state management with signals
- [ ] Auto-refresh unread count periodically
- [ ] Cache notifications locally
- [ ] Handle real-time updates preparation (WebSocket ready)
- [ ] Unit tests for service

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/core/services/notification.service.ts`
- `src/ClimaSite.Web/src/app/core/models/notification.model.ts`
- `src/ClimaSite.Web/src/app/core/models/notification-preferences.model.ts`

---

### Task NOT-013: Notification Bell Component
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-012

**Description:**
Create the notification bell icon component for the header.

**Acceptance Criteria:**
- [ ] Bell icon with unread count badge
- [ ] Badge shows count (max 99+)
- [ ] Animate badge on new notifications
- [ ] Click opens notification dropdown
- [ ] Auto-refresh count every 30 seconds
- [ ] Responsive design
- [ ] Accessibility (ARIA labels)
- [ ] Unit tests for component

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/shared/components/notification-bell/notification-bell.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/notification-bell/notification-bell.component.html`
- `src/ClimaSite.Web/src/app/shared/components/notification-bell/notification-bell.component.scss`
- `src/ClimaSite.Web/src/app/shared/components/notification-bell/notification-bell.component.spec.ts`

---

### Task NOT-014: Notification Dropdown Component
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-012, NOT-013

**Description:**
Create the notification dropdown for quick viewing.

**Acceptance Criteria:**
- [ ] Show latest 5 notifications
- [ ] Display notification icon, title, time
- [ ] Click notification marks as read and navigates
- [ ] "Mark all as read" button
- [ ] "View all" link to notification center
- [ ] Empty state when no notifications
- [ ] Loading state
- [ ] Close on outside click
- [ ] Keyboard navigation
- [ ] Unit tests for component

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/shared/components/notification-dropdown/notification-dropdown.component.ts`
- `src/ClimaSite.Web/src/app/shared/components/notification-dropdown/notification-dropdown.component.html`
- `src/ClimaSite.Web/src/app/shared/components/notification-dropdown/notification-dropdown.component.scss`
- `src/ClimaSite.Web/src/app/shared/components/notification-dropdown/notification-dropdown.component.spec.ts`

---

### Task NOT-015: Notification Center Page
**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** NOT-012

**Description:**
Create full notification center page for viewing all notifications.

**Acceptance Criteria:**
- [ ] List all notifications with pagination
- [ ] Filter by type (all, orders, account, promotions)
- [ ] Filter by read/unread status
- [ ] Click to view details and mark as read
- [ ] Bulk actions (mark all read, delete read)
- [ ] Delete individual notifications
- [ ] Relative time display (5 min ago, yesterday)
- [ ] Empty state per filter
- [ ] Infinite scroll or pagination
- [ ] Responsive design
- [ ] Unit tests for component

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/features/account/notifications/notification-center.component.ts`
- `src/ClimaSite.Web/src/app/features/account/notifications/notification-center.component.html`
- `src/ClimaSite.Web/src/app/features/account/notifications/notification-center.component.scss`
- `src/ClimaSite.Web/src/app/features/account/notifications/notification-item.component.ts`

---

### Task NOT-016: Notification Preferences Page
**Priority:** High
**Estimated Time:** 6 hours
**Dependencies:** NOT-012

**Description:**
Create notification preferences management page.

**Acceptance Criteria:**
- [ ] Display all preference options with toggles
- [ ] Group by category (Order updates, Marketing, etc.)
- [ ] Email frequency selector
- [ ] Quiet hours configuration
- [ ] Save changes with confirmation
- [ ] Reset to defaults option
- [ ] Unsaved changes warning
- [ ] Form validation
- [ ] Responsive design
- [ ] Unit tests for component

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/features/account/notification-preferences/notification-preferences.component.ts`
- `src/ClimaSite.Web/src/app/features/account/notification-preferences/notification-preferences.component.html`
- `src/ClimaSite.Web/src/app/features/account/notification-preferences/notification-preferences.component.scss`
- `src/ClimaSite.Web/src/app/features/account/notification-preferences/notification-preferences.component.spec.ts`

---

### Task NOT-017: Email Unsubscribe Page
**Priority:** Medium
**Estimated Time:** 4 hours
**Dependencies:** NOT-010

**Description:**
Create one-click email unsubscribe functionality.

**Acceptance Criteria:**
- [ ] Unsubscribe link in all marketing emails
- [ ] Token-based unsubscribe (no login required)
- [ ] Confirm unsubscription page
- [ ] Option to re-subscribe
- [ ] Option to manage all preferences
- [ ] Backend endpoint for token validation
- [ ] Unit tests

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/features/email/unsubscribe/unsubscribe.component.ts`
- `src/ClimaSite.Api/Controllers/EmailUnsubscribeController.cs`
- `src/ClimaSite.Core/Services/UnsubscribeTokenService.cs`

---

### Task NOT-018: Admin Email Template Editor
**Priority:** Low
**Estimated Time:** 8 hours
**Dependencies:** NOT-011

**Description:**
Create admin interface for managing email templates.

**Acceptance Criteria:**
- [ ] List all email templates
- [ ] Edit template HTML and subject
- [ ] Preview template with sample data
- [ ] Send test email
- [ ] Variable reference documentation
- [ ] Syntax highlighting for HTML
- [ ] Revert to default template
- [ ] Unit tests

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/features/admin/email-templates/email-template-list.component.ts`
- `src/ClimaSite.Web/src/app/features/admin/email-templates/email-template-editor.component.ts`
- `src/ClimaSite.Web/src/app/features/admin/email-templates/email-template-preview.component.ts`

---

### Task NOT-019: Admin Email Logs Viewer
**Priority:** Low
**Estimated Time:** 4 hours
**Dependencies:** NOT-011

**Description:**
Create admin interface for viewing email logs.

**Acceptance Criteria:**
- [ ] List emails with filtering (status, type, date)
- [ ] Search by recipient
- [ ] View email details and status history
- [ ] Resend failed emails
- [ ] Export logs to CSV
- [ ] Statistics dashboard (sent, failed, opened)
- [ ] Unit tests

**Files to Create/Modify:**
- `src/ClimaSite.Web/src/app/features/admin/email-logs/email-logs.component.ts`
- `src/ClimaSite.Web/src/app/features/admin/email-logs/email-log-detail.component.ts`

---

### Task NOT-020: Integration Tests
**Priority:** High
**Estimated Time:** 8 hours
**Dependencies:** NOT-001 through NOT-011

**Description:**
Create comprehensive integration tests for the notifications system.

**Acceptance Criteria:**
- [ ] Test notification creation flow
- [ ] Test email sending with test SMTP server
- [ ] Test preference checking logic
- [ ] Test all API endpoints
- [ ] Test event handlers
- [ ] Test background email processing
- [ ] Test template rendering
- [ ] Database integration tests

**Files to Create/Modify:**
- `tests/ClimaSite.Api.Tests/Integration/NotificationsControllerTests.cs`
- `tests/ClimaSite.Api.Tests/Integration/NotificationPreferencesTests.cs`
- `tests/ClimaSite.Infrastructure.Tests/NotificationServiceTests.cs`
- `tests/ClimaSite.Infrastructure.Tests/EmailServiceTests.cs`

---

## 6. Email Templates

### 6.1 Base Layout Template
```html
<!-- templates/base-layout.html -->
<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8">
  <meta name="viewport" content="width=device-width, initial-scale=1.0">
  <title>{{Subject}}</title>
  <style>
    body {
      font-family: -apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, Oxygen, Ubuntu, sans-serif;
      line-height: 1.6;
      color: #333;
      margin: 0;
      padding: 0;
      background-color: #f5f5f5;
    }
    .container {
      max-width: 600px;
      margin: 0 auto;
      background-color: #ffffff;
    }
    .header {
      background-color: #1976d2;
      padding: 24px;
      text-align: center;
    }
    .header img {
      max-height: 40px;
    }
    .content {
      padding: 32px 24px;
    }
    .footer {
      background-color: #f5f5f5;
      padding: 24px;
      text-align: center;
      font-size: 12px;
      color: #666;
    }
    .button {
      display: inline-block;
      background-color: #1976d2;
      color: #ffffff !important;
      text-decoration: none;
      padding: 12px 32px;
      border-radius: 4px;
      font-weight: 600;
      margin: 16px 0;
    }
    .button:hover {
      background-color: #1565c0;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      padding: 12px;
      text-align: left;
      border-bottom: 1px solid #eee;
    }
    th {
      background-color: #f9f9f9;
      font-weight: 600;
    }
    .order-summary {
      background-color: #f9f9f9;
      padding: 16px;
      border-radius: 4px;
      margin: 16px 0;
    }
    .total-row {
      font-size: 18px;
      font-weight: 700;
      border-top: 2px solid #333;
    }
  </style>
</head>
<body>
  <div class="container">
    <div class="header">
      <img src="{{BaseUrl}}/assets/logo-white.png" alt="ClimaSite" />
    </div>
    <div class="content">
      {{Content}}
    </div>
    <div class="footer">
      <p>ClimaSite - Your HVAC Solutions Partner</p>
      <p>123 Climate Street, Cool City, CC 12345</p>
      <p>
        <a href="{{BaseUrl}}/unsubscribe?token={{UnsubscribeToken}}">Unsubscribe</a> |
        <a href="{{BaseUrl}}/account/notification-preferences">Manage Preferences</a>
      </p>
      <p>&copy; {{Year}} ClimaSite. All rights reserved.</p>
    </div>
  </div>
</body>
</html>
```

### 6.2 Order Confirmation Template
```html
<!-- templates/order-confirmed.html -->
<h1 style="color: #1976d2; margin-bottom: 8px;">Order Confirmed!</h1>
<p>Hi {{FirstName}},</p>
<p>Thank you for your order! We've received your order and are getting it ready.</p>

<div class="order-summary">
  <p><strong>Order Number:</strong> {{OrderNumber}}</p>
  <p><strong>Order Date:</strong> {{OrderDate}}</p>
  <p><strong>Estimated Delivery:</strong> {{EstimatedDelivery}}</p>
</div>

<h2 style="font-size: 18px; margin-top: 24px;">Order Summary</h2>
<table>
  <thead>
    <tr>
      <th>Product</th>
      <th style="text-align: center;">Qty</th>
      <th style="text-align: right;">Price</th>
    </tr>
  </thead>
  <tbody>
    {{#each Items}}
    <tr>
      <td>
        <strong>{{Name}}</strong>
        {{#if Sku}}<br><small style="color: #666;">SKU: {{Sku}}</small>{{/if}}
      </td>
      <td style="text-align: center;">{{Quantity}}</td>
      <td style="text-align: right;">{{FormattedPrice}}</td>
    </tr>
    {{/each}}
  </tbody>
  <tfoot>
    <tr>
      <td colspan="2" style="text-align: right;">Subtotal:</td>
      <td style="text-align: right;">{{FormattedSubtotal}}</td>
    </tr>
    {{#if DiscountAmount}}
    <tr>
      <td colspan="2" style="text-align: right; color: #2e7d32;">Discount:</td>
      <td style="text-align: right; color: #2e7d32;">-{{FormattedDiscount}}</td>
    </tr>
    {{/if}}
    <tr>
      <td colspan="2" style="text-align: right;">Shipping:</td>
      <td style="text-align: right;">{{FormattedShipping}}</td>
    </tr>
    <tr>
      <td colspan="2" style="text-align: right;">Tax:</td>
      <td style="text-align: right;">{{FormattedTax}}</td>
    </tr>
    <tr class="total-row">
      <td colspan="2" style="text-align: right;">Total:</td>
      <td style="text-align: right;">{{FormattedTotal}}</td>
    </tr>
  </tfoot>
</table>

<h2 style="font-size: 18px; margin-top: 24px;">Shipping Address</h2>
<p>
  {{ShippingAddress.FullName}}<br>
  {{ShippingAddress.Street}}<br>
  {{#if ShippingAddress.Street2}}{{ShippingAddress.Street2}}<br>{{/if}}
  {{ShippingAddress.City}}, {{ShippingAddress.State}} {{ShippingAddress.PostalCode}}<br>
  {{ShippingAddress.Country}}
</p>

<div style="text-align: center; margin-top: 32px;">
  <a href="{{TrackOrderUrl}}" class="button">Track Your Order</a>
</div>

<p style="margin-top: 32px;">If you have any questions about your order, please contact our support team at <a href="mailto:support@climasite.com">support@climasite.com</a>.</p>

<p>Thanks for shopping with us!</p>
<p><strong>The ClimaSite Team</strong></p>
```

### 6.3 Order Shipped Template
```html
<!-- templates/order-shipped.html -->
<h1 style="color: #1976d2; margin-bottom: 8px;">Your Order is On Its Way!</h1>
<p>Hi {{FirstName}},</p>
<p>Great news! Your order has been shipped and is on its way to you.</p>

<div class="order-summary">
  <p><strong>Order Number:</strong> {{OrderNumber}}</p>
  <p><strong>Carrier:</strong> {{Carrier}}</p>
  <p><strong>Tracking Number:</strong> {{TrackingNumber}}</p>
  <p><strong>Estimated Delivery:</strong> {{EstimatedDelivery}}</p>
</div>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{TrackingUrl}}" class="button">Track Package</a>
</div>

<h2 style="font-size: 18px; margin-top: 24px;">Items in This Shipment</h2>
<table>
  <thead>
    <tr>
      <th>Product</th>
      <th style="text-align: center;">Qty</th>
    </tr>
  </thead>
  <tbody>
    {{#each Items}}
    <tr>
      <td>{{Name}}</td>
      <td style="text-align: center;">{{Quantity}}</td>
    </tr>
    {{/each}}
  </tbody>
</table>

<h2 style="font-size: 18px; margin-top: 24px;">Shipping To</h2>
<p>
  {{ShippingAddress.FullName}}<br>
  {{ShippingAddress.Street}}<br>
  {{ShippingAddress.City}}, {{ShippingAddress.State}} {{ShippingAddress.PostalCode}}
</p>

<p style="margin-top: 32px;">Thanks for shopping with ClimaSite!</p>
<p><strong>The ClimaSite Team</strong></p>
```

### 6.4 Order Delivered Template
```html
<!-- templates/order-delivered.html -->
<h1 style="color: #2e7d32; margin-bottom: 8px;">Your Order Has Been Delivered!</h1>
<p>Hi {{FirstName}},</p>
<p>Your order has been delivered. We hope you love your new HVAC equipment!</p>

<div class="order-summary">
  <p><strong>Order Number:</strong> {{OrderNumber}}</p>
  <p><strong>Delivered On:</strong> {{DeliveredDate}}</p>
</div>

<h2 style="font-size: 18px; margin-top: 24px;">What's Next?</h2>
<ul>
  <li><strong>Installation:</strong> Need help with installation? Check our <a href="{{BaseUrl}}/installation-guides">installation guides</a> or find a <a href="{{BaseUrl}}/find-installer">certified installer</a> near you.</li>
  <li><strong>Register Your Product:</strong> <a href="{{BaseUrl}}/register-product">Register your product</a> to activate your warranty.</li>
  <li><strong>Leave a Review:</strong> Share your experience and help other customers.</li>
</ul>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{ReviewUrl}}" class="button">Write a Review</a>
</div>

<p>If there are any issues with your order, please contact us within 30 days for assistance.</p>

<p style="margin-top: 32px;">Thank you for choosing ClimaSite!</p>
<p><strong>The ClimaSite Team</strong></p>
```

### 6.5 Password Reset Template
```html
<!-- templates/password-reset.html -->
<h1 style="color: #1976d2; margin-bottom: 8px;">Reset Your Password</h1>
<p>Hi {{FirstName}},</p>
<p>We received a request to reset your password for your ClimaSite account. Click the button below to create a new password:</p>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{ResetUrl}}" class="button">Reset Password</a>
</div>

<p style="color: #666; font-size: 14px;">This link will expire in {{ExpirationHours}} hours.</p>

<p>If you didn't request a password reset, you can safely ignore this email. Your password will remain unchanged.</p>

<p style="margin-top: 24px; padding: 16px; background-color: #fff3e0; border-radius: 4px;">
  <strong>Security Tip:</strong> Never share your password with anyone. ClimaSite will never ask for your password via email.
</p>

<p style="margin-top: 32px;">Best regards,</p>
<p><strong>The ClimaSite Team</strong></p>
```

### 6.6 Welcome Email Template
```html
<!-- templates/welcome.html -->
<h1 style="color: #1976d2; margin-bottom: 8px;">Welcome to ClimaSite!</h1>
<p>Hi {{FirstName}},</p>
<p>Thank you for creating an account with ClimaSite. We're excited to have you as part of our community!</p>

<h2 style="font-size: 18px; margin-top: 24px;">Get Started</h2>
<ul>
  <li><strong>Browse Products:</strong> Explore our wide range of <a href="{{BaseUrl}}/products/air-conditioners">air conditioners</a>, <a href="{{BaseUrl}}/products/heating">heating systems</a>, and <a href="{{BaseUrl}}/products/accessories">accessories</a>.</li>
  <li><strong>Create a Wishlist:</strong> Save items you love for later.</li>
  <li><strong>Set Your Preferences:</strong> <a href="{{BaseUrl}}/account/notification-preferences">Customize your notification preferences</a>.</li>
</ul>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{BaseUrl}}/products" class="button">Start Shopping</a>
</div>

<h2 style="font-size: 18px; margin-top: 24px;">Why Choose ClimaSite?</h2>
<table>
  <tr>
    <td style="text-align: center; padding: 16px;">
      <strong>Expert Support</strong><br>
      <span style="color: #666;">HVAC specialists available 24/7</span>
    </td>
    <td style="text-align: center; padding: 16px;">
      <strong>Free Shipping</strong><br>
      <span style="color: #666;">On orders over $99</span>
    </td>
    <td style="text-align: center; padding: 16px;">
      <strong>Easy Returns</strong><br>
      <span style="color: #666;">30-day hassle-free returns</span>
    </td>
  </tr>
</table>

<p style="margin-top: 32px;">Questions? Our team is here to help at <a href="mailto:support@climasite.com">support@climasite.com</a>.</p>

<p>Welcome aboard!</p>
<p><strong>The ClimaSite Team</strong></p>
```

### 6.7 Low Stock Alert Template (Admin)
```html
<!-- templates/admin-low-stock.html -->
<h1 style="color: #f57c00; margin-bottom: 8px;">Low Stock Alert</h1>
<p>The following products are running low on inventory:</p>

<table>
  <thead>
    <tr>
      <th>Product</th>
      <th>SKU</th>
      <th style="text-align: center;">Current Stock</th>
      <th style="text-align: center;">Threshold</th>
    </tr>
  </thead>
  <tbody>
    {{#each Products}}
    <tr>
      <td><a href="{{EditUrl}}">{{Name}}</a></td>
      <td>{{Sku}}</td>
      <td style="text-align: center; color: {{#if IsCritical}}#d32f2f{{else}}#f57c00{{/if}}; font-weight: bold;">
        {{CurrentStock}}
      </td>
      <td style="text-align: center;">{{Threshold}}</td>
    </tr>
    {{/each}}
  </tbody>
</table>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{InventoryUrl}}" class="button">Manage Inventory</a>
</div>

<p style="font-size: 12px; color: #666;">
  This is an automated alert. You can configure alert thresholds in
  <a href="{{SettingsUrl}}">Inventory Settings</a>.
</p>
```

### 6.8 Price Drop Alert Template
```html
<!-- templates/price-drop.html -->
<h1 style="color: #2e7d32; margin-bottom: 8px;">Price Drop Alert!</h1>
<p>Hi {{FirstName}},</p>
<p>Great news! An item on your wishlist just dropped in price.</p>

<div style="background-color: #f5f5f5; padding: 24px; border-radius: 4px; margin: 24px 0; text-align: center;">
  <img src="{{Product.ImageUrl}}" alt="{{Product.Name}}" style="max-width: 200px; margin-bottom: 16px;" />
  <h2 style="margin: 0 0 8px 0;">{{Product.Name}}</h2>
  <p style="margin: 0;">
    <span style="text-decoration: line-through; color: #666;">{{Product.OldPrice}}</span>
    <span style="color: #2e7d32; font-size: 24px; font-weight: bold; margin-left: 8px;">{{Product.NewPrice}}</span>
  </p>
  <p style="color: #2e7d32; font-weight: bold;">You save {{Product.Savings}} ({{Product.DiscountPercent}}% off)</p>
</div>

<div style="text-align: center; margin: 32px 0;">
  <a href="{{Product.Url}}" class="button">Buy Now</a>
</div>

<p style="font-size: 12px; color: #666;">
  Prices and availability are subject to change. Don't miss out!
</p>

<p style="margin-top: 32px;">Happy shopping!</p>
<p><strong>The ClimaSite Team</strong></p>
```

---

## 7. Frontend Components

### 7.1 Component Architecture

```
src/app/
├── core/
│   ├── services/
│   │   └── notification.service.ts
│   └── models/
│       ├── notification.model.ts
│       └── notification-preferences.model.ts
├── shared/
│   └── components/
│       ├── notification-bell/
│       │   ├── notification-bell.component.ts
│       │   ├── notification-bell.component.html
│       │   └── notification-bell.component.scss
│       └── notification-dropdown/
│           ├── notification-dropdown.component.ts
│           ├── notification-dropdown.component.html
│           └── notification-dropdown.component.scss
└── features/
    └── account/
        ├── notifications/
        │   ├── notification-center.component.ts
        │   ├── notification-center.component.html
        │   ├── notification-center.component.scss
        │   └── notification-item.component.ts
        └── notification-preferences/
            ├── notification-preferences.component.ts
            ├── notification-preferences.component.html
            └── notification-preferences.component.scss
```

### 7.2 Notification Service Implementation

```typescript
// notification.service.ts
import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, interval, switchMap, startWith } from 'rxjs';
import { Notification, NotificationsResponse, NotificationPreferences } from '../models/notification.model';

@Injectable({ providedIn: 'root' })
export class NotificationService {
  private readonly apiUrl = '/api/v1';

  // Reactive state with signals
  private notificationsState = signal<Notification[]>([]);
  private unreadCountState = signal<number>(0);
  private loadingState = signal<boolean>(false);

  // Public computed signals
  readonly notifications = this.notificationsState.asReadonly();
  readonly unreadCount = this.unreadCountState.asReadonly();
  readonly loading = this.loadingState.asReadonly();
  readonly hasUnread = computed(() => this.unreadCountState() > 0);

  constructor(private http: HttpClient) {
    this.startPolling();
  }

  private startPolling(): void {
    // Poll for unread count every 30 seconds
    interval(30000).pipe(
      startWith(0),
      switchMap(() => this.fetchUnreadCount())
    ).subscribe(count => this.unreadCountState.set(count));
  }

  private fetchUnreadCount(): Observable<number> {
    return this.http.get<{ count: number }>(`${this.apiUrl}/notifications/unread-count`)
      .pipe(map(res => res.count));
  }

  getNotifications(params: {
    page?: number;
    pageSize?: number;
    unreadOnly?: boolean;
    type?: string;
  } = {}): Observable<NotificationsResponse> {
    return this.http.get<NotificationsResponse>(`${this.apiUrl}/notifications`, { params });
  }

  markAsRead(id: string): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/notifications/${id}/read`, {});
  }

  markAllAsRead(): Observable<void> {
    return this.http.put<void>(`${this.apiUrl}/notifications/read-all`, {});
  }

  deleteNotification(id: string): Observable<void> {
    return this.http.delete<void>(`${this.apiUrl}/notifications/${id}`);
  }

  getPreferences(): Observable<NotificationPreferences> {
    return this.http.get<NotificationPreferences>(`${this.apiUrl}/users/me/notification-preferences`);
  }

  updatePreferences(preferences: Partial<NotificationPreferences>): Observable<NotificationPreferences> {
    return this.http.put<NotificationPreferences>(
      `${this.apiUrl}/users/me/notification-preferences`,
      preferences
    );
  }

  refreshUnreadCount(): void {
    this.fetchUnreadCount().subscribe(count => this.unreadCountState.set(count));
  }
}
```

### 7.3 Notification Bell Component

```typescript
// notification-bell.component.ts
import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { NotificationService } from '@core/services/notification.service';
import { NotificationDropdownComponent } from '../notification-dropdown/notification-dropdown.component';

@Component({
  selector: 'app-notification-bell',
  standalone: true,
  imports: [CommonModule, NotificationDropdownComponent],
  templateUrl: './notification-bell.component.html',
  styleUrl: './notification-bell.component.scss'
})
export class NotificationBellComponent {
  private notificationService = inject(NotificationService);

  unreadCount = this.notificationService.unreadCount;
  hasUnread = this.notificationService.hasUnread;

  isDropdownOpen = false;

  toggleDropdown(): void {
    this.isDropdownOpen = !this.isDropdownOpen;
  }

  closeDropdown(): void {
    this.isDropdownOpen = false;
  }

  get displayCount(): string {
    const count = this.unreadCount();
    return count > 99 ? '99+' : count.toString();
  }
}
```

```html
<!-- notification-bell.component.html -->
<div class="notification-bell" (clickOutside)="closeDropdown()">
  <button
    class="bell-button"
    (click)="toggleDropdown()"
    [attr.aria-label]="'Notifications' + (hasUnread() ? ', ' + unreadCount() + ' unread' : '')"
    aria-haspopup="true"
    [attr.aria-expanded]="isDropdownOpen"
    data-testid="notification-bell">
    <svg class="bell-icon" viewBox="0 0 24 24" fill="currentColor">
      <path d="M12 22c1.1 0 2-.9 2-2h-4c0 1.1.9 2 2 2zm6-6v-5c0-3.07-1.63-5.64-4.5-6.32V4c0-.83-.67-1.5-1.5-1.5s-1.5.67-1.5 1.5v.68C7.64 5.36 6 7.92 6 11v5l-2 2v1h16v-1l-2-2zm-2 1H8v-6c0-2.48 1.51-4.5 4-4.5s4 2.02 4 4.5v6z"/>
    </svg>
    @if (hasUnread()) {
      <span
        class="badge"
        [class.pulse]="hasUnread()"
        data-testid="notification-badge">
        {{ displayCount }}
      </span>
    }
  </button>

  @if (isDropdownOpen) {
    <app-notification-dropdown
      (close)="closeDropdown()"
      (markAllRead)="notificationService.markAllAsRead().subscribe()">
    </app-notification-dropdown>
  }
</div>
```

```scss
// notification-bell.component.scss
.notification-bell {
  position: relative;
}

.bell-button {
  position: relative;
  background: none;
  border: none;
  cursor: pointer;
  padding: 8px;
  border-radius: 50%;
  transition: background-color 0.2s;

  &:hover {
    background-color: rgba(0, 0, 0, 0.05);
  }

  &:focus {
    outline: 2px solid var(--primary-color);
    outline-offset: 2px;
  }
}

.bell-icon {
  width: 24px;
  height: 24px;
  color: var(--text-secondary);
}

.badge {
  position: absolute;
  top: 2px;
  right: 2px;
  min-width: 18px;
  height: 18px;
  padding: 0 4px;
  background-color: var(--error-color);
  color: white;
  font-size: 11px;
  font-weight: 600;
  border-radius: 9px;
  display: flex;
  align-items: center;
  justify-content: center;

  &.pulse {
    animation: pulse 2s infinite;
  }
}

@keyframes pulse {
  0%, 100% {
    transform: scale(1);
  }
  50% {
    transform: scale(1.1);
  }
}
```

### 7.4 Notification Preferences Component

```typescript
// notification-preferences.component.ts
import { Component, inject, signal, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { NotificationService } from '@core/services/notification.service';
import { NotificationPreferences } from '@core/models/notification.model';

@Component({
  selector: 'app-notification-preferences',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './notification-preferences.component.html',
  styleUrl: './notification-preferences.component.scss'
})
export class NotificationPreferencesComponent implements OnInit {
  private fb = inject(FormBuilder);
  private notificationService = inject(NotificationService);

  form!: FormGroup;
  loading = signal(true);
  saving = signal(false);
  saved = signal(false);

  ngOnInit(): void {
    this.initForm();
    this.loadPreferences();
  }

  private initForm(): void {
    this.form = this.fb.group({
      emailOrderUpdates: [true],
      emailShippingUpdates: [true],
      emailPromotions: [false],
      emailNewsletter: [false],
      emailPriceDrops: [false],
      emailBackInStock: [false],
      inAppEnabled: [true],
      inAppOrderUpdates: [true],
      inAppPromotions: [true],
      emailFrequency: ['immediate'],
      quietHoursEnabled: [false],
      quietHoursStart: [null],
      quietHoursEnd: [null]
    });
  }

  private loadPreferences(): void {
    this.notificationService.getPreferences().subscribe({
      next: (prefs) => {
        this.form.patchValue(prefs);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
      }
    });
  }

  onSubmit(): void {
    if (this.form.invalid) return;

    this.saving.set(true);
    this.saved.set(false);

    this.notificationService.updatePreferences(this.form.value).subscribe({
      next: () => {
        this.saving.set(false);
        this.saved.set(true);
        setTimeout(() => this.saved.set(false), 3000);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  resetToDefaults(): void {
    this.form.patchValue({
      emailOrderUpdates: true,
      emailShippingUpdates: true,
      emailPromotions: false,
      emailNewsletter: false,
      emailPriceDrops: false,
      emailBackInStock: false,
      inAppEnabled: true,
      inAppOrderUpdates: true,
      inAppPromotions: true,
      emailFrequency: 'immediate',
      quietHoursEnabled: false,
      quietHoursStart: null,
      quietHoursEnd: null
    });
  }
}
```

---

## 8. E2E Tests (Playwright - NO MOCKING)

### 8.1 Test Setup and Utilities

```typescript
// tests/e2e/fixtures/notification.fixture.ts
import { test as base, expect } from '@playwright/test';
import { TestFactory } from './test-factory';
import { loginAs } from './auth.helpers';

export const test = base.extend<{
  factory: TestFactory;
  loginAs: typeof loginAs;
}>({
  factory: async ({ request }, use) => {
    const factory = new TestFactory(request);
    await use(factory);
    await factory.cleanup();
  },
  loginAs: async ({}, use) => {
    await use(loginAs);
  }
});

export { expect };
```

```typescript
// tests/e2e/fixtures/test-factory.ts
import { APIRequestContext } from '@playwright/test';

export class TestFactory {
  private createdUsers: string[] = [];
  private createdProducts: string[] = [];
  private createdOrders: string[] = [];

  constructor(private request: APIRequestContext) {}

  async createUser(overrides: Partial<User> = {}): Promise<User> {
    const userData = {
      email: `test-${Date.now()}@example.com`,
      password: 'TestPass123!',
      firstName: 'Test',
      lastName: 'User',
      ...overrides
    };

    const response = await this.request.post('/api/v1/test/users', {
      data: userData
    });

    const user = await response.json();
    this.createdUsers.push(user.id);
    return { ...user, password: userData.password };
  }

  async createProduct(overrides: Partial<Product> = {}): Promise<Product> {
    const productData = {
      name: `Test Product ${Date.now()}`,
      slug: `test-product-${Date.now()}`,
      price: 99.99,
      stock: 100,
      ...overrides
    };

    const response = await this.request.post('/api/v1/test/products', {
      data: productData
    });

    const product = await response.json();
    this.createdProducts.push(product.id);
    return product;
  }

  async createOrder(userId: string, items: OrderItem[]): Promise<Order> {
    const response = await this.request.post('/api/v1/test/orders', {
      data: { userId, items }
    });

    const order = await response.json();
    this.createdOrders.push(order.id);
    return order;
  }

  async createNotification(userId: string, data: Partial<Notification>): Promise<Notification> {
    const response = await this.request.post('/api/v1/test/notifications', {
      data: { userId, ...data }
    });
    return response.json();
  }

  async cleanup(): Promise<void> {
    // Cleanup in reverse order of dependencies
    for (const orderId of this.createdOrders) {
      await this.request.delete(`/api/v1/test/orders/${orderId}`);
    }
    for (const productId of this.createdProducts) {
      await this.request.delete(`/api/v1/test/products/${productId}`);
    }
    for (const userId of this.createdUsers) {
      await this.request.delete(`/api/v1/test/users/${userId}`);
    }
  }
}
```

### 8.2 Notification on Order Tests

```typescript
// tests/e2e/notifications/order-notifications.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Order Notifications', () => {
  test('NOT-E2E-001: user receives notification when order is placed', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange: Create test data
    const user = await factory.createUser();
    const product = await factory.createProduct({
      name: 'Test Air Conditioner',
      price: 599.99,
      stock: 10
    });

    // Act: Login and complete checkout
    await loginAs(page, user.email, user.password);

    // Add product to cart
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await expect(page.getByText('Added to cart')).toBeVisible();

    // Go to checkout
    await page.goto('/checkout');

    // Fill shipping information
    await page.fill('[data-testid="shipping-firstName"]', 'John');
    await page.fill('[data-testid="shipping-lastName"]', 'Doe');
    await page.fill('[data-testid="shipping-street"]', '123 Test Street');
    await page.fill('[data-testid="shipping-city"]', 'Test City');
    await page.fill('[data-testid="shipping-state"]', 'CA');
    await page.fill('[data-testid="shipping-postalCode"]', '90210');
    await page.click('[data-testid="continue-to-payment"]');

    // Fill payment information (test card)
    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/25');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order"]');

    // Wait for order confirmation page
    await expect(page).toHaveURL(/\/orders\/ORD-/);
    await expect(page.getByText('Order Confirmed')).toBeVisible();

    // Assert: Check notification bell shows unread
    await expect(page.getByTestId('notification-badge')).toHaveText('1');

    // Open notification dropdown
    await page.click('[data-testid="notification-bell"]');

    // Verify notification content
    await expect(page.getByText('Order Confirmed')).toBeVisible();
    await expect(page.getByText(/Your order #ORD-/)).toBeVisible();
  });

  test('NOT-E2E-002: user receives shipping notification when order ships', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange: Create user and pre-existing order
    const user = await factory.createUser();
    const product = await factory.createProduct();
    const order = await factory.createOrder(user.id, [
      { productId: product.id, quantity: 1 }
    ]);

    // Login
    await loginAs(page, user.email, user.password);

    // Trigger shipping event via test API (simulating admin action)
    await request.post(`/api/v1/test/orders/${order.id}/ship`, {
      data: {
        carrier: 'FedEx',
        trackingNumber: 'TRACK123456789'
      }
    });

    // Refresh and check notifications
    await page.reload();
    await page.waitForTimeout(1000); // Wait for notification to be created

    // Assert: Check notification
    await page.click('[data-testid="notification-bell"]');
    await expect(page.getByText('Order Shipped')).toBeVisible();
    await expect(page.getByText(/tracking/i)).toBeVisible();
  });

  test('NOT-E2E-003: clicking order notification navigates to order details', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    const product = await factory.createProduct();
    const order = await factory.createOrder(user.id, [
      { productId: product.id, quantity: 1 }
    ]);

    // Create notification manually for test
    await factory.createNotification(user.id, {
      type: 'order_confirmed',
      title: 'Order Confirmed',
      message: `Your order #${order.orderNumber} has been confirmed.`,
      link: `/orders/${order.orderNumber}`
    });

    // Act
    await loginAs(page, user.email, user.password);
    await page.click('[data-testid="notification-bell"]');
    await page.click('[data-testid="notification-item"]:first-child');

    // Assert
    await expect(page).toHaveURL(new RegExp(`/orders/${order.orderNumber}`));
    await expect(page.getByText(order.orderNumber)).toBeVisible();
  });
});
```

### 8.3 Notification Preferences Tests

```typescript
// tests/e2e/notifications/notification-preferences.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Notification Preferences', () => {
  test('NOT-E2E-004: user can disable email notifications', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    await loginAs(page, user.email, user.password);

    // Act
    await page.goto('/account/notification-preferences');
    await expect(page.getByText('Notification Preferences')).toBeVisible();

    // Verify initial state (email order updates should be on by default)
    const emailOrderUpdates = page.getByTestId('email-order-updates');
    await expect(emailOrderUpdates).toBeChecked();

    // Disable email order updates
    await emailOrderUpdates.uncheck();
    await page.click('[data-testid="save-preferences"]');

    // Assert: Success message
    await expect(page.getByText('Preferences saved')).toBeVisible();

    // Verify via API that preference was saved
    const response = await request.get('/api/v1/users/me/notification-preferences', {
      headers: {
        'Authorization': `Bearer ${await page.evaluate(() => localStorage.getItem('accessToken'))}`
      }
    });
    const prefs = await response.json();
    expect(prefs.emailOrderUpdates).toBe(false);
  });

  test('NOT-E2E-005: user can enable promotional emails', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    await loginAs(page, user.email, user.password);

    // Act
    await page.goto('/account/notification-preferences');

    // Enable promotional emails
    const emailPromotions = page.getByTestId('email-promotions');
    await expect(emailPromotions).not.toBeChecked(); // Should be off by default

    await emailPromotions.check();
    await page.click('[data-testid="save-preferences"]');

    // Assert
    await expect(page.getByText('Preferences saved')).toBeVisible();

    // Verify persistence after page reload
    await page.reload();
    await expect(page.getByTestId('email-promotions')).toBeChecked();
  });

  test('NOT-E2E-006: user can configure quiet hours', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    await loginAs(page, user.email, user.password);

    // Act
    await page.goto('/account/notification-preferences');

    // Enable quiet hours
    await page.getByTestId('quiet-hours-enabled').check();

    // Set quiet hours
    await page.fill('[data-testid="quiet-hours-start"]', '22:00');
    await page.fill('[data-testid="quiet-hours-end"]', '08:00');

    await page.click('[data-testid="save-preferences"]');

    // Assert
    await expect(page.getByText('Preferences saved')).toBeVisible();

    // Verify after reload
    await page.reload();
    await expect(page.getByTestId('quiet-hours-enabled')).toBeChecked();
    await expect(page.getByTestId('quiet-hours-start')).toHaveValue('22:00');
    await expect(page.getByTestId('quiet-hours-end')).toHaveValue('08:00');
  });

  test('NOT-E2E-007: user can reset preferences to defaults', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    await loginAs(page, user.email, user.password);

    // First, modify some preferences
    await page.goto('/account/notification-preferences');
    await page.getByTestId('email-order-updates').uncheck();
    await page.getByTestId('email-promotions').check();
    await page.click('[data-testid="save-preferences"]');
    await expect(page.getByText('Preferences saved')).toBeVisible();

    // Act: Reset to defaults
    await page.click('[data-testid="reset-to-defaults"]');
    await page.click('[data-testid="confirm-reset"]'); // Confirmation dialog

    // Assert: All should be back to defaults
    await expect(page.getByTestId('email-order-updates')).toBeChecked();
    await expect(page.getByTestId('email-promotions')).not.toBeChecked();
    await expect(page.getByText('Preferences reset to defaults')).toBeVisible();
  });
});
```

### 8.4 Notification Center Tests

```typescript
// tests/e2e/notifications/notification-center.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Notification Center', () => {
  test('NOT-E2E-008: user can view all notifications in notification center', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange: Create multiple notifications
    const user = await factory.createUser();

    await factory.createNotification(user.id, {
      type: 'order_confirmed',
      title: 'Order Confirmed',
      message: 'Your order #ORD-001 has been confirmed.'
    });
    await factory.createNotification(user.id, {
      type: 'order_shipped',
      title: 'Order Shipped',
      message: 'Your order #ORD-001 is on its way.'
    });
    await factory.createNotification(user.id, {
      type: 'welcome',
      title: 'Welcome to ClimaSite',
      message: 'Thank you for joining us!'
    });

    // Act
    await loginAs(page, user.email, user.password);
    await page.goto('/account/notifications');

    // Assert
    await expect(page.getByText('Notification Center')).toBeVisible();
    await expect(page.getByTestId('notification-item')).toHaveCount(3);
    await expect(page.getByText('Order Confirmed')).toBeVisible();
    await expect(page.getByText('Order Shipped')).toBeVisible();
    await expect(page.getByText('Welcome to ClimaSite')).toBeVisible();
  });

  test('NOT-E2E-009: user can filter notifications by type', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();

    await factory.createNotification(user.id, {
      type: 'order_confirmed',
      title: 'Order Confirmed',
      message: 'Test order notification'
    });
    await factory.createNotification(user.id, {
      type: 'welcome',
      title: 'Welcome',
      message: 'Welcome notification'
    });
    await factory.createNotification(user.id, {
      type: 'promotion',
      title: 'Special Offer',
      message: 'Promotional notification'
    });

    await loginAs(page, user.email, user.password);
    await page.goto('/account/notifications');

    // Act: Filter by order type
    await page.click('[data-testid="filter-orders"]');

    // Assert: Only order notifications visible
    await expect(page.getByTestId('notification-item')).toHaveCount(1);
    await expect(page.getByText('Order Confirmed')).toBeVisible();
    await expect(page.getByText('Welcome')).not.toBeVisible();
    await expect(page.getByText('Special Offer')).not.toBeVisible();
  });

  test('NOT-E2E-010: user can mark notification as read', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    const notification = await factory.createNotification(user.id, {
      type: 'order_confirmed',
      title: 'Order Confirmed',
      message: 'Test notification',
      isRead: false
    });

    await loginAs(page, user.email, user.password);
    await page.goto('/account/notifications');

    // Assert: Initially unread
    const notificationItem = page.getByTestId(`notification-${notification.id}`);
    await expect(notificationItem).toHaveClass(/unread/);

    // Act: Mark as read
    await notificationItem.click();

    // Assert: Now read
    await expect(notificationItem).not.toHaveClass(/unread/);
    await expect(page.getByTestId('notification-badge')).not.toBeVisible();
  });

  test('NOT-E2E-011: user can mark all notifications as read', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();

    for (let i = 0; i < 5; i++) {
      await factory.createNotification(user.id, {
        type: 'order_confirmed',
        title: `Notification ${i + 1}`,
        message: 'Test notification',
        isRead: false
      });
    }

    await loginAs(page, user.email, user.password);

    // Verify initial unread count
    await expect(page.getByTestId('notification-badge')).toHaveText('5');

    // Act
    await page.goto('/account/notifications');
    await page.click('[data-testid="mark-all-read"]');

    // Assert
    await expect(page.getByText('All notifications marked as read')).toBeVisible();
    await expect(page.getByTestId('notification-badge')).not.toBeVisible();

    // Verify all items no longer have unread class
    const items = page.getByTestId('notification-item');
    const count = await items.count();
    for (let i = 0; i < count; i++) {
      await expect(items.nth(i)).not.toHaveClass(/unread/);
    }
  });

  test('NOT-E2E-012: user can delete a notification', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    const notification = await factory.createNotification(user.id, {
      type: 'promotion',
      title: 'Special Offer',
      message: 'Deletable notification'
    });

    await loginAs(page, user.email, user.password);
    await page.goto('/account/notifications');

    // Act
    const notificationItem = page.getByTestId(`notification-${notification.id}`);
    await notificationItem.hover();
    await notificationItem.getByTestId('delete-notification').click();

    // Confirm deletion
    await page.click('[data-testid="confirm-delete"]');

    // Assert
    await expect(page.getByText('Notification deleted')).toBeVisible();
    await expect(notificationItem).not.toBeVisible();
  });

  test('NOT-E2E-013: empty state shows when no notifications', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange: User with no notifications
    const user = await factory.createUser();

    // Act
    await loginAs(page, user.email, user.password);
    await page.goto('/account/notifications');

    // Assert
    await expect(page.getByTestId('empty-state')).toBeVisible();
    await expect(page.getByText('No notifications yet')).toBeVisible();
  });
});
```

### 8.5 Email Delivery Tests

```typescript
// tests/e2e/notifications/email-delivery.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Email Delivery', () => {
  test('NOT-E2E-014: order confirmation email is logged', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser({ email: `test-${Date.now()}@example.com` });
    const product = await factory.createProduct();

    // Act: Complete an order
    await loginAs(page, user.email, user.password);
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await page.goto('/checkout');

    // Fill checkout form
    await page.fill('[data-testid="shipping-firstName"]', 'Test');
    await page.fill('[data-testid="shipping-lastName"]', 'User');
    await page.fill('[data-testid="shipping-street"]', '123 Test St');
    await page.fill('[data-testid="shipping-city"]', 'Test City');
    await page.fill('[data-testid="shipping-state"]', 'CA');
    await page.fill('[data-testid="shipping-postalCode"]', '90210');
    await page.click('[data-testid="continue-to-payment"]');

    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/25');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order"]');

    await expect(page).toHaveURL(/\/orders\/ORD-/);

    // Wait for email to be processed
    await page.waitForTimeout(2000);

    // Assert: Check email log via test API
    const response = await request.get(`/api/v1/test/email-logs?recipient=${user.email}`);
    const logs = await response.json();

    expect(logs.items.length).toBeGreaterThan(0);

    const orderEmail = logs.items.find((log: any) => log.emailType === 'order_confirmed');
    expect(orderEmail).toBeDefined();
    expect(orderEmail.status).toBe('sent');
    expect(orderEmail.subject).toContain('Order Confirmed');
  });

  test('NOT-E2E-015: email not sent when user opts out', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange: Create user and disable email notifications
    const user = await factory.createUser({ email: `optout-${Date.now()}@example.com` });

    await loginAs(page, user.email, user.password);

    // Disable email notifications
    await page.goto('/account/notification-preferences');
    await page.getByTestId('email-order-updates').uncheck();
    await page.click('[data-testid="save-preferences"]');
    await expect(page.getByText('Preferences saved')).toBeVisible();

    // Act: Place an order
    const product = await factory.createProduct();
    await page.goto(`/products/${product.slug}`);
    await page.click('[data-testid="add-to-cart"]');
    await page.goto('/checkout');

    // Complete checkout
    await page.fill('[data-testid="shipping-firstName"]', 'Test');
    await page.fill('[data-testid="shipping-lastName"]', 'User');
    await page.fill('[data-testid="shipping-street"]', '123 Test St');
    await page.fill('[data-testid="shipping-city"]', 'Test City');
    await page.fill('[data-testid="shipping-state"]', 'CA');
    await page.fill('[data-testid="shipping-postalCode"]', '90210');
    await page.click('[data-testid="continue-to-payment"]');

    await page.fill('[data-testid="card-number"]', '4242424242424242');
    await page.fill('[data-testid="card-expiry"]', '12/25');
    await page.fill('[data-testid="card-cvc"]', '123');
    await page.click('[data-testid="place-order"]');

    await expect(page).toHaveURL(/\/orders\/ORD-/);

    // Wait for potential email processing
    await page.waitForTimeout(2000);

    // Assert: No order confirmation email should be logged
    const response = await request.get(`/api/v1/test/email-logs?recipient=${user.email}&emailType=order_confirmed`);
    const logs = await response.json();

    expect(logs.items.length).toBe(0);

    // But in-app notification should still exist
    await expect(page.getByTestId('notification-badge')).toBeVisible();
  });

  test('NOT-E2E-016: password reset email is sent', async ({
    page,
    request,
    factory
  }) => {
    // Arrange
    const user = await factory.createUser({ email: `reset-${Date.now()}@example.com` });

    // Act: Request password reset
    await page.goto('/forgot-password');
    await page.fill('[data-testid="email-input"]', user.email);
    await page.click('[data-testid="send-reset-link"]');

    await expect(page.getByText('Reset link sent')).toBeVisible();

    // Wait for email processing
    await page.waitForTimeout(2000);

    // Assert: Check email log
    const response = await request.get(`/api/v1/test/email-logs?recipient=${user.email}&emailType=password_reset`);
    const logs = await response.json();

    expect(logs.items.length).toBe(1);
    expect(logs.items[0].status).toBe('sent');
    expect(logs.items[0].subject).toContain('Reset Your Password');
  });
});
```

### 8.6 Admin Notification Tests

```typescript
// tests/e2e/notifications/admin-notifications.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Admin Notifications', () => {
  test('NOT-E2E-017: admin receives low stock notification', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange: Create admin user and product with low stock
    const admin = await factory.createUser({ role: 'admin' });
    const product = await factory.createProduct({
      name: 'Low Stock AC Unit',
      stock: 5,
      lowStockThreshold: 10
    });

    // Trigger low stock check via test API
    await request.post('/api/v1/test/inventory/check-stock');

    // Act: Login as admin
    await loginAs(page, admin.email, admin.password);

    // Assert: Admin should see low stock notification
    await expect(page.getByTestId('notification-badge')).toBeVisible();
    await page.click('[data-testid="notification-bell"]');
    await expect(page.getByText('Low Stock Alert')).toBeVisible();
    await expect(page.getByText('Low Stock AC Unit')).toBeVisible();
  });

  test('NOT-E2E-018: admin can broadcast notification to all users', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange: Create admin and regular users
    const admin = await factory.createUser({ role: 'admin' });
    const user1 = await factory.createUser();
    const user2 = await factory.createUser();

    // Act: Admin broadcasts notification
    await loginAs(page, admin.email, admin.password);
    await page.goto('/admin/notifications');
    await page.click('[data-testid="create-broadcast"]');

    await page.fill('[data-testid="broadcast-title"]', 'System Maintenance');
    await page.fill('[data-testid="broadcast-message"]', 'Scheduled maintenance tonight.');
    await page.selectOption('[data-testid="broadcast-audience"]', 'all_users');
    await page.click('[data-testid="send-broadcast"]');

    await expect(page.getByText('Broadcast sent successfully')).toBeVisible();

    // Assert: Both regular users should have received the notification
    await loginAs(page, user1.email, user1.password);
    await page.click('[data-testid="notification-bell"]');
    await expect(page.getByText('System Maintenance')).toBeVisible();

    await loginAs(page, user2.email, user2.password);
    await page.click('[data-testid="notification-bell"]');
    await expect(page.getByText('System Maintenance')).toBeVisible();
  });

  test('NOT-E2E-019: admin can view email logs', async ({
    page,
    factory,
    loginAs
  }) => {
    // Arrange: Create admin and generate some email logs
    const admin = await factory.createUser({ role: 'admin' });
    const user = await factory.createUser();

    // Create a notification that triggers email
    await factory.createNotification(user.id, {
      type: 'welcome',
      title: 'Welcome',
      message: 'Welcome email'
    });

    // Act
    await loginAs(page, admin.email, admin.password);
    await page.goto('/admin/email-logs');

    // Assert
    await expect(page.getByText('Email Logs')).toBeVisible();
    await expect(page.getByTestId('email-log-item')).toHaveCount.greaterThan(0);
  });
});
```

### 8.7 Real-time Updates Tests (Polling)

```typescript
// tests/e2e/notifications/realtime-updates.spec.ts
import { test, expect } from '../fixtures/notification.fixture';

test.describe('Real-time Notification Updates', () => {
  test('NOT-E2E-020: notification badge updates without page refresh', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();
    await loginAs(page, user.email, user.password);

    // Initial state: no unread notifications
    await expect(page.getByTestId('notification-badge')).not.toBeVisible();

    // Act: Create notification via API (simulating server-side event)
    await request.post('/api/v1/test/notifications', {
      data: {
        userId: user.id,
        type: 'order_confirmed',
        title: 'New Order',
        message: 'Your order has been confirmed'
      }
    });

    // Assert: Badge should appear after polling interval (max 30 seconds)
    // Using shorter timeout for test efficiency
    await expect(page.getByTestId('notification-badge')).toBeVisible({ timeout: 35000 });
    await expect(page.getByTestId('notification-badge')).toHaveText('1');
  });

  test('NOT-E2E-021: notification count updates when new notification arrives', async ({
    page,
    request,
    factory,
    loginAs
  }) => {
    // Arrange
    const user = await factory.createUser();

    // Create initial notification
    await factory.createNotification(user.id, {
      type: 'order_confirmed',
      title: 'Order 1',
      message: 'First order'
    });

    await loginAs(page, user.email, user.password);
    await expect(page.getByTestId('notification-badge')).toHaveText('1');

    // Act: Create another notification
    await request.post('/api/v1/test/notifications', {
      data: {
        userId: user.id,
        type: 'order_shipped',
        title: 'Order Shipped',
        message: 'Your order is on its way'
      }
    });

    // Assert: Count should update
    await expect(page.getByTestId('notification-badge')).toHaveText('2', { timeout: 35000 });
  });
});
```

---

## 9. Implementation Timeline

### Phase 1: Foundation (Week 1-2)
- NOT-001: Database Schema and Migrations
- NOT-002: Email Service Infrastructure
- NOT-003: Email Template Engine
- NOT-004: Notification Service Core

### Phase 2: Backend Features (Week 3-4)
- NOT-005: Background Email Processing
- NOT-006: Order Event Handlers
- NOT-007: User Event Handlers
- NOT-008: Inventory Event Handlers
- NOT-009: Notifications API Controller
- NOT-010: Notification Preferences API

### Phase 3: Frontend Core (Week 5-6)
- NOT-012: Angular Notification Service
- NOT-013: Notification Bell Component
- NOT-014: Notification Dropdown Component
- NOT-015: Notification Center Page
- NOT-016: Notification Preferences Page

### Phase 4: Admin & Polish (Week 7-8)
- NOT-011: Admin Notifications API
- NOT-017: Email Unsubscribe Page
- NOT-018: Admin Email Template Editor
- NOT-019: Admin Email Logs Viewer
- NOT-020: Integration Tests
- E2E Test Suite (NOT-E2E-001 through NOT-E2E-021)

---

## 10. Configuration

### 10.1 appsettings.json Configuration

```json
{
  "Notifications": {
    "Email": {
      "Provider": "SendGrid",
      "FromAddress": "noreply@climasite.com",
      "FromName": "ClimaSite",
      "ReplyToAddress": "support@climasite.com",
      "Smtp": {
        "Host": "smtp.example.com",
        "Port": 587,
        "Username": "",
        "Password": "",
        "EnableSsl": true
      },
      "SendGrid": {
        "ApiKey": ""
      },
      "RetryAttempts": 3,
      "RetryDelaySeconds": 30,
      "BatchSize": 100
    },
    "InApp": {
      "PollingIntervalSeconds": 30,
      "MaxNotificationsPerUser": 100,
      "RetentionDays": 90
    },
    "Templates": {
      "BasePath": "EmailTemplates",
      "CacheMinutes": 60
    },
    "Digest": {
      "DailyDigestTime": "08:00",
      "WeeklyDigestDay": "Monday",
      "WeeklyDigestTime": "08:00"
    }
  }
}
```

### 10.2 Environment Variables

```bash
# Email Configuration
EMAIL_PROVIDER=SendGrid
EMAIL_FROM_ADDRESS=noreply@climasite.com
SENDGRID_API_KEY=SG.xxxxxxxxxxxxx
SMTP_HOST=smtp.example.com
SMTP_PORT=587
SMTP_USERNAME=
SMTP_PASSWORD=
SMTP_ENABLE_SSL=true

# Notification Settings
NOTIFICATION_POLLING_INTERVAL=30
NOTIFICATION_MAX_PER_USER=100
NOTIFICATION_RETENTION_DAYS=90
```

---

## 11. Security Considerations

### 11.1 Email Security
- Use TLS for SMTP connections
- Validate email addresses before sending
- Rate limit email sending per user
- Include unsubscribe links in all marketing emails (CAN-SPAM compliance)
- Use signed tokens for unsubscribe links

### 11.2 API Security
- All notification endpoints require authentication
- Users can only access their own notifications
- Admin endpoints require admin role
- Rate limit notification creation
- Sanitize notification content to prevent XSS

### 11.3 Data Privacy
- Implement GDPR-compliant data handling
- Allow users to export notification history
- Delete notification data when user account is deleted
- Log access to notification preferences

---

## 12. Monitoring and Observability

### 12.1 Metrics to Track
- Email send success/failure rate
- Email delivery time
- Bounce rate
- Open rate (if tracking enabled)
- Notification creation rate
- Notification read rate
- API response times

### 12.2 Alerts
- Email send failure rate > 5%
- Email queue backlog > 1000
- Background processor crashes
- Template rendering errors

### 12.3 Logging
- Log all email send attempts with status
- Log notification creation events
- Log preference changes
- Log background job execution

---

## 13. Dependencies

### Backend Packages
```xml
<!-- ClimaSite.Infrastructure.csproj -->
<PackageReference Include="SendGrid" Version="9.*" />
<PackageReference Include="MailKit" Version="4.*" />
<PackageReference Include="Handlebars.Net" Version="2.*" />
<PackageReference Include="FluentValidation" Version="11.*" />
```

### Frontend Packages
```json
{
  "dependencies": {
    "@angular/cdk": "^19.0.0",
    "date-fns": "^3.0.0"
  }
}
```

---

## 14. Glossary

| Term | Definition |
|------|------------|
| In-App Notification | A notification displayed within the application UI |
| Email Notification | A notification sent to user's email address |
| Notification Preference | User settings controlling what notifications they receive |
| Quiet Hours | Time period during which non-critical notifications are held |
| Email Digest | A summary email containing multiple notifications |
| Broadcast | A notification sent to multiple users simultaneously |
| Bounce | An email that could not be delivered |
| Template Variable | A placeholder in email templates replaced with actual values |
