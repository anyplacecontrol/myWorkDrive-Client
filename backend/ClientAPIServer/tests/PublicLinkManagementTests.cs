using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class PublicLinkManagementTests : MockApiTestsBase
    {
        [TestMethod]
        public async Task GetPublicLinkSettings_ReturnsSettings()
        {
            // Act
            var response = await _client.GetAsync($"{BaseApiUrl}GetPublicLinkSettings");

            // Assert
            AssertSuccessStatusCode(response, "GetPublicLinkSettings");

            var content = await response.Content.ReadAsStringAsync();
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.ContainsKey("passwordIsMandatory"));
            Assert.IsTrue(settings.ContainsKey("allowDownloading"));
            Assert.IsTrue(settings.ContainsKey("allowUploading"));
            Assert.IsTrue(settings.ContainsKey("allowEditing"));
            Assert.IsTrue(settings.ContainsKey("allowFolderSharing"));
        }

        [TestMethod]
        public async Task GetPublicLinkSettings_WithPath_ReturnsPathSpecificSettings()
        {
            // Arrange
            var fileName = GenerateTestFileName("link-settings-test");
            await CreateTestFile(fileName, "test content for link settings");

            // Act
            var response = await _client.GetAsync(
                $"{BaseApiUrl}GetPublicLinkSettings?path={UrlEncode("/Documents/" + fileName)}");

            // Assert
            AssertSuccessStatusCode(response, "GetPublicLinkSettings with path");

            var content = await response.Content.ReadAsStringAsync();
            var settings = JsonConvert.DeserializeObject<Dictionary<string, object>>(content);
            
            Assert.IsNotNull(settings);
            Assert.IsTrue(settings.ContainsKey("allowOneDriveEditing"));
        }

        [TestMethod]
        public async Task CreatePublicLink_And_GetPublicLinkInfo_WorkCorrectly()
        {
            // Arrange
            var fileName = GenerateTestFileName("public-link-test");
            await CreateTestFile(fileName, "content for public link");
            var filePath = "/Documents/" + fileName;

            var linkData = new
            {
                path = filePath,
                allowDownloading = true,
                allowUploading = false,
                allowEditing = false,
                password = "testPassword123"
            };

            // Act - Create public link
            var createResponse = await _client.PostAsync(
                $"{BaseApiUrl}CreatePublicLink",
                CreateJsonContent(linkData));

            // Assert - Create public link
            AssertSuccessStatusCode(createResponse, "CreatePublicLink");

            var createContent = await createResponse.Content.ReadAsStringAsync();
            string publicLink;

            // Response can be either plain text or JSON
            if (createResponse.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(createContent);
                Assert.IsNotNull(linkInfo);
                Assert.IsTrue(linkInfo.ContainsKey("value"));
                publicLink = linkInfo["value"].ToString()!;
            }
            else
            {
                publicLink = createContent.Trim();
            }

            Assert.IsFalse(string.IsNullOrEmpty(publicLink));

            // Act - Get public link info
            var infoResponse = await _client.GetAsync(
                $"{BaseApiUrl}GetPublicLinkInfo?link={UrlEncode(publicLink)}");

            // Assert - Get public link info
            AssertSuccessStatusCode(infoResponse, "GetPublicLinkInfo");

            var infoContent = await infoResponse.Content.ReadAsStringAsync();
            var linkDetails = JsonConvert.DeserializeObject<Dictionary<string, object>>(infoContent);
            
            Assert.IsNotNull(linkDetails);
            Assert.IsTrue(linkDetails.ContainsKey("id"));
            Assert.IsTrue(linkDetails.ContainsKey("path"));
            Assert.IsTrue(linkDetails.ContainsKey("allowDownloading"));
            Assert.IsTrue(linkDetails.ContainsKey("hasPassword"));
            
            Assert.AreEqual(filePath, linkDetails["path"]);
            Assert.AreEqual(true, linkDetails["allowDownloading"]);
            Assert.AreEqual(true, linkDetails["hasPassword"]);

            // Clean up - Delete the public link
            var deleteResponse = await _client.GetAsync(
                $"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(publicLink)}");
            
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }

        [TestMethod]
        public async Task UpdatePublicLink_ModifiesExistingLink()
        {
            // Arrange
            var fileName = GenerateTestFileName("update-link-test");
            await CreateTestFile(fileName, "content for link update");
            var filePath = "/Documents/" + fileName;

            // Create initial public link
            var initialLinkData = new
            {
                path = filePath,
                allowDownloading = true,
                allowUploading = false,
                allowEditing = false,
                password = "initialPassword"
            };

            var createResponse = await _client.PostAsync(
                $"{BaseApiUrl}CreatePublicLink",
                CreateJsonContent(initialLinkData));

            AssertSuccessStatusCode(createResponse, "CreatePublicLink for update test");

            var createContent = await createResponse.Content.ReadAsStringAsync();
            string publicLink;

            if (createResponse.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(createContent);
                publicLink = linkInfo!["value"].ToString()!;
            }
            else
            {
                publicLink = createContent.Trim();
            }

            // Update the link
            var updateData = new
            {
                path = filePath,
                allowDownloading = true,
                allowUploading = true, // Changed from false
                allowEditing = true,   // Changed from false
                password = "updatedPassword"
            };

            // Act
            var updateResponse = await _client.PostAsync(
                $"{BaseApiUrl}UpdatePublicLink?link={UrlEncode(publicLink)}",
                CreateJsonContent(updateData));

            // Assert
            AssertSuccessStatusCode(updateResponse, "UpdatePublicLink");

            var updateContent = await updateResponse.Content.ReadAsStringAsync();
            var updatedLinkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(updateContent);
            
            Assert.IsNotNull(updatedLinkInfo);
            Assert.IsTrue(updatedLinkInfo.ContainsKey("allowUploading"));
            Assert.IsTrue(updatedLinkInfo.ContainsKey("allowEditing"));
            
            Assert.AreEqual(true, updatedLinkInfo["allowUploading"]);
            Assert.AreEqual(true, updatedLinkInfo["allowEditing"]);

            // Clean up
            await _client.GetAsync($"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(publicLink)}");
        }

        [TestMethod]
        public async Task ListPublicLinks_ShowsUserLinks()
        {
            // Arrange
            var fileName1 = GenerateTestFileName("list-link1");
            var fileName2 = GenerateTestFileName("list-link2");
            
            await CreateTestFile(fileName1, "content 1");
            await CreateTestFile(fileName2, "content 2");

            var createdLinks = new List<string>();

            // Create multiple public links
            for (int i = 0; i < 2; i++)
            {
                var fileName = i == 0 ? fileName1 : fileName2;
                var linkData = new
                {
                    path = "/Documents/" + fileName,
                    allowDownloading = true,
                    allowUploading = false,
                    allowEditing = false,
                    password = $"password{i + 1}"
                };

                var createResponse = await _client.PostAsync(
                    $"{BaseApiUrl}CreatePublicLink",
                    CreateJsonContent(linkData));

                AssertSuccessStatusCode(createResponse, $"Create link {i + 1}");

                var createContent = await createResponse.Content.ReadAsStringAsync();
                string publicLink;

                if (createResponse.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(createContent);
                    publicLink = linkInfo!["value"].ToString()!;
                }
                else
                {
                    publicLink = createContent.Trim();
                }

                createdLinks.Add(publicLink);
            }

            // Act
            var listResponse = await _client.GetAsync($"{BaseApiUrl}ListPublicLinks");

            // Assert
            AssertSuccessStatusCode(listResponse, "ListPublicLinks");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var links = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);
            
            Assert.IsNotNull(links);
            Assert.IsTrue(links.Count >= 2, "Should have at least the 2 links we created");

            // Verify our links are in the list
            var linkPaths = links.Select(l => l["path"].ToString()).ToList();
            Assert.IsTrue(linkPaths.Contains("/Documents/" + fileName1));
            Assert.IsTrue(linkPaths.Contains("/Documents/" + fileName2));

            // Clean up
            foreach (var link in createdLinks)
            {
                await _client.GetAsync($"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(link)}");
            }
        }

        [TestMethod]
        public async Task ListPublicLinks_WithQueryFilter_FiltersResults()
        {
            // Arrange
            var fileName = GenerateTestFileName("query-filter-test");
            await CreateTestFile(fileName, "content for query filter");

            var linkData = new
            {
                path = "/" + fileName,
                allowDownloading = true,
                allowUploading = false,
                allowEditing = false,
                password = "queryPassword"
            };

            var createResponse = await _client.PostAsync(
                $"{BaseApiUrl}CreatePublicLink",
                CreateJsonContent(linkData));

            AssertSuccessStatusCode(createResponse, "Create link for query test");

            var createContent = await createResponse.Content.ReadAsStringAsync();
            string publicLink;

            if (createResponse.Content.Headers.ContentType?.MediaType == "application/json")
            {
                var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(createContent);
                publicLink = linkInfo!["value"].ToString()!;
            }
            else
            {
                publicLink = createContent.Trim();
            }

            // Act
            var listResponse = await _client.GetAsync(
                $"{BaseApiUrl}ListPublicLinks?query={UrlEncode("query-filter")}");

            // Assert
            AssertSuccessStatusCode(listResponse, "ListPublicLinks with query");

            var listContent = await listResponse.Content.ReadAsStringAsync();
            var filteredLinks = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(listContent);
            
            Assert.IsNotNull(filteredLinks);
            
            // Should find our link since it contains "query-filter" in the path
            var matchingLink = filteredLinks.FirstOrDefault(l => l["path"].ToString()!.Contains(fileName));
            Assert.IsNotNull(matchingLink, "Should find the link with matching query");

            // Clean up
            await _client.GetAsync($"{BaseApiUrl}DeletePublicLinks?link={UrlEncode(publicLink)}");
        }

        [TestMethod]
        public async Task DeletePublicLinks_POST_DeletesMultipleLinks()
        {
            // Arrange
            var fileName1 = GenerateTestFileName("bulk-delete1");
            var fileName2 = GenerateTestFileName("bulk-delete2");
            
            await CreateTestFile(fileName1, "content 1");
            await CreateTestFile(fileName2, "content 2");

            var createdLinks = new List<string>();

            // Create links
            for (int i = 0; i < 2; i++)
            {
                var fileName = i == 0 ? fileName1 : fileName2;
                var linkData = new
                {
                    path = "/Documents/" + fileName,
                    allowDownloading = true,
                    allowUploading = false,
                    allowEditing = false,
                    password = $"bulkPassword{i + 1}"
                };

                var createResponse = await _client.PostAsync(
                    $"{BaseApiUrl}CreatePublicLink",
                    CreateJsonContent(linkData));

                AssertSuccessStatusCode(createResponse, $"Create bulk delete link {i + 1}");

                var createContent = await createResponse.Content.ReadAsStringAsync();
                string publicLink;

                if (createResponse.Content.Headers.ContentType?.MediaType == "application/json")
                {
                    var linkInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(createContent);
                    publicLink = linkInfo!["value"].ToString()!;
                }
                else
                {
                    publicLink = createContent.Trim();
                }

                createdLinks.Add(publicLink);
            }

            // Prepare bulk delete data
            var deleteData = new
            {
                links = createdLinks.ToArray()
            };

            // Act
            var deleteResponse = await _client.PostAsync(
                $"{BaseApiUrl}DeletePublicLinks",
                CreateJsonContent(deleteData));

            // Assert
            Assert.AreEqual(HttpStatusCode.NoContent, deleteResponse.StatusCode, "Bulk delete should return NoContent");

            // Verify links are deleted by trying to get info for them
            foreach (var link in createdLinks)
            {
                var infoResponse = await _client.GetAsync(
                    $"{BaseApiUrl}GetPublicLinkInfo?link={UrlEncode(link)}");
                
                Assert.IsFalse(infoResponse.IsSuccessStatusCode, "Link should be deleted and info should fail");
            }
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