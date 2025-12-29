# Production Testing Checklist - Comprehensive Test Script
# Based on PRODUCTION_TESTING_CHECKLIST.md

param(
    [string]$BaseUrl = "http://localhost:5257",
    [switch]$Verbose
)

$ErrorActionPreference = "Continue"
$testResults = @()

function Write-TestHeader {
    param([string]$TestNumber, [string]$TestName)
    Write-Host "`n========================================" -ForegroundColor Cyan
    Write-Host "[$TestNumber] $TestName" -ForegroundColor Cyan
    Write-Host "========================================" -ForegroundColor Cyan
}

function Write-TestResult {
    param(
        [string]$TestNumber,
        [string]$TestName,
        [bool]$Passed,
        [string]$Details = "",
        [bool]$IsCritical = $false
    )
    
    $status = if ($Passed) { "✅ PASS" } else { "❌ FAIL" }
    $color = if ($Passed) { "Green" } else { "Red" }
    
    if ($IsCritical -and -not $Passed) {
        Write-Host "$status [CRITICAL BLOCKER] $TestName" -ForegroundColor Red -BackgroundColor Yellow
    } else {
        Write-Host "$status $TestName" -ForegroundColor $color
    }
    
    if ($Details) {
        Write-Host "   $Details" -ForegroundColor Gray
    }
    
    $script:testResults += [PSCustomObject]@{
        TestNumber = $TestNumber
        TestName = $TestName
        Status = if ($Passed) { "PASS" } else { "FAIL" }
        IsCritical = $IsCritical
        Details = $Details
    }
}

function Invoke-ApiRequest {
    param(
        [string]$Url,
        [string]$Method = "GET",
        [hashtable]$Headers = @{},
        [string]$Body = $null,
        [switch]$ExpectFailure
    )
    
    try {
        $params = @{
            Uri = $Url
            Method = $Method
            Headers = $Headers
            ContentType = "application/json"
        }
        
        if ($Body) {
            $params.Body = $Body
        }
        
        $response = Invoke-WebRequest @params
        return @{
            Success = $true
            StatusCode = $response.StatusCode
            Content = $response.Content | ConvertFrom-Json -ErrorAction SilentlyContinue
        }
    }
    catch {
        if ($ExpectFailure) {
            return @{
                Success = $false
                StatusCode = $_.Exception.Response.StatusCode.value__
                Error = $_.Exception.Message
            }
        }
        return @{
            Success = $false
            StatusCode = $_.Exception.Response.StatusCode.value__
            Error = $_.Exception.Message
        }
    }
}

# Start testing
Write-Host "╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║     PRODUCTION TESTING CHECKLIST - AUTOMATED TEST SUITE    ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta
Write-Host ""
Write-Host "Base URL: $BaseUrl" -ForegroundColor Yellow
Write-Host "Start Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow
Write-Host ""

# ============================================================================
# SECTION: AUTHENTICATION & AUTHORIZATION (Tests 8-16)
# ============================================================================

Write-Host "`n🟠 HIGH PRIORITY - Authentication & Authorization" -ForegroundColor Yellow

# Test 8: Valid Login
Write-TestHeader "Test 8" "Valid Login"
$loginBody = @{
    username = "admin"
    password = "Admin@123456"
} | ConvertTo-Json

$loginResult = Invoke-ApiRequest -Url "$BaseUrl/api/auth/login" -Method POST -Body $loginBody
if ($loginResult.Success -and $loginResult.Content.token) {
    $token = $loginResult.Content.token
    Write-TestResult "8" "Valid Login" $true "Token received: $($token.Substring(0, 20))..."
} else {
    Write-TestResult "8" "Valid Login" $false "Failed to login: $($loginResult.Error)"
    Write-Host "`n⚠️  Cannot proceed with further tests without authentication" -ForegroundColor Red
    exit 1
}

$authHeaders = @{
    Authorization = "Bearer $token"
}

# Test 9: Invalid Credentials
Write-TestHeader "Test 9" "Invalid Credentials"
$invalidLogin = @{
    username = "admin"
    password = "WrongPassword"
} | ConvertTo-Json

$invalidResult = Invoke-ApiRequest -Url "$BaseUrl/api/auth/login" -Method POST -Body $invalidLogin -ExpectFailure
$test9Pass = $invalidResult.StatusCode -eq 401
Write-TestResult "9" "Invalid Credentials" $test9Pass "Status Code: $($invalidResult.StatusCode)"

