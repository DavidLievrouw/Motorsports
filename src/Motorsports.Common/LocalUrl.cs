namespace Motorsports.Common;

/// <summary>Guards redirects against open-redirect attacks: only a relative path on this host is local.</summary>
public static class LocalUrl
{
    /// <summary>True for a path starting with a single '/', without '//' or '/\' prefixes or control characters.</summary>
    public static bool IsLocal(string? url)
    {
        if (string.IsNullOrEmpty(url) || url[0] != '/')
        {
            return false;
        }

        if (url.Length > 1 && (url[1] == '/' || url[1] == '\\'))
        {
            return false;
        }

        foreach (var c in url)
        {
            if (char.IsControl(c) || c == '\\')
            {
                return false;
            }
        }

        return true;
    }

    public static string OrDefault(string? url, string fallback = "/") => IsLocal(url) ? url! : fallback;
}
