# Skill: API Auditor (SKILL-API)

## Purpose

The API Auditor reviews backend endpoints for consistency, proper error handling, correct status codes, security, and overall quality of the API contract.

## What You Look For

### 1. Response Consistency
- [ ] All endpoints return consistent JSON structure
- [ ] Success responses follow same pattern
- [ ] Error responses follow same pattern
- [ ] Pagination is consistent (pageNumber, pageSize, totalCount, totalPages)
- [ ] Null vs undefined vs empty array handling consistent
- [ ] Date formats consistent (ISO 8601)

### 2. HTTP Status Codes
- [ ] 200 OK for successful GET/PUT/PATCH
- [ ] 201 Created for successful POST that creates resource
- [ ] 204 No Content for successful DELETE
- [ ] 400 Bad Request for validation errors
- [ ] 401 Unauthorized for missing/invalid auth
- [ ] 403 Forbidden for insufficient permissions
- [ ] 404 Not Found for missing resources
- [ ] 409 Conflict for duplicate resources
- [ ] 422 Unprocessable Entity for semantic errors
- [ ] 500 Internal Server Error only for unexpected errors

### 3. Error Handling
- [ ] All errors return meaningful messages
- [ ] Error format is consistent (ProblemDetails)
- [ ] Stack traces not exposed in production
- [ ] Sensitive data not leaked in errors
- [ ] Validation errors list all issues (not just first)
- [ ] Error messages are user-friendly

### 4. Input Validation
- [ ] Required fields are validated
- [ ] Field types are validated
- [ ] Field lengths are validated
- [ ] Business rules are validated
- [ ] SQL injection prevented
- [ ] XSS prevented
- [ ] Invalid input returns 400 with details

### 5. Authentication & Authorization
- [ ] Protected endpoints require auth
- [ ] Auth tokens validated properly
- [ ] Expired tokens return 401
- [ ] Role-based access enforced
- [ ] Users can only access own data
- [ ] Admin endpoints protected
- [ ] CORS configured correctly

### 6. Performance
- [ ] Responses are reasonably fast (< 200ms typical)
- [ ] Large lists are paginated
- [ ] N+1 queries avoided
- [ ] Proper indexes exist
- [ ] No unnecessary data in responses
- [ ] Compression enabled

### 7. REST Best Practices
- [ ] Resource naming is consistent (plural, kebab-case)
- [ ] Proper HTTP methods (GET reads, POST creates, etc.)
- [ ] Idempotency where expected
- [ ] Proper use of query params vs path params
- [ ] Location header on 201 responses
- [ ] HATEOAS links (if applicable)

### 8. Documentation
- [ ] Swagger/OpenAPI is accurate
- [ ] All endpoints documented
- [ ] Request/response schemas correct
- [ ] Examples provided
- [ ] Error responses documented

## API Endpoints to Audit

### Products API
```
GET    /api/products                 # List products (paginated)
GET    /api/products/{id}            # Get product by ID
GET    /api/products/slug/{slug}     # Get product by slug
GET    /api/products/featured        # Get featured products
GET    /api/products/filter-options  # Get filter options
```

### Categories API
```
GET    /api/categories               # List categories
GET    /api/categories/{slug}        # Get category by slug
GET    /api/categories/{id}/products # Get products in category
```

### Cart API
```
GET    /api/cart                     # Get current cart
POST   /api/cart/items               # Add item to cart
PUT    /api/cart/items/{id}          # Update item quantity
DELETE /api/cart/items/{id}          # Remove item from cart
DELETE /api/cart                     # Clear cart
```

### Orders API
```
GET    /api/orders                   # List user's orders
GET    /api/orders/{id}              # Get order details
POST   /api/orders                   # Create order
POST   /api/orders/{id}/reorder      # Reorder previous order
```

### Auth API
```
POST   /api/auth/register            # Register new user
POST   /api/auth/login               # Login
POST   /api/auth/refresh             # Refresh token
POST   /api/auth/logout              # Logout
POST   /api/auth/forgot-password     # Request password reset
POST   /api/auth/reset-password      # Reset password
```

### Account API
```
GET    /api/account/profile          # Get user profile
PUT    /api/account/profile          # Update profile
PUT    /api/account/password         # Change password
```

### Addresses API
```
GET    /api/addresses                # List user's addresses
POST   /api/addresses                # Add new address
PUT    /api/addresses/{id}           # Update address
DELETE /api/addresses/{id}           # Delete address
PUT    /api/addresses/{id}/default   # Set default address
```

### Reviews API
```
GET    /api/products/{id}/reviews    # Get product reviews
POST   /api/products/{id}/reviews    # Add review
```

## Testing Methodology

### 1. Happy Path Testing
Test each endpoint with valid data, verify correct response.

### 2. Validation Testing
Test with invalid/missing data, verify proper errors.

### 3. Auth Testing
Test with no auth, invalid auth, expired auth, wrong role.

### 4. Edge Case Testing
Test with empty data, max lengths, special characters, unicode.

### 5. Performance Testing
Measure response times, check for slow queries.

## Severity Classification

| Severity | Description | Example |
|----------|-------------|---------|
| CRITICAL | Security issue or data corruption | Auth bypass, SQL injection |
| HIGH | Incorrect behavior affecting users | Wrong status code, missing validation |
| MEDIUM | Inconsistency or poor practice | Inconsistent error format |
| LOW | Minor improvement | Could include more fields in response |

## Output Format

```markdown
### [ISSUE-XXX] [Short descriptive title]

- **Severity**: [CRITICAL/HIGH/MEDIUM/LOW]
- **Category**: API
- **Endpoint**: [METHOD /path]
- **Description**: [What's wrong]
- **Request**: [curl command or request body]
- **Expected Response**: [What should return]
- **Actual Response**: [What returns now]
- **Status Code Expected**: [Expected HTTP status]
- **Status Code Actual**: [Actual HTTP status]
- **Suggested Fix**: [How to fix]
- **Status**: Open
```

## Common Issues to Watch For

1. **Wrong status code** - Returns 200 when should be 201
2. **Missing validation** - Accepts invalid email format
3. **Inconsistent errors** - Different error format on different endpoints
4. **Leaked data** - Returns password hash in user object
5. **N+1 queries** - Fetching related data in loop
6. **Missing auth** - Endpoint should require login but doesn't
7. **Over-fetching** - Returns entire entity when only ID needed
8. **No pagination** - Returns 10000 products at once
9. **Incorrect null handling** - Returns "null" string instead of null
10. **Missing CORS** - Frontend can't call API
11. **Swagger mismatch** - Docs don't match actual API
12. **Stack trace in error** - Implementation details leaked
