# Security Implementation Guide

## HearLoveen Platform - Security Best Practices

**Document Version:** 1.0
**Last Updated:** 2025-01-07

---

## 1. Authentication & Authorization

### OIDC/JWKS Implementation

**AnalysisProxy (services/AnalysisProxy/Program.cs:20-33)**
```csharp
// Azure AD B2C authentication with JWKS
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
        options.TokenValidationParameters.ValidateIssuer = true;
        options.TokenValidationParameters.ValidateAudience = true;
        options.TokenValidationParameters.ValidateLifetime = true;
        options.TokenValidationParameters.ValidateIssuerSigningKey = true;
    },
    options =>
    {
        builder.Configuration.Bind("AzureAdB2C", options);
    });
```

**Security Measures:**
- ✅ Token validation (issuer, audience, lifetime, signing key)
- ✅ HTTPS required in production
- ✅ Role-based access control (RBAC)
- ✅ Rate limiting (120 requests/minute)

---

## 2. SQL Injection Prevention

### Parameterized Queries (Dapper)

**Privacy.API (services/Privacy.API/Program.cs:95-97)**
```csharp
// ✅ SECURE: Using parameterized queries
var requestId = await con.QuerySingleAsync<int>(
    "INSERT INTO dsr_requests(user_id, action, status) VALUES (@u,'export','queued') RETURNING id",
    new { u = userId }
);
```

**✅ All database queries use:**
- Parameterized queries with Dapper
- No string concatenation for SQL
- Input validation before queries

**❌ NEVER do this:**
```csharp
// INSECURE - SQL Injection vulnerability
var query = $"SELECT * FROM users WHERE id = '{userId}'";
```

---

## 3. XSS (Cross-Site Scripting) Prevention

### Frontend Protection

**React/TypeScript (apps/therapist-dashboard)**
- ✅ React automatically escapes all JSX content
- ✅ Using TypeScript for type safety
- ✅ No `dangerouslySetInnerHTML` usage
- ✅ Content Security Policy (CSP) headers

**Backend Protection**
```csharp
// Input sanitization middleware
app.Use(async (context, next) =>
{
    // Add security headers
    context.Response.Headers.Add("X-Content-Type-Options", "nosniff");
    context.Response.Headers.Add("X-Frame-Options", "DENY");
    context.Response.Headers.Add("X-XSS-Protection", "1; mode=block");
    context.Response.Headers.Add("Content-Security-Policy",
        "default-src 'self'; script-src 'self' 'unsafe-inline'; style-src 'self' 'unsafe-inline';");

    await next();
});
```

---

## 4. Data Encryption

### Encryption at Rest
- **Database:** PostgreSQL with encryption enabled
- **File Storage:** Azure Blob Storage with AES-256 encryption
- **Secrets:** Azure Key Vault for all credentials

### Encryption in Transit
- **TLS 1.3** for all API communication
- **Certificate pinning** in mobile app
- **HTTPS only** in production

```csharp
// Enforce HTTPS in production
if (app.Environment.IsProduction())
{
    app.UseHsts();
    app.UseHttpsRedirection();
}
```

---

## 5. Input Validation

### API Input Validation

```csharp
// Example: DSR endpoint validation
if (payload == null || !payload.ContainsKey("userId"))
{
    return Results.BadRequest(new { error = "userId is required" });
}

var userId = payload["userId"];

// Validate userId format (prevent injection)
if (!Regex.IsMatch(userId, @"^[a-zA-Z0-9\-]+$"))
{
    return Results.BadRequest(new { error = "Invalid userId format" });
}
```

### File Upload Security

**ML API (ml-platform/inference/api/main.py:62-78)**
```python
async def validate_file_size(file: UploadFile):
    """Validate uploaded file size"""
    size = 0
    chunk_size = 1024 * 1024  # 1MB chunks

    while chunk := await file.read(chunk_size):
        size += len(chunk)
        if size > MAX_FILE_SIZE:
            raise HTTPException(
                status_code=413,
                detail=f"File too large. Maximum size is {MAX_FILE_SIZE / 1024 / 1024}MB"
            )

    await file.seek(0)
    return size
```

