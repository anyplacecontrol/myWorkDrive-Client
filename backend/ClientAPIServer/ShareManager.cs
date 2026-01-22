using WanPath.Common.Helpers;

namespace APIServer
{
    public class ShareManager
    {
        public class ShareInfo
        {
            public string ShareName { get; set; }
            public string DriveLetter { get; set; }
            public bool DownloadEnabled { get; set; }
            public bool DesktopClientEnabled { get; set; }
            public bool WebClientEnabled { get; set; }
            public string PhysicalPath { get; set; }
        }

        private static Dictionary<string, ShareInfo> _shares = new Dictionary<string, ShareInfo>(StringComparer.OrdinalIgnoreCase);
        
        private static bool _initialized = false;
        private static readonly object _initLock = new object();

        public static Dictionary<string, ShareInfo> GetAllShares() => _shares;

        public static ShareInfo GetShare(string shareName) => 
            _shares.TryGetValue(shareName, out var share) ? share : null;

        public static bool ShareExists(string shareName) => _shares.ContainsKey(shareName);

        public static void InitializeShares(string basePath = null)
        {
            lock (_initLock)
            {
                // For tests, allow re-initialization
                // if (_initialized) return;
                
                if (string.IsNullOrEmpty(basePath))
                    basePath = PathHelper.GetDefaultBasePath();
                    
                var sharesPath = Path.Combine(basePath, "shares");
                
                _shares = new Dictionary<string, ShareInfo>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "Documents",
                        new ShareInfo
                        {
                            ShareName = "Documents",
                            DriveLetter = "D:",
                            DownloadEnabled = true,
                            DesktopClientEnabled = true,
                            WebClientEnabled = true,
                            PhysicalPath = Path.Combine(sharesPath, "Documents")
                        }
                    },
                    {
                        "Pictures",
                        new ShareInfo
                        {
                            ShareName = "Pictures", 
                            DriveLetter = "P:",
                            DownloadEnabled = true,
                            DesktopClientEnabled = true,
                            WebClientEnabled = true,
                            PhysicalPath = Path.Combine(sharesPath, "Pictures")
                        }
                    },
                    {
                        "Projects",
                        new ShareInfo
                        {
                            ShareName = "Projects",
                            DriveLetter = "R:",
                            DownloadEnabled = true,
                            DesktopClientEnabled = true,
                            WebClientEnabled = true,
                            PhysicalPath = Path.Combine(sharesPath, "Projects")
                        }
                    }
                };
                
                foreach (var share in _shares.Values)
                {
                    Directory.CreateDirectory(share.PhysicalPath);
                }
                
                
                _initialized = true;
            }
        }
    }
}