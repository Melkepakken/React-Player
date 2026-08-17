using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    public class MediaRequest
    {
        public string Id { get; set; }
        public string User { get; set; }
        public string UserId { get; set; }

        public string OriginType { get; set; }
        public string OriginPlatform { get; set; }
        public string OriginLabel { get; set; }

        public string Provider { get; set; }
        public string MediaType { get; set; }

        public string OriginalUrl { get; set; }
        public string Url { get; set; }
        public string ContentId { get; set; }

        public string Title { get; set; }
        public string Author { get; set; }
        public string Creator { get; set; }
        public string Category { get; set; }
        public string ThumbnailUrl { get; set; }
        public int DurationSeconds { get; set; }

        public string AddedAt { get; set; }
    }

    public class TikTokOEmbedResponse
    {
        public string title { get; set; }
        public string author_name { get; set; }
        public string author_url { get; set; }
        public string thumbnail_url { get; set; }
        public string provider_name { get; set; }
    }

    public class MedalContentResponse
    {
        public string contentTitle { get; set; }
        public string contentDescription { get; set; }
        public double videoLengthSeconds { get; set; }
        public string thumbnailUrl { get; set; }
        public MedalPoster poster { get; set; }
    }

    public class MedalPoster
    {
        public string displayName { get; set; }
        public string userId { get; set; }
    }

    public class TwitchClipsResponse
    {
        public List<TwitchClipData> data { get; set; }
    }

    public class TwitchClipData
    {
        public string id { get; set; }
        public string url { get; set; }
        public string embed_url { get; set; }
        public string broadcaster_name { get; set; }
        public string creator_name { get; set; }
        public string game_id { get; set; }
        public string title { get; set; }
        public string thumbnail_url { get; set; }
        public double duration { get; set; }
        public int view_count { get; set; }
        public string created_at { get; set; }
    }

    public class TwitchGamesResponse
    {
        public List<TwitchGameData> data { get; set; }
    }

    public class TwitchGameData
    {
        public string id { get; set; }
        public string name { get; set; }
    }

    public class YouTubeDurationResponse
    {
        public List<YouTubeDurationItem> items { get; set; }
    }

    public class YouTubeDurationItem
    {
        public YouTubeContentDetails contentDetails { get; set; }
    }

    public class YouTubeContentDetails
    {
        public string duration { get; set; }
    }

    public bool Execute()
    {
        // Metadata enrichment is best-effort. The request is queued first.
        CPH.SetArgument("reactMetadataPending", false);
        CPH.SetArgument("reactRequestId", "");
        CPH.SetArgument("reactMetadataUrl", "");

        CPH.SetArgument("reactDurationPending", false);
        CPH.SetArgument("reactDurationUrl", "");

        CPH.SetArgument("reactTwitchMetadataPending", false);
        CPH.SetArgument("reactTwitchMetadataUrl", "");

        CPH.SetArgument("reactMedalMetadataPending", false);
        CPH.SetArgument("reactMedalMetadataUrl", "");

        CPH.SetArgument("reactTikTokMetadataPending", false);
        CPH.SetArgument("reactTikTokMetadataUrl", "");

        CPH.SetArgument("reactTikTokResolvePending", false);
        CPH.SetArgument("reactTikTokResolveUrl", "");

        CPH.SetArgument("reactTwitchCategoryPending", false);
        CPH.SetArgument("reactTwitchCategoryUrl", "");

        CPH.SetArgument("reactTwitchAuthorization", "");
        CPH.SetArgument("reactTwitchClientId", "");

        CPH.SetArgument("reactSubmitSuccess", false);
        CPH.SetArgument("reactSubmitErrorCode", "");
        CPH.SetArgument("reactSubmitError", "");

        bool manualSubmit = false;
        CPH.TryGetArg(
            "reactManualSubmit",
            out manualSubmit
        );

        string user = GetDefaultUserName();
        string userId = "";

        if (!CPH.TryGetArg("user", out user) ||
            string.IsNullOrWhiteSpace(user))
        {
            if (!CPH.TryGetArg("userName", out user) ||
                string.IsNullOrWhiteSpace(user))
            {
                user = GetDefaultUserName();
            }
        }

        CPH.TryGetArg("userId", out userId);

        string input = "";

        // Adapters can pass a clean URL through reactInput.
        // Other triggers can provide rawInput, userInput, or message.
        // Text around the first supported media link is allowed.
        if (!TryGetRequestInput(
                out input))
        {
            input = "";
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            if (manualSubmit)
            {
                SetManualSubmitError(
                    "missing_link",
                    "Lim inn en YouTube-, Twitch Clip-, Medal- eller TikTok-lenke."
                );
            }
            else
            {
                SendInvalidMessage(user);
            }

            return false;
        }

        input = input.Trim();

        string extractedInput =
            ExtractFirstSupportedRequestUrl(
                input
            );

        if (string.IsNullOrWhiteSpace(
                extractedInput))
        {
            if (manualSubmit)
            {
                SetManualSubmitError(
                    "invalid_link",
                    "Du må sende inn en gyldig YouTube-, Twitch Clip-, Medal- eller TikTok-lenke."
                );
            }
            else
            {
                SendInvalidMessage(user);
            }

            CPH.LogInfo(
                "[React Queue] No supported media URL found in input: " +
                input
            );

            return false;
        }

        input = extractedInput;

        CPH.SetArgument(
            "reactInput",
            input
        );

        if (IsTikTokShortLink(input))
        {
            string shortUrl =
                NormalizeUrl(
                    input
                );

            CPH.SetArgument(
                "reactTikTokResolveUrl",
                shortUrl
            );

            CPH.SetArgument(
                "reactTikTokResolvePending",
                true
            );

            CPH.LogInfo(
                "[React TikTok Resolve] Expanding short link: " +
                shortUrl
            );

            return true;
        }

        string provider;
        string mediaType;
        string normalizedUrl;
        string contentId;

        if (!TryParseMedia(
            input,
            out provider,
            out mediaType,
            out normalizedUrl,
            out contentId))
        {
            if (manualSubmit)
            {
                SetManualSubmitError(
                    "invalid_link",
                    "Du må sende inn en gyldig YouTube-, Twitch Clip-, Medal- eller TikTok-lenke."
                );
            }
            else
            {
                SendInvalidMessage(user);
            }

            CPH.LogInfo(
                "[React Queue] Unsupported media URL: " + input
            );

            return false;
        }

        return QueueParsedRequest(
            input,
            user,
            userId,
            provider,
            mediaType,
            normalizedUrl,
            contentId
        );
    }

    public bool ResolveTikTokShortlink()
    {
        bool manualSubmit = false;
        CPH.TryGetArg(
            "reactManualSubmit",
            out manualSubmit
        );

        string user = GetDefaultUserName();
        string userId = "";

        if (!CPH.TryGetArg("user", out user) ||
            string.IsNullOrWhiteSpace(user))
        {
            if (!CPH.TryGetArg("userName", out user) ||
                string.IsNullOrWhiteSpace(user))
            {
                user = GetDefaultUserName();
            }
        }

        CPH.TryGetArg(
            "userId",
            out userId
        );

        string originalInput = "";

        TryGetRequestInput(
            out originalInput
        );

        originalInput =
            (originalInput ?? "").Trim();

        if (!CPH.TryGetArg(
                "reactTikTokResolveRaw",
                out string rawHtml) ||
            string.IsNullOrWhiteSpace(rawHtml))
        {
            return TikTokResolveFailed(
                user,
                manualSubmit,
                "tiktok_short_open_failed",
                "TikTok-kortlenken kunne ikke åpnes."
            );
        }

        string canonicalUrl =
            ExtractTikTokCanonicalUrlFromHtml(
                rawHtml
            );

        if (string.IsNullOrWhiteSpace(
                canonicalUrl))
        {
            return TikTokResolveFailed(
                user,
                manualSubmit,
                "tiktok_short_resolve_failed",
                "TikTok-kortlenken kunne ikke løses til en video."
            );
        }

        string provider;
        string mediaType;
        string normalizedUrl;
        string contentId;

        if (!TryParseMedia(
                canonicalUrl,
                out provider,
                out mediaType,
                out normalizedUrl,
                out contentId) ||
            provider != "tiktok")
        {
            return TikTokResolveFailed(
                user,
                manualSubmit,
                "tiktok_short_unsupported",
                "TikTok-kortlenken pekte ikke til en støttet video."
            );
        }

        CPH.LogInfo(
            "[React TikTok Resolve] " +
            originalInput +
            " -> " +
            normalizedUrl
        );

        return QueueParsedRequest(
            originalInput,
            user,
            userId,
            provider,
            mediaType,
            normalizedUrl,
            contentId
        );
    }

    private void SetManualSubmitError(
        string errorCode,
        string message)
    {
        CPH.SetArgument(
            "reactSubmitErrorCode",
            errorCode ?? ""
        );

        CPH.SetArgument(
            "reactSubmitError",
            message ?? ""
        );
    }

    private bool TikTokResolveFailed(
        string user,
        bool manualSubmit,
        string errorCode,
        string message)
    {
        CPH.SetArgument(
            "reactSubmitSuccess",
            false
        );

        if (manualSubmit)
        {
            SetManualSubmitError(
                errorCode,
                message
            );
        }
        else
        {
            string chatMessage =
                "@" +
                user +
                " " +
                GetChatFailurePrefix() +
                TranslateChatError(message);

            SendPlatformChatMessage(
                chatMessage
            );
        }

        CPH.LogWarn(
            "[React TikTok Resolve] " +
            message
        );

        return false;
    }

    private string ExtractTikTokCanonicalUrlFromHtml(
        string rawHtml)
    {
        if (string.IsNullOrWhiteSpace(
                rawHtml))
        {
            return "";
        }

        string html =
            rawHtml
                .Replace("\\u002F", "/")
                .Replace("\\u002f", "/")
                .Replace("\\/", "/")
                .Replace("&amp;", "&")
                .Replace("&#38;", "&")
                .Replace("&quot;", "\"");

        string[] prefixes =
        {
            "https://www.tiktok.com/@",
            "https://tiktok.com/@"
        };

        foreach (string prefix in prefixes)
        {
            int searchFrom = 0;

            while (searchFrom < html.Length)
            {
                int start =
                    html.IndexOf(
                        prefix,
                        searchFrom,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (start < 0)
                    break;

                int videoMarker =
                    html.IndexOf(
                        "/video/",
                        start,
                        StringComparison.OrdinalIgnoreCase
                    );

                if (videoMarker > start &&
                    videoMarker - start < 250)
                {
                    int idStart =
                        videoMarker +
                        "/video/".Length;

                    int idEnd =
                        idStart;

                    while (
                        idEnd < html.Length &&
                        char.IsDigit(
                            html[idEnd]
                        )
                    )
                    {
                        idEnd++;
                    }

                    if (idEnd > idStart)
                    {
                        string candidate =
                            html.Substring(
                                start,
                                idEnd - start
                            );

                        return StripQueryAndFragment(
                            candidate
                        );
                    }
                }

                searchFrom =
                    start + prefix.Length;
            }
        }

        return "";
    }

    private bool TryGetRequestInput(
        out string input)
    {
        input = "";

        if (CPH.TryGetArg(
                "reactInput",
                out input) &&
            !string.IsNullOrWhiteSpace(
                input))
        {
            return true;
        }

        if (CPH.TryGetArg(
                "rawInput",
                out input) &&
            !string.IsNullOrWhiteSpace(
                input))
        {
            return true;
        }

        if (CPH.TryGetArg(
                "userInput",
                out input) &&
            !string.IsNullOrWhiteSpace(
                input))
        {
            return true;
        }

        if (CPH.TryGetArg(
                "message",
                out input) &&
            !string.IsNullOrWhiteSpace(
                input))
        {
            return true;
        }

        input = "";
        return false;
    }

    private string ExtractFirstSupportedRequestUrl(
        string text)
    {
        string input =
            (text ?? "").Trim();

        if (string.IsNullOrWhiteSpace(input))
        {
            return "";
        }

        string wholeCandidate =
            CleanRequestUrlToken(
                input
            );

        if (IsSupportedRequestUrl(
                wholeCandidate))
        {
            return wholeCandidate;
        }

        int index = 0;

        while (index < input.Length)
        {
            while (
                index < input.Length &&
                char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            if (index >= input.Length)
            {
                break;
            }

            int start = index;

            while (
                index < input.Length &&
                !char.IsWhiteSpace(input[index]))
            {
                index++;
            }

            string candidate =
                CleanRequestUrlToken(
                    input.Substring(
                        start,
                        index - start
                    )
                );

            if (IsSupportedRequestUrl(candidate))
            {
                return candidate;
            }
        }

        return "";
    }

    private string CleanRequestUrlToken(
        string value)
    {
        string token =
            (value ?? "").Trim();

        while (
            token.Length > 0 &&
            IsLeadingUrlWrapper(
                token[0]))
        {
            token =
                token.Substring(1);
        }

        while (
            token.Length > 0 &&
            IsTrailingUrlPunctuation(
                token[token.Length - 1]))
        {
            token =
                token.Substring(
                    0,
                    token.Length - 1
                );
        }

        return token.Trim();
    }

    private bool IsLeadingUrlWrapper(
        char value)
    {
        return
            value == '(' ||
            value == '[' ||
            value == '{' ||
            value == '<' ||
            value == '"' ||
            value == '\'';
    }

    private bool IsTrailingUrlPunctuation(
        char value)
    {
        return
            value == ')' ||
            value == ']' ||
            value == '}' ||
            value == '>' ||
            value == '"' ||
            value == '\'' ||
            value == '.' ||
            value == ',' ||
            value == '!' ||
            value == '?' ||
            value == ';' ||
            value == ':';
    }

    private bool IsSupportedRequestUrl(
        string candidate)
    {
        if (string.IsNullOrWhiteSpace(
                candidate))
        {
            return false;
        }

        if (IsTikTokShortLink(candidate))
        {
            return true;
        }

        string provider;
        string mediaType;
        string normalizedUrl;
        string contentId;

        return TryParseMedia(
            candidate,
            out provider,
            out mediaType,
            out normalizedUrl,
            out contentId
        );
    }

    private bool QueueParsedRequest(
        string originalInput,
        string user,
        string userId,
        string provider,
        string mediaType,
        string normalizedUrl,
        string contentId)
    {
        List<MediaRequest> queue = LoadQueue();

        string originType = "";
        string originPlatform = "";
        string originLabel = "";

        CPH.TryGetArg(
            "reactOriginType",
            out originType
        );

        CPH.TryGetArg(
            "reactOriginPlatform",
            out originPlatform
        );

        CPH.TryGetArg(
            "reactOriginLabel",
            out originLabel
        );

        MediaRequest request = new MediaRequest
        {
            Id = Guid.NewGuid().ToString("N"),

            User = user,
            UserId = userId,

            OriginType =
                (originType ?? "").Trim(),
            OriginPlatform =
                (originPlatform ?? "").Trim(),
            OriginLabel =
                (originLabel ?? "").Trim(),

            Provider = provider,
            MediaType = mediaType,

            OriginalUrl = originalInput,
            Url = normalizedUrl,
            ContentId = contentId,

            Title =
                provider == "twitch"
                    ? "Twitch Clip"
                    : provider == "medal"
                        ? "Medal Clip"
                        : provider == "tiktok"
                            ? "TikTok"
                            : "",
            Author = "",
            Creator = "",
            Category =
                provider == "medal"
                    ? ExtractMedalCategoryName(originalInput)
                    : "",
            ThumbnailUrl =
                provider == "youtube"
                    ? "https://i.ytimg.com/vi/" + contentId + "/hqdefault.jpg"
                    : "",
            DurationSeconds = 0,

            AddedAt = DateTime.UtcNow.ToString("o")
        };

        queue.Add(request);

        SaveQueue(queue);

        CPH.SetArgument(
            "reactRequestId",
            request.Id
        );

        CPH.SetArgument(
            "reactSubmitSuccess",
            true
        );

        if (provider == "tiktok")
        {
            CPH.SetArgument(
                "reactTikTokMetadataUrl",
                "https://www.tiktok.com/oembed?url=" +
                normalizedUrl
            );

            CPH.SetArgument(
                "reactTikTokMetadataPending",
                true
            );
        }

        if (provider == "medal")
        {
            CPH.SetArgument(
                "reactMedalMetadataUrl",
                "https://medal.tv/api/content/" +
                contentId
            );

            CPH.SetArgument(
                "reactMedalMetadataPending",
                true
            );
        }

        if (provider == "twitch")
        {
            try
            {
                string twitchToken =
                    CPH.TwitchOAuthToken;

                string twitchClientId =
                    CPH.TwitchClientId;

                if (!string.IsNullOrWhiteSpace(twitchToken) &&
                    !string.IsNullOrWhiteSpace(twitchClientId))
                {
                    CPH.SetArgument(
                        "reactTwitchAuthorization",
                        "Bearer " + twitchToken
                    );

                    CPH.SetArgument(
                        "reactTwitchClientId",
                        twitchClientId
                    );

                    CPH.SetArgument(
                        "reactTwitchMetadataUrl",
                        "https://api.twitch.tv/helix/clips?id=" +
                        contentId
                    );

                    CPH.SetArgument(
                        "reactTwitchMetadataPending",
                        true
                    );
                }
                else
                {
                    CPH.LogWarn(
                        "[React Twitch Metadata] Twitch is not authenticated in Streamer.bot. Keeping fallback metadata."
                    );
                }
            }
            catch (Exception ex)
            {
                CPH.LogWarn(
                    "[React Twitch Metadata] Could not prepare Twitch authentication: " +
                    ex.Message
                );
            }
        }

        if (provider == "youtube")
        {
            // The video ID has already been extracted from the submitted URL,
            // so we can safely build the encoded YouTube oEmbed target directly.
            // Build the encoded oEmbed target directly for Streamer.bot compatibility.
            string metadataUrl =
                "https://www.youtube.com/oembed?url=" +
                "https%3A%2F%2Fwww.youtube.com%2Fwatch%3Fv%3D" +
                contentId +
                "&format=json";

            CPH.SetArgument(
                "reactMetadataUrl",
                metadataUrl
            );

            CPH.SetArgument(
                "reactMetadataPending",
                true
            );

            string durationUrl =
                "https://react.melkepakken.tv/api/youtube/video" +
                "?id=" + contentId;

            CPH.SetArgument(
                "reactDurationUrl",
                durationUrl
            );

            CPH.SetArgument(
                "reactDurationPending",
                true
            );
        }

        CPH.LogInfo(
            "[React Queue] Added " +
            provider +
            "/" +
            mediaType +
            " request from " +
            user +
            ": " +
            normalizedUrl +
            " | Queue size: " +
            queue.Count
        );

        return true;
    }

    public bool ApplyTikTokMetadata()
    {
        try
        {
            if (!CPH.TryGetArg(
                    "reactRequestId",
                    out string requestId) ||
                string.IsNullOrWhiteSpace(requestId))
            {
                CPH.LogWarn(
                    "[React TikTok Metadata] Missing reactRequestId."
                );

                return true;
            }

            if (!CPH.TryGetArg(
                    "reactTikTokMetaRaw",
                    out string rawJson) ||
                string.IsNullOrWhiteSpace(rawJson))
            {
                CPH.LogWarn(
                    "[React TikTok Metadata] Empty TikTok oEmbed response for request " +
                    requestId
                );

                return true;
            }

            TikTokOEmbedResponse response =
                JsonConvert.DeserializeObject<TikTokOEmbedResponse>(
                    rawJson
                );

            if (response == null)
            {
                CPH.LogWarn(
                    "[React TikTok Metadata] Could not parse TikTok response for request " +
                    requestId
                );

                return true;
            }

            List<MediaRequest> queue =
                LoadQueue();

            MediaRequest request =
                FindRequestInQueue(
                    queue,
                    requestId
                );

            if (request == null)
            {
                CPH.LogWarn(
                    "[React TikTok Metadata] Request no longer exists in queue: " +
                    requestId
                );

                return true;
            }

            if (!string.IsNullOrWhiteSpace(
                    response.title))
            {
                string cleanTitle =
                    CleanTikTokTitle(
                        response.title
                    );

                request.Title =
                    string.IsNullOrWhiteSpace(
                        cleanTitle)
                        ? "TikTok"
                        : cleanTitle;
            }

            if (!string.IsNullOrWhiteSpace(
                    response.author_name))
            {
                request.Author =
                    response.author_name.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                    response.thumbnail_url))
            {
                request.ThumbnailUrl =
                    response.thumbnail_url.Trim();
            }

            SaveQueue(queue);

            CPH.LogInfo(
                "[React TikTok Metadata] Enriched request " +
                requestId +
                ": " +
                (request.Title ?? "") +
                " | author=" +
                (request.Author ?? "")
            );

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogWarn(
                "[React TikTok Metadata] Could not apply metadata: " +
                ex.Message
            );

            return true;
        }
    }

    public bool ApplyMedalMetadata()
    {
        try
        {
            if (!CPH.TryGetArg(
                    "reactRequestId",
                    out string requestId) ||
                string.IsNullOrWhiteSpace(requestId))
            {
                CPH.LogWarn(
                    "[React Medal Metadata] Missing reactRequestId."
                );

                return true;
            }

            if (!CPH.TryGetArg(
                    "reactMedalMetaRaw",
                    out string rawJson) ||
                string.IsNullOrWhiteSpace(rawJson))
            {
                CPH.LogWarn(
                    "[React Medal Metadata] Empty Medal response for request " +
                    requestId
                );

                return true;
            }

            MedalContentResponse response =
                JsonConvert.DeserializeObject<MedalContentResponse>(
                    rawJson
                );

            if (response == null)
            {
                CPH.LogWarn(
                    "[React Medal Metadata] Could not parse Medal response for request " +
                    requestId
                );

                return true;
            }

            List<MediaRequest> queue =
                LoadQueue();

            MediaRequest request =
                FindRequestInQueue(
                    queue,
                    requestId
                );

            if (request == null)
            {
                CPH.LogWarn(
                    "[React Medal Metadata] Request no longer exists in queue: " +
                    requestId
                );

                return true;
            }

            if (!string.IsNullOrWhiteSpace(
                    response.contentTitle))
            {
                request.Title =
                    response.contentTitle.Trim();
            }

            if (response.poster != null &&
                !string.IsNullOrWhiteSpace(
                    response.poster.displayName))
            {
                request.Author =
                    response.poster.displayName.Trim();
            }

            if (!string.IsNullOrWhiteSpace(
                    response.thumbnailUrl))
            {
                request.ThumbnailUrl =
                    response.thumbnailUrl.Trim();
            }

            if (response.videoLengthSeconds > 0)
            {
                request.DurationSeconds =
                    Math.Max(
                        1,
                        (int)Math.Round(
                            response.videoLengthSeconds
                        )
                    );
            }

            SaveQueue(queue);

            CPH.LogInfo(
                "[React Medal Metadata] Enriched request " +
                requestId +
                ": " +
                (request.Title ?? "") +
                " | uploader=" +
                (request.Author ?? "") +
                " | " +
                request.DurationSeconds +
                "s"
            );

            return true;
        }
        catch (Exception ex)
        {
            // Medal metadata is intentionally best-effort.
            // A valid queued request must survive even if Medal changes
            // or temporarily refuses its public content response.
            CPH.LogWarn(
                "[React Medal Metadata] Could not apply metadata: " +
                ex.Message
            );

            return true;
        }
    }

    public bool ApplyTwitchMetadata()
    {
        bool keepAuthForCategory = false;

        try
        {
            if (!CPH.TryGetArg(
                    "reactRequestId",
                    out string requestId) ||
                string.IsNullOrWhiteSpace(requestId))
            {
                CPH.LogWarn(
                    "[React Twitch Metadata] Missing reactRequestId."
                );

                return true;
            }

            if (!CPH.TryGetArg(
                    "reactTwitchMetaRaw",
                    out string rawJson) ||
                string.IsNullOrWhiteSpace(rawJson))
            {
                CPH.LogWarn(
                    "[React Twitch Metadata] Empty Twitch API response for request " +
                    requestId
                );

                return true;
            }

            TwitchClipsResponse response =
                JsonConvert.DeserializeObject<TwitchClipsResponse>(
                    rawJson
                );

            if (response == null ||
                response.data == null ||
                response.data.Count == 0 ||
                response.data[0] == null)
            {
                CPH.LogWarn(
                    "[React Twitch Metadata] Twitch returned no clip data for request " +
                    requestId
                );

                return true;
            }

            TwitchClipData clip =
                response.data[0];

            List<MediaRequest> queue =
                LoadQueue();

            MediaRequest request =
                FindRequestInQueue(
                    queue,
                    requestId
                );

            if (request == null)
            {
                CPH.LogWarn(
                    "[React Twitch Metadata] Request no longer exists in queue: " +
                    requestId
                );

                return true;
            }

            if (!string.IsNullOrWhiteSpace(clip.title))
                request.Title = clip.title.Trim();

            if (!string.IsNullOrWhiteSpace(clip.broadcaster_name))
                request.Author = clip.broadcaster_name.Trim();

            if (!string.IsNullOrWhiteSpace(clip.creator_name))
                request.Creator = clip.creator_name.Trim();

            if (!string.IsNullOrWhiteSpace(clip.thumbnail_url))
                request.ThumbnailUrl = clip.thumbnail_url.Trim();

            if (clip.duration > 0)
            {
                request.DurationSeconds =
                    Math.Max(
                        1,
                        (int)Math.Round(clip.duration)
                    );
            }

            if (!string.IsNullOrWhiteSpace(clip.url))
                request.Url = clip.url.Trim();

            if (!string.IsNullOrWhiteSpace(clip.game_id))
            {
                CPH.SetArgument(
                    "reactTwitchCategoryUrl",
                    "https://api.twitch.tv/helix/games?id=" +
                    clip.game_id.Trim()
                );

                CPH.SetArgument(
                    "reactTwitchCategoryPending",
                    true
                );

                keepAuthForCategory = true;
            }

            SaveQueue(queue);

            CPH.LogInfo(
                "[React Twitch Metadata] Enriched request " +
                requestId +
                ": " +
                (request.Title ?? "") +
                " | broadcaster=" +
                (request.Author ?? "") +
                " | creator=" +
                (request.Creator ?? "") +
                " | " +
                request.DurationSeconds +
                "s"
            );

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogWarn(
                "[React Twitch Metadata] Could not apply metadata: " +
                ex.Message
            );

            return true;
        }
        finally
        {
            if (!keepAuthForCategory)
                ClearTwitchAuthArguments();
        }
    }

    public bool ApplyTwitchCategoryMetadata()
    {
        try
        {
            if (!CPH.TryGetArg(
                    "reactRequestId",
                    out string requestId) ||
                string.IsNullOrWhiteSpace(requestId))
            {
                CPH.LogWarn(
                    "[React Twitch Category] Missing reactRequestId."
                );

                return true;
            }

            if (!CPH.TryGetArg(
                    "reactTwitchCategoryRaw",
                    out string rawJson) ||
                string.IsNullOrWhiteSpace(rawJson))
            {
                CPH.LogWarn(
                    "[React Twitch Category] Empty Twitch Games response for request " +
                    requestId
                );

                return true;
            }

            TwitchGamesResponse response =
                JsonConvert.DeserializeObject<TwitchGamesResponse>(
                    rawJson
                );

            if (response == null ||
                response.data == null ||
                response.data.Count == 0 ||
                response.data[0] == null ||
                string.IsNullOrWhiteSpace(response.data[0].name))
            {
                CPH.LogWarn(
                    "[React Twitch Category] Twitch returned no category name for request " +
                    requestId
                );

                return true;
            }

            List<MediaRequest> queue =
                LoadQueue();

            MediaRequest request =
                FindRequestInQueue(
                    queue,
                    requestId
                );

            if (request == null)
            {
                CPH.LogWarn(
                    "[React Twitch Category] Request no longer exists in queue: " +
                    requestId
                );

                return true;
            }

            request.Category =
                response.data[0].name.Trim();

            SaveQueue(queue);

            CPH.LogInfo(
                "[React Twitch Category] " +
                requestId +
                " = " +
                request.Category
            );

            return true;
        }
        catch (Exception ex)
        {
            CPH.LogWarn(
                "[React Twitch Category] Could not apply category metadata: " +
                ex.Message
            );

            return true;
        }
        finally
        {
            ClearTwitchAuthArguments();
        }
    }

    private void ClearTwitchAuthArguments()
    {
        CPH.SetArgument(
            "reactTwitchAuthorization",
            ""
        );

        CPH.SetArgument(
            "reactTwitchClientId",
            ""
        );
    }

    public bool ApplyMetadata()
    {
        if (!CPH.TryGetArg(
                "reactRequestId",
                out string requestId) ||
            string.IsNullOrWhiteSpace(requestId))
        {
            CPH.LogWarn(
                "[React Metadata] Missing reactRequestId."
            );

            return true;
        }

        string title = "";
        string author = "";
        string thumbnailUrl = "";

        CPH.TryGetArg(
            "reactMeta.title",
            out title
        );

        CPH.TryGetArg(
            "reactMeta.author_name",
            out author
        );

        CPH.TryGetArg(
            "reactMeta.thumbnail_url",
            out thumbnailUrl
        );

        if (string.IsNullOrWhiteSpace(title) &&
            string.IsNullOrWhiteSpace(author) &&
            string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            CPH.LogWarn(
                "[React Metadata] No metadata returned for request " +
                requestId +
                ". Keeping fallback data."
            );

            return true;
        }

        List<MediaRequest> queue = LoadQueue();

        MediaRequest request = FindRequestInQueue(
            queue,
            requestId
        );

        if (request == null)
        {
            CPH.LogWarn(
                "[React Metadata] Request no longer exists in queue: " +
                requestId
            );

            return true;
        }

        if (!string.IsNullOrWhiteSpace(title))
            request.Title = title.Trim();

        if (!string.IsNullOrWhiteSpace(author))
            request.Author = author.Trim();

        if (!string.IsNullOrWhiteSpace(thumbnailUrl))
            request.ThumbnailUrl = thumbnailUrl.Trim();

        SaveQueue(queue);

        CPH.LogInfo(
            "[React Metadata] Enriched request " +
            requestId +
            ": " +
            (request.Title ?? "")
        );

        return true;
    }

    public bool ApplyDuration()
    {
        if (!CPH.TryGetArg(
                "reactRequestId",
                out string requestId) ||
            string.IsNullOrWhiteSpace(requestId))
        {
            CPH.LogWarn(
                "[React Duration] Missing reactRequestId."
            );

            return true;
        }

        if (!CPH.TryGetArg(
                "reactDurationRaw",
                out string rawJson) ||
            string.IsNullOrWhiteSpace(rawJson))
        {
            CPH.LogWarn(
                "[React Duration] Empty YouTube API response for request " +
                requestId
            );

            return true;
        }

        string isoDuration = "";

        try
        {
            YouTubeDurationResponse response =
                JsonConvert.DeserializeObject<YouTubeDurationResponse>(
                    rawJson
                );

            if (response != null &&
                response.items != null &&
                response.items.Count > 0 &&
                response.items[0] != null &&
                response.items[0].contentDetails != null)
            {
                isoDuration =
                    response.items[0].contentDetails.duration ?? "";
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn(
                "[React Duration] Could not parse YouTube response: " +
                ex.Message
            );

            return true;
        }

        int durationSeconds =
            ParseIso8601DurationSeconds(
                isoDuration
            );

        if (durationSeconds <= 0)
        {
            CPH.LogWarn(
                "[React Duration] No usable duration returned for request " +
                requestId
            );

            return true;
        }

        List<MediaRequest> queue = LoadQueue();

        MediaRequest request = FindRequestInQueue(
            queue,
            requestId
        );

        if (request == null)
        {
            CPH.LogWarn(
                "[React Duration] Request no longer exists in queue: " +
                requestId
            );

            return true;
        }

        request.DurationSeconds = durationSeconds;

        SaveQueue(queue);

        CPH.LogInfo(
            "[React Duration] " +
            requestId +
            " = " +
            durationSeconds +
            " seconds"
        );

        return true;
    }

    private int ParseIso8601DurationSeconds(
        string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return 0;

        string number = "";
        bool inTimeSection = false;
        double totalSeconds = 0;

        for (int i = 0; i < value.Length; i++)
        {
            char character = value[i];

            if ((character >= '0' && character <= '9') ||
                character == '.')
            {
                number += character;
                continue;
            }

            if (character == 'P')
                continue;

            if (character == 'T')
            {
                inTimeSection = true;
                number = "";
                continue;
            }

            if (string.IsNullOrWhiteSpace(number))
                continue;

            double amount = 0;

            if (!double.TryParse(
                    number,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out amount))
            {
                number = "";
                continue;
            }

            if (character == 'D')
            {
                totalSeconds += amount * 86400;
            }
            else if (character == 'H')
            {
                totalSeconds += amount * 3600;
            }
            else if (character == 'M' &&
                     inTimeSection)
            {
                totalSeconds += amount * 60;
            }
            else if (character == 'S')
            {
                totalSeconds += amount;
            }

            number = "";
        }

        if (totalSeconds <= 0)
            return 0;

        if (totalSeconds >= int.MaxValue)
            return int.MaxValue;

        return (int)Math.Round(totalSeconds);
    }

    private MediaRequest FindRequestInQueue(
        List<MediaRequest> queue,
        string requestId)
    {
        if (queue == null ||
            string.IsNullOrWhiteSpace(requestId))
        {
            return null;
        }

        for (int i = 0; i < queue.Count; i++)
        {
            MediaRequest request = queue[i];

            if (request != null &&
                request.Id == requestId)
            {
                return request;
            }
        }

        return null;
    }

    private string NormalizeUrl(
        string input)
    {
        string url =
            (input ?? "").Trim();

        if (!url.StartsWith(
                "http://",
                StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith(
                "https://",
                StringComparison.OrdinalIgnoreCase))
        {
            url =
                "https://" + url;
        }

        return url;
    }

    private bool IsTikTokShortLink(
        string input)
    {
        string lower =
            NormalizeUrl(
                input
            ).ToLowerInvariant();

        return
            lower.StartsWith(
                "https://vm.tiktok.com/") ||
            lower.StartsWith(
                "http://vm.tiktok.com/") ||
            lower.StartsWith(
                "https://vt.tiktok.com/") ||
            lower.StartsWith(
                "http://vt.tiktok.com/") ||
            lower.StartsWith(
                "https://www.tiktok.com/t/") ||
            lower.StartsWith(
                "http://www.tiktok.com/t/") ||
            lower.StartsWith(
                "https://tiktok.com/t/") ||
            lower.StartsWith(
                "http://tiktok.com/t/");
    }

    private bool TryParseMedia(
        string input,
        out string provider,
        out string mediaType,
        out string normalizedUrl,
        out string contentId)
    {
        provider = "";
        mediaType = "";
        normalizedUrl = "";
        contentId = "";

        string url =
            NormalizeUrl(
                input
            );

        string lower =
            url.ToLowerInvariant();

        bool isTikTok =
            lower.StartsWith("https://www.tiktok.com/") ||
            lower.StartsWith("http://www.tiktok.com/") ||
            lower.StartsWith("https://tiktok.com/") ||
            lower.StartsWith("http://tiktok.com/");

        if (isTikTok &&
            lower.Contains("/video/"))
        {
            string tikTokId =
                ExtractPathId(
                    url,
                    lower,
                    "/video/"
                );

            if (!string.IsNullOrWhiteSpace(
                    tikTokId))
            {
                provider = "tiktok";
                mediaType = "video";
                contentId = tikTokId;

                normalizedUrl =
                    BuildTikTokCanonicalUrl(
                        url
                    );

                return true;
            }
        }

        bool isMedal =
            lower.StartsWith("https://medal.tv/") ||
            lower.StartsWith("https://www.medal.tv/") ||
            lower.StartsWith("http://medal.tv/") ||
            lower.StartsWith("http://www.medal.tv/");

        if (isMedal)
        {
            string medalClipId =
                ExtractMedalClipId(
                    url,
                    lower
                );

            if (!string.IsNullOrWhiteSpace(
                    medalClipId))
            {
                provider = "medal";
                mediaType = "clip";
                contentId = medalClipId;

                normalizedUrl =
                    "https://medal.tv/clips/" +
                    medalClipId;

                return true;
            }
        }

        bool isTwitch =
            lower.StartsWith("https://clips.twitch.tv/") ||
            lower.StartsWith("http://clips.twitch.tv/") ||
            lower.StartsWith("https://www.twitch.tv/") ||
            lower.StartsWith("http://www.twitch.tv/") ||
            lower.StartsWith("https://twitch.tv/") ||
            lower.StartsWith("http://twitch.tv/") ||
            lower.StartsWith("https://m.twitch.tv/") ||
            lower.StartsWith("http://m.twitch.tv/");

        if (isTwitch)
        {
            string twitchClipId = "";

            if (lower.Contains("clips.twitch.tv/"))
            {
                twitchClipId = ExtractPathId(
                    url,
                    lower,
                    "clips.twitch.tv/"
                );
            }
            else if (lower.Contains("/clip/"))
            {
                twitchClipId = ExtractPathId(
                    url,
                    lower,
                    "/clip/"
                );
            }

            if (!string.IsNullOrWhiteSpace(twitchClipId) &&
                !twitchClipId.Equals(
                    "embed",
                    StringComparison.OrdinalIgnoreCase))
            {
                provider = "twitch";
                mediaType = "clip";
                contentId = twitchClipId;

                normalizedUrl =
                    "https://clips.twitch.tv/" +
                    twitchClipId;

                return true;
            }
        }

        bool isYouTube =
            lower.StartsWith("https://youtube.com/") ||
            lower.StartsWith("https://www.youtube.com/") ||
            lower.StartsWith("https://m.youtube.com/") ||
            lower.StartsWith("https://music.youtube.com/") ||
            lower.StartsWith("https://youtu.be/") ||
            lower.StartsWith("http://youtube.com/") ||
            lower.StartsWith("http://www.youtube.com/") ||
            lower.StartsWith("http://m.youtube.com/") ||
            lower.StartsWith("http://music.youtube.com/") ||
            lower.StartsWith("http://youtu.be/");

        if (!isYouTube)
        {
            // Future providers go here:
            //
            // TikTok
            // Spotify
            // etc.
            return false;
        }

        provider = "youtube";
        normalizedUrl = url;

        if (lower.Contains("youtube.com/shorts/"))
        {
            mediaType = "short";

            contentId = ExtractPathId(
                url,
                lower,
                "youtube.com/shorts/"
            );

            return !string.IsNullOrWhiteSpace(contentId);
        }

        if (lower.Contains("youtu.be/"))
        {
            mediaType = "video";

            contentId = ExtractPathId(
                url,
                lower,
                "youtu.be/"
            );

            return !string.IsNullOrWhiteSpace(contentId);
        }

        if (lower.Contains("youtube.com/live/"))
        {
            mediaType = "video";

            contentId = ExtractPathId(
                url,
                lower,
                "youtube.com/live/"
            );

            return !string.IsNullOrWhiteSpace(contentId);
        }

        if (lower.Contains("youtube.com/embed/"))
        {
            mediaType = "video";

            contentId = ExtractPathId(
                url,
                lower,
                "youtube.com/embed/"
            );

            return !string.IsNullOrWhiteSpace(contentId);
        }

        if (lower.Contains("youtube.com/watch"))
        {
            mediaType = "video";
            contentId = ExtractQueryValue(url, "v");

            return !string.IsNullOrWhiteSpace(contentId);
        }

        return false;
    }

    private string BuildTikTokCanonicalUrl(
        string url)
    {
        string clean =
            StripQueryAndFragment(
                url
            );

        if (string.IsNullOrWhiteSpace(
                clean))
        {
            return "";
        }

        int schemeIndex =
            clean.IndexOf("://");

        if (schemeIndex >= 0)
        {
            clean =
                clean.Substring(
                    schemeIndex + 3
                );
        }

        int pathIndex =
            clean.IndexOf('/');

        if (pathIndex < 0)
        {
            return
                StripQueryAndFragment(
                    url
                );
        }

        string path =
            clean.Substring(
                pathIndex
            );

        return
            "https://www.tiktok.com" +
            path;
    }

    private string CleanTikTokTitle(
        string value)
    {
        if (string.IsNullOrWhiteSpace(
                value))
        {
            return "";
        }

        string[] parts =
            value.Split(
                new char[]
                {
                    ' ',
                    '\t',
                    '\r',
                    '\n'
                },
                StringSplitOptions.RemoveEmptyEntries
            );

        List<string> keptParts =
            new List<string>();

        for (
            int i = 0;
            i < parts.Length;
            i++
        )
        {
            string part =
                parts[i];

            if (string.IsNullOrWhiteSpace(
                    part))
            {
                continue;
            }

            if (part.StartsWith("#"))
            {
                continue;
            }

            keptParts.Add(
                part
            );
        }

        return string.Join(
            " ",
            keptParts.ToArray()
        ).Trim();
    }

    private string StripQueryAndFragment(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        int end =
            url.Length;

        int queryIndex =
            url.IndexOf('?');

        if (queryIndex >= 0 &&
            queryIndex < end)
        {
            end = queryIndex;
        }

        int fragmentIndex =
            url.IndexOf('#');

        if (fragmentIndex >= 0 &&
            fragmentIndex < end)
        {
            end = fragmentIndex;
        }

        return url.Substring(
            0,
            end
        );
    }

    private string ExtractMedalClipId(
        string original,
        string lower)
    {
        string[] markers =
        {
            "/clips/",
            "/clip/"
        };

        foreach (string marker in markers)
        {
            int index =
                lower.IndexOf(marker);

            if (index < 0)
                continue;

            int start =
                index + marker.Length;

            if (start >= original.Length)
                continue;

            string remainder =
                original.Substring(start);

            int end =
                remainder.Length;

            char[] stopCharacters =
            {
                '/',
                '?',
                '&',
                '#'
            };

            foreach (char stopCharacter in
                stopCharacters)
            {
                int position =
                    remainder.IndexOf(
                        stopCharacter
                    );

                if (position >= 0 &&
                    position < end)
                {
                    end = position;
                }
            }

            if (end <= 0)
                continue;

            string candidate =
                remainder.Substring(
                    0,
                    end
                );

            if (!candidate.Equals(
                    "embed",
                    StringComparison.OrdinalIgnoreCase))
            {
                return candidate;
            }
        }

        return "";
    }

    private string ExtractMedalCategoryName(
        string url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return "";

        string lower =
            url.ToLowerInvariant();

        string marker =
            "/games/";

        int markerIndex =
            lower.IndexOf(marker);

        if (markerIndex < 0)
            return "";

        int start =
            markerIndex + marker.Length;

        if (start >= url.Length)
            return "";

        string remainder =
            url.Substring(start);

        int slash =
            remainder.IndexOf('/');

        if (slash <= 0)
            return "";

        string slug =
            remainder.Substring(
                0,
                slash
            );

        if (string.IsNullOrWhiteSpace(slug))
            return "";

        string[] parts =
            slug.Split(
                new char[] { '-', '_' },
                StringSplitOptions.RemoveEmptyEntries
            );

        for (int i = 0; i < parts.Length; i++)
        {
            string part =
                parts[i];

            if (string.IsNullOrWhiteSpace(part))
                continue;

            if (part.Length == 1)
            {
                parts[i] =
                    part.ToUpperInvariant();
            }
            else
            {
                parts[i] =
                    char.ToUpperInvariant(
                        part[0]
                    ) +
                    part.Substring(1);
            }
        }

        return string.Join(
            " ",
            parts
        );
    }

    private string ExtractPathId(
        string original,
        string lower,
        string marker)
    {
        int index = lower.IndexOf(marker);

        if (index < 0)
            return "";

        int start = index + marker.Length;

        if (start >= original.Length)
            return "";

        string remainder = original.Substring(start);

        int end = remainder.Length;

        char[] stopCharacters =
        {
            '/',
            '?',
            '&',
            '#'
        };

        foreach (char stopCharacter in stopCharacters)
        {
            int position = remainder.IndexOf(stopCharacter);

            if (position >= 0 && position < end)
                end = position;
        }

        if (end <= 0)
            return "";

        return remainder.Substring(0, end);
    }

    private string ExtractQueryValue(
        string url,
        string key)
    {
        int questionMark = url.IndexOf('?');

        if (questionMark < 0 ||
            questionMark >= url.Length - 1)
        {
            return "";
        }

        string query = url.Substring(questionMark + 1);

        string[] parts = query.Split('&');

        foreach (string part in parts)
        {
            string[] pair = part.Split(
                new char[] { '=' },
                2
            );

            if (pair.Length != 2)
                continue;

            if (pair[0].Equals(
                key,
                StringComparison.OrdinalIgnoreCase))
            {
                return pair[1];
            }
        }

        return "";
    }

    private void SendInvalidMessage(string user)
    {
        string detail =
            IsNorwegianChatLanguage()
                ? "du må sende inn en gyldig YouTube-, Twitch Clip-, Medal- eller TikTok-lenke."
                : "submit a valid YouTube, Twitch Clip, Medal, or TikTok link.";

        SendPlatformChatMessage(
            "@" +
            user +
            " " +
            GetChatFailurePrefix() +
            detail
        );
    }

    private string GetChatLanguage()
    {
        string language = "";

        CPH.TryGetArg(
            "reactChatLanguage",
            out language
        );

        language =
            (language ?? "")
                .Trim()
                .ToLowerInvariant();

        if (language == "no" ||
            language == "nb" ||
            language == "nn")
        {
            return "no";
        }

        return "en";
    }

    private bool IsNorwegianChatLanguage()
    {
        return GetChatLanguage() == "no";
    }

    private string GetDefaultUserName()
    {
        return IsNorwegianChatLanguage()
            ? "Noen"
            : "Someone";
    }

    private string GetChatFailurePrefix()
    {
        return IsNorwegianChatLanguage()
            ? "React-request feilet - "
            : "React request failed - ";
    }

    private string TranslateChatError(
        string message)
    {
        if (IsNorwegianChatLanguage())
        {
            return message ?? "";
        }

        string value =
            (message ?? "").Trim();

        if (value ==
            "TikTok-kortlenken kunne ikke åpnes.")
        {
            return
                "The TikTok short link could not be opened.";
        }

        if (value ==
            "TikTok-kortlenken kunne ikke løses til en video.")
        {
            return
                "The TikTok short link could not be resolved to a video.";
        }

        if (value ==
            "TikTok-kortlenken pekte ikke til en støttet video.")
        {
            return
                "The TikTok short link did not point to a supported video.";
        }

        return value;
    }

    private void SendPlatformChatMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        string source = "";

        if (!CPH.TryGetArg(
                "commandSource",
                out source) ||
            string.IsNullOrWhiteSpace(source))
        {
            CPH.TryGetArg(
                "platform",
                out source
            );
        }

        source =
            (source ?? "")
                .Trim()
                .ToLowerInvariant();

        if (source.Contains("youtube"))
        {
            CPH.SendYouTubeMessageToLatestMonitored(
                message,
                true,
                true
            );

            return;
        }

        if (source.Contains("kick"))
        {
            CPH.SendKickMessage(
                message,
                true,
                true
            );

            return;
        }

        CPH.SendMessage(
            message,
            true,
            true
        );
    }

    private List<MediaRequest> LoadQueue()
    {
        string json =
            CPH.GetGlobalVar<string>(
                "ReactQueue",
                true
            );

        if (string.IsNullOrWhiteSpace(json))
            return new List<MediaRequest>();

        try
        {
            return
                JsonConvert.DeserializeObject<List<MediaRequest>>(
                    json
                )
                ?? new List<MediaRequest>();
        }
        catch
        {
            CPH.LogWarn(
                "[React Queue] Could not parse existing queue."
            );

            return new List<MediaRequest>();
        }
    }

    private void SaveQueue(
        List<MediaRequest> queue)
    {
        string json =
            JsonConvert.SerializeObject(queue);

        CPH.SetGlobalVar(
            "ReactQueue",
            json,
            true
        );
    }
}