using APIServer;
using WanPath.Common.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MockServerAPITests.AdaptedRealTests
{
    /// <summary>
    /// Adapted version of the original ClientAPITests from real_tests
    /// Modified to work with the mock server while preserving original test logic
    /// </summary>
    [TestClass]
    public class AdaptedClientAPITests : AdaptedRealTestsBase
    {
        // Adapted from original: const string PlaygroundDir = MyWorkDrive.IntegrationTests.Shares.Local.TestFolder;
        const string PlaygroundDir = "/"; // Mock server uses root path
        string testFolder;

        public AdaptedClientAPITests() : base()
        {
            // Adapted from original testFolder initialization  
            testFolder = MWDMockServer.Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(); // Use configured base path
        }

        private string GetRequestUri(string apiEndpoint)
        {
            // Adapted from original: return "http://localhost:8357" + apiEndpoint;
            return apiEndpoint; // Mock server uses relative URLs
        }

        #region Original Mirror Tests (for basic functionality validation)
        
        [TestMethod]        
        public async Task TestJsonRequestAsync()
        {
            // Original test preserved exactly as is
            string requestJSON = "{\r\n\t\"stringKey\": \"stringValue\",\r\n\t\"intKey\": 12345,\r\n\t\"objValue\": {\r\n\t  \"anotherStringKey\": \"anotherValue\",\r\n\t  \"boolKey\": true\t\r\n\t},\r\n\t\"arrayObj\": [\r\n\t\t{\r\n\t\t  \"arrayStringValue\": \"obj1\"\r\n\t\t},\r\n\t\t{\r\n\t\t  \"arrayStringValue\": \"obj2\"\r\n\t\t}\t\r\n\t]\r\n}";

            HttpResponseMessage response = null;
            try
            {                
                response = await _client.PostAsync(GetRequestUri(APIEndpoints.MIRROR), new StringContent(requestJSON, Encoding.UTF8, APIConstants.CONTENT_TYPE_APPLICATION_JSON));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode) 200, response.StatusCode);

            // Check content type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            JsonDocument doc = JsonDocument.Parse(content);

            Assert.AreEqual("stringValue", doc.RootElement.GetProperty("stringKey").GetString());
            Assert.AreEqual(12345, doc.RootElement.GetProperty("intKey").GetInt64());
            Assert.AreEqual("anotherValue", doc.RootElement.GetProperty("objValue.anotherStringKey").GetString());
            Assert.AreEqual(true, doc.RootElement.GetProperty("objValue.boolKey").GetBoolean());
            Assert.AreEqual("obj1", doc.RootElement.GetProperty("arrayObj[0].arrayStringValue").GetString());
            Assert.AreEqual("obj2", doc.RootElement.GetProperty("arrayObj[1].arrayStringValue").GetString());
        }

        [TestMethod]
        public async Task TestJsonRequestWithDateAsync()
        {
            // Original test preserved - adapted StrUtils reference
            DateTime now = DateTime.UtcNow;

            // we need to strip down micro- and nanoseconds because below, the comparison doesn't work as RFC8601 date carries only milliseconds.
            now = new DateTime(now.Year, now.Month, now.Day, now.Hour, now.Minute, now.Second);

            // Adapted: Use direct ISO8601 formatting instead of StrUtils.RenderDateISO8601
            string requestJSON = string.Format("{{\n\t\"dateKey\": \"{0}\"\n}}", now.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"));

            HttpResponseMessage response = null;
            try
            {
                response = await _client.PostAsync(GetRequestUri(APIEndpoints.MIRROR), new StringContent(requestJSON, Encoding.UTF8, APIConstants.CONTENT_TYPE_APPLICATION_JSON));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode) 200, response.StatusCode);

            // Check content type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            JsonDocument doc = JsonDocument.Parse(content);

            string serverNow = doc.RootElement.GetProperty("dateKey").GetString();
            Assert.IsNotNull(serverNow);
            // Adapted: Use DateTime.Parse instead of StrUtils.ParseDateISO8601
            Assert.AreEqual(now, DateTime.Parse(serverNow).ToUniversalTime());
        }

        [TestMethod]
        public async Task TestURLParamsRequestAsync()
        {
            // Original test preserved - adapted StrUtils reference
            Dictionary<string, string> queryParams = new Dictionary<string, string>()
            {
                { "stringKey", "stringValue" },
                { "intKey", "12345" },
                { "boolKey", "true" }
            };

            // Adapted: Use direct URL encoding instead of StrUtils.FlattenDictionary
            string requestParams = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            HttpResponseMessage response = null;
            try
            {
                var requestUri = string.Join("?", GetRequestUri(APIEndpoints.MIRROR), requestParams);
                response = await _client.GetAsync(requestUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode)200, response.StatusCode);

            // Check content type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            JsonDocument doc = JsonDocument.Parse(content);

            Assert.AreEqual("stringValue", doc.RootElement.GetProperty("stringKey").GetString());
            Assert.AreEqual(12345, doc.RootElement.GetProperty("intKey").GetInt64());
            Assert.AreEqual(true, doc.RootElement.GetProperty("boolKey").GetBoolean());
        }

        [TestMethod]        
        public async Task TestFormRequestAsync()
        {
            // Original test preserved - adapted StrUtils reference
            Dictionary<string, string> queryParams = new Dictionary<string, string>()
            {
                { "stringKey", "stringValue" },
                { "intKey", "12345" },
                { "boolKey", "true" }
            };

            // Adapted: Use direct URL encoding instead of StrUtils.FlattenDictionary
            string requestForm = string.Join("&", queryParams.Select(kvp => $"{Uri.EscapeDataString(kvp.Key)}={Uri.EscapeDataString(kvp.Value)}"));

            HttpResponseMessage response = null;
            try
            {
                response = await _client.PostAsync(GetRequestUri(APIEndpoints.MIRROR), new StringContent(requestForm, Encoding.UTF8, APIConstants.CONTENT_TYPE_APPLICATION_WWWFORM));
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode)200, response.StatusCode);

            // Check content type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            JsonDocument doc = JsonDocument.Parse(content);

            Assert.AreEqual("stringValue", doc.RootElement.GetProperty("stringKey").GetString());
            Assert.AreEqual(12345, doc.RootElement.GetProperty("intKey").GetInt64());
            Assert.AreEqual(true, doc.RootElement.GetProperty("boolKey").GetBoolean());
        }

        [TestMethod]
        public async Task TestMultipartFormRequestAsync()
        {
            // Original test preserved
            HttpResponseMessage response = null;
            try
            {
                var requestContent = new MultipartFormDataContent
                {
                    { new StringContent("stringValue", Encoding.UTF8), "stringKey" },
                    { new StringContent("12345", Encoding.UTF8), "intKey" },
                    { new StringContent("True", Encoding.UTF8), "boolKey" }
                };

                response = await _client.PostAsync(GetRequestUri(APIEndpoints.MIRROR), requestContent);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode)200, response.StatusCode);

            // Check content type
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            JsonDocument doc = JsonDocument.Parse(content);

            Assert.AreEqual("stringValue", doc.RootElement.GetProperty("stringKey").GetString());
            Assert.AreEqual(12345, doc.RootElement.GetProperty("intKey").GetInt64());
            Assert.AreEqual(true, doc.RootElement.GetProperty("boolKey").GetBoolean());
        }

        [TestMethod]
        public async Task TestFailingRequestAsync()
        {
            // Original test preserved
            HttpResponseMessage response = null;
            try
            {
                var requestUri = string.Join("?", GetRequestUri(APIEndpoints.MIRROR), "failRequest=451");
                response = await _client.GetAsync(requestUri);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                Assert.Fail(ex.Message);
            }
            // Check response code
            Assert.AreEqual((HttpStatusCode)451, response.StatusCode);

            // Check content type            
            string contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.AreEqual(APIConstants.CONTENT_TYPE_APPLICATION_JSON, contentType);

            // Parse the response
            string content = await response.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(content));
            
            JsonDocument doc = JsonDocument.Parse(content);
            Assert.AreEqual(APIConstants.REST_JSON_RESPONSE_STATUS_FAILED, doc.RootElement.GetProperty(APIConstants.REST_PROP_NAME_STATUS).GetString());
            Assert.AreEqual(APIErrorCodes.TEST_FAILURE, doc.RootElement.GetProperty(APIConstants.REST_PROP_NAME_ERROR_CODE).GetInt32());
            Assert.IsFalse(string.IsNullOrEmpty(doc.RootElement.GetProperty(APIConstants.REST_PROP_NAME_MESSAGE).GetString()));
        }
        
        #endregion

        #region Utility Methods (adapted from original)
                                      
        private void CleanUp()
        {                                      
        }

        private void DeleteFiles(List<string> files)
        {
            foreach(var f in files)
            {
                DeleteFile(f);
            }
        }

        private void DeleteFile(string file)
        {            
            var path = $"{testFolder}\\{file}";
            if (System.IO.File.Exists(path))
                System.IO.File.Delete(path);
        }

        private void DeleteDirectories(List<string> dirs)
        {
            foreach(var d in dirs)
            {
                DeleteDirectory(d);
            }
        }

        private void DeleteDirectory(string dir)
        {
            var path = $"{testFolder}\\{dir}";
            if (System.IO.Directory.Exists(path))
                System.IO.Directory.Delete(path, true);
        }

        private string ComposeItemPath(string itemName)
        {
            // Ensure proper path formatting for mock server
            if (string.IsNullOrEmpty(itemName) || itemName == "/")
                return "/";
            
            // Remove leading slash if present to avoid double slashes
            if (itemName.StartsWith("/"))
                itemName = itemName.Substring(1);
            
            return "/" + itemName;
        }

        async Task<bool> RemoteFileOrDirectoryExists(string precomposedPath)
        {
            // Check that the file exists - adapted for new API
            var fileExistsResponse = await _client.GetAsync(ClientAPIEndpoints.GET_ITEM_TYPE + "?path=" + Uri.EscapeDataString(precomposedPath));
            Assert.IsTrue(fileExistsResponse.IsSuccessStatusCode);
            var responseString = await fileExistsResponse.Content.ReadAsStringAsync();
            
            // Adapted for new API response format
            if (responseString.Contains("\"value\""))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                var itemType = result?["value"]?.ToString();
                return itemType == ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FILE || 
                       itemType == ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FOLDER;
            }
            
            return responseString.Equals(ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FILE) || 
                   responseString.Equals(ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FOLDER);
        }

        async Task<bool> RemoteFileExists(string precomposedPath)
        {
            // Check that the file exists - adapted for new API
            var requestUri = GetRequestUri(ClientAPIEndpoints.GET_ITEM_TYPE) + "?path=" + Uri.EscapeDataString(precomposedPath);
            var fileExistsResponse = await _client.GetAsync(requestUri);
            Assert.IsTrue(fileExistsResponse.IsSuccessStatusCode);
            var responseString = await fileExistsResponse.Content.ReadAsStringAsync();
            if (string.IsNullOrEmpty(responseString))
                return false;
            
            // Adapted for new API response format - handle both old and new response formats
            if (responseString.StartsWith("{"))
            {
                var result = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                return result?["value"]?.ToString() == ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FILE;
            }
            
            return responseString.Equals("{ value: \""+ ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FILE + "\" }");
        }

        async Task<bool> RemoteDirectoryExists(string precomposedPath)
        {
            // Check that the directory exists - adapted for new API
            var folderExistsResponse = await _client.GetAsync(GetRequestUri(ClientAPIEndpoints.GET_ITEM_TYPE) + "?path=" + Uri.EscapeDataString(precomposedPath));
            Assert.IsTrue(folderExistsResponse.IsSuccessStatusCode);
            var responseString = await folderExistsResponse.Content.ReadAsStringAsync();
            
            // Adapted for new API response format - handle both old and new response formats
            if (responseString.StartsWith("{"))
            {
                var jsonResult = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
                return jsonResult?["value"]?.ToString() == ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FOLDER;
            }
            
            var stringResult = responseString.Equals("{ value: \"" + ClientAPIConstants.RESPONSE_VALUE_ITEM_TYPE_FOLDER + "\" }");            
            return stringResult;
        }

        async Task<long> GetRemoteFileSize(string precomposedPath)
        {
            // Check that the file exists - preserved original logic
            var fileExistsResponse = await _client.GetAsync(GetRequestUri(ClientAPIEndpoints.GET_FILE_INFORMATION) + "?path=" + Uri.EscapeDataString(precomposedPath));
            Assert.IsTrue(fileExistsResponse.IsSuccessStatusCode);
            var responseString = await fileExistsResponse.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseString));
            var itemInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(responseString);
            Assert.IsTrue(itemInfo.ContainsKey(ClientAPIConstants.RESPONSE_VALUE_NAME_PATH));
            object size = itemInfo[ClientAPIConstants.RESPONSE_VALUE_NAME_SIZE];            
            long result = Convert.ToInt32(size);            
            return result;
        }

        #endregion

        #region File and Folder Tests (adapted from original)

        [TestMethod]
        public async Task TestCreateFilesAsync()
        {
            // Original test logic preserved, adapted for mock server
            string testFileName = "test-create-file";
            string testFileExt = "txt";
            string testDirName = "new-dir";
            DeleteFile($"{testFileName}.{testFileExt}");
            DeleteFile($".{testFileExt}");            
            DeleteDirectory(testDirName);            
            {            
                // 1
                var createFileResponse = await CreateFileAsync(CreateFileRequestBody(ComposeItemPath(PlaygroundDir), true, testFileName, testFileExt, false, ConflictBehavior.replace, null));
                Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
                Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                                createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                              "CreateFiles test 1 failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

                string responseString = await createFileResponse.Content.ReadAsStringAsync();
                Assert.IsFalse(string.IsNullOrEmpty(responseString));

                // 2                
                createFileResponse = await CreateFileAsync(CreateFileRequestBody(ComposeItemPath(PlaygroundDir), true, "", testFileExt, false, ConflictBehavior.replace, null));
                Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
                Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                                createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                              "CreateFiles test 2 failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

                responseString = await createFileResponse.Content.ReadAsStringAsync();
                Assert.IsFalse(string.IsNullOrEmpty(responseString));

                // 3                
                createFileResponse = await CreateFileAsync(CreateFileRequestBody(ComposeItemPath(PlaygroundDir), false, testDirName, null, false, ConflictBehavior.replace, null));
                Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
                Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                                createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                              "CreateFiles test 3 failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

                // Check that the file exists
                Assert.IsTrue(await RemoteFileExists(ComposeItemPath($"{testFileName}.{testFileExt}")));

                // Check that the folder exists
                Assert.IsTrue(await RemoteDirectoryExists(ComposeItemPath(testDirName)));
            }
        }

        private async Task<HttpResponseMessage> CreateFileAsync(HttpContent content)
        {
            return await _client.PostAsync(GetRequestUri(ClientAPIEndpoints.CREATE_FILE), content);
        }

        private enum ConflictBehavior
        {
            replace
        }

        private object CreateFileData(string path, bool createFile, string name, string ext, bool createContent, ConflictBehavior conflictBehavior, string repository = null)
        {
            // Adapted: Remove repository parameter as it's not needed in new API
            return new
            {
                path = path,
                createFile = createFile,
                name = name,
                extension = ext,
                createContent = createContent,
                conflictBehavior = conflictBehavior.ToString()
                // repository parameter removed in new API
            };
        }

        private StringContent CreateFileRequestBody(string path, string name)
        {
            return CreateFileRequestBody(path, true, name, null, false, ConflictBehavior.replace, null);
        }

        private StringContent CreateFileRequestBody(string path, bool createFile, string name, string ext, bool createContent, ConflictBehavior conflictBehavior, string repository)
        {
            var fileData = CreateFileData(path, createFile, name, ext, createContent, ConflictBehavior.replace, repository);
            return new StringContent(Newtonsoft.Json.JsonConvert.SerializeObject(fileData), System.Text.Encoding.UTF8, "application/json");
        }

        #endregion

        #region More File Operations Tests (selected from original)

        [TestMethod]
        public async Task TestCreateFolderAsync()
        {
            // Original test preserved
            string dirName = "testDir";
            DeleteDirectory(dirName);
            var createFileResponse = await CreateFileAsync(CreateFileRequestBody(ComposeItemPath(PlaygroundDir), false, dirName, null, false, ConflictBehavior.replace, "myRepoId"));
            Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
            Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                            createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                          "CreateFolder test failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

            Assert.IsTrue(await RemoteDirectoryExists(ComposeItemPath(dirName)));
            DeleteDirectory(dirName);
        }

        [TestMethod]
        public async Task TestListFolderAsync()
        {
            // Original test logic preserved
            List<string> testFiles = new List<string> { "list1.txt", "list2.txt" };
            List<string> testFolders = new List<string> { "listdir" };
            DeleteFiles(testFiles);
            DeleteDirectories(testFolders);
            
            // Arrange: create backend items
            var backendItems = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var f in testFiles)
            {
                backendItems.Add(f);
                await CreateRemoteFileAsync(ComposeItemPath(PlaygroundDir), f);
            }
            foreach (var d in testFolders)
            {
                backendItems.Add(d);
                await CreateRemoteDirectoryAsync(ComposeItemPath(PlaygroundDir), d);
            }

            // Act: call API            
            var response = await _client.GetAsync(GetRequestUri(ClientAPIEndpoints.LIST_FOLDER) + "?path=" + Uri.EscapeDataString(ComposeItemPath(PlaygroundDir)));
            if (!response.IsSuccessStatusCode)
                throw new Exception("RunListFolderTest: API call failed");
            var json = await response.Content.ReadAsStringAsync();

            // Assert: parse JSON response
            var items = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(json);
            Assert.IsNotNull(items);
            var apiNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in items)
            {
                if (item.TryGetValue("name", out var nameObj) && nameObj is string name)
                    apiNames.Add(name);
            }

            foreach(var i in backendItems)
            {
                Assert.IsTrue(apiNames.Contains(i));
            }                        
        }

        private async Task CreateRemoteFileAsync(string path, string filename, string content = "")
        {
            await CreateRemoteFileAsync(path, filename, string.IsNullOrEmpty(content) ? new byte[0] : Encoding.UTF8.GetBytes(content));
        }

        private async Task CreateRemoteFileAsync(string path, string filename, byte[] content)
        {                        
            var createFileResponse = await CreateFileAsync(CreateFileRequestBody(path, filename));
            Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
            Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                            createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                          "CreateRemoteFileAsync failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

            string responseString = await createFileResponse.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseString));

            if (content.Length > 0)
            {
                // Retrieve the name of the created file so that we could upload file data.
                var itemInfo = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseString);
                Assert.IsTrue(itemInfo.ContainsKey(ClientAPIConstants.RESPONSE_VALUE_NAME_PATH));
                string filePath = itemInfo[ClientAPIConstants.RESPONSE_VALUE_NAME_PATH];
                Assert.IsFalse(string.IsNullOrEmpty(filePath));

                using (var ms = new MemoryStream(content))
                using (var sc = new StreamContent(ms))
                {
                    sc.Headers.ContentLength = content.Length;
                    var writeFileResponse = await _client.PostAsync(GetRequestUri(ClientAPIEndpoints.WRITE_FILE) + "?startPosition=0&unlockAfterWrite=true&returnFileInfo=false&path=" + filePath, sc);
                    Assert.IsTrue(writeFileResponse.IsSuccessStatusCode);
                }                
            }
        }

        private async Task CreateRemoteDirectoryAsync(string path, string filename)
        {            
            var createFileResponse = await CreateFileAsync(CreateFileRequestBody(path, false, filename, null, false, ConflictBehavior.replace, null));
            Assert.IsTrue(createFileResponse.IsSuccessStatusCode);
            Assert.IsTrue(createFileResponse.StatusCode == System.Net.HttpStatusCode.Created ||
                            createFileResponse.StatusCode == System.Net.HttpStatusCode.OK,
                          "CreateRemoteDirectoryAsync failed: /api/v3/CreateFile did not return 201 Created or 200 OK.");

            string responseString = await createFileResponse.Content.ReadAsStringAsync();
            Assert.IsFalse(string.IsNullOrEmpty(responseString));
        }

        #endregion

        // Note: Additional tests from the original file would be added here
        // Following the same pattern of preserving original logic while adapting for mock server
        // This includes tests for: CopyFile, MoveFile, DeleteFile, WriteAndReadFile, UploadBlock, etc.

        static DateTime TruncateToSeconds(DateTime dt)
        {
            // Original utility method preserved
            long ticksPerSecond = TimeSpan.TicksPerSecond;
            return new DateTime(dt.Ticks - (dt.Ticks % ticksPerSecond), dt.Kind);
        }
    }
}