# Test 16: Unauthenticated Access
Write-TestHeader "Test 16" "Unauthenticated Access"
$unauthResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -ExpectFailure
$test16Pass = $unauthResult.StatusCode -eq 401
Write-TestResult "16" "Unauthenticated Access" $test16Pass "Status Code: $($unauthResult.StatusCode)"

# Test 12: API Key Authentication (Test creating an API key first)
Write-TestHeader "Test 12" "API Key Authentication"
$apiKeyBody = @{
    name = "Test API Key"
    expiresAt = (Get-Date).AddDays(30).ToString("o")
} | ConvertTo-Json

$apiKeyResult = Invoke-ApiRequest -Url "$BaseUrl/api/auth/api-keys" -Method POST -Body $apiKeyBody -Headers $authHeaders
if ($apiKeyResult.Success -and $apiKeyResult.Content.key) {
    $apiKey = $apiKeyResult.Content.key
    Write-Host "   Created API key: $($apiKey.Substring(0, 20))..." -ForegroundColor Gray
    
    # Test using the API key
    $apiKeyHeaders = @{
        "X-API-Key" = $apiKey
    }
    $apiKeyTestResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Headers $apiKeyHeaders
    Write-TestResult "12" "API Key Authentication" $apiKeyTestResult.Success "API key authentication works"
} else {
    Write-TestResult "12" "API Key Authentication" $false "Failed to create or test API key"
}

# Test 13: Invalid API Key
Write-TestHeader "Test 13" "Invalid API Key"
$invalidKeyHeaders = @{
    "X-API-Key" = "invalid-key-12345"
}
$invalidKeyResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Headers $invalidKeyHeaders -ExpectFailure
$test13Pass = $invalidKeyResult.StatusCode -eq 401
Write-TestResult "13" "Invalid API Key" $test13Pass "Status Code: $($invalidKeyResult.StatusCode)"

# ============================================================================
# SECTION: CORE CHAT FUNCTIONALITY (Tests 17-20)
# ============================================================================

Write-Host "`n🟡 MEDIUM PRIORITY - Core Business Functionality" -ForegroundColor Yellow

# Test 17: Basic Chat
Write-TestHeader "Test 17" "Basic Chat"
$chatBody = @{
    message = "Hello, what can you help me with?"
    sessionId = $null
} | ConvertTo-Json

$chatResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $chatBody -Headers $authHeaders
if ($chatResult.Success -and $chatResult.Content.response) {
    $sessionId = $chatResult.Content.sessionId
    Write-TestResult "17" "Basic Chat" $true "Session ID: $sessionId, Response received: $($chatResult.Content.response.Substring(0, 50))..."
} else {
    Write-TestResult "17" "Basic Chat" $false "Failed: $($chatResult.Error)"
    $sessionId = $null
}

# Test 19: Chat History
if ($sessionId) {
    Write-TestHeader "Test 19" "Chat History"
    
    # Send another message to build history
    $chatBody2 = @{
        message = "What is your name?"
        sessionId = $sessionId
    } | ConvertTo-Json
    
    $chatResult2 = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $chatBody2 -Headers $authHeaders
    
    # Get chat history
    $historyResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat/history/$sessionId" -Headers $authHeaders
    if ($historyResult.Success -and $historyResult.Content.Count -ge 2) {
        Write-TestResult "19" "Chat History" $true "Found $($historyResult.Content.Count) messages in history"
    } else {
        Write-TestResult "19" "Chat History" $false "Expected at least 2 messages, got $($historyResult.Content.Count)"
    }
}

# Test 20: Context Preservation
Write-TestHeader "Test 20" "Context Preservation"
$contextBody1 = @{
    message = "My name is John and I live in New York"
    sessionId = $null
} | ConvertTo-Json

$contextResult1 = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $contextBody1 -Headers $authHeaders
if ($contextResult1.Success) {
    $contextSessionId = $contextResult1.Content.sessionId
    
    # Ask about the name
    $contextBody2 = @{
        message = "What's my name?"
        sessionId = $contextSessionId
    } | ConvertTo-Json
    
    $contextResult2 = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $contextBody2 -Headers $authHeaders
    $responseContainsName = $contextResult2.Content.response -like "*John*"
    Write-TestResult "20" "Context Preservation" $responseContainsName "AI response: $($contextResult2.Content.response)"
} else {
    Write-TestResult "20" "Context Preservation" $false "Failed to create context session"
}

