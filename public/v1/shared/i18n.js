(function (global) {
    "use strict";

    const DEFAULT_LANGUAGE = "en";
    const STORAGE_KEY = "reactPlayerLanguage";
    const LOCALE_ROOT = "/v1/locales/";

    // English lives here as well as in en.json so a missing or blocked locale
    // request can never leave the dock or player without usable copy.
    const ENGLISH_FALLBACK = Object.freeze({
        "language.selectorLabel": "Language",
        "creator.madeWithBy": "Made with ♥ by",
        "creator.twitchTitle": "Melkepakken on Twitch",
        "creator.githubLabel": "Melkepakken on GitHub",

        "connection.connecting": "Connecting to Streamer.bot...",
        "connection.connected": "Connected",
        "connection.disconnected": "Not connected",

        "settings.title": "Settings",
        "settings.open": "Open settings",
        "settings.close": "Close settings",

        "obsSetup.title": "OBS setup",
        "obsSetup.unknown": "React Player has not checked the OBS setup yet.",
        "obsSetup.detecting": "Looking for the React Player Browser Source in OBS...",
        "obsSetup.ready": "React Player found this Browser Source in OBS.",
        "obsSetup.notFound": "React Player could not find an OBS Browser Source using the player URL.",
        "obsSetup.notPlaced": "The Browser Source exists, but it is not placed in an OBS scene or group.",
        "obsSetup.ambiguous": "More than one placement was found. Choose the scene or group this dock should control.",
        "obsSetup.error": "React Player could not check the OBS setup. Make sure OBS is connected to Streamer.bot, then try again.",
        "obsSetup.source": "Source",
        "obsSetup.scene": "Scene",
        "obsSetup.group": "Group",
        "obsSetup.detectAgain": "Detect again",
        "obsSetup.useSelected": "Use selected",
        "obsSetup.open": "Open OBS setup",
        "obsSetup.close": "Close OBS setup",
        "obsSetup.requiredUrl": "Required player URL",
        "obsSetup.selectTarget": "Choose an OBS target",

        "playbackPreferences.title": "Playback preferences",
        "playbackPreferences.description": "These settings are optional. Choose how each video service should handle captions, quality, volume and other player settings. Leave everything unchanged if it already works the way you want.",
        "playbackPreferences.provider": "Video service",
        "playbackPreferences.media": "Media",
        "playbackPreferences.noMedia": "Add a link from this service to the queue first.",
        "playbackPreferences.stopCurrent": "Stop the current video before opening playback preferences.",
        "playbackPreferences.instructions.youtube": "Choose your preferred captions, quality and other player settings.",
        "playbackPreferences.instructions.twitch": "Choose your preferred volume and quality. Unmute the clip if needed.",
        "playbackPreferences.instructions.medal": "If a cookie message appears, make your preferred choice. Then choose the volume and quality you want.",
        "playbackPreferences.instructions.tiktok": "Check that video and sound work the way you want.",
        "playbackPreferences.openPlayer": "Open test player",
        "playbackPreferences.openControls": "Open player controls",
        "playbackPreferences.closePlayer": "Close test player",

        "diagnostics.title": "Cannot connect to Streamer.bot",
        "diagnostics.intro": "React Player could not connect to Streamer.bot on this computer. Check the following:",
        "diagnostics.running": "Streamer.bot is running.",
        "diagnostics.websocketEnabled": "Servers/Clients > WebSocket Server is enabled.",
        "diagnostics.defaults": "Use the default address 127.0.0.1, port 8080, and endpoint /.",
        "diagnostics.localNetwork": "Allow Local Network Access for react.melkepakken.tv in Chrome or Edge.",
        "diagnostics.blockers": "Disable uBlock Origin or other content blockers for react.melkepakken.tv if they block the local WebSocket.",
        "diagnostics.obs": "Open the dock inside OBS, which is the primary supported runtime.",
        "diagnostics.retry": "Retry",
        "diagnostics.dismiss": "Hide",

        "current.nowPlaying": "Now playing",
        "current.nothingLoaded": "Nothing loaded",

        "state.empty": "Empty",
        "state.playing": "Playing",
        "state.paused": "Paused",
        "state.loading": "Loading",
        "state.ended": "Finished",
        "state.error": "Error",

        "playback.backTen": "-10s",
        "playback.forwardTen": "+10s",
        "playback.backTenLabel": "Go back 10 seconds",
        "playback.forwardTenLabel": "Go forward 10 seconds",
        "playback.playPause": "Play or pause",
        "playback.videoPosition": "Video position",
        "playback.volume": "Volume",

        "controls.showOnStream": "Show on stream",
        "controls.onStream": "On stream",
        "controls.audioOnly": "Audio only",
        "controls.showVideo": "Show video",
        "controls.fullscreen": "Fullscreen",
        "controls.closeFullscreen": "Close fullscreen",
        "controls.openUrl": "Open URL",
        "controls.copyUrl": "Copy URL",
        "controls.skip": "Skip",
        "controls.stop": "Stop",
        "controls.mute": "Mute",
        "controls.unmute": "Unmute",
        "controls.resume": "Resume",
        "controls.pause": "Pause",
        "controls.refreshPlayer": "Refresh player",
        "controls.refreshTitle": "Reload the React Player source",
        "controls.audioOnlyUnsupportedTwitch": "Twitch Clips must stay visible for reliable playback. Audio-only mode will resume automatically when the next supported request starts.",
        "controls.audioOnlyActive": "Video is hidden. Audio and playback continue.",
        "controls.audioOnlyInactive": "Hide the video while keeping audio and playback running.",
        "controls.pauseUnsupported": "This media type uses its own built-in player and cannot be paused remotely by React Player.",
        "controls.muteUnsupported": "Mute for this media type is controlled by its built-in player or OBS.",
        "controls.volumeUnsupported": "Volume for this media type is controlled by its built-in player or OBS.",

        "manual.placeholder": "Paste a YouTube, Twitch Clip, Medal, or TikTok link...",
        "manual.add": "Add",
        "manual.added": "Added to the queue.",
        "manual.addFailed": "The link could not be added.",
        "manual.pasteSupported": "Paste a YouTube, Twitch Clip, Medal, or TikTok link.",
        "manual.adding": "Adding...",
        "manual.validating": "Checking the link...",
        "manual.inputLabel": "Media link",
        "manual.noResponse": "React Player did not respond.",
        "manual.sendFailed": "Could not send the request to Streamer.bot.",
        "manual.onlyLink": "Submit only the link.",
        "manual.invalidLink": "Submit a valid YouTube, Twitch Clip, Medal, or TikTok link.",
        "manual.tiktokShortOpenFailed": "The TikTok short link could not be opened.",
        "manual.tiktokShortResolveFailed": "The TikTok short link could not be resolved to a video.",
        "manual.tiktokShortUnsupported": "The TikTok short link did not point to a supported video.",

        "tabs.queue": "Queue",
        "tabs.history": "History",
        "autoplay.label": "Autoplay",
        "autoplay.ariaLabel": "Autoplay next request",
        "autoplay.initialTitle": "Automatically play the next request when the current item ends",
        "autoplay.enabledTitle": "Autoplay is on - the next request starts automatically",
        "autoplay.disabledTitle": "Autoplay is off - click to turn it on",

        "queue.empty": "No React requests in the queue",
        "history.empty": "No history yet",
        "queue.clear": "Clear queue",
        "history.clear": "Clear history",

        "request.unknownUser": "Unknown user",
        "request.unknownMedia": "Unknown media",
        "request.showLess": "Show less",
        "request.showMore": "Show more",
        "request.clippedBy": "clipped by {creator}",
        "request.submittedBy": "submitted by {user}",
        "request.playing": "Playing",
        "request.moveUp": "Move up",
        "request.moveDown": "Move down",
        "request.playAgain": "Play again",
        "request.play": "Play",
        "request.done": "Done",
        "request.remove": "Remove",

        "copy.copied": "Copied!",
        "copy.failed": "Could not copy",

        "duration.short": "Short video - up to 10 min",
        "duration.medium": "Medium-length video - 10 to 30 min",
        "duration.long": "Long video - 30 to 60 min",
        "duration.veryLong": "Very long video - over 60 min",

        "confirm.clearQueue": "Are you sure you want to clear the React queue? The currently playing item will be kept.",
        "confirm.clearHistory": "Are you sure you want to clear the entire React history?",

        "player.controls": "Video controls",
        "player.playPause": "Play or pause",
        "player.videoPosition": "Video position",
        "player.error.invalidTikTok": "Invalid TikTok video.",
        "player.error.invalidMedal": "Invalid Medal clip.",
        "player.error.invalidTwitch": "Invalid Twitch Clip.",
        "player.error.unsupportedMedia": "This media type is not supported by React Player yet."
    });

    const localeCache = {
        en: ENGLISH_FALLBACK
    };

    const loadedLocales = new Set();
    const loadingLocales = new Map();
    const listeners = new Set();

    function normalizeSelection(value) {
        const language = String(value || "").trim().toLowerCase();

        if (language === "en" || language.startsWith("en-")) {
            return "en";
        }

        if (language === "no" || language.startsWith("no-")) {
            return "no";
        }

        return "";
    }

    function browserLanguage() {
        const candidates = [];

        if (global.navigator) {
            if (Array.isArray(global.navigator.languages)) {
                candidates.push(...global.navigator.languages);
            }

            if (global.navigator.language) {
                candidates.push(global.navigator.language);
            }
        }

        for (const candidate of candidates) {
            const base = String(candidate || "")
                .trim()
                .toLowerCase()
                .split("-")[0];

            if (base === "no" || base === "nb" || base === "nn") {
                return "no";
            }

            if (base === "en") {
                return "en";
            }
        }

        return DEFAULT_LANGUAGE;
    }

    function readStoredLanguage() {
        try {
            return normalizeSelection(
                global.localStorage.getItem(STORAGE_KEY)
            );
        } catch (error) {
            return "";
        }
    }

    function queryLanguage() {
        try {
            const params = new URLSearchParams(global.location.search || "");

            if (!params.has("lang")) {
                return null;
            }

            return normalizeSelection(params.get("lang")) || DEFAULT_LANGUAGE;
        } catch (error) {
            return null;
        }
    }

    function resolveLanguage() {
        const explicitLanguage = queryLanguage();

        if (explicitLanguage) {
            return explicitLanguage;
        }

        return readStoredLanguage() || browserLanguage() || DEFAULT_LANGUAGE;
    }

    let currentLanguage = resolveLanguage();

    function updateDocumentLanguage() {
        if (global.document && global.document.documentElement) {
            global.document.documentElement.lang = currentLanguage;
        }
    }

    function replaceVariables(value, variables) {
        return String(value).replace(
            /\{([a-zA-Z0-9_]+)\}/g,
            (match, name) => {
                if (
                    variables &&
                    Object.prototype.hasOwnProperty.call(variables, name)
                ) {
                    return String(variables[name]);
                }

                return match;
            }
        );
    }

    function t(key, variables) {
        const translations = localeCache[currentLanguage] || {};
        const value = typeof translations[key] === "string"
            ? translations[key]
            : ENGLISH_FALLBACK[key];

        return replaceVariables(
            typeof value === "string" ? value : key,
            variables
        );
    }

    function notify() {
        listeners.forEach((listener) => {
            try {
                listener(currentLanguage);
            } catch (error) {
                console.error("React Player localization listener failed:", error);
            }
        });
    }

    function sanitizeTranslations(value) {
        const translations = {};

        if (!value || typeof value !== "object" || Array.isArray(value)) {
            return translations;
        }

        Object.keys(value).forEach((key) => {
            if (typeof value[key] === "string") {
                translations[key] = value[key];
            }
        });

        return translations;
    }

    async function loadLocale(language) {
        if (loadedLocales.has(language)) {
            return true;
        }

        if (loadingLocales.has(language)) {
            return loadingLocales.get(language);
        }

        const loading = (async () => {
            let localeAvailable = false;

            try {
                const response = await global.fetch(
                    LOCALE_ROOT + language + ".json",
                    { cache: "no-store" }
                );

                if (!response.ok) {
                    throw new Error("HTTP " + response.status);
                }

                const translations = sanitizeTranslations(
                    await response.json()
                );

                localeCache[language] = language === "en"
                    ? Object.assign({}, ENGLISH_FALLBACK, translations)
                    : translations;

                localeAvailable = true;
            } catch (error) {
                localeCache[language] = language === "en"
                    ? ENGLISH_FALLBACK
                    : {};

                // The bundled English catalog is a complete locale even when
                // en.json is unavailable. Other locales should remain
                // retryable and make their English fallback explicit.
                localeAvailable = language === DEFAULT_LANGUAGE;

                console.warn(
                    "React Player locale could not be loaded; using English fallbacks:",
                    language,
                    error
                );
            } finally {
                if (localeAvailable) {
                    loadedLocales.add(language);
                } else {
                    loadedLocales.delete(language);
                }

                loadingLocales.delete(language);
            }

            return localeAvailable;
        })();

        loadingLocales.set(language, loading);
        return loading;
    }

    function translatableElements(root) {
        const selector = [
            "[data-i18n]",
            "[data-i18n-aria-label]",
            "[data-i18n-title]",
            "[data-i18n-placeholder]"
        ].join(",");

        const elements = [];

        if (root && root.nodeType === 1 && root.matches(selector)) {
            elements.push(root);
        }

        if (root && typeof root.querySelectorAll === "function") {
            elements.push(...root.querySelectorAll(selector));
        }

        return elements;
    }

    function translateDocument(root) {
        const translationRoot = root || global.document;

        translatableElements(translationRoot).forEach((element) => {
            const textKey = element.getAttribute("data-i18n");
            const ariaKey = element.getAttribute("data-i18n-aria-label");
            const titleKey = element.getAttribute("data-i18n-title");
            const placeholderKey = element.getAttribute("data-i18n-placeholder");

            if (textKey) {
                element.textContent = t(textKey);
            }

            if (ariaKey) {
                element.setAttribute("aria-label", t(ariaKey));
            }

            if (titleKey) {
                element.title = t(titleKey);
            }

            if (placeholderKey) {
                element.setAttribute("placeholder", t(placeholderKey));
            }
        });
    }

    async function init() {
        const language = currentLanguage;

        updateDocumentLanguage();

        const localeAvailable = await loadLocale(language);

        if (
            !localeAvailable &&
            language !== DEFAULT_LANGUAGE &&
            currentLanguage === language
        ) {
            currentLanguage = DEFAULT_LANGUAGE;
        }

        updateDocumentLanguage();
        notify();
        return currentLanguage;
    }

    function updateExistingLanguageQuery(language) {
        try {
            const url = new URL(global.location.href);

            if (!url.searchParams.has("lang")) {
                return;
            }

            url.searchParams.set("lang", language);
            global.history.replaceState(null, "", url.toString());
        } catch (error) {
            // The saved selection still provides persistence when URL updates
            // are unavailable (for example, in a restricted browser source).
        }
    }

    async function setLanguage(value) {
        const language = normalizeSelection(value) || DEFAULT_LANGUAGE;

        currentLanguage = language;

        try {
            global.localStorage.setItem(STORAGE_KEY, language);
        } catch (error) {
            // Some privacy modes block storage. The current page can still be
            // translated without persistence.
        }

        updateExistingLanguageQuery(language);
        updateDocumentLanguage();
        notify();

        const localeAvailable = await loadLocale(language);

        if (currentLanguage === language) {
            if (!localeAvailable && language !== DEFAULT_LANGUAGE) {
                currentLanguage = DEFAULT_LANGUAGE;
            }

            updateDocumentLanguage();
            notify();
        }

        return currentLanguage;
    }

    function onChange(listener) {
        if (typeof listener !== "function") {
            return function () {};
        }

        listeners.add(listener);

        return function () {
            listeners.delete(listener);
        };
    }

    updateDocumentLanguage();

    global.ReactPlayerI18n = Object.freeze({
        init: init,
        t: t,
        translateDocument: translateDocument,
        setLanguage: setLanguage,
        getLanguage: () => currentLanguage,
        getIntlLocale: () => currentLanguage === "no" ? "nb-NO" : "en-GB",
        onChange: onChange,
        storageKey: STORAGE_KEY
    });
})(window);
