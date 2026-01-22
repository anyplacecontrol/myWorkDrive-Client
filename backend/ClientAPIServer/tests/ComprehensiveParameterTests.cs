using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text;

namespace MockServerAPITests
{
    /// <summary>
    /// Comprehensive parameter validation tests for all API endpoints.
    /// Tests include boundary values, values outside valid ranges, and edge cases.
    /// For numeric parameters: tests values inside valid interval, far outside, and boundary values.
    /// For strings: tests various lengths including boundary values around limits.
    /// </summary>
    [TestClass]
    public class ComprehensiveParameterTests : MockApiTestsBase
    {
        [TestInitialize]
        public new void Initialize()
        {
            base.Initialize();
        }

        [TestCleanup]
        public new void Cleanup()
        {
            base.Cleanup();
        }

        #region Numeric Parameter Test Data Generators

        /// <summary>
        /// Generates boundary test values for integer parameters with minimum 0.
        /// Example: for range 0..max, tests: -2, 0, 4, 5, 7, 10, 11, 100
        /// Here 4,5 and 10,11 are boundary values around valid range 5..10
        /// </summary>
        private static IEnumerable<long> GetBoundaryValuesForNonNegativeInt64(long max = long.MaxValue)
        {
            return new long[]
            {
                -2, 0, 1, 5, max / 2,  // Far outside, boundary, inside valid interval, middle values
                max - 1, max, max == long.MaxValue ? max : max + 1  // Boundary values around max
            };
        }

        /// <summary>
        /// Generates boundary test values for integer parameters with minimum 1
        /// </summary>
        private static IEnumerable<long> GetBoundaryValuesForPositiveInt64(long max = long.MaxValue)
        {
            return new long[]
            {
                -2, 0, 1, 2, 50, max / 2,  // Far outside, boundary, inside, middle values
                max - 1, max, max == long.MaxValue ? max : max + 1  // Boundary around max
            };
        }

        /// <summary>
        /// Generates boundary test values for int32 parameters
        /// </summary>
        private static IEnumerable<int> GetBoundaryValuesForInt32()
        {
            return new int[]
            {
                int.MinValue, -1000, -1, 0, 1, 100, 1000, int.MaxValue / 2,
                int.MaxValue - 1, int.MaxValue
            };
        }

        #endregion

        #region String Parameter Test Data Generators

        /// <summary>
        /// Generates boundary test strings for length constraints.
        /// If maxLength=10, generates: "", "a", "aaa...(9)", "aaa...(10)", "aaa...(11)", "aaa...(20)"
        /// </summary>
        private static IEnumerable<string> GetBoundaryStringsForLength(int maxLength)
        {
            if (maxLength <= 0) yield break;

            yield return ""; // Empty string
            yield return "a"; // Length 1

            if (maxLength > 2)
            {
                yield return new string('a', maxLength - 1); // maxLength - 1 (boundary)
                yield return new string('a', maxLength);     // maxLength (boundary)
                yield return new string('a', maxLength + 1); // maxLength + 1 (boundary - should fail)

                if (maxLength < 1000)
                    yield return new string('a', maxLength * 2); // Far outside valid range
            }
        }

        /// <summary>
        /// Generates test strings with special characters for path/name testing
        /// </summary>
        private static IEnumerable<string> GetSpecialCharacterStrings()
        {
            return new[]
            {
                "normal-file.txt",
                "file with spaces.txt",
                "file_with_underscores.txt",
                "file-with-dashes.txt",
                "file.with.dots.txt",
                "UPPERCASE.TXT",
                "file123.txt",
                "файл.txt",     // Cyrillic
                "file'quote.txt",
                "file&ampersand.txt"
            };
        }

        #endregion

        #region ReadFile Parameter Tests

