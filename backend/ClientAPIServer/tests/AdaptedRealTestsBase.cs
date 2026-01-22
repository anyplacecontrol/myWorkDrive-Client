using APIServer;
using WanPath.Common.Helpers;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MWDMockServer;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Hosting;

namespace MockServerAPITests.AdaptedRealTests
{
    /// <summary>
    /// Adapted version of the original ApiTestsBase from real_tests
    /// Modified to work with the mock server instead of the real server
    /// </summary>
    public class AdaptedRealTestsBase
    {
        // Keeping similar structure to original ApiTestsBase but adapted for mock server
        protected Guid _sessionId;
        protected HttpClient _client;
        protected WebApplicationFactory<Program> _factory;

        public AdaptedRealTestsBase()
        {
            // Mock the original TestUser and configuration approach
            _sessionId = Guid.NewGuid(); // Mock session ID
            _factory = new WebApplicationFactory<Program>()
                .WithWebHostBuilder(builder =>
                {
                    // Set content root to the project directory to avoid looking for MWDMockServer subfolder
                    builder.UseContentRoot(Path.GetDirectoryName(Path.GetDirectoryName(Directory.GetCurrentDirectory())));
                });
            _client = CreateClient();

            // Initialize ShareManager for tests
            ShareManager.InitializeShares(Program.BASE_PATH ?? PathHelper.GetDefaultBasePath());
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
            var testPath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath(); // Use configured base path
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
        }

        private HttpClient CreateClient()
        {
            var c = _factory.CreateClient();
            c.BaseAddress = new Uri("http://localhost/");
            c.DefaultRequestHeaders.Accept.Clear();
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            c.Timeout = new TimeSpan(0, 0, 15);
            
            // Set up mock server authentication similar to original
            c.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("SessionID", _sessionId.ToString());
            
            return c;
        }

        // Mock equivalents of the original assertion methods
        // These would need to be adapted based on how the mock server file system works
        protected void AssertNotFound(string path)
        {
            // In mock server context, check if the file doesn't exist in the mock file system
            var basePath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath();
            var fullPath = PathHelper.ToSystemPath(path, basePath);
            Assert.IsFalse(File.Exists(fullPath) || Directory.Exists(fullPath), $"Path should not exist: {path}");
        }

        protected void AssertIsFile(string path)
        {
            var basePath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath();
            var fullPath = PathHelper.ToSystemPath(path, basePath);
            Assert.IsTrue(File.Exists(fullPath), $"File should exist: {path}");
            Assert.IsFalse(Directory.Exists(fullPath), $"Path should be file, not directory: {path}");
        }

        protected void AssertIsDirectory(string path)
        {
            var basePath = Program.BASE_PATH ?? PathHelper.GetDefaultBasePath();
            var fullPath = PathHelper.ToSystemPath(path, basePath);
            Assert.IsTrue(Directory.Exists(fullPath), $"Directory should exist: {path}");
            Assert.IsFalse(File.Exists(fullPath), $"Path should be directory, not file: {path}");
        }
    }
}