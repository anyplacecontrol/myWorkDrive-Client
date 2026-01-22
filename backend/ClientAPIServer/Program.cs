using APIServer;
using WanPath.Common.Helpers;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.Text;

namespace MWDMockServer
{
    public class Program
    {
        public static string BASE_PATH;
        public static int API_PORT;
        public static string BASE_API;

        static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            
            // Load platform-specific configuration
            var platformConfigFile = CrossPlatformHelper.GetPlatformSpecificConfigFile();
            builder.Configuration.AddJsonFile("appsettings.json", optional: true, reloadOnChange: true);
            builder.Configuration.AddJsonFile(platformConfigFile, optional: true, reloadOnChange: true);
            
            // Load configuration
            var configuration = builder.Configuration;
            
            // Get server configuration from appsettings.json
            var serverConfig = configuration.GetSection("ServerConfiguration");
            API_PORT = serverConfig.GetValue<int>("ApiPort", 5001);
            
            // Get default share configuration
            var defaultShare = serverConfig.GetValue<string>("DefaultShare", "Documents");
            
            // Get path format configuration ("scheme" or "share")
            var pathFormat = serverConfig.GetValue<string>("PathFormat", "scheme");
            
            // Initialize PathHelper with configuration
#if MOCK_SERVER
            PathHelper.Initialize(configuration);
#endif
            
            // Get base path from config or use platform-specific default
            var configBasePath = serverConfig.GetValue<string>("BasePath");
            if (string.IsNullOrEmpty(configBasePath))
            {
                BASE_PATH = PathHelper.GetDefaultBasePath();
            }
            else
            {
                BASE_PATH = PathHelper.ExpandPath(configBasePath);
            }
            
            // Ensure base path exists
            if (!Directory.Exists(BASE_PATH))
            {
                Directory.CreateDirectory(BASE_PATH);
            }
            
            BASE_API = $"http://localhost:{API_PORT}/api/v3/";
            
            Console.WriteLine($"Starting MWD Mock Server:");
            Console.WriteLine($"  Port: {API_PORT}");
            Console.WriteLine($"  Base Path: {BASE_PATH}");
            Console.WriteLine($"  API URL: {BASE_API}");

            builder.Services.AddControllers();
            builder.Services.AddOpenApi();
            builder.Services.AddMemoryCache();
            
            // Add CORS services
            builder.Services.AddCors(options =>
            {
                options.AddDefaultPolicy(policy =>
                {
                    policy.AllowAnyOrigin()
                          .AllowAnyMethod()
                          .AllowAnyHeader();
                });
            });

            builder.WebHost.ConfigureKestrel(options =>
            {
                options.ListenLocalhost(API_PORT);  // Use configured port
            });

            var app = builder.Build();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseHttpsRedirection();
            app.UseCors();
            app.UseAuthorization();
            app.MapControllers();

            // Set the base path and initialize the API handler
            ClientAPIHandler.BasePath = BASE_PATH;
            ClientAPIHandler.DefaultShare = defaultShare;
            ClientAPIHandler.PathFormat = pathFormat;
            ClientAPIHandler.GetInstance(); // Force initialization with correct BasePath
            
            // Initialize ShareManager with the base path
            ShareManager.InitializeShares(BASE_PATH);

            // Combine the API handlers with Controllers
            app.Use(async (context, next) =>
            {
                if (context.Request.Path.Value?.StartsWith(APIEndpoints.PATH_BASE, StringComparison.OrdinalIgnoreCase) == true)
                {
                    if (ClientAPIHandler.GetInstance().RecognizesPath(context.Request.Path))
                        await ClientAPIHandler.GetInstance().ProcessRequestAsync(context);
                    else
                        await next();
                }
                else
                    await next();
            });

            var serverTask = Task.Run(() => app.Run());

            // Run simple tests
            // Few simple tests to check
            // await RunTests();

            // Wait for the server to keep running
            await serverTask;
        }

        private static async Task RunTests()
        {
            await RunSimpleFileTest();
            await RunSimpleFolderTest();
        }

        private static void CleanUp()
        {
            try
            {
                if (!Directory.Exists(BASE_PATH))
                    return;

                foreach (var entry in Directory.GetFileSystemEntries(BASE_PATH))
                {
                    CrossPlatformHelper.ResetFileAttributes(entry);

                    if (Directory.Exists(entry))
                    {
                        Directory.Delete(entry, recursive: true);
                    }
                    else
                    {
                        File.Delete(entry);
                    }
                }
            }
            catch { }
        }

