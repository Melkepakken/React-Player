(function (global) {
    "use strict";

    const DEFINITIONS = Object.freeze({
        addLink: Object.freeze({
            id: "e873e6a6-96e2-40a3-9905-fe0024e52c81",
            name: "React Player: Add Link"
        }),
        control: Object.freeze({
            id: "bc4cd589-0d98-410f-8c19-5c14505ccab5",
            name: "React Player: Control"
        })
    });

    const STORAGE_PREFIX = "reactPlayerActionId.";
    const resolvedIds = {};

    function definition(key) {
        return DEFINITIONS[key] || null;
    }

    function readStoredId(key) {
        try {
            return String(
                global.localStorage.getItem(
                    STORAGE_PREFIX + key
                ) || ""
            ).trim();
        } catch (error) {
            return "";
        }
    }

    function saveResolvedId(key, id) {
        const cleanId =
            String(id || "").trim();

        if (!cleanId) {
            return;
        }

        resolvedIds[key] =
            cleanId;

        try {
            global.localStorage.setItem(
                STORAGE_PREFIX + key,
                cleanId
            );
        } catch (error) {
            // The current page can still use the resolved ID.
        }
    }

    function findActionById(actions, id) {
        const cleanId =
            String(id || "").trim();

        if (!cleanId) {
            return null;
        }

        return actions.find(
            (action) =>
                String(
                    action && action.id
                        ? action.id
                        : ""
                ) === cleanId
        ) || null;
    }

    function findActionByName(actions, name) {
        const cleanName =
            String(name || "").trim();

        if (!cleanName) {
            return null;
        }

        return actions.find(
            (action) =>
                String(
                    action && action.name
                        ? action.name
                        : ""
                ) === cleanName
        ) || null;
    }

    async function resolve(client) {
        let actions = [];

        try {
            const response =
                await client.getActions();

            if (
                response &&
                Array.isArray(response.actions)
            ) {
                actions =
                    response.actions;
            }
        } catch (error) {
            console.warn(
                "React Player could not read the Streamer.bot action list:",
                error
            );
        }

        Object.keys(DEFINITIONS).forEach(
            (key) => {
                const item =
                    DEFINITIONS[key];

                const storedId =
                    readStoredId(key);

                const storedAction =
                    findActionById(
                        actions,
                        storedId
                    );

                if (storedAction) {
                    saveResolvedId(
                        key,
                        storedAction.id
                    );

                    return;
                }

                const releaseAction =
                    findActionById(
                        actions,
                        item.id
                    );

                if (releaseAction) {
                    saveResolvedId(
                        key,
                        releaseAction.id
                    );

                    return;
                }

                const namedAction =
                    findActionByName(
                        actions,
                        item.name
                    );

                if (namedAction) {
                    saveResolvedId(
                        key,
                        namedAction.id
                    );

                    return;
                }

                if (!resolvedIds[key]) {
                    resolvedIds[key] =
                        storedId ||
                        item.id;
                }
            }
        );

        return Object.assign(
            {},
            resolvedIds
        );
    }

    function getId(key) {
        const item =
            definition(key);

        if (!item) {
            return "";
        }

        return (
            resolvedIds[key] ||
            readStoredId(key) ||
            item.id
        );
    }

    async function run(client, key, args) {
        const item =
            definition(key);

        if (!item) {
            throw new Error(
                "Unknown React Player action key: " +
                key
            );
        }

        let actionId =
            getId(key);

        if (actionId) {
            try {
                return await client.doAction(
                    actionId,
                    args || {}
                );
            } catch (idError) {
                try {
                    await resolve(client);

                    const refreshedId =
                        getId(key);

                    if (
                        refreshedId &&
                        refreshedId !== actionId
                    ) {
                        return await client.doAction(
                            refreshedId,
                            args || {}
                        );
                    }
                } catch (resolveError) {
                    // Name fallback below remains available.
                }
            }
        }

        const response =
            await client.doAction(
                { name: item.name },
                args || {}
            );

        resolve(client).catch(() => {});

        return response;
    }

    function matches(eventData, key) {
        const item =
            definition(key);

        if (!item || !eventData) {
            return false;
        }

        const actionId =
            String(
                eventData.actionId || ""
            ).trim();

        const resolvedId =
            getId(key);

        if (
            actionId &&
            resolvedId &&
            actionId === resolvedId
        ) {
            return true;
        }

        return String(
            eventData.name || ""
        ) === item.name;
    }

    global.ReactPlayerActions = Object.freeze({
        resolve: resolve,
        run: run,
        matches: matches,
        getId: getId,
        definitions: DEFINITIONS
    });
})(window);