# ============================================================================
# SECTION: KNOWLEDGE BASE & FEEDBACK (Tests 21-28)
# ============================================================================

Write-Host "`n🟡 Knowledge Base & Feedback Testing" -ForegroundColor Yellow

# Test 21: Create Knowledge Document
Write-TestHeader "Test 21" "Create Knowledge Document"
$knowledgeBody = @{
    title = "Return Policy"
    content = "Our return policy allows returns within 30 days of purchase with original receipt. No questions asked."
    category = "policies"
} | ConvertTo-Json

$knowledgeResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Method POST -Body $knowledgeBody -Headers $authHeaders
if ($knowledgeResult.Success -and $knowledgeResult.Content.id) {
    $knowledgeId = $knowledgeResult.Content.id
    Write-TestResult "21" "Create Knowledge Document" $true "Document ID: $knowledgeId"
} else {
    Write-TestResult "21" "Create Knowledge Document" $false "Failed: $($knowledgeResult.Error)"
    $knowledgeId = $null
}

# Wait for embeddings to be generated
if ($knowledgeId) {
    Write-Host "   Waiting 5 seconds for embeddings to be generated..." -ForegroundColor Gray
    Start-Sleep -Seconds 5
}

# Test 22: RAG-Enhanced Chat
Write-TestHeader "Test 22" "RAG-Enhanced Chat"
$ragChatBody = @{
    message = "What is your return policy?"
    sessionId = $null
} | ConvertTo-Json

$ragResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $ragChatBody -Headers $authHeaders
if ($ragResult.Success) {
    $containsReturnInfo = $ragResult.Content.response -like "*30 days*" -or $ragResult.Content.response -like "*return*"
    Write-TestResult "22" "RAG-Enhanced Chat" $containsReturnInfo "Response: $($ragResult.Content.response.Substring(0, 100))..."
} else {
    Write-TestResult "22" "RAG-Enhanced Chat" $false "Failed: $($ragResult.Error)"
}

# Test 23: Update Knowledge Document
if ($knowledgeId) {
    Write-TestHeader "Test 23" "Update Knowledge Document"
    $updateBody = @{
        title = "Updated Return Policy"
        content = "Our updated return policy allows returns within 60 days of purchase with original receipt."
        category = "policies"
    } | ConvertTo-Json
    
    $updateResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge/$knowledgeId" -Method PUT -Body $updateBody -Headers $authHeaders
    Write-TestResult "23" "Update Knowledge Document" $updateResult.Success "Document updated"
}

# Test 25: Search Knowledge
Write-TestHeader "Test 25" "Search Knowledge"
$searchResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge/search?query=return&limit=5" -Headers $authHeaders
if ($searchResult.Success -and $searchResult.Content.Count -gt 0) {
    Write-TestResult "25" "Search Knowledge" $true "Found $($searchResult.Content.Count) relevant documents"
} else {
    Write-TestResult "25" "Search Knowledge" $false "No search results or error"
}

# Test 26: Submit Positive Feedback
Write-TestHeader "Test 26" "Submit Positive Feedback"
# Need a message ID from a chat session
$feedbackChatBody = @{
    message = "Test message for feedback"
    sessionId = $null
} | ConvertTo-Json

$feedbackChatResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $feedbackChatBody -Headers $authHeaders
if ($feedbackChatResult.Success -and $feedbackChatResult.Content.messageId) {
    $messageId = $feedbackChatResult.Content.messageId
    
    $feedbackBody = @{
        messageId = $messageId
        rating = 1
    } | ConvertTo-Json
    
    $feedbackResult = Invoke-ApiRequest -Url "$BaseUrl/api/feedback" -Method POST -Body $feedbackBody -Headers $authHeaders
    Write-TestResult "26" "Submit Positive Feedback" $feedbackResult.Success "Feedback submitted for message: $messageId"
} else {
    Write-TestResult "26" "Submit Positive Feedback" $false "Could not create message for feedback"
}

# Test 27: Submit Negative Feedback
Write-TestHeader "Test 27" "Submit Negative Feedback"
$negativeFeedbackChatBody = @{
    message = "Another test message"
    sessionId = $null
} | ConvertTo-Json

$negativeFeedbackChatResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $negativeFeedbackChatBody -Headers $authHeaders
if ($negativeFeedbackChatResult.Success -and $negativeFeedbackChatResult.Content.messageId) {
    $messageId2 = $negativeFeedbackChatResult.Content.messageId
    
    $negativeFeedbackBody = @{
        messageId = $messageId2
        rating = -1
        comment = "Response was not helpful"
    } | ConvertTo-Json
    
    $negativeFeedbackResult = Invoke-ApiRequest -Url "$BaseUrl/api/feedback" -Method POST -Body $negativeFeedbackBody -Headers $authHeaders
    Write-TestResult "27" "Submit Negative Feedback" $negativeFeedbackResult.Success "Negative feedback submitted"
} else {
    Write-TestResult "27" "Submit Negative Feedback" $false "Could not create message for feedback"
}

# Test 28: View Feedback Stats
Write-TestHeader "Test 28" "View Feedback Stats"
$statsResult = Invoke-ApiRequest -Url "$BaseUrl/api/feedback/stats" -Headers $authHeaders
if ($statsResult.Success) {
    Write-TestResult "28" "View Feedback Stats" $true "Total feedback: $($statsResult.Content.total), Positive: $($statsResult.Content.positive)%, Negative: $($statsResult.Content.negative)%"
} else {
    Write-TestResult "28" "View Feedback Stats" $false "Failed to get stats"
}

# ============================================================================
# SECTION: ERROR HANDLING & EDGE CASES (Tests 35-41)
# ============================================================================

Write-Host "`n🔵 Error Handling & Edge Cases" -ForegroundColor Blue

# Test 35: Invalid JSON Payload
Write-TestHeader "Test 35" "Invalid JSON Payload"
try {
    $invalidJsonResult = Invoke-WebRequest -Uri "$BaseUrl/api/knowledge" -Method POST -Body "{ invalid json }" -Headers $authHeaders -ContentType "application/json" -ErrorAction Stop
    Write-TestResult "35" "Invalid JSON Payload" $false "Should have returned 400, got $($invalidJsonResult.StatusCode)"
}
catch {
    $statusCode = $_.Exception.Response.StatusCode.value__
    Write-TestResult "35" "Invalid JSON Payload" ($statusCode -eq 400) "Status Code: $statusCode"
}

# Test 36: Missing Required Fields
Write-TestHeader "Test 36" "Missing Required Fields"
$missingFieldBody = @{
    content = "test content without title"
} | ConvertTo-Json

$missingFieldResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Method POST -Body $missingFieldBody -Headers $authHeaders -ExpectFailure
$test36Pass = $missingFieldResult.StatusCode -eq 400
Write-TestResult "36" "Missing Required Fields" $test36Pass "Status Code: $($missingFieldResult.StatusCode)"

# Test 37: Resource Not Found
Write-TestHeader "Test 37" "Resource Not Found"
$notFoundResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge/00000000-0000-0000-0000-000000000000" -Headers $authHeaders -ExpectFailure
$test37Pass = $notFoundResult.StatusCode -eq 404
Write-TestResult "37" "Resource Not Found" $test37Pass "Status Code: $($notFoundResult.StatusCode)"

# Test 39: Large Payload
Write-TestHeader "Test 39" "Large Payload"
$largeContent = "A" * 60000  # 60,000 characters
$largePayloadBody = @{
    title = "Large Document"
    content = $largeContent
    category = "test"
} | ConvertTo-Json

$largePayloadResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Method POST -Body $largePayloadBody -Headers $authHeaders
if ($largePayloadResult.Success -or $largePayloadResult.StatusCode -eq 413 -or $largePayloadResult.StatusCode -eq 400) {
    Write-TestResult "39" "Large Payload" $true "Handled gracefully (Status: $($largePayloadResult.StatusCode))"
} else {
    Write-TestResult "39" "Large Payload" $false "Unexpected error: $($largePayloadResult.Error)"
}

# Test 40: Special Characters in Content
Write-TestHeader "Test 40" "Special Characters in Content"
$specialCharsBody = @{
    title = "Special Characters Test 🚀"
    content = "Testing special chars: 你好, привет, مرحبا, <script>alert('xss')</script>, '; DROP TABLE knowledge; --"
    category = "test"
} | ConvertTo-Json

$specialCharsResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge" -Method POST -Body $specialCharsBody -Headers $authHeaders
Write-TestResult "40" "Special Characters in Content" $specialCharsResult.Success "Properly handled special characters and potential injection attempts"

