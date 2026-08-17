using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

public class CPHInline
{
    private const string OBS_STATUS_UNKNOWN = "unknown";
    private const string OBS_STATUS_DETECTING = "detecting";
    private const string OBS_STATUS_READY = "ready";
    private const string OBS_STATUS_NOT_FOUND = "not_found";
    private const string OBS_STATUS_NOT_PLACED = "not_placed";
    private const string OBS_STATUS_AMBIGUOUS = "ambiguous";
    private const string OBS_STATUS_ERROR = "error";

    private const string OBS_CONTAINER_SCENE = "scene";
    private const string OBS_CONTAINER_GROUP = "group";

    // OBS does not currently expose a close-projector WebSocket request.
    // On Windows, close the specific React Player projector window directly.
    private const uint WM_CLOSE = 0x0010;

    [DllImport("user32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr FindWindow(
        string lpClassName,
        string lpWindowName
    );

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(
        IntPtr hWnd,
        uint Msg,
        IntPtr wParam,
        IntPtr lParam
    );

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
        public string CompletedAt { get; set; }
    }

    public class PlayerCommand
    {
        public string Command { get; set; }
        public string Nonce { get; set; }
        public int? Value { get; set; }
        public string Provider { get; set; }
        public string ContentId { get; set; }
        public string Url { get; set; }
    }

    public class PlayerTelemetry
    {
        public string RequestId { get; set; }
        public double CurrentTime { get; set; }
        public double Duration { get; set; }
        public double Buffered { get; set; }
        public string UpdatedAt { get; set; }
    }

    public class ObsMonitorInfo
    {
        public string monitorName { get; set; }
        public int monitorIndex { get; set; }
        public int monitorWidth { get; set; }
        public int monitorHeight { get; set; }
        public int monitorPositionX { get; set; }
        public int monitorPositionY { get; set; }
    }

    public class ObsMonitorListResponse
    {
        public List<ObsMonitorInfo> monitors { get; set; }
    }

    public class ObsTargetCandidate
    {
        public string SourceName { get; set; }
        public string ContainerName { get; set; }
        public string ContainerType { get; set; }
        public long SceneItemId { get; set; }
        public string Url { get; set; }
    }

    public class ObsInputInfo
    {
        public string inputName { get; set; }
        public string inputUuid { get; set; }
        public string inputKind { get; set; }
        public string unversionedInputKind { get; set; }
        public long? inputKindCaps { get; set; }
    }

    public class ObsInputListResponse
    {
        public List<ObsInputInfo> inputs { get; set; }
    }

    public class ObsBrowserInputSettings
    {
        public string url { get; set; }
    }

    public class ObsInputSettingsResponse
    {
        public ObsBrowserInputSettings inputSettings { get; set; }
        public string inputKind { get; set; }
    }

    public class ObsSceneInfo
    {
        public string sceneName { get; set; }
        public string sceneUuid { get; set; }
        public long? sceneIndex { get; set; }
    }

    public class ObsSceneListResponse
    {
        public List<ObsSceneInfo> scenes { get; set; }
    }

    public class ObsGroupListResponse
    {
        public List<string> groups { get; set; }
    }

    public class ObsSceneItemInfo
    {
        public string sourceName { get; set; }
        public string sourceUuid { get; set; }
        public string sourceType { get; set; }
        public string inputKind { get; set; }
        public bool? isGroup { get; set; }
        public long? sceneItemId { get; set; }
        public bool? sceneItemEnabled { get; set; }
    }

    public class ObsSceneItemListResponse
    {
        public List<ObsSceneItemInfo> sceneItems { get; set; }
    }

    public class ObsSceneItemEnabledResponse
    {
        public bool? sceneItemEnabled { get; set; }
    }

