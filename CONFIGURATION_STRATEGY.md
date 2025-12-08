# Configuration Strategy

## Overview
This document defines where each type of configuration belongs to maintain security, flexibility, and clarity.

---

## 📋 Configuration Layers

### 1. **appsettings.json** - Application Defaults (Committed to Git)
**Purpose**: Static application behavior and framework configuration  
**Contains**: Non-secret, environment-agnostic settings  
**Never Contains**: Passwords, API keys, connection strings, emails

```json
{
  "Logging": { ... },
  "Chat": {
    "MaxToolCalls": 5,
    "MaxConversationHistory": 20,
    "MaxMessageLength": 10000,
    "SearchScoreThreshold": 0.7,
    "RagTopK": 3
  },
  "Resilience": { ... },
  "Cache": { ... },
  "IpRateLimiting": { ... },
  "Serilog": { ... },
  "HealthChecks": { ... }
}
```

**What Goes Here**:
- ✅ Logging levels
- ✅ Performance settings (cache, timeouts, retries)
- ✅ Rate limiting rules
- ✅ Static limits (message length, max conversations)
- ✅ Feature framework configuration

**What Does NOT Go Here**:
- ❌ Secrets (API keys, passwords)
- ❌ Environment-specific URLs
- ❌ User credentials
- ❌ Database connection strings

---

### 2. **Environment Variables / .env** - Deployment Secrets (NOT in Git)
**Purpose**: Secrets and environment-specific infrastructure  
**Contains**: Everything that changes per environment or contains sensitive data

```bash
# .env (NEVER commit this file)
# Database
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=...;Password=...

# Azure OpenAI
AZUREOPENAI__ENDPOINT=https://your-resource.openai.azure.com/
AZUREOPENAI__APIKEY=sk-xxxxxxxxxxxxx

# Email SMTP
EMAIL__SMTPHOST=smtp.gmail.com
EMAIL__SMTPPORT=587
EMAIL__USERNAME=your-email@gmail.com
EMAIL__PASSWORD=your-app-password
EMAIL__FROMEMAIL=notifications@yourcompany.com

# Qdrant
QDRANT__ENDPOINT=http://qdrant:6333

# API Keys for Authentication
APIKEYS__ADMIN-USER=secure-random-key-here
```

**What Goes Here**:
- ✅ Database connection strings (with passwords)
- ✅ API keys (Azure OpenAI, third-party services)
- ✅ SMTP credentials
- ✅ Service endpoints (different per environment)
- ✅ Authentication tokens
- ✅ Encryption keys

**What Does NOT Go Here**:
- ❌ Application logic settings
- ❌ User-configurable business rules
- ❌ UI/UX preferences

**File Structure**:
```
.env                 (NOT in git - actual secrets)
.env.example         (IN git - template with placeholders)
.gitignore           (must include .env)
```

---

### 3. **Database (AdminConfiguration Table)** - Runtime Business Logic
**Purpose**: Settings that business users can change via admin panel  
**Contains**: AI behavior, branding, feature toggles, user-facing limits

**Categories**:

#### **AI Settings** (User-Controlled)
- System Prompt
- Temperature, MaxTokens, TopP
- FrequencyPenalty, PresencePenalty
- Model Name

#### **RAG Settings** (Performance Tuning)
- Enabled/Disabled
- TopK Results
- Score Threshold
- Context Length
- Chunk Size & Overlap

#### **Features** (On/Off Switches)
- Enable File Upload
- Enable Export
- Enable Feedback
- Enable Email Tools
- Streaming Enabled
- Max Conversation History

#### **Security** (User Limits)
- Session Timeout
- Require Authentication
- Max Conversations Per User
- Max Message Length
- Rate Limit Per Minute
- Enable CORS

#### **Branding** (UI/UX)
- Application Name
- Company Name
- Welcome Message
- Theme Color
- Support Email
- Logo URL

**Why Database**:
- ✅ Changes take effect immediately (no restart)
- ✅ Non-technical users can modify via admin panel
- ✅ Audit trail (who changed what, when)
- ✅ Can be different per tenant (multi-tenancy ready)
- ✅ Can be rolled back via admin panel

---

## 🔒 Security Best Practices

### DO NOT Commit to Git:
- `.env` (actual secrets)
- `appsettings.Development.json` with real credentials
- `appsettings.Production.json` with real credentials

### DO Commit to Git:
- `appsettings.json` (template with empty secrets)
- `.env.example` (template with placeholders)
- `.gitignore` (listing secret files)

### Credential Storage:
**Development**:
- Use `.env` file or User Secrets (`dotnet user-secrets`)
- Add `.env` to `.gitignore`

**Production**:
- Use Azure Key Vault
- Use Docker Secrets
- Use Kubernetes Secrets
- Use CI/CD pipeline secret management

---

## 📂 File Organization

