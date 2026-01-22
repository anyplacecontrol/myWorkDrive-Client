#!/bin/bash
# Simple bash script to test SetFileInfo boundary values

BASE_URL="http://localhost:8357"
SESSION_ID="test-session-123"
TEST_FILE="setfileinfo_test_file.txt"

echo "Starting SetFileInfo Boundary Value Tests"
echo "Target URL: $BASE_URL"

# Function to create test file
create_test_file() {
    echo "Creating test file: $TEST_FILE"
    curl -s -X POST "$BASE_URL/api/v3/CreateFile" \
        -H "Authorization: SessionID $SESSION_ID" \
        -H "Content-Type: application/json" \
        -d "{\"path\":\"\",\"createFile\":true,\"name\":\"$TEST_FILE\"}" \
        > /dev/null
}

# Function to test SetFileInfo request
test_setfileinfo() {
    local test_name="$1"
    local request_body="$2"
    local expected_status="$3"

    echo -n "Testing $test_name... "

    response=$(curl -s -w "\n%{http_code}" -X POST \
        "$BASE_URL/api/v3/SetFileInfo?path=$TEST_FILE" \
        -H "Authorization: SessionID $SESSION_ID" \
        -H "Content-Type: application/json" \
        -d "$request_body")

    actual_status=$(echo "$response" | tail -n1)

    if [ "$actual_status" = "$expected_status" ]; then
        echo "✓ PASS (Expected: $expected_status, Got: $actual_status)"
        return 0
    else
        echo "✗ FAIL (Expected: $expected_status, Got: $actual_status)"
        return 1
    fi
}

# Create test file
create_test_file

echo
echo "=== SIZE BOUNDARY VALUE TESTS ==="

# Test negative size (should fail with 400)
test_setfileinfo "Negative size (-1)" '{"size":-1,"returnFileInfo":true}' "400"

# Test zero size (should work with 200)
test_setfileinfo "Zero size (0)" '{"size":0,"returnFileInfo":true}' "200"

# Test small positive size (should work with 200)
test_setfileinfo "Small positive size (1024)" '{"size":1024,"returnFileInfo":true}' "200"

# Test very large size (should fail with 400)
test_setfileinfo "Very large size (Long.MaxValue)" '{"size":9223372036854775807,"returnFileInfo":true}' "400"

echo
echo "=== DATETIME BOUNDARY VALUE TESTS ==="

# Test valid UTC datetime (should work with 200)
test_setfileinfo "Valid UTC datetime" '{"modified":"2024-01-01T12:00:00Z","returnFileInfo":true}' "200"

# Test invalid datetime string (should fail with 400)
test_setfileinfo "Invalid datetime string" '{"modified":"invalid-date","returnFileInfo":true}' "400"

# Test invalid month (should fail with 400)
test_setfileinfo "Invalid month (13)" '{"modified":"2024-13-01T00:00:00Z","returnFileInfo":true}' "400"

# Test invalid day (should fail with 400)
test_setfileinfo "Invalid day (32)" '{"modified":"2024-01-32T00:00:00Z","returnFileInfo":true}' "400"

# Test invalid hour (should fail with 400)
test_setfileinfo "Invalid hour (25)" '{"modified":"2024-01-01T25:00:00Z","returnFileInfo":true}' "400"

# Test date without time (should fail with 400)
test_setfileinfo "Date without time" '{"modified":"2024-01-01","returnFileInfo":true}' "400"

echo
echo "Test completed!"
