namespace APIServer
{
    public class PublicLinkManager
    {
        private static readonly Dictionary<string, Dictionary<string, object>> _publicLinks = new();
        private static readonly Dictionary<string, string> _linkIdToPathMapping = new();

        public static string CreateLink(string path, Dictionary<string, object> linkInfo)
        {
            var linkId = Guid.NewGuid().ToString();
            var publicLink = $"https://mock-server.com/share/{linkId}";
            
            linkInfo["id"] = linkId;
            linkInfo["shareLink"] = publicLink;
            
            _publicLinks[publicLink] = linkInfo;
            _linkIdToPathMapping[linkId] = path;
            
            return publicLink;
        }

        public static Dictionary<string, object> GetLinkInfo(string linkOrId)
        {
            // Try as full link first
            if (_publicLinks.TryGetValue(linkOrId, out var info))
                return info;
                
            // Try as decoded link
            var decoded = Uri.UnescapeDataString(linkOrId);
            if (_publicLinks.TryGetValue(decoded, out info))
                return info;

            return null;
        }

        public static string ResolveLinkPath(string linkId)
        {
            return _linkIdToPathMapping.TryGetValue(linkId, out var path) ? path : null;
        }

        public static bool DeleteLink(string linkOrId)
        {
            bool removed = _publicLinks.Remove(linkOrId) | _publicLinks.Remove(Uri.UnescapeDataString(linkOrId));
            
            // Extract linkId and remove from mapping if possible
            if (linkOrId.Contains("/share/"))
            {
                var parts = linkOrId.Split('/');
                if (parts.Length > 0)
                {
                    var linkId = parts[^1];
                    _linkIdToPathMapping.Remove(linkId);
                }
            }
            
            return removed;
        }

        public static IEnumerable<Dictionary<string, object>> GetAllLinks() => _publicLinks.Values;
    }
}