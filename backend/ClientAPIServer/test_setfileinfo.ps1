# PowerShell script to test SetFileInfo boundary values
# This script tests the fixed SetFileInfo implementation for size and datetime validation

param(
    [string]$BaseUrl = "http://localhost:8357",
    [string]$SessionId = "test-session-123"
)

function Test-SetFileInfoRequest {
    param(
        [string]$TestName,
        [string]$FilePath,
        [object]$RequestBody,
        [int]$ExpectedStatusCode = 200
    )

    try {
        $headers = @{
            "Authorization" = "SessionID $SessionId"
            "Content-Type" = "application/json"
        }

        $json = $RequestBody | ConvertTo-Json -Depth 10
        Write-Host "Testing $TestName..."
        Write-Host "Request: $json"

        $response = Invoke-WebRequest -Uri "$BaseUrl/api/v3/SetFileInfo?path=$FilePath" -Method POST -Body $json -Headers $headers -UseBasicParsing

        $actualStatus = $response.StatusCode
        if ($actualStatus -eq $ExpectedStatusCode) {
            Write-Host "✓ PASS: $TestName (Expected: $ExpectedStatusCode, Got: $actualStatus)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ FAIL: $TestName (Expected: $ExpectedStatusCode, Got: $actualStatus)" -ForegroundColor Red
            return $false
        }
    }
    catch {
        $actualStatus = $_.Exception.Response.StatusCode.value__
        if ($actualStatus -eq $ExpectedStatusCode) {
            Write-Host "✓ PASS: $TestName (Expected: $ExpectedStatusCode, Got: $actualStatus)" -ForegroundColor Green
            return $true
        } else {
            Write-Host "✗ FAIL: $TestName (Expected: $ExpectedStatusCode, Got: $actualStatus)" -ForegroundColor Red
            Write-Host "Error details: $($_.Exception.Message)" -ForegroundColor Red
            return $false
        }
    }
}

function Create-TestFile {
    param([string]$FilePath)

    try {
        $headers = @{
            "Authorization" = "SessionID $SessionId"
            "Content-Type" = "application/json"
        }

        $createBody = @{
            path = ""
            createFile = $true
            name = $FilePath
        } | ConvertTo-Json

        $response = Invoke-WebRequest -Uri "$BaseUrl/api/v3/CreateFile" -Method POST -Body $createBody -Headers $headers -UseBasicParsing
        Write-Host "Created test file: $FilePath"
        return $true
    }
    catch {
        Write-Host "Failed to create test file: $FilePath - $($_.Exception.Message)" -ForegroundColor Yellow
        return $false
    }
}

# Main test execution
Write-Host "Starting SetFileInfo Boundary Value Tests" -ForegroundColor Cyan
Write-Host "Target URL: $BaseUrl" -ForegroundColor Cyan

$testResults = @()
$testFile = "setfileinfo_test_file.txt"

# Create a test file first
if (-not (Create-TestFile -FilePath $testFile)) {
    Write-Host "Cannot proceed without test file" -ForegroundColor Red
    exit 1
}

Write-Host "`n=== SIZE BOUNDARY VALUE TESTS ===" -ForegroundColor Yellow

# Test 1: Negative size (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Negative size (-1)" -FilePath $testFile -RequestBody @{ size = -1; returnFileInfo = $true } -ExpectedStatusCode 400

# Test 2: Zero size (should work with 200)
$testResults += Test-SetFileInfoRequest -TestName "Zero size (0)" -FilePath $testFile -RequestBody @{ size = 0; returnFileInfo = $true } -ExpectedStatusCode 200

# Test 3: Small positive size (should work with 200)
$testResults += Test-SetFileInfoRequest -TestName "Small positive size (1024)" -FilePath $testFile -RequestBody @{ size = 1024; returnFileInfo = $true } -ExpectedStatusCode 200

# Test 4: Very large size (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Very large size (Long.MaxValue)" -FilePath $testFile -RequestBody @{ size = 9223372036854775807; returnFileInfo = $true } -ExpectedStatusCode 400

Write-Host "`n=== DATETIME BOUNDARY VALUE TESTS ===" -ForegroundColor Yellow

# Test 5: Valid UTC datetime (should work with 200)
$testResults += Test-SetFileInfoRequest -TestName "Valid UTC datetime" -FilePath $testFile -RequestBody @{ modified = "2024-01-01T12:00:00Z"; returnFileInfo = $true } -ExpectedStatusCode 200

# Test 6: Invalid datetime string (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Invalid datetime string" -FilePath $testFile -RequestBody @{ modified = "invalid-date"; returnFileInfo = $true } -ExpectedStatusCode 400

# Test 7: Invalid month (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Invalid month (13)" -FilePath $testFile -RequestBody @{ modified = "2024-13-01T00:00:00Z"; returnFileInfo = $true } -ExpectedStatusCode 400

# Test 8: Invalid day (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Invalid day (32)" -FilePath $testFile -RequestBody @{ modified = "2024-01-32T00:00:00Z"; returnFileInfo = $true } -ExpectedStatusCode 400

# Test 9: Invalid hour (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Invalid hour (25)" -FilePath $testFile -RequestBody @{ modified = "2024-01-01T25:00:00Z"; returnFileInfo = $true } -ExpectedStatusCode 400

# Test 10: Date without time (should fail with 400)
$testResults += Test-SetFileInfoRequest -TestName "Date without time" -FilePath $testFile -RequestBody @{ modified = "2024-01-01"; returnFileInfo = $true } -ExpectedStatusCode 400

Write-Host "`n=== TEST RESULTS ===" -ForegroundColor Cyan
$passed = ($testResults | Where-Object { $_ -eq $true }).Count
$total = $testResults.Count

Write-Host "Tests passed: $passed/$total" -ForegroundColor $(if ($passed -eq $total) { "Green" } else { "Red" })

if ($passed -eq $total) {
    Write-Host "All tests PASSED! SetFileInfo implementation is working correctly." -ForegroundColor Green
    exit 0
} else {
    Write-Host "Some tests FAILED! Check the implementation." -ForegroundColor Red
    exit 1
}
