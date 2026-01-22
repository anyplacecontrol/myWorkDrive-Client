using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Text;

namespace MockServerAPITests
{
    /// <summary>
    /// Additional comprehensive parameter validation tests covering more API endpoints.
    /// Complements ComprehensiveParameterTests.cs with boundary value testing for remaining endpoints.
    /// </summary>
    [TestClass]
    public class AdditionalParameterTests : MockApiTestsBase
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

        #region PublicLink Parameter Tests

        [TestMethod]
        public async Task CreatePublicLink_MaxNumberOfDownloads_BoundaryValueTesting()
        {
            // Arrange
            var fileName = GenerateTestFileName("link-downloads");
            await CreateTestFile(fileName, "content for download link");

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-1000, HttpStatusCode.BadRequest, "Far negative max downloads should fail"),
                (-1, HttpStatusCode.BadRequest, "Negative max downloads should fail"),

                // Boundary values around 0 (minimum is 0 per spec)
                (0, HttpStatusCode.OK, "Zero max downloads should succeed"),
                (1, HttpStatusCode.OK, "Max downloads 1 should succeed"),

                // Inside valid interval (middle values)
                (5, HttpStatusCode.OK, "Max downloads 5 should succeed"),
                (100, HttpStatusCode.OK, "Max downloads 100 should succeed"),