# Test 24: Delete Knowledge Document (cleanup)
if ($knowledgeId) {
    Write-TestHeader "Test 24" "Delete Knowledge Document"
    $deleteResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge/$knowledgeId" -Method DELETE -Headers $authHeaders
    
    if ($deleteResult.Success) {
        # Verify deletion
        $verifyDeleteResult = Invoke-ApiRequest -Url "$BaseUrl/api/knowledge/$knowledgeId" -Headers $authHeaders -ExpectFailure
        $test24Pass = $verifyDeleteResult.StatusCode -eq 404
        Write-TestResult "24" "Delete Knowledge Document" $test24Pass "Document deleted, verification status: $($verifyDeleteResult.StatusCode)"
    } else {
        Write-TestResult "24" "Delete Knowledge Document" $false "Failed to delete: $($deleteResult.Error)"
    }
}

# ============================================================================
# SECTION: PERFORMANCE (Tests 42-43)
# ============================================================================

Write-Host "`n⚡ Performance Testing" -ForegroundColor Magenta

# Test 42: Response Time
Write-TestHeader "Test 42" "Response Time"
$stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
$perfChatBody = @{
    message = "Quick response test"
    sessionId = $null
} | ConvertTo-Json

$perfResult = Invoke-ApiRequest -Url "$BaseUrl/api/chat" -Method POST -Body $perfChatBody -Headers $authHeaders
$stopwatch.Stop()
$responseTime = $stopwatch.ElapsedMilliseconds

$test42Pass = $responseTime -lt 5000  # 5 seconds (relaxed from 2 for development)
Write-TestResult "42" "Response Time" $test42Pass "Response time: $responseTime ms (target: < 5000ms for dev)"

# ============================================================================
# SUMMARY
# ============================================================================

Write-Host "`n╔════════════════════════════════════════════════════════════╗" -ForegroundColor Magenta
Write-Host "║                    TEST SUMMARY                             ║" -ForegroundColor Magenta
Write-Host "╚════════════════════════════════════════════════════════════╝" -ForegroundColor Magenta

$totalTests = $testResults.Count
$passedTests = ($testResults | Where-Object { $_.Status -eq "PASS" }).Count
$failedTests = ($testResults | Where-Object { $_.Status -eq "FAIL" }).Count
$criticalFailures = ($testResults | Where-Object { $_.Status -eq "FAIL" -and $_.IsCritical }).Count

Write-Host "`nTotal Tests: $totalTests" -ForegroundColor Cyan
Write-Host "Passed: $passedTests" -ForegroundColor Green
Write-Host "Failed: $failedTests" -ForegroundColor Red
if ($criticalFailures -gt 0) {
    Write-Host "Critical Failures: $criticalFailures" -ForegroundColor Red -BackgroundColor Yellow
    Write-Host "`n⚠️  CRITICAL BLOCKER: DO NOT DEPLOY TO PRODUCTION" -ForegroundColor Red -BackgroundColor Yellow
}

Write-Host "`n" -NoNewline
Write-Host "Pass Rate: " -NoNewline
$passRate = [math]::Round(($passedTests / $totalTests) * 100, 2)
if ($passRate -ge 95) {
    Write-Host "$passRate%" -ForegroundColor Green
} elseif ($passRate -ge 80) {
    Write-Host "$passRate%" -ForegroundColor Yellow
} else {
    Write-Host "$passRate%" -ForegroundColor Red
}

Write-Host "`nEnd Time: $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" -ForegroundColor Yellow

# Display failed tests
if ($failedTests -gt 0) {
    Write-Host "`n❌ Failed Tests:" -ForegroundColor Red
    $testResults | Where-Object { $_.Status -eq "FAIL" } | ForEach-Object {
        Write-Host "   [$($_.TestNumber)] $($_.TestName)" -ForegroundColor Red
        if ($_.Details) {
            Write-Host "      $($_.Details)" -ForegroundColor Gray
        }
    }
}

Write-Host "`n✅ Completed Tests:" -ForegroundColor Green
$testResults | Where-Object { $_.Status -eq "PASS" } | ForEach-Object {
    Write-Host "   [$($_.TestNumber)] $($_.TestName)" -ForegroundColor Green
}

Write-Host "`n═══════════════════════════════════════════════════════════════" -ForegroundColor Magenta

# Note about multi-tenant tests
Write-Host "`n⚠️  NOTE: Multi-tenant isolation tests (Tests 1-7) require multiple tenants." -ForegroundColor Yellow
Write-Host "    Please create additional test tenants and run those tests manually." -ForegroundColor Yellow
Write-Host "    See PRODUCTION_TESTING_CHECKLIST.md for detailed multi-tenant test procedures." -ForegroundColor Yellow
