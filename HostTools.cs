using System;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

namespace ZexQoLMenu
{
    /// <summary>
    /// Host moderation tools: kick/ban system and room management
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // HOST TOOLS
        // ============================================================
        protected bool kickConfirmationVisible;
        protected Player pendingKickPlayer;

        protected bool kickAllConfirmationVisible;
        protected float kickAllCooldownUntil;
        protected const float KickAllCooldownSeconds = 3f;

        protected bool banConfirmationVisible;
        protected Player pendingBanPlayer;

        // Ban list: kicked players are tracked by Photon UserId (falls back to nickname)
        // so a rejoin attempt from the same account can be rejected by the host.
        protected readonly HashSet<string> bannedUserIds = new HashSet<string>();
        protected string roomLabelInput = "";
        protected bool roomLabelFocused;
        protected const string RoomLabelPropertyKey = "ZexQoLRoomLabel";
        protected const string RoomPlayersPropertyKey = "ZexQoLPlayers";
        protected Vector2 recentEventsScroll = Vector2.zero;
        protected Vector2 bannedListScroll = Vector2.zero;

        // QoL: destroy local body before leaving a room (cuts leftover corpses)
        protected bool destroyBodyOnLeave = true;

        // Host publishes player names into room props so browser can hover-preview
        protected bool publishRoomPlayers = true;
        protected float nextRoomPlayersPublishTime;
        protected string lastPublishedRoomPlayers = "";

        // Live nickname editor
        protected string nameEditText = "";
        protected bool nameEditFocused;
        protected string nameEditStatus = "";
        protected float nameEditStatusUntil;

        // Server browser hover tip (player list from room props)
        protected string serverHoverRoomName = "";
        protected string serverHoverPlayersText = "";
        protected Vector2 serverHoverGuiPos;

        // Room player scanner — single coroutine owns leave/join/read/leave/rejoin
        protected readonly Dictionary<string, string> peekedRoomPlayers = new Dictionary<string, string>();
        protected readonly Queue<string> scanQueue = new Queue<string>();
        protected bool scanRunning;
        protected bool scanAbort;
        protected string scanCurrentRoom = "";
        protected string peekStatus = "";
        protected float peekStatusUntil;
        protected string scanHomeRoom = "";
        protected bool scanShouldRejoinHome;
        protected Coroutine scanCoroutine;
        protected string scanLastError = "";
        
        // Kept for any leftover references
        protected bool peekInProgress;
        protected string peekTargetRoom = "";
        protected bool peekSuppressBrowseRestore;
        protected bool peekNeedLobbyAfterLeave;
        protected bool peekAwaitingTargetJoin;
        protected bool peekRejoinAfter;
        protected string peekRejoinRoomName = "";
        protected Coroutine peekCoroutine;

        protected readonly Dictionary<int, string> knownRoomPlayers = new Dictionary<int, string>();
        protected readonly List<string> recentPlayerEvents = new List<string>();
    }
}