    public bool Execute()
    {
        // Used by a later If/Else + Run a Program sub-action to open the URL in the user's default browser.
        CPH.SetArgument("reactShouldOpenBrowser", false);
        CPH.SetArgument("reactBrowserUrl", "");

        if (!CPH.TryGetArg("operation", out string operation) ||
            string.IsNullOrWhiteSpace(operation))
        {
            CPH.LogWarn("[React Player] Missing operation.");
            return false;
        }

        operation = operation.Trim().ToLowerInvariant();

        if (operation == "init")
        {
            // Initialization is deliberately done only here.
            // High-frequency operations such as telemetry should stay lean.
            EnsureGlobals();

            if (InitializeObsTarget())
            {
                SyncVisibility();
                SyncProjectorState();
            }
            else
            {
                SetObsDependentStateUnavailable();
            }

            return true;
        }

        if (operation == "detectobs")
        {
            EnsureObsGlobals();

            if (DetectObsTarget())
            {
                SyncVisibility();
                SyncProjectorState();
            }
            else
            {
                SetObsDependentStateUnavailable();
            }

            return true;
        }

        if (operation == "setobstarget")
        {
            EnsureObsGlobals();

            if (!TryGetObsTargetArgument(out ObsTargetCandidate requestedTarget))
            {
                CPH.LogWarn(
                    "[React Player] Invalid OBS target selection."
                );

                return false;
            }

            try
            {
                if (!ValidateObsTarget(
                    requestedTarget,
                    out ObsTargetCandidate validatedTarget))
                {
                    CPH.LogWarn(
                        "[React Player] The selected OBS target no longer matches the React Player source. Detect again."
                    );

                    return false;
                }

                SaveReadyObsTarget(
                    validatedTarget,
                    new List<ObsTargetCandidate>
                    {
                        validatedTarget
                    }
                );

                SyncVisibility();
                SyncProjectorState();
                return true;
            }
            catch (Exception ex)
            {
                SetObsDetectionStatus(OBS_STATUS_ERROR);
                SetObsDependentStateUnavailable();

                CPH.LogWarn(
                    "[React Player] Could not validate the selected OBS target: " +
                    ex.Message
                );

                return false;
            }
        }

        // Lightweight operations first. The dock calls "sync" regularly,
        // so avoid loading/parsing the queue and history unless needed.
        if (operation == "sync")
        {
            SyncVisibility();
            SyncProjectorState();
            return true;
        }

        if (operation == "playerstate")
        {
            if (CPH.TryGetArg("state", out string state) &&
                !string.IsNullOrWhiteSpace(state))
            {
                string normalizedState =
                    state.Trim().ToLowerInvariant();

                CPH.SetGlobalVar(
                    "ReactPlayerState",
                    normalizedState,
                    true
                );

                if (normalizedState == "ended")
                {
                    EndPlaybackSession();

                    if (IsAutoPlayEnabled())
                    {
                        AdvanceQueue("Autoplay");
                    }
                }
            }

            return true;
        }

        if (operation == "toggleautoplay")
        {
            bool nextAutoPlay =
                !IsAutoPlayEnabled();

            CPH.SetGlobalVar(
                "ReactAutoPlay",
                nextAutoPlay,
                true
            );

            CPH.LogInfo(
                "[React Player] Autoplay next: " +
                (nextAutoPlay ? "ON" : "OFF")
            );

            // If autoplay is enabled after the current item has already ended,
            // advance immediately instead of waiting for another state event.
            if (nextAutoPlay)
            {
                string playerState =
                    CPH.GetGlobalVar<string>(
                        "ReactPlayerState",
                        true
                    );

                if (string.Equals(
                        playerState,
                        "ended",
                        StringComparison.OrdinalIgnoreCase))
                {
                    AdvanceQueue("Autoplay");
                }
            }

            return true;
        }

        if (operation == "mute")
        {
            return Mute();
        }

        if (operation == "unmute")
        {
            return Unmute();
        }

        if (operation == "togglemute")
        {
            return ToggleMute();
        }

        if (operation == "hidevideo")
        {
            return HideVideo();
        }

        if (operation == "showvideo")
        {
            return ShowVideo();
        }

        if (operation == "togglevideo")
        {
            return ToggleVideo();
        }

        if (operation == "telemetry")
        {
            MediaRequest currentForTelemetry = LoadCurrent();

            if (currentForTelemetry == null)
            {
                ClearTelemetry();
                return true;
            }

            string requestId = "";

            CPH.TryGetArg(
                "requestId",
                out requestId
            );

            if (string.IsNullOrWhiteSpace(requestId) ||
                currentForTelemetry.Id != requestId)
            {
                return true;
            }

            double currentTime = 0;
            double duration = 0;
            double buffered = 0;

            CPH.TryGetArg(
                "currentTime",
                out currentTime
            );

            CPH.TryGetArg(
                "duration",
                out duration
            );

            CPH.TryGetArg(
                "buffered",
                out buffered
            );

            currentTime = Math.Max(0, currentTime);
            duration = Math.Max(0, duration);
            buffered = Math.Max(
                0,
                Math.Min(1, buffered)
            );

            if (duration > 0)
            {
                currentTime = Math.Min(
                    duration,
                    currentTime
                );
            }

            if (duration > 0 &&
                currentForTelemetry.DurationSeconds <= 0)
            {
                int learnedDuration =
                    (int)Math.Round(duration);

                if (learnedDuration > 0)
                {
                    currentForTelemetry.DurationSeconds =
                        learnedDuration;

                    SaveCurrent(
                        currentForTelemetry
                    );

                    List<MediaRequest> durationQueue =
                        LoadList("ReactQueue");

                    bool queueChanged = false;

                    for (int i = 0; i < durationQueue.Count; i++)
                    {
                        MediaRequest queuedRequest =
                            durationQueue[i];

                        if (queuedRequest != null &&
                            queuedRequest.Id == currentForTelemetry.Id &&
                            queuedRequest.DurationSeconds <= 0)
                        {
                            queuedRequest.DurationSeconds =
                                learnedDuration;

                            queueChanged = true;
                            break;
                        }
                    }

                    if (queueChanged)
                    {
                        SaveList(
                            "ReactQueue",
                            durationQueue
                        );
                    }
                }
            }

            PlayerTelemetry telemetry = new PlayerTelemetry
            {
                RequestId = requestId,
                CurrentTime = currentTime,
                Duration = duration,
                Buffered = buffered,
                UpdatedAt = DateTime.UtcNow.ToString("o")
            };

            CPH.SetGlobalVar(
                "ReactPlayerTelemetry",
                JsonConvert.SerializeObject(telemetry),
                false
            );

            return true;
        }

        if (operation == "seek")
        {
            MediaRequest currentForSeek = LoadCurrent();

            if (currentForSeek == null)
                return false;

            int seconds = 0;

            if (!CPH.TryGetArg(
                    "seconds",
                    out seconds))
            {
                string rawSeconds = "";

                if (CPH.TryGetArg(
                        "seconds",
                        out rawSeconds))
                {
                    int.TryParse(
                        rawSeconds,
                        out seconds
                    );
                }
            }

            seconds = Math.Max(0, seconds);

            SendPlayerCommand(
                "seek",
                seconds
            );

            return true;
        }

        if (operation == "seekrelative")
        {
            MediaRequest currentForRelativeSeek = LoadCurrent();

            if (currentForRelativeSeek == null)
                return false;

            int delta = 0;

            if (!CPH.TryGetArg(
                    "delta",
                    out delta))
            {
                string rawDelta = "";

                if (CPH.TryGetArg(
                        "delta",
                        out rawDelta))
                {
                    int.TryParse(
                        rawDelta,
                        out delta
                    );
                }
            }

            delta = Math.Max(
                -3600,
                Math.Min(3600, delta)
            );

            SendPlayerCommand(
                "seekrelative",
                delta
            );

            return true;
        }

        if (operation == "volume")
        {
            int volume = 100;

            if (!CPH.TryGetArg("volume", out volume))
            {
                string rawVolume = "";

                if (CPH.TryGetArg("volume", out rawVolume))
                {
                    int.TryParse(rawVolume, out volume);
                }
            }

            volume = Math.Max(0, Math.Min(100, volume));

            CPH.SetGlobalVar(
                "ReactPlayerVolume",
                volume,
                true
            );

            SendPlayerCommand(
                "volume",
                volume
            );

            return true;
        }

        if (operation == "togglevisibility")
        {
            if (!TryGetValidatedObsTargetForOperation(
                "change source visibility",
                out ObsTargetCandidate visibilityTarget))
            {
                return false;
            }

            try
            {
                bool visible = GetSceneItemEnabled(
                    visibilityTarget
                );

                bool nextVisible = !visible;

                SetSceneItemEnabled(
                    visibilityTarget,
                    nextVisible
                );

                CPH.SetGlobalVar(
                    "ReactPlayerVisible",
                    nextVisible,
                    true
                );

                return true;
            }
            catch (Exception ex)
            {
                HandleObsOperationError(
                    "change source visibility",
                    ex
                );

                return false;
            }
        }

        if (operation == "toggleprojector")
        {
            if (!TryGetValidatedObsTargetForOperation(
                "toggle the source projector",
                out ObsTargetCandidate projectorTarget))
            {
                return false;
            }

            IntPtr projectorWindow = FindPlayerProjectorWindow(
                projectorTarget.SourceName
            );

            if (projectorWindow != IntPtr.Zero)
            {
                bool closeSent = PostMessage(
                    projectorWindow,
                    WM_CLOSE,
                    IntPtr.Zero,
                    IntPtr.Zero
                );

                if (closeSent)
                {
                    // WM_CLOSE is asynchronous. Updating the global immediately
                    // keeps the dock responsive; the regular sync remains the
                    // authoritative safety net if OBS refuses to close it.
                    CPH.SetGlobalVar(
                        "ReactPlayerProjectorOpen",
                        false,
                        false
                    );
                }
                else
                {
                    CPH.LogWarn(
                        "[React Player] Could not close the source projector window."
                    );

                    SyncProjectorState();
                }

                return true;
            }

            int monitorIndex = GetMainMonitorIndex();

            string data = JsonConvert.SerializeObject(new
            {
                sourceName = projectorTarget.SourceName,
                monitorIndex = monitorIndex
            });

            try
            {
                SendObsCommand(
                    "OpenSourceProjector",
                    data
                );
            }
            catch (Exception ex)
            {
                HandleObsOperationError(
                    "open the source projector",
                    ex
                );

                return false;
            }

            // The projector window is created by OBS on its UI thread.
            // Mark it open immediately for responsive UI; the regular sync
            // corrects this against the real window state every two seconds.
            CPH.SetGlobalVar(
                "ReactPlayerProjectorOpen",
                true,
                false
            );

            return true;
        }

        if (operation == "interact")
        {
            if (!TryGetValidatedObsTargetForOperation(
                "open OBS Interact",
                out ObsTargetCandidate interactTarget))
            {
                return false;
            }

            string data = JsonConvert.SerializeObject(new
            {
                inputName = interactTarget.SourceName
            });

            try
            {
                SendObsCommand(
                    "OpenInputInteractDialog",
                    data
                );
            }
            catch (Exception ex)
            {
                HandleObsOperationError(
                    "open OBS Interact",
                    ex
                );

                return false;
            }

            return true;
        }

        if (operation == "refresh")
        {
            if (!TryGetValidatedObsTargetForOperation(
                "refresh the player source",
                out ObsTargetCandidate refreshTarget))
            {
                return false;
            }

            string data = JsonConvert.SerializeObject(new
            {
                inputName = refreshTarget.SourceName,
                propertyName = "refreshnocache"
            });

            try
            {
                SendObsCommand(
                    "PressInputPropertiesButton",
                    data
                );
            }
            catch (Exception ex)
            {
                HandleObsOperationError(
                    "refresh the player source",
                    ex
                );

                return false;
            }

            MediaRequest currentForRefresh = LoadCurrent();

            if (currentForRefresh != null)
            {
                CPH.SetGlobalVar(
                    "ReactPlayerState",
                    "loading",
                    true
                );

                ClearTelemetry();
            }

            return true;
        }

        if (operation == "pause")
        {
            MediaRequest currentForPause = LoadCurrent();

            if (currentForPause == null)
                return false;

            string state = CPH.GetGlobalVar<string>(
                "ReactPlayerState",
                true
            );

            if (string.Equals(
                state,
                "paused",
                StringComparison.OrdinalIgnoreCase))
            {
                SendPlayerCommand("play");
            }
            else
            {
                SendPlayerCommand("pause");
            }

            return true;
        }

        if (operation == "stop")
        {
            SaveCurrent(null);
            EndPlaybackSession();
            ClearTelemetry();

            CPH.SetGlobalVar(
                "ReactPlayerState",
                "idle",
                true
            );

            SendPlayerCommand("stop");
            return true;
        }

        if (operation == "clearhistory")
        {
            List<MediaRequest> historyToClear = LoadList(
                "ReactHistory"
            );

            historyToClear.Clear();
            SaveList("ReactHistory", historyToClear);
            return true;
        }

        if (operation == "clearqueue")
        {
            List<MediaRequest> queueToClear = LoadList(
                "ReactQueue"
            );

            MediaRequest currentForClear = LoadCurrent();
            List<MediaRequest> nextQueue = new List<MediaRequest>();

            // Keep the item currently being watched so "Ferdig" can still
            // move it into history after the rest of the queue is cleared.
            if (currentForClear != null)
            {
                MediaRequest queuedCurrent = FindInList(
                    queueToClear,
                    currentForClear.Id
                );

                if (queuedCurrent != null)
                    nextQueue.Add(queuedCurrent);
            }

            SaveList("ReactQueue", nextQueue);
            return true;
        }

        if (operation == "skip")
        {
            return Skip();
        }

        if (operation == "closepreferences")
        {
            ClosePlaybackPreferences(true);
            return true;
        }

        if (operation == "openpreferences")
        {
            return OpenPlaybackPreferences();
        }

        if (!CPH.TryGetArg("id", out string id) ||
            string.IsNullOrWhiteSpace(id))
        {
            CPH.LogWarn(
                "[React Player] Operation " +
                operation +
                " is missing request ID."
            );

            return false;
        }

        List<MediaRequest> queue = LoadList("ReactQueue");
        List<MediaRequest> history = LoadList("ReactHistory");
        MediaRequest current = LoadCurrent();

        MediaRequest request = FindRequest(
            id,
            current,
            queue,
            history
        );

        if (request == null)
        {
            CPH.LogWarn(
                "[React Player] Request not found: " + id
            );

            return false;
        }

        if (operation == "moveup" ||
            operation == "movedown")
        {
            int currentIndex = -1;

            for (int i = 0; i < queue.Count; i++)
            {
                MediaRequest queuedRequest = queue[i];

                if (queuedRequest != null &&
                    queuedRequest.Id == id)
                {
                    currentIndex = i;
                    break;
                }
            }

            if (currentIndex < 0)
            {
                CPH.LogWarn(
                    "[React Player] Cannot reorder request because it is not in ReactQueue: " +
                    id
                );

                return false;
            }

            int targetIndex =
                operation == "moveup"
                    ? currentIndex - 1
                    : currentIndex + 1;

            if (targetIndex < 0 ||
                targetIndex >= queue.Count)
            {
                return true;
            }

            MediaRequest movedRequest =
                queue[currentIndex];

            queue[currentIndex] =
                queue[targetIndex];

            queue[targetIndex] =
                movedRequest;

            SaveList(
                "ReactQueue",
                queue
            );

            CPH.LogInfo(
                "[React Player] Moved request " +
                id +
                (operation == "moveup"
                    ? " up."
                    : " down.")
            );

            return true;
        }

        if (operation == "play")
        {
            ClosePlaybackPreferences(false);
            SaveCurrent(request);
            StartPlaybackSession(request);
            ClearTelemetry();

            CPH.SetGlobalVar(
                "ReactPlayerState",
                "loading",
                true
            );

            SendPlayerCommand("load");

            CPH.LogInfo(
                "[React Player] Loaded request from " +
                request.User +
                ": " +
                request.Url
            );

            return true;
        }

        if (operation == "browser")
        {
            CPH.SetArgument(
                "reactBrowserUrl",
                request.Url
            );

            CPH.SetArgument(
                "reactShouldOpenBrowser",
                true
            );

            return true;
        }

        if (operation == "done")
        {
            MediaRequest queuedRequest = FindInList(
                queue,
                id
            );

            if (queuedRequest != null)
            {
                queue.Remove(queuedRequest);

                queuedRequest.CompletedAt =
                    DateTime.UtcNow.ToString("o");

                history.Insert(0, queuedRequest);

                SaveList("ReactQueue", queue);
                SaveList("ReactHistory", history);
            }

            if (current != null && current.Id == id)
            {
                SaveCurrent(null);
                EndPlaybackSession();
                ClearTelemetry();

                CPH.SetGlobalVar(
                    "ReactPlayerState",
                    "idle",
                    true
                );

                SendPlayerCommand("stop");
            }

            CPH.LogInfo(
                "[React Player] Completed request from " +
                request.User
            );

            return true;
        }

        if (operation == "remove")
        {
            MediaRequest queuedRequest = FindInList(
                queue,
                id
            );

            if (queuedRequest == null)
                return false;

            queue.Remove(queuedRequest);
            SaveList("ReactQueue", queue);

            if (current != null && current.Id == id)
            {
                SaveCurrent(null);
                EndPlaybackSession();
                ClearTelemetry();

                CPH.SetGlobalVar(
                    "ReactPlayerState",
                    "idle",
                    true
                );

                SendPlayerCommand("stop");
            }

            CPH.LogInfo(
                "[React Player] Removed request from " +
                request.User
            );

            return true;
        }

        CPH.LogWarn(
            "[React Player] Unknown operation: " +
            operation
        );

        return false;
    }