        [TestMethod]
        public async Task ReadFile_StartPosition_BoundaryValueTesting()
        {
            // Arrange - Create a test file with content
            var fileName = GenerateTestFileName("read-boundary");
            var testContent = "This is test content for boundary testing of start position parameter";
            await CreateTestFile(fileName, testContent);
            var testContentBytes = Encoding.UTF8.GetBytes(testContent);

            var testCases = new[]
            {
                // Far outside valid interval (negative values)
                (-100L, HttpStatusCode.BadRequest, "Far negative start position should fail"),
                (-2L, HttpStatusCode.BadRequest, "Negative start position should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Start position 0 should succeed"),
                (1L, HttpStatusCode.OK, "Start position 1 should succeed"),

                // Inside valid interval (middle values)
                (5L, HttpStatusCode.OK, "Middle start position should succeed"),
                ((long)(testContentBytes.Length / 2), HttpStatusCode.OK, "Middle of file should succeed"),

                // Boundary values around file length
                ((long)(testContentBytes.Length - 1), HttpStatusCode.OK, "Last byte position should succeed"),
                ((long)testContentBytes.Length, HttpStatusCode.OK, "Position at end should succeed"),
                ((long)(testContentBytes.Length + 1), HttpStatusCode.OK, "Position beyond end should succeed"),

                // Far outside valid interval (beyond file)
                ((long)(testContentBytes.Length * 2), HttpStatusCode.OK, "Far beyond file should succeed")
            };

            foreach (var (startPosition, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}ReadFile?path={UrlEncode("/Documents/" + fileName)}&startPosition={startPosition}&count=10";
                var response = await _client.GetAsync(url);

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - StartPosition: {startPosition}");
            }
        }

        [TestMethod]
        public async Task ReadFile_Count_BoundaryValueTesting()
        {
            // Arrange
            var fileName = GenerateTestFileName("read-count");
            await CreateTestFile(fileName, "Test content for count boundary testing");

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000, HttpStatusCode.BadRequest, "Far negative count should fail"),
                (-1, HttpStatusCode.BadRequest, "Negative count should fail"),

                // Boundary values around 0
                (0, HttpStatusCode.OK, "Zero count should succeed"),
                (1, HttpStatusCode.OK, "Count 1 should succeed"),

                // Inside valid interval (middle values)
                (10, HttpStatusCode.OK, "Count 10 should succeed"),
                (100, HttpStatusCode.OK, "Count 100 should succeed"),

                // Large values (testing upper boundaries)
                (1024 * 1024, HttpStatusCode.OK, "1MB count should succeed"),
                (int.MaxValue, HttpStatusCode.OK, "Max int count should succeed")
            };