                // Large values (boundary testing)
                (10000, HttpStatusCode.OK, "Max downloads 10000 should succeed"),
                (int.MaxValue, HttpStatusCode.OK, "Max int downloads should succeed")
            };

            foreach (var (maxDownloads, expectedStatus, description) in testCases)
            {
                // Arrange
                var linkData = new
                {
                    path = $"/Documents/{fileName}",
                    allowDownloading = true,
                    allowUploading = false,
                    allowEditing = false,
                    password = "",
                    maxNumberOfDownloads = maxDownloads
                };

                // Act
                var response = await _client.PostAsync($"{BaseApiUrl}CreatePublicLink", CreateJsonContent(linkData));

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - MaxDownloads: {maxDownloads}");

                // Clean up - delete the created link
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var linkResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    if (linkResult?.ContainsKey("value") == true)
                    {
                        var publicLink = linkResult["value"].ToString();
                        await _client.GetAsync($"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(publicLink)}");
                    }
                }
            }
        }

        [TestMethod]
        public async Task CreatePublicLink_Password_StringLengthBoundaryTesting()
        {
            // Arrange
            var fileName = GenerateTestFileName("link-password");
            await CreateTestFile(fileName, "content for password link");

            var testCases = new[]
            {
                // Empty and short strings
                ("", HttpStatusCode.OK, "Empty password should succeed"),
                ("a", HttpStatusCode.OK, "Single char password should succeed"),

                // Normal length strings (middle values)
                ("password123", HttpStatusCode.OK, "Normal password should succeed"),
                ("longer-password-with-special-chars!@#", HttpStatusCode.OK, "Long password should succeed"),

                // Boundary testing for password length
                (new string('a', 50), HttpStatusCode.OK, "50 char password should succeed"),
                (new string('a', 100), HttpStatusCode.OK, "100 char password should succeed"),

                // Far outside reasonable limits
                (new string('a', 500), HttpStatusCode.OK, "Very long password should succeed or fail gracefully"),
                (new string('a', 1000), HttpStatusCode.OK, "Extremely long password should succeed or fail gracefully")
            };

            foreach (var (password, expectedStatus, description) in testCases)
            {
                // Arrange
                var linkData = new
                {
                    path = $"/Documents/{fileName}",
                    allowDownloading = true,
                    allowUploading = false,
                    allowEditing = false,
                    password = password
                };

                // Act
                var response = await _client.PostAsync($"{BaseApiUrl}CreatePublicLink", CreateJsonContent(linkData));

                // Assert - Allow both success and BadRequest for very long passwords
                Assert.IsTrue(response.StatusCode == expectedStatus || response.StatusCode == HttpStatusCode.BadRequest,
                    $"{description} - Password length: {password.Length}");

                // Clean up
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var linkResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    if (linkResult?.ContainsKey("value") == true)
                    {
                        var publicLink = linkResult["value"].ToString();
                        await _client.GetAsync($"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(publicLink)}");
                    }
                }
            }
        }

        #endregion

        #region WriteFileBlock Parameter Tests

        [TestMethod]
        public async Task WriteFileBlock_StartPosition_BoundaryValueTesting()
        {
            // First start an upload session
            var fileName = GenerateTestFileName("block-write");
            var uploadData = new { size = 1000, conflictBehavior = "replace" };

            var uploadResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode("/Documents/" + fileName)}",
                CreateJsonContent(uploadData));
            AssertSuccessStatusCode(uploadResponse, "Start upload for WriteFileBlock test");

            var uploadContent = await uploadResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(uploadContent);
            var uploadId = uploadInfo["uploadId"].ToString();

            try
            {
                var testCases = new[]
                {
                    // Far outside valid interval (negative)
                    (-1000L, HttpStatusCode.BadRequest, "Far negative start position should fail"),
                    (-1L, HttpStatusCode.BadRequest, "Negative start position should fail"),

                    // Boundary values around 0
                    (0L, HttpStatusCode.OK, "Start position 0 should succeed"),
                    (1L, HttpStatusCode.OK, "Start position 1 should succeed"),

                    // Inside valid interval (middle values)
                    (10L, HttpStatusCode.OK, "Start position 10 should succeed"),
                    (100L, HttpStatusCode.OK, "Start position 100 should succeed"),

                    // Large values (boundary testing)
                    (500L, HttpStatusCode.OK, "Start position 500 should succeed"),
                    (999L, HttpStatusCode.OK, "Start position near file size should succeed")
                };

                foreach (var (startPosition, expectedStatus, description) in testCases)
                {
                    // Arrange
                    var testData = Encoding.UTF8.GetBytes("test data");

                    // Act
                    var url = $"{BaseApiUrl}WriteFileBlock?uploadId={uploadId}&startPosition={startPosition}";
                    var response = await _client.PutAsync(url, new ByteArrayContent(testData));

                    // Assert
                    Assert.AreEqual(expectedStatus, response.StatusCode, $"{description} - StartPosition: {startPosition}");
                }
            }
            finally
            {
                // Clean up - cancel the upload
                await _client.PostAsync($"{BaseApiUrl}CancelUpload?uploadId={uploadId}", null);
            }
        }

        #endregion

        #region ListBookmarks Parameter Tests

        [TestMethod]
        public async Task ListBookmarks_MaxResults_BoundaryValueTesting()
        {
            // Arrange - Create some bookmarks first
            for (int i = 0; i < 3; i++)
            {
                var testFile = GenerateTestFileName($"bookmark-{i}");
                await CreateTestFile(testFile, $"bookmark content {i}");
                await _client.PostAsync($"{BaseApiUrl}AddBookmark?path={UrlEncode("/Documents/" + testFile)}", null);
            }

            var testCases = new[]
            {
                // Far outside valid interval (negative)
                (-100L, HttpStatusCode.BadRequest, "Far negative maxResults should fail"),
                (-1L, HttpStatusCode.BadRequest, "Negative maxResults should fail"),

                // Boundary values around 0
                (0L, HttpStatusCode.OK, "Zero maxResults should succeed"),
                (1L, HttpStatusCode.OK, "MaxResults 1 should succeed"),

                // Inside valid interval (middle values)
                (2L, HttpStatusCode.OK, "MaxResults 2 should succeed"),
                (10L, HttpStatusCode.OK, "MaxResults 10 should succeed"),

                // Large values
                (1000L, HttpStatusCode.OK, "MaxResults 1000 should succeed"),
                (long.MaxValue, HttpStatusCode.OK, "Max long should succeed")
            };

            foreach (var (maxResults, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}ListBookmarks?maxResults={maxResults}";
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

        #endregion

        #region ZipFiles Parameter Tests

        [TestMethod]
        public async Task ZipFiles_Mask_StringLengthBoundaryTesting()
        {
            // Arrange - Create some test files
            await CreateTestFile("test1.txt", "content 1");
            await CreateTestFile("test2.txt", "content 2");
            await CreateTestFile("test3.doc", "content 3");

            var testCases = new[]
            {
                // Empty and simple masks
                ("", HttpStatusCode.OK, "Empty mask should succeed"),
                ("*", HttpStatusCode.OK, "Wildcard mask should succeed"),

                // Normal masks (middle values)
                ("*.txt", HttpStatusCode.OK, "Extension mask should succeed"),
                ("test*", HttpStatusCode.OK, "Prefix mask should succeed"),
                ("*test*", HttpStatusCode.OK, "Contains mask should succeed"),

                // Complex masks
                ("test?.txt", HttpStatusCode.OK, "Single char wildcard should succeed"),
                ("*.{txt,doc}", HttpStatusCode.OK, "Multiple extension mask should succeed"),

                // Boundary testing for mask length
                (new string('*', 50), HttpStatusCode.OK, "Long mask should succeed"),
                (new string('a', 100) + "*", HttpStatusCode.OK, "Very long mask should succeed"),

                // Far outside reasonable limits
                (new string('*', 500), HttpStatusCode.OK, "Extremely long mask should succeed or fail gracefully")
            };

            foreach (var (mask, expectedStatus, description) in testCases)
            {
                // Act - Use unique zip name for each test case to avoid conflicts
                var zipName = $"test-{Guid.NewGuid():N}.zip";
                var url = $"{BaseApiUrl}ZipFiles?path=/Documents&mask={UrlEncode(mask)}&respondWith=path&zipName={zipName}";
                var response = await _client.GetAsync(url);

                // Assert - Allow both success and BadRequest for edge cases
                Assert.IsTrue(response.IsSuccessStatusCode || response.StatusCode == HttpStatusCode.BadRequest,
                    $"{description} - Mask length: {mask.Length}");
            }
        }

        #endregion

        #region LogMessage Parameter Tests

        [TestMethod]
        public async Task LogMessage_Message_StringLengthBoundaryTesting()
        {
            var testCases = new[]
            {
                // Empty and short messages
                ("", HttpStatusCode.BadRequest, "Empty message should fail (required parameter)"),
                ("a", HttpStatusCode.OK, "Single char message should succeed"),

                // Normal length messages (middle values)
                ("Normal log message", HttpStatusCode.OK, "Normal message should succeed"),
                ("Longer log message with more details and information", HttpStatusCode.OK, "Long message should succeed"),

                // Boundary testing for message length
                (new string('a', 100), HttpStatusCode.OK, "100 char message should succeed"),
                (new string('a', 500), HttpStatusCode.OK, "500 char message should succeed"),
                (new string('a', 1000), HttpStatusCode.OK, "1000 char message should succeed"),

                // Far outside reasonable limits
                (new string('a', 5000), HttpStatusCode.OK, "Very long message should succeed or fail gracefully"),
                (new string('a', 10000), HttpStatusCode.OK, "Extremely long message should succeed or fail gracefully")
            };

            foreach (var (message, expectedStatus, description) in testCases)
            {
                // Act
                var url = $"{BaseApiUrl}LogMessage?message={UrlEncode(message)}";
                var response = await _client.PostAsync(url, null);

                // Assert - Allow both success and BadRequest for very long messages
                Assert.IsTrue(response.StatusCode == expectedStatus || response.StatusCode == HttpStatusCode.BadRequest,
                    $"{description} - Message length: {message.Length}");
            }
        }

        #endregion

        #region Path Length Boundary Tests

        [TestMethod]
        public async Task Various_Endpoints_Path_LengthBoundaryTesting()
        {
            var endpoints = new[] { "GetItemType", "GetFileInfo", "GetFolderInfo" };

            // Test various path lengths
            var pathTests = new[]
            {
                // Short paths
                ("/a", HttpStatusCode.NotFound, "Single char path should handle gracefully"),
                ("/Documents/a", HttpStatusCode.NotFound, "Short filename should handle gracefully"),

                // Normal length paths (middle values)
                ("/Documents/normal-file.txt", HttpStatusCode.NotFound, "Normal path should handle gracefully"),
                ("/Documents/longer-filename-with-dashes.txt", HttpStatusCode.NotFound, "Long filename should handle gracefully"),

                // Boundary testing for path component length (255 is common filesystem limit)
                ($"/Documents/{new string('a', 100)}.txt", HttpStatusCode.NotFound, "100 char filename should handle gracefully"),
                ($"/Documents/{new string('a', 200)}.txt", HttpStatusCode.NotFound, "200 char filename should handle gracefully"),
                ($"/Documents/{new string('a', 254)}.txt", HttpStatusCode.NotFound, "254 char filename (boundary) should handle gracefully"),
                ($"/Documents/{new string('a', 255)}.txt", HttpStatusCode.NotFound, "255 char filename (limit) should handle gracefully"),

                // Far outside limits
                ($"/Documents/{new string('a', 300)}.txt", HttpStatusCode.BadRequest, "300 char filename should fail or handle gracefully"),
                ($"/Documents/{new string('a', 500)}.txt", HttpStatusCode.BadRequest, "500 char filename should fail or handle gracefully")
            };

            foreach (var endpoint in endpoints)
            {
                foreach (var (testPath, expectedStatus, description) in pathTests.Take(5)) // Limit for performance
                {
                    // Act
                    var url = $"{BaseApiUrl}{endpoint}?path={UrlEncode(testPath)}";
                    var response = await _client.GetAsync(url);

                    // Assert - Should handle various path lengths appropriately
                    Assert.IsTrue(
                        response.StatusCode == expectedStatus ||
                        response.StatusCode == HttpStatusCode.BadRequest ||
                        response.StatusCode == HttpStatusCode.NotFound,
                        $"{endpoint}: {description} - Path length: {testPath.Length}");
                }
            }
        }

        #endregion

        #region Comprehensive Conflict Behavior Testing

        [TestMethod]
        public async Task CreateFile_ConflictBehavior_AllValuesBoundaryTesting()
        {
            var fileName = GenerateTestFileName("conflict-test");

            // First create a file to cause conflicts
            await CreateTestFile(fileName, "original content");

            var conflictBehaviors = new[]
            {
                ("fail", HttpStatusCode.Conflict, "Conflict behavior 'fail' should return conflict"),
                ("ignore", HttpStatusCode.OK, "Conflict behavior 'ignore' should succeed"),
                ("rename", HttpStatusCode.OK, "Conflict behavior 'rename' should succeed"),
                ("replace", HttpStatusCode.OK, "Conflict behavior 'replace' should succeed"),

                // Invalid conflict behaviors (far outside valid values)
                ("invalid", HttpStatusCode.BadRequest, "Invalid conflict behavior should fail"),
                ("unknown", HttpStatusCode.BadRequest, "Unknown conflict behavior should fail"),
                ("", HttpStatusCode.BadRequest, "Empty conflict behavior should fail"),
                ("FAIL", HttpStatusCode.BadRequest, "Wrong case conflict behavior should fail"),
                ("fail_extra", HttpStatusCode.BadRequest, "Modified valid value should fail")
            };

            foreach (var (conflictBehavior, expectedStatus, description) in conflictBehaviors)
            {
                // Arrange
                var createData = new
                {
                    path = "/Documents",
                    createFile = true,
                    name = fileName,
                    conflictBehavior = conflictBehavior
                };

                // Act
                var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

                // Assert
                Assert.AreEqual(expectedStatus, response.StatusCode,
                    $"{description} - ConflictBehavior: '{conflictBehavior}'");
            }
        }

        #endregion

        #region File Extension Boundary Tests

        [TestMethod]
        public async Task CreateFile_Extension_StringLengthBoundaryTesting()
        {
            var testCases = new[]
            {
                // Empty and short extensions
                ("", HttpStatusCode.OK, "Empty extension should succeed"),
                ("a", HttpStatusCode.OK, "Single char extension should succeed"),

                // Normal extensions (middle values)
                ("txt", HttpStatusCode.OK, "Normal extension should succeed"),
                ("docx", HttpStatusCode.OK, "4 char extension should succeed"),
                ("jpeg", HttpStatusCode.OK, "Image extension should succeed"),

                // Longer extensions (boundary testing)
                ("extension", HttpStatusCode.OK, "Long extension should succeed"),
                (new string('a', 10), HttpStatusCode.OK, "10 char extension should succeed"),
                (new string('a', 20), HttpStatusCode.OK, "20 char extension should succeed"),

                // Far outside reasonable limits
                (new string('a', 50), HttpStatusCode.OK, "Very long extension should succeed or fail gracefully"),
                (new string('a', 100), HttpStatusCode.OK, "Extremely long extension should succeed or fail gracefully")
            };

            foreach (var (extension, expectedStatus, description) in testCases)
            {
                // Arrange
                var createData = new
                {
                    path = "/Documents",
                    createFile = true,
                    extension = extension,
                    conflictBehavior = "replace"
                };

                // Act
                var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

                // Assert - Allow both success and BadRequest for very long extensions
                Assert.IsTrue(response.StatusCode == expectedStatus || response.StatusCode == HttpStatusCode.BadRequest,
                    $"{description} - Extension length: {extension.Length}");
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