    private void EnsureGlobals()
    {
        EnsureObsGlobals();
        EnsurePlaybackPreferencesGlobals();

        string queue = CPH.GetGlobalVar<string>(
            "ReactQueue",
            true
        );

        if (string.IsNullOrWhiteSpace(queue))
        {
            CPH.SetGlobalVar(
                "ReactQueue",
                "[]",
                true
            );
        }

        string history = CPH.GetGlobalVar<string>(
            "ReactHistory",
            true
        );

        if (string.IsNullOrWhiteSpace(history))
        {
            CPH.SetGlobalVar(
                "ReactHistory",
                "[]",
                true
            );
        }

        string current = CPH.GetGlobalVar<string>(
            "ReactCurrent",
            true
        );

        if (string.IsNullOrWhiteSpace(current))
        {
            CPH.SetGlobalVar(
                "ReactCurrent",
                "null",
                true
            );
        }

        string playerState = CPH.GetGlobalVar<string>(
            "ReactPlayerState",
            true
        );

        if (string.IsNullOrWhiteSpace(playerState))
        {
            playerState = "idle";

            CPH.SetGlobalVar(
                "ReactPlayerState",
                playerState,
                true
            );
        }

        string playbackSessionId =
            CPH.GetGlobalVar<string>(
                "ReactPlaybackSessionId",
                false
            );

        if (string.IsNullOrWhiteSpace(playbackSessionId))
        {
            MediaRequest currentForSession =
                LoadCurrent();

            bool sessionCanBeActive =
                currentForSession != null &&
                !string.Equals(
                    playerState,
                    "idle",
                    StringComparison.OrdinalIgnoreCase
                ) &&
                !string.Equals(
                    playerState,
                    "ended",
                    StringComparison.OrdinalIgnoreCase
                ) &&
                !string.Equals(
                    playerState,
                    "error",
                    StringComparison.OrdinalIgnoreCase
                );

            if (sessionCanBeActive)
            {
                StartPlaybackSession(
                    currentForSession
                );
            }
            else
            {
                EndPlaybackSession();
            }
        }

        // 0 is a valid user-selected volume. Only initialize the variable
        // if it does not exist yet.
        string playerVolumeRaw = CPH.GetGlobalVar<string>(
            "ReactPlayerVolume",
            true
        );

        if (string.IsNullOrWhiteSpace(playerVolumeRaw))
        {
            CPH.SetGlobalVar(
                "ReactPlayerVolume",
                100,
                true
            );
        }

        bool? playerMuted =
            CPH.GetGlobalVar<bool?>(
                "ReactPlayerMuted",
                true
            );

        if (!playerMuted.HasValue)
        {
            CPH.SetGlobalVar(
                "ReactPlayerMuted",
                false,
                true
            );
        }

        bool? playerVideoHidden =
            CPH.GetGlobalVar<bool?>(
                "ReactPlayerVideoHidden",
                true
            );

        if (!playerVideoHidden.HasValue)
        {
            CPH.SetGlobalVar(
                "ReactPlayerVideoHidden",
                false,
                true
            );
        }

        string playerTelemetry = CPH.GetGlobalVar<string>(
            "ReactPlayerTelemetry",
            false
        );

        if (string.IsNullOrWhiteSpace(playerTelemetry))
        {
            CPH.SetGlobalVar(
                "ReactPlayerTelemetry",
                "null",
                false
            );
        }

        string projectorState = CPH.GetGlobalVar<string>(
            "ReactPlayerProjectorOpen",
            false
        );

        if (string.IsNullOrWhiteSpace(projectorState))
        {
            CPH.SetGlobalVar(
                "ReactPlayerProjectorOpen",
                false,
                false
            );
        }

        string autoPlayRaw =
            CPH.GetGlobalVar<string>(
                "ReactAutoPlay",
                true
            );

        if (string.IsNullOrWhiteSpace(autoPlayRaw))
        {
            CPH.SetGlobalVar(
                "ReactAutoPlay",
                false,
                true
            );
        }

        string playerCommand = CPH.GetGlobalVar<string>(
            "ReactPlayerCommand",
            false
        );

        if (string.IsNullOrWhiteSpace(playerCommand))
        {
            CPH.SetGlobalVar(
                "ReactPlayerCommand",
                "null",
                false
            );
        }
    }

