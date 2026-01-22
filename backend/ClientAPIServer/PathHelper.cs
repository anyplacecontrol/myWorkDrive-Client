#if !MOCK_SERVER
using Microsoft.Owin;
using System.Web;
#endif
using System;

namespace WanPath.Common.Helpers
{
    public static class PathHelper
    {
        public static string StandardizePath(string path, bool removeDav = false, bool decode = true)
        {
            if (decode)
                path = EncodeUtil.DecodeUrlPart(path);

            if (path.Contains(@"\"))
                path = path.Replace(@"\", "/");

            if (path.EndsWith("/"))
                path = path.TrimEnd('/');

            //because of iOS files provider
            if (path.StartsWith("//"))
                path = path.TrimStart('/');

            if (removeDav)
            {
                if (path.StartsWith("dav2/", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = path.Substring(4);
                }
                else if (path.StartsWith("/dav2/", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = path.Substring(5);
                }
                else if (path.StartsWith("dav/", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = path.Substring(3);
                }
                else if (path.StartsWith("/dav/", StringComparison.InvariantCultureIgnoreCase))
                {
                    path = path.Substring(4);
                }
                else // we don't need to call AddFirstSlash if the dav prefix was stripped because Substring above is called with the position that references a slash
                    path = AddFirstSlash(path);
            }

            return path;
        }

        public static bool IsLinkPath(string combinedPath, out string linkID, out string pathInLink)
        {
            string pathType;
            linkID = string.Empty;
            return SplitComplexPath(combinedPath, out pathType, out linkID, out pathInLink) && "lnk".Equals(pathType) && !string.IsNullOrEmpty(linkID);
        }

        public static bool SplitComplexPath(string combinedPath, out string pathType, out string pathID, out string path, char separator = ':')
        {
            pathType = null;
            pathID = null;
            path = null;
            if (combinedPath == null)
                return false;
            else
            if (combinedPath.Length == 0)
            {
                path = string.Empty;
                return true;
            }

            int sourceLen = combinedPath.Length;
            int curIdx = 0;
            int startIdx = 0;
            int endIdx = 0;

            // complex path
            if (combinedPath[curIdx] == separator)
            {
                startIdx = curIdx + 1;
                endIdx = 1;
                while (endIdx < sourceLen)
                {
                    if (combinedPath[endIdx] == separator)
                        break;
                    endIdx++;
                }
                // the end of pathType was not found, the string is in the wrong format
                if (endIdx == sourceLen)
                    return false;
                pathType = combinedPath.Substring(startIdx, endIdx - startIdx);

                // Pick the ID
                startIdx = endIdx + 1;
                endIdx = startIdx;

                while (endIdx < sourceLen)
                {
                    if (combinedPath[endIdx] == separator)
                        break;
                    endIdx++;
                }
                // the end of pathType was not found, the string is in the wrong format
                if (endIdx == sourceLen)
                    return false;
                pathID = combinedPath.Substring(startIdx, endIdx - startIdx);
                curIdx = endIdx + 1;
                if (curIdx == sourceLen)
                {
                    path = string.Empty;
                    return true;
                }
            }

            path = combinedPath.Substring(curIdx);
            return true;
        }
        public static string AddFirstSlash(string input)
        {
            return (input.Length > 0 && input[0] == '/') ? input : '/' + input;
            /*string output = input;
            if (!output.StartsWith("/"))
                output = "/" + output;
            return output;*/
        }

        public static string StandardizeRequestUrl(string url)
        {
            if (String.IsNullOrEmpty(url))
            {
                return url;
            }

            url = url.Replace(":8357", "");
            url = url.Replace("http://", "https://");

            return url;
        }

#if !MOCK_SERVER
        public static string GetRequestOrProxyUri(HttpRequest request, string redirectUrl)
        {
            if (request == null) { return redirectUrl; }

            var proxyUrl = GetProxyUrl(request, redirectUrl);
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                return proxyUrl;
            }
            return PathHelper.StandardizeRequestUrl(request.Url.GetLeftPart(UriPartial.Authority));
        }

        private static string GetProxyUrl(HttpRequest request, string redirectUrl)
        {
            //var forwardedPort = request.Headers["X-Forwarded-For"];
            var proxy = request.Headers["X-MS-Proxy"];

            //this means it is using proxy
            if (!string.IsNullOrEmpty(proxy))
            {
                return redirectUrl;
            }

            return null;
        }

        public static string GetRequestOrProxyUri(IOwinRequest request, string redirectUrl)
        {
            if (request == null) { return redirectUrl; }

            var proxyUrl = GetProxyUrl(request, redirectUrl);
            if (!string.IsNullOrEmpty(proxyUrl))
            {
                return proxyUrl;
            }
            return PathHelper.StandardizeRequestUrl(request.Uri.GetLeftPart(UriPartial.Authority));
        }

        private static string GetProxyUrl(IOwinRequest request, string redirectUrl)
        {
            //var forwardedPort = request.Headers["X-Forwarded-For"];
            var proxy = request.Headers["X-MS-Proxy"];

            //this means it is using proxy
            if (!string.IsNullOrEmpty(proxy))
            {
                return redirectUrl;
            }

            return null;
        }
#endif

#if MOCK_SERVER
        // Methods for mock server compatibility - from old PathHelper
        private static readonly char ApiSeparator = '/';
        
        // Configuration-based base path resolver
        private static Microsoft.Extensions.Configuration.IConfiguration _configuration;
        
        public static void Initialize(Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            _configuration = configuration;
        }
        
        public static string GetDefaultBasePath()
        {
            // Try to get from configuration first
            if (_configuration != null)
            {
                var configBasePath = _configuration["ServerConfiguration:BasePath"];
                if (!string.IsNullOrEmpty(configBasePath))
                {
                    return ExpandPath(configBasePath);
                }
            }
            
            // Fallback to platform defaults if no configuration
            if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Windows))
            {
                return @"C:\temp\mwd";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.Linux))
            {
                return "/tmp/mwd";
            }
            else if (System.Runtime.InteropServices.RuntimeInformation.IsOSPlatform(System.Runtime.InteropServices.OSPlatform.OSX))
            {
                return System.IO.Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile), "mwd");
            }
            else
            {
                return System.IO.Path.Combine(System.IO.Path.GetTempPath(), "mwd");
            }
        }
        
