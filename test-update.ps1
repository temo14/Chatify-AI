# Test Knowledge Update
$baseUrl = "http://localhost:5257"

# Login
$loginBody = @{ username = "admin"; password = "Admin@123456" } | ConvertTo-Json
$loginResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType 'application/json'
$headers = @{ Authorization = "Bearer $($loginResp.token)" }

Write-Host "Testing Knowledge Update..." -ForegroundColor Cyan

# Create document
$docBody = @{ 
    title = "Test Update Doc"
    content = "Original content for testing update"
    category = "test" 
} | ConvertTo-Json

$doc = Invoke-RestMethod -Uri "$baseUrl/api/knowledge" -Method POST -Body $docBody -Headers $headers -ContentType 'application/json'
Write-Host "Created: $($doc.id)" -ForegroundColor Gray

Start-Sleep -Seconds 2

# Update document
$updateBody = @{ 
    title = "Updated Title"
    content = "This is the updated content"
    category = "test"
    isActive = $true
} | ConvertTo-Json

try {
    $updated = Invoke-RestMethod -Uri "$baseUrl/api/knowledge/$($doc.id)" -Method PUT -Body $updateBody -Headers $headers -ContentType 'application/json'
    Write-Host "SUCCESS: Document updated!" -ForegroundColor Green
    Write-Host "New title: $($updated.title)" -ForegroundColor Gray
} catch {
    Write-Host "FAILED: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.ErrorDetails) {
        Write-Host "Details: $($_.ErrorDetails.Message)" -ForegroundColor Yellow
    }
}
