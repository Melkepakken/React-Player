using System;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CPHInline
{
    private const int DEFAULT_REQUIRED_VOTES = 3;
    private const string DEFAULT_PROGRESS_MESSAGE =
        "{votes}/{required} votes to skip current content.";

    public class MediaRequest
    {
        public string Id { get; set; }
        public string Title { get; set; }
    }

    public bool Execute()
    {
        CPH.SetArgument(
            "reactVoteSkipAccepted",
            false
        );

        CPH.SetArgument(
            "reactVoteSkipDuplicate",
            false
        );

        CPH.SetArgument(
            "reactVoteSkipPassed",
            false
        );

        CPH.SetArgument(
            "reactVoteSkipCount",
            0
        );

        int requiredVotes =
            ReadRequiredVotes();

        CPH.SetArgument(
            "reactVoteSkipRequired",
            requiredVotes
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

        MediaRequest current =
            LoadCurrent();

        if (current == null ||
            string.IsNullOrWhiteSpace(
                current.Id))
        {
            return true;
        }

        string playerState =
            CPH.GetGlobalVar<string>(
                "ReactPlayerState",
                true
            );

        if (!IsActivePlayerState(
                playerState))
        {
            return true;
        }

        string playbackSessionId =
            CPH.GetGlobalVar<string>(
                "ReactPlaybackSessionId",
                false
            );

        if (string.IsNullOrWhiteSpace(
                playbackSessionId))
        {
            playbackSessionId =
                current.Id +
                ":" +
                Guid.NewGuid().ToString("N");

            CPH.SetGlobalVar(
                "ReactPlaybackSessionId",
                playbackSessionId,
                false
            );
        }

        string voteSessionId =
            CPH.GetGlobalVar<string>(
                "ReactVoteSkipSessionId",
                false
            );

        List<string> voters =
            LoadVoters();

        if (!string.Equals(
                voteSessionId,
                playbackSessionId,
                StringComparison.Ordinal))
        {
            voteSessionId =
                playbackSessionId;

            voters.Clear();

            SaveVoteState(
                voteSessionId,
                voters
            );
        }

        string voterKey =
            GetVoterKey();

        if (string.IsNullOrWhiteSpace(
                voterKey))
        {
            return true;
        }

        if (voters.Contains(voterKey))
        {
            CPH.SetArgument(
                "reactVoteSkipDuplicate",
                true
            );

            CPH.SetArgument(
                "reactVoteSkipCount",
                voters.Count
            );

            return true;
        }

        voters.Add(
            voterKey
        );

        SaveVoteState(
            voteSessionId,
            voters
        );

        int voteCount =
            voters.Count;

        CPH.SetArgument(
            "reactVoteSkipAccepted",
            true
        );

        CPH.SetArgument(
            "reactVoteSkipCount",
            voteCount
        );

        string message =
            BuildProgressMessage(
                voteCount,
                requiredVotes,
                current.Title
            );

        SendProgressMessage(
            message
        );

        if (voteCount >= requiredVotes)
        {
            CPH.SetArgument(
                "reactVoteSkipPassed",
                true
            );

            // Clear immediately so a second command cannot pass the same
            // session again while the Skip helper action is running.
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

        return true;
    }

    private int ReadRequiredVotes()
    {
        int requiredVotes = 0;

        if (!CPH.TryGetArg(
                "reactVoteSkipRequired",
                out requiredVotes))
        {
            string rawRequired = "";

            if (CPH.TryGetArg(
                    "reactVoteSkipRequired",
                    out rawRequired))
            {
                int.TryParse(
                    rawRequired,
                    out requiredVotes
                );
            }
        }

        if (requiredVotes <= 0)
        {
            requiredVotes =
                DEFAULT_REQUIRED_VOTES;
        }

        return Math.Max(
            1,
            Math.Min(
                100,
                requiredVotes
            )
        );
    }

    private string ReadProgressTemplate()
    {
        string template = "";

        CPH.TryGetArg(
            "reactVoteSkipMessage",
            out template
        );

        if (string.IsNullOrWhiteSpace(
                template))
        {
            template =
                DEFAULT_PROGRESS_MESSAGE;
        }

        return template.Trim();
    }

    private MediaRequest LoadCurrent()
    {
        string json =
            CPH.GetGlobalVar<string>(
                "ReactCurrent",
                true
            );

        if (string.IsNullOrWhiteSpace(json) ||
            string.Equals(
                json,
                "null",
                StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        try
        {
            return
                JsonConvert.DeserializeObject<MediaRequest>(
                    json
                );
        }
        catch
        {
            return null;
        }
    }

    private List<string> LoadVoters()
    {
        string json =
            CPH.GetGlobalVar<string>(
                "ReactVoteSkipVoters",
                false
            );

        if (string.IsNullOrWhiteSpace(
                json))
        {
            return new List<string>();
        }

        try
        {
            return
                JsonConvert.DeserializeObject<List<string>>(
                    json
                ) ??
                new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private void SaveVoteState(
        string sessionId,
        List<string> voters)
    {
        CPH.SetGlobalVar(
            "ReactVoteSkipSessionId",
            sessionId ?? "",
            false
        );

        CPH.SetGlobalVar(
            "ReactVoteSkipVoters",
            JsonConvert.SerializeObject(
                voters ??
                new List<string>()
            ),
            false
        );

        CPH.SetGlobalVar(
            "ReactVoteSkipCount",
            voters == null
                ? 0
                : voters.Count,
            false
        );
    }

    private string GetVoterKey()
    {
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
            (source ?? "unknown")
                .Trim()
                .ToLowerInvariant();

        string userId = "";

        CPH.TryGetArg(
            "userId",
            out userId
        );

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            CPH.TryGetArg(
                "userName",
                out userId
            );
        }

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            CPH.TryGetArg(
                "user",
                out userId
            );
        }

        if (string.IsNullOrWhiteSpace(
                userId))
        {
            return "";
        }

        return
            source +
            ":" +
            userId
                .Trim()
                .ToLowerInvariant();
    }

    private bool IsActivePlayerState(
        string state)
    {
        string normalized =
            (state ?? "")
                .Trim()
                .ToLowerInvariant();

        return
            normalized == "playing" ||
            normalized == "paused" ||
            normalized == "loading";
    }

    private string BuildProgressMessage(
        int votes,
        int required,
        string title)
    {
        string message =
            ReadProgressTemplate();

        int remaining =
            Math.Max(
                0,
                required - votes
            );

        return message
            .Replace(
                "{votes}",
                votes.ToString()
            )
            .Replace(
                "{required}",
                required.ToString()
            )
            .Replace(
                "{remaining}",
                remaining.ToString()
            )
            .Replace(
                "{title}",
                string.IsNullOrWhiteSpace(title)
                    ? "current content"
                    : title.Trim()
            );
    }

    private void SendProgressMessage(
        string message)
    {
        if (string.IsNullOrWhiteSpace(
                message))
        {
            return;
        }

        string source = "";

        CPH.TryGetArg(
            "commandSource",
            out source
        );

        source =
            (source ?? "")
                .Trim()
                .ToLowerInvariant();

        if (source == "youtube")
        {
            CPH.SendYouTubeMessageToLatestMonitored(
                message,
                true,
                true
            );

            return;
        }

        if (source == "kick")
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
}
