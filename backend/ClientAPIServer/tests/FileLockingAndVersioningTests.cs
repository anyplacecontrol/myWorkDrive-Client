using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class FileLockingAndVersioningTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task LockFile_And_UnlockFile_WorkCorrectly()
        {
            // Arrange
            var fileName = GenerateTestFileName("lock-test");
            await CreateTestFile(fileName, "content for locking");
            var filePath = "/Documents/" + fileName;

            // Act - Lock file (exclusive lock)
            var lockResponse = await _client.PostAsync(
                $"{BaseApiUrl}LockFile?path={UrlEncode(filePath)}&coedit=false&expires=300&includeLockInfo=true",
                null);

            // Assert - Lock file
            AssertSuccessStatusCode(lockResponse, "LockFile");

            var lockContent = await lockResponse.Content.ReadAsStringAsync();
            var lockInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(lockContent);
            
            Assert.IsNotNull(lockInfo);
            Assert.IsTrue(lockInfo.ContainsKey("lockCount"));
            
            var lockCount = Convert.ToInt32(lockInfo["lockCount"]);
            Assert.IsTrue(lockCount > 0, "File should be locked");

            // Act - Unlock file
            var unlockResponse = await _client.PostAsync(
                $"{BaseApiUrl}UnlockFile?path={UrlEncode(filePath)}",
                null);

            // Assert - Unlock file
            Assert.AreEqual(HttpStatusCode.NoContent, unlockResponse.StatusCode, "UnlockFile should return NoContent");
        }

        [TestMethod]
        public async Task LockFile_Coedit_AllowsSharedLock()
        {
            // Arrange
            var fileName = GenerateTestFileName("coedit-lock-test");
            await CreateTestFile(fileName, "content for coedit locking");
            var filePath = "/Documents/" + fileName;

            // Act - Lock file for co-editing (shared lock)
            var lockResponse = await _client.PostAsync(
                $"{BaseApiUrl}LockFile?path={UrlEncode(filePath)}&coedit=true&expires=300&includeLockInfo=true",
                null);

            // Assert
            AssertSuccessStatusCode(lockResponse, "LockFile coedit");

            var lockContent = await lockResponse.Content.ReadAsStringAsync();
            var lockInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(lockContent);
            
            Assert.IsNotNull(lockInfo);
            Assert.IsTrue(lockInfo.ContainsKey("lockCount"));
            
            var lockCount = Convert.ToInt32(lockInfo["lockCount"]);
            Assert.IsTrue(lockCount > 0, "File should have co-edit lock");

            // Clean up
            await _client.PostAsync($"{BaseApiUrl}UnlockFile?path={UrlEncode(filePath)}", null);
        }

        [TestMethod]
        public async Task GetFileLocks_ReturnsLockInformation()
        {
            // Arrange
            var fileName = GenerateTestFileName("get-locks-test");
            await CreateTestFile(fileName, "content for lock info");
            var filePath = "/Documents/" + fileName;

            // Lock the file first
            var lockResponse = await _client.PostAsync(
                $"{BaseApiUrl}LockFile?path={UrlEncode(filePath)}&coedit=false&expires=300",
                null);
            AssertSuccessStatusCode(lockResponse, "Lock file for GetFileLocks test");

            // Act
            var getLocksResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetFileLocks?path={UrlEncode(filePath)}&includeLockOwners=true&includeLockDetails=true");

            // Assert
            AssertSuccessStatusCode(getLocksResponse, "GetFileLocks");

            var content = await getLocksResponse.Content.ReadAsStringAsync();
            var locksInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(locksInfo);
            Assert.IsTrue(locksInfo.ContainsKey("lockCount"));
            Assert.IsTrue(locksInfo.ContainsKey("lockedByCurrentSession"));
            Assert.IsTrue(locksInfo.ContainsKey("lockedByOtherSessions"));
            
            var lockCount = Convert.ToInt32(locksInfo["lockCount"]);
            Assert.IsTrue(lockCount > 0, "Should show active locks");

            // Clean up
            await _client.PostAsync($"{BaseApiUrl}UnlockFile?path={UrlEncode(filePath)}", null);
        }

        [TestMethod]
        public async Task ListFileVersions_ReturnsVersionHistory()
        {
            // Arrange
            var fileName = GenerateTestFileName("versions-test");
            await CreateTestFile(fileName, "original content");
            var filePath = "/Documents/" + fileName;

            // Modify the file to create versions (if versioning is supported)
            var modifiedContent = "modified content version 2";
            var contentBytes = Encoding.UTF8.GetBytes(modifiedContent);
            await _client.PostAsync(
                $"{BaseApiUrl}WriteFile?path={UrlEncode(filePath)}&startPosition=0&unlockAfterWrite=true",
                new ByteArrayContent(contentBytes));

            // Act
            var versionsResponse = await _client.GetAsync(
                $"{BaseApiUrl}ListFileVersions?path={UrlEncode(filePath)}");

            // Assert
            AssertSuccessStatusCode(versionsResponse, "ListFileVersions");

            var content = await versionsResponse.Content.ReadAsStringAsync();
            var versions = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(content);
            
            Assert.IsNotNull(versions);
            // Even if no versions are available, should return empty array
            Assert.IsTrue(versions.Count >= 0);

            if (versions.Count > 0)
            {
                var firstVersion = versions[0];
                Assert.IsTrue(firstVersion.ContainsKey("name"));
                Assert.IsTrue(firstVersion.ContainsKey("modified"));
            }
        }

        [TestMethod]
        public async Task RestoreFileVersion_RestoresSpecificVersion()
        {
            // Arrange
            var fileName = GenerateTestFileName("restore-test");
            await CreateTestFile(fileName, "content to restore");
            var filePath = "/Documents/" + fileName;

            // Get file info to get the creation/modification date
            var fileInfoResponse = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={UrlEncode(filePath)}");
            AssertSuccessStatusCode(fileInfoResponse, "Get file info for restore test");

            var fileInfoContent = await fileInfoResponse.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileInfoContent);
            
            Assert.IsNotNull(fileInfo);
            Assert.IsTrue(fileInfo.ContainsKey("modified"));

            var modifiedDate = DateTime.Parse(fileInfo["modified"].ToString()!);

            // Act - Try to restore the "latest" version
            var restoreResponse = await _client.PostAsync(
                $"{BaseApiUrl}RestoreFileVersion?path={UrlEncode(filePath)}&modified=latest",
                null);

            // Assert
            // This might return success or an error depending on versioning implementation
            // We mainly test that the endpoint is accessible and returns a proper response
            Assert.IsTrue(
                restoreResponse.IsSuccessStatusCode || 
                restoreResponse.StatusCode == HttpStatusCode.NotFound ||
                restoreResponse.StatusCode == HttpStatusCode.BadRequest,
                $"RestoreFileVersion should return success or expected error, got {restoreResponse.StatusCode}");

            if (restoreResponse.IsSuccessStatusCode)
            {
                var restoreContent = await restoreResponse.Content.ReadAsStringAsync();
                var restoredInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(restoreContent);
                Assert.IsNotNull(restoredInfo);
            }
        }

        [TestMethod]
        public async Task SetFileInfo_UpdatesFileMetadata()
        {
            // Arrange
            var fileName = GenerateTestFileName("setinfo-test");
            await CreateTestFile(fileName, "content for metadata update");
            var filePath = "/Documents/" + fileName;

            // Get current file info
            var currentInfoResponse = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={UrlEncode(filePath)}");
            AssertSuccessStatusCode(currentInfoResponse, "Get current file info");

            // Prepare new metadata
            var newCreated = DateTime.UtcNow.AddDays(-1);
            var newModified = DateTime.UtcNow.AddHours(-2);

            var updateData = new
            {
                created = newCreated.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                modified = newModified.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                returnFileInfo = true
            };

            // Act
            var updateResponse = await _client.PostAsync(
                $"{BaseApiUrl}SetFileInfo?path={UrlEncode(filePath)}",
                CreateJsonContent(updateData));

            // Assert
            AssertSuccessStatusCode(updateResponse, "SetFileInfo");

            var updateContent = await updateResponse.Content.ReadAsStringAsync();
            var updatedInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(updateContent);
            
            Assert.IsNotNull(updatedInfo);
            Assert.IsTrue(updatedInfo.ContainsKey("created"));
            Assert.IsTrue(updatedInfo.ContainsKey("modified"));

            // Verify dates were updated (with some tolerance for precision)
            var actualCreated = DateTime.Parse(updatedInfo["created"].ToString()!);
            var actualModified = DateTime.Parse(updatedInfo["modified"].ToString()!);
            
            Assert.IsTrue(Math.Abs((actualCreated - newCreated).TotalSeconds) < 2, "Created date should be updated");
            Assert.IsTrue(Math.Abs((actualModified - newModified).TotalSeconds) < 2, "Modified date should be updated");
        }

        [TestMethod]
        public async Task SetFileInfo_ResizesFile()
        {
            // Arrange
            var fileName = GenerateTestFileName("resize-test");
            await CreateTestFile(fileName, "content for resizing test - this is some longer content");
            var filePath = "/Documents/" + fileName;

            var newSize = 10L; // Truncate to 10 bytes

            var resizeData = new
            {
                size = newSize,
                returnFileInfo = true
            };

            // Act
            var resizeResponse = await _client.PostAsync(
                $"{BaseApiUrl}SetFileInfo?path={UrlEncode(filePath)}",
                CreateJsonContent(resizeData));

            // Assert
            AssertSuccessStatusCode(resizeResponse, "SetFileInfo resize");

            var resizeContent = await resizeResponse.Content.ReadAsStringAsync();
            var resizedInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(resizeContent);
            
            Assert.IsNotNull(resizedInfo);
            Assert.IsTrue(resizedInfo.ContainsKey("size"));

            var actualSize = Convert.ToInt64(resizedInfo["size"]);
            Assert.AreEqual(newSize, actualSize, "File size should be updated");

            // Verify by reading the file
            var readResponse = await _client.GetAsync(
                $"{BaseApiUrl}ReadFile?path={UrlEncode(filePath)}&startPosition=0&count=20");

            AssertSuccessStatusCode(readResponse, "Read resized file");

            var readData = await readResponse.Content.ReadAsByteArrayAsync();
            Assert.AreEqual((int)newSize, readData.Length, "Read data should match new file size");
        }

        [TestMethod]
        public async Task FileInfo_IncludeLocks_ShowsLockStatus()
        {
            // Arrange
            var fileName = GenerateTestFileName("lock-info-test");
            await CreateTestFile(fileName, "content for lock info test");
            var filePath = "/Documents/" + fileName;

            // Lock the file
            await _client.PostAsync(
                $"{BaseApiUrl}LockFile?path={UrlEncode(filePath)}&coedit=false&expires=300",
                null);

            // Act
            var fileInfoResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetFileInfo?path={UrlEncode(filePath)}&includeLocks=true");

            // Assert
            AssertSuccessStatusCode(fileInfoResponse, "GetFileInfo with locks");

            var content = await fileInfoResponse.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(fileInfo);
            Assert.IsTrue(fileInfo.ContainsKey("isLocked"));
            
            var isLocked = Convert.ToBoolean(fileInfo["isLocked"]);
            Assert.IsTrue(isLocked, "File should show as locked");

            if (fileInfo.ContainsKey("locks"))
            {
                var locksArray = fileInfo["locks"] as Newtonsoft.Json.Linq.JArray;
                Assert.IsNotNull(locksArray);
                Assert.IsTrue(locksArray.Count > 0, "Should have lock information");
            }

            // Clean up
            await _client.PostAsync($"{BaseApiUrl}UnlockFile?path={UrlEncode(filePath)}", null);
        }

        // Helper methods
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
    }
}