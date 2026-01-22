using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class UploadDownloadTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task StartFileUpload_And_CompleteUpload_WorksCorrectly()
        {
            // Arrange
            var fileName = GenerateTestFileName("upload-test");
            var filePath = "/Documents/" + fileName;
            var fileSize = 1024L;

            var startUploadData = new
            {
                size = fileSize,
                conflictBehavior = "replace"
            };

            // Act - Start upload
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            // Assert - Start upload
            AssertSuccessStatusCode(startResponse, "StartFileUpload");

            var startContent = await startResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(startContent);
            
            Assert.IsNotNull(uploadInfo);
            Assert.IsTrue(uploadInfo.ContainsKey("uploadId"));

            var uploadId = uploadInfo["uploadId"].ToString();
            Assert.IsFalse(string.IsNullOrEmpty(uploadId));

            // Act - Complete upload
            var completeResponse = await _client.PostAsync(
                $"{BaseApiUrl}CompleteUpload?uploadId={UrlEncode(uploadId)}&checksum=",
                null);

            // Assert - Complete upload
            AssertSuccessStatusCode(completeResponse, "CompleteUpload");
        }

        [TestMethod]
        public async Task WriteFileBlock_UploadProcess_WorksCorrectly()
        {
            // Arrange
            var fileName = GenerateTestFileName("block-upload");
            var filePath = "/Documents/" + fileName;
            
            var blockSize = 1000;
            var numBlocks = 5;
            var totalSize = blockSize * numBlocks;

            // Generate test data
            var testData = new byte[totalSize];
            var random = new Random(42); // Fixed seed for reproducible tests
            random.NextBytes(testData);

            var startUploadData = new
            {
                size = totalSize,
                conflictBehavior = "replace"
            };

            // Act - Start upload session
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(startResponse, "StartFileUpload");

            var startContent = await startResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(startContent);
            var uploadId = uploadInfo!["uploadId"].ToString();

            // Act - Upload blocks
            for (int i = 0; i < numBlocks; i++)
            {
                var blockData = new byte[blockSize];
                Array.Copy(testData, i * blockSize, blockData, 0, blockSize);

                var blockResponse = await _client.PutAsync(
                    $"{BaseApiUrl}WriteFileBlock?uploadId={UrlEncode(uploadId!)}&startPosition={i * blockSize}",
                    new ByteArrayContent(blockData));

                AssertSuccessStatusCode(blockResponse, $"WriteFileBlock {i}");
            }

            // Act - Complete upload
            var completeResponse = await _client.PostAsync(
                $"{BaseApiUrl}CompleteUpload?uploadId={UrlEncode(uploadId!)}&checksum=",
                null);

            AssertSuccessStatusCode(completeResponse, "CompleteUpload");

            // Verify file was created with correct size
            var fileInfoResponse = await _client.GetAsync($"{BaseApiUrl}GetFileInfo?path={UrlEncode(filePath)}");
            AssertSuccessStatusCode(fileInfoResponse, "GetFileInfo after upload");

            var fileInfoContent = await fileInfoResponse.Content.ReadAsStringAsync();
            var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileInfoContent);
            
            Assert.IsNotNull(fileInfo);
            Assert.IsTrue(fileInfo.ContainsKey("size"));
            
            var actualSize = Convert.ToInt64(fileInfo["size"]);
            Assert.AreEqual(totalSize, actualSize, "File size should match uploaded data size");

            // Verify file content by reading it back
            var readResponse = await _client.GetAsync(
                $"{BaseApiUrl}ReadFile?path={UrlEncode(filePath)}&startPosition=0&count={totalSize}");

            AssertSuccessStatusCode(readResponse, "ReadFile after upload");

            var readData = await readResponse.Content.ReadAsByteArrayAsync();
            Assert.AreEqual(totalSize, readData.Length, "Read data length should match");
            
            // Compare data
            for (int i = 0; i < totalSize; i++)
            {
                if (testData[i] != readData[i])
                {
                    Assert.Fail($"Data mismatch at position {i}. Expected: {testData[i]}, Actual: {readData[i]}");
                }
            }
        }

        [TestMethod]
        public async Task GetUploadStatus_ReturnsCorrectStatus()
        {
            // Arrange
            var fileName = GenerateTestFileName("status-test");
            var filePath = "/Documents/" + fileName;

            var startUploadData = new
            {
                size = 1024L,
                conflictBehavior = "replace"
            };

            // Start upload session
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(startResponse, "StartFileUpload");

            var startContent = await startResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(startContent);
            var uploadId = uploadInfo!["uploadId"].ToString();

            // Act - Get upload status
            var statusResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetUploadStatus?uploadId={UrlEncode(uploadId!)}");

            // Assert
            AssertSuccessStatusCode(statusResponse, "GetUploadStatus");

            var statusContent = await statusResponse.Content.ReadAsStringAsync();
            var statusInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(statusContent);
            
            Assert.IsNotNull(statusInfo);
            Assert.IsTrue(statusInfo.ContainsKey("status"));
            
            var status = statusInfo["status"].ToString();
            Assert.IsTrue(new[] { "InProgress", "Complete", "Error" }.Contains(status));
        }

        [TestMethod]
        public async Task CancelUpload_CancelsUploadSession()
        {
            // Arrange
            var fileName = GenerateTestFileName("cancel-test");
            var filePath = "/Documents/" + fileName;

            var startUploadData = new
            {
                size = 2048L,
                conflictBehavior = "replace"
            };

            // Start upload session
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(startResponse, "StartFileUpload");

            var startContent = await startResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(startContent);
            var uploadId = uploadInfo!["uploadId"].ToString();

            // Act - Cancel upload
            var cancelResponse = await _client.PostAsync(
                $"{BaseApiUrl}CancelUpload?uploadId={UrlEncode(uploadId!)}",
                null);

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, cancelResponse.StatusCode, "Cancel upload should return NoContent");

            // Verify that trying to complete the cancelled upload fails
            var completeResponse = await _client.PostAsync(
                $"{BaseApiUrl}CompleteUpload?uploadId={UrlEncode(uploadId!)}&checksum=",
                null);

            Assert.IsFalse(completeResponse.IsSuccessStatusCode, "Complete upload should fail after cancellation");
        }

        [TestMethod]
        public async Task GetTransferLink_ReturnsValidLink()
        {
            // Arrange
            var fileName = GenerateTestFileName("transfer-link");
            await CreateTestFile(fileName, "content for transfer link");

            // Act - Get download link
            var downloadLinkResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetTransferLink?path={UrlEncode("/" + fileName)}&operation=download");

            // Assert
            AssertSuccessStatusCode(downloadLinkResponse, "GetTransferLink download");

            var linkContent = await downloadLinkResponse.Content.ReadAsStringAsync();
            
            // Response can be either plain text or JSON
            if (downloadLinkResponse.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(linkContent);
                Assert.IsNotNull(linkInfo);
                Assert.IsTrue(linkInfo.ContainsKey("link"));
                
                var link = linkInfo["link"].ToString();
                Assert.IsFalse(string.IsNullOrEmpty(link));
                Assert.IsTrue(Uri.IsWellFormedUriString(link, UriKind.RelativeOrAbsolute));
            }
            else
            {
                // Plain text response
                Assert.IsFalse(string.IsNullOrEmpty(linkContent));
            }

            // Act - Get upload link
            var uploadLinkResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetTransferLink?path={UrlEncode("/" + fileName)}&operation=upload");

            // Assert
            AssertSuccessStatusCode(uploadLinkResponse, "GetTransferLink upload");
        }

        [TestMethod]
        public async Task StartFileUpload_WithConflictBehaviorFail_Returns409WhenFileExists()
        {
            // Arrange - Create a test file first
            var fileName = GenerateTestFileName("conflict-test");
            var filePath = "/Documents/" + fileName;
            await CreateTestFile(fileName, "existing content");

            var startUploadData = new
            {
                size = 1024L,
                conflictBehavior = "fail"
            };

            // Act - Try to start upload with conflictBehavior = "fail"
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            // Assert - Should return 409 Conflict
            Assert.AreEqual(HttpStatusCode.Conflict, startResponse.StatusCode,
                "StartFileUpload should return 409 Conflict when file exists and conflictBehavior is 'fail'");

            var responseContent = await startResponse.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Contains("already exists"),
                "Response should indicate that the object already exists");
        }

        [TestMethod]
        public async Task StartFileUpload_MultipleCallsWithConflictBehaviorFail_Returns409ForSecondCall()
        {
            // Test that multiple calls to StartFileUpload with conflictBehavior=fail returns 409 on second call
            var fileName = GenerateTestFileName("multi-upload-fail-test");
            var filePath = "/Documents/" + fileName;

            var startUploadData = new
            {
                size = 1024L,
                conflictBehavior = "fail"
            };

            // First call - should succeed
            var firstResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(firstResponse, "First StartFileUpload call should succeed");

            // Second call with same path and fail behavior - should return 409
            var secondResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            Assert.AreEqual(HttpStatusCode.Conflict, secondResponse.StatusCode,
                "Second StartFileUpload call should return 409 Conflict when conflictBehavior is 'fail'");

            var responseContent = await secondResponse.Content.ReadAsStringAsync();
            Assert.IsTrue(responseContent.Contains("upload session") && responseContent.Contains("already exists"),
                "Response should indicate that an upload session already exists");
        }

        [TestMethod]
        public async Task StartFileUpload_MultipleCallsWithConflictBehaviorRename_BothSucceed()
        {
            // Test that multiple calls to StartFileUpload with conflictBehavior=rename both succeed
            var fileName = GenerateTestFileName("multi-upload-rename-test");
            var filePath = "/Documents/" + fileName;

            var startUploadData = new
            {
                size = 1024L,
                conflictBehavior = "rename"
            };

            // First call - should succeed
            var firstResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(firstResponse, "First StartFileUpload call should succeed");

            var firstContent = await firstResponse.Content.ReadAsStringAsync();
            var firstUploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(firstContent);
            Assert.IsNotNull(firstUploadInfo);
            Assert.IsTrue(firstUploadInfo.ContainsKey("uploadId"), "First response should contain uploadId");
            var firstUploadId = firstUploadInfo["uploadId"].ToString();

            // Second call with same path and rename behavior - should also succeed
            var secondResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            AssertSuccessStatusCode(secondResponse, "Second StartFileUpload call should also succeed with rename behavior");

            var secondContent = await secondResponse.Content.ReadAsStringAsync();
            var secondUploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(secondContent);
            Assert.IsNotNull(secondUploadInfo);
            Assert.IsTrue(secondUploadInfo.ContainsKey("uploadId"), "Second response should contain uploadId");
            var secondUploadId = secondUploadInfo["uploadId"].ToString();

            // Upload IDs should be different
            Assert.AreNotEqual(firstUploadId, secondUploadId, "Upload IDs should be different for rename behavior");
        }

        [TestMethod]
        public async Task StartFileUpload_WithConflictBehaviorFail_SucceedsWhenFileDoesNotExist()
        {
            // Arrange
            var fileName = GenerateTestFileName("no-conflict-test");
            var filePath = "/Documents/" + fileName;

            var startUploadData = new
            {
                size = 1024L,
                conflictBehavior = "fail"
            };

            // Act - Start upload on non-existing file with conflictBehavior = "fail"
            var startResponse = await _client.PostAsync(
                $"{BaseApiUrl}StartFileUpload?path={UrlEncode(filePath)}",
                CreateJsonContent(startUploadData));

            // Assert - Should succeed
            AssertSuccessStatusCode(startResponse, "StartFileUpload should succeed when file doesn't exist");

            var startContent = await startResponse.Content.ReadAsStringAsync();
            var uploadInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(startContent);

            Assert.IsNotNull(uploadInfo);
            Assert.IsTrue(uploadInfo.ContainsKey("uploadId"));

            var uploadId = uploadInfo["uploadId"].ToString();
            Assert.IsFalse(string.IsNullOrEmpty(uploadId));
        }

        // Helper method to create test files
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
                    $"{BaseApiUrl}WriteFile?path={UrlEncode("/" + fileName)}&startPosition=0&unlockAfterWrite=true",
                    new ByteArrayContent(contentBytes));
                AssertSuccessStatusCode(writeResponse, $"Write content to {fileName}");
            }
        }
    }
}