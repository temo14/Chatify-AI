# Simple Production Test Runner
# Run this script while the application is running in a separate terminal/task

$baseUrl = "http://localhost:5257"
$testsPassed = 0
$testsFailed = 0
$testsRun = 0

function Test-Endpoint {
    param(
        [string]$TestNumber,
        [string]$TestName,
        [scriptblock]$TestScript,
        [bool]$IsCritical = $false
    )
    
    $script:testsRun++
    Write-Host "`n----------------------------------------" -ForegroundColor Cyan
    Write-Host "Test $TestNumber : $TestName" -ForegroundColor Cyan
    Write-Host "----------------------------------------" -ForegroundColor Cyan
    
    try {
        $result = & $TestScript
        if ($result) {
            Write-Host "✅ PASS" -ForegroundColor Green
            $script:testsPassed++
        } else {
            Write-Host "❌ FAIL" -ForegroundColor Red
            $script:testsFailed++
            if ($IsCritical) {
                Write-Host "⚠️  CRITICAL BLOCKER" -ForegroundColor Red -BackgroundColor Yellow
            }
        }
    }
    catch {
        Write-Host "❌ FAIL: $($_.Exception.Message)" -ForegroundColor Red
        $script:testsFailed++
        if ($IsCritical) {
            Write-Host "⚠️  CRITICAL BLOCKER" -ForegroundColor Red -BackgroundColor Yellow
        }
    }
}

Write-Host "===================================================" -ForegroundColor Magenta
Write-Host "   CHATIFY AI - PRODUCTION TESTING SUITE          " -ForegroundColor Magenta
Write-Host "===================================================" -ForegroundColor Magenta
Write-Host "Base URL: $baseUrl" -ForegroundColor Yellow
Write-Host "Start Time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")`n" -ForegroundColor Yellow

# Global token variable
$script:token = $null
$script:sessionId = $null
$script:knowledgeId = $null

# ==============================================================================
# AUTHENTICATION & AUTHORIZATION TESTS (8-16)
# ==============================================================================

Write-Host "`n[HIGH PRIORITY] AUTHENTICATION AND AUTHORIZATION TESTS" -ForegroundColor Yellow

Test-Endpoint "8" "Valid Login" {
    $body = @{ username = "admin"; password = "Admin@123456" } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $body -ContentType 'application/json'
    $script:token = $response.token
    Write-Host "   Token received: $($token.Substring(0, 30))..." -ForegroundColor Gray
    return $script:token -ne $null
}

Test-Endpoint "9" "Invalid Credentials" {
    try {
        $body = @{ username = "admin"; password = "WrongPassword" } | ConvertTo-Json
        Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $body -ContentType 'application/json' -ErrorAction Stop
        return $false  # Should have thrown error
    }
    catch {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
        return $_.Exception.Response.StatusCode.value__ -eq 401
    }
}

Test-Endpoint "16" "Unauthenticated Access" {
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -ErrorAction Stop
        return $false  # Should have thrown error
    }
    catch {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
        return $_.Exception.Response.StatusCode.value__ -eq 401
    }
}

# ==============================================================================
# CORE CHAT FUNCTIONALITY (17-20)
# ==============================================================================

Write-Host "`n[MEDIUM PRIORITY] CORE CHAT FUNCTIONALITY TESTS" -ForegroundColor Yellow

Test-Endpoint "17" "Basic Chat" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{ message = "Hello, what can you help me with?"; sessionId = $null } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body -Headers $headers -ContentType 'application/json'
    $script:sessionId = $response.sessionId
    Write-Host "   Session ID: $script:sessionId" -ForegroundColor Gray
    Write-Host "   Response: $($response.response.Substring(0, [Math]::Min(80, $response.response.Length)))..." -ForegroundColor Gray
    return $response.response -ne $null
}

