const CACHE_TTL_SECONDS = 86400;
const VIDEO_ID_PATTERN = /^[A-Za-z0-9_-]{11}$/;

function jsonResponse(payload, status, headers = {}) {
    return new Response(
        JSON.stringify(payload),
        {
            status,
            headers: {
                "Content-Type": "application/json; charset=utf-8",
                "X-Robots-Tag": "noindex, nofollow",
                ...headers
            }
        }
    );
}

function errorResponse(error, status, headers = {}) {
    return jsonResponse(
        { error },
        status,
        {
            "Cache-Control": "no-store",
            "X-React-Player-Cache": "MISS",
            ...headers
        }
    );
}

function cachedResponse(response) {
    const headers = new Headers(response.headers);
    headers.set("X-React-Player-Cache", "HIT");

    return new Response(
        response.body,
        {
            status: response.status,
            statusText: response.statusText,
            headers
        }
    );
}

export function onRequest() {
    return errorResponse(
        "method_not_allowed",
        405,
        { Allow: "GET" }
    );
}

export async function onRequestGet(context) {
    const requestUrl = new URL(context.request.url);
    const videoId = requestUrl.searchParams.get("id");

    if (!videoId || !VIDEO_ID_PATTERN.test(videoId)) {
        return errorResponse(
            "invalid_video_id",
            400
        );
    }

    const apiKey = context.env.YOUTUBE_API_KEY;

    if (typeof apiKey !== "string" || !apiKey.trim()) {
        return errorResponse(
            "youtube_api_not_configured",
            503
        );
    }

    const cacheUrl = new URL(
        "/api/youtube/video",
        requestUrl.origin
    );
    cacheUrl.searchParams.set("id", videoId);

    const cacheKey = new Request(
        cacheUrl.toString(),
        { method: "GET" }
    );
    const cache = caches.default;
    const match = await cache.match(cacheKey);

    if (match) {
        return cachedResponse(match);
    }

    const youtubeUrl = new URL(
        "https://www.googleapis.com/youtube/v3/videos"
    );
    youtubeUrl.searchParams.set("part", "contentDetails,status");
    youtubeUrl.searchParams.set("id", videoId);
    youtubeUrl.searchParams.set(
        "fields",
        "items(contentDetails(duration),status(madeForKids))"
    );

    let youtubeResponse;

    try {
        youtubeResponse = await fetch(
            youtubeUrl.toString(),
            {
                method: "GET",
                headers: {
                    "X-Goog-Api-Key": apiKey
                }
            }
        );
    }
    catch {
        console.warn(
            "[React YouTube Proxy] YouTube request failed."
        );

        return errorResponse(
            "youtube_api_failed",
            502
        );
    }

    if (!youtubeResponse.ok) {
        console.warn(
            "[React YouTube Proxy] YouTube returned HTTP " +
            youtubeResponse.status +
            "."
        );

        return errorResponse(
            "youtube_api_failed",
            502
        );
    }

    let youtubePayload;

    try {
        youtubePayload = await youtubeResponse.json();
    }
    catch {
        console.warn(
            "[React YouTube Proxy] YouTube returned invalid JSON."
        );

        return errorResponse(
            "youtube_api_failed",
            502
        );
    }

    const item =
        youtubePayload &&
        Array.isArray(youtubePayload.items) &&
        youtubePayload.items.length > 0
            ? youtubePayload.items[0]
            : null;

    const duration =
        item &&
        item.contentDetails &&
        typeof item.contentDetails.duration === "string"
            ? item.contentDetails.duration
            : "";

    const madeForKids =
        Boolean(
            item &&
            item.status &&
            item.status.madeForKids === true
        );

    const payload = item
        ? {
            items: [
                {
                    contentDetails: {
                        duration
                    },
                    status: {
                        madeForKids
                    }
                }
            ]
        }
        : { items: [] };

    const response = jsonResponse(
        payload,
        200,
        {
            "Cache-Control":
                "public, max-age=" + CACHE_TTL_SECONDS,
            "X-React-Player-Cache": "MISS"
        }
    );

    context.waitUntil(
        cache.put(
            cacheKey,
            response.clone()
        )
    );

    return response;
}
