using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

namespace ZexQoLMenu
{
    /// <summary>
    /// Server browser and room discovery system
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // SERVER BROWSER
        // ============================================================
        protected readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
        protected Vector2 serverListScroll = Vector2.zero;
        protected string serverListStatus = "IDLE";
        protected bool isBrowsingServers;
        protected bool pendingRejoinPrevious;
        protected string selectedRoomName = "";
        protected string previousRoomName = "";
        protected string pendingJoinRoomName = "";
        protected bool joinPendingInProgress;
        protected float lastRoomListUpdateTime;
        protected Coroutine rejoinCoroutine;

        // Server filters / favorites
        protected string serverNameFilter = "";
        protected bool serverNameFilterFocused;
        protected bool serverFilterOpenOnly = true;
        protected int serverFilterMinPlayers; // 0 = any
        protected int serverFilterMaxPlayers = 255; // 255 = any
        protected readonly HashSet<string> favoriteRoomNames = new HashSet<string>();
        protected bool serverShowFavoritesOnly;

        // Remember where you were before REFRESH left the room, restore on auto-rejoin
        protected bool browseHasSavedTransform;
        protected Vector3 browseSavedPosition;
        protected Quaternion browseSavedRotation;
        protected string browseSavedPrefabName = "";
        protected int browseSavedSpeciesIndex = -1;
        protected object browseSavedGenes;
        protected Coroutine restoreBrowsePositionCoroutine;
        
        // Continuous re-apply window (game spawn snap often wins a one-shot teleport)
        protected bool browseRestoreActive;
        protected float browseRestoreUntil;
        protected int browseRestoreHits;
        protected bool browseDidRespawnRestore;
        protected bool browseTriedRespawnRestore;
        
        // Toggle: after server-browser rejoin, restore saved position (Teleport tab)
        protected bool browsePositionRestoreEnabled = true;

        // Auto water-splash on join (fixes common visual/physics desync)
        protected bool autoSplashOnJoin = true;
        protected string autoSplashStatus = "";
        protected float autoSplashStatusUntil;
        protected float nextAutoSplashAllowed;
    }
}