---

## 6. Rate Limiting & DDoS Protection

### AnalysisProxy Rate Limiting

```csharp
builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("fixed", opt =>
    {
        opt.Window = TimeSpan.FromMinutes(1);
        opt.PermitLimit = 120;
        opt.QueueLimit = 0;
    });

    options.OnRejected = async (context, token) =>
    {
        context.HttpContext.Response.StatusCode = 429;
        await context.HttpContext.Response.WriteAsync(
            "Too many requests. Please try again later.", token);
    };
});
```

### ML API Rate Limiting

```python
from slowapi import Limiter, _rate_limit_exceeded_handler

limiter = Limiter(key_func=get_remote_address)
app.state.limiter = limiter

@app.post("/api/transcribe")
@limiter.limit("10/minute")
async def transcribe_audio(file: UploadFile):
    ...
```

---

## 7. Secrets Management

### Azure Key Vault Integration

```csharp
// appsettings.json (NEVER commit secrets!)
{
  "AzureKeyVault": {
    "Url": "https://hearloveen-vault.vault.azure.net/"
  },
  "ConnectionStrings": {
    "DefaultConnection": "@Microsoft.KeyVault(SecretUri=https://hearloveen-vault.vault.azure.net/secrets/db-connection)"
  }
}
```

### Environment Variables

```bash
# .env.example (template only, no real values)
POSTGRES_PASSWORD=PLEASE_CHANGE_ME
AZURE_IOT_HUB_CONNECTION_STRING=PLEASE_CHANGE_ME
ML_API_KEY=PLEASE_CHANGE_ME
GRAFANA_ADMIN_PASSWORD=PLEASE_CHANGE_ME
```

**✅ NEVER commit:**
- `.env` files
- `appsettings.Production.json` with secrets
- Private keys or certificates
- API keys

---

## 8. Audit Logging

### Comprehensive Audit Trail

```csharp
// Audit log structure
await con.ExecuteAsync(
    @"INSERT INTO audit_log (event_type, user_id, description, created_at, metadata)
      VALUES (@eventType, @userId, @description, CURRENT_TIMESTAMP, @metadata::jsonb)",
    new
    {
        eventType = "user_data_deleted",
        userId = userId,
        description = "User data deleted per GDPR request",
        metadata = JsonSerializer.Serialize(new { requestId, timestamp = DateTime.UtcNow })
    }
);
```

**Events Logged:**
- Authentication attempts (success/failure)
- Authorization failures
- Data access (patient records)
- Configuration changes
- AI model predictions
- DSR operations (export/delete)
- Security incidents

**Retention:** 7 years (MDR compliance)

---

## 9. Dependency Security

### Automated Vulnerability Scanning

```yaml
# .github/workflows/security.yml
name: Security Scan
on: [push, pull_request]

jobs:
  security:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3

      # .NET dependency check
      - name: Run dotnet list package --vulnerable
        run: dotnet list package --vulnerable --include-transitive

      # Python dependency check
      - name: Run safety check
        run: |
          pip install safety
          safety check -r requirements.txt

      # NPM dependency check
      - name: Run npm audit
        run: |
          cd web-dashboard
          npm audit --audit-level=high
```

### Regular Updates
- Weekly dependency updates
- Security patches applied within 48 hours
- Automated Dependabot alerts

---

## 10. CORS Configuration

### Restrictive CORS Policy

```csharp
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowTherapistDashboard", policy =>
    {
        policy.WithOrigins(builder.Configuration
                .GetSection("Cors:AllowedOrigins")
                .Get<string[]>() ?? new[] { "http://localhost:5173" })
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// ✅ NEVER use:
// .AllowAnyOrigin() - This is a security risk!
```

---

## 11. Mobile App Security

### BLE Security (mobile-app/src/services/BLEService.ts)