        public static string ExpandPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            if (path.StartsWith("~"))
            {
                string homeDirectory = System.Environment.GetFolderPath(System.Environment.SpecialFolder.UserProfile);
                if (path.Length == 1)
                    return homeDirectory;
                if (path[1] == System.IO.Path.DirectorySeparatorChar || path[1] == System.IO.Path.AltDirectorySeparatorChar)
                    return System.IO.Path.Combine(homeDirectory, path.Substring(2));
            }
            
            return path;
        }
        
        public static string NormalizeApiPath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return "/";
                
            path = path.Replace('\\', ApiSeparator);
            
            if (!path.StartsWith(ApiSeparator))
                path = ApiSeparator + path;
                
            if (path.Length > 1 && path.EndsWith(ApiSeparator))
                path = path.TrimEnd(ApiSeparator);
                
            return path;
        }
        
        public static string ToSystemPath(string apiPath, string basePath)
        {
            if (string.IsNullOrEmpty(apiPath) || apiPath == "/")
                return basePath;
                
            apiPath = apiPath.TrimStart(ApiSeparator);
            
            string[] segments = apiPath.Split(ApiSeparator);
            
            // Security check: prevent directory traversal attacks
            foreach (string segment in segments)
            {
                if (segment == ".." || segment == "." || segment.Contains(".."))
                {
                    throw new UnauthorizedAccessException("Directory traversal is not allowed in path segments");
                }
            }

            var cleanedSegments = segments.Select(s => s.TrimStart('\\', '/')).ToArray();
            string combinedPath = System.IO.Path.Combine(basePath, System.IO.Path.Combine(cleanedSegments));
            
            // Additional security check: ensure the resolved path is still under base path
            string normalizedResult = System.IO.Path.GetFullPath(combinedPath);
            string normalizedBase = System.IO.Path.GetFullPath(basePath);
            
            if (!normalizedResult.StartsWith(normalizedBase, StringComparison.OrdinalIgnoreCase))
            {
                throw new UnauthorizedAccessException("Access to paths outside the base directory is not allowed");
            }
            
            return combinedPath;
        }
        
        public static string ToApiPath(string systemPath, string basePath)
        {
            if (string.IsNullOrEmpty(systemPath))
                return "/";
                
            if (!systemPath.StartsWith(basePath, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Path '{systemPath}' is not under base path '{basePath}'");
                
            string relativePath = systemPath.Substring(basePath.Length);
            
            if (string.IsNullOrEmpty(relativePath))
                return "/";
                
            relativePath = relativePath.Replace(System.IO.Path.DirectorySeparatorChar, ApiSeparator);
            relativePath = relativePath.Replace(System.IO.Path.AltDirectorySeparatorChar, ApiSeparator);
            
            if (!relativePath.StartsWith(ApiSeparator))
                relativePath = ApiSeparator + relativePath;
                
            return relativePath;
        }
        
        public static string CombineApiPaths(params string[] paths)
        {
            if (paths == null || paths.Length == 0)
                return "/";
                
            string combined = "";
            foreach (var path in paths)
            {
                if (string.IsNullOrEmpty(path))
                    continue;
                    
                string normalized = path.Replace('\\', ApiSeparator);
                
                if (combined.Length == 0)
                {
                    combined = normalized;
                }
                else
                {
                    if (!combined.EndsWith(ApiSeparator))
                        combined += ApiSeparator;
                    combined += normalized.TrimStart(ApiSeparator);
                }
            }
            
            return NormalizeApiPath(combined);
        }
        
        public static string EnsureDirectorySeparator(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;
                
            if (!path.EndsWith(System.IO.Path.DirectorySeparatorChar.ToString()) && 
                !path.EndsWith(System.IO.Path.AltDirectorySeparatorChar.ToString()))
            {
                return path + System.IO.Path.DirectorySeparatorChar;
            }
            
            return path;
        }
#endif
    }
}
