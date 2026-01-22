using Microsoft.VisualStudio.TestTools.UnitTesting;
using APIServer;
using WanPath.Common.Helpers;
using Newtonsoft.Json;
using System.IO.Compression;
using System.Net;
using System.Text;
using MWDMockServer;

namespace MockServerAPITests
{
    [TestClass]
    public class ZipFilesTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task ZipFiles_GET_WithValidFolder_ReturnsZipReference()
        {
            // Arrange - Create test files
            var testFolder = "/Documents/test-zip";
            var fullFolderPath = Path.Combine(MWDMockServer.Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "test-zip");
            Directory.CreateDirectory(fullFolderPath);

            var testFile1 = Path.Combine(fullFolderPath, "file1.txt");
            var testFile2 = Path.Combine(fullFolderPath, "file2.txt");
            await File.WriteAllTextAsync(testFile1, "Content of file 1");
            await File.WriteAllTextAsync(testFile2, "Content of file 2");

            // Act - GET request to zip files
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=ref");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles GET");

            var zipReference = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(zipReference.StartsWith("zip-"));
            Assert.IsTrue(zipReference.Length > 4);
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithValidFiles_ReturnsZipReference()
        {
            // Arrange - Create test files
            var testFolder = "/Documents/test-zip-post";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "test-zip-post");
            Directory.CreateDirectory(fullFolderPath);

            var testFile1Path = "/Documents/test-zip-post/file1.txt";
            var testFile2Path = "/Documents/test-zip-post/file2.txt";
            var fullFile1Path = Path.Combine(fullFolderPath, "file1.txt");
            var fullFile2Path = Path.Combine(fullFolderPath, "file2.txt");

            await File.WriteAllTextAsync(fullFile1Path, "Content of file 1");
            await File.WriteAllTextAsync(fullFile2Path, "Content of file 2");

            var requestData = new { paths = new[] { testFile1Path, testFile2Path } };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=ref", CreateJsonContent(requestData));

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles POST");

            var zipReference = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(zipReference.StartsWith("zip-"));
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithDataResponse_ReturnsBinaryZip()
        {
            // Arrange
            var testFolder = "/Documents/test-zip-binary";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "test-zip-binary");
            Directory.CreateDirectory(fullFolderPath);

