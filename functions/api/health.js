export function onRequestGet() {
    const payload = {
        ok: true,
        service: "react-player",
        version: "v1-rc"
    };

    return new Response(
        JSON.stringify(payload, null, 2),
        {
            status: 200,
            headers: {
                "Content-Type": "application/json; charset=utf-8",
                "Cache-Control": "no-store",
                "X-Robots-Tag": "noindex, nofollow"
            }
        }
    );
}
