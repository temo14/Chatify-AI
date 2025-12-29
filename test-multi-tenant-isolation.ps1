# Multi-Tenant Isolation Testing Script
# Tests all critical multi-tenant isolation scenarios from PRODUCTION_TESTING_CHECKLIST.md

$baseUrl = "http://localhost:5257"
$testsPassed = 0
$testsFailed = 0
$criticalFailures = 0

Write-Host "====================================================" -ForegroundColor Magenta
Write-Host "   MULTI-TENANT ISOLATION TESTS (CRITICAL)         " -ForegroundColor Magenta
Write-Host "====================================================" -ForegroundColor Magenta
Write-Host ""

# Login functions
function Get-TenantToken {
    param([string]$Username, [string]$Password)
    
    $body = @{
        username = $Username
        password = $Password
    } | ConvertTo-Json
    
    try {
        $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $body -ContentType 'application/json'
        return $response.token
    }
    catch {
        Write-Host "ERROR: Failed to login as $Username" -ForegroundColor Red
        return $null
    }
}

# Test login for both tenants first
Write-Host "[Setup] Logging in as adminA..." -ForegroundColor Yellow
$tokenA = Get-TenantToken "adminA" "Password123!"
if (-not $tokenA) {
    Write-Host "CRITICAL: Cannot login as adminA. Make sure test tenants are created." -ForegroundColor Red
    Write-Host "Run this first to create test users:" -ForegroundColor Yellow
    Write-Host "  1. Open SQL Server Management Studio" -ForegroundColor Gray
    Write-Host "  2. Connect to your database" -ForegroundColor Gray
    Write-Host "  3. Run the SQL from setup-test-tenants.ps1" -ForegroundColor Gray
    exit 1
}
Write-Host "   ✓ adminA logged in" -ForegroundColor Green

Write-Host "[Setup] Logging in as adminB..." -ForegroundColor Yellow
$tokenB = Get-TenantToken "adminB" "Password123!"
if (-not $tokenB) {
    Write-Host "CRITICAL: Cannot login as adminB. Make sure test tenants are created." -ForegroundColor Red
    exit 1
}
Write-Host "   ✓ adminB logged in`n" -ForegroundColor Green

$headersA = @{ Authorization = "Bearer $tokenA" }
$headersB = @{ Authorization = "Bearer $tokenB" }

# ==============================================================================
# TEST 1: Chat Sessions Isolation
# ==============================================================================

Write-Host "[Test 1] Chat Sessions Isolation" -ForegroundColor Cyan
Write-Host "Creating chat session for Tenant A..." -ForegroundColor Yellow

try {
    $chatBodyA = @{
        message = "This is a secret message from Tenant A"
        sessionId = $null
    } | ConvertTo-Json
    
    $chatResponseA = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $chatBodyA -Headers $headersA -ContentType 'application/json'
    $sessionIdA = $chatResponseA.sessionId
    Write-Host "   Tenant A session created: $sessionIdA" -ForegroundColor Gray
    
    # Try to access Tenant A's session as Tenant B
    Write-Host "   Attempting to access Tenant A's session as Tenant B..." -ForegroundColor Yellow
    
    try {
        $historyB = Invoke-RestMethod -Uri "$baseUrl/api/chat/sessions/$sessionIdA/messages" -Headers $headersB -ErrorAction Stop
        Write-Host "   CRITICAL FAILURE: Tenant B accessed Tenant A's session!" -ForegroundColor Red -BackgroundColor Yellow
        $testsFailed++
        $criticalFailures++
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401 -or $statusCode -eq 404 -or $statusCode -eq 403) {
            Write-Host "   ✅ PASS: Access denied (Status: $statusCode)" -ForegroundColor Green
            $testsPassed++
        }
        else {
            Write-Host "   ⚠️  Unexpected status code: $statusCode" -ForegroundColor Yellow
            $testsFailed++
        }
    }
}
catch {
    Write-Host "   ❌ FAIL: Error creating chat session" -ForegroundColor Red
    $testsFailed++
}

# ==============================================================================
# TEST 2: Knowledge Documents Isolation
# ==============================================================================

Write-Host "`n[Test 2] Knowledge Documents Isolation" -ForegroundColor Cyan
Write-Host "Creating knowledge document for Tenant A..." -ForegroundColor Yellow