            var testFile = Path.Combine(fullFolderPath, "test.txt");
            await File.WriteAllTextAsync(testFile, "Test content for ZIP");

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=data");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles GET with data response");
            Assert.AreEqual("application/octet-stream", response.Content.Headers.ContentType?.MediaType);
            Assert.IsTrue(response.Content.Headers.ContentDisposition?.FileName?.Contains(".zip") ?? false);

            // Verify it's a valid ZIP file
            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(zipBytes.Length > 0);

            // Try to extract and verify content
            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.IsTrue(archive.Entries.Count > 0);
                var entry = archive.Entries.First(e => e.Name == "test.txt");
                using (var entryStream = entry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Test content for ZIP", content);
                }
            }
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithNonExistentPath_Returns404()
        {
            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path=/NonExistent/Folder&respondWith=ref");

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithNoFiles_Returns204()
        {
            // Arrange - Create empty folder
            var testFolder = "/Documents/empty-folder";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "empty-folder");
            Directory.CreateDirectory(fullFolderPath);

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=ref");

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, response.StatusCode);
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithSomeFilesMissing_Returns206()
        {
            // Arrange - Create only one of two requested files
            var testFolder = "/Documents/partial-files";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "partial-files");
            Directory.CreateDirectory(fullFolderPath);

            var existingFilePath = "/Documents/partial-files/existing.txt";
            var missingFilePath = "/Documents/partial-files/missing.txt";
            var fullExistingPath = Path.Combine(fullFolderPath, "existing.txt");

            await File.WriteAllTextAsync(fullExistingPath, "This file exists");
            // Don't create the missing file

            var requestData = new { paths = new[] { existingFilePath, missingFilePath } };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=data", CreateJsonContent(requestData));

            // Assert
            Assert.AreEqual((HttpStatusCode)206, response.StatusCode); // 206 Partial Content

            // Verify ZIP contains only the existing file
            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.AreEqual(1, archive.Entries.Count);
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "existing.txt"));
            }
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithPathResponse_SavesZipFile()
        {
            // Arrange
            var testFolder = "/Documents/path-test";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "path-test");
            Directory.CreateDirectory(fullFolderPath);

            var testFile = Path.Combine(fullFolderPath, "path-test.txt");
            await File.WriteAllTextAsync(testFile, "Path test content");

            var zipName = "test-output.zip";

            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=path&zipName={zipName}");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles with path response");

            var returnedPath = await response.Content.ReadAsStringAsync();
            Assert.AreEqual(zipName, returnedPath);

            // Verify file was created
            var fullZipPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", zipName);
            Assert.IsTrue(File.Exists(fullZipPath));

            // Cleanup
            File.Delete(fullZipPath);
        }

        [TestMethod]
        public async Task ZipFiles_WithConflictBehaviorFail_Returns409WhenFileExists()
        {
            // Arrange
            var testFolder = "/Documents/conflict-test";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "conflict-test");
            Directory.CreateDirectory(fullFolderPath);

            var testFile = Path.Combine(fullFolderPath, "conflict.txt");
            await File.WriteAllTextAsync(testFile, "Conflict test");

            var zipName = "conflict-test.zip";
            var fullZipPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", zipName);

            // Pre-create the ZIP file
            await File.WriteAllTextAsync(fullZipPath, "Existing file");

            try
            {
                // Act
                var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=path&zipName={zipName}&conflictBehavior=fail");

                // Assert
                Assert.AreEqual(HttpStatusCode.Conflict, response.StatusCode);
            }
            finally
            {
                // Cleanup
                if (File.Exists(fullZipPath))
                    File.Delete(fullZipPath);
            }
        }

        [TestMethod]
        public async Task ZipFiles_WithConflictBehaviorRename_GeneratesUniqueFileName()
        {
            // Arrange
            var testFolder = "/Documents/rename-test";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "rename-test");
            Directory.CreateDirectory(fullFolderPath);

            var testFile = Path.Combine(fullFolderPath, "rename.txt");
            await File.WriteAllTextAsync(testFile, "Rename test");

            var zipName = "rename-test.zip";
            var fullZipPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", zipName);

            // Pre-create the ZIP file
            await File.WriteAllTextAsync(fullZipPath, "Existing file");

            try
            {
                // Act
                var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&respondWith=path&zipName={zipName}&conflictBehavior=rename");

                // Assert
                AssertSuccessStatusCode(response, "ZipFiles with rename conflict behavior");

                var returnedPath = await response.Content.ReadAsStringAsync();
                Assert.AreNotEqual(zipName, returnedPath); // Should be renamed
                Assert.IsTrue(returnedPath.Contains("rename-test") && returnedPath.EndsWith(".zip"));

                // Verify new file was created
                var newFullZipPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", returnedPath);
                Assert.IsTrue(File.Exists(newFullZipPath));

                // Cleanup new file
                if (File.Exists(newFullZipPath))
                    File.Delete(newFullZipPath);
            }
            finally
            {
                // Cleanup original file
                if (File.Exists(fullZipPath))
                    File.Delete(fullZipPath);
            }
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithEmptyPathsArray_Returns400()
        {
            // Arrange
            var requestData = new { paths = new string[0] };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=ref", CreateJsonContent(requestData));

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithMaskFilter_FiltersCorrectly()
        {
            // Arrange
            var testFolder = "/Documents/mask-test";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "mask-test");
            Directory.CreateDirectory(fullFolderPath);

            // Create files with different extensions
            await File.WriteAllTextAsync(Path.Combine(fullFolderPath, "doc1.txt"), "Text file 1");
            await File.WriteAllTextAsync(Path.Combine(fullFolderPath, "doc2.txt"), "Text file 2");
            await File.WriteAllTextAsync(Path.Combine(fullFolderPath, "image.jpg"), "Image file");
            await File.WriteAllTextAsync(Path.Combine(fullFolderPath, "data.json"), "JSON data");

            // Act - Only ZIP .txt files
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(testFolder)}&mask=*.txt&respondWith=data");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles with mask filter");

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.AreEqual(2, archive.Entries.Count);
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "doc1.txt"));
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "doc2.txt"));
                Assert.IsFalse(archive.Entries.Any(e => e.Name == "image.jpg"));
                Assert.IsFalse(archive.Entries.Any(e => e.Name == "data.json"));
            }
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithSchemeBasedPath_WorksCorrectly()
        {
            // Arrange - Create files directly in Documents share using the physical path
            var documentsPhysicalPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents");
            Directory.CreateDirectory(documentsPhysicalPath);

            // Create test files
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "scheme-test1.txt"), "Scheme test file 1");
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "scheme-test2.txt"), "Scheme test file 2");
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "scheme-test.log"), "Log file should not be included");

            // Act - Use scheme-based path format (:sh:Documents:/) with mask filter
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(":sh:Documents:/")}&mask=*.txt&respondWith=data");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles with scheme-based path");
            Assert.AreEqual("application/octet-stream", response.Content.Headers.ContentType?.MediaType);

            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(zipBytes.Length > 0, "ZIP file should not be empty");

            // Verify ZIP contents
            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Should contain only the .txt files, not the .log file
                Assert.IsTrue(archive.Entries.Count >= 2, "ZIP should contain at least 2 files");
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "scheme-test1.txt"), "Should contain scheme-test1.txt");
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "scheme-test2.txt"), "Should contain scheme-test2.txt");
                Assert.IsFalse(archive.Entries.Any(e => e.Name == "scheme-test.log"), "Should NOT contain .log file due to mask filter");
            }
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithSchemeBasedPath_ReturnsReference()
        {
            // Arrange - Create files directly in Documents share
            var documentsPhysicalPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents");
            Directory.CreateDirectory(documentsPhysicalPath);

            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "ref-test1.txt"), "Reference test file 1");
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "ref-test2.txt"), "Reference test file 2");

            // Act - Use scheme-based path format with default respondWith=ref
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?path={Uri.EscapeDataString(":sh:Documents:/")}&mask=ref-*.txt");

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles with scheme-based path returning reference");
            Assert.AreEqual("text/plain", response.Content.Headers.ContentType?.MediaType);

            var zipReference = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(zipReference.StartsWith("zip-"), "Response should be a zip reference starting with 'zip-'");
            Assert.IsTrue(zipReference.Length > 10, "Zip reference should be a meaningful string");
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithSchemeBasedPaths_ReturnsZipReference()
        {
            // Arrange - Create test files using the exact paths from the user's issue
            var documentsPhysicalPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents");
            Directory.CreateDirectory(documentsPhysicalPath);

            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "111.txt"), "Content of 111.txt");
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "222.txt"), "Content of 222.txt");

            // Use the exact request body format from the user's issue
            var requestData = new { paths = new[] { ":sh:Documents:/111.txt", ":sh:Documents:/222.txt" } };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=ref", CreateJsonContent(requestData));

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles POST with scheme-based paths");

            var zipReference = await response.Content.ReadAsStringAsync();
            Assert.IsTrue(zipReference.StartsWith("zip-"), "Response should be a zip reference starting with 'zip-'");
            Assert.IsTrue(zipReference.Length > 4, "Zip reference should be a meaningful string");
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithSchemeBasedPaths_ReturnsZipData()
        {
            // Arrange - Create test files using scheme-based paths
            var documentsPhysicalPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents");
            Directory.CreateDirectory(documentsPhysicalPath);

            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "data1.txt"), "Content of data1.txt");
            await File.WriteAllTextAsync(Path.Combine(documentsPhysicalPath, "data2.txt"), "Content of data2.txt");

            var requestData = new { paths = new[] { ":sh:Documents:/data1.txt", ":sh:Documents:/data2.txt" } };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=data", CreateJsonContent(requestData));

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles POST with scheme-based paths returning data");
            Assert.AreEqual("application/octet-stream", response.Content.Headers.ContentType?.MediaType);

            // Verify it's a valid ZIP file with both files
            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(zipBytes.Length > 0);

            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.AreEqual(2, archive.Entries.Count);
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "data1.txt"));
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "data2.txt"));

                // Verify file contents
                var entry1 = archive.Entries.First(e => e.Name == "data1.txt");
                using (var entryStream = entry1.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Content of data1.txt", content);
                }

                var entry2 = archive.Entries.First(e => e.Name == "data2.txt");
                using (var entryStream = entry2.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Content of data2.txt", content);
                }
            }
        }

        [TestMethod]
        public async Task ZipFiles_WorkflowWithReference_CreatesAndRetrievesZip()
        {
            // Arrange - Create test files
            var testFolder = "/Documents/ref-workflow";
            var fullFolderPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents", "ref-workflow");
            Directory.CreateDirectory(fullFolderPath);

            var testFile1 = Path.Combine(fullFolderPath, "ref1.txt");
            var testFile2 = Path.Combine(fullFolderPath, "ref2.txt");
            await File.WriteAllTextAsync(testFile1, "Reference workflow file 1");
            await File.WriteAllTextAsync(testFile2, "Reference workflow file 2");

            // Step 1: POST to create ZIP and get reference
            var response1 = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=ref",
                CreateJsonContent(new { paths = new[] { "/Documents/ref-workflow/ref1.txt", "/Documents/ref-workflow/ref2.txt" } }));

            AssertSuccessStatusCode(response1, "ZipFiles POST to create reference");
            var zipReference = await response1.Content.ReadAsStringAsync();
            Assert.IsTrue(zipReference.StartsWith("zip-"));

            // Step 2: GET with ref parameter to retrieve ZIP data
            var response2 = await _client.GetAsync($"{BaseApiUrl}ZipFiles?ref={Uri.EscapeDataString(zipReference)}");

            // Assert
            AssertSuccessStatusCode(response2, "ZipFiles GET with ref parameter");
            Assert.AreEqual("application/octet-stream", response2.Content.Headers.ContentType?.MediaType);
            Assert.IsTrue(response2.Content.Headers.ContentDisposition?.FileName?.Contains(".zip") ?? false);

            // Verify it's a valid ZIP file with expected content
            var zipBytes = await response2.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(zipBytes.Length > 0);

            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                Assert.AreEqual(2, archive.Entries.Count);
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "ref1.txt"));
                Assert.IsTrue(archive.Entries.Any(e => e.Name == "ref2.txt"));

                // Verify content of one file
                var entry1 = archive.Entries.First(e => e.Name == "ref1.txt");
                using (var entryStream = entry1.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Reference workflow file 1", content);
                }
            }
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithInvalidReference_Returns404()
        {
            // Act - Try to get ZIP with invalid reference
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles?ref=invalid-reference");

            // Assert
            Assert.AreEqual(HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task ZipFiles_GET_WithoutPathOrRef_ReturnsBadRequest()
        {
            // Act - Try to get ZIP without path or ref parameter
            var response = await _client.GetAsync($"{BaseApiUrl}ZipFiles");

            // Assert
            Assert.AreEqual(HttpStatusCode.BadRequest, response.StatusCode);
        }

        [TestMethod]
        public async Task ZipFiles_POST_WithFolderPath_IncludesFolderContentsInZip()
        {
            // Arrange - Create a test folder with files and subdirectories
            var testFolderPath = ":sh:Documents:/folder";
            var documentsPhysicalPath = Path.Combine(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(), "shares", "Documents");
            var testFolderPhysicalPath = Path.Combine(documentsPhysicalPath, "folder");

            Directory.CreateDirectory(testFolderPhysicalPath);

            // Create files in the main folder
            await File.WriteAllTextAsync(Path.Combine(testFolderPhysicalPath, "file1.txt"), "Content of file1 in folder");
            await File.WriteAllTextAsync(Path.Combine(testFolderPhysicalPath, "file2.txt"), "Content of file2 in folder");

            // Create a subdirectory with files
            var subFolderPath = Path.Combine(testFolderPhysicalPath, "subfolder");
            Directory.CreateDirectory(subFolderPath);
            await File.WriteAllTextAsync(Path.Combine(subFolderPath, "subfile1.txt"), "Content of subfile1");
            await File.WriteAllTextAsync(Path.Combine(subFolderPath, "subfile2.txt"), "Content of subfile2");

            // Create request with folder path (this is the issue being fixed)
            var requestData = new { paths = new[] { testFolderPath } };

            // Act
            var response = await _client.PostAsync($"{BaseApiUrl}ZipFiles?respondWith=data", CreateJsonContent(requestData));

            // Assert
            AssertSuccessStatusCode(response, "ZipFiles POST with folder path");
            Assert.AreEqual("application/octet-stream", response.Content.Headers.ContentType?.MediaType);

            // Verify it's a valid ZIP file with folder contents
            var zipBytes = await response.Content.ReadAsByteArrayAsync();
            Assert.IsTrue(zipBytes.Length > 0, "ZIP file should not be empty");

            using (var stream = new MemoryStream(zipBytes))
            using (var archive = new ZipArchive(stream, ZipArchiveMode.Read))
            {
                // Should contain files from the folder and subfolder
                Assert.IsTrue(archive.Entries.Count >= 4, "ZIP should contain at least 4 files (2 from main folder + 2 from subfolder)");

                // Check for main folder files with folder prefix
                Assert.IsTrue(archive.Entries.Any(e => e.FullName == "folder/file1.txt"), "Should contain folder/file1.txt");
                Assert.IsTrue(archive.Entries.Any(e => e.FullName == "folder/file2.txt"), "Should contain folder/file2.txt");

                // Check for subfolder files
                Assert.IsTrue(archive.Entries.Any(e => e.FullName == "folder/subfolder/subfile1.txt"), "Should contain folder/subfolder/subfile1.txt");
                Assert.IsTrue(archive.Entries.Any(e => e.FullName == "folder/subfolder/subfile2.txt"), "Should contain folder/subfolder/subfile2.txt");

                // Verify file contents
                var mainFileEntry = archive.Entries.First(e => e.FullName == "folder/file1.txt");
                using (var entryStream = mainFileEntry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Content of file1 in folder", content);
                }

                var subFileEntry = archive.Entries.First(e => e.FullName == "folder/subfolder/subfile1.txt");
                using (var entryStream = subFileEntry.Open())
                using (var reader = new StreamReader(entryStream))
                {
                    var content = await reader.ReadToEndAsync();
                    Assert.AreEqual("Content of subfile1", content);
                }
            }
        }
    }
}