```
ChatAI.Api/
├── appsettings.json              (✅ Git) - Defaults
├── appsettings.Development.json  (⚠️ Git with NO SECRETS)
├── appsettings.Staging.json      (⚠️ Git with NO SECRETS)
├── appsettings.Production.json   (⚠️ Git with NO SECRETS)
├── .env                          (❌ Git) - YOUR secrets
├── .env.example                  (✅ Git) - Template
└── .gitignore                    (✅ Git) - Must exclude .env
```

---

## 🔄 Configuration Override Order

**.NET Configuration Priority** (later overrides earlier):
1. `appsettings.json`
2. `appsettings.{Environment}.json`
3. User Secrets (Development only)
4. Environment Variables
5. Command-line arguments

**Example**:
```bash
# appsettings.json has: "AzureOpenAI:Endpoint": ""
# Environment variable: AZUREOPENAI__ENDPOINT=https://prod.openai.com
# Result: Uses https://prod.openai.com (environment variable wins)
```

---

## 🛠️ Migration Checklist

### Phase 1: Clean appsettings.json Files
- [ ] Remove all secrets from `appsettings.json`
- [ ] Remove all secrets from `appsettings.Development.json`
- [ ] Remove all secrets from `appsettings.Production.json`
- [ ] Replace with empty strings or placeholders

### Phase 2: Create .env Template
- [ ] Create comprehensive `.env.example`
- [ ] Document all required environment variables
- [ ] Add clear comments for each variable

### Phase 3: Update .gitignore
- [ ] Add `.env` to `.gitignore`
- [ ] Verify no secrets in committed files
- [ ] Consider `git-secrets` tool for prevention

### Phase 4: Database Consolidation
- [ ] Remove deprecated `DefaultSystemPrompt` from appsettings
- [ ] Remove `AdminEmail` from appsettings (use database)
- [ ] Verify all dynamic settings in AdminConfiguration

### Phase 5: Documentation
- [ ] Update README with setup instructions
- [ ] Document environment variable requirements
- [ ] Create deployment guide

---

## 📝 Example Setup Flow

### Developer Onboarding:
```bash
# 1. Clone repo
git clone https://github.com/temo14/Chatify-AI.git
cd Chatify-AI

# 2. Copy environment template
cp .env.example .env

# 3. Fill in your credentials in .env
nano .env

# 4. Run application (loads .env automatically)
docker-compose up
# OR
dotnet run --project ChatAI.Api
```

### Production Deployment:
```bash
# 1. Set environment variables in hosting platform
#    (Azure App Service, AWS ECS, Kubernetes, etc.)

# 2. Deploy application (no .env file needed)

# 3. Configure runtime settings via admin panel
#    https://yourapp.com/admin.html → Configuration
```

---

## 🎯 Decision Tree: Where Does This Setting Go?

```
Is it a SECRET (password, API key)?
├─ YES → Environment Variable (.env)
└─ NO → Continue...

Does it change per deployment (dev/staging/prod)?
├─ YES → Environment Variable (.env)
└─ NO → Continue...

Should business users be able to change it at runtime?
├─ YES → Database (AdminConfiguration)
└─ NO → Continue...

Is it application framework configuration?
├─ YES → appsettings.json
└─ NO → You might not need it!
```

---

## 🔍 Current State vs Target State

### Before (❌ Insecure):
```json
// appsettings.Development.json (IN GIT!)
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=ChatifyAI_Dev;..."
  },
  "Email": {
    "Username": "t.baindurashvili.gm@gmail.com",
    "Password": "actual-password-here",  // ❌ LEAKED!
    "AdminEmail": "temo599922030@gmail.com"
  }
}
```

### After (✅ Secure):
```json
// appsettings.Development.json (IN GIT)
{
  "ConnectionStrings": {
    "DefaultConnection": ""  // From environment
  },
  "Email": {
    "SmtpHost": "smtp.gmail.com",
    "SmtpPort": 587,
    "EnableSsl": true,
    "Username": "",  // From environment
    "Password": "",  // From environment
    "TimeoutSeconds": 30
  }
}
```

```bash
# .env (NOT IN GIT)
CONNECTIONSTRINGS__DEFAULTCONNECTION=Server=localhost;Database=ChatifyAI_Dev;...
EMAIL__USERNAME=t.baindurashvili.gm@gmail.com
EMAIL__PASSWORD=your-app-password
EMAIL__FROMEMAIL=temo599922030@gmail.com
```

```sql
-- Database (Runtime configurable)
INSERT INTO AdminConfiguration VALUES
('Email.AdminEmail', 't.baindurashvili.gm@gmail.com', ...);
```

---

## 📚 References

- [ASP.NET Core Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/)
- [User Secrets in Development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets)
- [Azure Key Vault](https://learn.microsoft.com/en-us/azure/key-vault/general/overview)
- [12-Factor App Config](https://12factor.net/config)