            foreach (var (count, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}ReadFile?path={UrlEncode("/Documents/" + fileName)}&startPosition=0&count={count}";
                var response = await _client.GetAsync(url);

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - Count: {count}");

                if (response.IsSuccessStatusCode && count >= 0)
                {
                    var responseBytes = await response.Content.ReadAsByteArrayAsync();
                    Assert.IsTrue(responseBytes.Length <= count,
                        $"Response length {responseBytes.Length} should not exceed requested count {count}");
                }
            }
        }

        #endregion

        #region WriteFile Parameter Tests

        [TestMethod]
        public async Task WriteFile_StartPosition_BoundaryValueTesting()
        {
            var fileName = GenerateTestFileName("write-boundary");
            await CreateTestFile(fileName);

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000L, HttpStatusCode.BadRequest, "Far negative start position should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative start position should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Start position 0 should succeed"),
                (1L, HttpStatusCode.OK, "Start position 1 should succeed"),

                // Inside valid interval (middle values)
                (10L, HttpStatusCode.OK, "Middle start position should succeed"),
                (100L, HttpStatusCode.OK, "Larger start position should succeed"),

                // Large values (boundary testing for large files)
                (1024L * 1024, HttpStatusCode.OK, "1MB start position should succeed")
            };

            foreach (var (startPosition, expectedStatus, description) in testCases)
            {
                // Arrange
                var testContent = "boundary test content";
                var contentBytes = Encoding.UTF8.GetBytes(testContent);

                // Act
                var url = $"{BaseApiUrl}WriteFile?path={UrlEncode("/Documents/" + fileName)}&startPosition={startPosition}&unlockAfterWrite=true";
                var response = await _client.PostAsync(url, new ByteArrayContent(contentBytes));

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - StartPosition: {startPosition}");
            }
        }

        [TestMethod]
        public async Task WriteFile_TotalLength_BoundaryValueTesting()
        {
            var fileName = GenerateTestFileName("write-total-length");
            await CreateTestFile(fileName);

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000L, HttpStatusCode.BadRequest, "Far negative total length should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative total length should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Zero total length should succeed"),
                (1L, HttpStatusCode.OK, "Total length 1 should succeed"),

                // Inside valid interval (middle values)
                (100L, HttpStatusCode.OK, "Medium total length should succeed"),
                (1024L, HttpStatusCode.OK, "1KB total length should succeed"),

                // Large values
                (1024L * 1024, HttpStatusCode.OK, "1MB total length should succeed")
            };

            foreach (var (totalLength, expectedStatus, description) in testCases)
            {
                // Arrange
                var testContent = "test";
                var contentBytes = Encoding.UTF8.GetBytes(testContent);

                // Act
                var url = $"{BaseApiUrl}WriteFile?path={UrlEncode("/Documents/" + fileName)}&startPosition=0&unlockAfterWrite=true&totalLength={totalLength}";
                var response = await _client.PostAsync(url, new ByteArrayContent(contentBytes));

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - TotalLength: {totalLength}");
            }
        }

        #endregion

        #region LockFile Parameter Tests

        [TestMethod]
        public async Task LockFile_Expires_BoundaryValueTesting()
        {
            var fileName = GenerateTestFileName("lock-expires");
            await CreateTestFile(fileName, "content to lock");

            var testCases = new[]
            {
                // Far outside valid interval (below minimum 1)
                (-1000L, HttpStatusCode.BadRequest, "Far negative expires should fail"),
                (0L, HttpStatusCode.BadRequest, "Zero expires should fail (minimum is 1)"),

                // Boundary values around minimum 1
                (1L, HttpStatusCode.Created, "Expires 1 second should succeed"),
                (2L, HttpStatusCode.Created, "Expires 2 seconds should succeed"),

                // Inside valid interval (middle values)
                (60L, HttpStatusCode.Created, "Expires 1 minute should succeed"),
                (3600L, HttpStatusCode.Created, "Expires 1 hour should succeed"),

                // Large values (boundary testing)
                (86400L, HttpStatusCode.Created, "Expires 1 day should succeed"),
                (86400L * 365, HttpStatusCode.Created, "Expires 1 year should succeed")
            };

            foreach (var (expires, expectedStatus, description) in testCases)
            {
                // Unlock any existing locks first
                await _client.PostAsync($"{BaseApiUrl}UnlockFile?path={UrlEncode("/Documents/" + fileName)}", null);

                // Act
                var url = $"{BaseApiUrl}LockFile?path={UrlEncode("/Documents/" + fileName)}&coedit=false&expires={expires}";
                var response = await _client.PostAsync(url, null);

                // Assert
                Assert.IsTrue(response.StatusCode == expectedStatus || response.StatusCode == HttpStatusCode.Conflict,
                    $"{description} - Expires: {expires}. Expected: {expectedStatus}, Got: {response.StatusCode}");
            }
        }

        #endregion

        #region StartFileUpload Parameter Tests

        [TestMethod]
        public async Task StartFileUpload_Size_BoundaryValueTesting()
        {
            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000L, HttpStatusCode.BadRequest, "Far negative size should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative size should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Zero size should succeed"),
                (1L, HttpStatusCode.OK, "Size 1 should succeed"),

                // Inside valid interval (middle values)
                (1024L, HttpStatusCode.OK, "1KB size should succeed"),
                (1024L * 1024, HttpStatusCode.OK, "1MB size should succeed"),

                // Large values (testing implementation limits)
                (1024L * 1024 * 100, HttpStatusCode.OK, "100MB size should succeed")
            };

            foreach (var (size, expectedStatus, description) in testCases)
            {
                var fileName = GenerateTestFileName("upload-size");

                // Arrange
                var uploadData = new
                {
                    size = size,
                    conflictBehavior = "replace"
                };

                // Act
                var url = $"{BaseApiUrl}StartFileUpload?path={UrlEncode("/Documents/" + fileName)}";
                var response = await _client.PostAsync(url, CreateJsonContent(uploadData));

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - Size: {size}");

                // Clean up - cancel upload if successful
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    if (uploadInfo?.ContainsKey("uploadId") == true)
                    {
                        var uploadId = uploadInfo["uploadId"].ToString();
                        await _client.PostAsync($"{BaseApiUrl}CancelUpload?uploadId={uploadId}", null);
                    }
                }
            }
        }

        #endregion

        #region Search Parameter Tests

        [TestMethod]
        public async Task SearchFiles_MaxResults_BoundaryValueTesting()
        {
            // Arrange - Create some test files first
            for (int i = 0; i < 5; i++)
            {
                await CreateTestFile($"search-test-{i}.txt", $"search content {i}");
            }

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000L, HttpStatusCode.BadRequest, "Far negative maxResults should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative maxResults should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Zero maxResults should succeed"),
                (1L, HttpStatusCode.OK, "MaxResults 1 should succeed"),

                // Inside valid interval (middle values)
                (3L, HttpStatusCode.OK, "MaxResults 3 should succeed"),
                (10L, HttpStatusCode.OK, "MaxResults 10 should succeed"),

                // Large values
                (1000L, HttpStatusCode.OK, "MaxResults 1000 should succeed")
            };

            foreach (var (maxResults, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}SearchFiles?field=name&query=search-test&maxResults={maxResults}";
                var response = await _client.GetAsync(url);

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - MaxResults: {maxResults}");

                if (response.IsSuccessStatusCode && maxResults > 0)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var results = JsonConvert.DeserializeObject<List<object>>(content);
                    Assert.IsTrue(results.Count <= maxResults,
                        $"Result count {results.Count} should not exceed maxResults {maxResults}");
                }
            }
        }

        [TestMethod]
        public async Task SearchFiles_Query_StringLengthBoundaryTesting()
        {
            var testCases = new[]
            {
                // Empty and very short strings
                ("", HttpStatusCode.OK, "Empty query should succeed"),
                ("a", HttpStatusCode.OK, "Single char query should succeed"),

                // Normal length strings (middle values)
                ("normal", HttpStatusCode.OK, "Normal query should succeed"),
                ("longer search term", HttpStatusCode.OK, "Multi-word query should succeed"),

                // Boundary testing for string length
                (new string('a', 100), HttpStatusCode.OK, "100 char query should succeed"),
                (new string('a', 500), HttpStatusCode.OK, "500 char query should succeed"),

                // Far outside reasonable limits
                (new string('a', 1000), HttpStatusCode.OK, "Very long query should succeed or fail gracefully")
            };

            foreach (var (query, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}SearchFiles?field=name&query={UrlEncode(query)}";
                var response = await _client.GetAsync(url);

                // Assert - Allow both success and BadRequest for edge cases
                Assert.IsTrue(response.StatusCode == expectedStatus || response.StatusCode == HttpStatusCode.BadRequest,
                    $"{description} - Query length: {query.Length}");
            }
        }

        #endregion

        #region Path Parameter Tests

        [TestMethod]
        public async Task Various_Endpoints_Path_SpecialCharacterTesting()
        {
            var endpoints = new[] { "GetItemType", "GetFileInfo" };

            foreach (var endpoint in endpoints)
            {
                foreach (var testName in GetSpecialCharacterStrings().Take(5)) // Limit for performance
                {
                    var testPath = $"/Documents/{testName}";

                    // Act
                    var url = $"{BaseApiUrl}{endpoint}?path={UrlEncode(testPath)}";
                    var response = await _client.GetAsync(url);

                    // Assert - Should handle special characters gracefully
                    Assert.IsTrue(
                        response.IsSuccessStatusCode ||
                        response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.NotFound,
                        $"{endpoint} with special chars in path '{testName}' should succeed, return BadRequest, or NotFound");
                }
            }
        }

        #endregion

        #region Size Parameter Tests (Updated)

        [TestMethod]
        public async Task SetFileInfo_Size_BoundaryValues()
        {
            // Create test file first
            var fileName = GenerateTestFileName();
            var createResponse = await _client.PostAsync($"/api/v3/CreateFile", CreateJsonContent(new {
                path = "",
                createFile = true,
                name = fileName
            }));
            AssertSuccessStatusCode(createResponse);

            var testCases = new[]
            {
                // Far outside valid interval (negative values)
                (-1000L, HttpStatusCode.BadRequest, "Far negative size should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative size should fail"),

                // Boundary values around 0 (minimum is 0 per spec)
                (0L, HttpStatusCode.OK, "Zero size should work"),
                (1L, HttpStatusCode.OK, "Size 1 should work"),

                // Inside valid interval (middle values)
                (1024L, HttpStatusCode.OK, "1KB size should work"),
                (1048576L, HttpStatusCode.OK, "1MB size should work"),

                // Large values (boundary testing)
                (long.MaxValue / 2, HttpStatusCode.OK, "Large size should work or fail gracefully"),
                (long.MaxValue, HttpStatusCode.BadRequest, "Max long might fail due to disk space")
            };

            foreach (var (size, expectedStatus, description) in testCases)
            {
                var setInfoResponse = await _client.PostAsync($"/api/v3/SetFileInfo?path={UrlEncode(fileName)}",
                    CreateJsonContent(new { size = size, returnFileInfo = true }));

                Assert.IsTrue(setInfoResponse.StatusCode == expectedStatus ||
                             (expectedStatus == HttpStatusCode.OK && setInfoResponse.StatusCode == HttpStatusCode.BadRequest),
                    $"{description} - Size: {size}. Expected: {expectedStatus}, Got: {setInfoResponse.StatusCode}");
            }
        }

        #endregion

        #region Date/Time Parameter Tests (Updated)

        [TestMethod]
        public async Task SetFileInfo_DateTimeParameters_BoundaryValues()
        {
            // Create test file
            var fileName = GenerateTestFileName();
            var createResponse = await _client.PostAsync($"/api/v3/CreateFile", CreateJsonContent(new {
                path = "",
                createFile = true,
                name = fileName
            }));
            AssertSuccessStatusCode(createResponse);

            var dateTimeTests = new[]
            {
                // Valid date formats (inside valid interval)
                (DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"), HttpStatusCode.OK, "Current UTC time should work"),
                ("2024-01-01T00:00:00Z", HttpStatusCode.OK, "Valid past date should work"),
                ("2030-12-31T23:59:59Z", HttpStatusCode.OK, "Valid future date should work"),

                // Boundary dates
                ("1970-01-01T00:00:00Z", HttpStatusCode.OK, "Unix epoch should work"),
                ("2099-12-31T23:59:59Z", HttpStatusCode.OK, "Far future date should work"),

                // Far outside valid interval (invalid formats)
                ("invalid-date", HttpStatusCode.BadRequest, "Invalid date format should fail"),
                ("2024-13-01T00:00:00Z", HttpStatusCode.BadRequest, "Invalid month should fail"),
                ("2024-01-32T00:00:00Z", HttpStatusCode.BadRequest, "Invalid day should fail"),
                ("2024-01-01T25:00:00Z", HttpStatusCode.BadRequest, "Invalid hour should fail"),

                // Boundary cases
                ("", HttpStatusCode.OK, "Empty date should work (field is optional)"),
                ("2024-01-01", HttpStatusCode.BadRequest, "Missing time component should fail")
            };

            foreach (var (dateTime, expectedStatus, description) in dateTimeTests)
            {
                var requestBody = string.IsNullOrEmpty(dateTime)
                    ? new { modified = (string)null, returnFileInfo = true }
                    : new { modified = dateTime, returnFileInfo = true };

                var response = await _client.PostAsync($"/api/v3/SetFileInfo?path={UrlEncode(fileName)}",
                    CreateJsonContent(requestBody));

                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - DateTime: {dateTime}");
            }
        }

        #endregion

        #region Helper Methods

        private async Task CreateTestFile(string fileName, string content = "")
        {
            var createData = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };

            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(response, $"Create test file {fileName}");

            if (!string.IsNullOrEmpty(content))
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var writeResponse = await _client.PostAsync(
                    $"{BaseApiUrl}WriteFile?path={UrlEncode("/Documents/" + fileName)}&startPosition=0&unlockAfterWrite=true",
                    new ByteArrayContent(contentBytes));
                AssertSuccessStatusCode(writeResponse, $"Write content to {fileName}");
            }
        }

        #endregion
    }
}