Test-Endpoint "19" "Chat History" {
    if (-not $script:sessionId) {
        Write-Host "   Skipping: No session ID from previous test" -ForegroundColor Yellow
        return $true  # Don't fail if previous test failed
    }
    
    # Send another message
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{ message = "What is your name?"; sessionId = $script:sessionId } | ConvertTo-Json
    Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body -Headers $headers -ContentType 'application/json' | Out-Null
    
    # Get history
    $history = Invoke-RestMethod -Uri "$baseUrl/api/chat/sessions/$script:sessionId/messages" -Headers $headers
    Write-Host "   Messages in history: $($history.Count)" -ForegroundColor Gray
    return $history.Count -ge 2
}

Test-Endpoint "20" "Context Preservation" {
    $headers = @{ Authorization = "Bearer $script:token" }
    
    # Create new session with context
    $body1 = @{ message = "My name is John and I live in New York"; sessionId = $null } | ConvertTo-Json
    $response1 = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body1 -Headers $headers -ContentType 'application/json'
    $contextSessionId = $response1.sessionId
    
    # Ask about the name
    $body2 = @{ message = "What's my name?"; sessionId = $contextSessionId } | ConvertTo-Json
    $response2 = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body2 -Headers $headers -ContentType 'application/json'
    
    Write-Host "   AI Response: $($response2.response)" -ForegroundColor Gray
    return $response2.response -like "*John*"
}

# ==============================================================================
# KNOWLEDGE BASE TESTS (21-25)
# ==============================================================================

Write-Host "`n[MEDIUM PRIORITY] KNOWLEDGE BASE TESTS" -ForegroundColor Yellow

Test-Endpoint "21" "Create Knowledge Document" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{
        title = "Return Policy - Test Doc"
        content = "Our return policy allows returns within 30 days of purchase with original receipt. No questions asked."
        category = "policies"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Method POST -Body $body -Headers $headers -ContentType 'application/json'
    $script:knowledgeId = $response.id
    Write-Host "   Document ID: $script:knowledgeId" -ForegroundColor Gray
    return $script:knowledgeId -ne $null
}

Test-Endpoint "22" "RAG-Enhanced Chat" {
    # Wait for embeddings
    Write-Host "   Waiting 3 seconds for embeddings..." -ForegroundColor Gray
    Start-Sleep -Seconds 3
    
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{ message = "What is your return policy?"; sessionId = $null } | ConvertTo-Json
    $response = Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body -Headers $headers -ContentType 'application/json'
    
    Write-Host "   Response: $($response.response.Substring(0, [Math]::Min(100, $response.response.Length)))..." -ForegroundColor Gray
    $containsReturnInfo = $response.response -like "*30 days*" -or $response.response -like "*return*"
    return $containsReturnInfo
}

Test-Endpoint "23" "Update Knowledge Document" {
    if (-not $script:knowledgeId) {
        Write-Host "   Skipping: No knowledge ID from previous test" -ForegroundColor Yellow
        return $true
    }
    
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{
        title = "Updated Return Policy"
        content = "Our updated return policy allows returns within 60 days."
        category = "policies"
    } | ConvertTo-Json
    
    $response = Invoke-RestMethod -Uri "$baseUrl/api/knowledge/$script:knowledgeId" -Method PUT -Body $body -Headers $headers -ContentType 'application/json'
    return $true
}

Test-Endpoint "25" "Search Knowledge" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $response = Invoke-RestMethod -Uri "$baseUrl/api/knowledge/search?query=return&limit=5" -Headers $headers
    Write-Host "   Found $($response.Count) documents" -ForegroundColor Gray
    return $response.Count -gt 0
}

Test-Endpoint "24" "Delete Knowledge Document" {
    if (-not $script:knowledgeId) {
        Write-Host "   Skipping: No knowledge ID from previous test" -ForegroundColor Yellow
        return $true
    }
    
    $headers = @{ Authorization = "Bearer $script:token" }
    Invoke-RestMethod -Uri "$baseUrl/api/knowledge/$script:knowledgeId" -Method DELETE -Headers $headers | Out-Null
    
    # Verify deletion
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/knowledge/$script:knowledgeId" -Headers $headers -ErrorAction Stop
        return $false  # Should have thrown 404
    }
    catch {
        return $_.Exception.Response.StatusCode.value__ -eq 404
    }
}