    private bool IsAutoPlayEnabled()
    {
        string raw =
            CPH.GetGlobalVar<string>(
                "ReactAutoPlay",
                true
            );

        if (string.IsNullOrWhiteSpace(raw))
            return false;

        return string.Equals(
            raw,
            "true",
            StringComparison.OrdinalIgnoreCase
        );
    }

    public bool Mute()
    {
        return SetMuted(
            true
        );
    }

    public bool Unmute()
    {
        return SetMuted(
            false
        );
    }

    public bool ToggleMute()
    {
        return SetMuted(
            !IsMuted()
        );
    }

    private bool SetMuted(
        bool muted)
    {
        CPH.SetGlobalVar(
            "ReactPlayerMuted",
            muted,
            true
        );

        SendPlayerCommand(
            muted
                ? "mute"
                : "unmute"
        );

        CPH.LogInfo(
            "[React Player] Desired player mute state: " +
            (muted ? "MUTED" : "UNMUTED")
        );

        return true;
    }

    private bool IsMuted()
    {
        bool? muted =
            CPH.GetGlobalVar<bool?>(
                "ReactPlayerMuted",
                true
            );

        return
            muted.HasValue &&
            muted.Value;
    }

    public bool HideVideo()
    {
        return SetVideoHidden(
            true
        );
    }

    public bool ShowVideo()
    {
        return SetVideoHidden(
            false
        );
    }

    public bool ToggleVideo()
    {
        return SetVideoHidden(
            !IsVideoHidden()
        );
    }

    private bool SetVideoHidden(
        bool hidden)
    {
        CPH.SetGlobalVar(
            "ReactPlayerVideoHidden",
            hidden,
            true
        );

        SendPlayerCommand(
            hidden
                ? "hidevideo"
                : "showvideo"
        );

        CPH.LogInfo(
            "[React Player] Video output: " +
            (hidden ? "HIDDEN" : "VISIBLE")
        );

        return true;
    }

    private bool IsVideoHidden()
    {
        bool? hidden =
            CPH.GetGlobalVar<bool?>(
                "ReactPlayerVideoHidden",
                true
            );

        return
            hidden.HasValue &&
            hidden.Value;
    }

    public bool Skip()
    {
        EnsureGlobals();

        return AdvanceQueue(
            "Skip"
        );
    }

    private bool AdvanceQueue(
        string reason)
    {
        MediaRequest finished =
            LoadCurrent();

        if (finished == null)
        {
            CPH.LogInfo(
                "[React Player] " +
                reason +
                " requested with no current media."
            );

            return false;
        }

        List<MediaRequest> queue =
            LoadList("ReactQueue");

        List<MediaRequest> history =
            LoadList("ReactHistory");

        MediaRequest queuedFinished =
            FindInList(
                queue,
                finished.Id
            );

        // Normal queued requests are completed when advancing.
        // A replayed history item is not inserted into history again.
        if (queuedFinished != null)
        {
            queue.Remove(
                queuedFinished
            );

            queuedFinished.CompletedAt =
                DateTime.UtcNow.ToString("o");

            history.Insert(
                0,
                queuedFinished
            );

            SaveList(
                "ReactQueue",
                queue
            );

            SaveList(
                "ReactHistory",
                history
            );
        }

        MediaRequest nextRequest =
            queue.Count > 0
                ? queue[0]
                : null;

        ClearTelemetry();

        if (nextRequest == null)
        {
            SaveCurrent(null);
            EndPlaybackSession();

            CPH.SetGlobalVar(
                "ReactPlayerState",
                "idle",
                true
            );

            SendPlayerCommand(
                "stop"
            );

            CPH.LogInfo(
                "[React Player] " +
                reason +
                " reached the end of the queue."
            );

            return true;
        }

        ClosePlaybackPreferences(false);

        SaveCurrent(
            nextRequest
        );

        StartPlaybackSession(
            nextRequest
        );

        CPH.SetGlobalVar(
            "ReactPlayerState",
            "loading",
            true
        );

        SendPlayerCommand(
            "load"
        );

        CPH.LogInfo(
            "[React Player] " +
            reason +
            " advanced to " +
            nextRequest.Provider +
            "/" +
            nextRequest.MediaType +
            " request from " +
            nextRequest.User +
            ": " +
            nextRequest.Url
        );

        return true;
    }

    private void StartPlaybackSession(
        MediaRequest request)
    {
        string requestId =
            request == null
                ? ""
                : request.Id ?? "";

        string sessionId =
            string.IsNullOrWhiteSpace(requestId)
                ? Guid.NewGuid().ToString("N")
                : requestId +
                    ":" +
                    Guid.NewGuid().ToString("N");

        CPH.SetGlobalVar(
            "ReactPlaybackSessionId",
            sessionId,
            false
        );

        ResetVoteSkipSession();
    }

    private void EndPlaybackSession()
    {
        CPH.SetGlobalVar(
            "ReactPlaybackSessionId",
            "",
            false
        );

        ResetVoteSkipSession();
    }

    private void ResetVoteSkipSession()
    {
        CPH.SetGlobalVar(
            "ReactVoteSkipSessionId",
            "",
            false
        );

        CPH.SetGlobalVar(
            "ReactVoteSkipVoters",
            "[]",
            false
        );

        CPH.SetGlobalVar(
            "ReactVoteSkipCount",
            0,
            false
        );
    }

    private void ClearTelemetry()
    {
        CPH.SetGlobalVar(
            "ReactPlayerTelemetry",
            "null",
            false
        );
    }

    private void EnsureObsGlobals()
    {
        string rawStatus = CPH.GetGlobalVar<string>(
            "ReactObsDetectionStatus",
            true
        );

        string status = string.IsNullOrWhiteSpace(rawStatus)
            ? ""
            : rawStatus.Trim().ToLowerInvariant();

        if (!IsKnownObsStatus(status))
        {
            SetObsDetectionStatus(OBS_STATUS_UNKNOWN);
        }

        EnsureObsStringGlobal("ReactObsSource");
        EnsureObsStringGlobal("ReactObsContainer");
        EnsureObsStringGlobal("ReactObsContainerType");
        EnsureObsStringGlobal("ReactObsSourceUrl");

        if (!TryGetObsSceneItemId(out long sceneItemId) ||
            sceneItemId < -1)
        {
            CPH.SetGlobalVar(
                "ReactObsSceneItemId",
                -1L,
                true
            );
        }

        string candidatesJson = CPH.GetGlobalVar<string>(
            "ReactObsCandidates",
            true
        );

        bool candidatesValid = false;

        if (!string.IsNullOrWhiteSpace(candidatesJson))
        {
            try
            {
                List<ObsTargetCandidate> candidates =
                    JsonConvert.DeserializeObject<
                        List<ObsTargetCandidate>
                    >(candidatesJson);

                candidatesValid = candidates != null;
            }
            catch (Exception)
            {
                candidatesValid = false;
            }
        }

        if (!candidatesValid)
        {
            CPH.SetGlobalVar(
                "ReactObsCandidates",
                "[]",
                true
            );
        }
    }

    private void EnsureObsStringGlobal(string variableName)
    {
        string value = CPH.GetGlobalVar<string>(
            variableName,
            true
        );

        if (value == null)
        {
            CPH.SetGlobalVar(
                variableName,
                "",
                true
            );
        }
    }

