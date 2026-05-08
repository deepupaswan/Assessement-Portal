# FluentValidation Implementation Guide

## Overview
FluentValidation has been added to ALL backend services for comprehensive input validation at the API layer. This prevents invalid, malicious, or malformed data from entering the business logic layer.

**Status**: ✅ **IMPLEMENTED** (All 5 API services)

## What Was Added

### 1. **NuGet Packages**
```xml
<PackageReference Include="FluentValidation" Version="11.9.2" />
<PackageReference Include="FluentValidation.AspNetCore" Version="11.9.2" />
```

Added to all API projects:
- `backend/identity-service/Api/IdentityService.Api.csproj`
- `backend/answer-service/Api/AnswerService.Api.csproj`
- `backend/assessment-service/Api/AssessmentService.Api.csproj`
- `backend/candidate-service/Api/CandidateService.Api.csproj`
- `backend/result-service/Api/ResultService.Api.csproj`

### 2. **Service Registration in Program.cs**
```csharp
// Add FluentValidation for input validation (CRITICAL SECURITY)
builder.Services.AddFluentValidationAutoValidation();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

This automatically:
- Discovers all validators in the assembly
- Registers them in the DI container
- Enables automatic model validation on controller actions
- Returns 400 Bad Request with validation errors automatically

### 3. **Validator Classes**

#### **Identity Service** (`Api/Validators/AuthValidators.cs`)
- `RegisterRequestValidator` - Validates user registration data
  - Email: Required, valid format, max 254 chars
  - Password: Min 10 chars, requires uppercase, lowercase, digit, special char
  - FirstName/LastName: Required, max 100 chars, alphanumeric only
  - Role: Valid enum value

- `LoginRequestValidator` - Validates login data
  - Email: Required, valid format
  - Password: Required, max 128 chars

- `RefreshTokenRequestValidator` - Validates token refresh
  - RefreshToken: Required, max 2048 chars

#### **Assessment Service** (`Api/Validators/AssessmentValidators.cs`)
- `CreateAssessmentRequestValidator`
  - Title: Required, 3-255 chars, no special characters
  - Description: Optional, max 2000 chars, no scripts
  - DurationMinutes: 1-480 minutes (max 8 hours)

- `UpdateAssessmentRequestValidator`
  - Same rules as create (for updates)

#### **Answer Service** (`Api/Validators/AnswerValidators.cs`)
- `SubmitAnswerRequestValidator`
  - CandidateAssessmentId: Required, valid GUID
  - QuestionId: Required, valid GUID
  - SelectedOptionIds: Required, non-empty list

- `UpdateAnswerRequestValidator`
  - SelectedOptionIds: Required, non-empty list

#### **Candidate Service** (`Api/Validators/CandidateValidators.cs`)
- `CreateCandidateRequestValidator`
  - Email: Required, valid format
  - FirstName/LastName: Required, max 100 chars
  - Phone: Optional, max 20 chars, phone format validation

- `UpdateCandidateRequestValidator`
  - Same phone validation as create

#### **Result Service** (`Api/Validators/ResultValidators.cs`)
- `CreateResultRequestValidator`
  - CandidateAssessmentId: Required
  - TotalQuestions: Must be > 0
  - CorrectAnswers: Must be 0-100, cannot exceed total
  - ScorePercentage: Must be 0-100 range

- `UpdateResultRequestValidator`
  - Same rules for updates

## How It Works

### Automatic Validation
When a request arrives at a controller with a validated DTO:

```csharp
[HttpPost]
public async Task<IActionResult> CreateAssessment([FromBody] CreateAssessmentRequest request)
{
    // request is automatically validated before this method is called
    // If invalid, returns 400 with error details below
}
```

### Error Response Format
Invalid requests return HTTP 400 with error details:
```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "One or more validation errors occurred.",
  "status": 400,
  "traceId": "00-...",
  "errors": {
    "Password": [
      "Password must be at least 10 characters long",
      "Password must contain at least one special character (!@#$%^&*(),.?\":{}|<>)"
    ]
  }
}
```

### Custom Validation Rules

Example from AssessmentValidator:
```csharp
RuleFor(x => x.Title)
    .NotEmpty()
    .WithMessage("Title is required")
    .MinimumLength(3)
    .WithMessage("Title must be at least 3 characters long")
    .Matches(@"^[a-zA-Z0-9\s\-.,()&':;/]+$")
    .WithMessage("Title contains invalid characters");
```

**Common Rules Used**:
- `NotEmpty()` - Required field
- `EmailAddress()` - Valid email format
- `MinimumLength(n)` / `MaximumLength(n)` - String length
- `GreaterThan(n)` / `LessThanOrEqualTo(n)` - Numeric range
- `Matches(regex)` - Pattern matching
- `IsInEnum()` - Enum validation
- `Must(predicate)` - Custom predicate
- `When(condition)` - Conditional validation

## Security Benefits

1. **SQL Injection Prevention**: Email and string inputs validated
2. **XSS Prevention**: Special characters and script tags blocked (e.g., in description)
3. **Brute Force Protection**: Email format validated early (fails fast)
4. **Type Safety**: Numeric ranges enforced (can't set Duration to -1)
5. **Data Quality**: Format requirements ensure clean data in database
6. **Password Strength**: Multi-character class requirements prevent weak passwords

## Testing Validation

### Test Invalid Data
```bash
# Register with weak password
POST /api/auth/register
{
  "email": "user@test.com",
  "password": "weak",
  "firstName": "John",
  "lastName": "Doe"
}

# Response: 400 Bad Request
{
  "errors": {
    "Password": [
      "Password must be at least 10 characters long",
      "Password must contain at least one uppercase letter",
      ...
    ]
  }
}
```

### Test Valid Data
```bash
POST /api/auth/register
{
  "email": "user@test.com",
  "password": "SecureP@ss123",
  "firstName": "John",
  "lastName": "Doe"
}

# Response: 200 OK
```

## Next Steps

1. **Integration Tests**: Create test cases for each validator
2. **Custom Rules**: Add business logic validators (e.g., "Email not already registered")
3. **Localization**: Add multilingual error messages
4. **API Gateway**: Add similar validation at gateway level for defense-in-depth

## Reference Documentation
- FluentValidation: https://docs.fluentvalidation.net/
- ASP.NET Core Integration: https://docs.fluentvalidation.net/latest/aspnet.html
- Built-in Rules: https://docs.fluentvalidation.net/latest/built-in-validators.html