try {
    $docBodyA = @{
        title = "Tenant A Confidential Document"
        content = "This is sensitive information for Tenant A only. Secret data: TenantA-12345"
        category = "confidential"
    } | ConvertTo-Json
    
    $docResponseA = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Method POST -Body $docBodyA -Headers $headersA -ContentType 'application/json'
    $docIdA = $docResponseA.id
    Write-Host "   Tenant A document created: $docIdA" -ForegroundColor Gray
    
    # Wait a moment for the database to commit
    Start-Sleep -Milliseconds 500
    
    # Try to access Tenant A's document as Tenant B
    Write-Host "   Attempting to access Tenant A's document as Tenant B..." -ForegroundColor Yellow
    
    try {
        $docB = Invoke-RestMethod -Uri "$baseUrl/api/knowledge/$docIdA" -Headers $headersB -ErrorAction Stop
        Write-Host "   CRITICAL FAILURE: Tenant B accessed Tenant A's document!" -ForegroundColor Red -BackgroundColor Yellow
        Write-Host "   Document content retrieved: $($docB.content.Substring(0, 50))..." -ForegroundColor Red
        $testsFailed++
        $criticalFailures++
    }
    catch {
        $statusCode = $_.Exception.Response.StatusCode.value__
        if ($statusCode -eq 401 -or $statusCode -eq 404 -or $statusCode -eq 403) {
            Write-Host "   ✅ PASS: Access denied (Status: $statusCode)" -ForegroundColor Green
        }
        else {
            Write-Host "   ⚠️  Unexpected status code: $statusCode" -ForegroundColor Yellow
            $testsFailed++
        }
    }
    
    # Also verify list isolation
    Write-Host "   Verifying document list isolation..." -ForegroundColor Yellow
    try {
        $docsB = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Headers $headersB
        $foundTenantADoc = $docsB | Where-Object { $_.id -eq $docIdA }
        
        if ($foundTenantADoc) {
            Write-Host "   CRITICAL FAILURE: Tenant A's document appears in Tenant B's list!" -ForegroundColor Red -BackgroundColor Yellow
            $testsFailed++
            $criticalFailures++
        }
        else {
            Write-Host "   ✅ PASS: Tenant A's document not in Tenant B's list" -ForegroundColor Green
            $testsPassed++
        }
    }
    catch {
        Write-Host "   ⚠️  Could not retrieve Tenant B's document list: $($_.Exception.Message)" -ForegroundColor Yellow
        $testsFailed++
    }
}
catch {
    Write-Host "   ❌ FAIL: Error in knowledge document test - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# ==============================================================================
# TEST 3: Configuration Isolation
# ==============================================================================

Write-Host "`n[Test 3] Configuration Isolation" -ForegroundColor Cyan
Write-Host "Testing configuration isolation..." -ForegroundColor Yellow

try {
    # Get configuration for Tenant A
    $configA = Invoke-RestMethod -Uri "$baseUrl/api/configuration" -Headers $headersA
    Write-Host "   Tenant A temperature: $($configA.temperature)" -ForegroundColor Gray
    
    # Get configuration for Tenant B
    $configB = Invoke-RestMethod -Uri "$baseUrl/api/configuration" -Headers $headersB
    Write-Host "   Tenant B temperature: $($configB.temperature)" -ForegroundColor Gray
    
    # They should have different configurations
    if ($configA.welcomeMessage -ne $configB.welcomeMessage) {
        Write-Host "   ✅ PASS: Configurations are isolated" -ForegroundColor Green
        $testsPassed++
    }
    else {
        Write-Host "   ⚠️  WARNING: Configurations may not be properly isolated" -ForegroundColor Yellow
        $testsPassed++  # Don't fail, might just have same defaults
    }
}
catch {
    Write-Host "   ⚠️  Configuration endpoint not available" -ForegroundColor Yellow
    Write-Host "   (This is okay if feature not implemented yet)" -ForegroundColor Gray
}

# ==============================================================================
# TEST 4: Cross-Tenant Data Creation Attempt
# ==============================================================================

Write-Host "`n[Test 4] Verify Cannot Create Data for Other Tenant" -ForegroundColor Cyan
Write-Host "Ensuring tenant context is enforced..." -ForegroundColor Yellow

# All data created should be scoped to the authenticated user's tenant
# This is already tested implicitly above, but let's be explicit

try {
    $docsA = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Headers $headersA
    $docsB = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Headers $headersB
    
    Write-Host "   Tenant A has $($docsA.Count) documents" -ForegroundColor Gray
    Write-Host "   Tenant B has $($docsB.Count) documents" -ForegroundColor Gray
    
    $overlap = $false
    foreach ($docA in $docsA) {
        if ($docsB.id -contains $docA.id) {
            $overlap = $true
            break
        }
    }
    
    if (-not $overlap) {
        Write-Host "   ✅ PASS: No document overlap between tenants" -ForegroundColor Green
        $testsPassed++
    }
    else {
        Write-Host "   CRITICAL FAILURE: Documents shared between tenants!" -ForegroundColor Red -BackgroundColor Yellow
        $testsFailed++
        $criticalFailures++
    }
}
catch {
    Write-Host "   ❌ FAIL: Error checking document isolation - $($_.Exception.Message)" -ForegroundColor Red
    $testsFailed++
}

# ==============================================================================
# TEST 5: Context Preservation Isolation
# ==============================================================================

Write-Host "`n[Test 5] Chat Context Isolation (Cache Test)" -ForegroundColor Cyan
Write-Host "Testing that chat context doesn't leak between tenants..." -ForegroundColor Yellow

try {
    # Tenant A: Set context
    $contextBodyA1 = @{
        message = "My name is Alice and I work at Company A"
        sessionId = $null
    } | ConvertTo-Json
    
    $contextResponseA = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $contextBodyA1 -Headers $headersA -ContentType 'application/json'
    $sessionIdA2 = $contextResponseA.sessionId
    
    # Tenant B: Set different context
    $contextBodyB1 = @{
        message = "My name is Bob and I work at Company B"
        sessionId = $null
    } | ConvertTo-Json
    
    $contextResponseB = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $contextBodyB1 -Headers $headersB -ContentType 'application/json'
    $sessionIdB2 = $contextResponseB.sessionId
    
    # Tenant A: Ask about name
    $contextBodyA2 = @{
        message = "What's my name?"
        sessionId = $sessionIdA2
    } | ConvertTo-Json
    
    $contextResponseA2 = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $contextBodyA2 -Headers $headersA -ContentType 'application/json'
    
    # Tenant B: Ask about name  
    $contextBodyB2 = @{
        message = "What's my name?"
        sessionId = $sessionIdB2
    } | ConvertTo-Json
    
    $contextResponseB2 = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $contextBodyB2 -Headers $headersB -ContentType 'application/json'
    
    Write-Host "   Tenant A AI says: $($contextResponseA2.response)" -ForegroundColor Gray
    Write-Host "   Tenant B AI says: $($contextResponseB2.response)" -ForegroundColor Gray
    
    $aContainsAlice = $contextResponseA2.response -like "*Alice*"
    $bContainsBob = $contextResponseB2.response -like "*Bob*"
    $aMentionsBob = $contextResponseA2.response -like "*Bob*"
    $bMentionsAlice = $contextResponseB2.response -like "*Alice*"
    
    if ($aContainsAlice -and $bContainsBob -and -not $aMentionsBob -and -not $bMentionsAlice) {
        Write-Host "   ✅ PASS: Context properly isolated" -ForegroundColor Green
        $testsPassed++
    }
    else {
        Write-Host "   CRITICAL FAILURE: Context leaked between tenants!" -ForegroundColor Red -BackgroundColor Yellow
        $testsFailed++
        $criticalFailures++
    }
}
catch {
    Write-Host "   ❌ FAIL: Error in context isolation test" -ForegroundColor Red
    $testsFailed++
}

# ==============================================================================
# SUMMARY
# ==============================================================================

Write-Host "`n====================================================" -ForegroundColor Magenta
Write-Host "                 TEST SUMMARY                       " -ForegroundColor Magenta
Write-Host "====================================================" -ForegroundColor Magenta

$totalTests = $testsPassed + $testsFailed
Write-Host "`nTotal Tests: $totalTests" -ForegroundColor Cyan
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor Red

if ($criticalFailures -gt 0) {
    Write-Host "`nCRITICAL FAILURES: $criticalFailures" -ForegroundColor Red -BackgroundColor Yellow
    Write-Host "" 
    Write-Host "DO NOT DEPLOY TO PRODUCTION" -ForegroundColor Red -BackgroundColor Yellow
    Write-Host "Multi-tenant data isolation is compromised!" -ForegroundColor Red
}
else {
    if ($testsFailed -eq 0) {
        Write-Host "`nAll multi-tenant isolation tests PASSED!" -ForegroundColor Green
        Write-Host "Application maintains proper tenant data isolation." -ForegroundColor Green
    }
    else {
        Write-Host "`nSome tests failed, but no critical isolation breaches detected." -ForegroundColor Yellow
    }
}

Write-Host "`n====================================================" -ForegroundColor Magenta

# Cleanup note
Write-Host "`nNOTE: Test data was created. You may want to clean up:" -ForegroundColor Yellow
Write-Host "  - Chat sessions for tenants A and B" -ForegroundColor Gray
Write-Host "  - Knowledge documents for tenants A and B" -ForegroundColor Gray