        private static async Task RunSimpleFileTest()
        {
            CleanUp();

            string fileName = "testfile.txt";

            using (var client = new HttpClient())
            {
                // Set up authentication header with SessionID
                string sessionId = Guid.NewGuid().ToString();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SessionID", sessionId);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Create file
                var createData = new
                {
                    path = "/",
                    createFile = true,
                    name = "testfile",
                    extension = "txt",
                    createContent = false,
                    conflictBehavior = "replace"
                };

                var content = new StringContent(JsonConvert.SerializeObject(createData), Encoding.UTF8, "application/json");
                var createResponse = await client.PostAsync(BASE_API + "CreateFile", content);
                
                if (!createResponse.IsSuccessStatusCode)
                    throw new Exception("RunSimpleFileTest: Create file failed");

                Console.WriteLine("RunSimpleFileTest: File created successfully");

                // Check file info/existence
                var infoResponse = await client.GetAsync(BASE_API + $"GetFileInfo?repository=myRepoId&path=/{fileName}");
                if (!infoResponse.IsSuccessStatusCode)
                    throw new Exception("RunSimpleFileTest: Get file info failed");

                var infoContent = await infoResponse.Content.ReadAsStringAsync();
                var fileInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(infoContent);
                
                if (fileInfo == null || !fileInfo.ContainsKey("name") || fileInfo["name"].ToString() != fileName)
                    throw new Exception("RunSimpleFileTest: File info validation failed");

                Console.WriteLine("RunSimpleFileTest: File info verified successfully");

                // Delete file
                var deleteResponse = await client.PostAsync(BASE_API + $"DeleteFile?repository=myRepoId&path=/{fileName}", null);
                if (deleteResponse.StatusCode != System.Net.HttpStatusCode.NoContent)
                    throw new Exception("RunSimpleFileTest: Delete file failed");

                // Verify file is deleted
                if (File.Exists(Path.Combine(BASE_PATH, fileName)))
                    throw new Exception("RunSimpleFileTest: File still exists after delete");

                Console.WriteLine("RunSimpleFileTest: File deleted successfully - TEST PASSED");
            }
        }

        private static async Task RunSimpleFolderTest()
        {
            string folderName = "testfolder";

            using (var client = new HttpClient())
            {
                // Set up authentication header with SessionID
                string sessionId = Guid.NewGuid().ToString();
                client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("SessionID", sessionId);
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));

                // Create folder
                var createData = new
                {
                    path = "/",
                    createFile = false,
                    name = folderName,
                    conflictBehavior = "replace"
                };

                var content = new StringContent(JsonConvert.SerializeObject(createData), Encoding.UTF8, "application/json");
                var createResponse = await client.PostAsync(BASE_API + "CreateFile", content);
                
                if (!createResponse.IsSuccessStatusCode)
                    throw new Exception("RunSimpleFolderTest: Create folder failed");

                Console.WriteLine("RunSimpleFolderTest: Folder created successfully");

                // Check folder info/existence
                var infoResponse = await client.GetAsync(BASE_API + $"GetFolderInfo?repository=myRepoId&path=/{folderName}");
                if (!infoResponse.IsSuccessStatusCode)
                    throw new Exception("RunSimpleFolderTest: Get folder info failed");

                var infoContent = await infoResponse.Content.ReadAsStringAsync();
                var folderInfo = JsonConvert.DeserializeObject<Dictionary<string, object>>(infoContent);
                
                if (folderInfo == null || !folderInfo.ContainsKey("name") || folderInfo["name"].ToString() != folderName)
                    throw new Exception("RunSimpleFolderTest: Folder info validation failed");

                if (folderInfo["isFolder"].ToString().ToLower() != "true")
                    throw new Exception("RunSimpleFolderTest: Item is not a folder");

                Console.WriteLine("RunSimpleFolderTest: Folder info verified successfully");

                // Delete folder
                var deleteResponse = await client.PostAsync(BASE_API + $"DeleteFolder?repository=myRepoId&path=/{folderName}", null);
                if (deleteResponse.StatusCode != System.Net.HttpStatusCode.NoContent)
                    throw new Exception("RunSimpleFolderTest: Delete folder failed");

                // Verify folder is deleted
                if (Directory.Exists(Path.Combine(BASE_PATH, folderName)))
                    throw new Exception("RunSimpleFolderTest: Folder still exists after delete");

                Console.WriteLine("RunSimpleFolderTest: Folder deleted successfully - TEST PASSED");
            }
        }



        static DateTime TruncateToSeconds(DateTime dt)
        {
            // remove the fraction of a second
            long ticksPerSecond = TimeSpan.TicksPerSecond;
            return new DateTime(dt.Ticks - (dt.Ticks % ticksPerSecond), dt.Kind);
        }
    }
}
