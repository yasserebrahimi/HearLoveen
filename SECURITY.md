# Security Recommendations

## Critical Security Issues Fixed

This document outlines the security improvements made to the HearLoveen project.

### 1. Removed Hardcoded Credentials

**Issue**: Default passwords and connection strings were hardcoded in configuration files.

**Fix**:
- All credentials now use environment variables
- Added `.env.example` with placeholders
- Never commit `.env` files with real credentials to version control

**Action Required**:
```bash
# Copy the example file and fill in your values
cp .env.example .env
# Edit .env with your actual credentials (never commit this file!)
```

### 2. CORS Configuration Restricted

**Issue**: ML API allowed requests from any origin (`allow_origins=["*"]`).

**Fix**:
- CORS now restricts to specific origins via `CORS_ORIGINS` environment variable
- Default to localhost for development
- Production deployments should set `CORS_ORIGINS` to your actual frontend domains

**Example**:
```bash
CORS_ORIGINS=https://app.hearloveen.com,https://admin.hearloveen.com
```

### 3. Authorization Checks Added

**Issue**: Missing authorization checks allowed users to modify data they shouldn't access.

**Fix**:
- Added authorization check in `ApplyFeedback` handler
- Therapists can only update curricula for children in their scope
- Admin users bypass scope restrictions

### 4. Input Validation

**Issue**: Missing validation allowed invalid data to enter the system.

**Fix**:
- Phoneme validation against known phoneme set
- Audio format validation
- Payload validation in AI worker

### 5. Secure Configuration Management

**Recommendations**:

1. **Use Azure Key Vault** for production secrets:
   ```csharp
   builder.Configuration.AddAzureKeyVault(
       new Uri($"https://{keyVaultName}.vault.azure.net/"),
       new DefaultAzureCredential());
   ```

2. **Enable HTTPS** in production:
   ```csharp
   app.UseHttpsRedirection();
   app.UseHsts();
   ```

3. **Set secure cookie options**:
   ```csharp
   services.AddAuthentication(options => {
       options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
       options.Cookie.HttpOnly = true;
       options.Cookie.SameSite = SameSiteMode.Strict;
   });
   ```

4. **Implement rate limiting** (see Program.cs TODO)

5. **Regular security audits**:
   ```bash
   # Run .NET security analysis
   dotnet list package --vulnerable

   # Run Python dependency check
   pip-audit

   # Run npm audit
   npm audit
   ```

### 6. Data Encryption

**Issue**: CryptoService implementation needs proper key management.

**Recommendations**:
- Store encryption keys in Azure Key Vault
- Implement key rotation policy
- Use different keys for different environments
- Never log or expose encrypted data

### 7. Database Security

**Recommendations**:
1. Use separate database users with minimal permissions for each service
2. Enable SSL/TLS for database connections
3. Implement row-level security in PostgreSQL
4. Regular backups with encryption at rest

### 8. Monitoring and Auditing

**Recommendations**:
1. Log all authentication attempts
2. Monitor for unusual patterns (e.g., many failed logins)
3. Set up alerts for security events
4. Implement audit trails for sensitive operations

### 9. Deployment Security Checklist

Before deploying to production:

- [ ] All secrets moved to Key Vault or secure secret management
- [ ] HTTPS enforced
- [ ] CORS properly configured
- [ ] Rate limiting enabled
- [ ] Database connections use SSL
- [ ] Authentication properly configured
- [ ] Authorization checks in place
- [ ] Input validation on all endpoints
- [ ] Logging and monitoring configured
- [ ] Security headers configured (HSTS, CSP, etc.)
- [ ] Dependencies updated and scanned for vulnerabilities
- [ ] Backup and disaster recovery plan in place

### 10. Incident Response

If you discover a security vulnerability:

1. **Do not** open a public GitHub issue
2. Contact the security team privately
3. Provide details about the vulnerability
4. Allow time for fix before public disclosure

---

## Additional Resources

- [OWASP Top 10](https://owasp.org/www-project-top-ten/)
- [ASP.NET Core Security](https://docs.microsoft.com/en-us/aspnet/core/security/)
- [Azure Security Best Practices](https://docs.microsoft.com/en-us/azure/security/fundamentals/best-practices-and-patterns)
