# Test Knowledge Search
$baseUrl = "http://localhost:5257"

$loginBody = @{ username = "admin"; password = "Admin@123456" } | ConvertTo-Json
$loginResp = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginBody -ContentType 'application/json'
$headers = @{ Authorization = "Bearer $($loginResp.token)" }

Write-Host "Testing Knowledge Search..." -ForegroundColor Cyan

# Search for existing documents
try {
    $results = Invoke-RestMethod -Uri "$baseUrl/api/knowledge/search?query=return&limit=10" -Headers $headers
    Write-Host "Found $($results.Count) results" -ForegroundColor Green
    $results | ForEach-Object {
        Write-Host "  - $($_.title)" -ForegroundColor Gray
    }
} catch {
    Write-Host "Search failed: $($_.Exception.Message)" -ForegroundColor Red
}
