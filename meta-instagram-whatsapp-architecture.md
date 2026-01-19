# 📘 Meta Instagram & WhatsApp Integration Architecture
## (Post-Facebook OAuth)

---

## 0. Purpose of This Document

This document defines **how Instagram and WhatsApp integrations must work** within the existing system **after Facebook OAuth is already implemented and verified as correct**.

This is a **planning and architectural specification**, not an implementation guide.

The goal is to ensure:
- Full alignment with the existing project architecture
- Secure, multi-tenant operation
- No accidental complexity or parallel integration paths
- Production-grade correctness (not MVP shortcuts)

---

## 1. System Goal & Core Invariants

### 1.1 System Goal

The system is a **multi-tenant SaaS platform** that allows tenants to:

- Securely connect external messaging channels
- Receive and send messages through a **single unified chat pipeline**
- Maintain strict tenant isolation, reliability, and auditability

Instagram and WhatsApp are **first-class channels**, not special cases.

---

### 1.2 Non-Negotiable System Invariants

These rules must **never be violated**:

1. **Tenant Isolation**
   - Every inbound and outbound operation executes under exactly one tenant
   - Tokens, connections, and conversations are never shared across tenants

2. **Asynchronous Webhook Processing**
   - Webhooks are acknowledged immediately
   - All processing occurs in background workers

3. **Single Chat Pipeline**
   - All inbound messages flow through the existing chat pipeline
   - No channel-specific shortcuts are allowed

4. **Channel Isolation**
   - Channel-specific rules live only in channel-specific infrastructure
   - Application layer remains channel-agnostic

5. **Security First**
   - Tokens encrypted at rest
   - OAuth and webhook signatures always validated
   - No sensitive data logged

---

## 2. Architectural Context (Must Be Understood First)

### 2.1 Layered Architecture

| Layer | Responsibility |
|-----|---------------|
| Controllers | HTTP handling only |
| Application | Orchestration, validation, workflows |
| Infrastructure | External APIs, persistence, encryption |
| Background | Async webhook processing |
| Domain | Chat sessions and messages |

---

### 2.2 Multi-Tenancy Model

- Tenant resolved via `TenantResolutionMiddleware`
- `ITenantContext` enforces tenant scope
- EF Core global filters ensure isolation
- **Exception**: inbound webhooks resolve tenant *after* identifying connection

---

## 3. Shared Meta Foundation (Already Implemented)

Instagram and WhatsApp **must reuse** the existing Meta foundation.

### 3.1 Fully Reusable Components

- OAuth initiation & completion flow
- OAuth state signing and replay protection
- Token exchange (short-lived → long-lived)
- Token encryption and storage
- Unified webhook controller
- Signature validation
- Background queue infrastructure
- Deduplication logic
- Chat pipeline integration
- Resilience policies (retry, circuit breaker, timeout)

❗ No parallel OAuth systems are allowed.

---

## 4. Integration Lifecycle (Applies to All Channels)

Each channel connection follows the same lifecycle:

1. Disconnected  
2. Connected (Unvalidated)  
3. Active  
4. Degraded (token or permission issue)  
5. Revoked / Disconnected  

State transitions must be explicit and auditable.

---

## 5. Instagram Integration Specification

### 5.1 Connection Model

- Instagram Business accounts **must be linked to a Facebook Page**
- One connection represents:
  - One tenant
  - One Instagram Business Account
  - One Facebook Page
  - One encrypted token

---

### 5.2 OAuth Flow

1. Tenant initiates “Connect Instagram”
2. OAuth initiated with Instagram + Page scopes
3. User authorizes and selects Facebook Page
4. OAuth completion:
   - Validate state
   - Exchange token
   - Fetch Page → Instagram Business Account
   - Validate permissions
   - Create connection

If no Instagram Business account is linked → **fail explicitly**

---

### 5.3 Required Permissions

| Scope | Purpose |
|-----|--------|
| instagram_basic | Account validation |
| instagram_manage_messages | Messaging |
| pages_show_list | Page → IG discovery |

Missing required scopes → connection **must not activate**

---

### 5.4 Inbound Messaging

- Delivered via unified webhook endpoint
- Signature validated (HMAC-SHA256)
- Deduplicated using Meta message ID
- Enqueued for background processing

Processing flow:
1. Resolve connection (bypass tenant filter)
2. Set tenant context
3. Map external user → chat session
4. Persist inbound message
5. Pass to chat pipeline
6. Send reply via Instagram client

---

### 5.5 Outbound Messaging

- Always sent via `IMetaInstagramClient`
- Application layer never calls Meta APIs directly
- Client enforces:
  - Character limits
  - Retry rules
  - Error classification

Fatal token errors disable connection.

---

## 6. WhatsApp Integration Specification

### 6.1 Account Model

- Hierarchy:
  - Business Manager → WABA → Phone Numbers
- One connection per phone number
- Identity = WhatsApp Phone Number ID

---

### 6.2 OAuth & Verification

1. Tenant initiates “Connect WhatsApp”
2. OAuth grants access to WABA + phone numbers
3. Phone numbers fetched
4. Verification status checked

Unverified numbers:
- Stored but inactive
- Auto-activated when verified

---

### 6.3 Messaging Rules (Critical)

**24-Hour Customer Care Window**

- Free-form messages allowed only within 24h of last inbound message
- Outside window → approved template required
- Enforced **before API call**

---

### 6.4 Inbound Messaging

- Unified webhook endpoint
- Signature validation
- Deduplication
- External user ID = `wa_id`
- FIFO processing per conversation

---

### 6.5 Outbound Messaging

- Sent only via `IMetaWhatsAppClient`
- Client handles:
  - Template vs free-form logic
  - Error codes
  - Blocked users
  - Rate limits

Permanent failures are not retried.

---

## 7. Idempotency & Reliability Rules

### 7.1 Deduplication

- Key: `(Channel, ConnectionId, MetaMessageId)`
- Stored with TTL (7 days)
- Duplicate events are acknowledged but not processed

---

### 7.2 Retry Strategy

| Error Type | Action |
|---------|-------|
| Network / 5xx | Retry |
| Permission revoked | Disable connection |
| Token expired | Disable connection |
| User blocked | Do not retry |

---

## 8. What Must Never Happen

- Webhooks processed synchronously
- Tokens logged or returned to UI
- Tenant context guessed or inferred
- Tokens reused across tenants
- Business logic inside controllers
- Meta APIs called from application layer

---

## 9. Review Checklist

Before approving any change, verify:

- [ ] Does this code assume tenant context?
- [ ] Does it touch OAuth or tokens?
- [ ] Does it process webhooks?
- [ ] Does it bypass the chat pipeline?
- [ ] Does it introduce channel-specific logic into shared layers?

If yes → re-evaluate the design.

---

## 10. Expected Outcome

After Instagram and WhatsApp integrations:

- All Meta channels behave consistently
- Tenants manage channels safely
- The system remains extensible
- No architectural drift is introduced