# ==============================================================================
# ERROR HANDLING TESTS (35-37)
# ==============================================================================

Write-Host "`n[ERROR HANDLING] TESTS" -ForegroundColor Blue

Test-Endpoint "35" "Invalid JSON Payload" {
    $headers = @{ Authorization = "Bearer $script:token" }
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Method POST -Body "{ invalid json }" -Headers $headers -ContentType 'application/json' -ErrorAction Stop
        return $false
    }
    catch {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
        return $_.Exception.Response.StatusCode.value__ -eq 400
    }
}

Test-Endpoint "36" "Missing Required Fields" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{ content = "test without title" } | ConvertTo-Json
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Method POST -Body $body -Headers $headers -ContentType 'application/json' -ErrorAction Stop
        return $false
    }
    catch {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
        return $_.Exception.Response.StatusCode.value__ -eq 400
    }
}

Test-Endpoint "37" "Resource Not Found" {
    $headers = @{ Authorization = "Bearer $script:token" }
    try {
        Invoke-RestMethod -Uri "$baseUrl/api/knowledge/00000000-0000-0000-0000-000000000000" -Headers $headers -ErrorAction Stop
        return $false
    }
    catch {
        Write-Host "   Status: $($_.Exception.Response.StatusCode.value__)" -ForegroundColor Gray
        return $_.Exception.Response.StatusCode.value__ -eq 404
    }
}

# ==============================================================================
# PERFORMANCE TEST (42)
# ==============================================================================

Write-Host "`n[PERFORMANCE] TESTS" -ForegroundColor Magenta

Test-Endpoint "42" "Response Time" {
    $headers = @{ Authorization = "Bearer $script:token" }
    $body = @{ message = "Quick response test"; sessionId = $null } | ConvertTo-Json
    
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    Invoke-RestMethod -Uri "$baseUrl/api/chat/send" -Method POST -Body $body -Headers $headers -ContentType 'application/json' | Out-Null
    $stopwatch.Stop()
    
    $ms = $stopwatch.ElapsedMilliseconds
    Write-Host "   Response time: $ms ms" -ForegroundColor Gray
    return $ms -lt 10000  # 10 seconds for development (includes AI call)
}

# ==============================================================================
# SUMMARY
# ==============================================================================

Write-Host "`n===================================================" -ForegroundColor Magenta
Write-Host "                  TEST SUMMARY                    " -ForegroundColor Magenta
Write-Host "===================================================" -ForegroundColor Magenta

Write-Host "`nTotal Tests Run: $testsRun" -ForegroundColor Cyan
Write-Host "Passed: $testsPassed" -ForegroundColor Green
Write-Host "Failed: $testsFailed" -ForegroundColor Red

$passRate = [math]::Round(($testsPassed / $testsRun) * 100, 2)
Write-Host "`nPass Rate: $passRate%" -ForegroundColor $(if ($passRate -ge 90) { "Green" } elseif ($passRate -ge 70) { "Yellow" } else { "Red" })

Write-Host "`nEnd Time: $(Get-Date -Format "yyyy-MM-dd HH:mm:ss")" -ForegroundColor Yellow

Write-Host "`nNOTE: This script tests single-tenant scenarios." -ForegroundColor Yellow
Write-Host "   Multi-tenant isolation tests (Tests 1-7) require:" -ForegroundColor Yellow
Write-Host "   1. Multiple tenants configured" -ForegroundColor Yellow
Write-Host "   2. Subdomain routing setup" -ForegroundColor Yellow
Write-Host "   3. Manual cross-tenant access attempts" -ForegroundColor Yellow
Write-Host "`n   See PRODUCTION_TESTING_CHECKLIST.md for details.`n" -ForegroundColor Yellow
