# Secrets Management Setup Guide

## CRITICAL SECURITY: Never commit secrets to source control!

This guide explains how to properly manage secrets across all microservices.

---

## Development Setup (User Secrets)

### Prerequisites
- .NET CLI installed
- Project must have `UserSecretsId` in .csproj

### Step 1: Initialize User Secrets (One-time per service)

```bash
# Navigate to API project
cd backend/identity-service/Api
dotnet user-secrets init
# This creates a UserSecretsId in IdentityService.Api.csproj (already done)

# Repeat for each service:
cd backend/answer-service/Api
dotnet user-secrets init

cd backend/assessment-service/Api
dotnet user-secrets init

cd backend/candidate-service/Api
dotnet user-secrets init

cd backend/result-service/Api
dotnet user-secrets init
```

### Step 2: Set Development Secrets

```bash
# For Identity Service
cd backend/identity-service/Api
dotnet user-secrets set "Jwt:Key" "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=IdentityServiceDb;User Id=sa;Password=REPLACE_WITH_STRONG_PASSWORD;TrustServerCertificate=True"

# For Answer Service
cd backend/answer-service/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AnswerServiceDb;User Id=sa;Password=REPLACE_WITH_STRONG_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"

# For Assessment Service
cd backend/assessment-service/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=AssessmentServiceDb;User Id=sa;Password=REPLACE_WITH_STRONG_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"

# For Candidate Service
cd backend/candidate-service/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=CandidateServiceDb;User Id=sa;Password=REPLACE_WITH_STRONG_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"

# For Result Service
cd backend/result-service/Api
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost;Database=ResultServiceDb;User Id=sa;Password=REPLACE_WITH_STRONG_PASSWORD;TrustServerCertificate=True"
dotnet user-secrets set "Jwt:Key" "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"
```

### Step 3: Verify Secrets (List All)

```bash
cd backend/identity-service/Api
dotnet user-secrets list
```

### Step 4: Development Startup

Secrets are automatically loaded when running with `IHostEnvironment.IsDevelopment() == true`:

```bash
dotnet run
# or
dotnet watch
```

---

## Production Setup (Azure Key Vault)

### Prerequisites
- Azure subscription
- Azure CLI installed (`az login`)
- Azure Key Vault created

### Step 1: Create Azure Key Vault

```bash
az keyvault create \
  --name "assessmentportal-keyvault" \
  --resource-group "assessment-portal-rg" \
  --location "eastus"
```

### Step 2: Add Secrets to Key Vault

```bash
# JWT Key (same across all services)
az keyvault secret set \
  --vault-name "assessmentportal-keyvault" \
  --name "Jwt--Key" \
  --value "your-super-secret-256-bit-jwt-signing-key-here-minimum-32-chars"

# Identity Service
az keyvault secret set \
  --vault-name "assessmentportal-keyvault" \
  --name "IdentityService--ConnectionStrings--DefaultConnection" \
  --value "Server=identity-db.database.windows.net;Database=IdentityServiceDb;User Id=admin;Password=YourPassword123!;"

# Answer Service
az keyvault secret set \
  --vault-name "assessmentportal-keyvault" \
  --name "AnswerService--ConnectionStrings--DefaultConnection" \
  --value "Server=answer-db.database.windows.net;Database=AnswerServiceDb;User Id=admin;Password=YourPassword123!;"

# (Repeat for other services...)
```

### Step 3: Configure Managed Identity (Azure Container Instances / App Service)

```bash
# Assign managed identity to App Service
az webapp identity assign \
  --name "assessment-identity-service" \
  --resource-group "assessment-portal-rg"

# Grant Key Vault access
az keyvault set-policy \
  --name "assessmentportal-keyvault" \
  --object-id <managed-identity-object-id> \
  --secret-permissions get list
```

### Step 4: Update Program.cs for Production

```csharp
// Already configured in Program.cs - loaded automatically:
if (!builder.Environment.IsDevelopment())
{
    var keyVaultUrl = new Uri(builder.Configuration["KeyVault:Url"] 
        ?? throw new InvalidOperationException("KeyVault:Url not configured"));
    
    var credential = new DefaultAzureCredential();  // Uses managed identity
    builder.Configuration.AddAzureKeyVault(keyVaultUrl, credential);
}
```

