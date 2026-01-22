using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MWDMockServer;
using System.Net.Http.Headers;
using System.Text;

namespace MockServerAPITests
{
    [TestClass]
    public class DebugTest
    {
        protected HttpClient _client;
        protected WebApplicationFactory<Program> _factory;

        [TestInitialize]
        public void Initialize()
        {
            _factory = new WebApplicationFactory<Program>();
            _client = _factory.CreateClient();
            _client.BaseAddress = new Uri("http://localhost/");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.Timeout = new TimeSpan(0, 0, 15);
            
            // Set up mock server authentication
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", Guid.NewGuid().ToString());
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
        }

        [TestMethod]
        public async Task TestSimpleCreateFile()
        {
            // Test basic request to /api/v3/CreateFile
            var requestData = new
            {
                path = "/",
                createFile = true,
                name = "testfile",
                extension = "txt",
                createContent = false,
                conflictBehavior = "replace"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var response = await _client.PostAsync("/api/v3/CreateFile", content);

            Console.WriteLine($"Status Code: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}");

            // Just assert that we get some kind of response (not necessarily success)
            Assert.IsNotNull(response);
        }
        
        [TestMethod]
        public async Task TestPathRecognition()
        {
            // Test if the middleware recognizes the path
            var response = await _client.GetAsync("/api/v3/CreateFile");
            
            Console.WriteLine($"Status Code: {response.StatusCode}");
            var responseContent = await response.Content.ReadAsStringAsync();
            Console.WriteLine($"Response Content: {responseContent}");

            // Should not be 404 if path is recognized
            Assert.AreNotEqual(System.Net.HttpStatusCode.NotFound, response.StatusCode);
        }

        [TestMethod]
        public async Task TestGetItemType()
        {
            // First create a file
            var requestData = new
            {
                path = "/",
                createFile = true,
                name = "testfile",
                extension = "txt",
                createContent = false,
                conflictBehavior = "replace"
            };

            var json = System.Text.Json.JsonSerializer.Serialize(requestData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var createResponse = await _client.PostAsync("/api/v3/CreateFile", content);
            Console.WriteLine($"Create Status: {createResponse.StatusCode}");
            var createResponseContent = await createResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"Create Response: {createResponseContent}");

            // Now test GetItemType
            var getItemResponse = await _client.GetAsync("/api/v3/GetItemType?path=/testfile.txt");
            Console.WriteLine($"GetItem Status: {getItemResponse.StatusCode}");
            var getItemContent = await getItemResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"GetItem Response: {getItemContent}");

            Assert.IsTrue(getItemResponse.IsSuccessStatusCode);
        }
    }
}