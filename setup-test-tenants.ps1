# Script to create test tenants for multi-tenant isolation testing
# Run this script to set up tenanta and tenantb for comprehensive testing

$connectionString = "Data Source=(localdb)\MSSQLLocalDB;Initial Catalog=ChatifyAI;Integrated Security=True"

Write-Host "====================================================" -ForegroundColor Cyan
Write-Host "   Creating Test Tenants for Multi-Tenant Testing   " -ForegroundColor Cyan
Write-Host "====================================================" -ForegroundColor Cyan

# SQL script to create test tenants
$sql = @"
-- Check if tenants already exist
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Slug = 'tenanta')
BEGIN
    DECLARE @TenantAId UNIQUEIDENTIFIER = NEWID()
    DECLARE @TenantAAdminId UNIQUEIDENTIFIER = NEWID()
    
    -- Create Tenant A
    INSERT INTO Tenants (Id, Slug, Name, Email, PlanTier, IsActive, MaxDocuments, MaxMonthlyMessages, 
                         CurrentDocumentCount, CurrentMonthMessages, BillingPeriodStart, CreatedAt)
    VALUES (@TenantAId, 'tenanta', 'Test Tenant A', 'admin@tenanta.com', 'Basic', 1, 100, 10000, 
            0, 0, GETUTCDATE(), GETUTCDATE())
    
    -- Create Admin User for Tenant A
    INSERT INTO AdminUsers (Id, Username, PasswordHash, Email, FullName, TenantId, IsPlatformAdmin, IsActive, CreatedAt)
    VALUES (@TenantAAdminId, 'adminA', 
            'AQAAAAIAAYagAAAAEJxq8qT9fI8tZ3F9cN0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kQ==', -- Password123!
            'admin@tenanta.com', 'Admin User A', @TenantAId, 0, 1, GETUTCDATE())
    
    -- Create Tenant Settings for Tenant A
    INSERT INTO TenantSettings (Id, TenantId, VectorStorageMode, EnableDocumentChunking, EnableChatHistory,
                                ChatHistoryRetentionDays, EnableFeedback, EnableOverview, 
                                WelcomeMessage, Temperature, MaxTokens, EnableTools, CreatedAt)
    VALUES (NEWID(), @TenantAId, 'SQL', 1, 1, 90, 1, 1,
            'Welcome to Tenant A! How can I help you?', 0.7, 2000, 1, GETUTCDATE())
    
    PRINT 'Created Tenant A (tenanta) with admin user adminA'
END
ELSE
BEGIN
    PRINT 'Tenant A (tenanta) already exists'
END

-- Check if Tenant B already exists
IF NOT EXISTS (SELECT 1 FROM Tenants WHERE Slug = 'tenantb')
BEGIN
    DECLARE @TenantBId UNIQUEIDENTIFIER = NEWID()
    DECLARE @TenantBAdminId UNIQUEIDENTIFIER = NEWID()
    
    -- Create Tenant B
    INSERT INTO Tenants (Id, Slug, Name, Email, PlanTier, IsActive, MaxDocuments, MaxMonthlyMessages, 
                         CurrentDocumentCount, CurrentMonthMessages, BillingPeriodStart, CreatedAt)
    VALUES (@TenantBId, 'tenantb', 'Test Tenant B', 'admin@tenantb.com', 'Pro', 1, 500, 50000, 
            0, 0, GETUTCDATE(), GETUTCDATE())
    
    -- Create Admin User for Tenant B
    INSERT INTO AdminUsers (Id, Username, PasswordHash, Email, FullName, TenantId, IsPlatformAdmin, IsActive, CreatedAt)
    VALUES (@TenantBAdminId, 'adminB', 
            'AQAAAAIAAYagAAAAEJxq8qT9fI8tZ3F9cN0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kZ0qJxE0kQ==', -- Password123!
            'admin@tenantb.com', 'Admin User B', @TenantBId, 0, 1, GETUTCDATE())
    
    -- Create Tenant Settings for Tenant B
    INSERT INTO TenantSettings (Id, TenantId, VectorStorageMode, EnableDocumentChunking, EnableChatHistory,
                                ChatHistoryRetentionDays, EnableFeedback, EnableOverview, 
                                WelcomeMessage, Temperature, MaxTokens, EnableTools, CreatedAt)
    VALUES (NEWID(), @TenantBId, 'SQL', 1, 1, 180, 1, 1,
            'Welcome to Tenant B! How can I assist you today?', 0.8, 3000, 1, GETUTCDATE())
    
    PRINT 'Created Tenant B (tenantb) with admin user adminB'
END
ELSE
BEGIN
    PRINT 'Tenant B (tenantb) already exists'
END

-- Display created tenants
SELECT Slug, Name, Email, PlanTier, IsActive FROM Tenants WHERE Slug IN ('tenanta', 'tenantb', 'dott')
"@

try {
    # Load SQL Server assembly
    Add-Type -AssemblyName "System.Data"
    
    # Create connection
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    
    Write-Host "`nExecuting SQL to create tenants..." -ForegroundColor Yellow
    
    # Execute SQL
    $command = $connection.CreateCommand()
    $command.CommandText = $sql
    $command.CommandTimeout = 60
    
    $reader = $command.ExecuteReader()
    
    # Read messages
    do {
        while ($reader.Read()) {
            for ($i = 0; $i -lt $reader.FieldCount; $i++) {
                Write-Host "$($reader.GetName($i)): $($reader.GetValue($i))" -ForegroundColor Gray
            }
        }
    } while ($reader.NextResult())
    
    $reader.Close()
    $connection.Close()
    
    Write-Host "`nSUCCESS: Test tenants created!" -ForegroundColor Green
    Write-Host "`nTest Credentials:" -ForegroundColor Cyan
    Write-Host "  Tenant A:" -ForegroundColor Yellow
    Write-Host "    Username: adminA" -ForegroundColor Gray
    Write-Host "    Password: Password123!" -ForegroundColor Gray
    Write-Host "    Email: admin@tenanta.com" -ForegroundColor Gray
    Write-Host ""
    Write-Host "  Tenant B:" -ForegroundColor Yellow
    Write-Host "    Username: adminB" -ForegroundColor Gray
    Write-Host "    Password: Password123!" -ForegroundColor Gray
    Write-Host "    Email: admin@tenantb.com" -ForegroundColor Gray
    Write-Host ""
    Write-Host "NOTE: In production, subdomain routing would route:" -ForegroundColor Yellow
    Write-Host "  - http://tenanta.yourdomain.com -> Tenant A" -ForegroundColor Gray
    Write-Host "  - http://tenantb.yourdomain.com -> Tenant B" -ForegroundColor Gray
    Write-Host ""
    Write-Host "For local testing, use the slug in login requests or configure hosts file." -ForegroundColor Yellow
}
catch {
    Write-Host "ERROR: Failed to create tenants" -ForegroundColor Red
    Write-Host $_.Exception.Message -ForegroundColor Red
    
    if ($_.Exception.InnerException) {
        Write-Host "Inner Exception: $($_.Exception.InnerException.Message)" -ForegroundColor Red
    }
    
    exit 1
}
