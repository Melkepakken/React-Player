(function () {
    "use strict";

    const ACCEPTANCE_KEY = "reactPlayerLegalAccepted";
    const ACCEPTANCE_VERSION = "2026-08-17-v1";
    const PRIVACY_URL = "/privacy/";
    const TERMS_URL = "/terms/";
    const YOUTUBE_TERMS_URL = "https://www.youtube.com/t/terms";

    function hasAccepted() {
        try {
            return window.localStorage.getItem(ACCEPTANCE_KEY) === ACCEPTANCE_VERSION;
        } catch (error) {
            return false;
        }
    }

    function rememberAcceptance() {
        try {
            window.localStorage.setItem(ACCEPTANCE_KEY, ACCEPTANCE_VERSION);
        } catch (error) {
            // The download can still continue when storage is unavailable.
        }
    }

    function closeDialog() {
        const overlay = document.querySelector(".react-legal-overlay");

        if (overlay) {
            overlay.remove();
        }
    }

    function link(href, label, external) {
        const anchor = document.createElement("a");
        anchor.href = href;
        anchor.textContent = label;

        if (external) {
            anchor.target = "_blank";
            anchor.rel = "noopener noreferrer";
        }

        return anchor;
    }

    function startDownload(href) {
        const download = document.createElement("a");
        download.href = href;
        download.download = "React-Player-Import.txt";
        download.hidden = true;
        document.body.appendChild(download);
        download.click();
        download.remove();
    }

    function showDownloadDialog(href) {
        closeDialog();

        const overlay = document.createElement("div");
        overlay.className = "react-legal-overlay";

        const dialog = document.createElement("section");
        dialog.className = "react-legal-dialog";
        dialog.setAttribute("role", "dialog");
        dialog.setAttribute("aria-modal", "true");
        dialog.setAttribute("aria-labelledby", "reactLegalTitle");

        const body = document.createElement("div");
        body.className = "react-legal-dialog-body";

        const title = document.createElement("h2");
        title.id = "reactLegalTitle";
        title.textContent = "Before downloading React Player";

        const intro = document.createElement("p");
        intro.textContent = "Please review and accept the React Player Privacy Policy and Terms of Use. YouTube features are also subject to the YouTube Terms of Service.";

        const links = document.createElement("div");
        links.className = "react-legal-dialog-links";
        links.append(
            link(PRIVACY_URL, "Privacy Policy", true),
            link(TERMS_URL, "Terms of Use", true),
            link(YOUTUBE_TERMS_URL, "YouTube Terms", true)
        );

        const actions = document.createElement("div");
        actions.className = "react-legal-actions";

        const accept = document.createElement("button");
        accept.type = "button";
        accept.className = "react-legal-button";
        accept.textContent = "AGREE AND DOWNLOAD";
        accept.addEventListener("click", function () {
            rememberAcceptance();
            closeDialog();
            startDownload(href);
        });

        const cancel = document.createElement("button");
        cancel.type = "button";
        cancel.className = "react-legal-button secondary";
        cancel.textContent = "CANCEL";
        cancel.addEventListener("click", closeDialog);

        const note = document.createElement("p");
        note.className = "react-legal-note";
        note.textContent = "React Player does not use first-party advertising or analytics cookies. Functional browser storage remembers settings and this acceptance.";

        actions.append(accept, cancel);
        body.append(title, intro, links, actions, note);
        dialog.appendChild(body);
        overlay.appendChild(dialog);
        document.body.appendChild(overlay);

        accept.focus();
    }

    function initialize() {
        const downloads = document.querySelectorAll(
            'a[href="/downloads/React-Player-Import.txt"]'
        );

        for (const anchor of downloads) {
            anchor.addEventListener("click", function (event) {
                if (hasAccepted()) {
                    return;
                }

                event.preventDefault();
                showDownloadDialog(anchor.href);
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize, { once: true });
    } else {
        initialize();
    }

    window.ReactPlayerLegal = Object.freeze({
        hasAccepted: hasAccepted,
        version: ACCEPTANCE_VERSION
    });
})();
