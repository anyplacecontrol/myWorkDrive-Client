using Microsoft.VisualStudio.TestTools.UnitTesting;
using MWDMockServer;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class CopyFolderDefensiveTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task CopyFolder_EmptyFolder_SucceedsWithDefensiveChecks()
        {
            // Test for CopyFolder issue #4 fix - copying empty folder should work
            // This tests the defensive FileSystemInfo creation in HandleCopyFileCommonAsync

            // Arrange - Create an empty test folder
            var sourceFolderName = GenerateTestFolderName("empty-source");
            await CreateTestFolder(sourceFolderName);

            var targetFolderName = GenerateTestFolderName("empty-target");

            var copyData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
                $"CopyFolder should succeed for empty folder. Status: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()}");

            var content = await response.Content.ReadAsStringAsync();
            var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            Assert.IsNotNull(folderInfo);
            Assert.IsTrue(folderInfo.ContainsKey("name"));
            Assert.IsTrue(folderInfo.ContainsKey("isFolder"));
            Assert.AreEqual(true, folderInfo["isFolder"]);

            // Verify both source and target folders exist
            var sourceExists = await CheckItemExists(sourceFolderName);
            var targetExists = await CheckItemExists(targetFolderName);

            Assert.IsTrue(sourceExists, "Source folder should still exist after copy");
            Assert.IsTrue(targetExists, "Target folder should exist after copy");
        }

        [TestMethod]
        public async Task CopyFolder_FolderWithFiles_SucceedsWithDefensiveChecks()
        {
            // Test copying a folder containing files - ensures FileSystemInfoToDictionary handles both files and folders

            // Arrange - Create folder with files inside
            var sourceFolderName = GenerateTestFolderName("source-with-files");
            await CreateTestFolder(sourceFolderName);

            // Create files inside the source folder
            var file1Name = GenerateTestFileName("file1");
            var file2Name = GenerateTestFileName("file2");

            await CreateTestFileInFolder(sourceFolderName, file1Name, "content of file 1");
            await CreateTestFileInFolder(sourceFolderName, file2Name, "content of file 2");

            var targetFolderName = GenerateTestFolderName("target-with-files");

            var copyData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
                $"CopyFolder should succeed for folder with files. Status: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()}");

            // Verify target folder exists and contains copied files
            var targetExists = await CheckItemExists(targetFolderName);
            Assert.IsTrue(targetExists, "Target folder should exist after copy");

            // List target folder contents to verify files were copied
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode("/Documents/" + targetFolderName)}");
            AssertSuccessStatusCode(listResponse, "List target folder contents");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            Assert.IsNotNull(items);
            Assert.AreEqual(2, items.Count, "Target folder should contain 2 copied files");

            var fileNames = items.Select(item => item["name"].ToString()).ToList();
            Assert.IsTrue(fileNames.Contains(file1Name), "Target folder should contain file1");
            Assert.IsTrue(fileNames.Contains(file2Name), "Target folder should contain file2");

            // Verify all items have proper size information (tests FileSystemInfoToDictionary defensive fixes)
            foreach (var item in items)
            {
                Assert.IsTrue(item.ContainsKey("size"), "Each item should have size information");
                // Note: isFolder is only included for folders, not files (due to writeDefaultValues=false in ListFolder)
                // Files don't have isFolder property in the response - this is expected behavior
                Assert.IsTrue((long)item["size"] >= 0, "File size should be non-negative");
            }
        }

        [TestMethod]
        public async Task CopyFolder_NestedFolders_SucceedsWithDefensiveChecks()
        {
            // Test copying nested folder structure - comprehensive test for recursive copy operations

            // Arrange - Create nested folder structure
            var sourceFolderName = GenerateTestFolderName("nested-source");
            await CreateTestFolder(sourceFolderName);

            // Create subfolder
            var subfolderName = GenerateTestFolderName("subfolder");
            await CreateTestFolderInFolder(sourceFolderName, subfolderName);

            // Create files in both main folder and subfolder
            var mainFile = GenerateTestFileName("main");
            var subFile = GenerateTestFileName("sub");

            await CreateTestFileInFolder(sourceFolderName, mainFile, "main folder content");
            await CreateTestFileInFolder($"{sourceFolderName}/{subfolderName}", subFile, "subfolder content");

            var targetFolderName = GenerateTestFolderName("nested-target");

            var copyData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
                $"CopyFolder should succeed for nested folders. Status: {response.StatusCode}, Content: {await response.Content.ReadAsStringAsync()}");

            // Verify target folder structure
            var targetExists = await CheckItemExists(targetFolderName);
            Assert.IsTrue(targetExists, "Target folder should exist after copy");

            // Check main folder contents
            var mainListResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode("/Documents/" + targetFolderName)}");
            AssertSuccessStatusCode(mainListResponse, "List target main folder");

            var mainItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                await mainListResponse.Content.ReadAsStringAsync());

            Assert.IsNotNull(mainItems);
            Assert.AreEqual(2, mainItems.Count, "Target main folder should contain 1 file + 1 subfolder");

            // Check that we have both file and folder
            // Files don't have isFolder property (due to writeDefaultValues=false), folders do have it and it's true
            var hasFile = mainItems.Any(item => item["name"].ToString() == mainFile && !item.ContainsKey("isFolder"));
            var hasFolder = mainItems.Any(item => item["name"].ToString() == subfolderName &&
                                                 item.ContainsKey("isFolder") && (bool)item["isFolder"] == true);

            Assert.IsTrue(hasFile, "Target should contain the main file");
            Assert.IsTrue(hasFolder, "Target should contain the subfolder");

            // Check subfolder contents
            var subListResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode("/Documents/" + targetFolderName + "/" + subfolderName)}");
            AssertSuccessStatusCode(subListResponse, "List target subfolder");

            var subItems = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                await subListResponse.Content.ReadAsStringAsync());

            Assert.IsNotNull(subItems);
            Assert.AreEqual(1, subItems.Count, "Target subfolder should contain 1 file");
            Assert.AreEqual(subFile, subItems[0]["name"].ToString(), "Subfolder should contain the sub file");
            // Files don't have isFolder property in ListFolder response, so we just verify it's not present
            Assert.IsFalse(subItems[0].ContainsKey("isFolder"), "Files should not have isFolder property in ListFolder response");
        }

        [TestMethod]
        public async Task CopyFolder_ToExistingFolder_WithRename_SucceedsWithDefensiveChecks()
        {
            // Test conflict resolution with rename behavior

            // Arrange - Create source folder and conflicting target
            var sourceFolderName = GenerateTestFolderName("copy-source");
            var targetFolderName = GenerateTestFolderName("copy-target");

            await CreateTestFolder(sourceFolderName);
            await CreateTestFolder(targetFolderName); // Create conflicting target

            // Add content to source to differentiate
            var sourceFile = GenerateTestFileName("source-content");
            await CreateTestFileInFolder(sourceFolderName, sourceFile, "source content");

            var copyData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "rename"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            Assert.IsTrue(response.StatusCode == HttpStatusCode.Created || response.StatusCode == HttpStatusCode.OK,
                $"CopyFolder should succeed with rename behavior. Status: {response.StatusCode}");

            var content = await response.Content.ReadAsStringAsync();
            var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            Assert.IsNotNull(folderInfo);
            Assert.AreNotEqual(targetFolderName, folderInfo["name"].ToString(),
                "Renamed folder should have different name");

            // Verify original target still exists
            var originalTargetExists = await CheckItemExists(targetFolderName);
            Assert.IsTrue(originalTargetExists, "Original target folder should still exist");

            // Verify renamed copy exists and contains source content
            var renamedFolderName = folderInfo["name"].ToString();
            var renamedExists = await CheckItemExists(renamedFolderName);
            Assert.IsTrue(renamedExists, "Renamed copy folder should exist");

            // Check that renamed folder contains the source file
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode("/Documents/" + renamedFolderName)}");
            AssertSuccessStatusCode(listResponse, "List renamed folder contents");

            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(
                await listResponse.Content.ReadAsStringAsync());

            Assert.IsNotNull(items);
            Assert.AreEqual(1, items.Count, "Renamed folder should contain source file");
            Assert.AreEqual(sourceFile, items[0]["name"].ToString(), "Renamed folder should contain the source file");
        }

        [TestMethod]
        public async Task CopyFolder_InvalidSource_ReturnsError()
        {
            // Test defensive behavior when source doesn't exist

            // Arrange - Use non-existent source folder
            var nonExistentSource = GenerateTestFolderName("non-existent");
            var targetFolderName = GenerateTestFolderName("target");

            var copyData = new
            {
                path = "/Documents/" + nonExistentSource,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert - Should return error for non-existent source
            Assert.IsFalse(response.IsSuccessStatusCode,
                "CopyFolder should fail when source doesn't exist");

            // Target should not be created
            var targetExists = await CheckItemExists(targetFolderName);
            Assert.IsFalse(targetExists, "Target folder should not be created when source doesn't exist");
        }

        [TestMethod]
        public async Task CopyFolder_RecursiveDestination_Returns400WithInfiniteRecursionError()
        {
            // Test that copying a folder into itself returns consistent 400 error with recursion message

            // Arrange - Create source folder
            var sourceFolderName = GenerateTestFolderName("recursive-source");
            await CreateTestFolder(sourceFolderName);

            var copyData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + sourceFolderName + "/inside",
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
                "CopyFolder should return 400 (BadRequest) for recursive destination");

            var content = await response.Content.ReadAsStringAsync();
            var errorInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            Assert.IsNotNull(errorInfo);
            Assert.IsTrue(errorInfo.ContainsKey("errorCode"));
            Assert.AreEqual(40004, Convert.ToInt32(errorInfo["errorCode"]),
                "Should return error code 40004 for infinite recursion");

            Assert.IsTrue(errorInfo.ContainsKey("message"));
            var message = errorInfo["message"].ToString();
            Assert.IsTrue(message.Contains("destination is inside the source directory"),
                "Error message should mention destination inside source");
            Assert.IsTrue(message.Contains("infinite recursion"),
                "Error message should mention infinite recursion");
            Assert.IsTrue(message.Contains(":sh:Documents:"),
                "Error message should use logical path format (:sh:Documents:) instead of physical paths");
        }

        [TestMethod]
        public async Task MoveFolder_RecursiveDestination_Returns400WithInfiniteRecursionError()
        {
            // Test that moving a folder into itself returns consistent 400 error with recursion message

            // Arrange - Create source folder
            var sourceFolderName = GenerateTestFolderName("recursive-move-source");
            await CreateTestFolder(sourceFolderName);

            var moveData = new
            {
                path = "/Documents/" + sourceFolderName,
                newPath = "/Documents/" + sourceFolderName + "/inside",
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}MoveFolder", CreateJsonContent(moveData));

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode,
                "MoveFolder should return 400 (BadRequest) for recursive destination");

            var content = await response.Content.ReadAsStringAsync();
            var errorInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);

            Assert.IsNotNull(errorInfo);
            Assert.IsTrue(errorInfo.ContainsKey("errorCode"));
            Assert.AreEqual(40004, Convert.ToInt32(errorInfo["errorCode"]),
                "Should return error code 40004 for infinite recursion");

            Assert.IsTrue(errorInfo.ContainsKey("message"));
            var message = errorInfo["message"].ToString();
            Assert.IsTrue(message.Contains("destination is inside the source directory"),
                "Error message should mention destination inside source");
            Assert.IsTrue(message.Contains("infinite recursion"),
                "Error message should mention infinite recursion");
            Assert.IsTrue(message.Contains(":sh:Documents:"),
                "Error message should use logical path format (:sh:Documents:) instead of physical paths");
        }

        [TestMethod]
        public async Task CopyFolder_FileSystemInfoToDictionary_HandlesEdgeCases()
        {
            // Test the FileSystemInfoToDictionary defensive fixes specifically
            // This test ensures the method handles various file system objects correctly

            // Arrange - Create folder with different types of content
            var folderName = GenerateTestFolderName("edge-case-test");
            await CreateTestFolder(folderName);

            // Create files with different characteristics
            var emptyFile = GenerateTestFileName("empty");
            var contentFile = GenerateTestFileName("with-content");
            var subFolder = GenerateTestFolderName("sub");

            await CreateTestFileInFolder(folderName, emptyFile, "");  // Empty file (size = 0)
            await CreateTestFileInFolder(folderName, contentFile, "Some content for size test");
            await CreateTestFolderInFolder(folderName, subFolder);

            var targetFolderName = GenerateTestFolderName("edge-case-target");

            var copyData = new
            {
                path = "/Documents/" + folderName,
                newPath = "/Documents/" + targetFolderName,
                conflictBehavior = "replace"
            };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}CopyFolder", CreateJsonContent(copyData));

            // Assert
            AssertSuccessStatusCode(response, "CopyFolder with edge case content");

            // Verify target folder listing includes proper metadata for all item types
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListFolder?path={UrlEncode("/Documents/" + targetFolderName)}");
            AssertSuccessStatusCode(listResponse, "List target folder with edge cases");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);

            Assert.IsNotNull(items);
            Assert.AreEqual(3, items.Count, "Should have 2 files + 1 subfolder");

            // Verify each item has proper metadata
            foreach (var item in items)
            {
                Assert.IsTrue(item.ContainsKey("name"), "Item should have name");
                Assert.IsTrue(item.ContainsKey("path"), "Item should have path");

                // Size property is only included if size > 0 or writeDefaultValues=true (due to API design)
                // Empty files don't have size property in ListFolder response
                if (item.ContainsKey("size"))
                {
                    var size = Convert.ToInt64(item["size"]);
                    Assert.IsTrue(size >= 0, $"Size should be non-negative for {item["name"]}");
                }

                // Only folders have isFolder property set to true, files don't have this property
                if (item.ContainsKey("isFolder") && (bool)item["isFolder"])
                {
                    // Folders may or may not have size property depending on if they're empty
                    // but if they do, it should be 0
                    if (item.ContainsKey("size"))
                    {
                        var size = Convert.ToInt64(item["size"]);
                        Assert.AreEqual(0L, size, $"Folder {item["name"]} should have size 0");
                    }
                }
            }

            // Find and verify specific items
            var emptyFileItem = items.FirstOrDefault(i => i["name"].ToString() == emptyFile);
            var contentFileItem = items.FirstOrDefault(i => i["name"].ToString() == contentFile);
            var subFolderItem = items.FirstOrDefault(i => i["name"].ToString() == subFolder);

            Assert.IsNotNull(emptyFileItem, "Empty file should be copied");
            Assert.IsNotNull(contentFileItem, "Content file should be copied");
            Assert.IsNotNull(subFolderItem, "Subfolder should be copied");

            // Files don't have isFolder property in ListFolder, folders do and it's true
            Assert.IsFalse(emptyFileItem.ContainsKey("isFolder"), "Empty file should not have isFolder property");
            Assert.IsFalse(contentFileItem.ContainsKey("isFolder"), "Content file should not have isFolder property");
            Assert.IsTrue(subFolderItem.ContainsKey("isFolder") && (bool)subFolderItem["isFolder"], "Subfolder should have isFolder=true");

            // Content file should have size > 0, empty file may not have size property (due to size=0)
            // Empty files don't have size property in ListFolder response
            Assert.IsFalse(emptyFileItem.ContainsKey("size"), "Empty file should not have size property (size=0)");
            Assert.IsTrue(contentFileItem.ContainsKey("size") && Convert.ToInt64(contentFileItem["size"]) > 0, "Content file should have size > 0");
        }

        // Helper methods specific to CopyFolder tests

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

        private async Task CreateTestFolderInFolder(string parentFolder, string folderName)
        {
            var createData = new
            {
                path = "/Documents/" + parentFolder,
                createFile = false,
                name = folderName,
                conflictBehavior = "replace"
            };

            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(response, $"Create test folder {folderName} in {parentFolder}");
        }

        private async Task CreateTestFileInFolder(string folderPath, string fileName, string content = "")
        {
            var createData = new
            {
                path = "/Documents/" + folderPath,
                createFile = true,
                name = fileName,
                conflictBehavior = "replace"
            };

            var response = await _client.PostAsync($"{BaseApiUrl}CreateFile", CreateJsonContent(createData));
            AssertSuccessStatusCode(response, $"Create test file {fileName} in {folderPath}");

            if (!string.IsNullOrEmpty(content))
            {
                var contentBytes = Encoding.UTF8.GetBytes(content);
                var writeResponse = await _client.PostAsync(
                    $"{BaseApiUrl}WriteFile?path={UrlEncode("/Documents/" + folderPath + "/" + fileName)}&startPosition=0&unlockAfterWrite=true",
                    new ByteArrayContent(contentBytes));
                AssertSuccessStatusCode(writeResponse, $"Write content to {fileName} in {folderPath}");
            }
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
    }
}