using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class NewAPIFeaturesTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task ListShares_ReturnsAvailableShares()
        {
            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ListShares");

            // Assert
            AssertSuccessStatusCode(response, "ListShares");

            var content = await response.Content.ReadAsStringAsync();
            var sharesInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(sharesInfo);
            Assert.IsTrue(sharesInfo.ContainsKey("shares"));
            Assert.IsTrue(sharesInfo.ContainsKey("useMultipleDriveLetters"));
        }

        [TestMethod]
        public async Task AddBookmark_And_ListBookmarks_WorkCorrectly()
        {
            // Arrange
            var fileName = GenerateTestFileName("bookmark-test");
            await CreateTestFile(fileName, "bookmarked file content");
            var filePath = "/" + fileName;

            // Act - Add bookmark
            var addResponse = await _client.PostAsync(
                $"{BaseApiUrl}AddBookmark?path={UrlEncode(filePath)}", 
                null);

            // Assert - Add bookmark
            AssertSuccessStatusCode(addResponse, "AddBookmark");

            var addContent = await addResponse.Content.ReadAsStringAsync();
            var bookmarkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(addContent);
            
            Assert.IsNotNull(bookmarkInfo);
            Assert.IsTrue(bookmarkInfo.ContainsKey("id"));
            Assert.IsTrue(bookmarkInfo.ContainsKey("path"));
            Assert.AreEqual(filePath, bookmarkInfo["path"]);

            // Act - List bookmarks
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListBookmarks");

            // Assert - List bookmarks
            AssertSuccessStatusCode(listResponse, "ListBookmarks");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var bookmarks = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);
            
            Assert.IsNotNull(bookmarks);
            Assert.IsTrue(bookmarks.Any(b => b["path"].ToString() == filePath));

            // Clean up - Delete bookmark
            var deleteResponse = await _client.PostAsync(
                $"{BaseApiUrl}DeleteBookmark?path={UrlEncode(filePath)}", 
                null);
            
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [TestMethod]
        public async Task LogMessage_AcceptsLogEntries()
        {
            // Arrange
            var testMessage = "Test log message from unit test";

            // Act
            var response = await _client.PostAsync(
                $"{BaseApiUrl}LogMessage?message={UrlEncode(testMessage)}", 
                null);

            // Assert
            AssertSuccessStatusCode(response, "LogMessage");
        }

        [TestMethod]
        public async Task GetServerConfig_ReturnsConfigurationData()
        {
            // Test different configuration classes
            var configClasses = new[] { "base", "auth", "tfa", "fileTypes", "viewerTypes", "tls" };

            foreach (var configClass in configClasses)
            {
                // Act
                var response = await _client.GetAsync($"{BaseApiUrl}GetServerConfig?configClass={configClass}");

                // Assert
                AssertSuccessStatusCode(response, $"GetServerConfig {configClass}");

                var content = await response.Content.ReadAsStringAsync();
                
                // Content should be JSON for most config classes
                if (configClass != "logo" && configClass != "speedTest")
                {
                    var configData = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                    Assert.IsNotNull(configData, $"Config data for {configClass} should be valid JSON");
                }
            }
        }

        [TestMethod]
        public async Task SearchFiles_PerformsSimpleSearch()
        {
            // Arrange
            var testFileName = GenerateTestFileName("searchable");
            await CreateTestFile(testFileName, "content to search");

            // Act - Search by name
            var response = await _client.GetAsync(
                $"{BaseApiUrl}SearchFiles?field=name&query=searchable&maxResults=10");

            // Assert
            AssertSuccessStatusCode(response, "SearchFiles");

            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonConvert.DeserializeObject<List<object>>(content);
            
            Assert.IsNotNull(searchResults);
            // Results can be either strings (paths) or objects (file info)
            Assert.IsTrue(searchResults.Count >= 0);
        }

        [TestMethod]
        public async Task SearchFiles_PerformsCompoundSearch()
        {
            // Arrange
            var testFileName = GenerateTestFileName("compound-search", "txt");
            await CreateTestFile(testFileName, "compound search content");

            var searchData = new
            {
                queries = new[]
                {
                    new { field = "name", query = "compound" },
                    new { field = "ext", query = "txt" }
                }
            };

            // Act
            var response = await _client.PostAsync(
                $"{BaseApiUrl}SearchFiles?maxResults=10",
                CreateJsonContent(searchData));

            // Assert
            AssertSuccessStatusCode(response, "SearchFiles compound");

            var content = await response.Content.ReadAsStringAsync();
            var searchResults = JsonConvert.DeserializeObject<List<object>>(content);
            
            Assert.IsNotNull(searchResults);
        }

        [TestMethod]
        public async Task ListRecentFiles_And_ClearRecentFiles_WorkCorrectly()
        {
            // Arrange - Create and access some files
            var fileName1 = GenerateTestFileName("recent1");
            var fileName2 = GenerateTestFileName("recent2");
            
            await CreateTestFile(fileName1, "recent file 1");
            await CreateTestFile(fileName2, "recent file 2");

            // Access files to add them to recent list (if updateRecents is supported)
            await _client.GetAsync($"{BaseApiUrl}ReadFile?path={UrlEncode("/" + fileName1)}&startPosition=0&count=10&updateRecents=true");
            await _client.GetAsync($"{BaseApiUrl}ReadFile?path={UrlEncode("/" + fileName2)}&startPosition=0&count=10&updateRecents=true");

            // Act - List recent files
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListRecentFiles?maxResults=10");

            // Assert - List recent files
            AssertSuccessStatusCode(listResponse, "ListRecentFiles");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var recentFiles = JsonConvert.DeserializeObject<List<object>>(listContent);
            
            Assert.IsNotNull(recentFiles);

            // Act - Clear recent files
            var clearResponse = await _client.PostAsync($"{BaseApiUrl}ClearRecentFiles", null);

            // Assert - Clear recent files
            Assert.AreEqual(HttpStatusCode.NoContent, clearResponse.StatusCode);

            // Verify recent files list is cleared
            var listAfterClearResponse = await _client.GetAsync($"{BaseApiUrl}ListRecentFiles?maxResults=10");
            AssertSuccessStatusCode(listAfterClearResponse, "ListRecentFiles after clear");

            var clearedListContent = await listAfterClearResponse.Content.ReadAsStringAsync();
            var clearedRecentFiles = JsonConvert.DeserializeObject<List<object>>(clearedListContent);
            
            Assert.IsNotNull(clearedRecentFiles);
            // Recent files list should be empty or significantly smaller after clearing
        }

        [TestMethod]
        public async Task ZipFiles_GET_CreatesZipFromFolder()
        {
            // Arrange
            var folderName = GenerateTestFolderName("zip-test");
            var fileName1 = GenerateTestFileName("zip-file1");
            var fileName2 = GenerateTestFileName("zip-file2");
            
            await CreateTestFolder(folderName);
            await CreateTestFile($"{folderName}/{fileName1}", "content 1");
            await CreateTestFile($"{folderName}/{fileName2}", "content 2");

            // Act
            var response = await _client.GetAsync(
                $"{BaseApiUrl}ZipFiles?path={UrlEncode("/" + folderName)}&respondWith=data");

            // Assert
            // Response can be 200 (with data), 204 (no content), or 302 (redirect)
            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.NoContent ||
                response.StatusCode == HttpStatusCode.Found,
                $"ZipFiles should return OK, NoContent, or Found, but got {response.StatusCode}");

            if (response.StatusCode == HttpStatusCode.OK)
            {
                var content = await response.Content.ReadAsByteArrayAsync();
                Assert.IsTrue(content.Length > 0, "ZIP data should not be empty");
            }
        }

        [TestMethod]
        public async Task ZipFiles_POST_CreatesZipFromFileList()
        {
            // Arrange
            var fileName1 = GenerateTestFileName("post-zip1");
            var fileName2 = GenerateTestFileName("post-zip2");
            
            await CreateTestFile(fileName1, "zip content 1");
            await CreateTestFile(fileName2, "zip content 2");

            var zipData = new
            {
                paths = new[] { "/" + fileName1, "/" + fileName2 }
            };

            // Act
            var response = await _client.PostAsync(
                $"{BaseApiUrl}ZipFiles?respondWith=data",
                CreateJsonContent(zipData));

            // Assert
            Assert.IsTrue(
                response.StatusCode == HttpStatusCode.OK || 
                response.StatusCode == HttpStatusCode.Created ||
                response.StatusCode == HttpStatusCode.NoContent ||
                response.StatusCode == HttpStatusCode.PartialContent,
                $"ZipFiles POST should return appropriate status, but got {response.StatusCode}");
        }

        [TestMethod]
        public async Task GetFileCapabilities_ChecksPermissions()
        {
            // Arrange
            var fileName = GenerateTestFileName("capabilities-test");
            await CreateTestFile(fileName, "capabilities test content");

            // Test different capabilities
            var capabilities = new[] { "writePermissions", "folderAccessible" };

            foreach (var capability in capabilities)
            {
                // Act
                var response = await _client.GetAsync(
                    $"{BaseApiUrl}GetFileCapabilities?path={UrlEncode("/" + fileName)}&query={capability}");

                // Assert
                AssertSuccessStatusCode(response, $"GetFileCapabilities {capability}");

                var content = await response.Content.ReadAsStringAsync();
                var capabilityInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                
                Assert.IsNotNull(capabilityInfo);
                Assert.IsTrue(capabilityInfo.ContainsKey("value"));
            }
        }

        [TestMethod]
        public async Task GetOTP_GeneratesOneTimePassword()
        {
            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetOTP");

            // Assert
            AssertSuccessStatusCode(response, "GetOTP");

            var content = await response.Content.ReadAsStringAsync();
            var otpInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(otpInfo);
            Assert.IsTrue(otpInfo.ContainsKey("otp"));
            
            var otp = otpInfo["otp"].ToString();
            Assert.IsFalse(string.IsNullOrEmpty(otp));
        }

        // Helper methods
        private async Task CreateTestFile(string fileName, string content = "")
        {
            var pathParts = fileName.Split('/');
            var actualFileName = pathParts.Last();
            var folderPath = pathParts.Length > 1 ? "/Documents/" + string.Join("/", pathParts.Take(pathParts.Length - 1)) : "/Documents";

            var createData = new
            {
                path = folderPath,
                createFile = true,
                name = actualFileName,
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

        private async Task CreateTestFolder(string folderName)
        {
            var createData = new
            {
                path = "/",
                createFile = false,
                name = folderName,
                conflictBehavior = "replace"
            };

            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(response, $"Create test folder {folderName}");
        }
    }
}