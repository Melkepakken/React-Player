const CACHE_TTL_SECONDS = 86400;

function jsonResponse(payload, status, headers = {}) {
    return new Response(JSON.stringify(payload), {
        status,
        headers: {
            "Content-Type": "application/json; charset=utf-8",
            "X-Robots-Tag": "noindex, nofollow",
            ...headers
        }
    });
}

function errorResponse(error, status, headers = {}) {
    return jsonResponse({ error }, status, { "Cache-Control": "no-store", ...headers });
}

export function onRequest() {
    return errorResponse("method_not_allowed", 405, { Allow: "GET" });
}

export async function onRequestGet(context) {
    const requestUrl = new URL(context.request.url);
    const playlistId = requestUrl.searchParams.get("id");
    if (!playlistId || !/^[A-Za-z0-9_-]+$/.test(playlistId) || playlistId.length > 200) {
        return errorResponse("invalid_playlist_id", 400);
    }
    const apiKey = context.env.YOUTUBE_API_KEY;
    if (typeof apiKey !== "string" || !apiKey.trim()) {
        return errorResponse("youtube_api_not_configured", 503);
    }
    const cacheUrl = new URL("/api/youtube/playlist", requestUrl.origin);
    cacheUrl.searchParams.set("id", playlistId);
    const cacheKey = new Request(cacheUrl.toString(), { method: "GET" });
    const cache = caches.default;
    const cached = await cache.match(cacheKey);
    if (cached) {
        const headers = new Headers(cached.headers);
        headers.set("X-React-Player-Cache", "HIT");
        return new Response(cached.body, { status: cached.status, headers });
    }
    const youtubeUrl = new URL("https://www.googleapis.com/youtube/v3/playlists");
    youtubeUrl.searchParams.set("part", "snippet,contentDetails");
    youtubeUrl.searchParams.set("id", playlistId);
    youtubeUrl.searchParams.set("fields", "items(id,snippet(title,channelTitle,thumbnails),contentDetails(itemCount))");
    let payload;
    try {
        const response = await fetch(youtubeUrl.toString(), {
            headers: { "X-Goog-Api-Key": apiKey }
        });
        if (!response.ok) return errorResponse("youtube_api_failed", 502);
        payload = await response.json();
    } catch {
        return errorResponse("youtube_api_failed", 502);
    }
    const item = Array.isArray(payload && payload.items) ? payload.items[0] : null;
    const response = jsonResponse({ items: item ? [item] : [] }, 200, {
        "Cache-Control": "public, max-age=" + CACHE_TTL_SECONDS,
        "X-React-Player-Cache": "MISS"
    });
    context.waitUntil(cache.put(cacheKey, response.clone()));
    return response;
}