    private bool InitializeObsTarget()
    {
        if (TryLoadSavedObsTarget(out ObsTargetCandidate savedTarget))
        {
            try
            {
                if (ValidateObsTarget(
                    savedTarget,
                    out ObsTargetCandidate validatedTarget))
                {
                    SaveReadyObsTarget(
                        validatedTarget,
                        new List<ObsTargetCandidate>
                        {
                            validatedTarget
                        }
                    );

                    return true;
                }
            }
            catch (Exception)
            {
                // A renamed or removed source can make validation fail as a
                // raw OBS request error. A fresh scan distinguishes that from
                // a wider OBS connection or response problem.
            }
        }

        return DetectObsTarget();
    }

    private bool DetectObsTarget()
    {
        TryLoadSavedObsTarget(
            out ObsTargetCandidate previouslySelectedTarget
        );

        SetObsDetectionStatus(OBS_STATUS_DETECTING);

        try
        {
            ObsInputListResponse inputList =
                SendObsRequest<ObsInputListResponse>(
                    "GetInputList",
                    null
                );

            if (inputList.inputs == null)
            {
                throw new InvalidOperationException(
                    "GetInputList did not return an inputs array."
                );
            }

            Dictionary<string, string> matchingSources =
                new Dictionary<string, string>(
                    StringComparer.Ordinal
                );

            for (int i = 0; i < inputList.inputs.Count; i++)
            {
                ObsInputInfo input = inputList.inputs[i];

                if (input == null ||
                    string.IsNullOrWhiteSpace(input.inputName) ||
                    (!IsBrowserInputKind(input.inputKind) &&
                     !IsBrowserInputKind(input.unversionedInputKind)))
                {
                    continue;
                }

                if (TryGetReactPlayerSourceUrl(
                    input.inputName,
                    out string sourceUrl))
                {
                    matchingSources[input.inputName] = sourceUrl;
                }
            }

            if (matchingSources.Count == 0)
            {
                ClearObsTargetFields();
                SaveObsCandidates(
                    new List<ObsTargetCandidate>()
                );
                SetObsDetectionStatus(OBS_STATUS_NOT_FOUND);
                return false;
            }

            List<ObsTargetCandidate> sourceDiagnostics =
                new List<ObsTargetCandidate>();

            foreach (KeyValuePair<string, string> source in matchingSources)
            {
                sourceDiagnostics.Add(new ObsTargetCandidate
                {
                    SourceName = source.Key,
                    ContainerName = "",
                    ContainerType = "",
                    SceneItemId = -1L,
                    Url = source.Value
                });
            }

            sourceDiagnostics.Sort(CompareObsCandidates);

            List<ObsTargetCandidate> placements =
                FindObsTargetPlacements(matchingSources);

            placements.Sort(CompareObsCandidates);

            if (placements.Count == 0)
            {
                if (sourceDiagnostics.Count == 1)
                {
                    SetObsTargetFields(sourceDiagnostics[0]);
                }
                else
                {
                    ClearObsTargetFields();
                }

                SaveObsCandidates(sourceDiagnostics);
                SetObsDetectionStatus(OBS_STATUS_NOT_PLACED);
                return false;
            }

            if (placements.Count == 1)
            {
                SaveReadyObsTarget(
                    placements[0],
                    placements
                );

                return true;
            }

            ObsTargetCandidate preservedTarget =
                FindMatchingObsTarget(
                    previouslySelectedTarget,
                    placements
                );

            if (preservedTarget != null)
            {
                SetObsTargetFields(preservedTarget);
            }
            else
            {
                ClearObsTargetFields();
            }

            SaveObsCandidates(placements);
            SetObsDetectionStatus(OBS_STATUS_AMBIGUOUS);
            return false;
        }
        catch (Exception ex)
        {
            SetObsDetectionStatus(OBS_STATUS_ERROR);

            CPH.LogWarn(
                "[React Player] OBS target detection failed: " +
                ex.Message
            );

            return false;
        }
    }

    private List<ObsTargetCandidate> FindObsTargetPlacements(
        Dictionary<string, string> matchingSources)
    {
        List<ObsTargetCandidate> placements =
            new List<ObsTargetCandidate>();

        ObsSceneListResponse sceneList =
            SendObsRequest<ObsSceneListResponse>(
                "GetSceneList",
                null
            );

        if (sceneList.scenes == null)
        {
            throw new InvalidOperationException(
                "GetSceneList did not return a scenes array."
            );
        }

        for (int i = 0; i < sceneList.scenes.Count; i++)
        {
            ObsSceneInfo scene = sceneList.scenes[i];

            if (scene == null ||
                string.IsNullOrWhiteSpace(scene.sceneName))
            {
                continue;
            }

            AddObsTargetPlacements(
                placements,
                matchingSources,
                scene.sceneName,
                OBS_CONTAINER_SCENE,
                "GetSceneItemList"
            );
        }

        ObsGroupListResponse groupList =
            SendObsRequest<ObsGroupListResponse>(
                "GetGroupList",
                null
            );

        if (groupList.groups == null)
        {
            throw new InvalidOperationException(
                "GetGroupList did not return a groups array."
            );
        }

        for (int i = 0; i < groupList.groups.Count; i++)
        {
            string groupName = groupList.groups[i];

            if (string.IsNullOrWhiteSpace(groupName))
                continue;

            AddObsTargetPlacements(
                placements,
                matchingSources,
                groupName,
                OBS_CONTAINER_GROUP,
                "GetGroupSceneItemList"
            );
        }

        return placements;
    }

    private void AddObsTargetPlacements(
        List<ObsTargetCandidate> placements,
        Dictionary<string, string> matchingSources,
        string containerName,
        string containerType,
        string requestType)
    {
        ObsSceneItemListResponse itemList =
            SendObsRequest<ObsSceneItemListResponse>(
                requestType,
                new
                {
                    sceneName = containerName
                }
            );

        if (itemList.sceneItems == null)
        {
            throw new InvalidOperationException(
                requestType +
                " did not return a sceneItems array."
            );
        }

        for (int i = 0; i < itemList.sceneItems.Count; i++)
        {
            ObsSceneItemInfo item = itemList.sceneItems[i];

            if (item == null ||
                string.IsNullOrWhiteSpace(item.sourceName) ||
                !item.sceneItemId.HasValue ||
                item.sceneItemId.Value < 0 ||
                !matchingSources.TryGetValue(
                    item.sourceName,
                    out string sourceUrl))
            {
                continue;
            }

            ObsTargetCandidate candidate = new ObsTargetCandidate
            {
                SourceName = item.sourceName,
                ContainerName = containerName,
                ContainerType = containerType,
                SceneItemId = item.sceneItemId.Value,
                Url = sourceUrl
            };

            if (FindMatchingObsTarget(
                candidate,
                placements) == null)
            {
                placements.Add(candidate);
            }
        }
    }

    private bool TryGetReactPlayerSourceUrl(
        string sourceName,
        out string sourceUrl)
    {
        sourceUrl = "";

        ObsInputSettingsResponse settings =
            SendObsRequest<ObsInputSettingsResponse>(
                "GetInputSettings",
                new
                {
                    inputName = sourceName
                }
            );

        if (!IsBrowserInputKind(settings.inputKind) ||
            settings.inputSettings == null ||
            string.IsNullOrWhiteSpace(settings.inputSettings.url) ||
            !IsReactPlayerUrl(settings.inputSettings.url))
        {
            return false;
        }

        sourceUrl = settings.inputSettings.url.Trim();
        return true;
    }

    private bool IsBrowserInputKind(string inputKind)
    {
        if (string.IsNullOrWhiteSpace(inputKind))
            return false;

        string normalized = inputKind.Trim().ToLowerInvariant();

        if (normalized == "browser_source")
            return true;

        const string versionedPrefix = "browser_source_v";

        if (!normalized.StartsWith(
            versionedPrefix,
            StringComparison.Ordinal))
        {
            return false;
        }

        string version = normalized.Substring(
            versionedPrefix.Length
        );

        return IsDigits(version);
    }

