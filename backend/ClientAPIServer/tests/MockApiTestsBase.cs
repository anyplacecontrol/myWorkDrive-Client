using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MWDMockServer;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using WanPath.Common.Helpers;
using APIServer;

namespace MockServerAPITests
{
    public class MockApiTestsBase
    {
        protected HttpClient _client;
        protected WebApplicationFactory<Program> _factory;
        protected string _sessionId;
        protected string BaseApiUrl => "http://localhost/api/v3/";

        [TestInitialize]
        public void Initialize()
        {
            // Create WebApplicationFactory with proper configuration for tests
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Set content root to the project directory to avoid looking for MWDMockServer subfolder
                    builder.UseContentRoot(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())));

                    builder.ConfigureAppConfiguration((context, config) =>
                    {
                        // Load platform-specific configuration for tests
                        var platformConfigFile = CrossPlatformHelper.GetPlatformSpecificConfigFile();
                        config.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
                        config.AddJsonFile(platformConfigFile, optional: true, reloadOnChange: true);
                        
                        // Override with test-specific settings - use platform appropriate temp path
                        var testBasePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                            ? @"C:\temp\mwd"
                            : "/tmp/mwd";
                            
                        config.AddInMemoryCollection(new Dictionary<string, string>
                        {
                            ["ServerConfiguration:BasePath"] = testBasePath,
                            ["ServerConfiguration:ApiPort"] = "5001",
                            ["ServerConfiguration:PathFormat"] = "scheme"
                        });
                    });
                    
                    builder.ConfigureServices(services =>
                    {
                        // Additional test-specific service configuration if needed
                    });
                });
                
            _client = _factory.CreateClient();
            _client.BaseAddress = new Uri("http://localhost/");
            _client.DefaultRequestHeaders.Accept.Clear();
            _client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            _client.Timeout = TimeSpan.FromSeconds(30);

            // Set up mock session ID for authorization
            _sessionId = Guid.NewGuid().ToString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", _sessionId);

            // Initialize PathHelper and ShareManager for tests
            var testBasePath = RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                ? @"C:\temp\mwd"
                : "/tmp/mwd";
            PathHelper.Initialize(_factory.Services.GetRequiredService<IConfiguration>());
            ShareManager.InitializeShares(testBasePath);

            // Clean up test directory before tests
            CleanUpTestDirectory();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _client?.Dispose();
            _factory?.Dispose();
            CleanUpTestDirectory();
        }

        protected void CleanUpTestDirectory()
        {
            var testPath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath();
            if (Directory.Exists(testPath))
            {
                try
                {
                    foreach (var entry in Directory.GetFileSystemEntries(testPath))
                    {
                        CrossPlatformHelper.ResetFileAttributes(entry);
                        if (Directory.Exists(entry))
                            Directory.Delete(entry, recursive: true);
                        else
                            File.Delete(entry);
                    }
                }
                catch { /* Ignore cleanup errors */ }
            }
            else
            {
                Directory.CreateDirectory(testPath);
            }
            
            // Ensure shares directories exist for new path format tests
            EnsureShareDirectories();
        }
        
        protected void EnsureShareDirectories()
        {
            var basePath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath();
            var shares = new[] { "Documents", "Pictures", "Projects" };
            foreach (var share in shares)
            {
                var sharePath = Path.Combine(basePath, "shares", share);
                if (!Directory.Exists(sharePath))
                {
                    Directory.CreateDirectory(sharePath);
                }
            }
        }

        protected StringContent CreateJsonContent(object data)
        {
            var json = JsonConvert.SerializeObject(data);
            return new StringContent(json, Encoding.UTF8, "application/json");
        }

        protected string UrlEncode(string value) => Uri.EscapeDataString(value);

        protected async Task<T?> DeserializeResponse<T>(HttpResponseMessage response)
        {
            var content = await response.Content.ReadAsStringAsync();
            return JsonConvert.DeserializeObject<T>(content);
        }

        protected void AssertSuccessStatusCode(HttpResponseMessage response, string? context = null)
        {
            Assert.IsTrue(response.IsSuccessStatusCode, 
                $"{context ?? "Request"} failed. Status: {response.StatusCode}, Reason: {response.ReasonPhrase}");
        }

        protected static DateTime TruncateToSeconds(DateTime dt)
        {
            long ticksPerSecond = TimeSpan.TicksPerSecond;
            return new DateTime(dt.Ticks - (dt.Ticks % ticksPerSecond), dt.Kind);
        }

        // Helper method to generate unique test file names
        protected string GenerateTestFileName(string prefix = "test", string extension = "txt")
        {
            return $"{prefix}-{Guid.NewGuid():N}.{extension}";
        }

        // Helper method to generate unique test folder names
        protected string GenerateTestFolderName(string prefix = "folder")
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}