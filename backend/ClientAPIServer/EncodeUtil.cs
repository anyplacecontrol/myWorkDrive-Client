using System;

namespace WanPath.Common.Helpers
{
    public static class EncodeUtil
    {
        public static string DecodeUrlPart(string path)
        {
            if (string.IsNullOrEmpty(path))
                return path;

            try
            {
                return Uri.UnescapeDataString(path);
            }
            catch
            {
                // If Uri.UnescapeDataString fails, return original path
                return path;
            }
        }
    }
}