### Step 5: Configure Environment Variables

Set in Azure deployment:

```
KeyVault:Url=https://assessmentportal-keyvault.vault.azure.net/
ASPNETCORE_ENVIRONMENT=Production
```

---

## Docker Deployment (Secure Secrets Injection)

### Do NOT use Dockerfile secrets (security anti-pattern!)

**❌ WRONG:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
ENV JWT_KEY="secret-hardcoded-here"  # INSECURE!
```

**✅ CORRECT:**
```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app/publish .

# Secrets injected at runtime via:
# - Environment variables
# - Docker secrets (Swarm mode)
# - Kubernetes secrets
# - Azure Key Vault

EXPOSE 80
ENTRYPOINT ["dotnet", "IdentityService.Api.dll"]
```

### Docker Compose with Secrets

```yaml
version: '3.8'

services:
  identity-service:
    image: assessment-portal/identity-service:latest
    environment:
      - Jwt__Key=${JWT_KEY}  # From host environment
      - ConnectionStrings__DefaultConnection=${IDENTITY_DB_CONNECTION}
      - ASPNETCORE_ENVIRONMENT=Production
    env_file:
      - .env.production  # ⚠️ .env.production must be in .gitignore
    secrets:
      - jwt_key
      - db_password

secrets:
  jwt_key:
    file: ./secrets/jwt_key.txt
  db_password:
    file: ./secrets/db_password.txt
```

### Kubernetes Deployment with Secrets

```yaml
apiVersion: v1
kind: Secret
metadata:
  name: assessment-portal-secrets
type: Opaque
data:
  jwt-key: base64-encoded-jwt-key
  db-password: base64-encoded-password

---
apiVersion: apps/v1
kind: Deployment
metadata:
  name: identity-service
spec:
  containers:
  - name: identity-service
    env:
    - name: Jwt__Key
      valueFrom:
        secretKeyRef:
          name: assessment-portal-secrets
          key: jwt-key
    - name: ConnectionStrings__DefaultConnection
      valueFrom:
        secretKeyRef:
          name: assessment-portal-secrets
          key: db-connection
```

---

## Verification Checklist

- [ ] No JWT keys in appsettings.json (only in user-secrets/Key Vault)
- [ ] No database passwords in source control
- [ ] No connection strings in appsettings.json
- [ ] User secrets initialized (`dotnet user-secrets init`)
- [ ] Development secrets set (`dotnet user-secrets set`)
- [ ] Production: Key Vault configured
- [ ] Production: Managed identity assigned
- [ ] .gitignore includes `.vs/`, `user-secrets/`, `*.key`
- [ ] CI/CD: Secrets passed as environment variables
- [ ] Docker: No secrets in Dockerfile ENV statements

---

## Troubleshooting

### Secrets not loading?

```bash
# Check if user-secrets is initialized
cd backend/identity-service/Api
cat IdentityService.Api.csproj | grep UserSecretsId

# List all configured secrets
dotnet user-secrets list

# Clear all and start over
dotnet user-secrets clear
dotnet user-secrets init
```

### Key Vault access denied?

```bash
# Verify managed identity has Key Vault permissions
az keyvault list-policies \
  --name "assessmentportal-keyvault"

# Grant access
az keyvault set-policy \
  --name "assessmentportal-keyvault" \
  --object-id <managed-identity-id> \
  --secret-permissions get list
```

### Local development connection issues?

```bash
# Verify connection string is correct
dotnet user-secrets get ConnectionStrings:DefaultConnection

# Test connection
sqlcmd -S localhost -U sa -P YourPassword -Q "SELECT @@VERSION"
```

---

## Security Best Practices

1. **Never commit secrets** - Use .gitignore for user-secrets folder
2. **Rotate secrets regularly** - Update Key Vault keys every 90 days
3. **Audit access** - Enable Key Vault logging to track secret access
4. **Principle of least privilege** - Only grant necessary permissions
5. **Use managed identities** - Avoid storing Azure credentials locally
6. **Monitor secrets** - Set up alerts for unauthorized access attempts

