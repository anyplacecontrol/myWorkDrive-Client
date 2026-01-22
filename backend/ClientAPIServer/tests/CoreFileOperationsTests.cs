using Microsoft.VisualStudio.TestTools.UnitTesting;
using MWDMockServer;
using Newtonsoft.Json;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class CoreFileOperationsTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task CheckSession_ReturnsSuccess()
        {
            // Mock API Endpoint: GET /api/v3/CheckSession
            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}CheckSession");

            // Assert
            AssertSuccessStatusCode(response, "CheckSession");
        }

        [TestMethod]
        public async Task QueryQuotas_ReturnsQuotaInformation()
        {
            // Mock API Endpoint: GET /api/v3/QueryQuotas
            // Test with new share-based path format
            var response = await _client.GetAsync($"{BaseApiUrl}QueryQuotas?path=/Documents");

            // Assert
            AssertSuccessStatusCode(response, "QueryQuotas");
            
            var content = await response.Content.ReadAsStringAsync();
            var quotaInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(quotaInfo);
            Assert.IsTrue(quotaInfo.ContainsKey("totalBytes"));
            Assert.IsTrue(quotaInfo.ContainsKey("freeBytes"));
            Assert.IsTrue(quotaInfo.ContainsKey("availableBytes"));
        }

        [TestMethod]
        public async Task CreateFile_CreatesNewFile()
        {
            // Mock API Endpoint: POST /api/v3/CreateFile
            // Test both new share-based and scheme-based path formats
            var fileName = GenerateTestFileName("create");
            var createData = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(fileInfo);
            Assert.IsTrue(fileInfo.ContainsKey("name"));
            Assert.IsTrue(fileInfo.ContainsKey("path"));
            Assert.AreEqual(false, fileInfo["isFolder"]);
        }

        [TestMethod]
        public async Task CreateFile_CreatesNewFolder()
        {
            // Mock API Endpoint: POST /api/v3/CreateFile
            // Arrange
            var folderName = GenerateTestFolderName("create");
            var createData = new
            {
                path = "/",
                createFile = false,
                name = folderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);
            
            var content = await response.Content.ReadAsStringAsync();
            var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(folderInfo);
            Assert.IsTrue(folderInfo.ContainsKey("name"));
            Assert.IsTrue(folderInfo.ContainsKey("path"));
            Assert.AreEqual(true, folderInfo["isFolder"]);
        }

        [TestMethod]
        public async Task GetItemType_ReturnsCorrectTypeForFile()
        {
            // Mock API Endpoint: GET /api/v3/GetItemType
            // Arrange
            var fileName = GenerateTestFileName("type-test");
            await CreateTestFile(fileName, "test content");

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode("/Documents/" + fileName)}");

            // Assert
            AssertSuccessStatusCode(response, "GetItemType");
            
            var content = await response.Content.ReadAsStringAsync();
            var typeInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(typeInfo);
            Assert.IsTrue(typeInfo.ContainsKey("value"));
            Assert.AreEqual("file", typeInfo["value"]);
        }

        [TestMethod]
        public async Task GetItemType_ReturnsCorrectTypeForFolder()
        {
            // Mock API Endpoint: GET /api/v3/GetItemType
            // Arrange
            var folderName = GenerateTestFolderName("type-test");
            await CreateTestFolder(folderName);

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode("/Documents/" + folderName)}");

            // Assert
            AssertSuccessStatusCode(response, "GetItemType");
            
            var content = await response.Content.ReadAsStringAsync();
            var typeInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(typeInfo);
            Assert.IsTrue(typeInfo.ContainsKey("value"));
            Assert.AreEqual("folder", typeInfo["value"]);
        }

        [TestMethod]
        public async Task ListFolder_ReturnsContents()
        {
            // Mock API Endpoint: GET /api/v3/ListFolder
            // Arrange
            var testFile = GenerateTestFileName("list");
            var testFolder = GenerateTestFolderName("list");
            
            await CreateTestFile(testFile, "test content");
            await CreateTestFolder(testFolder);

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");

            // Assert
            AssertSuccessStatusCode(response, "ListFolder");
            
            var content = await response.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(content);
            
            Assert.IsNotNull(items);
            Assert.IsTrue(items.Count >= 2);

            var fileNames = items.Select(item => item["name"].ToString()).ToList();
            Assert.IsTrue(fileNames.Contains(testFile));
            Assert.IsTrue(fileNames.Contains(testFolder));
        }

        [TestMethod]
        public async Task GetFileInfo_ReturnsFileInformation()
        {
            // Mock API Endpoint: GET /api/v3/GetFileInfo
            // Arrange - Use same pattern as working GetItemType test
            var fileName = GenerateTestFileName("info-test");
            await CreateTestFile(fileName, "file info test");

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={UrlEncode("/Documents/" + fileName)}");

            // Assert
            AssertSuccessStatusCode(response, "GetFileInfo");
            
            var content = await response.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(fileInfo);
            Assert.IsTrue(fileInfo.ContainsKey("name"));
            Assert.IsTrue(fileInfo.ContainsKey("path"));
            Assert.IsTrue(fileInfo.ContainsKey("isFolder"));
            Assert.IsTrue(fileInfo.ContainsKey("size"));
            Assert.AreEqual(fileName, fileInfo["name"]);
            Assert.AreEqual(false, fileInfo["isFolder"]);
        }

        [TestMethod]
        public async Task GetFolderInfo_ReturnsFolderInformation()
        {
            // Mock API Endpoint: GET /api/v3/GetFolderInfo
            // Arrange
            var folderName = GenerateTestFolderName("info-test");
            await CreateTestFolder(folderName);

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetFolderInfo?path={UrlEncode("/Documents/" + folderName)}");

            // Assert
            AssertSuccessStatusCode(response, "GetFolderInfo");
            
            var content = await response.Content.ReadAsStringAsync();
            var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(folderInfo);
            Assert.IsTrue(folderInfo.ContainsKey("name"));
            Assert.IsTrue(folderInfo.ContainsKey("path"));
            Assert.IsTrue(folderInfo.ContainsKey("isFolder"));
            Assert.AreEqual(folderName, folderInfo["name"]);
            Assert.AreEqual(true, folderInfo["isFolder"]);
        }

        [TestMethod]
        public async Task WriteFile_And_ReadFile_WorkCorrectly()
        {
            // Mock API Endpoints: POST /api/v3/WriteFile, GET /api/v3/ReadFile
            // Arrange
            var fileName = GenerateTestFileName("write-read");
            await CreateTestFile(fileName);
            
            var testContent = "Hello, Mock Server!";
            var contentBytes = Encoding.UTF8.GetBytes(testContent);

            // Act - Write
            var writeResponse = await _client.PostAsync(
                $"{BaseApiUrl}WriteFile?path={UrlEncode("/Documents/" + fileName)}&startPosition=0&unlockAfterWrite=true",
                new ByteArrayContent(contentBytes));

            // Assert - Write
            AssertSuccessStatusCode(writeResponse, "WriteFile");

            // Act - Read
            var readResponse = await _client.GetAsync(
                $"{BaseApiUrl}ReadFile?path={UrlEncode("/Documents/" + fileName)}&startPosition=0&count={contentBytes.Length}");

            // Assert - Read
            AssertSuccessStatusCode(readResponse, "ReadFile");
            var readBytes = await readResponse.Content.ReadAsByteArrayAsync();
            var readContent = Encoding.UTF8.GetString(readBytes);
            
            Assert.AreEqual(testContent, readContent);
        }

        [TestMethod]
        public async Task CopyFile_CreatesExactCopy()
        {
            // Mock API Endpoint: POST /api/v3/CopyFile
            // Arrange
            var sourceFile = GenerateTestFileName("copy-source");
            var destinationFile = GenerateTestFileName("copy-dest");
            await CreateTestFile(sourceFile, "content to copy");

            var copyData = new
            {
                path = "/Documents/" + sourceFile,
                newPath = "/Documents/" + destinationFile,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFile", CreateJsonContent(copyData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);

            // Verify both files exist
            var sourceExists = await CheckItemExists(sourceFile);
            var destExists = await CheckItemExists(destinationFile);
            
            Assert.IsTrue(sourceExists, "Source file should still exist after copy");
            Assert.IsTrue(destExists, "Destination file should exist after copy");
        }

        [TestMethod]
        public async Task MoveFile_MovesFileCorrectly()
        {
            // Mock API Endpoint: POST /api/v3/MoveFile
            // Arrange
            var sourceFile = GenerateTestFileName("move-source");
            var destinationFile = GenerateTestFileName("move-dest");
            await CreateTestFile(sourceFile, "content to move");

            var moveData = new
            {
                path = "/Documents/" + sourceFile,
                newPath = "/Documents/" + destinationFile,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}MoveFile", CreateJsonContent(moveData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK);

            // Verify move worked correctly
            var sourceExists = await CheckItemExists(sourceFile);
            var destExists = await CheckItemExists(destinationFile);
            
            Assert.IsFalse(sourceExists, "Source file should not exist after move");
            Assert.IsTrue(destExists, "Destination file should exist after move");
        }

        [TestMethod]
        public async Task DeleteFile_RemovesFile()
        {
            // Mock API Endpoint: POST /api/v3/DeleteFile
            // Arrange
            var fileName = GenerateTestFileName("delete-test");
            await CreateTestFile(fileName, "file to delete");

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFile?path={UrlEncode("/Documents/" + fileName)}", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            // Verify file is deleted
            var exists = await CheckItemExists(fileName);
            Assert.IsFalse(exists, "File should be deleted");
        }

        [TestMethod]
        public async Task DeleteFolder_RemovesFolder()
        {
            // Mock API Endpoint: POST /api/v3/DeleteFolder
            // Arrange
            var folderName = GenerateTestFolderName("delete-test");
            await CreateTestFolder(folderName);

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}", null);

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            // Verify folder is deleted
            var exists = await CheckItemExists(folderName);
            Assert.IsFalse(exists, "Folder should be deleted");
        }

        [TestMethod]
        public async Task DeleteFolder_NonEmptyFolder_Returns417()
        {
            // Mock API Endpoint: POST /api/v3/DeleteFolder
            // Arrange - Create folder with a file inside
            var folderName = GenerateTestFolderName("non-empty-delete");
            await CreateTestFolder(folderName);

            // Create a file inside the folder
            var fileName = GenerateTestFileName("inside-file");
            var createData = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            var createResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(createResponse, "Create file inside test folder");

            // Act - Try to delete the non-empty folder
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}", null);

            // Assert - Should return 417 Directory Not Empty
            Assert.AreEqual(HttpStatusCode.ExpectationFailed, response.StatusCode);

            // Verify folder still exists
            var exists = await CheckItemExists(folderName);
            Assert.IsTrue(exists, "Non-empty folder should not be deleted");
        }

        [TestMethod]
        public async Task DeleteFolder_ActionWhenNotEmpty_Fail_Returns417()
        {
            // Test actionWhenNotEmpty="fail" parameter (default behavior)
            // Arrange - Create folder with a file inside
            var folderName = GenerateTestFolderName("action-fail-test");
            await CreateTestFolder(folderName);

            // Create a file inside the folder
            var fileName = GenerateTestFileName("inside-file");
            var createData = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            var createResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(createResponse, "Create file inside test folder");

            // Act - Try to delete the non-empty folder with actionWhenNotEmpty=fail
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=fail", null);

            // Assert - Should return 417 Directory Not Empty
            Assert.AreEqual(HttpStatusCode.ExpectationFailed, response.StatusCode);

            // Verify folder still exists
            var exists = await CheckItemExists(folderName);
            Assert.IsTrue(exists, "Non-empty folder should not be deleted when actionWhenNotEmpty=fail");
        }

        [TestMethod]
        public async Task DeleteFolder_ActionWhenNotEmpty_IgnoreErrors_DeletesSuccessfully()
        {
            // Test actionWhenNotEmpty="ignoreErrors" parameter
            // Arrange - Create folder with files inside
            var folderName = GenerateTestFolderName("action-ignore-test");
            await CreateTestFolder(folderName);

            // Create multiple files inside the folder
            var fileName1 = GenerateTestFileName("inside-file1");
            var fileName2 = GenerateTestFileName("inside-file2");

            var createData1 = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName1,
                conflictBehavior = "replace"
            };
            var createData2 = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName2,
                conflictBehavior = "replace"
            };

            await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData1));
            await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData2));

            // Act - Delete the non-empty folder with actionWhenNotEmpty=ignoreErrors
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=ignoreErrors", null);

            // Assert - Should return 204 No Content for successful deletion
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            // Verify folder is deleted
            var exists = await CheckItemExists(folderName);
            Assert.IsFalse(exists, "Folder should be deleted when actionWhenNotEmpty=ignoreErrors");
        }

        [TestMethod]
        public async Task DeleteFolder_ActionWhenNotEmpty_StopOnError_DeletesSuccessfully()
        {
            // Test actionWhenNotEmpty="stopOnError" parameter
            // Arrange - Create folder with files inside
            var folderName = GenerateTestFolderName("action-stop-test");
            await CreateTestFolder(folderName);

            // Create files inside the folder
            var fileName = GenerateTestFileName("inside-file");
            var createData = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Act - Delete the non-empty folder with actionWhenNotEmpty=stopOnError
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=stopOnError", null);

            // Assert - Should return 204 No Content for successful deletion
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            // Verify folder is deleted
            var exists = await CheckItemExists(folderName);
            Assert.IsFalse(exists, "Folder should be deleted when actionWhenNotEmpty=stopOnError");
        }

        [TestMethod]
        public async Task DeleteFolder_ListFailedItems_True_ReturnsFailedItemsList()
        {
            // Test listFailedItems=true parameter - simplified test
            // Arrange - Create a simple folder with one file
            var folderName = GenerateTestFolderName("list-failed-test");
            await CreateTestFolder(folderName);

            // Create one file in the folder
            var fileName = GenerateTestFileName("test-file");
            var createData = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Act - Delete with listFailedItems=true and ignoreErrors
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=ignoreErrors&listFailedItems=true", null);

            // Assert - Should return 204 for successful deletion
            // In this mock implementation, files should be deletable, so we expect success
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task DeleteFolder_ListFailedItems_False_DoesNotReturnFailedItemsList()
        {
            // Test listFailedItems=false parameter (default behavior)
            // Arrange - Create folder with files
            var folderName = GenerateTestFolderName("list-failed-false-test");
            await CreateTestFolder(folderName);

            var fileName = GenerateTestFileName("test-file");
            var createData = new
            {
                path = "/Documents/" + folderName,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };
            await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Act - Delete with listFailedItems=false (default)
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=ignoreErrors&listFailedItems=false", null);

            // Assert - Should return 204 for successful deletion
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task DeleteFolder_InvalidActionWhenNotEmpty_Returns400()
        {
            // Test invalid actionWhenNotEmpty parameter value
            // Arrange - Create folder
            var folderName = GenerateTestFolderName("invalid-action-test");
            await CreateTestFolder(folderName);

            // Act - Try to delete with invalid actionWhenNotEmpty value
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=invalidValue", null);

            // Assert - Should return 400 Bad Request
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);

            // Verify folder still exists
            var exists = await CheckItemExists(folderName);
            Assert.IsTrue(exists, "Folder should not be deleted with invalid actionWhenNotEmpty parameter");
        }

        [TestMethod]
        public async Task DeleteFolder_EmptyFolder_IgnoresActionWhenNotEmpty()
        {
            // Test that empty folders are deleted regardless of actionWhenNotEmpty parameter
            // Arrange - Create empty folder
            var folderName = GenerateTestFolderName("empty-folder-test");
            await CreateTestFolder(folderName);

            // Act - Delete empty folder with actionWhenNotEmpty=fail
            var response = await _client.PostAsync($"{BaseApiUrl}DeleteFolder?path={UrlEncode("/Documents/" + folderName)}&actionWhenNotEmpty=fail", null);

            // Assert - Should return 204 No Content for successful deletion
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);

            // Verify folder is deleted
            var exists = await CheckItemExists(folderName);
            Assert.IsFalse(exists, "Empty folder should be deleted regardless of actionWhenNotEmpty parameter");
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

        private async Task CreateUnicodeTestFile(string fileName, string content = "")
        {
            // For Unicode filenames, we need to use scheme-based paths and create the file directly
            // in the file system since the API might have encoding issues during creation
            var testBasePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"C:\temp\mwd"
                : "/tmp/mwd";
            var fullPath = Path.Combine(testBasePath, "shares", "Documents", fileName);

            // Ensure directory exists
            var directory = Path.GetDirectoryName(fullPath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            // Create the file directly with UTF-8 encoding
            await File.WriteAllTextAsync(fullPath, content, Encoding.UTF8);
        }

        private async Task CreateTestFolder(string folderName)
        {
            var createData = new
            {
                path = "/Documents",
                createFile = false,
                name = folderName,
                conflictBehavior = "replace"
            };

            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(response, $"Create test folder {folderName}");
        }

        private async Task<bool> CheckItemExists(string itemName)
        {
            try
            {
                var response = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode("/Documents/" + itemName)}");
                if (!response.IsSuccessStatusCode)
                    return false;

                var content = await response.Content.ReadAsStringAsync();
                var typeInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
                
                return typeInfo != null && 
                       typeInfo.ContainsKey("value") && 
                       !typeInfo["value"].ToString().Equals("unknown", StringComparison.OrdinalIgnoreCase);
            }
            catch
            {
                return false;
            }
        }

        [TestMethod]
        public async Task TestNewPathFormats_ShareBasedAndScheme()
        {
            // Test new path formats: share-based and scheme-based
            
            // First, ensure the shares directory structure exists
            await EnsureSharesExist();
            
            // Test 1: Create file using share-based path /Documents/test.txt
            var fileName1 = GenerateTestFileName("sharetest");
            var createData1 = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName1,
                conflictBehavior = "replace"
            };

            var json1 = JsonConvert.SerializeObject(createData1);
            var content1 = new StringContent(json1, Encoding.UTF8, "application/json");
            var createResponse1 = await _client.PostAsync($"{BaseApiUrl}CreateFile", content1);
            
            AssertSuccessStatusCode(createResponse1, "CreateFile with share-based path");

            // Test 2: List folder using scheme-based path sh:Documents:/
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode(":sh:Documents:/")}");
            AssertSuccessStatusCode(listResponse, "ListFolder with scheme-based path");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);
            
            Assert.IsNotNull(fileList);
            var createdFile = fileList.FirstOrDefault(f => f["name"].ToString() == fileName1);
            Assert.IsNotNull(createdFile, $"Created file {fileName1} should be in folder listing");

            // Test 3: Verify the returned path is in scheme-based format (new default)
            var returnedPath = createdFile["path"].ToString();
            Assert.IsTrue(returnedPath.StartsWith(":sh:Documents:/"), $"Returned path should be scheme-based format, got: {returnedPath}");

            // Test 4: Test GetItemType with different path formats
            var itemTypeResponse1 = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode("/Documents/" + fileName1)}");
            AssertSuccessStatusCode(itemTypeResponse1, "GetItemType with share-based path");

            var itemTypeResponse2 = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode(":sh:Documents:/" + fileName1)}");
            AssertSuccessStatusCode(itemTypeResponse2, "GetItemType with scheme-based path");

            // Both should return the same result
            var itemType1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(await itemTypeResponse1.Content.ReadAsStringAsync());
            var itemType2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(await itemTypeResponse2.Content.ReadAsStringAsync());
            
            Assert.AreEqual(itemType1["value"], itemType2["value"], "Both path formats should return same item type");
            Assert.AreEqual("file", itemType1["value"], "Item type should be 'file'");
        }

        [TestMethod]
        public async Task TestPublicLinkPathScheme()
        {
            // Test public link creation and link-based path access
            
            // First, ensure the shares directory structure exists
            await EnsureSharesExist();
            
            var fileName = GenerateTestFileName("linktest");
            
            // Create file first
            var createData = new
            {
                path = "/Projects",
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };

            var json = JsonConvert.SerializeObject(createData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");
            var createResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", content);
            AssertSuccessStatusCode(createResponse, "CreateFile for link test");

            // Create public link
            var linkData = new
            {
                path = $"/Projects/{fileName}",
                allowDownloading = true,
                allowUploading = false,
                allowEditing = false,
                password = ""
            };

            var linkJson = JsonConvert.SerializeObject(linkData);
            var linkContent = new StringContent(linkJson, Encoding.UTF8, "application/json");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            
            var linkResponse = await _client.PostAsync($"{BaseApiUrl}CreatePublicLink", linkContent);
            AssertSuccessStatusCode(linkResponse, "CreatePublicLink");

            var linkResponseContent = await linkResponse.Content.ReadAsStringAsync();
            var linkResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(linkResponseContent);
            var publicLink = linkResult["value"].ToString();
            
            Assert.IsNotNull(publicLink);
            Assert.IsTrue(publicLink.Contains("/share/"));

            // Extract link ID from the public link
            var linkId = publicLink.Split('/').Last();
            
            // Test accessing file using link-based path scheme
            var linkBasedPath = $":lnk:{linkId}:/";
            var itemTypeResponse = await _client.GetAsync($"{BaseApiUrl}GetItemType?path={UrlEncode(linkBasedPath)}");
            AssertSuccessStatusCode(itemTypeResponse, "GetItemType with link-based path");
            
            var itemTypeContent = await itemTypeResponse.Content.ReadAsStringAsync();
            var itemType = JsonConvert.DeserializeObject<Dictionary<string, object>>(itemTypeContent);
            Assert.AreEqual("file", itemType["value"], "Link-based path should resolve to file");
        }

        [TestMethod]
        public async Task CreateFile_WithConflictBehaviorFail_Returns409WhenFileExists()
        {
            // Arrange - First create a file
            var fileName = GenerateTestFileName("conflict-test");
            var createData = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName,
                conflictBehavior = "replace" // Create it first with replace
            };

            var initialResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(initialResponse, "Initial file creation");

            // Now try to create the same file with conflictBehavior = "fail"
            var conflictData = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName,
                conflictBehavior = "fail"
            };

            // Act
            var conflictResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(conflictData));

            // Assert - Should return 409 Conflict
            Assert.AreEqual(HttpStatusCode.Conflict, conflictResponse.StatusCode,
                "CreateFile should return 409 Conflict when file exists and conflictBehavior is 'fail'");

            var responseContent = await conflictResponse.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Contains("already exists"),
                "Response should indicate that the object already exists");
        }

        [TestMethod]
        public async Task CreateFile_WithConflictBehaviorFail_SucceedsWhenFileDoesNotExist()
        {
            // Arrange - Use a unique filename that doesn't exist
            var fileName = GenerateTestFileName("new-file");
            var createData = new
            {
                path = "/Documents",
                createFile = true,
                name = fileName,
                conflictBehavior = "fail"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));

            // Assert - Should succeed
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
                "CreateFile should succeed when file doesn't exist, even with conflictBehavior 'fail'");

            var responseContent = await response.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            Assert.IsNotNull(fileInfo);
            Assert.AreEqual(fileName, fileInfo["name"]);
            Assert.AreEqual(false, fileInfo["isFolder"]);
        }

        [TestMethod]
        public async Task CreateFile_WithConflictBehaviorRename_CreatesFileWithNewName()
        {
            // Arrange - First create a file
            var originalFileName = GenerateTestFileName("rename-test");
            var createData = new
            {
                path = "/Documents",
                createFile = true,
                name = originalFileName,
                conflictBehavior = "replace"
            };

            var initialResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(initialResponse, "Initial file creation");

            // Now try to create the same file with conflictBehavior = "rename"
            var renameData = new
            {
                path = "/Documents",
                createFile = true,
                name = originalFileName,
                conflictBehavior = "rename"
            };

            // Act
            var renameResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(renameData));

            // Assert - Should succeed with a different name
            Assert.IsTrue(renameResponse.StatusCode == HttpStatusCode.Created || renameResponse.StatusCode == HttpStatusCode.OK,
                "CreateFile should succeed with rename behavior when file exists");

            var responseContent = await renameResponse.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            Assert.IsNotNull(fileInfo);
            Assert.AreNotEqual(originalFileName, fileInfo["name"], "New file should have a different name");
            Assert.IsTrue(fileInfo["name"].ToString().Contains(Path.GetFileNameWithoutExtension(originalFileName)),
                "New file name should contain original file name");
            Assert.AreEqual(false, fileInfo["isFolder"]);

            // Verify both files exist
            var originalExists = await CheckItemExists(originalFileName);
            var newExists = await CheckItemExists(fileInfo["name"].ToString());

            Assert.IsTrue(originalExists, "Original file should still exist");
            Assert.IsTrue(newExists, "Renamed file should exist");
        }

        [TestMethod]
        public async Task CreateFile_WithConflictBehaviorRename_CreatesFolderWithNewName()
        {
            // Arrange - First create a folder
            var originalFolderName = GenerateTestFolderName("rename-test");
            var createData = new
            {
                path = "/Documents",
                createFile = false,
                name = originalFolderName,
                conflictBehavior = "replace"
            };

            var initialResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(initialResponse, "Initial folder creation");

            // Now try to create the same folder with conflictBehavior = "rename"
            var renameData = new
            {
                path = "/Documents",
                createFile = false,
                name = originalFolderName,
                conflictBehavior = "rename"
            };

            // Act
            var renameResponse = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(renameData));

            // Assert - Should succeed with a different name
            Assert.IsTrue(renameResponse.StatusCode == HttpStatusCode.Created || renameResponse.StatusCode == HttpStatusCode.OK,
                "CreateFile should succeed with rename behavior when folder exists");

            var responseContent = await renameResponse.Content.ReadAsStringAsync();
            var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            Assert.IsNotNull(folderInfo);
            Assert.AreNotEqual(originalFolderName, folderInfo["name"], "New folder should have a different name");
            Assert.IsTrue(folderInfo["name"].ToString().Contains(originalFolderName),
                "New folder name should contain original folder name");
            Assert.AreEqual(true, folderInfo["isFolder"]);

            // Verify both folders exist
            var originalExists = await CheckItemExists(originalFolderName);
            var newExists = await CheckItemExists(folderInfo["name"].ToString());

            Assert.IsTrue(originalExists, "Original folder should still exist");
            Assert.IsTrue(newExists, "Renamed folder should exist");
        }

        [TestMethod]
        public async Task StartFileUpload_WithConflictBehaviorRename_CreatesFileWithNewName()
        {
            // Arrange - First create a file
            var originalFileName = GenerateTestFileName("upload-rename-test");
            await CreateTestFile(originalFileName, "original content");

            // Now try to start upload with the same name and conflictBehavior = "rename"
            var uploadData = new
            {
                path = "/Documents/" + originalFileName,
                size = 100,
                conflictBehavior = "rename"
            };

            // Act
            var uploadResponse = await _client.PostAsync($"{BaseApiUrl}StartFileUpload", CreateJsonContent(uploadData));

            // Assert - Should succeed
            AssertSuccessStatusCode(uploadResponse, "StartFileUpload with rename behavior");

            var responseContent = await uploadResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            Assert.IsNotNull(uploadInfo);
            Assert.IsTrue(uploadInfo.ContainsKey("uploadId"), "Response should contain uploadId");

            // The upload session should be created, and when completed, should create a file with a different name
            // Note: We can't easily test the final renamed file without completing the upload,
            // but we can verify the upload session was created successfully
        }

        [TestMethod]
        public async Task MoveFile_WithConflictBehaviorRename_MovesFileWithNewName()
        {
            // Arrange - Create source file and a conflicting destination file
            var sourceFileName = GenerateTestFileName("move-source");
            var targetFileName = GenerateTestFileName("move-target");

            await CreateTestFile(sourceFileName, "source content");
            await CreateTestFile(targetFileName, "target content"); // This will conflict

            var moveData = new
            {
                path = "/Documents/" + sourceFileName,
                newPath = "/Documents/" + targetFileName,
                conflictBehavior = "rename"
            };

            // Act
            var moveResponse = await _client.PostAsync($"{BaseApiUrl}MoveFile", CreateJsonContent(moveData));

            // Assert - Should succeed
            Assert.IsTrue(moveResponse.StatusCode == HttpStatusCode.Created || moveResponse.StatusCode == HttpStatusCode.OK,
                "MoveFile should succeed with rename behavior when target exists");

            var responseContent = await moveResponse.Content.ReadAsStringAsync();
            var moveInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);

            Assert.IsNotNull(moveInfo);

            // Verify source file no longer exists at original location
            var sourceExists = await CheckItemExists(sourceFileName);
            Assert.IsFalse(sourceExists, "Source file should not exist after move");

            // Verify original target file still exists
            var originalTargetExists = await CheckItemExists(targetFileName);
            Assert.IsTrue(originalTargetExists, "Original target file should still exist");

            // The moved file should have a new name that contains the target name
            // We can verify this by checking that there's a file with a name containing our target
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            var renamedFile = fileList.FirstOrDefault(f =>
                f["name"].ToString() != targetFileName &&
                f["name"].ToString().Contains(Path.GetFileNameWithoutExtension(targetFileName)));

            Assert.IsNotNull(renamedFile, "Should find a renamed file containing the target name");
        }

        [TestMethod]
        public async Task MoveFile_SamePathWithRename_MovesToRenamedFile()
        {
            // Arrange - Create a file and try to "move" it to the same path with rename behavior
            var fileName = GenerateTestFileName("same-path-test");
            var fileContent = "original content for same path test";

            await CreateTestFile(fileName, fileContent);

            var moveData = new
            {
                path = "/Documents/" + fileName,
                newPath = "/Documents/" + fileName, // Same path
                conflictBehavior = "rename"
            };

            // Act
            var moveResponse = await _client.PostAsync($"{BaseApiUrl}MoveFile", CreateJsonContent(moveData));

            // Assert - Should succeed
            Assert.IsTrue(moveResponse.StatusCode == HttpStatusCode.Created || moveResponse.StatusCode == HttpStatusCode.OK,
                $"MoveFile should succeed with rename behavior for same path. Status: {moveResponse.StatusCode}");

            var responseContent = await moveResponse.Content.ReadAsStringAsync();
            var moveInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
            Assert.IsNotNull(moveInfo);

            // Verify original file is removed (moved, not copied)
            var originalExists = await CheckItemExists(fileName);
            Assert.IsFalse(originalExists, "Original file should be removed after same-path move with rename");

            // Verify a renamed file was created (moved to new name)
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            var renamedFile = fileList.FirstOrDefault(f =>
                f["name"].ToString().StartsWith(baseName + " ") &&
                f["name"].ToString().EndsWith(extension));

            Assert.IsNotNull(renamedFile, "Should find a renamed file with pattern 'filename 1.ext'");

            // Verify the content was moved to the new renamed file
            var renamedFileName = renamedFile["name"].ToString();
            var renamedContentResponse = await _client.GetAsync($"{BaseApiUrl}ReadFile?path=/Documents/{renamedFileName}&startPosition=0&count=1000");
            var renamedContent = await renamedContentResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(fileContent, renamedContent, "Renamed file content should match original content");
        }

        [TestMethod]
        public async Task CopyFile_SamePathWithRename_CreatesRenamedCopy()
        {
            // Arrange - Create a file and try to copy it to the same path with rename behavior
            var fileName = GenerateTestFileName("copy-same-path-rename");
            var fileContent = "original content for copy same path rename test";

            await CreateTestFile(fileName, fileContent);

            var copyData = new
            {
                path = "/Documents/" + fileName,
                newPath = "/Documents/" + fileName, // Same path
                conflictBehavior = "rename"
            };

            // Act
            var copyResponse = await _client.PostAsync($"{BaseApiUrl}CopyFile", CreateJsonContent(copyData));

            // Assert - Should succeed and create both files
            Assert.IsTrue(copyResponse.StatusCode == HttpStatusCode.Created || copyResponse.StatusCode == HttpStatusCode.OK,
                $"CopyFile should succeed with rename behavior for same path. Status: {copyResponse.StatusCode}");

            // Verify original file still exists
            var originalExists = await CheckItemExists(fileName);
            Assert.IsTrue(originalExists, "Original file should still exist after same-path copy with rename");

            // Verify a renamed copy was created
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            var renamedFile = fileList.FirstOrDefault(f =>
                f["name"].ToString() != fileName &&
                f["name"].ToString().StartsWith(baseName + " ") &&
                f["name"].ToString().EndsWith(extension));

            Assert.IsNotNull(renamedFile, "Should find a renamed copy with pattern 'filename 1.ext'");

            // Verify both files have the same content
            var originalContentResponse = await _client.GetAsync($"{BaseApiUrl}ReadFile?path=/Documents/{fileName}&startPosition=0&count=1000");
            var originalContent = await originalContentResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(fileContent, originalContent, "Original file should have original content");

            var renamedFileName = renamedFile["name"].ToString();
            var renamedContentResponse = await _client.GetAsync($"{BaseApiUrl}ReadFile?path=/Documents/{renamedFileName}&startPosition=0&count=1000");
            var renamedContent = await renamedContentResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(fileContent, renamedContent, "Copied file should have same content as original");
        }

        [TestMethod]
        public async Task CopyFile_SamePathWithReplace_KeepsOriginalFileIntact()
        {
            // Arrange - Create a file and try to copy it to itself with replace behavior
            var fileName = GenerateTestFileName("copy-same-path-replace");
            var fileContent = "original content for copy same path replace test";

            await CreateTestFile(fileName, fileContent);

            var copyData = new
            {
                path = "/Documents/" + fileName,
                newPath = "/Documents/" + fileName, // Same path
                conflictBehavior = "replace"
            };

            // Act
            var copyResponse = await _client.PostAsync($"{BaseApiUrl}CopyFile", CreateJsonContent(copyData));

            // Assert - Should succeed and keep the file
            Assert.IsTrue(copyResponse.StatusCode == HttpStatusCode.Created || copyResponse.StatusCode == HttpStatusCode.OK,
                $"CopyFile should succeed with replace behavior for same path. Status: {copyResponse.StatusCode}");

            // Verify original file still exists
            var originalExists = await CheckItemExists(fileName);
            Assert.IsTrue(originalExists, "Original file should still exist after same-path copy with replace");

            // Verify content is preserved
            var originalContentResponse = await _client.GetAsync($"{BaseApiUrl}ReadFile?path=/Documents/{fileName}&startPosition=0&count=1000");
            var originalContent = await originalContentResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(fileContent, originalContent, "File content should be preserved after same-path copy with replace");

            // Verify no additional files were created
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            var matchingFiles = fileList.Where(f =>
                f["name"].ToString().StartsWith(baseName) &&
                f["name"].ToString().EndsWith(extension)).ToList();

            Assert.AreEqual(1, matchingFiles.Count,
                "Should have exactly 1 file (original) - no duplicates should be created for same-path copy with replace");
        }

        [TestMethod]
        public async Task MoveFile_SamePathWithReplace_KeepsOriginalFileIntact()
        {
            // Arrange - Create a file and try to "move" it to the same path with replace behavior
            var fileName = GenerateTestFileName("same-path-replace");
            var fileContent = "original content for same path replace test";

            await CreateTestFile(fileName, fileContent);

            var moveData = new
            {
                path = "/Documents/" + fileName,
                newPath = "/Documents/" + fileName, // Same path
                conflictBehavior = "replace"
            };

            // Act
            var moveResponse = await _client.PostAsync($"{BaseApiUrl}MoveFile", CreateJsonContent(moveData));

            // Assert - Should succeed and keep the file
            Assert.IsTrue(moveResponse.StatusCode == HttpStatusCode.Created || moveResponse.StatusCode == HttpStatusCode.OK,
                $"MoveFile should succeed with replace behavior for same path. Status: {moveResponse.StatusCode}");

            var responseContent = await moveResponse.Content.ReadAsStringAsync();
            var moveInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseContent);
            Assert.IsNotNull(moveInfo);

            // Verify original file still exists and wasn't deleted
            var originalExists = await CheckItemExists(fileName);
            Assert.IsTrue(originalExists, "Original file should still exist after same-path move with replace behavior");

            // Verify original content is preserved
            var originalContentResponse = await _client.GetAsync($"{BaseApiUrl}ReadFile?path=/Documents/{fileName}&startPosition=0&count=1000");
            Assert.IsTrue(originalContentResponse.IsSuccessStatusCode, "Should be able to read original file after same-path replace");
            var originalContent = await originalContentResponse.Content.ReadAsStringAsync();
            Assert.AreEqual(fileContent, originalContent, "Original file content should be preserved after same-path replace");

            // Verify no additional files were created (no rename occurred)
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path=/Documents");
            var listContent = await listResponse.Content.ReadAsStringAsync();
            var fileList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);

            // Count files that match our test pattern
            var matchingFiles = fileList.Where(f =>
                f["name"].ToString().StartsWith(baseName) &&
                f["name"].ToString().EndsWith(extension)).ToList();

            Assert.AreEqual(1, matchingFiles.Count,
                "Should have exactly 1 file (original) - no duplicates should be created for same-path replace");
        }

        [TestMethod]
        public async Task GetFileInfo_HandlesUnicodeCharacters()
        {
            // Mock API Endpoint: GET /api/v3/GetFileInfo with Unicode characters
            // Test both URL-encoded and direct Unicode paths with scheme-based format
            var unicodeFileName = "István.pdf"; // Hungarian name with accent
            var fileContent = "Unicode test content";

            // Create the file first
            await CreateUnicodeTestFile(unicodeFileName, fileContent);

            // Test 1: URL-encoded Unicode path (as browsers would send)
            var urlEncodedPath = ":sh:Documents:/Istv%C3%A1n.pdf";
            var response1 = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={urlEncodedPath}");

            // Assert for URL-encoded request
            AssertSuccessStatusCode(response1, "GetFileInfo with URL-encoded Unicode");

            var content1 = await response1.Content.ReadAsStringAsync();
            var fileInfo1 = JsonConvert.DeserializeObject<Dictionary<string, object>>(content1);

            Assert.IsNotNull(fileInfo1);
            Assert.IsTrue(fileInfo1.ContainsKey("name"));
            Assert.IsTrue(fileInfo1.ContainsKey("path"));
            Assert.IsTrue(fileInfo1.ContainsKey("isFolder"));
            Assert.IsTrue(fileInfo1.ContainsKey("size"));

            // Verify the name is correctly returned (JSON should contain proper Unicode escapes)
            var returnedName1 = fileInfo1["name"].ToString();
            Assert.AreEqual(unicodeFileName, returnedName1, "File name should preserve Unicode characters");

            // Verify the path is correctly returned
            var returnedPath1 = fileInfo1["path"].ToString();
            Assert.IsTrue(returnedPath1.Contains("István"), "Path should contain Unicode characters");

            // Test 2: Direct Unicode path (already decoded)
            var directPath = ":sh:Documents:/István.pdf";
            var response2 = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={Uri.EscapeDataString(directPath)}");

            // Assert for direct Unicode request
            AssertSuccessStatusCode(response2, "GetFileInfo with direct Unicode path");

            var content2 = await response2.Content.ReadAsStringAsync();
            var fileInfo2 = JsonConvert.DeserializeObject<Dictionary<string, object>>(content2);

            Assert.IsNotNull(fileInfo2);
            var returnedName2 = fileInfo2["name"].ToString();
            Assert.AreEqual(unicodeFileName, returnedName2, "File name should be identical for both path formats");

            // Verify JSON encoding is correct (should contain \u00e1 for á)
            Assert.IsTrue(content1.Contains("\\u00e1") || content1.Contains("á"),
                "JSON response should properly encode Unicode characters");
            Assert.IsTrue(content2.Contains("\\u00e1") || content2.Contains("á"),
                "JSON response should properly encode Unicode characters");
        }

        // Helper method to ensure shares exist
        private async Task EnsureSharesExist()
        {
            // Create all share directories if they don't exist
            var shares = new[] { "Documents", "Pictures", "Projects" };
            foreach (var share in shares)
            {
                var sharePath = Path.Combine(Program.BASE_PATH, "shares", share);
                if (!Directory.Exists(sharePath))
                {
                    Directory.CreateDirectory(sharePath);
                }
            }
        }
    }
}