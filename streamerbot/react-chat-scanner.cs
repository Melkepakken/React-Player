using System;

public class CPHInline
{
    public bool Execute()
    {
        CPH.SetArgument(
            "reactLinkFound",
            false
        );

        CPH.SetArgument(
            "reactInput",
            ""
        );

        bool isInternal = false;

        CPH.TryGetArg(
            "isInternal",
            out isInternal
        );

        if (isInternal)
        {
            return true;
        }

        string message = "";

        if (!CPH.TryGetArg(
                "message",
                out message) ||
            string.IsNullOrWhiteSpace(
                message))
        {
            if (!CPH.TryGetArg(
                    "rawInput",
                    out message) ||
                string.IsNullOrWhiteSpace(
                    message))
            {
                CPH.TryGetArg(
                    "userInput",
                    out message
                );
            }
        }

        if (string.IsNullOrWhiteSpace(
                message))
        {
            return true;
        }

        string trimmed =
            message.TrimStart();

        if (trimmed.StartsWith(
                "!",
                StringComparison.Ordinal))
        {
            return true;
        }

        string link =
            FindFirstSupportedLink(
                message
            );

        if (string.IsNullOrWhiteSpace(
                link))
        {
            return true;
        }

        CPH.SetArgument(
            "reactInput",
            link
        );

        CPH.SetArgument(
            "reactLinkFound",
            true
        );

        CPH.SetArgument(
            "reactOriginType",
            "chat"
        );

        CPH.SetArgument(
            "reactOriginLabel",
            "Chat"
        );

        string platform = "";

        if (CPH.TryGetArg(
                "platform",
                out platform) &&
            !string.IsNullOrWhiteSpace(
                platform))
        {
            CPH.SetArgument(
                "reactOriginPlatform",
                platform.Trim().ToLowerInvariant()
            );
        }

        return true;
    }

    private string FindFirstSupportedLink(
        string message)
    {
        int searchFrom = 0;

        while (searchFrom < message.Length)
        {
            int httpIndex =
                message.IndexOf(
                    "http://",
                    searchFrom,
                    StringComparison.OrdinalIgnoreCase
                );

            int httpsIndex =
                message.IndexOf(
                    "https://",
                    searchFrom,
                    StringComparison.OrdinalIgnoreCase
                );

            int start =
                FirstValidIndex(
                    httpIndex,
                    httpsIndex
                );

            if (start < 0)
            {
                return "";
            }

            int end = start;

            while (
                end < message.Length &&
                !char.IsWhiteSpace(
                    message[end]
                ) &&
                message[end] != '<' &&
                message[end] != '>' &&
                message[end] != '"' &&
                message[end] != '\'')
            {
                end++;
            }

            string candidate =
                TrimTrailingPunctuation(
                    message.Substring(
                        start,
                        end - start
                    )
                );

            if (IsSupportedUrl(
                    candidate))
            {
                return candidate;
            }

            searchFrom =
                Math.Max(
                    end,
                    start + 1
                );
        }

        return "";
    }

    private int FirstValidIndex(
        int first,
        int second)
    {
        if (first < 0)
            return second;

        if (second < 0)
            return first;

        return Math.Min(
            first,
            second
        );
    }

    private string TrimTrailingPunctuation(
        string value)
    {
        int end =
            value.Length;

        while (end > 0)
        {
            char character =
                value[end - 1];

            if (
                character == '.' ||
                character == ',' ||
                character == ';' ||
                character == ':' ||
                character == '!' ||
                character == '?' ||
                character == ')' ||
                character == ']' ||
                character == '}')
            {
                end--;
                continue;
            }

            break;
        }

        return value.Substring(
            0,
            end
        );
    }

    private bool IsSupportedUrl(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return false;
        }

        int schemeEnd =
            value.IndexOf(
                "://",
                StringComparison.Ordinal
            );

        if (schemeEnd < 0)
        {
            return false;
        }

        string scheme =
            value.Substring(
                0,
                schemeEnd
            );

        if (
            !string.Equals(
                scheme,
                "http",
                StringComparison.OrdinalIgnoreCase
            ) &&
            !string.Equals(
                scheme,
                "https",
                StringComparison.OrdinalIgnoreCase
            ))
        {
            return false;
        }

        int hostStart =
            schemeEnd + 3;

        if (hostStart >= value.Length)
        {
            return false;
        }

        int hostEnd =
            value.Length;

        for (
            int i = hostStart;
            i < value.Length;
            i++
        )
        {
            char character =
                value[i];

            if (
                character == '/' ||
                character == '?' ||
                character == '#' ||
                character == ':')
            {
                hostEnd = i;
                break;
            }
        }

        if (hostEnd <= hostStart)
        {
            return false;
        }

        string host =
            value.Substring(
                hostStart,
                hostEnd - hostStart
            ).Trim().ToLowerInvariant();

        return
            IsHostOrSubdomain(
                host,
                "youtube.com"
            ) ||
            IsHostOrSubdomain(
                host,
                "youtu.be"
            ) ||
            IsHostOrSubdomain(
                host,
                "twitch.tv"
            ) ||
            IsHostOrSubdomain(
                host,
                "medal.tv"
            ) ||
            IsHostOrSubdomain(
                host,
                "tiktok.com"
            );
    }

    private bool IsHostOrSubdomain(
        string host,
        string domain)
    {
        return
            string.Equals(
                host,
                domain,
                StringComparison.OrdinalIgnoreCase
            ) ||
            host.EndsWith(
                "." + domain,
                StringComparison.OrdinalIgnoreCase
            );
    }
}