    private bool IsReactPlayerUrl(string rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl))
            return false;

        string value = rawUrl.Trim();
        int queryIndex = value.IndexOf('?');
        int fragmentIndex = value.IndexOf('#');
        int suffixIndex = FirstNonNegative(
            queryIndex,
            fragmentIndex
        );

        if (suffixIndex >= 0)
        {
            value = value.Substring(0, suffixIndex);
        }

        int schemeSeparator = value.IndexOf(
            "://",
            StringComparison.Ordinal
        );

        if (schemeSeparator <= 0)
            return false;

        string scheme = value.Substring(
            0,
            schemeSeparator
        ).ToLowerInvariant();

        if (scheme != "http" && scheme != "https")
            return false;

        string remainder = value.Substring(
            schemeSeparator + 3
        );

        int pathIndex = remainder.IndexOf('/');

        if (pathIndex <= 0)
            return false;

        string authority = remainder.Substring(0, pathIndex);
        string path = remainder.Substring(pathIndex);

        if (authority.Length == 0 ||
            authority.IndexOf('@') >= 0 ||
            authority.StartsWith("[", StringComparison.Ordinal))
        {
            return false;
        }

        if (path.EndsWith("/", StringComparison.Ordinal))
        {
            path = path.Substring(0, path.Length - 1);
        }

        if (!string.Equals(
            path,
            "/v1/player",
            StringComparison.Ordinal))
        {
            return false;
        }

        string host = authority;
        string port = "";
        int colonIndex = authority.LastIndexOf(':');

        if (colonIndex >= 0)
        {
            if (authority.IndexOf(':') != colonIndex)
                return false;

            host = authority.Substring(0, colonIndex);
            port = authority.Substring(colonIndex + 1);

            if (!IsDigits(port) ||
                !int.TryParse(port, out int portNumber) ||
                portNumber < 1 ||
                portNumber > 65535)
            {
                return false;
            }
        }

        host = host.ToLowerInvariant();

        if (host == "localhost" || host == "127.0.0.1")
            return true;

        if (scheme != "https" || port.Length > 0)
            return false;

        if (host == "react.melkepakken.tv" ||
            host == "react-player.pages.dev")
        {
            return true;
        }

        const string previewSuffix = ".react-player.pages.dev";

        return host.Length > previewSuffix.Length &&
            host.EndsWith(
                previewSuffix,
                StringComparison.Ordinal
            );
    }

    private int FirstNonNegative(int first, int second)
    {
        if (first < 0)
            return second;

        if (second < 0)
            return first;

        return Math.Min(first, second);
    }

    private bool IsDigits(string value)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        for (int i = 0; i < value.Length; i++)
        {
            if (value[i] < '0' || value[i] > '9')
                return false;
        }

        return true;
    }

    private T SendObsRequest<T>(
        string requestType,
        object requestData)
        where T : class
    {
        string data = requestData == null
            ? "{}"
            : JsonConvert.SerializeObject(requestData);

        string response = CPH.ObsSendRaw(
            requestType,
            data,
            0
        );

        if (string.IsNullOrWhiteSpace(response))
        {
            throw new InvalidOperationException(
                requestType + " returned an empty response."
            );
        }

        T parsed = JsonConvert.DeserializeObject<T>(response);

        if (parsed == null)
        {
            throw new InvalidOperationException(
                requestType + " returned an invalid response."
            );
        }

        return parsed;
    }

    private void SendObsCommand(
        string requestType,
        string data)
    {
        CPH.ObsSendRaw(
            requestType,
            data,
            0
        );
    }

    private bool ValidateObsTarget(
        ObsTargetCandidate target,
        out ObsTargetCandidate validatedTarget)
    {
        validatedTarget = null;

        if (target == null ||
            string.IsNullOrWhiteSpace(target.SourceName) ||
            string.IsNullOrWhiteSpace(target.ContainerName) ||
            (target.ContainerType != OBS_CONTAINER_SCENE &&
             target.ContainerType != OBS_CONTAINER_GROUP) ||
            target.SceneItemId < 0)
        {
            return false;
        }

        if (!TryGetReactPlayerSourceUrl(
            target.SourceName,
            out string currentUrl))
        {
            return false;
        }

        string requestType = target.ContainerType == OBS_CONTAINER_GROUP
            ? "GetGroupSceneItemList"
            : "GetSceneItemList";

        ObsSceneItemListResponse itemList =
            SendObsRequest<ObsSceneItemListResponse>(
                requestType,
                new
                {
                    sceneName = target.ContainerName
                }
            );

        if (itemList.sceneItems == null)
        {
            throw new InvalidOperationException(
                requestType +
                " did not return a sceneItems array."
            );
        }

        for (int i = 0; i < itemList.sceneItems.Count; i++)
        {
            ObsSceneItemInfo item = itemList.sceneItems[i];

            if (item != null &&
                item.sceneItemId.HasValue &&
                item.sceneItemId.Value == target.SceneItemId &&
                string.Equals(
                    item.sourceName,
                    target.SourceName,
                    StringComparison.Ordinal))
            {
                validatedTarget = new ObsTargetCandidate
                {
                    SourceName = target.SourceName,
                    ContainerName = target.ContainerName,
                    ContainerType = target.ContainerType,
                    SceneItemId = target.SceneItemId,
                    Url = currentUrl
                };

                return true;
            }
        }

        return false;
    }

    private bool TryGetValidatedObsTargetForOperation(
        string operationDescription,
        out ObsTargetCandidate validatedTarget)
    {
        validatedTarget = null;

        if (GetObsDetectionStatus() != OBS_STATUS_READY)
        {
            CPH.LogWarn(
                "[React Player] Cannot " +
                operationDescription +
                " because no validated OBS target is ready."
            );

            return false;
        }

        if (!TryLoadSavedObsTarget(
            out ObsTargetCandidate savedTarget))
        {
            RefreshInvalidObsTarget(operationDescription);
            return false;
        }

        try
        {
            if (ValidateObsTarget(
                savedTarget,
                out validatedTarget))
            {
                SetObsTargetFields(validatedTarget);
                return true;
            }
        }
        catch (Exception)
        {
            // A fresh scan below reports an OBS-wide failure once, or updates
            // the target state if only the saved source/container changed.
        }

        RefreshInvalidObsTarget(operationDescription);
        validatedTarget = null;
        return false;
    }

    private void RefreshInvalidObsTarget(
        string operationDescription)
    {
        bool ready = DetectObsTarget();

        if (!ready)
        {
            SetObsDependentStateUnavailable();
        }

        if (GetObsDetectionStatus() != OBS_STATUS_ERROR)
        {
            CPH.LogWarn(
                "[React Player] Cannot " +
                operationDescription +
                " because the saved OBS target changed. Detection was refreshed; retry after choosing a target if needed."
            );
        }
    }

    private bool GetSceneItemEnabled(
        ObsTargetCandidate target)
    {
        ObsSceneItemEnabledResponse enabledResponse =
            SendObsRequest<ObsSceneItemEnabledResponse>(
                "GetSceneItemEnabled",
                new
                {
                    sceneName = target.ContainerName,
                    sceneItemId = target.SceneItemId
                }
            );

        if (!enabledResponse.sceneItemEnabled.HasValue)
        {
            throw new InvalidOperationException(
                "GetSceneItemEnabled did not return sceneItemEnabled."
            );
        }

        return enabledResponse.sceneItemEnabled.Value;
    }

    private void SetSceneItemEnabled(
        ObsTargetCandidate target,
        bool enabled)
    {
        string data = JsonConvert.SerializeObject(new
        {
            sceneName = target.ContainerName,
            sceneItemId = target.SceneItemId,
            sceneItemEnabled = enabled
        });

        SendObsCommand(
            "SetSceneItemEnabled",
            data
        );
    }

    private bool TryGetObsTargetArgument(
        out ObsTargetCandidate target)
    {
        target = null;

        if (!CPH.TryGetArg("sourceName", out string sourceName) ||
            !CPH.TryGetArg("containerName", out string containerName) ||
            !CPH.TryGetArg("containerType", out string containerType) ||
            !TryGetLongArgument("sceneItemId", out long sceneItemId) ||
            string.IsNullOrWhiteSpace(sourceName) ||
            string.IsNullOrWhiteSpace(containerName) ||
            string.IsNullOrWhiteSpace(containerType) ||
            sceneItemId < 0)
        {
            return false;
        }

        containerType = containerType.Trim().ToLowerInvariant();

        if (containerType != OBS_CONTAINER_SCENE &&
            containerType != OBS_CONTAINER_GROUP)
        {
            return false;
        }

        target = new ObsTargetCandidate
        {
            SourceName = sourceName,
            ContainerName = containerName,
            ContainerType = containerType,
            SceneItemId = sceneItemId,
            Url = ""
        };

        return true;
    }

    private bool TryGetLongArgument(
        string argumentName,
        out long value)
    {
        if (CPH.TryGetArg(argumentName, out value))
            return true;

        if (CPH.TryGetArg(argumentName, out int intValue))
        {
            value = intValue;
            return true;
        }

        if (CPH.TryGetArg(argumentName, out string rawValue) &&
            long.TryParse(rawValue, out value))
        {
            return true;
        }

        value = -1L;
        return false;
    }

    private bool TryLoadSavedObsTarget(
        out ObsTargetCandidate target)
    {
        target = null;

        string sourceName = CPH.GetGlobalVar<string>(
            "ReactObsSource",
            true
        );

        string containerName = CPH.GetGlobalVar<string>(
            "ReactObsContainer",
            true
        );

        string containerType = CPH.GetGlobalVar<string>(
            "ReactObsContainerType",
            true
        );

        string sourceUrl = CPH.GetGlobalVar<string>(
            "ReactObsSourceUrl",
            true
        );

        if (!TryGetObsSceneItemId(out long sceneItemId) ||
            string.IsNullOrWhiteSpace(sourceName) ||
            string.IsNullOrWhiteSpace(containerName) ||
            string.IsNullOrWhiteSpace(containerType) ||
            sceneItemId < 0)
        {
            return false;
        }

        containerType = containerType.Trim().ToLowerInvariant();

        if (containerType != OBS_CONTAINER_SCENE &&
            containerType != OBS_CONTAINER_GROUP)
        {
            return false;
        }

        target = new ObsTargetCandidate
        {
            SourceName = sourceName,
            ContainerName = containerName,
            ContainerType = containerType,
            SceneItemId = sceneItemId,
            Url = sourceUrl ?? ""
        };

        return true;
    }

    private bool TryGetObsSceneItemId(out long value)
    {
        try
        {
            long? storedValue = CPH.GetGlobalVar<long?>(
                "ReactObsSceneItemId",
                true
            );

            if (storedValue.HasValue)
            {
                value = storedValue.Value;
                return true;
            }
        }
        catch (Exception)
        {
            // Older/manual globals may have been stored as text.
        }

        string rawValue = CPH.GetGlobalVar<string>(
            "ReactObsSceneItemId",
            true
        );

        return long.TryParse(rawValue, out value);
    }

    private void SaveReadyObsTarget(
        ObsTargetCandidate target,
        List<ObsTargetCandidate> candidates)
    {
        SetObsTargetFields(target);
        SaveObsCandidates(candidates);
        SetObsDetectionStatus(OBS_STATUS_READY);
    }

    private void SetObsTargetFields(
        ObsTargetCandidate target)
    {
        CPH.SetGlobalVar(
            "ReactObsSource",
            target == null ? "" : target.SourceName ?? "",
            true
        );

        CPH.SetGlobalVar(
            "ReactObsContainer",
            target == null ? "" : target.ContainerName ?? "",
            true
        );

        CPH.SetGlobalVar(
            "ReactObsContainerType",
            target == null ? "" : target.ContainerType ?? "",
            true
        );

        CPH.SetGlobalVar(
            "ReactObsSceneItemId",
            target == null ? -1L : target.SceneItemId,
            true
        );

        CPH.SetGlobalVar(
            "ReactObsSourceUrl",
            target == null ? "" : target.Url ?? "",
            true
        );
    }

    private void ClearObsTargetFields()
    {
        SetObsTargetFields(null);
    }

    private void SaveObsCandidates(
        List<ObsTargetCandidate> candidates)
    {
        CPH.SetGlobalVar(
            "ReactObsCandidates",
            JsonConvert.SerializeObject(
                candidates ?? new List<ObsTargetCandidate>()
            ),
            true
        );
    }

    private string GetObsDetectionStatus()
    {
        string status = CPH.GetGlobalVar<string>(
            "ReactObsDetectionStatus",
            true
        );

        if (string.IsNullOrWhiteSpace(status))
            return OBS_STATUS_UNKNOWN;

        return status.Trim().ToLowerInvariant();
    }

    private void SetObsDetectionStatus(string status)
    {
        CPH.SetGlobalVar(
            "ReactObsDetectionStatus",
            status,
            true
        );
    }

    private bool IsKnownObsStatus(string status)
    {
        return status == OBS_STATUS_UNKNOWN ||
            status == OBS_STATUS_DETECTING ||
            status == OBS_STATUS_READY ||
            status == OBS_STATUS_NOT_FOUND ||
            status == OBS_STATUS_NOT_PLACED ||
            status == OBS_STATUS_AMBIGUOUS ||
            status == OBS_STATUS_ERROR;
    }

    private ObsTargetCandidate FindMatchingObsTarget(
        ObsTargetCandidate target,
        List<ObsTargetCandidate> candidates)
    {
        if (target == null || candidates == null)
            return null;

        for (int i = 0; i < candidates.Count; i++)
        {
            ObsTargetCandidate candidate = candidates[i];

            if (candidate != null &&
                candidate.SceneItemId == target.SceneItemId &&
                string.Equals(
                    candidate.SourceName,
                    target.SourceName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    candidate.ContainerName,
                    target.ContainerName,
                    StringComparison.Ordinal) &&
                string.Equals(
                    candidate.ContainerType,
                    target.ContainerType,
                    StringComparison.Ordinal))
            {
                return candidate;
            }
        }

        return null;
    }

    private int CompareObsCandidates(
        ObsTargetCandidate first,
        ObsTargetCandidate second)
    {
        if (object.ReferenceEquals(first, second))
            return 0;

        if (first == null)
            return -1;

        if (second == null)
            return 1;

        int firstTypeOrder = GetObsContainerTypeOrder(
            first.ContainerType
        );

        int secondTypeOrder = GetObsContainerTypeOrder(
            second.ContainerType
        );

        int comparison = firstTypeOrder.CompareTo(secondTypeOrder);

        if (comparison != 0)
            return comparison;

        comparison = string.Compare(
            first.ContainerName ?? "",
            second.ContainerName ?? "",
            StringComparison.Ordinal
        );

        if (comparison != 0)
            return comparison;

        comparison = string.Compare(
            first.SourceName ?? "",
            second.SourceName ?? "",
            StringComparison.Ordinal
        );

        if (comparison != 0)
            return comparison;

        return first.SceneItemId.CompareTo(
            second.SceneItemId
        );
    }

    private int GetObsContainerTypeOrder(string containerType)
    {
        if (containerType == OBS_CONTAINER_SCENE)
            return 0;

        if (containerType == OBS_CONTAINER_GROUP)
            return 1;

        return 2;
    }

    private void SetObsDependentStateUnavailable()
    {
        CPH.SetGlobalVar(
            "ReactPlayerVisible",
            false,
            true
        );

        CPH.SetGlobalVar(
            "ReactPlayerProjectorOpen",
            false,
            false
        );
    }

    private void HandleObsOperationError(
        string operationDescription,
        Exception ex)
    {
        SetObsDetectionStatus(OBS_STATUS_ERROR);
        SetObsDependentStateUnavailable();

        CPH.LogWarn(
            "[React Player] Could not " +
            operationDescription +
            ": " +
            ex.Message
        );
    }

    private string GetProjectorWindowTitle(string sourceName)
    {
        // Current English OBS source-projector title format:
        // "Projector - Source: <source name>"
        return "Projector - Source: " + sourceName;
    }

    private IntPtr FindPlayerProjectorWindow(string sourceName)
    {
        return FindWindow(
            null,
            GetProjectorWindowTitle(sourceName)
        );
    }

    private void SyncProjectorState()
    {
        bool open = false;

        if (GetObsDetectionStatus() == OBS_STATUS_READY &&
            TryLoadSavedObsTarget(out ObsTargetCandidate target))
        {
            open = FindPlayerProjectorWindow(
                target.SourceName
            ) != IntPtr.Zero;
        }

        CPH.SetGlobalVar(
            "ReactPlayerProjectorOpen",
            open,
            false
        );
    }

    private int GetMainMonitorIndex()
    {
        try
        {
            string response = CPH.ObsSendRaw(
                "GetMonitorList",
                "{}",
                0
            );

            ObsMonitorListResponse monitorResponse =
                JsonConvert.DeserializeObject<ObsMonitorListResponse>(
                    response
                );

            if (monitorResponse != null &&
                monitorResponse.monitors != null)
            {
                // Windows/OBS place the primary display at desktop origin.
                // Your current OBS layout has the main 2560x1440 display at 0,0.
                for (int i = 0; i < monitorResponse.monitors.Count; i++)
                {
                    ObsMonitorInfo monitor =
                        monitorResponse.monitors[i];

                    if (monitor != null &&
                        monitor.monitorPositionX == 0 &&
                        monitor.monitorPositionY == 0)
                    {
                        return monitor.monitorIndex;
                    }
                }

                if (monitorResponse.monitors.Count > 0 &&
                    monitorResponse.monitors[0] != null)
                {
                    return monitorResponse.monitors[0].monitorIndex;
                }
            }
        }
        catch (Exception ex)
        {
            CPH.LogWarn(
                "[React Player] Could not resolve the main monitor: " +
                ex.Message
            );
        }

        return 0;
    }

    private void SyncVisibility()
    {
        if (GetObsDetectionStatus() != OBS_STATUS_READY)
        {
            CPH.SetGlobalVar(
                "ReactPlayerVisible",
                false,
                true
            );

            return;
        }

        if (!TryLoadSavedObsTarget(
            out ObsTargetCandidate target))
        {
            SetObsDetectionStatus(OBS_STATUS_ERROR);
            SetObsDependentStateUnavailable();

            CPH.LogWarn(
                "[React Player] The ready OBS target globals are incomplete. Detect again."
            );

            return;
        }

        try
        {
            bool visible = GetSceneItemEnabled(target);

            CPH.SetGlobalVar(
                "ReactPlayerVisible",
                visible,
                true
            );
        }
        catch (Exception ex)
        {
            HandleObsOperationError(
                "synchronize source visibility",
                ex
            );
        }
    }

    private void SendPlayerCommand(
        string command,
        int? value = null,
        string provider = null,
        string contentId = null,
        string url = null)
    {
        PlayerCommand playerCommand = new PlayerCommand
        {
            Command = command,
            Nonce = Guid.NewGuid().ToString("N"),
            Value = value,
            Provider = provider,
            ContentId = contentId,
            Url = url
        };

        CPH.SetGlobalVar(
            "ReactPlayerCommand",
            JsonConvert.SerializeObject(playerCommand),
            false
        );
    }

    private bool OpenPlaybackPreferences()
    {
        if (HasCurrentPlayback())
        {
            CPH.LogWarn(
                "[React Player] Stop the current media before opening playback preferences."
            );

            return false;
        }

        if (!CPH.TryGetArg("id", out string id) ||
            string.IsNullOrWhiteSpace(id))
        {
            CPH.LogWarn(
                "[React Player] Open playback preferences is missing request ID."
            );

            return false;
        }

        id = id.Trim();

        MediaRequest request = FindInList(
            LoadList("ReactQueue"),
            id
        );

        if (request == null)
        {
            request = FindInList(
                LoadList("ReactHistory"),
                id
            );
        }

        if (request == null)
        {
            CPH.LogWarn(
                "[React Player] Playback preferences request not found in ReactQueue or ReactHistory: " +
                id
            );

            return false;
        }

        string provider = string.IsNullOrWhiteSpace(
                request.Provider)
            ? ""
            : request.Provider.Trim().ToLowerInvariant();

        if (!IsSupportedPlaybackPreferencesProvider(provider))
        {
            CPH.LogWarn(
                "[React Player] Playback preferences do not support provider: " +
                provider
            );

            return false;
        }

        string contentId = string.IsNullOrWhiteSpace(
                request.ContentId)
            ? ""
            : request.ContentId.Trim();

        string url = !string.IsNullOrWhiteSpace(request.Url)
            ? request.Url.Trim()
            : !string.IsNullOrWhiteSpace(request.OriginalUrl)
                ? request.OriginalUrl.Trim()
                : "";

        if (string.IsNullOrWhiteSpace(contentId) ||
            string.IsNullOrWhiteSpace(url))
        {
            CPH.LogWarn(
                "[React Player] Playback preferences request is missing its content ID or URL: " +
                id
            );

            return false;
        }

        SendPlayerCommand(
            "openpreferences",
            null,
            provider,
            contentId,
            url
        );

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesActive",
            true,
            false
        );

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesProvider",
            provider,
            false
        );

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesRequestId",
            id,
            false
        );

        return true;
    }

    private bool HasCurrentPlayback()
    {
        string current = CPH.GetGlobalVar<string>(
            "ReactCurrent",
            true
        );

        if (string.IsNullOrWhiteSpace(current))
            return false;

        return !string.Equals(
            current.Trim(),
            "null",
            StringComparison.OrdinalIgnoreCase
        );
    }

    private bool IsSupportedPlaybackPreferencesProvider(
        string provider)
    {
        return
            provider == "youtube" ||
            provider == "twitch" ||
            provider == "medal" ||
            provider == "tiktok";
    }

    private void EnsurePlaybackPreferencesGlobals()
    {
        bool? active = CPH.GetGlobalVar<bool?>(
            "ReactPlaybackPreferencesActive",
            false
        );

        if (!active.HasValue)
        {
            CPH.SetGlobalVar(
                "ReactPlaybackPreferencesActive",
                false,
                false
            );
        }

        string provider = CPH.GetGlobalVar<string>(
            "ReactPlaybackPreferencesProvider",
            false
        );

        if (provider == null)
        {
            CPH.SetGlobalVar(
                "ReactPlaybackPreferencesProvider",
                "",
                false
            );
        }

        string requestId = CPH.GetGlobalVar<string>(
            "ReactPlaybackPreferencesRequestId",
            false
        );

        if (requestId == null)
        {
            CPH.SetGlobalVar(
                "ReactPlaybackPreferencesRequestId",
                "",
                false
            );
        }
    }

    private void ClosePlaybackPreferences(
        bool sendPlayerCommand)
    {
        if (sendPlayerCommand)
        {
            SendPlayerCommand(
                "closepreferences"
            );
        }

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesActive",
            false,
            false
        );

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesProvider",
            "",
            false
        );

        CPH.SetGlobalVar(
            "ReactPlaybackPreferencesRequestId",
            "",
            false
        );
    }

    private MediaRequest FindRequest(
        string id,
        MediaRequest current,
        List<MediaRequest> queue,
        List<MediaRequest> history)
    {
        if (current != null && current.Id == id)
            return current;

        MediaRequest request = FindInList(queue, id);

        if (request != null)
            return request;

        return FindInList(history, id);
    }

    private MediaRequest FindInList(
        List<MediaRequest> list,
        string id)
    {
        if (list == null || string.IsNullOrWhiteSpace(id))
            return null;

        for (int i = 0; i < list.Count; i++)
        {
            MediaRequest request = list[i];

            if (request != null && request.Id == id)
                return request;
        }

        return null;
    }

    private MediaRequest LoadCurrent()
    {
        string json = CPH.GetGlobalVar<string>(
            "ReactCurrent",
            true
        );

        if (string.IsNullOrWhiteSpace(json))
            return null;

        try
        {
            return JsonConvert.DeserializeObject<MediaRequest>(json);
        }
        catch
        {
            CPH.LogWarn(
                "[React Player] Could not parse ReactCurrent."
            );

            return null;
        }
    }

    private void SaveCurrent(MediaRequest request)
    {
        string json = request == null
            ? "null"
            : JsonConvert.SerializeObject(request);

        CPH.SetGlobalVar(
            "ReactCurrent",
            json,
            true
        );
    }

    private List<MediaRequest> LoadList(string variableName)
    {
        string json = CPH.GetGlobalVar<string>(
            variableName,
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
                "[React Player] Could not parse " +
                variableName +
                "."
            );

            return new List<MediaRequest>();
        }
    }

    private void SaveList(
        string variableName,
        List<MediaRequest> requests)
    {
        CPH.SetGlobalVar(
            variableName,
            JsonConvert.SerializeObject(requests),
            true
        );
    }
}
