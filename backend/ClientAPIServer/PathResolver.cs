using System.Text.RegularExpressions;

namespace APIServer
{
    public class PathResolver
    {
        public class ParsedPath
        {
            public string ShareName { get; set; }
            public string RelativePath { get; set; }
            public PathType Type { get; set; }
            public string LinkId { get; set; }
        }

        public enum PathType
        {
            ShareBased,
            SchemeShareBased,
            LinkBased
        }

        public static ParsedPath ParsePath(string path)
        {
            if (string.IsNullOrEmpty(path))
                return null;

            // Handle link paths: :lnk:{link-id}:/path/to/file
            if (path.StartsWith(":lnk:", StringComparison.OrdinalIgnoreCase))
            {
                var linkMatch = Regex.Match(path, @"^:lnk:([^:]+):(.*)$");
                if (linkMatch.Success)
                {
                    return new ParsedPath
                    {
                        Type = PathType.LinkBased,
                        LinkId = linkMatch.Groups[1].Value,
                        RelativePath = linkMatch.Groups[2].Value
                    };
                }
            }

            // Handle scheme-based paths: :sh:share-name:/path/to/file
            if (path.StartsWith(":sh:", StringComparison.OrdinalIgnoreCase))
            {
                var schemeMatch = Regex.Match(path, @"^:sh:([^:]+):(.*)$");
                if (schemeMatch.Success)
                {
                    return new ParsedPath
                    {
                        Type = PathType.SchemeShareBased,
                        ShareName = schemeMatch.Groups[1].Value,
                        RelativePath = schemeMatch.Groups[2].Value
                    };
                }
            }

            // Handle share-based paths: /share-name/path/to/file
            if (path.StartsWith("/"))
            {
                var parts = path.Substring(1).Split('/', 2);
                if (parts.Length >= 1)
                {
                    var parsedPath = new ParsedPath
                    {
                        Type = PathType.ShareBased,
                        ShareName = parts[0],
                        RelativePath = parts.Length > 1 ? "/" + parts[1] : "/"
                    };
                    return parsedPath;
                }
            }

            return null;
        }

        public static string ToShareBasedPath(string shareName, string relativePath)
        {
            relativePath = relativePath?.TrimStart('/') ?? "";
            return $"/{shareName}/{relativePath}";
        }

        public static string ToSchemeBasedPath(string shareName, string relativePath)
        {
            return $":sh:{shareName}:{relativePath}";
        }

        public static string ToLinkBasedPath(string linkId, string relativePath)
        {
            return $":lnk:{linkId}:{relativePath}";
        }
    }
}