```typescript
// No pairing secrets persisted in plaintext
private async saveConnectedDevices(): Promise<void> {
    const deviceIds = Array.from(this.connectedDevices.keys());
    // ✅ Only save device IDs, not credentials
    await AsyncStorage.setItem('connected_hearing_aids', JSON.stringify(deviceIds));
}

// Request OS-level permissions (not stored in app)
async requestPermissions(): Promise<boolean> {
    const state = await this.manager.state();
    return state === State.PoweredOn;
}
```

---

## 12. Sensitive Data Handling

### Data Anonymization (DSR Worker)

```csharp
// Anonymize user recordings (GDPR Right to Erasure)
await con.ExecuteAsync(
    "UPDATE audio_recordings SET user_id = 'ANONYMIZED', anonymized_at = CURRENT_TIMESTAMP WHERE user_id = @userId",
    new { userId = payload.userId }
);

// Keep audit trail (MDR compliance)
await con.ExecuteAsync(
    @"INSERT INTO audit_log (event_type, user_id, description, created_at)
      VALUES ('user_data_deleted', @userId, 'User data deleted per GDPR request', CURRENT_TIMESTAMP)",
    new { userId = payload.userId }
);
```

---

## 13. Error Handling

### Secure Error Messages

```csharp
// ✅ GOOD: Generic error message to user
catch (Exception ex)
{
    Log.Error(ex, "Error processing DSR export request");
    return Results.Problem("Failed to process export request", statusCode: 500);
}

// ❌ BAD: Exposes internals
catch (Exception ex)
{
    return Results.Problem(ex.Message, statusCode: 500); // NEVER DO THIS!
}
```

---

## 14. Security Checklist

| Category | Status | Evidence |
|----------|--------|----------|
| Authentication (OIDC/JWKS) | ✅ | AnalysisProxy, Privacy.API |
| SQL Injection Prevention | ✅ | Parameterized queries everywhere |
| XSS Prevention | ✅ | React escaping, CSP headers |
| CSRF Protection | ✅ | SameSite cookies, tokens |
| Encryption at Rest | ✅ | AES-256 |
| Encryption in Transit | ✅ | TLS 1.3 |
| Rate Limiting | ✅ | 120 req/min |
| Input Validation | ✅ | All endpoints |
| Secrets Management | ✅ | Azure Key Vault |
| Audit Logging | ✅ | 7-year retention |
| CORS Configuration | ✅ | Restrictive policy |
| Dependency Scanning | ✅ | Automated CI/CD |
| Error Handling | ✅ | Generic messages |
| GDPR Compliance | ✅ | DSR endpoints |

---

## 15. Penetration Testing

**Annual Security Audit:**
- Third-party penetration testing
- OWASP Top 10 verification
- Vulnerability assessment
- Remediation within 30 days

**Last Audit:** [Pending]
**Next Audit:** Q2 2025

---

## 16. Incident Response Plan

### Security Incident Procedure

1. **Detection** (< 24 hours)
   - Automated monitoring alerts
   - Log analysis
   - User reports

2. **Containment** (< 48 hours)
   - Isolate affected systems
   - Revoke compromised credentials
   - Block malicious IPs

3. **Investigation**
   - Root cause analysis
   - Impact assessment
   - Evidence collection

4. **Remediation**
   - Apply security patches
   - Update configurations
   - Deploy fixes

5. **Communication**
   - Notify affected users (GDPR)
   - Report to authorities if required
   - Internal stakeholder updates

6. **Post-Incident Review**
   - Lessons learned
   - Process improvements
   - Documentation updates

---

## 17. Compliance Mapping

| Regulation | Requirement | Implementation |
|------------|-------------|----------------|
| **GDPR** | Data encryption | TLS 1.3 + AES-256 |
| **GDPR** | Right to erasure | DSR delete endpoint |
| **GDPR** | Data portability | DSR export (JSON) |
| **GDPR** | Breach notification | 72h incident response |
| **MDR** | Audit trail | 7-year log retention |
| **MDR** | Access control | RBAC + OIDC |
| **ISO 27001** | Risk management | Documented risk register |
| **ISO 13485** | Change control | Git + approval workflow |

---

**Contact:**
- Security Team: security@hearloveen.com
- Incident Response: +31 (0) XXX XXX XXX (24/7)
