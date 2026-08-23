using BepInEx;
using BepInEx.Configuration;
using ExitGames.Client.Photon;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace ZexQoLMenu
{
    [BepInPlugin("Zex_QOL_Menu", "Zex's QOL Menu", "1.0.0")]
    public class Plugin : BaseUnityPlugin, IConnectionCallbacks, ILobbyCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
    {
        // ============================================================
        // MASTER UI
        // ============================================================
        private bool menuVisible = true;
        private int tab = 0; // 0 ESP, 1 SPAWNER, 2 TELEPORT, 3 HOST TOOLS, 4 MISC, 5 SERVERS, 6 SILLYS, 7 HOST LOGS, 8 GENES, 9 TESTING, 10 SETTINGS, 11 QOL, 12 MODS
        private Rect menuRect = new Rect(90f, 45f, 1000f, 780f);

        // ============================================================
        // GENES (local kobold character stats — reflection-based)
        // ============================================================
        private class GeneFieldDef
        {
            public string Label;
            public string[] FieldNames; // tried in order
            public float DefaultValue;
            public float Min;
            public float Max;

            public GeneFieldDef(string label, string[] names, float def, float min, float max)
            {
                Label = label;
                FieldNames = names;
                DefaultValue = def;
                Min = min;
                Max = max;
            }
        }

        private readonly GeneFieldDef[] geneFieldDefs =
        {
            // Official KoboldGenes field names (github.com/naelstrof/KoboldKare)
            new GeneFieldDef("Energy",    new[] { "maxEnergy" }, 5f, 0.1f, 100f),
            new GeneFieldDef("Belly",    new[] { "bellySize" }, 20f, 0f, 200f),
            new GeneFieldDef("Meta",     new[] { "metabolizeCapacitySize" }, 20f, 0f, 200f),
            new GeneFieldDef("Grab",     new[] { "grabCount" }, 1f, 0f, 20f),
            new GeneFieldDef("Size",     new[] { "baseSize" }, 20f, 0.1f, 200f),
            new GeneFieldDef("Tits",     new[] { "breastSize" }, 0f, 0f, 200f),
            new GeneFieldDef("Fat",      new[] { "fatSize" }, 0f, 0f, 200f),
            new GeneFieldDef("Psize",    new[] { "dickSize" }, 10f, 0f, 200f),
            new GeneFieldDef("Balls",    new[] { "ballSize" }, 10f, 0f, 200f),
            new GeneFieldDef("Hue",      new[] { "hue" }, 0f, 0f, 255f),
            new GeneFieldDef("Bright",   new[] { "brightness" }, 128f, 0f, 255f),
            new GeneFieldDef("Satur",    new[] { "saturation" }, 128f, 0f, 255f),
            new GeneFieldDef("Cloth Hue", new[] { "clothingHue" }, 0f, 0f, 255f),
            new GeneFieldDef("Thick",    new[] { "dickThickness" }, 0.5f, 0f, 3f),
        };

        private readonly float[] geneCurrent = new float[14];
        private readonly float[] geneToSet = new float[14];
        private readonly string[] geneToSetText = new string[14];
        private int geneEditIndex = -1;
        private string geneStatus = "Not loaded";
        private float geneStatusUntil;
        private Type koboldType;
        private Type koboldGenesType;
        private MethodInfo getGenesMethod;
        private MethodInfo setGenesMethod;
        private bool geneTypesResolved;
        private Vector2 genesScroll = Vector2.zero;

        // Dick / species / thickness
        private readonly List<string> dickOptions = new List<string>();
        private int selectedDickIndex;
        private Vector2 dickScroll = Vector2.zero;
        private float cockThickness = 0.7f;
        private int speciesId;
        private string speciesName = "";
        private string speciesEditText = "0";
        private bool speciesEditing;

        // Modded character / avatar list (PlayerDatabase / PrefabDatabase)
        private readonly List<string> characterOptions = new List<string>();
        private int selectedCharacterIndex;
        private Vector2 characterScroll = Vector2.zero;
        private string characterFilter = "";
        private bool characterFilterEditing;

        // Equipment / clothing (EquipmentDatabase + KoboldInventory)
        private readonly List<string> equipNames = new List<string>(); // currently worn
        private readonly List<string> equipCatalog = new List<string>(); // all from EquipmentDatabase
        private int selectedCatalogEquip = -1;
        private int selectedWornEquip = -1;
        private Vector2 equipScroll = Vector2.zero;
        private Vector2 equipCatalogScroll = Vector2.zero;
        private string equipStatus = "";
        private string equipFilter = "";
        private bool equipFilterEditing;

        // Presets — full (char+genes+clothes) is primary; old stats/equip kept for compatibility
        private ConfigEntry<string> configStatsPresets;
        private ConfigEntry<string> configEquipPresets;
        private ConfigEntry<string> configFullPresets;
        private readonly List<string> statsPresetNames = new List<string>();
        private readonly Dictionary<string, string> statsPresetData = new Dictionary<string, string>();
        private readonly List<string> equipPresetNames = new List<string>();
        private readonly Dictionary<string, string> equipPresetData = new Dictionary<string, string>();
        private readonly List<string> fullPresetNames = new List<string>();
        private readonly Dictionary<string, string> fullPresetData = new Dictionary<string, string>();
        private string newPresetName = "MyPreset";
        private bool presetNameEditing;
        private Vector2 presetScroll = Vector2.zero;
        private int selectedStatsPreset = -1;
        private int selectedEquipPreset = -1;
        private int selectedFullPreset = -1;
        private Coroutine applyFullPresetCoroutine;

        // Presets pop-out window (avoids crushing the genes panel layout)
        private bool presetsPopupVisible;
        private Rect presetsPopupRect = new Rect(420f, 120f, 360f, 480f);
        private float equipStatusUntil;

        // Waypoints pop-out (opened from Teleport tab)
        private bool waypointsPopupVisible;
        private Rect waypointsPopupRect = new Rect(420f, 160f, 340f, 420f);

        // Modern menu shell inspired by the supplied Neverlose-style reference.
        private GUIStyle windowStyle;
        private GUIStyle sidebarStyle;
        private GUIStyle sidebarSelectedStyle;
        private GUIStyle topBarStyle;
        private GUIStyle cardStyle;
        private GUIStyle sectionStyle;
        private GUIStyle valueStyle;
        private GUIStyle accentLabelStyle;
        private GUIStyle modernButtonStyle;
        private GUIStyle modernSelectedButtonStyle;
        private GUIStyle modernSmallStyle;
        private Texture2D uiWindowTexture;
        private Texture2D uiSidebarTexture;
        private Texture2D uiCardTexture;
        private Texture2D uiButtonTexture;
        private Texture2D uiButtonHoverTexture;
        private Texture2D uiButtonActiveTexture;
        private Texture2D uiAccentTexture;
        private Texture2D uiMutedTexture;
        private bool sillysNameEditing;
        private string sillysName = "Someone";

        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle selectedButtonStyle;
        private GUIStyle headerStyle;
        private GUIStyle smallStyle;
        private GUIStyle overlayHeaderStyle;
        private GUIStyle overlayPlayerStyle;
        private GUIStyle overlayInfoStyle;
        private GUIStyle overlayRoleStyle;
        private GUIStyle overlayServerStyle;
        private bool stylesCreated;

        // Background from the ESP mod.
        private Texture2D menuBackground;
        private Material backgroundMaterial;

        // Background appearance controls
        private float backgroundHue = 0f;
        private float backgroundOpacity = 1f;
        // true = greyscale chrome (no saturation)
        private bool menuColorGreyscale;
        // true = hue slowly cycles; false = stay on current hue (LOCK)
        private bool menuHueCycling = true;
        // Full rainbow loop duration in seconds while RGB CYCLE is on
        private float menuHueCycleSeconds = 20f;
        private const float MenuHueCycleSecondsMin = 5f;
        private const float MenuHueCycleSecondsMax = 120f;

        private const float MaxMoneyValue = 999999f;
        private const int MaxStarsValue = 999999;
        private string rewardStatus = "";
        private float rewardStatusUntil;

        // Animated sprite-sheet background
        private const int BackgroundColumns = 5;
        private const int BackgroundRows = 18;
        private const int BackgroundFrameCount = BackgroundColumns * BackgroundRows;
        private float BackgroundFramesPerSecond = 24f;

        // ===========================================================
        // ESP
        // ============================================================
        private bool showNames = true;
        private bool showDistance = true;
        private bool showActorID = false;
        private bool hideSelf = true;
        private bool tracersEnabled = false;
        private bool visibilityCheck = false;
        private bool offscreenArrows = true;
        private float maxDistance = 240f;
        private float tracerThickness = 1.0f;
        private int tracerOrigin = 0;
        private bool tracerDistanceFade = true;
        private bool scaleNames = true;
        private const float NameHeight = 1.7f;
        private const float MinNameScale = 0.70f;
        private const float MaxNameScale = 1.15f;
        private const float OffscreenArrowSize = 18f;
        private const float OffscreenArrowMargin = 35f;
        private readonly Color playerColor = new Color(1f, .2f, 0f);
        private readonly Color hiddenColor = new Color(.55f, .55f, .60f);
        private GUIStyle espStyle;
        private Material tracerMaterial;
        // Track current esp font size because GUIStyle.fontSize has no getter.
        private int espFontSize = 14;

        // ============================================================
        // QOL / STATUS / SPECTATE / PLAYER COLORS
        // ============================================================
        private float fpsValue;
        private float objectCountTimer;
        private int sceneObjectCount;
        private readonly HashSet<int> friendActorIds = new HashSet<int>();

        private bool spectating;
        private Transform spectateTarget;
        private Vector3 savedCameraPosition;
        private static Plugin Instance;
        private GameObject cachedLocalPlayer;
        private float nextPlayerCacheRefresh;
        private const float PlayerCacheRefreshInterval = 1f;
        private Harmony spectateHarmony;
        private Quaternion savedCameraRotation;
        private bool cameraStateSaved;
        private float spectateCameraHeight = 0.85f; // Adjustable camera height (0.0 - 5.0 m)
        private float spectateCameraDistance = 3.25f; // Adjustable camera distance (1.0 - 10.0 m)
        private int spectateActorId = -1; // Actor ID of the player being spectated
        private float spectateCameraRotation; // Adjustable rotation around player (0 - 360 degrees)

        private float spectateYaw;
        private float spectatePitch = 10f;
        private const float SpectateMouseSensitivity = 3f;
        private const float SpectateMinPitch = -75f;
        private const float SpectateMaxPitch = 75f;

        private int normalColorIndex;
        private int selectedColorIndex = 1;
        private int friendColorIndex = 2;

        private readonly Color[] espColorOptions =
        {
            new Color(1f, .2f, 0f),
            new Color(.75f, .25f, 1f),
            new Color(.2f, 1f, .35f),
            new Color(1f, .85f, .15f),
            new Color(.2f, .75f, 1f),
            new Color(1f, .35f, .65f),
            Color.white
        };

        private readonly string[] espColorNames =
        {
            "RED", "PURPLE", "GREEN", "YELLOW", "CYAN", "PINK", "WHITE"
        };

        // ============================================================
        // SPAWNER
        // ============================================================
        private class PrefabEntry
        {
            public string Name;
            public GameObject Prefab;
            public PrefabEntry(string name, GameObject prefab) { Name = name; Prefab = prefab; }
        }

        private readonly List<PrefabEntry> prefabList = new List<PrefabEntry>();
        private readonly List<PrefabEntry> filteredPrefabList = new List<PrefabEntry>();
        private int selectedPrefabIndex = -1;
        private int prefabListOffset;
        private string prefabStatus = "NOT SCANNED";
        private string searchText = "";
        private bool searchFocused;
        private int amount = 1;
        private float spawnDistance = 2f;
        private string spawnStatus = "WAITING";
        private readonly List<GameObject> spawnedObjects = new List<GameObject>();
        private Type preparePoolType;
        private FieldInfo preparePoolInstanceField;
        private FieldInfo dynamicPrefabsField;
        private readonly HashSet<string> favoritePrefabNames = new HashSet<string>();

        // ============================================================
        // TELEPORT
        // ============================================================
        private Vector2 playerScroll = Vector2.zero;
        private int selectedActorId = -1;
        private Player selectedPlayer;
        private float behindDistance = 3f;
        private float frontDistance = 3f;
        private float aboveDistance = 4f;
        private bool originCaptured;
        private Vector3 originPosition = Vector3.zero;

        private readonly Dictionary<string, Vector3> savedWaypoints =
            new Dictionary<string, Vector3>();
        private string newWaypointName = "";
        private bool waypointNameFocused;
        private Vector2 waypointListScroll = Vector2.zero;
        private int waypointAutoIndex = 1;

        // Soft teleport (lerp)
        private bool softTeleportEnabled = true;
        private float softTeleportDuration = 0.35f;
        private Coroutine softTeleportCoroutine;

        // Flying noclip — CharCon-style: Kobold.body.velocity + OrbitCamera look
        private bool flyingNoclipActive;
        private float flySpeed = 25f;
        private Vector3 flySavedPosition;
        private FieldInfo koboldBodyField;
        private int lastHotkeyFrame = -1;
        private Rigidbody flyCachedBody;
        private Behaviour flyCachedKoboldController;
        private bool flyKoboldControllerWasEnabled = true;
        private string flyDebugStatus = "";
        private Vector3 flyPendingVelocity;
        private bool flyHasPendingVelocity;

        // TESTING: ownership + world ping marker
        private string ownershipStatus = "";
        private float ownershipStatusUntil;
        private bool pingMarkActive;
        private Vector3 pingMarkWorld;
        private float pingMarkUntil;
        private const float PingMarkDuration = 8f;

        // Host freeze (best-effort: keep shoving target back to locked pos)
        private readonly Dictionary<int, Vector3> frozenPlayerPositions = new Dictionary<int, Vector3>();
        private readonly HashSet<int> frozenActorIds = new HashSet<int>();

        private readonly Dictionary<string, string> playerNotes = new Dictionary<string, string>();
        private string notesInput = "";
        private bool notesInputFocused;

        // Global search + collapsible sections
        private string globalSearchText = "";
        private bool globalSearchFocused;
        private readonly Dictionary<string, bool> sectionCollapsed = new Dictionary<string, bool>();

        // Keybinds — CharCon-style: UnityInput.Current for KeyCode / held strings;
        // KeyboardShortcut only for multi-key (menu), evaluated via UnityInput too.
        private ConfigEntry<KeyCode> noclipToggleKey;
        private ConfigEntry<KeyCode> waypointQuickSaveKey;
        private ConfigEntry<KeyCode> flySpeedUpKey;
        private ConfigEntry<KeyCode> flySpeedDownKey;
        private ConfigEntry<KeyCode> spectateNextKey;
        private ConfigEntry<KeyCode> spectatePrevKey;
        private bool waitingForKeyRebind;
        private string rebindTarget; // "menu" | "noclip" | "waypoint" | "flyUp" | "flyDown" | "specNext" | "specPrev"

        // ============================================================
        // PLAYER OVERLAY
        // ============================================================
        private Rect playerOverlayRect = new Rect(10f, 10f, 300f, 220f);
        private Vector2 playerOverlayScroll = Vector2.zero;
        private bool showPlayerOverlay = true;

        // ============================================================
        // PLAYER CONTEXT MENU / RADAR
        // ============================================================
        private Player contextPlayer;
        private bool playerContextMenuVisible;
        private Vector2 playerContextMenuPosition;
        private bool targetLocked;

        private int followPlayerActorId = -1;
        private float followDistance = 3f;
        private float followHeight = 0.5f;

        private Rect playerRadarRect = new Rect(20f, 250f, 230f, 230f);
        private bool showPlayerRadar = true;
        private bool radarRotateWithCamera = true;
        private bool radarShowNames = true;
        private bool radarShowDistance = true;

        private float radarRange = 30f;
        private const float RadarMinSize = 170f;
        private const float RadarMaxSize = 360f;

        // ============================================================
        // CONFIGURATION
        // ============================================================
        private ConfigEntry<KeyboardShortcut> menuToggleKey;

        private ConfigEntry<float> configCameraHeight;
        private ConfigEntry<float> configCameraDistance;
        private ConfigEntry<float> configCameraRotation;

        private ConfigEntry<float> configBackgroundHue;
        private ConfigEntry<float> configBackgroundOpacity;
        private ConfigEntry<float> configBackgroundFPS;
        private ConfigEntry<bool> configMenuGreyscale;

        private ConfigEntry<string> configBannedUserIds;
        private ConfigEntry<string> configFavoritePrefabNames;
        private ConfigEntry<string> configFavoriteRoomNames;
        private ConfigEntry<bool> configSoftTeleport;
        private ConfigEntry<float> configFlySpeed;

        // ============================================================
        // HOST TOOLS
        // ============================================================
        private bool kickConfirmationVisible;
        private Player pendingKickPlayer;

        private bool kickAllConfirmationVisible;
        private float kickAllCooldownUntil;
        private const float KickAllCooldownSeconds = 3f;

        private bool banConfirmationVisible;
        private Player pendingBanPlayer;

        // Ban list: kicked players are tracked by Photon UserId (falls back to nickname)
        // so a rejoin attempt from the same account can be rejected by the host.
        private readonly HashSet<string> bannedUserIds = new HashSet<string>();
        private string roomLabelInput = "";
        private bool roomLabelFocused;
        private const string RoomLabelPropertyKey = "ZexQoLRoomLabel";
        private const string RoomPlayersPropertyKey = "ZexQoLPlayers";
        private Vector2 recentEventsScroll = Vector2.zero;
        private Vector2 bannedListScroll = Vector2.zero;

        // QoL: destroy local body before leaving a room (cuts leftover corpses)
        private bool destroyBodyOnLeave = true;
        private ConfigEntry<bool> configDestroyBodyOnLeave;

        // Host publishes player names into room props so browser can hover-preview
        private bool publishRoomPlayers = true;
        private ConfigEntry<bool> configPublishRoomPlayers;
        private float nextRoomPlayersPublishTime;
        private string lastPublishedRoomPlayers = "";

        // Live nickname editor
        private string nameEditText = "";
        private bool nameEditFocused;
        private string nameEditStatus = "";
        private float nameEditStatusUntil;

        // Server browser hover tip (player list from room props)
        private string serverHoverRoomName = "";
        private string serverHoverPlayersText = "";
        private Vector2 serverHoverGuiPos;

        // Room player scanner — single coroutine owns leave/join/read/leave/rejoin
        private readonly Dictionary<string, string> peekedRoomPlayers = new Dictionary<string, string>();
        private readonly Queue<string> scanQueue = new Queue<string>();
        private bool scanRunning;
        private bool scanAbort;
        private string scanCurrentRoom = "";
        private string peekStatus = "";
        private float peekStatusUntil;
        private string scanHomeRoom = "";
        private bool scanShouldRejoinHome;
        private Coroutine scanCoroutine;
        private string scanLastError = "";
        // Kept for any leftover references
        private bool peekInProgress;
        private string peekTargetRoom = "";
        private bool peekSuppressBrowseRestore;
        private bool peekNeedLobbyAfterLeave;
        private bool peekAwaitingTargetJoin;
        private bool peekRejoinAfter;
        private string peekRejoinRoomName = "";
        private Coroutine peekCoroutine;

        private readonly Dictionary<int, string> knownRoomPlayers =
            new Dictionary<int, string>();

        private readonly List<string> recentPlayerEvents =
            new List<string>();

        // ============================================================
        // SERVER BROWSER
        // ============================================================
        private readonly Dictionary<string, RoomInfo> cachedRooms = new Dictionary<string, RoomInfo>();
        private Vector2 serverListScroll = Vector2.zero;
        private string serverListStatus = "IDLE";
        private bool isBrowsingServers;
        private bool pendingRejoinPrevious;
        private string selectedRoomName = "";
        private string previousRoomName = "";
        private string pendingJoinRoomName = "";
        private bool joinPendingInProgress;
        private float lastRoomListUpdateTime;
        private Coroutine rejoinCoroutine;

        // Server filters / favorites
        private string serverNameFilter = "";
        private bool serverNameFilterFocused;
        private bool serverFilterOpenOnly = true;
        private int serverFilterMinPlayers; // 0 = any
        private int serverFilterMaxPlayers = 255; // 255 = any
        private readonly HashSet<string> favoriteRoomNames = new HashSet<string>();
        private bool serverShowFavoritesOnly;

        // Remember where you were before REFRESH left the room, restore on auto-rejoin
        private bool browseHasSavedTransform;
        private Vector3 browseSavedPosition;
        private Quaternion browseSavedRotation;
        private string browseSavedPrefabName = "";
        private int browseSavedSpeciesIndex = -1;
        private object browseSavedGenes;
        private Coroutine restoreBrowsePositionCoroutine;
        // Continuous re-apply window (game spawn snap often wins a one-shot teleport)
        private bool browseRestoreActive;
        private float browseRestoreUntil;
        private int browseRestoreHits;
        private bool browseDidRespawnRestore;
        private bool browseTriedRespawnRestore;
        // Toggle: after server-browser rejoin, restore saved position (Teleport tab)
        private bool browsePositionRestoreEnabled = true;
        private ConfigEntry<bool> configBrowsePositionRestore;

        // Auto water-splash on join (fixes common visual/physics desync)
        private bool autoSplashOnJoin = true;
        private ConfigEntry<bool> configAutoSplashOnJoin;
        private string autoSplashStatus = "";
        private float autoSplashStatusUntil;
        private float nextAutoSplashAllowed;

        // Quick create lobby (Host Tools / Servers)
        // Mods tab (local/Workshop selection + apply)
        private readonly List<string> quickLobbyModTitles = new List<string>();
        private readonly List<string> quickLobbyModIds = new List<string>();
        private readonly List<string> quickLobbyModFolders = new List<string>();
        private readonly List<bool> quickLobbyModEnabled = new List<bool>();
        private Vector2 quickLobbyModScroll;
        private Vector2 quickLobbyModSelectedScroll;
        private float nextModListRefresh;
        private string quickLobbyModFilter = "";
        private bool quickLobbyModFilterFocused;
        private bool applyModsRunning;
        private string applyModsStatus = "";
        private float applyModsStatusUntil;
        private string modPresetName = "default";
        private bool modPresetNameFocused = false;
        private readonly List<string> modPresetNames = new List<string>();
        private int selectedModPreset = -1;
        private Vector2 modPresetScroll;
        private ConfigEntry<string> configModPresets;

        // Global status toast queue (one visible at a time)
        private string toastMessage = "";
        private float toastUntil;
        private readonly System.Collections.Generic.Queue<string> toastQueue =
            new System.Collections.Generic.Queue<string>();
        private const float ToastDuration = 2.8f;

        // Welcome join message (separate from water auto-splash)
        private bool welcomeMessageOnJoin = true;
        private ConfigEntry<bool> configWelcomeMessageOnJoin;

        // ============================================================
        // STARTUP / UPDATE
        // ============================================================
        private void Awake()
        {
            Instance = this;

            PhotonNetwork.AddCallbackTarget(this);

            BindConfig();

            FindPreparePool();
            StartCoroutine(LoadBackgroundAfterStartup());

            spectateHarmony =
                new Harmony(
                    "com.zex.qolmenu.spectate"
                );

            PatchOrbitCamera();
            PatchPhotonRoomCreation();
            StartCoroutine(DelayedPatchScanModSkip());

            StartCoroutine(InitialPrefabScan());
        }

        private void BindConfig()
        {
            // CharCon pattern: menu = KeyboardShortcut, action toggles = single KeyCode
            menuToggleKey = Config.Bind(
                "Controls",
                "Toggle_menu_visibility",
                new KeyboardShortcut(KeyCode.Insert),
                "Keybind to toggle QoL menu (supports modifiers).");

            noclipToggleKey = Config.Bind(
                "Controls",
                "Toggle_Noclip",
                KeyCode.F1,
                "Single key to toggle flying noclip (CharCon-style UnityInput).");

            waypointQuickSaveKey = Config.Bind(
                "Controls",
                "Quick_Waypoint",
                KeyCode.F6,
                "Single key to quick-save a waypoint.");

            flySpeedUpKey = Config.Bind(
                "Controls",
                "Fly_Speed_Up",
                KeyCode.F3,
                "Increase flying noclip speed by 10.");

            flySpeedDownKey = Config.Bind(
                "Controls",
                "Fly_Speed_Down",
                KeyCode.F2,
                "Decrease flying noclip speed by 10.");

            spectateNextKey = Config.Bind(
                "Keybinds",
                "SpectateNext",
                KeyCode.RightBracket,
                "Cycle spectate to next player.");

            spectatePrevKey = Config.Bind(
                "Keybinds",
                "SpectatePrev",
                KeyCode.LeftBracket,
                "Cycle spectate to previous player.");

            configSoftTeleport = Config.Bind(
                "Teleport",
                "SoftTeleport",
                true,
                "When true, teleports lerp smoothly instead of snapping.");

            configBrowsePositionRestore = Config.Bind(
                "Teleport",
                "BrowsePositionRestore",
                true,
                "After server-browser refresh/rejoin, restore your saved position (and optional respawn).");

            configAutoSplashOnJoin = Config.Bind(
                "QoL",
                "AutoSplashOnJoin",
                true,
                "On join: splash everyone. When someone else joins: splash everyone except them. One toast per splash.");

            configWelcomeMessageOnJoin = Config.Bind(
                "QoL",
                "WelcomeMessageOnJoin",
                true,
                "When you join a room, show a single toast: You've Joined (room name).");

            configDestroyBodyOnLeave = Config.Bind(
                "QoL",
                "DestroyBodyOnLeave",
                true,
                "Destroy your local body before leaving a room to reduce clutter.");

            configPublishRoomPlayers = Config.Bind(
                "Servers",
                "PublishRoomPlayers",
                true,
                "When host, publish player names into room properties for server-browser hover.");

            configFlySpeed = Config.Bind(
                "Movement",
                "FlySpeed",
                25f,
                "Flying noclip speed (CharCon range ~5–500).");

            configCameraHeight = Config.Bind(
                "Spectate",
                "CameraHeight",
                0.85f,
                "Default spectate camera height in meters.");

            configCameraDistance = Config.Bind(
                "Spectate",
                "CameraDistance",
                3.25f,
                "Default spectate camera distance in meters.");

            configCameraRotation = Config.Bind(
                "Spectate",
                "CameraRotation",
                0f,
                "Default spectate camera rotation in degrees.");

            configBackgroundHue = Config.Bind(
                "Background",
                "Hue",
                0f,
                "Menu background hue (0-1).");

            configBackgroundOpacity = Config.Bind(
                "Background",
                "Opacity",
                1f,
                "Menu background opacity (0-1).");

            configBackgroundFPS = Config.Bind(
                "Background",
                "FramesPerSecond",
                24f,
                "Menu background animation speed in frames per second.");

            configMenuGreyscale = Config.Bind(
                "Background",
                "Greyscale",
                false,
                "When true, menu chrome is greyscale instead of RGB/hue-tinted.");

            configBannedUserIds = Config.Bind(
                "HostTools",
                "BannedUserIds",
                "",
                "Comma-separated list of banned Photon UserIds/names. Persists across sessions.");

            configFavoritePrefabNames = Config.Bind(
                "Spawner",
                "FavoritePrefabNames",
                "",
                "Comma-separated list of favorited/pinned prefab names. Persists across sessions.");

            configFavoriteRoomNames = Config.Bind(
                "Servers",
                "FavoriteRoomNames",
                "",
                "Comma-separated list of favorite room names. Persists across sessions.");

            configStatsPresets = Config.Bind(
                "Genes",
                "StatsPresets",
                "",
                "Saved gene/stat presets. Format: name=payload;name2=payload2");

            configEquipPresets = Config.Bind(
                "Genes",
                "EquipPresets",
                "",
                "Saved equipment presets. Format: name=payload;name2=payload2");

            configFullPresets = Config.Bind(
                "Genes",
                "FullPresets",
                "",
                "Full presets (character + genes + clothing). Format: name=payload;name2=payload2");

            // Apply loaded values to the live fields used by the UI/logic.
            spectateCameraHeight = configCameraHeight.Value;
            spectateCameraDistance = configCameraDistance.Value;
            spectateCameraRotation = configCameraRotation.Value;

            backgroundHue = configBackgroundHue.Value;
            backgroundOpacity = configBackgroundOpacity.Value;
            BackgroundFramesPerSecond = configBackgroundFPS.Value;
            menuColorGreyscale = configMenuGreyscale != null && configMenuGreyscale.Value;

            if (configSoftTeleport != null)
                softTeleportEnabled = configSoftTeleport.Value;
            if (configBrowsePositionRestore != null)
                browsePositionRestoreEnabled = configBrowsePositionRestore.Value;
            if (configAutoSplashOnJoin != null)
                autoSplashOnJoin = configAutoSplashOnJoin.Value;
            if (configWelcomeMessageOnJoin != null)
                welcomeMessageOnJoin = configWelcomeMessageOnJoin.Value;
            if (configFlySpeed != null)
                flySpeed = Mathf.Clamp(configFlySpeed.Value, 5f, 500f);
            if (configDestroyBodyOnLeave != null)
                destroyBodyOnLeave = configDestroyBodyOnLeave.Value;
            if (configPublishRoomPlayers != null)
                publishRoomPlayers = configPublishRoomPlayers.Value;

            try
            {
                if (PhotonNetwork.LocalPlayer != null && !string.IsNullOrEmpty(PhotonNetwork.LocalPlayer.NickName))
                    nameEditText = PhotonNetwork.LocalPlayer.NickName;
                else if (PhotonNetwork.NickName != null)
                    nameEditText = PhotonNetwork.NickName;
            }
            catch { }

            configModPresets = Config.Bind(
                "Lobby",
                "ModPresets",
                "",
                "Named mod presets for quick lobby. Format: name=jsonarray;name2=jsonarray");
            LoadModPresetsFromConfig();
            LoadBannedUserIds();
            LoadFavoritePrefabNames();
            LoadFavoriteRoomNames();
            LoadGenePresetsFromConfig();
            // Pull presets from KK CharCon cfg if present (Komar.koboldkare.CharConCheat.cfg)
            TryAutoImportCharConConfig();
        }

        /// <summary>
        /// Resolve a type from game assemblies first (skip Unity*/System*).
        /// Falls back to AccessTools if needed so ModManager etc. still resolve.
        /// </summary>
        private static Type SafeGameType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Assembly asm = assemblies[a];
                    if (asm == null) continue;
                    string an = asm.GetName().Name ?? "";
                    if (an.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("netstandard", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("Photon", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("Harmony", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("0Harmony", StringComparison.OrdinalIgnoreCase))
                        continue;
                    if (an.StartsWith("BepInEx", StringComparison.OrdinalIgnoreCase) &&
                        !an.EndsWith("UnityInput", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool prefer =
                        an == "Assembly-CSharp" ||
                        an == "Assembly-CSharp-firstpass" ||
                        an.IndexOf("Kobold", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        an.IndexOf("Assembly-CSharp", StringComparison.OrdinalIgnoreCase) >= 0 ||
                        an.EndsWith(".UnityInput", StringComparison.OrdinalIgnoreCase);
                    if (!prefer)
                        continue;

                    Type t = null;
                    try { t = asm.GetType(name, false); } catch { }
                    if (t != null) return t;
                    try { t = asm.GetType("KoboldKare." + name, false); } catch { }
                    if (t != null) return t;

                    Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtl) { types = rtl.Types; }
                    catch { continue; }
                    if (types == null) continue;

                    for (int i = 0; i < types.Length; i++)
                    {
                        Type cand = types[i];
                        if (cand == null) continue;
                        if (cand.Name == name || cand.FullName == name)
                            return cand;
                        if (cand.FullName != null &&
                            (cand.FullName.EndsWith("." + name, StringComparison.Ordinal) ||
                             cand.FullName.EndsWith("+" + name, StringComparison.Ordinal)))
                            return cand;
                    }
                }
            }
            catch { }

            try { return AccessTools.TypeByName(name); }
            catch { return null; }
        }

        private void PatchOrbitCamera()
        {
            try
            {
                Type orbitCameraType =
                    SafeGameType("OrbitCamera");

                if (orbitCameraType == null)
                {
                    Logger.LogWarning(
                        "Spectate: OrbitCamera type was not found."
                    );
                    return;
                }

                MethodInfo lateUpdate =
                    AccessTools.Method(
                        orbitCameraType,
                        "LateUpdate"
                    );

                if (lateUpdate == null)
                {
                    Logger.LogWarning(
                        "Spectate: OrbitCamera.LateUpdate was not found."
                    );
                    return;
                }

                spectateHarmony.Patch(
                    lateUpdate,
                    new HarmonyMethod(
                        typeof(Plugin),
                        nameof(OrbitCameraLateUpdatePrefix)
                    )
                );

                Logger.LogInfo(
                    "Spectate: OrbitCamera.LateUpdate patched."
                );
            }
            catch (Exception ex)
            {
                Logger.LogError(
                    "Spectate patch failed: " + ex
                );
            }
        }

        /// <summary>
        /// Ensure ZexQoLPlayers is in CustomRoomProperties + CustomRoomPropertiesForLobby
        /// so lobby RoomInfo can carry the host-published name list.
        /// </summary>
        private void PatchPhotonRoomCreation()
        {
            try
            {
                Type photonNet = typeof(PhotonNetwork);
                int patched = 0;

                // CreateRoom(string, RoomOptions, TypedLobby, string[])
                MethodInfo create = AccessTools.Method(
                    photonNet,
                    "CreateRoom",
                    new Type[]
                    {
                        typeof(string),
                        typeof(RoomOptions),
                        typeof(TypedLobby),
                        typeof(string[])
                    });
                if (create != null)
                {
                    spectateHarmony.Patch(
                        create,
                        new HarmonyMethod(typeof(Plugin), nameof(PhotonCreateRoomPrefix)));
                    patched++;
                }

                // JoinOrCreateRoom(string, RoomOptions, TypedLobby, string[])
                MethodInfo joinOrCreate = AccessTools.Method(
                    photonNet,
                    "JoinOrCreateRoom",
                    new Type[]
                    {
                        typeof(string),
                        typeof(RoomOptions),
                        typeof(TypedLobby),
                        typeof(string[])
                    });
                if (joinOrCreate != null)
                {
                    spectateHarmony.Patch(
                        joinOrCreate,
                        new HarmonyMethod(typeof(Plugin), nameof(PhotonJoinOrCreateRoomPrefix)));
                    patched++;
                }

                // Fallback: any CreateRoom / JoinOrCreateRoom overload with a RoomOptions parameter
                if (patched == 0)
                {
                    MethodInfo[] methods = photonNet.GetMethods(BindingFlags.Public | BindingFlags.Static);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo m = methods[i];
                        if (m == null) continue;
                        if (m.Name != "CreateRoom" && m.Name != "JoinOrCreateRoom") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        bool hasOpts = false;
                        for (int p = 0; p < ps.Length; p++)
                        {
                            if (ps[p].ParameterType == typeof(RoomOptions))
                            {
                                hasOpts = true;
                                break;
                            }
                        }
                        if (!hasOpts) continue;
                        spectateHarmony.Patch(
                            m,
                            new HarmonyMethod(typeof(Plugin), nameof(PhotonRoomOptionsPrefixGeneric)));
                        patched++;
                    }
                }

                Logger.LogInfo("Photon room-create lobby props patch: " + patched + " method(s).");
            }
            catch (Exception ex)
            {
                Logger.LogError("Photon room-create patch failed: " + ex);
            }
        }

        // Harmony prefixes — inject lobby-visible player-list property into RoomOptions
        private static void PhotonCreateRoomPrefix(ref RoomOptions roomOptions)
        {
            InjectPlayerListLobbyProps(ref roomOptions);
        }

        private static void PhotonJoinOrCreateRoomPrefix(ref RoomOptions roomOptions)
        {
            InjectPlayerListLobbyProps(ref roomOptions);
        }

        /// <summary>Generic prefix: finds RoomOptions arg by scanning __args (Harmony injected).</summary>
        private static void PhotonRoomOptionsPrefixGeneric(object[] __args)
        {
            if (__args == null) return;
            for (int i = 0; i < __args.Length; i++)
            {
                if (__args[i] is RoomOptions)
                {
                    RoomOptions opts = (RoomOptions)__args[i];
                    InjectPlayerListLobbyProps(ref opts);
                    __args[i] = opts;
                    return;
                }
                if (__args[i] == null)
                {
                    // Can't know if this slot is RoomOptions without signature — skip
                }
            }
        }

        private static void InjectPlayerListLobbyProps(ref RoomOptions roomOptions)
        {
            try
            {
                if (roomOptions == null)
                    roomOptions = new RoomOptions();

                // Custom properties bag
                ExitGames.Client.Photon.Hashtable props = roomOptions.CustomRoomProperties;
                if (props == null)
                    props = new ExitGames.Client.Photon.Hashtable();

                if (!props.ContainsKey(RoomPlayersPropertyKey))
                    props[RoomPlayersPropertyKey] = "";

                roomOptions.CustomRoomProperties = props;

                // Lobby-visible keys
                string[] lobbyKeys = roomOptions.CustomRoomPropertiesForLobby;
                bool hasKey = false;
                if (lobbyKeys != null)
                {
                    for (int i = 0; i < lobbyKeys.Length; i++)
                    {
                        if (lobbyKeys[i] == RoomPlayersPropertyKey)
                        {
                            hasKey = true;
                            break;
                        }
                    }
                }

                if (!hasKey)
                {
                    if (lobbyKeys == null || lobbyKeys.Length == 0)
                    {
                        roomOptions.CustomRoomPropertiesForLobby = new string[] { RoomPlayersPropertyKey };
                    }
                    else
                    {
                        string[] expanded = new string[lobbyKeys.Length + 1];
                        for (int i = 0; i < lobbyKeys.Length; i++)
                            expanded[i] = lobbyKeys[i];
                        expanded[lobbyKeys.Length] = RoomPlayersPropertyKey;
                        roomOptions.CustomRoomPropertiesForLobby = expanded;
                    }
                }
            }
            catch
            {
                // Never block room create
            }
        }

        private IEnumerator DelayedPatchScanModSkip()
        {
            // Script Engine / hot-reload: wait until Assembly-CSharp types exist
            for (int i = 0; i < 50; i++)
            {
                if (SafeGameType("ModManager") != null ||
                    SafeGameType("NetworkManager") != null ||
                    SafeGameType("SteamWorkshopModLoader") != null)
                    break;
                yield return new WaitForSecondsRealtime(0.1f);
            }
            yield return null;
            PatchScanModSkip();
        }

        /// <summary>
        /// While room-scanning, skip the game's mod handshake / spawn work so we can
        /// read PhotonNetwork.PlayerList and leave without downloading/reloading mods.
        /// KoboldKare: host raises mod-list event → client compares → may Leave+download+rejoin.
        /// </summary>
        private void PatchScanModSkip()
        {
            int patched = 0;
            try
            {
                // 1) Block PhotonNetwork.Instantiate while scanning (no local kobold spawn)
                MethodInfo[] instMethods = typeof(PhotonNetwork).GetMethods(BindingFlags.Public | BindingFlags.Static);
                for (int i = 0; i < instMethods.Length; i++)
                {
                    MethodInfo m = instMethods[i];
                    if (m == null || m.Name != "Instantiate") continue;
                    if (m.ReturnType != typeof(GameObject)) continue;
                    try
                    {
                        spectateHarmony.Patch(
                            m,
                            new HarmonyMethod(typeof(Plugin), nameof(ScanSkipInstantiatePrefix)));
                        patched++;
                    }
                    catch { }
                }

                // 2) Emulate non-Workshop join: skip mod handshake / Steam download while scanning.
                //    Known KK types: ModManager, SteamWorkshopModLoader, NetworkManager (mod sync).
                List<string> patchedNames = new List<string>();
                List<Type> targetTypes = new List<Type>();

                string[] knownTypeNames =
                {
                    "ModManager",
                    "SteamWorkshopModLoader",
                    "SteamWorkshopItem",
                    "SteamWorkshop",
                    "WorkshopManager",
                    "ModLoader",
                    "ModDatabase",
                    "NetworkManager"
                };
                for (int k = 0; k < knownTypeNames.Length; k++)
                {
                    Type kt = SafeGameType(knownTypeNames[k]);
                    if (kt != null && !targetTypes.Contains(kt))
                        targetTypes.Add(kt);
                    Logger.LogInfo("Scan mod-skip type " + knownTypeNames[k] + ": " +
                        (kt != null ? kt.FullName : "NOT FOUND"));
                }

                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < assemblies.Length; a++)
                {
                    Assembly asm = assemblies[a];
                    if (asm == null) continue;
                    string an = asm.GetName().Name ?? "";
                    if (an != "Assembly-CSharp" && an != "Assembly-CSharp-firstpass")
                        continue;

                    Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtl) { types = rtl.Types; }
                    catch { continue; }
                    if (types == null) continue;

                    for (int t = 0; t < types.Length; t++)
                    {
                        Type type = types[t];
                        if (type == null) continue;
                        string tn = type.Name ?? "";
                        string fn = type.FullName ?? "";

                        bool modRelated =
                            fn.IndexOf(".Modding.", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            tn.IndexOf("SteamWorkshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                            tn.Equals("ModManager", StringComparison.OrdinalIgnoreCase) ||
                            tn.Equals("ModLoader", StringComparison.OrdinalIgnoreCase) ||
                            (tn.StartsWith("Mod", StringComparison.OrdinalIgnoreCase) &&
                             tn.IndexOf("Module", StringComparison.OrdinalIgnoreCase) < 0 &&
                             tn.IndexOf("Modifier", StringComparison.OrdinalIgnoreCase) < 0 &&
                             tn.IndexOf("Model", StringComparison.OrdinalIgnoreCase) < 0 &&
                             tn.IndexOf("Mode", StringComparison.OrdinalIgnoreCase) < 0);

                        if (modRelated && !targetTypes.Contains(type))
                            targetTypes.Add(type);
                    }
                }

                for (int t = 0; t < targetTypes.Count; t++)
                {
                    Type type = targetTypes[t];
                    if (type == null) continue;
                    string tn = type.Name ?? "";

                    bool isNetworkManager = tn.Equals("NetworkManager", StringComparison.OrdinalIgnoreCase);
                    bool isWorkshop = tn.IndexOf("SteamWorkshop", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool isModManager = tn.Equals("ModManager", StringComparison.OrdinalIgnoreCase) ||
                                        tn.Equals("ModLoader", StringComparison.OrdinalIgnoreCase);

                    MethodInfo[] methods;
                    try
                    {
                        // DeclaredOnly — never patch Unity MonoBehaviour (SendMessage, Awake, etc.)
                        methods = type.GetMethods(
                            BindingFlags.Public | BindingFlags.NonPublic |
                            BindingFlags.Instance | BindingFlags.Static |
                            BindingFlags.DeclaredOnly);
                    }
                    catch { continue; }

                    for (int mi = 0; mi < methods.Length; mi++)
                    {
                        MethodInfo method = methods[mi];
                        if (method == null || method.IsAbstract || method.IsGenericMethodDefinition)
                            continue;
                        // Only methods actually declared on this type
                        if (method.DeclaringType != type)
                            continue;

                        string mn = method.Name ?? "";
                        if (mn.StartsWith("get_") || mn.StartsWith("set_") ||
                            mn.StartsWith("add_") || mn.StartsWith("remove_"))
                            continue;

                        bool interesting;
                        if (isWorkshop)
                        {
                            // All declared workshop methods (download/query/subscribe callbacks)
                            interesting = true;
                        }
                        else if (isNetworkManager)
                        {
                            interesting =
                                mn.IndexOf("Mod", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Workshop", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Handshake", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Bundle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Catalog", StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                        else if (isModManager)
                        {
                            interesting =
                                mn.IndexOf("Sync", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Compare", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Handshake", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Validate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Load", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Reload", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Download", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Mount", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Unmount", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Apply", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Receive", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Process", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Finished", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Require", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Missing", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Join", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Room", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Bundle", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Activate", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Enable", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("Listener", StringComparison.OrdinalIgnoreCase) >= 0;
                        }
                        else
                        {
                            interesting =
                                mn.IndexOf("Download", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("LoadMod", StringComparison.OrdinalIgnoreCase) >= 0 ||
                                mn.IndexOf("ModList", StringComparison.OrdinalIgnoreCase) >= 0;
                        }

                        if (!interesting) continue;

                        if (method.ReturnType != typeof(void))
                            continue;

                        try
                        {
                            spectateHarmony.Patch(
                                method,
                                new HarmonyMethod(typeof(Plugin), nameof(ScanSkipModMethodPrefix)));
                            patched++;
                            if (patchedNames.Count < 40)
                                patchedNames.Add(tn + "." + mn);
                        }
                        catch { }
                    }
                }

                Logger.LogInfo("Scan mod-skip Harmony patches: " + patched + " method(s).");
                if (patchedNames.Count > 0)
                    Logger.LogInfo("Scan mod-skip targets: " + string.Join(", ", patchedNames.ToArray()));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PatchScanModSkip failed: " + ex.Message);
            }
        }

        /// <summary>Harmony prefix: skip PhotonNetwork.Instantiate while room-scanning.</summary>
        private static bool ScanSkipInstantiatePrefix(ref GameObject __result)
        {
            if (Instance == null || !Instance.scanRunning)
                return true;
            __result = null;
            return false;
        }

        /// <summary>Harmony prefix: skip mod sync/load/download methods while room-scanning.</summary>
        private static bool ScanSkipModMethodPrefix()
        {
            if (Instance == null || !Instance.scanRunning)
                return true;
            return false; // skip original
        }

        private static bool OrbitCameraLateUpdatePrefix()
        {
            if (Instance == null)
                return true;

            // Only spectate owns the camera. Fly uses orbit look direction (CharCon-style).
            return !Instance.spectating;
        }

        /// <summary>
        /// Swallow Photon custom events that drive the mod handshake while scanning.
        /// DeepWiki: host RaiseEvent mod list → client compares → leave/download/rejoin.
        /// Event code is game-defined; we block common codes and string payloads that look like mod lists.
        /// </summary>
        public void OnEvent(EventData photonEvent)
        {
            if (!scanRunning || photonEvent == null)
                return;

            try
            {
                byte code = photonEvent.Code;
                // PUN reserved codes are 200+; custom are 0–199. Mod sync is custom.
                // Also block letter codes sometimes used as (byte)'M' == 77.
                if (code == (byte)'M' || code == (byte)'m' || code == 77)
                {
                    // Eat event by not processing — we can't cancel Photon delivery, but if the
                    // game's handler is also Harmony-patched we're fine. Log once per scan room.
                    return;
                }

                object data = photonEvent.CustomData;
                if (data is string)
                {
                    string s = (string)data;
                    if (s.IndexOf("mod", StringComparison.OrdinalIgnoreCase) >= 0 &&
                        (s.IndexOf('{') >= 0 || s.IndexOf('[') >= 0))
                    {
                        // Looks like mod-list JSON — game handler may still see it unless patched
                        return;
                    }
                }
            }
            catch { }
        }

        private IEnumerator InitialPrefabScan()
        {
            yield return new WaitForSeconds(2f);
            RefreshPrefabs();
        }

        private void Update()
        {
            TryCaptureOrigin();

            float dt = Time.unscaledDeltaTime;
            if (dt > 0.0001f)
                fpsValue = Mathf.Lerp(fpsValue, 1f / dt, 0.12f);

            // RGB CYCLE: advance hue through the full spectrum (LOCK freezes; GREY disables)
            TickMenuHueCycle(dt);

            // Server-browser position restore after rejoin (Teleport toggle)
            if (browseRestoreActive)
            {
                if (!browsePositionRestoreEnabled || !PhotonNetwork.InRoom || Time.unscaledTime > browseRestoreUntil)
                {
                    browseRestoreActive = false;
                    if (!browsePositionRestoreEnabled)
                        browseHasSavedTransform = false;
                    else if (Time.unscaledTime > browseRestoreUntil)
                        browseHasSavedTransform = false;
                    if (PhotonNetwork.InRoom && browsePositionRestoreEnabled)
                        serverListStatus = "CACHED • " + cachedRooms.Count + " rooms • pos restore done (" + browseRestoreHits + " hits)";
                    Logger.LogInfo("Server browse: restore window ended, hits=" + browseRestoreHits + " respawned=" + browseDidRespawnRestore + " enabled=" + browsePositionRestoreEnabled);
                }
                else
                {
                    try
                    {
                        // Once a body exists, destroy+respawn at saved coords (like character swap)
                        if (!browseTriedRespawnRestore && ResolveLocalPlayerBody() != null)
                        {
                            browseTriedRespawnRestore = true;
                            if (TryRespawnAtBrowsePosition())
                            {
                                browseDidRespawnRestore = true;
                                browseRestoreHits++;
                                serverListStatus = "CACHED • respawned at saved pos";
                            }
                        }

                        // Keep shoving in case spawn/network still fights us
                        ForceTeleportLocalPlayer(browseSavedPosition, browseSavedRotation);
                        browseRestoreHits++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Browse restore tick failed: " + ex.Message);
                    }
                }
            }

            objectCountTimer -= dt;
            if (objectCountTimer <= 0f)
            {
                objectCountTimer = 1f;
                try { sceneObjectCount = UnityEngine.Object.FindObjectsOfType<GameObject>().Length; }
                catch { sceneObjectCount = 0; }
            }

            TrackRecentPlayers();
            UpdateRoomPlayersPublish();
            UpdateFollowPlayer();
            ProcessHotkeys();
            UpdateFlyingNoclipInput(); // input + compute velocity
            UpdateFrozenPlayers();

            if (spectating)
            {
                Player p = GetPlayerByActorId(spectateActorId);
                GameObject target = p == null ? null : FindPlayerObject(p);
                if (p == null || p.IsLocal || target == null)
                    StopSpectating();
                else
                    spectateTarget = target.transform;
            }
        }

        /// <summary>
        /// CharCon-style hotkeys via UnityInput.Current (see UserSettings.Dothekeybinds).
        /// Runs once per frame in Update only.
        /// </summary>
        private void ProcessHotkeys()
        {
            if (waitingForKeyRebind)
                return;

            int frame = Time.frameCount;
            if (frame == lastHotkeyFrame)
                return;
            lastHotkeyFrame = frame;

            try
            {
                // Menu: KeyboardShortcut (CharCon ToggleMenuVisibility.Value.IsDown equivalent)
                if (menuToggleKey != null && ZexInput.ShortcutDown(menuToggleKey.Value))
                    menuVisible = !menuVisible;

                // Noclip / waypoint: single KeyCode + UnityInput.GetKeyDown (CharCon)
                if (noclipToggleKey != null && ZexInput.GetKeyDown(noclipToggleKey.Value))
                    ToggleFlyingNoclip();

                if (waypointQuickSaveKey != null && ZexInput.GetKeyDown(waypointQuickSaveKey.Value))
                    QuickSaveWaypoint();

                if (flySpeedUpKey != null && ZexInput.GetKeyDown(flySpeedUpKey.Value))
                    AdjustFlySpeed(10f);

                if (flySpeedDownKey != null && ZexInput.GetKeyDown(flySpeedDownKey.Value))
                    AdjustFlySpeed(-10f);

                if (spectateNextKey != null && ZexInput.GetKeyDown(spectateNextKey.Value))
                    SpectateCycleFromHotkey(1);
                if (spectatePrevKey != null && ZexInput.GetKeyDown(spectatePrevKey.Value))
                    SpectateCycleFromHotkey(-1);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("ProcessHotkeys: " + ex.Message);
            }
        }

        /// <summary>
        /// Hotkey: cycle spectate target without opening the menu.
        /// Starts spectating if not already, then moves next/prev.
        /// </summary>
        private void SpectateCycleFromHotkey(int direction)
        {
            if (!PhotonNetwork.InRoom)
            {
                ShowToast("Not in a room");
                return;
            }
            if (!spectating)
            {
                // Pick first remote player
                Player[] list = PhotonNetwork.PlayerList;
                if (list == null) return;
                Player pick = null;
                for (int i = 0; i < list.Length; i++)
                {
                    if (list[i] != null && !list[i].IsLocal)
                    {
                        pick = list[i];
                        break;
                    }
                }
                if (pick == null)
                {
                    ShowToast("No one to spectate");
                    return;
                }
                selectedPlayer = pick;
                selectedActorId = pick.ActorNumber;
                StartSpectating();
                if (spectating)
                    ShowToast("Spectate: " + (pick.NickName ?? ("#" + pick.ActorNumber)));
                return;
            }
            CycleSpectatePlayer(direction);
            if (selectedPlayer != null)
                ShowToast("Spectate: " + (selectedPlayer.NickName ?? ("#" + selectedPlayer.ActorNumber)));
        }

        private void AdjustFlySpeed(float delta)
        {
            flySpeed = Mathf.Clamp(flySpeed + delta, 5f, 500f);
            if (configFlySpeed != null)
                configFlySpeed.Value = flySpeed;
            flyDebugStatus = "FLY SPD " + flySpeed.ToString("0");
        }

        private void TrackRecentPlayers()
        {
            if (!PhotonNetwork.InRoom)
            {
                if (knownRoomPlayers.Count > 0)
                    knownRoomPlayers.Clear();
                return;
            }

            Player[] players = PhotonNetwork.PlayerList;
            bool rosterChanged = false;

            HashSet<int> currentActors = new HashSet<int>();
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null)
                    continue;

                currentActors.Add(p.ActorNumber);

                if (!knownRoomPlayers.ContainsKey(p.ActorNumber))
                {
                    knownRoomPlayers[p.ActorNumber] = GetPlayerName(p);
                    rosterChanged = true;

                    if (!p.IsLocal)
                    {
                        // Enforce the ban list: reject a banned account that tries to rejoin.
                        if (PhotonNetwork.IsMasterClient && IsPlayerBanned(p))
                        {
                            PhotonNetwork.CloseConnection(p);
                            AddRecentPlayerEvent("REJECTED BANNED PLAYER: " + GetPlayerName(p) + "  #" + p.ActorNumber);
                        }
                        else
                        {
                            AddRecentPlayerEvent("JOINED: " + GetPlayerName(p) + "  #" + p.ActorNumber);
                        }
                    }
                }
            }

            List<int> knownActors = new List<int>(knownRoomPlayers.Keys);
            for (int i = 0; i < knownActors.Count; i++)
            {
                int actorId = knownActors[i];
                if (!currentActors.Contains(actorId))
                {
                    string name = knownRoomPlayers[actorId];
                    knownRoomPlayers.Remove(actorId);
                    rosterChanged = true;
                    AddRecentPlayerEvent("LEFT: " + name + "  #" + actorId);
                }
            }

            if (rosterChanged && publishRoomPlayers && PhotonNetwork.IsMasterClient)
            {
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
            }
        }

        private bool IsPlayerBanned(Player player)
        {
            if (player == null)
                return false;

            string key = !string.IsNullOrEmpty(player.UserId) ? player.UserId : GetPlayerName(player);
            return bannedUserIds.Contains(key);
        }


        private void LateUpdate()
        {
            if (!spectating)
                return;

            if (spectateTarget == null)
            {
                StopSpectating();
                return;
            }

            Camera cam = Camera.main;

            if (cam == null)
                return;

            Vector3 target =
                spectateTarget.position +
                Vector3.up * 1.25f;

            Vector3 behindOffset =
                -spectateTarget.forward * spectateCameraDistance;

            Vector3 rotatedOffset =
                Quaternion.AngleAxis(
                    spectateCameraRotation,
                    Vector3.up
                ) * behindOffset;

            Vector3 desired =
                target +
                rotatedOffset +
                Vector3.up * spectateCameraHeight;

            cam.transform.position = desired;

            Vector3 direction =
                target -
                cam.transform.position;

            if (direction.sqrMagnitude > 0.001f)
            {
                cam.transform.rotation =
                    Quaternion.LookRotation(
                        direction,
                        Vector3.up
                    );
            }
        }

        // ============================================================
        // GUI
        // ============================================================
        private void OnGUI()
        {
            CreateStyles();
            ApplyMenuHueToStyles();

            if (Event.current != null && Event.current.type == EventType.Repaint)
            {
                DrawESP(GetEspStyle());
                DrawPingMark();
            }

            DrawPlayerOverlay();
            DrawPlayerRadar();
            DrawPlayerContextMenu();
            DrawPresetsPopup();
            DrawWaypointsPopup();

            HandleInput();

            // Toast draws above everything (works with menu closed)
            DrawStatusToast();

            if (!menuVisible)
                return;

            ClampMenu();

            menuRect = GUI.Window(
                9001,
                menuRect,
                DrawMainWindow,
                GUIContent.none,
                windowStyle
            );
        }

        private void HandleInput()
        {
            Event e = Event.current;
            if (e == null)
                return;

            // Key rebind capture
            if (waitingForKeyRebind && e.type == EventType.KeyDown && e.keyCode != KeyCode.None)
            {
                if (e.keyCode == KeyCode.Escape)
                {
                    waitingForKeyRebind = false;
                    rebindTarget = null;
                    e.Use();
                    return;
                }

                if (rebindTarget == "menu" && menuToggleKey != null)
                    menuToggleKey.Value = new KeyboardShortcut(e.keyCode);
                else if (rebindTarget == "noclip" && noclipToggleKey != null)
                    noclipToggleKey.Value = e.keyCode;
                else if (rebindTarget == "waypoint" && waypointQuickSaveKey != null)
                    waypointQuickSaveKey.Value = e.keyCode;
                else if (rebindTarget == "flyUp" && flySpeedUpKey != null)
                    flySpeedUpKey.Value = e.keyCode;
                else if (rebindTarget == "flyDown" && flySpeedDownKey != null)
                    flySpeedDownKey.Value = e.keyCode;
                else if (rebindTarget == "specNext" && spectateNextKey != null)
                    spectateNextKey.Value = e.keyCode;
                else if (rebindTarget == "specPrev" && spectatePrevKey != null)
                    spectatePrevKey.Value = e.keyCode;

                waitingForKeyRebind = false;
                rebindTarget = null;
                e.Use();
                return;
            }

            // Spawner search input.
            if (tab == 1 && searchFocused && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (searchText.Length > 0)
                        searchText = searchText.Substring(0, searchText.Length - 1);

                    ApplySearch();
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    searchFocused = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character))
                {
                    if (searchText.Length < 128)
                        searchText += e.character;

                    ApplySearch();
                    e.Use();
                }
            }

            // Sillys name input. We intentionally use Event.current instead
            // of GUI.TextField/TextArea because this game's GUI reference set
            // does not expose those APIs.
            if (tab == 6 && sillysNameEditing && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (sillysName.Length > 0)
                        sillysName = sillysName.Substring(0, sillysName.Length - 1);

                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    sillysNameEditing = false;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    sillysNameEditing = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character))
                {
                    if (sillysName.Length < 32)
                        sillysName += e.character;

                    e.Use();
                }
            }

            // Hotkeys are handled in Update via ProcessHotkeys() so rebinds work
            // and IsDown() does not double-fire during OnGUI Layout/Repaint.
        }

        private void ClampMenu()
        {
            menuRect.x = Mathf.Clamp(
                menuRect.x,
                5f,
                Mathf.Max(5f, Screen.width - menuRect.width - 5f)
            );

            menuRect.y = Mathf.Clamp(
                menuRect.y,
                5f,
                Mathf.Max(5f, Screen.height - menuRect.height - 5f)
            );
        }

        /// <summary>
        /// Menu chrome colors driven by backgroundHue so the whole UI shifts together.
        /// Base palette is a cool blue at hue ~0.61; we rotate that hue by the slider.
        /// </summary>
        private float MenuSat(float sat)
        {
            return menuColorGreyscale ? 0f : sat;
        }

        /// <summary>
        /// Advances backgroundHue when RGB CYCLE is enabled. Call from Update with unscaled dt.
        /// Invalidates style cache so selected buttons / sidebar recolor every frame.
        /// </summary>
        private void TickMenuHueCycle(float dt)
        {
            if (menuColorGreyscale || !menuHueCycling)
                return;
            float period = Mathf.Clamp(menuHueCycleSeconds, MenuHueCycleSecondsMin, MenuHueCycleSecondsMax);
            if (period < 0.1f || dt <= 0f)
                return;

            backgroundHue = Mathf.Repeat(backgroundHue + dt / period, 1f);
            // Force ApplyMenuHueToStyles to rebuild accent textures this frame
            lastStyledHue = -1f;
        }

        /// <summary>
        /// Live RGB accent from current menu hue (cycles when RGB CYCLE is on).
        /// </summary>
        private Color GetMenuAccentColor(float alpha = 1f)
        {
            // Strong saturated accent (selected tabs, highlights) — grey when greyscale mode
            Color c = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), MenuSat(0.85f), 1f);
            c.a = alpha;
            return c;
        }

        /// <summary>
        /// Same as accent but slightly dimmer — list selection fills.
        /// </summary>
        private Color GetMenuSelectionColor(float alpha = 0.9f)
        {
            Color c = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), MenuSat(0.75f), 0.95f);
            c.a = alpha;
            return c;
        }

        private Color GetMenuButtonTint(float alpha = 1f)
        {
            // Lighter desaturated button wash (sidebar idle buttons etc.)
            Color c = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), MenuSat(0.28f), 1f);
            c.a = alpha;
            return c;
        }

        private Color GetMenuPanelColor(float value, float alpha = 1f)
        {
            // Dark panels slightly tinted toward the chosen hue
            Color c = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), MenuSat(0.35f), value);
            c.a = alpha;
            return c;
        }

        private void BeginDarkUI()
        {
            GUI.color = GetMenuPanelColor(0.12f, 1f);
        }

        private void BeginCardUI()
        {
            GUI.color = GetMenuPanelColor(0.18f, 1f);
        }

        private void BeginButtonUI()
        {
            GUI.color = GetMenuButtonTint(1f);
        }

        private void BeginAccentUI()
        {
            GUI.color = GetMenuAccentColor(1f);
        }

        private void EndUIColor()
        {
            GUI.color = Color.white;
        }

        private float lastStyledHue = -1f;
        private bool lastStyledGreyscale;

        private void ApplyMenuHueToStyles()
        {
            if (!stylesCreated)
                return;
            if (Mathf.Abs(lastStyledHue - backgroundHue) < 0.0005f && lastStyledGreyscale == menuColorGreyscale)
                return;
            lastStyledHue = backgroundHue;
            lastStyledGreyscale = menuColorGreyscale;

            Color accent = GetMenuAccentColor(1f);
            Color muted = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), MenuSat(0.18f), 0.72f);

            if (accentLabelStyle != null)
                accentLabelStyle.normal.textColor = accent;
            if (modernSmallStyle != null)
                modernSmallStyle.normal.textColor = muted;
            if (sectionStyle != null)
                sectionStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.35f);
            if (headerStyle != null)
                headerStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.25f);
            // KK's IMGUI strip has no GUIStyle.hover/active/on* or GUIStyleState.background.
            // Selection RGB is applied via GUI.backgroundColor / GetMenuSelectionColor at draw time.
            if (selectedButtonStyle != null)
                selectedButtonStyle.normal.textColor = Color.Lerp(Color.white, accent, 0.15f);
            if (modernSelectedButtonStyle != null)
                modernSelectedButtonStyle.normal.textColor = Color.white;
            if (sidebarSelectedStyle != null)
                sidebarSelectedStyle.normal.textColor = Color.white;
            if (valueStyle != null)
                valueStyle.normal.textColor = Color.Lerp(new Color(0.90f, 0.91f, 0.96f), accent, 0.2f);
        }

        private void DrawMainWindow(int id)
        {
            GUI.color = Color.white;

            // Animated / tinted background (hue + opacity apply here).
            // Avoid GUI.skin.window chrome so no black title bar is drawn.
            DrawMenuBackground();

            // Soft dark fill so content stays readable if the sprite sheet is bright (hue-tinted).
            Color prevBg = GUI.color;
            Color fill = GetMenuPanelColor(0.10f, 0.55f * backgroundOpacity);
            GUI.color = fill;
            GUI.Box(
                new Rect(0f, 0f, menuRect.width, menuRect.height),
                GUIContent.none,
                GUI.skin.box
            );
            GUI.color = prevBg;

            float sidebarW = 190f;
            float topH = 62f;
            // Extra right inset so lists/buttons don't sit flush against the window edge.
            const float contentPadL = 18f;
            const float contentPadR = 32f;
            float contentX = sidebarW + contentPadL;
            float contentY = topH + 18f;
            float contentW = menuRect.width - contentX - contentPadR;

            DrawModernTopBar(sidebarW, topH);
            DrawModernSidebar(sidebarW, topH);

            // Page area (card slightly larger than content for a soft inset).
            BeginCardUI();
            GUI.Box(
                new Rect(
                    contentX - 8f,
                    contentY - 8f,
                    contentW + 16f,
                    menuRect.height - contentY - 50f
                ),
                new GUIContent(""),
                cardStyle
            );
            EndUIColor();

            string pageTitle = GetModernPageTitle();
            string pageSubtitle = GetModernPageSubtitle();

            GUI.Label(
                new Rect(contentX + 10f, contentY + 10f, contentW - 20f, 30f),
                new GUIContent(pageTitle),
                headerStyle
            );

            GUI.Label(
                new Rect(contentX + 10f, contentY + 40f, contentW - 20f, 22f),
                new GUIContent(pageSubtitle),
                modernSmallStyle
            );

            float panelY = contentY + 70f;
            // Leave room for the bottom status bar so panel content does not clip into it.
            float panelMaxH = Mathf.Max(200f, menuRect.height - panelY - 48f);

            if (tab == 0)
                DrawESPPanel(contentX, panelY, contentW);
            else if (tab == 1)
                DrawSpawnerPanel(contentX, panelY, contentW);
            else if (tab == 2)
                DrawTeleportPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 3)
                DrawHostToolsPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 4)
                DrawMiscPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 5)
                DrawServersPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 7)
                DrawHostLogsPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 8)
                DrawGenesPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 9)
                DrawTestingPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 10)
                DrawSettingsPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 11)
                DrawQoLPanel(contentX, panelY, contentW, panelMaxH);
            else if (tab == 12)
                DrawModsPanel(contentX, panelY, contentW, panelMaxH);
            else
                DrawSillysPanel(contentX, panelY, contentW);

            DrawModernStatusBar(
                contentX,
                menuRect.height - 38f,
                contentW
            );

            // Only the top bar is draggable.
            GUI.DragWindow(
                new Rect(
                    0f,
                    0f,
                    menuRect.width,
                    62f
                )
            );

            GUI.color = Color.white;
        }

        private string GetModernPageTitle()
        {
            switch (tab)
            {
                case 0: return "Visuals";
                case 1: return "Spawner";
                case 2: return "Teleport/Spectate";
                case 3: return "Host Tools";
                case 4: return "Miscellaneous";
                case 5: return "Better Server List";
                case 6: return "Sillys";
                case 7: return "Host Logs";
                case 8: return "( Fixing Text Offset soon. )";
                case 9: return "Every update there will be features to test and vote to add";
                case 10: return "Settings";
                case 11: return "QoL";
                case 12: return "Mods | DO NOT CHANGE OR APPLY MODS IN GAME. YOU WILL FUCK YOURSELF |";
            }

            return "Zex's QoL";
        }

        private string GetModernPageSubtitle()
        {
            switch (tab)
            {
                case 0: return "ESP, tracers, colors and player visibility.";
                case 1: return "Browse, favorite and spawn available prefabs.";
                case 2: return "Player movement, waypoints and spectate controls.";
                case 3: return "Room administration and host-only actions.";
                case 4: return "Leftover misc — most tools moved to QoL / Settings.";
                case 5: return "Browse and switch between Photon rooms.";
                case 6: return "Completely unnecessary tools will be here xD alot of local shit.";
                case 7: return "Join/leave events and banned player list.";
                case 8: return "Credits to Komar for the Inspo. This Menu May Change";
       //         case 9: return "Host bring/freeze, ownership tools.";
                case 10: return "Keybinds, soft teleport, menu background.";
                case 11: return "Qol Is a Bit Empty ATM. Next Comminuity Vote Will Be Qol Additions and Will Be Added Here";
                case 12: return "Browse, select, and apply local/Workshop mods.";
            }

            return "Basically Minecrafts Community Voting.";
        }

        private void DrawModernTopBar(float sidebarW, float height)
        {
            GUI.Box(
                new Rect(
                    sidebarW,
                    0f,
                    menuRect.width - sidebarW,
                    height
                ),
                new GUIContent(""),
                topBarStyle
            );

            GUI.Label(
                new Rect(
                    sidebarW + 24f,
                    16f,
                    200f,
                    28f
                ),
                new GUIContent("˚ʚ♡ɞ˚ Zex's QoL ˚ʚ♡ɞ˚"),
                accentLabelStyle
            );

            string roomName =
                PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                    ? PhotonNetwork.CurrentRoom.Name
                    : "Not connected";

            string roomText =
                PhotonNetwork.InRoom
                    ? "●  " + roomName
                    : "○  " + roomName;

            GUI.Label(
                new Rect(
                    sidebarW + 220f,
                    19f,
                    320f,
                    24f
                ),
                new GUIContent(roomText),
                valueStyle
            );

            string masterText =
                PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient
                    ? "HOST"
                    : "CLIENT";

            GUI.Label(
                new Rect(
                    menuRect.width - 190f,
                    19f,
                    90f,
                    24f
                ),
                new GUIContent(masterText),
                PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient
                    ? accentLabelStyle
                    : modernSmallStyle
            );

            BeginButtonUI();
            if (GUI.Button(
                new Rect(
                    menuRect.width - 88f,
                    14f,
                    68f,
                    34f
                ),
                new GUIContent("SETTINGS"),
                modernButtonStyle
            ))
            {
                tab = 10;
            }
            EndUIColor();
        }

        private void DrawModernSidebar(float width, float topH)
        {
            GUI.Box(
                new Rect(
                    0f,
                    0f,
                    width,
                    menuRect.height
                ),
                new GUIContent(""),
                sidebarStyle
            );

            GUI.Label(
                new Rect(
                    18f,
                    22f,
                    width - 36f,
                    32f
                ),
                new GUIContent("BETA"),
                headerStyle
            );

            float y = 70f;

            DrawModernSidebarGroup(
                ref y,
                "WILL",
                new string[] { "ESP", "SPAWNER" },
                new int[] { 0, 1 },
                width
            );

            DrawModernSidebarGroup(
                ref y,
                "ORDER",
                new string[] { "TELEPORT/SPECTATE", "HOST TOOLS", "HOST LOGS", "SERVERS" },
                new int[] { 2, 3, 7, 5 },
                width
            );

            DrawModernSidebarGroup(
                ref y,
                "SOON",
                new string[] { "QOL", "CHARACTER EDITOR", "MOD LOADER", "SILLYS", "TESTING" },
                new int[] { 11, 8, 12, 6, 9 },
                width
            );

            GUI.Label(
                new Rect(
                    18f,
                    menuRect.height - 36f,
                    width - 36f,
                    18f
                ),
                new GUIContent("Public Beta V1.0"),
                modernSmallStyle
            );
        }

        private void DrawModernSidebarGroup(
            ref float y,
            string title,
            string[] names,
            int[] indexes,
            float width
        )
        {
            GUI.Label(
                new Rect(
                    18f,
                    y,
                    width - 36f,
                    18f
                ),
                new GUIContent(title),
                modernSmallStyle
            );

            y += 24f;

            for (int i = 0; i < names.Length; i++)
            {
                int page = indexes[i];

                GUIStyle style =
                    tab == page
                        ? sidebarSelectedStyle
                        : sidebarStyle;

                if (tab == page)
                    BeginAccentUI();
                else
                    BeginButtonUI();

                bool clicked = GUI.Button(
                    new Rect(
                        12f,
                        y,
                        width - 24f,
                        36f
                    ),
                    new GUIContent(names[i]),
                    style
                );

                EndUIColor();

                if (clicked)
                {
                    tab = page;
                    searchFocused = false;
                    sillysNameEditing = false;
                }

                y += 40f;
            }

            y += 10f;
        }

        private void DrawModernStatusBar(float x, float y, float width)
        {
            GUI.Box(
                new Rect(
                    x,
                    y,
                    width,
                    26f
                ),
                new GUIContent(""),
                topBarStyle
            );

            int players =
                PhotonNetwork.InRoom && PhotonNetwork.PlayerList != null
                    ? PhotonNetwork.PlayerList.Length
                    : 0;

            string host =
                PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient
                    ? "MASTER"
                    : "CLIENT";

            string fly = flyingNoclipActive ? "     FLY" : "";
            string froze = frozenActorIds.Count > 0 ? "     FROZEN " + frozenActorIds.Count : "";
            string text =
                "FPS " + Mathf.RoundToInt(fpsValue) +
                "     PLAYERS " + players +
                "     SPAWNED " + spawnedObjects.Count +
                "     " + host + fly + froze;

            GUI.Label(
                new Rect(
                    x + 10f,
                    y + 4f,
                    width - 20f,
                    20f
                ),
                new GUIContent(text),
                modernSmallStyle
            );
        }

        private void DrawTabs(float x, float y, float width)
        {
            // Kept for compatibility with older calls elsewhere.
            // The new menu uses the left sidebar instead.
        }

        // ============================================================
        // GENES PANEL
        // ============================================================
        private void DrawGenesPanel(float x, float y, float width, float maxHeight)
        {
            ResolveGeneTypes();
            float startY = y;
            Event e = Event.current;

            // ---- Top bar ----
            GUI.Label(new Rect(x, y, 160f, 22f), new GUIContent("KOBOLD"), headerStyle);

            // Right-aligned action buttons (presets lives up here so it never gets clipped)
            float bx = x + width;
            float bw;
            bw = 88f; bx -= bw;
            if (GUI.Button(new Rect(bx, y - 2f, bw, 26f), new GUIContent("APPLY"), buttonStyle))
                ApplyGenesToKobold();
            bx -= 4f;
            bw = 72f; bx -= bw;
            if (GUI.Button(new Rect(bx, y - 2f, bw, 26f), new GUIContent("EQUIP"), buttonStyle))
                TryEquipSelectedDick();
            bx -= 4f;
            bw = 86f; bx -= bw;
            if (GUI.Button(new Rect(bx, y - 2f, bw, 26f), new GUIContent("SET CHAR"), buttonStyle))
                TryApplySelectedCharacter();
            bx -= 4f;
            bw = 80f; bx -= bw;
            if (GUI.Button(new Rect(bx, y - 2f, bw, 26f), new GUIContent("Refresh"), buttonStyle))
            {
                RefreshGenesFromKobold();
                RefreshDickOptions();
                RefreshEquipmentCatalog();
                RefreshEquipmentList();
                RefreshCharacterList();
            }
            bx -= 4f;
            int totalPresetsTop = fullPresetNames.Count + statsPresetNames.Count + equipPresetNames.Count;
            string presetsTopLabel = presetsPopupVisible
                ? "CLOSE PRESETS"
                : ("PRESETS (" + totalPresetsTop + ")");
            bw = 130f; bx -= bw;
            if (GUI.Button(new Rect(bx, y - 2f, bw, 26f), new GUIContent(presetsTopLabel), buttonStyle))
            {
                presetsPopupVisible = !presetsPopupVisible;
                if (presetsPopupVisible)
                {
                    presetsPopupRect.x = menuRect.xMax + 12f;
                    presetsPopupRect.y = menuRect.y + 80f;
                    if (presetsPopupRect.xMax > Screen.width - 8f)
                        presetsPopupRect.x = Mathf.Max(8f, menuRect.x - presetsPopupRect.width - 12f);
                }
            }

            y += 28f;
            GUI.Label(new Rect(x, y, width, 18f), new GUIContent(geneStatus), smallStyle);
            y += 22f;

            // ---- Character picker (modded server avatars) ----
            float charBlockH = Mathf.Min(150f, maxHeight * 0.22f);
            GUI.Label(new Rect(x, y, 160f, 18f), new GUIContent("CHARACTERS"), sectionStyle);
            GUI.Label(new Rect(x + 170f, y, 40f, 18f), new GUIContent("Filter"), smallStyle);
            float filterW = Mathf.Min(280f, width - 400f);
            if (filterW < 160f) filterW = 160f;
            Rect filterRect = new Rect(x + 215f, y - 1f, filterW, 22f);
            GUI.Box(filterRect, "");
            string charFilterShown = string.IsNullOrEmpty(characterFilter) ? "..." : characterFilter;
            if (characterFilterEditing)
                charFilterShown = characterFilter + "|";
            GUI.Label(new Rect(filterRect.x + 6f, filterRect.y + 2f, filterRect.width - 12f, 18f),
                new GUIContent(charFilterShown), labelStyle);
            if (e != null && e.type == EventType.MouseDown && filterRect.Contains(e.mousePosition))
            {
                characterFilterEditing = true;
                geneEditIndex = -1;
                speciesEditing = false;
                presetNameEditing = false;
                equipFilterEditing = false;
                e.Use();
            }
            GUI.Label(new Rect(filterRect.xMax + 10f, y, 160f, 18f),
                new GUIContent(characterOptions.Count + " available"), smallStyle);
            y += 22f;

            Rect charRect = new Rect(x, y, width, charBlockH);
            GUI.Box(charRect, "");
            List<string> filteredChars = GetFilteredCharacters();
            if (filteredChars.Count == 0)
            {
                GUI.Label(new Rect(x + 10f, y + 8f, width - 20f, 40f),
                    new GUIContent(characterOptions.Count == 0
                        ? "Press REFRESH to load characters from Player/Prefab database (modded servers add many more)."
                        : "No characters match filter."),
                    smallStyle);
            }
            else
            {
                const float cRow = 22f;
                // grid-ish: 3 columns
                int cols = 3;
                float colW = (width - 16f) / cols;
                float cContent = Mathf.Ceil(filteredChars.Count / (float)cols) * cRow;
                float cMax = Mathf.Max(0f, cContent - charBlockH + 6f);
                if (e != null && e.type == EventType.ScrollWheel && charRect.Contains(e.mousePosition))
                {
                    characterScroll.y = Mathf.Clamp(characterScroll.y + e.delta.y * 22f, 0f, cMax);
                    e.Use();
                }
                characterScroll.y = Mathf.Clamp(characterScroll.y, 0f, cMax);
                GUI.BeginGroup(new Rect(x + 4f, y + 4f, width - 8f, charBlockH - 8f), GUIContent.none, GUIStyle.none);
                for (int i = 0; i < filteredChars.Count; i++)
                {
                    int col = i % cols;
                    int row = i / cols;
                    float charX = col * colW;
                    float charY = row * cRow - characterScroll.y;
                    if (charY + cRow < 0f || charY > charBlockH)
                        continue;
                    int realIdx = characterOptions.IndexOf(filteredChars[i]);
                    GUIStyle st = (realIdx == selectedCharacterIndex) ? selectedButtonStyle : buttonStyle;
                    if (GUI.Button(new Rect(charX, charY, colW - 4f, 20f), new GUIContent(filteredChars[i]), st))
                        selectedCharacterIndex = realIdx >= 0 ? realIdx : i;
                }
                GUI.EndGroup();
            }
            y += charBlockH + 10f;

            float gap = 12f;
            float leftW = width * 0.52f;
            float rightW = width - leftW - gap;
            float rightX = x + leftW + gap;
            float bodyH = Mathf.Max(180f, maxHeight - (y - startY) - 8f);

            // ================= LEFT: GENES =================
            float leftY = y;
            GUI.Label(new Rect(x, leftY, leftW, 18f), new GUIContent("GENES"), sectionStyle);
            leftY += 20f;

            float colGene = 78f;
            float colCur = 58f;
            float colSet = leftW - colGene - colCur - 8f;
            GUI.Label(new Rect(x, leftY, colGene, 16f), new GUIContent("Gene"), smallStyle);
            GUI.Label(new Rect(x + colGene, leftY, colCur, 16f), new GUIContent("Cur"), smallStyle);
            GUI.Label(new Rect(x + colGene + colCur, leftY, colSet, 16f), new GUIContent("To set"), smallStyle);
            leftY += 18f;

            float genesListH = Mathf.Min(bodyH * 0.55f, geneFieldDefs.Length * 26f + 8f);
            Rect genesRect = new Rect(x, leftY, leftW, genesListH);
            GUI.Box(genesRect, "");

            const float rowH = 26f;
            float genesContentH = geneFieldDefs.Length * rowH;
            float genesMaxScroll = Mathf.Max(0f, genesContentH - genesListH + 6f);
            if (e != null && e.type == EventType.ScrollWheel && genesRect.Contains(e.mousePosition))
            {
                genesScroll.y = Mathf.Clamp(genesScroll.y + e.delta.y * 22f, 0f, genesMaxScroll);
                e.Use();
            }
            genesScroll.y = Mathf.Clamp(genesScroll.y, 0f, genesMaxScroll);

            GUI.BeginGroup(new Rect(x + 3f, leftY + 3f, leftW - 6f, genesListH - 6f), GUIContent.none, GUIStyle.none);
            float rowY = -genesScroll.y;
            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                GUI.Label(new Rect(2f, rowY + 3f, colGene - 4f, 18f), new GUIContent(geneFieldDefs[i].Label), labelStyle);
                GUI.Label(new Rect(colGene, rowY + 3f, colCur - 2f, 18f),
                    new GUIContent(geneCurrent[i].ToString("0.##")), smallStyle);

                Rect fieldRect = new Rect(colGene + colCur, rowY + 1f, Mathf.Max(56f, colSet - 8f), 20f);
                GUI.Box(fieldRect, "");
                string display = geneToSetText[i] ?? geneToSet[i].ToString("0.##");
                if (geneEditIndex == i)
                    display = display + "|";
                GUI.Label(new Rect(fieldRect.x + 4f, fieldRect.y + 1f, fieldRect.width - 8f, 16f),
                    new GUIContent(display), labelStyle);
                if (e != null && e.type == EventType.MouseDown && fieldRect.Contains(e.mousePosition))
                {
                    geneEditIndex = i;
                    speciesEditing = false;
                    presetNameEditing = false;
                    e.Use();
                }
                rowY += rowH;
            }
            GUI.EndGroup();
            leftY += genesListH + 10f;

            // Species + thickness under genes
            GUI.Label(new Rect(x, leftY, 70f, 18f), new GUIContent("Species"), labelStyle);
            Rect spRect = new Rect(x + 72f, leftY, 56f, 20f);
            GUI.Box(spRect, "");
            string spShown = speciesEditing ? speciesEditText + "|" : speciesEditText;
            GUI.Label(new Rect(spRect.x + 4f, spRect.y + 1f, spRect.width - 8f, 16f),
                new GUIContent(TruncateForDisplay(spShown, 6)), labelStyle);
            if (e != null && e.type == EventType.MouseDown && spRect.Contains(e.mousePosition))
            {
                speciesEditing = true;
                geneEditIndex = -1;
                presetNameEditing = false;
                e.Use();
            }
            GUI.Label(new Rect(x + 128f, leftY, leftW - 130f, 18f), new GUIContent(speciesName), smallStyle);
            leftY += 24f;

            GUI.Label(new Rect(x, leftY, 90f, 18f),
                new GUIContent("CockThick " + cockThickness.ToString("0.00")), labelStyle);
            cockThickness = GUI.HorizontalSlider(
                new Rect(x + 100f, leftY + 2f, leftW - 108f, 16f),
                cockThickness, 0.1f, 3f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            leftY += 28f;

            // ================= RIGHT: DICK + EQUIP + PRESETS =================
            float rightY = y;

            // Dick list  (presets moved to pop-out → give dick/clothes more room)
            GUI.Label(new Rect(rightX, rightY, rightW, 18f), new GUIContent("DICK / EQUIP"), sectionStyle);
            rightY += 20f;
            float dickH = bodyH * 0.38f;
            Rect dickRect = new Rect(rightX, rightY, rightW, dickH);
            GUI.Box(dickRect, "");
            if (dickOptions.Count == 0)
            {
                GUI.Label(new Rect(rightX + 8f, rightY + 8f, rightW - 16f, 40f),
                    new GUIContent("Refresh to load"), smallStyle);
            }
            else
            {
                const float dRow = 24f;
                float dContent = dickOptions.Count * dRow;
                float dMax = Mathf.Max(0f, dContent - dickH + 6f);
                if (e != null && e.type == EventType.ScrollWheel && dickRect.Contains(e.mousePosition))
                {
                    dickScroll.y = Mathf.Clamp(dickScroll.y + e.delta.y * 22f, 0f, dMax);
                    e.Use();
                }
                dickScroll.y = Mathf.Clamp(dickScroll.y, 0f, dMax);
                GUI.BeginGroup(new Rect(rightX + 3f, rightY + 3f, rightW - 6f, dickH - 6f), GUIContent.none, GUIStyle.none);
                float dy = -dickScroll.y;
                for (int i = 0; i < dickOptions.Count; i++)
                {
                    GUIStyle st = (i == selectedDickIndex) ? selectedButtonStyle : buttonStyle;
                    if (GUI.Button(new Rect(0f, dy, rightW - 12f, 22f), new GUIContent(dickOptions[i]), st))
                        selectedDickIndex = i;
                    dy += dRow;
                }
                GUI.EndGroup();
            }
            rightY += dickH + 8f;

            // Clothing catalog + worn list
            GUI.Label(new Rect(rightX, rightY, rightW - 160f, 18f), new GUIContent("CLOTHES"), sectionStyle);
            if (GUI.Button(new Rect(rightX + rightW - 150f, rightY - 2f, 70f, 20f), new GUIContent("WEAR"), buttonStyle))
                TryWearSelectedEquipment();
            if (GUI.Button(new Rect(rightX + rightW - 75f, rightY - 2f, 75f, 20f), new GUIContent("REMOVE"), buttonStyle))
                TryRemoveSelectedEquipment();
            rightY += 22f;

            // Filter
            GUI.Label(new Rect(rightX, rightY, 40f, 18f), new GUIContent("Find"), smallStyle);
            Rect eqFilterRect = new Rect(rightX + 42f, rightY - 1f, rightW - 50f, 22f);
            GUI.Box(eqFilterRect, "");
            string eqFilterShown = string.IsNullOrEmpty(equipFilter) ? "..." : equipFilter;
            if (equipFilterEditing)
                eqFilterShown = equipFilter + "|";
            GUI.Label(new Rect(eqFilterRect.x + 6f, eqFilterRect.y + 2f, eqFilterRect.width - 12f, 18f),
                new GUIContent(eqFilterShown), labelStyle);
            if (e != null && e.type == EventType.MouseDown && eqFilterRect.Contains(e.mousePosition))
            {
                equipFilterEditing = true;
                geneEditIndex = -1;
                speciesEditing = false;
                presetNameEditing = false;
                characterFilterEditing = false;
                e.Use();
            }
            rightY += 22f;

            // Leave ~36px at the bottom for status + PRESETS button
            float eqH = Mathf.Max(90f, bodyH * 0.42f);
            float halfEq = (rightW - 6f) * 0.5f;

            // Catalog (left)
            GUI.Label(new Rect(rightX, rightY, halfEq, 16f),
                new GUIContent("All (" + equipCatalog.Count + ")"), smallStyle);
            GUI.Label(new Rect(rightX + halfEq + 6f, rightY, halfEq, 16f),
                new GUIContent("Worn (" + equipNames.Count + ")"), smallStyle);
            rightY += 16f;

            Rect catalogRect = new Rect(rightX, rightY, halfEq, eqH);
            Rect wornRect = new Rect(rightX + halfEq + 6f, rightY, halfEq, eqH);
            GUI.Box(catalogRect, "");
            GUI.Box(wornRect, "");

            List<string> filteredEquip = GetFilteredEquipCatalog();
            const float eRow = 20f;

            // Catalog scroll
            float catContent = filteredEquip.Count * eRow;
            float catMax = Mathf.Max(0f, catContent - eqH + 4f);
            if (e != null && e.type == EventType.ScrollWheel && catalogRect.Contains(e.mousePosition))
            {
                equipCatalogScroll.y = Mathf.Clamp(equipCatalogScroll.y + e.delta.y * 20f, 0f, catMax);
                e.Use();
            }
            equipCatalogScroll.y = Mathf.Clamp(equipCatalogScroll.y, 0f, catMax);
            GUI.BeginGroup(new Rect(catalogRect.x + 2f, catalogRect.y + 2f, halfEq - 4f, eqH - 4f), GUIContent.none, GUIStyle.none);
            float cy = -equipCatalogScroll.y;
            if (filteredEquip.Count == 0)
            {
                GUI.Label(new Rect(4f, 4f, halfEq - 12f, 40f),
                    new GUIContent(equipCatalog.Count == 0 ? "Refresh" : "No match"), smallStyle);
            }
            else
            {
                for (int i = 0; i < filteredEquip.Count; i++)
                {
                    int realIdx = equipCatalog.IndexOf(filteredEquip[i]);
                    GUIStyle st = (realIdx == selectedCatalogEquip) ? selectedButtonStyle : buttonStyle;
                    if (GUI.Button(new Rect(0f, cy, halfEq - 8f, 18f), new GUIContent(filteredEquip[i]), st))
                        selectedCatalogEquip = realIdx >= 0 ? realIdx : i;
                    cy += eRow;
                }
            }
            GUI.EndGroup();

            // Worn scroll
            float wornContent = equipNames.Count * eRow;
            float wornMax = Mathf.Max(0f, wornContent - eqH + 4f);
            if (e != null && e.type == EventType.ScrollWheel && wornRect.Contains(e.mousePosition))
            {
                equipScroll.y = Mathf.Clamp(equipScroll.y + e.delta.y * 20f, 0f, wornMax);
                e.Use();
            }
            equipScroll.y = Mathf.Clamp(equipScroll.y, 0f, wornMax);
            GUI.BeginGroup(new Rect(wornRect.x + 2f, wornRect.y + 2f, halfEq - 4f, eqH - 4f), GUIContent.none, GUIStyle.none);
            float wy = -equipScroll.y;
            if (equipNames.Count == 0)
            {
                GUI.Label(new Rect(4f, 4f, halfEq - 12f, 40f),
                    new GUIContent(string.IsNullOrEmpty(equipStatus) ? "Nothing worn" : equipStatus), smallStyle);
            }
            else
            {
                for (int i = 0; i < equipNames.Count; i++)
                {
                    GUIStyle st = (i == selectedWornEquip) ? selectedButtonStyle : buttonStyle;
                    if (GUI.Button(new Rect(0f, wy, halfEq - 8f, 18f), new GUIContent(equipNames[i]), st))
                        selectedWornEquip = i;
                    wy += eRow;
                }
            }
            GUI.EndGroup();
            rightY += eqH + 6f;

            // Auto-clear stale wear/remove status so it doesn't permanently eat layout
            if (!string.IsNullOrEmpty(equipStatus) && Time.unscaledTime > equipStatusUntil)
                equipStatus = "";

            if (!string.IsNullOrEmpty(equipStatus))
            {
                GUI.Label(new Rect(rightX, rightY, rightW, 16f), new GUIContent(equipStatus), smallStyle);
                rightY += 18f;
            }

            // Presets button is in the top bar now (see above)

            // ---- Keyboard input for gene / species / filters / preset name ----
            HandleGenesTextInput(e);
        }

        private void HandleGenesTextInput(Event e)
        {
            if (e == null || e.type != EventType.KeyDown)
                return;

            if (geneEditIndex >= 0 && geneEditIndex < geneFieldDefs.Length)
            {
                int i = geneEditIndex;
                if (geneToSetText[i] == null)
                    geneToSetText[i] = geneToSet[i].ToString("0.##");
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (geneToSetText[i].Length > 0)
                        geneToSetText[i] = geneToSetText[i].Substring(0, geneToSetText[i].Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    float parsed;
                    if (float.TryParse(geneToSetText[i], out parsed))
                        geneToSet[i] = Mathf.Clamp(parsed, geneFieldDefs[i].Min, geneFieldDefs[i].Max);
                    geneToSetText[i] = geneToSet[i].ToString("0.##");
                    geneEditIndex = -1;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character))
                {
                    char c = e.character;
                    if ((char.IsDigit(c) || c == '.' || c == '-' || c == ',') && geneToSetText[i].Length < 12)
                    {
                        geneToSetText[i] += c == ',' ? '.' : c;
                        e.Use();
                    }
                }
                return;
            }

            if (speciesEditing)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (speciesEditText.Length > 0)
                        speciesEditText = speciesEditText.Substring(0, speciesEditText.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    int sid;
                    if (int.TryParse(speciesEditText, out sid))
                        speciesId = sid;
                    speciesEditText = speciesId.ToString();
                    speciesEditing = false;
                    e.Use();
                }
                else if (e.character != '\0' && char.IsDigit(e.character) && speciesEditText.Length < 4)
                {
                    speciesEditText += e.character;
                    e.Use();
                }
                return;
            }

            if (presetNameEditing)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (newPresetName.Length > 0)
                        newPresetName = newPresetName.Substring(0, newPresetName.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    presetNameEditing = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && newPresetName.Length < 24)
                {
                    newPresetName += e.character;
                    e.Use();
                }
                return;
            }

            if (characterFilterEditing)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (characterFilter.Length > 0)
                        characterFilter = characterFilter.Substring(0, characterFilter.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    characterFilterEditing = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && characterFilter.Length < 32)
                {
                    characterFilter += e.character;
                    e.Use();
                }
                return;
            }

            if (equipFilterEditing)
            {
                if (e.keyCode == KeyCode.Backspace)
                {
                    if (equipFilter.Length > 0)
                        equipFilter = equipFilter.Substring(0, equipFilter.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter || e.keyCode == KeyCode.Escape)
                {
                    equipFilterEditing = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && equipFilter.Length < 32)
                {
                    equipFilter += e.character;
                    e.Use();
                }
            }
        }

        // ============================================================
        // PRESETS POP-OUT WINDOW
        // ============================================================
        private void DrawPresetsPopup()
        {
            if (!presetsPopupVisible)
                return;

            // Keep on screen
            presetsPopupRect.width = Mathf.Clamp(presetsPopupRect.width, 280f, 520f);
            presetsPopupRect.height = Mathf.Clamp(presetsPopupRect.height, 300f, 640f);
            presetsPopupRect.x = Mathf.Clamp(presetsPopupRect.x, 4f, Mathf.Max(4f, Screen.width - presetsPopupRect.width - 4f));
            presetsPopupRect.y = Mathf.Clamp(presetsPopupRect.y, 4f, Mathf.Max(4f, Screen.height - presetsPopupRect.height - 4f));

            presetsPopupRect = GUI.Window(
                9010,
                presetsPopupRect,
                DrawPresetsPopupWindow,
                GUIContent.none,
                windowStyle != null ? windowStyle : GUI.skin.window
            );
        }

        private void DrawPresetsPopupWindow(int id)
        {
            float pad = 12f;
            float x = pad;
            float y = pad;
            float w = presetsPopupRect.width - pad * 2f;
            GUIStyle hdr = headerStyle != null ? headerStyle : GUI.skin.label;
            GUIStyle lbl = labelStyle != null ? labelStyle : GUI.skin.label;
            GUIStyle sm = smallStyle != null ? smallStyle : GUI.skin.label;
            GUIStyle btn = buttonStyle != null ? buttonStyle : GUI.skin.button;
            GUIStyle selBtn = selectedButtonStyle != null ? selectedButtonStyle : GUI.skin.button;

            GUI.Label(new Rect(x, y, w - 70f, 22f), new GUIContent("FULL PRESETS"), hdr);
            if (GUI.Button(new Rect(presetsPopupRect.width - pad - 64f, y - 2f, 64f, 24f),
                new GUIContent("CLOSE"), btn))
            {
                presetsPopupVisible = false;
                presetNameEditing = false;
            }
            y += 26f;

            GUI.Label(new Rect(x, y, w, 32f),
                new GUIContent("Character + genes + clothes"),
                sm);
            y += 34f;

            // Name field
            GUI.Label(new Rect(x, y, 44f, 18f), new GUIContent("Name"), sm);
            Rect nameRect = new Rect(x + 48f, y - 1f, w - 52f, 22f);
            GUI.Box(nameRect, "");
            string nameShown = string.IsNullOrEmpty(newPresetName) ? "Preset name..." : newPresetName;
            if (presetNameEditing) nameShown = newPresetName + "|";
            if (nameShown.Length > 28) nameShown = nameShown.Substring(0, 26) + "…";
            GUI.Label(new Rect(nameRect.x + 6f, nameRect.y + 2f, nameRect.width - 12f, 18f),
                new GUIContent(nameShown), lbl);

            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && nameRect.Contains(e.mousePosition))
            {
                presetNameEditing = true;
                geneEditIndex = -1;
                speciesEditing = false;
                characterFilterEditing = false;
                equipFilterEditing = false;
                e.Use();
            }
            y += 28f;

            // Primary actions
            if (GUI.Button(new Rect(x, y, w, 28f), new GUIContent("SAVE FULL PRESET"), btn))
                SaveCurrentFullPreset();
            y += 32f;

            if (GUI.Button(new Rect(x, y, w, 28f), new GUIContent("APPLY SELECTED (1-click)"), btn))
                ApplySelectedFullPreset();
            y += 32f;

            // Secondary: legacy partial presets
            float third = (w - 12f) / 3f;
            if (GUI.Button(new Rect(x, y, third, 22f), new GUIContent("Save Stats"), btn))
                SaveCurrentStatsPreset();
            if (GUI.Button(new Rect(x + third + 6f, y, third, 22f), new GUIContent("Save Equip"), btn))
                SaveCurrentEquipPreset();
            if (GUI.Button(new Rect(x + (third + 6f) * 2f, y, third, 22f), new GUIContent("Delete"), btn))
                DeleteSelectedPreset();
            y += 26f;

            // Import / export / clone
            if (GUI.Button(new Rect(x, y, third, 22f), new GUIContent("Import CharCon"), btn))
                ImportDefaultCharConPresets();
            if (GUI.Button(new Rect(x + third + 6f, y, third, 22f), new GUIContent("Export All"), btn))
                ExportAllPresetsToLog();
            if (GUI.Button(new Rect(x + (third + 6f) * 2f, y, third, 22f), new GUIContent("Clone Near"), btn))
                CloneNearbyPlayerToPreset();
            y += 28f;

            GUI.Label(new Rect(x, y, w, 16f),
                new GUIContent("[F] full  [S] stats  [E] equip  · CharCon = old KK format"), sm);
            y += 18f;

            // List
            float listH = Mathf.Max(80f, presetsPopupRect.height - y - pad - 8f);
            Rect listRect = new Rect(x, y, w, listH);
            GUI.Box(listRect, "");

            List<string> allPresetLines = new List<string>();
            List<string> allKinds = new List<string>(); // "F" / "S" / "E"
            List<int> allIdx = new List<int>();
            for (int i = 0; i < fullPresetNames.Count; i++)
            {
                allPresetLines.Add("[F] " + fullPresetNames[i]);
                allKinds.Add("F");
                allIdx.Add(i);
            }
            for (int i = 0; i < statsPresetNames.Count; i++)
            {
                allPresetLines.Add("[S] " + statsPresetNames[i]);
                allKinds.Add("S");
                allIdx.Add(i);
            }
            for (int i = 0; i < equipPresetNames.Count; i++)
            {
                allPresetLines.Add("[E] " + equipPresetNames[i]);
                allKinds.Add("E");
                allIdx.Add(i);
            }

            if (allPresetLines.Count == 0)
            {
                GUI.Label(new Rect(x + 8f, y + 10f, w - 16f, 50f),
                    new GUIContent("No presets"),
                    sm);
            }
            else
            {
                const float pRow = 24f;
                float pContent = allPresetLines.Count * pRow;
                float pMax = Mathf.Max(0f, pContent - listH + 4f);
                if (e != null && e.type == EventType.ScrollWheel && listRect.Contains(e.mousePosition))
                {
                    presetScroll.y = Mathf.Clamp(presetScroll.y + e.delta.y * 22f, 0f, pMax);
                    e.Use();
                }
                presetScroll.y = Mathf.Clamp(presetScroll.y, 0f, pMax);

                GUI.BeginGroup(new Rect(x + 3f, y + 3f, w - 6f, listH - 6f), GUIContent.none, GUIStyle.none);
                float py = -presetScroll.y;
                for (int i = 0; i < allPresetLines.Count; i++)
                {
                    string kind = allKinds[i];
                    int idx = allIdx[i];
                    bool sel =
                        (kind == "F" && selectedFullPreset == idx) ||
                        (kind == "S" && selectedStatsPreset == idx) ||
                        (kind == "E" && selectedEquipPreset == idx);
                    if (GUI.Button(new Rect(0f, py, w - 12f, 22f), new GUIContent(allPresetLines[i]), sel ? selBtn : btn))
                    {
                        selectedFullPreset = -1;
                        selectedStatsPreset = -1;
                        selectedEquipPreset = -1;
                        if (kind == "F") selectedFullPreset = idx;
                        else if (kind == "S") selectedStatsPreset = idx;
                        else selectedEquipPreset = idx;
                    }
                    py += pRow;
                }
                GUI.EndGroup();
            }

            GUI.DragWindow(new Rect(0f, 0f, presetsPopupRect.width, 28f));

            if (presetNameEditing)
                HandleGenesTextInput(Event.current);
        }

        private static string TruncateForDisplay(string s, int maxChars)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (s.Length <= maxChars) return s;
            if (maxChars <= 1) return "…";
            return s.Substring(0, maxChars - 1) + "…";
        }

        private List<string> GetFilteredCharacters()
        {
            if (string.IsNullOrEmpty(characterFilter))
                return new List<string>(characterOptions);
            string f = characterFilter.ToLowerInvariant();
            List<string> result = new List<string>();
            for (int i = 0; i < characterOptions.Count; i++)
            {
                if (characterOptions[i] != null && characterOptions[i].ToLowerInvariant().Contains(f))
                    result.Add(characterOptions[i]);
            }
            return result;
        }

        private static readonly string[] CharacterNameBlocklist =
        {
            "bandage", "banana", "apple", "food", "seed", "weapon", "prop", "item",
            "bottle", "potion", "crate", "barrel", "chair", "table", "door", "wall",
            "floor", "cube", "sphere", "plane", "camera", "light", "audio", "ui_",
            "debug", "test_", "tmp", "particle", "effect", "vfx", "sfx", "hud",
            "projectile", "bullet", "arrow", "tool", "hammer", "shovel", "bucket",
            "plant", "tree", "rock", "stone", "grass", "water", "lava", "fire",
            "prefab", "spawn", "pool", "manager", "system", "network", "photon"
        };

        private bool IsLikelyCharacterName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;
            if (name.Length < 3 || name.Length > 48)
                return false;
            if (name.StartsWith("System.", StringComparison.OrdinalIgnoreCase))
                return false;

            string lower = name.ToLowerInvariant();

            // Hard reject obvious items / props
            for (int i = 0; i < CharacterNameBlocklist.Length; i++)
            {
                if (lower.Contains(CharacterNameBlocklist[i]))
                    return false;
            }

            // Arm_Bandages style: underscore + lowercase segment often = item
            if (name.IndexOf('_') >= 0)
            {
                // Allow a few character packs that use underscores, but reject
                // "Something_Something" that looks like equipment pieces
                string[] parts = name.Split('_');
                for (int p = 0; p < parts.Length; p++)
                {
                    string pl = parts[p].ToLowerInvariant();
                    if (pl == "arm" || pl == "leg" || pl == "head" || pl == "body" ||
                        pl == "hand" || pl == "foot" || pl == "left" || pl == "right" ||
                        pl == "item" || pl == "gear" || pl == "cloth" || pl == "hat")
                        return false;
                }
            }

            // Prefer PascalCase / letter-starting avatar names (ArgonianMaid, Ralsei, Kobold)
            if (!char.IsLetter(name[0]))
                return false;

            // Reject pure numbers / very generic
            if (lower == "player" || lower == "default" || lower == "null" || lower == "none")
                return false;

            return true;
        }

        private void AddCharacterName(string name, HashSet<string> seen, List<string> into)
        {
            if (!IsLikelyCharacterName(name))
                return;
            if (seen.Add(name))
                into.Add(name);
        }

        /// <summary>
        /// Official path: GameManager.GetPlayerDatabase().GetValidPrefabReferenceInfos()
        /// Each entry has GetKey() — species byte indexes this list.
        /// </summary>
        private object GetGamePlayerDatabase()
        {
            Type gm = SafeGameType("GameManager");
            if (gm == null) return null;
            MethodInfo m = AccessTools.Method(gm, "GetPlayerDatabase");
            if (m == null || !m.IsStatic) return null;
            return m.Invoke(null, null);
        }

        private object GetGamePenisDatabase()
        {
            Type gm = SafeGameType("GameManager");
            if (gm == null) return null;
            MethodInfo m = AccessTools.Method(gm, "GetPenisDatabase");
            if (m == null || !m.IsStatic) return null;
            return m.Invoke(null, null);
        }

        private List<object> GetValidPrefabInfos(object database)
        {
            List<object> result = new List<object>();
            if (database == null) return result;
            MethodInfo getValid = AccessTools.Method(database.GetType(), "GetValidPrefabReferenceInfos");
            if (getValid == null) return result;
            object list = getValid.Invoke(database, null);
            IEnumerable en = list as IEnumerable;
            if (en == null) return result;
            foreach (object item in en)
            {
                if (item != null) result.Add(item);
            }
            return result;
        }

        private string GetPrefabInfoKey(object info)
        {
            if (info == null) return null;
            MethodInfo getKey = AccessTools.Method(info.GetType(), "GetKey");
            if (getKey != null)
            {
                object k = getKey.Invoke(info, null);
                return k != null ? k.ToString() : null;
            }
            FieldInfo nf = AccessTools.Field(info.GetType(), "name")
                ?? AccessTools.Field(info.GetType(), "key");
            if (nf != null)
            {
                object v = nf.GetValue(info);
                return v != null ? v.ToString() : null;
            }
            return info.ToString();
        }

        private void RefreshCharacterList()
        {
            characterOptions.Clear();

            try
            {
                // Official: GameManager.GetPlayerDatabase() → GetValidPrefabReferenceInfos() → GetKey()
                object playerDb = GetGamePlayerDatabase();
                List<object> infos = GetValidPrefabInfos(playerDb);
                for (int i = 0; i < infos.Count; i++)
                {
                    string key = GetPrefabInfoKey(infos[i]);
                    if (!string.IsNullOrEmpty(key))
                        characterOptions.Add(key);
                }

                if (characterOptions.Count == 0)
                {
                    characterOptions.Add("Kobold");
                    geneStatus = "Player DB empty — only default. Join modded room + REFRESH.";
                }
                else
                {
                    geneStatus = "Player DB characters: " + characterOptions.Count + " (species index = list order)";
                }
                geneStatusUntil = Time.unscaledTime + 4f;

                if (selectedCharacterIndex >= characterOptions.Count)
                    selectedCharacterIndex = 0;

                // Sync species label from current genes if possible
                if (speciesId >= 0 && speciesId < characterOptions.Count)
                    speciesName = characterOptions[speciesId];
            }
            catch (Exception ex)
            {
                geneStatus = "Character list error: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("RefreshCharacterList: " + ex);
            }
        }

        private void CollectCharacterNamesFromEnumerable(object source, HashSet<string> seen, List<string> into)
        {
            if (source == null)
                return;

            IDictionary dict = source as IDictionary;
            if (dict != null)
            {
                foreach (object key in dict.Keys)
                {
                    if (key == null) continue;
                    AddCharacterName(key.ToString(), seen, into);
                }
                return;
            }

            IEnumerable list = source as IEnumerable;
            if (list == null || source is string)
                return;

            foreach (object item in list)
            {
                if (item == null) continue;
                string name = null;
                Type it = item.GetType();
                FieldInfo nf = AccessTools.Field(it, "name")
                    ?? AccessTools.Field(it, "Name")
                    ?? AccessTools.Field(it, "prefabName")
                    ?? AccessTools.Field(it, "key")
                    ?? AccessTools.Field(it, "id");
                if (nf != null)
                {
                    object v = nf.GetValue(item);
                    if (v != null) name = v.ToString();
                }
                if (name == null)
                {
                    PropertyInfo np = AccessTools.Property(it, "name")
                        ?? AccessTools.Property(it, "Name");
                    if (np != null)
                    {
                        object v = np.GetValue(item, null);
                        if (v != null) name = v.ToString();
                    }
                }
                if (name == null)
                    name = item.ToString();
                AddCharacterName(name, seen, into);
            }
        }

        private void CollectNamesFromEnumerable(object source, HashSet<string> seen, List<string> into)
        {
            CollectCharacterNamesFromEnumerable(source, seen, into);
        }

        private void TryApplySelectedCharacter()
        {
            if (selectedCharacterIndex < 0 || selectedCharacterIndex >= characterOptions.Count)
            {
                geneStatus = "Select a character first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            // species byte = index in player DB (genetics). Mesh comes from the spawned prefab —
            // we also try a soft respawn with the selected player prefab when possible.
            string charName = characterOptions[selectedCharacterIndex];
            byte speciesByte = (byte)Mathf.Clamp(selectedCharacterIndex, 0, 255);

            Component kob = FindLocalKobold();
            if (kob == null)
            {
                geneStatus = "No local kobold";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            try
            {
                ResolveGeneTypes();
                object genes = getGenesMethod != null ? getGenesMethod.Invoke(kob, null) : null;
                if (genes == null)
                {
                    geneStatus = "GetGenes returned null";
                    geneStatusUntil = Time.unscaledTime + 3f;
                    return;
                }

                // Always update species gene on a CLONE (in-place mutate breaks change detection)
                object work = CloneGenes(genes);
                FieldInfo spField = AccessTools.Field(work.GetType(), "species");
                if (spField != null)
                    spField.SetValue(work, speciesByte);

                if (setGenesMethod != null)
                    setGenesMethod.Invoke(kob, new object[] { work });

                speciesId = selectedCharacterIndex;
                speciesEditText = speciesId.ToString();
                speciesName = charName;

                // Mesh swap: different character = different networked prefab.
                // Attempt Photon destroy + instantiate using player DB prefab key.
                bool respawned = TryRespawnAsCharacter(kob, selectedCharacterIndex, work);

                geneStatus = respawned
                    ? ("Respawned as " + charName)
                    : ("Species gene → " + charName + " (mesh may need leave/rejoin if body didn't change)");
                geneStatusUntil = Time.unscaledTime + 5f;
            }
            catch (Exception ex)
            {
                geneStatus = "SET CHAR failed: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("TryApplySelectedCharacter: " + ex);
            }
        }

        private bool TryRespawnAsCharacter(Component kob, int speciesIndex, object genesObj)
        {
            try
            {
                PhotonView pv = kob.GetComponent<PhotonView>() ?? kob.GetComponentInParent<PhotonView>();
                if (pv == null || !pv.IsMine)
                    return false;

                object playerDb = GetGamePlayerDatabase();
                List<object> infos = GetValidPrefabInfos(playerDb);
                if (speciesIndex < 0 || speciesIndex >= infos.Count)
                    return false;

                // Photon name must be PrefabReferenceInfo.GetKey() (same as selectedPlayerPrefab.GetPrefab())
                string photonName = GetPrefabInfoKey(infos[speciesIndex]);
                if (string.IsNullOrEmpty(photonName))
                    return false;

                Vector3 pos = kob.transform.position;
                Quaternion rot = kob.transform.rotation;

                // Point the in-game character setting at this prefab first
                TrySetSelectedPlayerPrefab(speciesIndex, photonName);

                PhotonNetwork.Destroy(pv.gameObject);
                cachedLocalPlayer = null;
                if (PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.TagObject = null;

                // Match NetworkManager.SpawnControllablePlayerRoutine:
                // BitBuffer + AddKoboldGenes + AddBool(true) then PhotonNetwork.Instantiate
                if (TryOfficialSpawnPlayer(pos, rot, photonName, genesObj, speciesIndex))
                    return true;

                // Last resort bare instantiate
                GameObject spawned = PhotonNetwork.Instantiate(photonName, pos, rot, 0);
                if (spawned == null)
                    return false;

                cachedLocalPlayer = spawned;
                Component newKob = GetKoboldOn(spawned);
                if (newKob != null)
                {
                    if (PhotonNetwork.LocalPlayer != null)
                        PhotonNetwork.LocalPlayer.TagObject = newKob;
                    if (genesObj != null && setGenesMethod != null)
                    {
                        object g2 = CloneGenes(genesObj);
                        FieldInfo sp = AccessTools.Field(g2.GetType(), "species");
                        if (sp != null)
                            sp.SetValue(g2, (byte)Mathf.Clamp(speciesIndex, 0, 255));
                        setGenesMethod.Invoke(newKob, new object[] { g2 });
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("TryRespawnAsCharacter: " + ex.Message);
            }
            return false;
        }

        private void TrySetSelectedPlayerPrefab(int index, string prefabKey)
        {
            try
            {
                Type nmType = SafeGameType("NetworkManager");
                if (nmType == null) return;

                object nm = null;
                FieldInfo instF = AccessTools.Field(nmType, "instance") ?? AccessTools.Field(nmType, "Instance");
                if (instF != null)
                    nm = instF.GetValue(null);
                if (nm == null)
                {
                    UnityEngine.Object[] found = UnityEngine.Object.FindObjectsOfType(nmType);
                    if (found != null && found.Length > 0)
                        nm = found[0];
                }
                if (nm == null) return;

                FieldInfo prefabField = AccessTools.Field(nmType, "selectedPlayerPrefab");
                if (prefabField == null) return;
                object setting = prefabField.GetValue(nm);
                if (setting == null) return;

                MethodInfo setVal = AccessTools.Method(setting.GetType(), "SetValue", new Type[] { typeof(int) });
                if (setVal != null)
                    setVal.Invoke(setting, new object[] { index });

                FieldInfo selPrefab = AccessTools.Field(setting.GetType(), "selectedPrefab");
                if (selPrefab != null && selPrefab.FieldType == typeof(string))
                    selPrefab.SetValue(setting, prefabKey);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("TrySetSelectedPlayerPrefab: " + ex.Message);
            }
        }

        private bool TryOfficialSpawnPlayer(Vector3 pos, Quaternion rot, string photonName, object genesObj, int speciesIndex)
        {
            try
            {
                Type bitBufferType = SafeGameType("NetStack.Serialization.BitBuffer")
                    ?? SafeGameType("BitBuffer");
                if (bitBufferType == null)
                    return false;

                object buffer = Activator.CreateInstance(bitBufferType, new object[] { 16 });
                if (buffer == null)
                    return false;

                object genesToWrite = genesObj;
                Type loaderType = SafeGameType("PlayerKoboldLoader");
                if (loaderType != null)
                {
                    MethodInfo getPlayerGenes = AccessTools.Method(loaderType, "GetPlayerGenes");
                    if (getPlayerGenes != null && getPlayerGenes.IsStatic)
                    {
                        object pg = getPlayerGenes.Invoke(null, null);
                        if (pg != null)
                            genesToWrite = pg;
                    }
                }

                if (genesToWrite != null)
                {
                    genesToWrite = CloneGenes(genesToWrite);
                    FieldInfo sp = AccessTools.Field(genesToWrite.GetType(), "species");
                    if (sp != null)
                        sp.SetValue(genesToWrite, (byte)Mathf.Clamp(speciesIndex, 0, 255));

                    Type extType = SafeGameType("KoboldGenesBitBufferExtension");
                    MethodInfo addGenes = extType != null
                        ? AccessTools.Method(extType, "AddKoboldGenes")
                        : null;
                    if (addGenes == null)
                        addGenes = AccessTools.Method(bitBufferType, "AddKoboldGenes");

                    if (addGenes != null)
                    {
                        if (addGenes.IsStatic)
                            addGenes.Invoke(null, new object[] { buffer, genesToWrite });
                        else
                            addGenes.Invoke(buffer, new object[] { genesToWrite });
                    }
                }

                MethodInfo addBool = AccessTools.Method(bitBufferType, "AddBool", new Type[] { typeof(bool) });
                if (addBool != null)
                    addBool.Invoke(buffer, new object[] { true }); // Is player kobold

                GameObject spawned = PhotonNetwork.Instantiate(photonName, pos, rot, 0, new object[] { buffer });
                if (spawned == null)
                    return false;

                cachedLocalPlayer = spawned;
                Component newKob = GetKoboldOn(spawned);
                if (newKob != null && PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.TagObject = newKob;

                try
                {
                    Type cdType = SafeGameType("CharacterDescriptor");
                    if (cdType != null)
                    {
                        Component cd = spawned.GetComponentInChildren(cdType, true);
                        if (cd != null)
                        {
                            MethodInfo setEye = AccessTools.Method(cdType, "SetEyeDir");
                            if (setEye != null)
                                setEye.Invoke(cd, new object[] { rot * Vector3.forward });
                        }
                    }
                }
                catch { }

                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("TryOfficialSpawnPlayer: " + ex.Message);
                return false;
            }
        }

        private void ResolveGeneTypes()
        {
            if (geneTypesResolved)
                return;
            geneTypesResolved = true;

            try
            {
                koboldType = SafeGameType("Kobold");
                koboldGenesType = SafeGameType("KoboldGenes");
                if (koboldGenesType == null)
                    koboldGenesType = SafeGameType("KoboldKare.KoboldGenes");

                if (koboldType != null)
                {
                    getGenesMethod = AccessTools.Method(koboldType, "GetGenes");
                    setGenesMethod = AccessTools.Method(koboldType, "SetGenes", new Type[] { koboldGenesType });
                    if (setGenesMethod == null && koboldGenesType != null)
                        setGenesMethod = AccessTools.Method(koboldType, "SetGenes");
                }

                if (koboldType == null || koboldGenesType == null)
                    geneStatus = "Gene types not found — open game with Assembly-CSharp loaded";
                else if (getGenesMethod == null)
                    geneStatus = "Kobold found, but GetGenes missing";
                else
                    geneStatus = "Ready — press REFRESH";
            }
            catch (Exception ex)
            {
                geneStatus = "Resolve failed: " + ex.Message;
                Logger.LogWarning("Genes resolve: " + ex);
            }

            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                geneCurrent[i] = geneFieldDefs[i].DefaultValue;
                geneToSet[i] = geneFieldDefs[i].DefaultValue;
                geneToSetText[i] = geneFieldDefs[i].DefaultValue.ToString("0.##");
            }
        }

        /// <summary>
        /// True only for actual player kobold bodies — not bananas, doors, props, etc.
        /// Official game stores the Kobold component on Player.TagObject.
        /// </summary>
        private bool IsValidPlayerKoboldObject(GameObject go)
        {
            if (go == null)
                return false;

            ResolveGeneTypes();
            if (koboldType == null)
                return false;

            // Must have a Kobold on this object (not only buried deep under a prop hierarchy)
            Component kob = go.GetComponent(koboldType);
            if (kob == null)
            {
                // TagObject is sometimes the Kobold component's gameObject already
                kob = go.GetComponentInChildren(koboldType, true);
                if (kob == null)
                    return false;
                // Prefer the kobold's own gameObject as root
                go = kob.gameObject;
            }

            PhotonView view = go.GetComponent<PhotonView>();
            if (view == null)
                view = go.GetComponentInParent<PhotonView>();
            if (view == null)
                return false;

            // Must be owned/controlled by a real Photon player (not scene objects)
            if (view.Owner == null && view.Controller == null && !view.IsMine)
                return false;

            return true;
        }

        private Component GetKoboldOn(GameObject go)
        {
            if (go == null)
                return null;
            ResolveGeneTypes();
            if (koboldType == null)
                return null;

            Component k = go.GetComponent(koboldType);
            if (k != null)
                return k;
            return go.GetComponentInChildren(koboldType, true);
        }

        private Component FindLocalKobold()
        {
            // 1) Official path: TagObject is the Kobold itself
            if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
            {
                object tag = PhotonNetwork.LocalPlayer.TagObject;
                if (tag != null)
                {
                    if (koboldType != null && koboldType.IsInstanceOfType(tag))
                        return (Component)tag;

                    Component asComp = tag as Component;
                    if (asComp != null)
                    {
                        Component k = GetKoboldOn(asComp.gameObject);
                        if (k != null)
                            return k;
                    }

                    GameObject asGo = tag as GameObject;
                    if (asGo != null)
                    {
                        Component k = GetKoboldOn(asGo);
                        if (k != null)
                            return k;
                    }
                }
            }

            // 2) Cached root (do NOT call GetLocalPlayer here — it can call us)
            if (cachedLocalPlayer != null && IsValidPlayerKoboldObject(cachedLocalPlayer))
            {
                Component k = GetKoboldOn(cachedLocalPlayer);
                if (k != null)
                    return k;
            }

            // 3) Last resort: PhotonView.IsMine that actually has a Kobold
            PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            if (views != null)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    PhotonView view = views[i];
                    if (view == null || !view.IsMine)
                        continue;
                    Component k = GetKoboldOn(view.gameObject);
                    if (k != null)
                    {
                        cachedLocalPlayer = k.gameObject;
                        return k;
                    }
                }
            }

            return null;
        }

        private FieldInfo FindGeneField(object genes, string[] names)
        {
            if (genes == null)
                return null;
            Type t = genes.GetType();
            for (int i = 0; i < names.Length; i++)
            {
                FieldInfo f = AccessTools.Field(t, names[i]);
                if (f != null)
                    return f;
            }
            // Property fallback
            return null;
        }

        private float ReadGeneValue(object genes, GeneFieldDef def)
        {
            FieldInfo f = FindGeneField(genes, def.FieldNames);
            if (f == null)
                return def.DefaultValue;
            try
            {
                object v = f.GetValue(genes);
                if (v is float) return (float)v;
                if (v is double) return (float)(double)v;
                if (v is int) return (int)v;
                if (v is byte) return (byte)v;
                if (v is short) return (short)v;
                float parsed;
                if (v != null && float.TryParse(v.ToString(), out parsed))
                    return parsed;
            }
            catch { }
            return def.DefaultValue;
        }

        private void WriteGeneValue(object genes, GeneFieldDef def, float value)
        {
            FieldInfo f = FindGeneField(genes, def.FieldNames);
            if (f == null)
                return;
            try
            {
                value = Mathf.Clamp(value, def.Min, def.Max);
                Type ft = f.FieldType;
                if (ft == typeof(float))
                    f.SetValue(genes, value);
                else if (ft == typeof(double))
                    f.SetValue(genes, (double)value);
                else if (ft == typeof(int))
                    f.SetValue(genes, Mathf.RoundToInt(value));
                else if (ft == typeof(byte))
                    f.SetValue(genes, (byte)Mathf.Clamp(Mathf.RoundToInt(value), 0, 255));
                else if (ft == typeof(short))
                    f.SetValue(genes, (short)Mathf.RoundToInt(value));
                else
                    f.SetValue(genes, Convert.ChangeType(value, ft));
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Write gene " + def.Label + ": " + ex.Message);
            }
        }

        private void RefreshGenesFromKobold()
        {
            ResolveGeneTypes();
            Component kob = FindLocalKobold();
            if (kob == null)
            {
                geneStatus = "No local kobold found (join a room / spawn in)";
                geneStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            object genes = null;
            try
            {
                if (getGenesMethod != null)
                    genes = getGenesMethod.Invoke(kob, null);
            }
            catch (Exception ex)
            {
                geneStatus = "GetGenes failed: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            if (genes == null)
            {
                // Try public field "genes" / "Genes"
                FieldInfo gf = AccessTools.Field(kob.GetType(), "genes")
                    ?? AccessTools.Field(kob.GetType(), "Genes");
                if (gf != null)
                    genes = gf.GetValue(kob);
            }

            if (genes == null)
            {
                geneStatus = "Could not read genes object";
                geneStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                float v = ReadGeneValue(genes, geneFieldDefs[i]);
                geneCurrent[i] = v;
                geneToSet[i] = v;
                geneToSetText[i] = v.ToString("0.##");
            }

            float thick = ReadGeneValue(genes, new GeneFieldDef("thick",
                new[] { "dickThickness", "DickThickness", "cockThickness" }, cockThickness, 0.1f, 3f));
            cockThickness = thick;

            FieldInfo spF = FindGeneField(genes, new[] { "species", "Species" });
            if (spF != null)
            {
                try
                {
                    object sv = spF.GetValue(genes);
                    if (sv is int) speciesId = (int)sv;
                    else if (sv is byte) speciesId = (byte)sv;
                    else if (sv is float) speciesId = Mathf.RoundToInt((float)sv);
                    else
                    {
                        int parsed;
                        if (sv != null && int.TryParse(sv.ToString(), out parsed))
                            speciesId = parsed;
                    }
                    speciesEditText = speciesId.ToString();
                    speciesName = speciesId == 0 ? "Kobold" : ("id " + speciesId);
                }
                catch { }
            }

            geneStatus = "Loaded from local kobold";
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        private void ApplyGenesToKobold()
        {
            ResolveGeneTypes();
            Component kob = FindLocalKobold();
            if (kob == null)
            {
                geneStatus = "No local kobold found";
                geneStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            object genes = null;
            try
            {
                if (getGenesMethod != null)
                    genes = getGenesMethod.Invoke(kob, null);
            }
            catch { }

            if (genes == null)
            {
                FieldInfo gf = AccessTools.Field(kob.GetType(), "genes")
                    ?? AccessTools.Field(kob.GetType(), "Genes");
                if (gf != null)
                    genes = gf.GetValue(kob);
            }

            if (genes == null)
            {
                geneStatus = "Could not get genes to write";
                geneStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            // If genes is a struct, box-modify-unbox via SetGenes
            bool isValueType = genes.GetType().IsValueType;
            object work = genes;

            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                float parsed = geneToSet[i];
                if (geneToSetText[i] != null)
                {
                    float t;
                    if (float.TryParse(geneToSetText[i], out t))
                        parsed = t;
                }
                parsed = Mathf.Clamp(parsed, geneFieldDefs[i].Min, geneFieldDefs[i].Max);
                geneToSet[i] = parsed;
                WriteGeneValue(work, geneFieldDefs[i], parsed);
            }

            try
            {
                if (setGenesMethod != null)
                {
                    setGenesMethod.Invoke(kob, new object[] { work });
                }
                else
                {
                    FieldInfo gf = AccessTools.Field(kob.GetType(), "genes")
                        ?? AccessTools.Field(kob.GetType(), "Genes");
                    if (gf != null)
                        gf.SetValue(kob, work);
                }
            }
            catch (Exception ex)
            {
                geneStatus = "SetGenes failed: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("ApplyGenes: " + ex);
                return;
            }

            // Also push thickness + species if fields exist
            try
            {
                FieldInfo thickF = FindGeneField(work, new[] { "dickThickness", "DickThickness", "cockThickness" });
                if (thickF != null)
                    WriteGeneValue(work, new GeneFieldDef("thick", new[] { thickF.Name }, cockThickness, 0.1f, 3f), cockThickness);

                FieldInfo spF = FindGeneField(work, new[] { "species", "Species" });
                if (spF != null)
                {
                    if (spF.FieldType == typeof(int) || spF.FieldType == typeof(byte))
                        spF.SetValue(work, Convert.ChangeType(speciesId, spF.FieldType));
                    else if (spF.FieldType == typeof(float))
                        spF.SetValue(work, (float)speciesId);
                }

                if (setGenesMethod != null)
                    setGenesMethod.Invoke(kob, new object[] { work });
            }
            catch (Exception ex)
            {
                Logger.LogWarning("Apply species/thick: " + ex.Message);
            }

            // Re-read to confirm
            RefreshGenesFromKobold();
            geneStatus = "Applied to local kobold";
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        /// <summary>
        /// Clone KoboldGenes so SetGenes sees a real change.
        /// Mutating the live GetGenes() object in place makes dickEquip != GetGenes().dickEquip always false.
        /// </summary>
        private object CloneGenes(object genes)
        {
            if (genes == null)
                return null;
            try
            {
                MethodInfo mc = AccessTools.Method(typeof(object), "MemberwiseClone");
                if (mc != null)
                    return mc.Invoke(genes, null);
            }
            catch { }

            try
            {
                object copy = Activator.CreateInstance(genes.GetType());
                FieldInfo[] fields = genes.GetType().GetFields(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < fields.Length; i++)
                {
                    if (fields[i].IsStatic) continue;
                    fields[i].SetValue(copy, fields[i].GetValue(genes));
                }
                return copy;
            }
            catch
            {
                return genes;
            }
        }

        private void RefreshDickOptions()
        {
            dickOptions.Clear();
            // CommandDick.unEquipID = 0; real dicks are 1..Count (database[id-1])
            dickOptions.Add("None|id:0");
            try
            {
                object penisDb = GetGamePenisDatabase();
                List<object> infos = GetValidPrefabInfos(penisDb);
                for (int i = 0; i < infos.Count; i++)
                {
                    string key = GetPrefabInfoKey(infos[i]);
                    if (string.IsNullOrEmpty(key)) key = "Dick_" + i;
                    dickOptions.Add(key + "|id:" + (i + 1));
                }

                if (dickOptions.Count <= 1)
                    geneStatus = "Penis DB empty — only None.";
                else
                    geneStatus = "Penis DB: " + (dickOptions.Count - 1) + " (1-based ids)";
                geneStatusUntil = Time.unscaledTime + 3f;
            }
            catch (Exception ex)
            {
                geneStatus = "Dick refresh error: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("RefreshDickOptions: " + ex);
            }

            if (selectedDickIndex >= dickOptions.Count)
                selectedDickIndex = 0;
        }

        private void TryEquipSelectedDick()
        {
            Component kob = FindLocalKobold();
            if (kob == null)
            {
                geneStatus = "No local kobold — can't equip";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            if (selectedDickIndex < 0 || selectedDickIndex >= dickOptions.Count)
            {
                geneStatus = "Select a dick entry first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            string entry = dickOptions[selectedDickIndex];
            short dickId = 0; // unEquipID
            int idMark = entry.LastIndexOf("|id:", StringComparison.Ordinal);
            if (idMark >= 0)
            {
                short parsed;
                if (short.TryParse(entry.Substring(idMark + 4), out parsed))
                    dickId = parsed;
            }

            try
            {
                // Same path as /dick cheat: RPC SetDickRPC so all clients swap the mesh
                PhotonView pv = kob.GetComponent<PhotonView>();
                if (pv == null)
                    pv = kob.GetComponentInParent<PhotonView>();

                if (pv != null && pv.IsMine)
                {
                    pv.RPC("SetDickRPC", RpcTarget.All, dickId);
                }
                else
                {
                    ResolveGeneTypes();
                    object genes = getGenesMethod != null ? getGenesMethod.Invoke(kob, null) : null;
                    if (genes == null)
                    {
                        geneStatus = "GetGenes null";
                        return;
                    }
                    object work = CloneGenes(genes);
                    FieldInfo dickField = AccessTools.Field(work.GetType(), "dickEquip");
                    if (dickField != null)
                        dickField.SetValue(work, dickId);
                    FieldInfo thickField = AccessTools.Field(work.GetType(), "dickThickness");
                    if (thickField != null)
                        thickField.SetValue(work, cockThickness);
                    if (setGenesMethod != null)
                        setGenesMethod.Invoke(kob, new object[] { work });
                }

                // Apply thickness after RPC (SetDickRPC only sets dickEquip)
                {
                    ResolveGeneTypes();
                    object genes = getGenesMethod != null ? getGenesMethod.Invoke(kob, null) : null;
                    if (genes != null && setGenesMethod != null)
                    {
                        object work = CloneGenes(genes);
                        FieldInfo thickField = AccessTools.Field(work.GetType(), "dickThickness");
                        if (thickField != null)
                        {
                            thickField.SetValue(work, cockThickness);
                            setGenesMethod.Invoke(kob, new object[] { work });
                        }
                    }
                }

                geneStatus = "Equipped dick id=" + dickId + " (" + entry + ")";
                geneStatusUntil = Time.unscaledTime + 4f;
                RefreshEquipmentList();
            }
            catch (Exception ex)
            {
                geneStatus = "Equip failed: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("TryEquipSelectedDick: " + ex);
            }
        }

        private Component GetLocalInventory()
        {
            Component kob = FindLocalKobold();
            if (kob == null) return null;
            Type invType = SafeGameType("KoboldInventory");
            if (invType == null) return null;
            return kob.GetComponent(invType) ?? kob.GetComponentInChildren(invType, true);
        }

        private List<string> GetFilteredEquipCatalog()
        {
            if (string.IsNullOrEmpty(equipFilter))
                return new List<string>(equipCatalog);
            string f = equipFilter.ToLowerInvariant();
            List<string> result = new List<string>();
            for (int i = 0; i < equipCatalog.Count; i++)
            {
                if (equipCatalog[i] != null && equipCatalog[i].ToLowerInvariant().Contains(f))
                    result.Add(equipCatalog[i]);
            }
            return result;
        }

        private void RefreshEquipmentCatalog()
        {
            equipCatalog.Clear();
            try
            {
                // EquipmentDatabase.GetAssetKeys() / GetAssets()
                Type dbType = SafeGameType("EquipmentDatabase");
                if (dbType == null)
                {
                    equipStatus = "EquipmentDatabase type missing";
                    return;
                }

                MethodInfo getKeys = AccessTools.Method(dbType, "GetAssetKeys");
                if (getKeys != null && getKeys.IsStatic)
                {
                    object keysObj = getKeys.Invoke(null, null);
                    IEnumerable keys = keysObj as IEnumerable;
                    if (keys != null)
                    {
                        foreach (object k in keys)
                        {
                            if (k == null) continue;
                            string name = k.ToString();
                            if (!string.IsNullOrEmpty(name))
                                equipCatalog.Add(name);
                        }
                    }
                }

                if (equipCatalog.Count == 0)
                {
                    MethodInfo getAssets = AccessTools.Method(dbType, "GetAssets");
                    if (getAssets != null && getAssets.IsStatic)
                    {
                        object assetsObj = getAssets.Invoke(null, null);
                        IEnumerable assets = assetsObj as IEnumerable;
                        if (assets != null)
                        {
                            foreach (object a in assets)
                            {
                                if (a == null) continue;
                                // ScriptableObject.name
                                string name = null;
                                try
                                {
                                    PropertyInfo np = a.GetType().GetProperty("name");
                                    if (np != null)
                                    {
                                        object v = np.GetValue(a, null);
                                        if (v != null) name = v.ToString();
                                    }
                                }
                                catch { }
                                if (string.IsNullOrEmpty(name))
                                    name = a.ToString();
                                if (!string.IsNullOrEmpty(name))
                                    equipCatalog.Add(name);
                            }
                        }
                    }
                }

                equipCatalog.Sort(StringComparer.OrdinalIgnoreCase);
                equipStatus = equipCatalog.Count > 0
                    ? ("Catalog: " + equipCatalog.Count + " items")
                    : "Equipment catalog empty";
                if (selectedCatalogEquip >= equipCatalog.Count)
                    selectedCatalogEquip = -1;
            }
            catch (Exception ex)
            {
                equipStatus = "Catalog error: " + ex.Message;
                Logger.LogWarning("RefreshEquipmentCatalog: " + ex);
            }
        }

        private void RefreshEquipmentList()
        {
            equipNames.Clear();
            Component inv = GetLocalInventory();
            if (inv == null)
            {
                equipStatus = "No KoboldInventory on local kobold";
                return;
            }

            try
            {
                MethodInfo getAll = AccessTools.Method(inv.GetType(), "GetAllEquipment");
                if (getAll != null)
                {
                    object result = getAll.Invoke(inv, null);
                    IEnumerable list = result as IEnumerable;
                    if (list != null)
                    {
                        foreach (object item in list)
                        {
                            if (item == null) continue;
                            string name = null;
                            try
                            {
                                PropertyInfo np = item.GetType().GetProperty("name");
                                if (np != null)
                                {
                                    object v = np.GetValue(item, null);
                                    if (v != null) name = v.ToString();
                                }
                            }
                            catch { }
                            if (string.IsNullOrEmpty(name))
                                name = item.ToString();
                            equipNames.Add(name);
                        }
                    }
                }

                if (selectedWornEquip >= equipNames.Count)
                    selectedWornEquip = -1;
            }
            catch (Exception ex)
            {
                equipStatus = "Worn list error: " + ex.Message;
                Logger.LogWarning("RefreshEquipmentList: " + ex);
            }
        }

        private void SetEquipStatus(string msg, float seconds = 4f)
        {
            equipStatus = msg ?? "";
            equipStatusUntil = Time.unscaledTime + Mathf.Max(0.5f, seconds);
        }

        private void TryWearSelectedEquipment()
        {
            if (selectedCatalogEquip < 0 || selectedCatalogEquip >= equipCatalog.Count)
            {
                SetEquipStatus("Select an item from All list first");
                return;
            }

            string equipName = equipCatalog[selectedCatalogEquip];
            if (!TryWearEquipmentByName(equipName))
            {
                // TryWearEquipmentByName already set status on failure paths it owns;
                // if it returned false without a message, leave a generic one.
                if (string.IsNullOrEmpty(equipStatus))
                    SetEquipStatus("Wear failed for: " + equipName, 5f);
            }
        }

        /// <summary>
        /// Official path: KoboldInventory.PickupEquipmentRPC(short equipmentID, int groundPrefabViewId)
        /// with groundPrefabViewId = 0 (no world prop). Falls back to local PickupEquipment(equip, null).
        /// </summary>
        private bool TryWearEquipmentCore(string equipName)
        {
            if (string.IsNullOrEmpty(equipName))
                return false;

            Component inv = GetLocalInventory();
            if (inv == null)
            {
                SetEquipStatus("No KoboldInventory");
                return false;
            }

            Type dbType = SafeGameType("EquipmentDatabase");
            if (dbType == null)
            {
                SetEquipStatus("EquipmentDatabase type missing");
                return false;
            }

            // Resolve Equipment asset by name (Database key == ScriptableObject.name)
            object equipAsset = null;
            MethodInfo[] methods = dbType.GetMethods(BindingFlags.Public | BindingFlags.Static);
            MethodInfo tryGetByName = null;
            MethodInfo getIdMethod = null;
            for (int i = 0; i < methods.Length; i++)
            {
                ParameterInfo[] ps = methods[i].GetParameters();
                if (methods[i].Name == "TryGetAsset" && ps.Length == 2 && ps[0].ParameterType == typeof(string))
                    tryGetByName = methods[i];
                if (methods[i].Name == "GetID" && ps.Length == 1)
                    getIdMethod = methods[i];
            }

            if (tryGetByName != null)
            {
                object[] args = new object[] { equipName, null };
                bool ok = (bool)tryGetByName.Invoke(null, args);
                if (ok)
                    equipAsset = args[1];
            }

            // Case-insensitive fallback scan of GetAssets()
            if (equipAsset == null)
            {
                MethodInfo getAssets = AccessTools.Method(dbType, "GetAssets", Type.EmptyTypes);
                if (getAssets != null)
                {
                    object listObj = getAssets.Invoke(null, null);
                    IList list = listObj as IList;
                    if (list != null)
                    {
                        for (int i = 0; i < list.Count; i++)
                        {
                            object item = list[i];
                            if (item == null) continue;
                            string n = null;
                            try
                            {
                                // UnityEngine.Object.name
                                PropertyInfo np = item.GetType().GetProperty("name");
                                if (np != null)
                                {
                                    object v = np.GetValue(item, null);
                                    if (v != null) n = v.ToString();
                                }
                            }
                            catch { }
                            if (n != null && string.Equals(n, equipName, StringComparison.OrdinalIgnoreCase))
                            {
                                equipAsset = item;
                                break;
                            }
                        }
                    }
                }
            }

            if (equipAsset == null)
            {
                SetEquipStatus("Not in EquipmentDatabase: " + equipName);
                return false;
            }

            // Prefer network RPC so other clients see the equip (and matches game pickup path)
            short equipId = 0;
            bool haveId = false;
            if (getIdMethod != null)
            {
                try
                {
                    object idObj = getIdMethod.Invoke(null, new object[] { equipAsset });
                    if (idObj is short)
                    {
                        equipId = (short)idObj;
                        haveId = true;
                    }
                    else if (idObj is int)
                    {
                        equipId = (short)(int)idObj;
                        haveId = true;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("GetID failed: " + ex.Message);
                }
            }

            PhotonView invPv = inv.GetComponent<PhotonView>();
            if (invPv == null)
                invPv = inv.GetComponentInParent<PhotonView>();

            Exception lastEx = null;

            // Path A: official RPC — groundPrefabID 0 → no world object
            if (haveId && invPv != null && invPv.ViewID > 0)
            {
                try
                {
                    invPv.RPC("PickupEquipmentRPC", RpcTarget.AllBuffered, equipId, 0);
                    SetEquipStatus("Worn: " + equipName + " (rpc id=" + equipId + ")");
                    RefreshEquipmentList();
                    return true;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    Logger.LogWarning("PickupEquipmentRPC failed, trying local: " + ex);
                }
            }

            // Path B: local PickupEquipment(equip, null) — same as ReplaceEquipmentWith
            try
            {
                MethodInfo pickup = AccessTools.Method(inv.GetType(), "PickupEquipment");
                if (pickup == null)
                {
                    SetEquipStatus("PickupEquipment missing");
                    return false;
                }

                // Some equipment OnEquip paths crash when groundPrefab is null AND
                // wearable internal refs are broken — still the official offline path.
                pickup.Invoke(inv, new object[] { equipAsset, null });
                SetEquipStatus("Worn: " + equipName + " (local)");
                RefreshEquipmentList();
                return true;
            }
            catch (Exception ex)
            {
                // Unwrap TargetInvocationException for the real message
                Exception root = ex;
                while (root.InnerException != null)
                    root = root.InnerException;

                string detail = root.Message;
                if (root is ArgumentNullException ane && !string.IsNullOrEmpty(ane.ParamName))
                    detail = "null arg '" + ane.ParamName + "' (often missing attach point / prefab on this body)";

                SetEquipStatus("WEAR failed: " + detail, 8f);
                Logger.LogWarning("TryWearEquipmentCore local failed for " + equipName + ": " + ex);
                if (lastEx != null)
                    Logger.LogWarning("  prior RPC error: " + lastEx);
                return false;
            }
        }

        private void TryRemoveSelectedEquipment()
        {
            if (selectedWornEquip < 0 || selectedWornEquip >= equipNames.Count)
            {
                SetEquipStatus("Select a worn item first");
                return;
            }

            string equipName = equipNames[selectedWornEquip];
            Component inv = GetLocalInventory();
            if (inv == null)
            {
                SetEquipStatus("No KoboldInventory");
                return;
            }

            try
            {
                MethodInfo getAll = AccessTools.Method(inv.GetType(), "GetAllEquipment");
                object listObj = getAll != null ? getAll.Invoke(inv, null) : null;
                IList list = listObj as IList;
                object target = null;
                if (list != null && selectedWornEquip < list.Count)
                    target = list[selectedWornEquip];

                if (target == null)
                {
                    // Match by name
                    IEnumerable en = listObj as IEnumerable;
                    if (en != null)
                    {
                        foreach (object item in en)
                        {
                            if (item == null) continue;
                            string n = null;
                            try
                            {
                                PropertyInfo np = item.GetType().GetProperty("name");
                                if (np != null)
                                {
                                    object v = np.GetValue(item, null);
                                    if (v != null) n = v.ToString();
                                }
                            }
                            catch { }
                            if (n == equipName)
                            {
                                target = item;
                                break;
                            }
                        }
                    }
                }

                if (target == null)
                {
                    SetEquipStatus("Couldn't resolve worn item");
                    return;
                }

                // RemoveEquipment(Equipment thing, bool dropOnGround)
                MethodInfo remove = null;
                MethodInfo[] methods = inv.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance);
                for (int i = 0; i < methods.Length; i++)
                {
                    if (methods[i].Name != "RemoveEquipment") continue;
                    ParameterInfo[] ps = methods[i].GetParameters();
                    if (ps.Length == 2 && ps[1].ParameterType == typeof(bool))
                    {
                        // Prefer Equipment overload over EquipmentSlot
                        if (ps[0].ParameterType.Name == "Equipment" || ps[0].ParameterType.IsAssignableFrom(target.GetType()))
                        {
                            remove = methods[i];
                            break;
                        }
                        if (remove == null)
                            remove = methods[i];
                    }
                }

                if (remove == null)
                {
                    SetEquipStatus("RemoveEquipment missing");
                    return;
                }

                remove.Invoke(inv, new object[] { target, false });
                SetEquipStatus("Removed: " + equipName);
                selectedWornEquip = -1;
                RefreshEquipmentList();
            }
            catch (Exception ex)
            {
                SetEquipStatus("REMOVE failed: " + ex.Message, 6f);
                Logger.LogWarning("TryRemoveSelectedEquipment: " + ex);
            }
        }

        // ============================================================
        // FULL PRESETS (character + genes + clothing)
        // payload v1:
        //   v1|charName|speciesId|thickness|g0,g1,...|dickIndex|dickEntry|equip1,equip2,...
        // ============================================================
        private void SaveCurrentFullPreset()
        {
            if (string.IsNullOrEmpty(newPresetName))
            {
                geneStatus = "Enter a preset name first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            RefreshEquipmentList();

            string charName = "";
            if (selectedCharacterIndex >= 0 && selectedCharacterIndex < characterOptions.Count)
                charName = characterOptions[selectedCharacterIndex] ?? "";
            if (string.IsNullOrEmpty(charName) && !string.IsNullOrEmpty(speciesName))
                charName = speciesName;

            string dickEntry = "";
            if (selectedDickIndex >= 0 && selectedDickIndex < dickOptions.Count)
                dickEntry = dickOptions[selectedDickIndex] ?? "";

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("v1|");
            sb.Append(SanitizePresetToken(charName)).Append('|');
            sb.Append(speciesId).Append('|');
            sb.Append(cockThickness.ToString("0.###")).Append('|');

            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                if (i > 0) sb.Append(',');
                float v = geneToSet[i];
                if (geneToSetText[i] != null)
                {
                    float t;
                    if (float.TryParse(geneToSetText[i], out t))
                        v = t;
                }
                sb.Append(v.ToString("0.###"));
            }
            sb.Append('|');
            sb.Append(selectedDickIndex).Append('|');
            sb.Append(SanitizePresetToken(dickEntry)).Append('|');

            // worn clothes (comma-separated, tokens sanitized)
            for (int i = 0; i < equipNames.Count; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(SanitizePresetToken(equipNames[i]));
            }

            string key = newPresetName.Trim();
            fullPresetData[key] = sb.ToString();
            if (!fullPresetNames.Contains(key))
                fullPresetNames.Add(key);
            selectedFullPreset = fullPresetNames.IndexOf(key);
            selectedStatsPreset = -1;
            selectedEquipPreset = -1;
            SaveGenePresetsToConfig();

            geneStatus = "Saved FULL preset: " + key +
                         " (char=" + (string.IsNullOrEmpty(charName) ? "?" : charName) +
                         ", genes, " + equipNames.Count + " clothes)";
            geneStatusUntil = Time.unscaledTime + 4f;
        }

        private static string SanitizePresetToken(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            // Keep payload pipe/semicolon safe
            return s.Replace("|", "/").Replace(";", ",").Replace("\n", " ").Replace("\r", "");
        }

        private void ApplySelectedFullPreset()
        {
            // Prefer [F]; if user selected legacy [S]/[E] alone, fall back
            if (selectedFullPreset >= 0 && selectedFullPreset < fullPresetNames.Count)
            {
                string key = fullPresetNames[selectedFullPreset];
                string payload;
                if (!fullPresetData.TryGetValue(key, out payload) || string.IsNullOrEmpty(payload))
                {
                    geneStatus = "Full preset data missing";
                    geneStatusUntil = Time.unscaledTime + 3f;
                    return;
                }

                if (applyFullPresetCoroutine != null)
                {
                    StopCoroutine(applyFullPresetCoroutine);
                    applyFullPresetCoroutine = null;
                }
                applyFullPresetCoroutine = StartCoroutine(ApplyFullPresetRoutine(key, payload));
                return;
            }

            if (selectedStatsPreset >= 0)
            {
                LoadSelectedStatsPreset();
                ApplyGenesToKobold();
                return;
            }

            if (selectedEquipPreset >= 0)
            {
                LoadSelectedEquipPreset();
                return;
            }

            geneStatus = "Select a [F] preset first";
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        private IEnumerator ApplyFullPresetRoutine(string key, string payload)
        {
            geneStatus = "Applying full preset: " + key + "...";
            geneStatusUntil = Time.unscaledTime + 8f;

            string[] parts = payload.Split('|');
            // v1|char|species|thick|genes|dickIdx|dickEntry|equips
            if (parts.Length < 5 || parts[0] != "v1")
            {
                geneStatus = "Bad full preset format (need v1)";
                geneStatusUntil = Time.unscaledTime + 4f;
                applyFullPresetCoroutine = null;
                yield break;
            }

            string charName = parts.Length > 1 ? parts[1] : "";
            int sid = speciesId;
            if (parts.Length > 2) int.TryParse(parts[2], out sid);
            float thick = cockThickness;
            if (parts.Length > 3) float.TryParse(parts[3], out thick);
            string genesCsv = parts.Length > 4 ? parts[4] : "";
            int dickIdx = selectedDickIndex;
            if (parts.Length > 5) int.TryParse(parts[5], out dickIdx);
            string dickEntry = parts.Length > 6 ? parts[6] : "";
            string equipsCsv = parts.Length > 7 ? parts[7] : "";

            // --- 1) Character ---
            if (characterOptions.Count == 0)
                RefreshCharacterList();

            int charIdx = FindCharacterIndexForPreset(charName, key, sid);

            if (charIdx >= 0)
            {
                selectedCharacterIndex = charIdx;
                speciesId = charIdx;
                speciesEditText = speciesId.ToString();
                speciesName = characterOptions[charIdx];
                geneStatus = "Applying full preset: " + key + " → char " + speciesName + "...";
                TryApplySelectedCharacter();
                // Wait for possible respawn / gene write
                yield return new WaitForSecondsRealtime(0.75f);
            }
            else if (!string.IsNullOrEmpty(charName))
            {
                Logger.LogWarning("Full preset '" + key + "': no character match for '" + charName +
                                  "' (" + characterOptions.Count + " options loaded)");
            }
            else
            {
                speciesId = sid;
                speciesEditText = sid.ToString();
            }

            // --- 2) Genes + thickness ---
            cockThickness = thick;
            string[] gens = genesCsv.Split(',');
            for (int i = 0; i < geneFieldDefs.Length && i < gens.Length; i++)
            {
                float v;
                if (float.TryParse(gens[i], out v))
                {
                    geneToSet[i] = Mathf.Clamp(v, geneFieldDefs[i].Min, geneFieldDefs[i].Max);
                    geneToSetText[i] = geneToSet[i].ToString("0.##");
                }
            }
            // keep thickness gene in sync if present
            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                if (geneFieldDefs[i].FieldNames != null &&
                    geneFieldDefs[i].FieldNames.Length > 0 &&
                    geneFieldDefs[i].FieldNames[0] == "dickThickness")
                {
                    geneToSet[i] = thick;
                    geneToSetText[i] = thick.ToString("0.##");
                }
            }
            ApplyGenesToKobold();
            yield return new WaitForSecondsRealtime(0.25f);

            // --- 3) Dick ---
            if (dickOptions.Count == 0)
                RefreshDickOptions();

            if (!string.IsNullOrEmpty(dickEntry))
            {
                int found = -1;
                for (int i = 0; i < dickOptions.Count; i++)
                {
                    if (dickOptions[i] == dickEntry ||
                        (dickOptions[i] != null && dickOptions[i].IndexOf(dickEntry, StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        found = i;
                        break;
                    }
                }
                if (found >= 0) dickIdx = found;
            }
            if (dickOptions.Count > 0)
            {
                selectedDickIndex = Mathf.Clamp(dickIdx, 0, dickOptions.Count - 1);
                TryEquipSelectedDick();
                yield return new WaitForSecondsRealtime(0.2f);
            }

            // --- 4) Clothing ---
            if (!string.IsNullOrEmpty(equipsCsv))
            {
                if (equipCatalog.Count == 0)
                    RefreshEquipmentCatalog();

                string[] wanted = equipsCsv.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                int wornOk = 0;
                for (int i = 0; i < wanted.Length; i++)
                {
                    string name = wanted[i].Trim();
                    if (string.IsNullOrEmpty(name)) continue;
                    if (TryWearEquipmentByName(name))
                        wornOk++;
                    yield return new WaitForSecondsRealtime(0.05f);
                }
                RefreshEquipmentList();
                geneStatus = "Applied FULL preset: " + key +
                             " — char + genes + dick + " + wornOk + "/" + wanted.Length + " clothes";
            }
            else
            {
                geneStatus = "Applied FULL preset: " + key + " — char + genes + dick (no clothes stored)";
            }
            geneStatusUntil = Time.unscaledTime + 6f;
            applyFullPresetCoroutine = null;
        }

        private bool TryWearEquipmentByName(string equipName)
        {
            if (string.IsNullOrEmpty(equipName)) return false;

            // Resolve to catalog name when possible (exact / contains)
            string resolved = equipName;
            for (int i = 0; i < equipCatalog.Count; i++)
            {
                if (string.Equals(equipCatalog[i], equipName, StringComparison.OrdinalIgnoreCase))
                {
                    resolved = equipCatalog[i];
                    break;
                }
            }
            if (resolved == equipName)
            {
                for (int i = 0; i < equipCatalog.Count; i++)
                {
                    if (equipCatalog[i] != null &&
                        equipCatalog[i].IndexOf(equipName, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        resolved = equipCatalog[i];
                        break;
                    }
                }
            }

            return TryWearEquipmentCore(resolved);
        }

        private void DeleteSelectedPreset()
        {
            if (selectedFullPreset >= 0 && selectedFullPreset < fullPresetNames.Count)
            {
                string key = fullPresetNames[selectedFullPreset];
                fullPresetNames.RemoveAt(selectedFullPreset);
                fullPresetData.Remove(key);
                selectedFullPreset = -1;
                SaveGenePresetsToConfig();
                geneStatus = "Deleted full preset: " + key;
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }
            if (selectedStatsPreset >= 0 && selectedStatsPreset < statsPresetNames.Count)
            {
                string key = statsPresetNames[selectedStatsPreset];
                statsPresetNames.RemoveAt(selectedStatsPreset);
                statsPresetData.Remove(key);
                selectedStatsPreset = -1;
                SaveGenePresetsToConfig();
                geneStatus = "Deleted stats preset: " + key;
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }
            if (selectedEquipPreset >= 0 && selectedEquipPreset < equipPresetNames.Count)
            {
                string key = equipPresetNames[selectedEquipPreset];
                equipPresetNames.RemoveAt(selectedEquipPreset);
                equipPresetData.Remove(key);
                selectedEquipPreset = -1;
                SaveGenePresetsToConfig();
                geneStatus = "Deleted equip preset: " + key;
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }
            geneStatus = "Select a preset to delete";
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        private void SaveCurrentStatsPreset()
        {
            if (string.IsNullOrEmpty(newPresetName))
            {
                geneStatus = "Enter a preset name first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            // payload: v0|species|thick|gene0,gene1,...
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("v0|").Append(speciesId).Append('|').Append(cockThickness.ToString("0.###")).Append('|');
            for (int i = 0; i < geneFieldDefs.Length; i++)
            {
                if (i > 0) sb.Append(',');
                float v = geneToSet[i];
                if (geneToSetText[i] != null)
                {
                    float t;
                    if (float.TryParse(geneToSetText[i], out t))
                        v = t;
                }
                sb.Append(v.ToString("0.###"));
            }

            string key = newPresetName.Trim();
            statsPresetData[key] = sb.ToString();
            if (!statsPresetNames.Contains(key))
                statsPresetNames.Add(key);
            selectedStatsPreset = statsPresetNames.IndexOf(key);
            SaveGenePresetsToConfig();
            geneStatus = "Saved stats preset: " + key;
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        private void LoadSelectedStatsPreset()
        {
            if (selectedStatsPreset < 0 || selectedStatsPreset >= statsPresetNames.Count)
            {
                geneStatus = "Select a [S] preset first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            string key = statsPresetNames[selectedStatsPreset];
            string payload;
            if (!statsPresetData.TryGetValue(key, out payload) || string.IsNullOrEmpty(payload))
            {
                geneStatus = "Preset data missing";
                return;
            }

            try
            {
                string[] parts = payload.Split('|');
                if (parts.Length < 4)
                {
                    geneStatus = "Bad preset format";
                    return;
                }
                int sid;
                if (int.TryParse(parts[1], out sid))
                {
                    speciesId = sid;
                    speciesEditText = sid.ToString();
                }
                float thick;
                if (float.TryParse(parts[2], out thick))
                    cockThickness = thick;

                string[] gens = parts[3].Split(',');
                for (int i = 0; i < geneFieldDefs.Length && i < gens.Length; i++)
                {
                    float v;
                    if (float.TryParse(gens[i], out v))
                    {
                        geneToSet[i] = v;
                        geneToSetText[i] = v.ToString("0.##");
                    }
                }
                geneStatus = "Loaded stats preset: " + key + " (press APPLY)";
                geneStatusUntil = Time.unscaledTime + 4f;
            }
            catch (Exception ex)
            {
                geneStatus = "Load preset failed: " + ex.Message;
            }
        }

        private void SaveCurrentEquipPreset()
        {
            if (string.IsNullOrEmpty(newPresetName))
            {
                geneStatus = "Enter a preset name first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            RefreshEquipmentList();
            string key = newPresetName.Trim();
            string payload = selectedDickIndex + "|" + cockThickness.ToString("0.###") + "|" +
                             string.Join(",", equipNames.ToArray());
            equipPresetData[key] = payload;
            if (!equipPresetNames.Contains(key))
                equipPresetNames.Add(key);
            selectedEquipPreset = equipPresetNames.IndexOf(key);
            SaveGenePresetsToConfig();
            geneStatus = "Saved equip preset: " + key;
            geneStatusUntil = Time.unscaledTime + 3f;
        }

        private void LoadSelectedEquipPreset()
        {
            if (selectedEquipPreset < 0 || selectedEquipPreset >= equipPresetNames.Count)
            {
                geneStatus = "Select an [E] preset first";
                geneStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            string key = equipPresetNames[selectedEquipPreset];
            string payload;
            if (!equipPresetData.TryGetValue(key, out payload))
            {
                geneStatus = "Equip preset missing";
                return;
            }

            try
            {
                string[] parts = payload.Split('|');
                int di;
                if (parts.Length > 0 && int.TryParse(parts[0], out di))
                    selectedDickIndex = Mathf.Clamp(di, 0, Mathf.Max(0, dickOptions.Count - 1));
                float thick;
                if (parts.Length > 1 && float.TryParse(parts[1], out thick))
                    cockThickness = thick;
                TryEquipSelectedDick();
                geneStatus = "Loaded equip preset: " + key;
                geneStatusUntil = Time.unscaledTime + 3f;
            }
            catch (Exception ex)
            {
                geneStatus = "Load equip failed: " + ex.Message;
            }
        }

        private void LoadGenePresetsFromConfig()
        {
            statsPresetNames.Clear();
            statsPresetData.Clear();
            equipPresetNames.Clear();
            equipPresetData.Clear();
            fullPresetNames.Clear();
            fullPresetData.Clear();

            ParsePresetConfig(configStatsPresets != null ? configStatsPresets.Value : "", statsPresetNames, statsPresetData);
            ParsePresetConfig(configEquipPresets != null ? configEquipPresets.Value : "", equipPresetNames, equipPresetData);
            ParsePresetConfig(configFullPresets != null ? configFullPresets.Value : "", fullPresetNames, fullPresetData);
        }

        private void ParsePresetConfig(string raw, List<string> names, Dictionary<string, string> data)
        {
            if (string.IsNullOrEmpty(raw))
                return;
            string[] entries = raw.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < entries.Length; i++)
            {
                int eq = entries[i].IndexOf('=');
                if (eq <= 0) continue;
                string name = entries[i].Substring(0, eq).Trim();
                string payload = entries[i].Substring(eq + 1);
                if (name.Length == 0) continue;
                data[name] = payload;
                if (!names.Contains(name))
                    names.Add(name);
            }
        }

        private void SaveGenePresetsToConfig()
        {
            if (configStatsPresets != null)
                configStatsPresets.Value = JoinPresetConfig(statsPresetNames, statsPresetData);
            if (configEquipPresets != null)
                configEquipPresets.Value = JoinPresetConfig(equipPresetNames, equipPresetData);
            if (configFullPresets != null)
                configFullPresets.Value = JoinPresetConfig(fullPresetNames, fullPresetData);
        }

        // ============================================================
        // CHARCON IMPORT / EXPORT / CLONE NEARBY
        // ============================================================

        /// <summary>
        /// BepInEx config filename for the old Character Control cheat.
        /// Looked up next to this plugin's config on startup and on manual import.
        /// </summary>
        private const string CharConConfigFileName = "Komar.koboldkare.CharConCheat.cfg";

        /// <summary>
        /// On launch: if CharCon cfg exists, merge any presets we don't already have.
        /// Safe for new users — their CharCon outfits show up in this menu automatically.
        /// </summary>
        private void TryAutoImportCharConConfig()
        {
            try
            {
                string path = FindCharConConfigPath();
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Logger.LogInfo("CharCon auto-import: no " + CharConConfigFileName + " found (ok if CharCon isn't installed)");
                    return;
                }

                string equipments, stats;
                if (!TryReadCharConPresetStrings(path, out equipments, out stats))
                {
                    Logger.LogWarning("CharCon auto-import: found cfg but Equipments/Stats were empty");
                    return;
                }

                int before = fullPresetNames.Count + statsPresetNames.Count + equipPresetNames.Count;
                int n = ImportCharConPresets(equipments, stats, onlyAddMissing: true);
                int after = fullPresetNames.Count + statsPresetNames.Count + equipPresetNames.Count;
                Logger.LogInfo("CharCon auto-import from " + path + ": +" + n + " new (total presets " + before + " → " + after + ")");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("CharCon auto-import failed: " + ex.Message);
            }
        }

        private string FindCharConConfigPath()
        {
            // 1) Same folder as this plugin's config (normal BepInEx/config/)
            try
            {
                string dir = Path.GetDirectoryName(Config.ConfigFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    string p = Path.Combine(dir, CharConConfigFileName);
                    if (File.Exists(p)) return p;
                }
            }
            catch { }

            // 2) Walk up from plugin path looking for BepInEx/config/
            try
            {
                string start = Path.GetDirectoryName(Info.Location);
                for (int i = 0; i < 6 && !string.IsNullOrEmpty(start); i++)
                {
                    string candidate = Path.Combine(start, "config", CharConConfigFileName);
                    if (File.Exists(candidate)) return candidate;
                    candidate = Path.Combine(start, "BepInEx", "config", CharConConfigFileName);
                    if (File.Exists(candidate)) return candidate;
                    start = Path.GetDirectoryName(start);
                }
            }
            catch { }

            return null;
        }

        private bool TryReadCharConPresetStrings(string path, out string equipments, out string stats)
        {
            equipments = null;
            stats = null;
            if (string.IsNullOrEmpty(path) || !File.Exists(path))
                return false;

            string[] lines = File.ReadAllLines(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (string.IsNullOrEmpty(line) || line[0] == '#' || line[0] == '[')
                    continue;

                int eq = line.IndexOf('=');
                if (eq <= 0) continue;
                string key = line.Substring(0, eq).Trim();
                string val = line.Substring(eq + 1).Trim();
                if (key.Equals("Equipments", StringComparison.OrdinalIgnoreCase))
                    equipments = val;
                else if (key.Equals("Stats", StringComparison.OrdinalIgnoreCase))
                    stats = val;
            }

            return !string.IsNullOrEmpty(equipments) || !string.IsNullOrEmpty(stats);
        }

        /// <summary>
        /// Manual button: re-read CharCon cfg from disk (overwrites same names),
        /// or fall back to the built-in sample if the file is missing.
        /// </summary>
        private void ImportDefaultCharConPresets()
        {
            string path = FindCharConConfigPath();
            string equipments = null;
            string stats = null;

            if (!string.IsNullOrEmpty(path) && TryReadCharConPresetStrings(path, out equipments, out stats))
            {
                int n = ImportCharConPresets(equipments, stats, onlyAddMissing: false);
                geneStatus = "Imported " + n + " from CharCon cfg";
                geneStatusUntil = Time.unscaledTime + 5f;
                Logger.LogInfo("CharCon manual import from " + path + ": " + n);
                return;
            }

            // Fallback sample (your shared presets) if cfg isn't on disk
            equipments =
                "Flint FlintHair FlintBelt FlintBoots FlintEarringL FlintFishnetTop FlintPants FlintTop " +
                "$Zex_Kobo TopHat Tailbag SpikeyBracelets SpikeyBracelets SpikedCollar SpikedCollar NippleBarbells HighHeels Tailbag Tailbag " +
                "$Flint_Stipper FlintHair FlintBelt FlintBoots FlintEarringL FlintFishnetTop FlintFishnetLegL FlintFishnetLegR FlintThong FlintCollar FlintBarbells " +
                "$Krox_Stipper KroxBellyPiercing KroxBellySize25 KroxBreastShape25 KroxButt120 KroxFishnetArmStockings KroxFishnetLegStockings KroxNippleBarbellsGold KroxNosePiercing KroxSequinBraLift KroxSequinPantiesNSFW KroxVagPiercing " +
                "$Gemma GTop GJacket GEdgyThighs GEdgyPanties GEdgyBracelets MamaGGlasses SpikedCollar TagPuppyslut";

            stats =
                "Zex_Kobo 1 10 70 70 1 26 37 0 5 0.5 7.5 None 255 110 0 0 " +
                "Flint 1 5 20 20 1 24 30 0 10 0.7001953 10 None 0 108 0 255 " +
                "Flint_Stripper 1 10 70 70 1 26 20 0 5 0.7001953 7.5 None 255 110 0 0 " +
                "Krox_Stipper 1 5 20 20 1 24 30 0 10 0.7001953 10 None 0 108 0 255 " +
                "Gemma 1 10 70 70 1 26 15 0 5 0.7 7.5 None 175 100 120 0";

            int n2 = ImportCharConPresets(equipments, stats, onlyAddMissing: false);
            geneStatus = "No CharCon cfg found — imported " + n2 + " built-in sample(s)";
            geneStatusUntil = Time.unscaledTime + 5f;
        }

        /// <summary>
        /// Parse KKCharCon Equipments + Stats strings into this mod's presets.
        /// Equipments: "$Name item item $Name2 item..."
        /// Stats: "Name Energy MaxEn Belly Meta Grab Size Boobs Fat DickSize Thick Balls Dick Hue Bright Sat ClothHue" repeated.
        /// </summary>
        private int ImportCharConPresets(string equipmentsRaw, string statsRaw, bool onlyAddMissing = false)
        {
            Dictionary<string, List<string>> equipMap = ParseCharConEquipments(equipmentsRaw);
            Dictionary<string, CharConStats> statsMap = ParseCharConStats(statsRaw);

            int added = 0;
            HashSet<string> used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Merge matching names into full presets
            foreach (var kv in statsMap)
            {
                string name = kv.Key;
                CharConStats st = kv.Value;

                if (onlyAddMissing && fullPresetData.ContainsKey(name))
                {
                    used.Add(name);
                    // still mark matching equip block as used so it isn't dual-added
                    if (equipMap.ContainsKey(name)) used.Add(name);
                    continue;
                }

                List<string> clothes;
                equipMap.TryGetValue(name, out clothes);
                // Also try loose match (Flint_Stripper vs Flint_Stipper typo)
                if (clothes == null)
                {
                    foreach (var ek in equipMap.Keys)
                    {
                        if (string.Equals(ek, name, StringComparison.OrdinalIgnoreCase) ||
                            string.Equals(ek.Replace("Stipper", "Stripper"), name.Replace("Stipper", "Stripper"), StringComparison.OrdinalIgnoreCase))
                        {
                            clothes = equipMap[ek];
                            used.Add(ek);
                            break;
                        }
                    }
                }
                else
                {
                    used.Add(name);
                }

                string payload = BuildFullPresetPayloadFromCharCon(name, st, clothes);
                fullPresetData[name] = payload;
                if (!fullPresetNames.Contains(name))
                    fullPresetNames.Add(name);
                used.Add(name);
                added++;
            }

            // Equip-only leftovers (same shape as SaveCurrentEquipPreset: dickIdx|thick|clothes)
            foreach (var kv in equipMap)
            {
                if (used.Contains(kv.Key)) continue;
                string key = kv.Key;
                if (onlyAddMissing && (equipPresetData.ContainsKey(key) || fullPresetData.ContainsKey(key)))
                    continue;

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("-1|0.5|");
                List<string> items = kv.Value;
                for (int i = 0; i < items.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(SanitizePresetToken(items[i]));
                }
                equipPresetData[key] = sb.ToString();
                if (!equipPresetNames.Contains(key))
                    equipPresetNames.Add(key);
                added++;
            }

            if (added > 0)
                SaveGenePresetsToConfig();
            Logger.LogInfo("CharCon import: " + added + " presets (" + fullPresetNames.Count + " full, " +
                           statsPresetNames.Count + " stats, " + equipPresetNames.Count + " equip)" +
                           (onlyAddMissing ? " [missing-only]" : " [overwrite]"));
            return added;
        }

        private struct CharConStats
        {
            public float maxEnergy, belly, meta, grab, size, tits, fat, dickSize, thick, balls;
            public float hue, bright, satur, clothHue;
            public string dickName;
        }

        private static float ParseCharConFloat(string s)
        {
            if (string.IsNullOrEmpty(s)) return 0f;
            s = s.Trim().Replace(',', '.');
            float v;
            if (float.TryParse(s, System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture, out v))
                return v;
            if (float.TryParse(s, out v))
                return v;
            return 0f;
        }

        private Dictionary<string, List<string>> ParseCharConEquipments(string raw)
        {
            var map = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(raw)) return map;

            string[] blocks = raw.Split(new[] { '$' }, StringSplitOptions.RemoveEmptyEntries);
            for (int b = 0; b < blocks.Length; b++)
            {
                string[] tokens = blocks[b].Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                if (tokens.Length == 0) continue;
                string name = tokens[0].Trim();
                var items = new List<string>();
                for (int i = 1; i < tokens.Length; i++)
                    items.Add(tokens[i].Trim());
                map[name] = items;
            }
            return map;
        }

        private Dictionary<string, CharConStats> ParseCharConStats(string raw)
        {
            var map = new Dictionary<string, CharConStats>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrEmpty(raw)) return map;

            string[] tokens = raw.Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
            // Each preset: Name + 16 fields
            const int stride = 17;
            for (int i = 0; i + stride - 1 < tokens.Length; i += stride)
            {
                string name = tokens[i];
                // indices after name:
                // 1 Energy (unused), 2 MaxEnergy, 3 Belly, 4 Meta, 5 Grab, 6 Size, 7 Boobs, 8 Fat,
                // 9 DickSize, 10 Thick, 11 Balls, 12 Dick, 13 Hue, 14 Bright, 15 Sat, 16 ClothHue
                CharConStats st = new CharConStats();
                st.maxEnergy = ParseCharConFloat(tokens[i + 2]);
                st.belly = ParseCharConFloat(tokens[i + 3]);
                st.meta = ParseCharConFloat(tokens[i + 4]);
                st.grab = ParseCharConFloat(tokens[i + 5]);
                st.size = ParseCharConFloat(tokens[i + 6]);
                st.tits = ParseCharConFloat(tokens[i + 7]);
                st.fat = ParseCharConFloat(tokens[i + 8]);
                st.dickSize = ParseCharConFloat(tokens[i + 9]);
                st.thick = ParseCharConFloat(tokens[i + 10]);
                st.balls = ParseCharConFloat(tokens[i + 11]);
                st.dickName = tokens[i + 12];
                if (string.Equals(st.dickName, "None", StringComparison.OrdinalIgnoreCase))
                    st.dickName = "";
                st.hue = ParseCharConFloat(tokens[i + 13]);
                st.bright = ParseCharConFloat(tokens[i + 14]);
                st.satur = ParseCharConFloat(tokens[i + 15]);
                st.clothHue = ParseCharConFloat(tokens[i + 16]);
                map[name] = st;
            }
            return map;
        }

        /// <summary>
        /// CharCon preset names are often outfits (Flint_Stripper, Zex_Kobo).
        /// Pull a likely character/mesh name for the full-preset char field.
        /// </summary>
        private static string GuessCharacterNameFromCharConPreset(string presetName)
        {
            if (string.IsNullOrEmpty(presetName))
                return "";

            // Known outfit → character aliases (expand as needed)
            string lower = presetName.ToLowerInvariant();
            if (lower.StartsWith("flint")) return "Flint";
            if (lower.StartsWith("gemma")) return "Gemma";
            if (lower.StartsWith("krox")) return "Krox";
            if (lower.StartsWith("zex") || lower.Contains("kobo")) return "Kobold"; // fallback base form

            // Flint_Stripper / Krox_Stipper → take token before '_'
            int us = presetName.IndexOf('_');
            if (us > 0)
                return presetName.Substring(0, us);

            return presetName;
        }

        private string BuildFullPresetPayloadFromCharCon(string name, CharConStats st, List<string> clothes)
        {
            // geneFieldDefs order: MaxEn,Belly,Meta,Grab,Size,Tits,Fat,Psize,Balls,Hue,Bright,Satur,ClthHue,Thick
            float[] genes = new float[geneFieldDefs.Length];
            for (int i = 0; i < genes.Length; i++)
                genes[i] = geneFieldDefs[i].DefaultValue;

            void SetGene(string label, float value)
            {
                for (int i = 0; i < geneFieldDefs.Length; i++)
                {
                    if (geneFieldDefs[i].Label == label)
                    {
                        genes[i] = value;
                        return;
                    }
                }
            }

            SetGene("MaxEn", st.maxEnergy);
            SetGene("Belly", st.belly);
            SetGene("Meta", st.meta);
            SetGene("Grab", st.grab);
            SetGene("Size", st.size);
            SetGene("Tits", st.tits);
            SetGene("Fat", st.fat);
            SetGene("Psize", st.dickSize);
            SetGene("Balls", st.balls);
            SetGene("Hue", st.hue);
            SetGene("Bright", st.bright);
            SetGene("Satur", st.satur);
            SetGene("Clth Hue", st.clothHue);
            SetGene("Thick", st.thick);

            // Prefer a real character name over the outfit title so APPLY can SET CHAR
            string charName = GuessCharacterNameFromCharConPreset(name);
            // If the player DB is already loaded, lock onto an exact option when possible
            charName = ResolveCharacterOptionName(charName, name);

            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append("v1|");
            sb.Append(SanitizePresetToken(charName)).Append('|');
            // species index if we can resolve it from the character list
            int sid = ResolveCharacterOptionIndex(charName);
            if (sid < 0) sid = 0;
            sb.Append(sid).Append('|');
            sb.Append(st.thick.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
            for (int i = 0; i < genes.Length; i++)
            {
                if (i > 0) sb.Append(',');
                sb.Append(genes[i].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
            }
            sb.Append('|');
            sb.Append("-1|"); // dick index unknown
            sb.Append(SanitizePresetToken(st.dickName ?? "")).Append('|');
            if (clothes != null)
            {
                for (int i = 0; i < clothes.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(SanitizePresetToken(clothes[i]));
                }
            }
            return sb.ToString();
        }

        private string ResolveCharacterOptionName(string guessed, string presetName)
        {
            if (characterOptions == null || characterOptions.Count == 0)
            {
                try { RefreshCharacterList(); } catch { }
            }
            if (characterOptions == null || characterOptions.Count == 0)
                return guessed;

            // Try candidates in order
            string[] candidates = {
                guessed,
                presetName,
                GuessCharacterNameFromCharConPreset(presetName)
            };

            for (int c = 0; c < candidates.Length; c++)
            {
                string cand = candidates[c];
                if (string.IsNullOrEmpty(cand)) continue;

                for (int i = 0; i < characterOptions.Count; i++)
                {
                    if (string.Equals(characterOptions[i], cand, StringComparison.OrdinalIgnoreCase))
                        return characterOptions[i];
                }
                string lower = cand.ToLowerInvariant();
                for (int i = 0; i < characterOptions.Count; i++)
                {
                    if (characterOptions[i] == null) continue;
                    string opt = characterOptions[i];
                    string ol = opt.ToLowerInvariant();
                    if (ol.Contains(lower) || lower.Contains(ol) || ol.StartsWith(lower) || lower.StartsWith(ol))
                        return opt;
                }
            }
            return guessed;
        }

        private int ResolveCharacterOptionIndex(string charName)
        {
            if (string.IsNullOrEmpty(charName) || characterOptions == null)
                return -1;
            for (int i = 0; i < characterOptions.Count; i++)
            {
                if (string.Equals(characterOptions[i], charName, StringComparison.OrdinalIgnoreCase))
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Match a full-preset character field (or outfit title) to an entry in characterOptions.
        /// </summary>
        private int FindCharacterIndexForPreset(string charName, string presetKey, int speciesHint)
        {
            if (characterOptions == null || characterOptions.Count == 0)
            {
                try { RefreshCharacterList(); } catch { }
            }
            if (characterOptions == null || characterOptions.Count == 0)
                return -1;

            List<string> tries = new List<string>();
            if (!string.IsNullOrEmpty(charName)) tries.Add(charName);
            if (!string.IsNullOrEmpty(presetKey)) tries.Add(presetKey);
            string guessed = GuessCharacterNameFromCharConPreset(
                !string.IsNullOrEmpty(charName) ? charName : presetKey);
            if (!string.IsNullOrEmpty(guessed)) tries.Add(guessed);
            // also base before underscore of each
            int n = tries.Count;
            for (int i = 0; i < n; i++)
            {
                int us = tries[i].IndexOf('_');
                if (us > 0)
                    tries.Add(tries[i].Substring(0, us));
            }

            // 1) Exact
            for (int t = 0; t < tries.Count; t++)
            {
                for (int i = 0; i < characterOptions.Count; i++)
                {
                    if (string.Equals(characterOptions[i], tries[t], StringComparison.OrdinalIgnoreCase))
                        return i;
                }
            }
            // 2) Starts with / contains
            for (int t = 0; t < tries.Count; t++)
            {
                string lower = tries[t].ToLowerInvariant();
                if (lower.Length < 2) continue;
                for (int i = 0; i < characterOptions.Count; i++)
                {
                    if (characterOptions[i] == null) continue;
                    string ol = characterOptions[i].ToLowerInvariant();
                    if (ol.StartsWith(lower) || lower.StartsWith(ol) || ol.Contains(lower))
                        return i;
                }
            }
            // 3) Species index hint (only if it looks like a real index, not the old "always 0")
            if (speciesHint > 0 && speciesHint < characterOptions.Count)
                return speciesHint;

            return -1;
        }

        private void ExportAllPresetsToLog()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== ZEX PRESET EXPORT ===");
            sb.AppendLine("FullPresets=" + (configFullPresets != null ? configFullPresets.Value : JoinPresetConfig(fullPresetNames, fullPresetData)));
            sb.AppendLine("StatsPresets=" + (configStatsPresets != null ? configStatsPresets.Value : JoinPresetConfig(statsPresetNames, statsPresetData)));
            sb.AppendLine("EquipPresets=" + (configEquipPresets != null ? configEquipPresets.Value : JoinPresetConfig(equipPresetNames, equipPresetData)));
            sb.AppendLine("=== END EXPORT ===");
            string text = sb.ToString();
            Logger.LogInfo(text);
            try
            {
                // Also write next to the plugin config for easy sharing
                string dir = Path.GetDirectoryName(Config.ConfigFilePath);
                if (!string.IsNullOrEmpty(dir))
                {
                    string path = Path.Combine(dir, "ZexPresets_export.txt");
                    File.WriteAllText(path, text);
                    geneStatus = "Exported to log + " + path;
                }
                else
                    geneStatus = "Exported to BepInEx log";
            }
            catch (Exception ex)
            {
                geneStatus = "Exported to log (file failed: " + ex.Message + ")";
            }
            geneStatusUntil = Time.unscaledTime + 6f;
        }

        private void CloneNearbyPlayerToPreset()
        {
            try
            {
                Component self = FindLocalKobold();
                Vector3 origin = self != null ? self.transform.position : Vector3.zero;
                if (self == null)
                {
                    Camera cam = Camera.main;
                    if (cam != null) origin = cam.transform.position;
                }

                Component best = null;
                float bestDist = float.MaxValue;
                string bestName = "Nearby";

                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views != null)
                {
                    for (int i = 0; i < views.Length; i++)
                    {
                        PhotonView view = views[i];
                        if (view == null || view.IsMine) continue;
                        Component k = GetKoboldOn(view.gameObject);
                        if (k == null) continue;
                        float d = Vector3.Distance(origin, k.transform.position);
                        if (d < bestDist)
                        {
                            bestDist = d;
                            best = k;
                            Player owner = view.Owner;
                            if (owner != null && !string.IsNullOrEmpty(owner.NickName))
                                bestName = owner.NickName;
                        }
                    }
                }

                if (best == null)
                {
                    geneStatus = "No nearby player kobold found";
                    geneStatusUntil = Time.unscaledTime + 3f;
                    return;
                }

                ResolveGeneTypes();
                object genes = getGenesMethod != null ? getGenesMethod.Invoke(best, null) : null;
                if (genes == null)
                {
                    geneStatus = "Nearby kobold has no genes";
                    geneStatusUntil = Time.unscaledTime + 3f;
                    return;
                }

                // Pull gene values into geneToSet
                for (int i = 0; i < geneFieldDefs.Length; i++)
                {
                    float v = ReadGeneValue(genes, geneFieldDefs[i]);
                    geneToSet[i] = v;
                    geneToSetText[i] = v.ToString("0.##");
                }

                int sid = 0;
                FieldInfo sp = AccessTools.Field(genes.GetType(), "species");
                if (sp != null && sp.GetValue(genes) != null)
                    sid = Convert.ToInt32(sp.GetValue(genes));
                speciesId = sid;
                speciesEditText = sid.ToString();

                // Equipment from their inventory if present
                List<string> clothes = new List<string>();
                try
                {
                    Component inv = null;
                    Type invType = SafeGameType("KoboldInventory");
                    if (invType != null)
                        inv = best.GetComponent(invType) ?? best.GetComponentInChildren(invType, true);
                    if (inv != null)
                    {
                        MethodInfo getAll = AccessTools.Method(inv.GetType(), "GetAllEquipment");
                        if (getAll != null)
                        {
                            object result = getAll.Invoke(inv, null);
                            IEnumerable list = result as IEnumerable;
                            if (list != null)
                            {
                                foreach (object item in list)
                                {
                                    if (item == null) continue;
                                    string name = null;
                                    try
                                    {
                                        PropertyInfo np = item.GetType().GetProperty("name");
                                        if (np != null)
                                        {
                                            object v = np.GetValue(item, null);
                                            if (v != null) name = v.ToString();
                                        }
                                    }
                                    catch { }
                                    if (string.IsNullOrEmpty(name)) name = item.ToString();
                                    if (!string.IsNullOrEmpty(name)) clothes.Add(name);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Clone clothes read: " + ex.Message);
                }

                float thick = cockThickness;
                for (int i = 0; i < geneFieldDefs.Length; i++)
                {
                    if (geneFieldDefs[i].Label == "Thick")
                    {
                        thick = geneToSet[i];
                        break;
                    }
                }

                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.Append("v1|");
                sb.Append(SanitizePresetToken(bestName)).Append('|');
                sb.Append(sid).Append('|');
                sb.Append(thick.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)).Append('|');
                for (int i = 0; i < geneFieldDefs.Length; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(geneToSet[i].ToString("0.###", System.Globalization.CultureInfo.InvariantCulture));
                }
                sb.Append("|-1||");
                for (int i = 0; i < clothes.Count; i++)
                {
                    if (i > 0) sb.Append(',');
                    sb.Append(SanitizePresetToken(clothes[i]));
                }

                string key = "Clone_" + SanitizePresetToken(bestName);
                if (string.IsNullOrEmpty(key) || key == "Clone_")
                    key = "Clone_Near";
                // unique if exists
                string baseKey = key;
                int n = 2;
                while (fullPresetData.ContainsKey(key))
                {
                    key = baseKey + "_" + n;
                    n++;
                }

                fullPresetData[key] = sb.ToString();
                if (!fullPresetNames.Contains(key))
                    fullPresetNames.Add(key);
                selectedFullPreset = fullPresetNames.IndexOf(key);
                selectedStatsPreset = -1;
                selectedEquipPreset = -1;
                SaveGenePresetsToConfig();

                geneStatus = "Cloned " + bestName + " (" + bestDist.ToString("0.0") + "m) → preset " + key +
                             " · " + clothes.Count + " clothes. APPLY to use.";
                geneStatusUntil = Time.unscaledTime + 6f;
                Logger.LogInfo("Clone nearby: " + key + " from " + bestName + " dist=" + bestDist);
            }
            catch (Exception ex)
            {
                geneStatus = "Clone failed: " + ex.Message;
                geneStatusUntil = Time.unscaledTime + 4f;
                Logger.LogWarning("CloneNearbyPlayerToPreset: " + ex);
            }
        }

        private string JoinPresetConfig(List<string> names, Dictionary<string, string> data)
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < names.Count; i++)
            {
                string n = names[i];
                string p;
                if (!data.TryGetValue(n, out p)) continue;
                if (sb.Length > 0) sb.Append(';');
                sb.Append(n).Append('=').Append(p.Replace(";", ","));
            }
            return sb.ToString();
        }

        private void DrawSillysPanel(float x, float y, float width)
        {
            float colGap = 14f;
            float colW = (width - colGap) * 0.5f;

            GUI.Box(
                new Rect(x, y, colW, 190f),
                new GUIContent("Credits / Inspo.\n" +
                "Huge Thank You to Uwo for Introducing Me to this Lovely game\n" +
                "Credits to Komar for Inspo the Char Editor Section on This QoL\n\n\n\n\n" +
                "If You Find a Bug Let Me Know or if You Have Suggestions Ping/Dm me\n" +
                " Disc : AnUnknownFurry\n\n\n\n" +
                "Sorry to the People Ive Crashed and the Servers Ive Trolled\n" +
                "I Do Hope One Day I Can Make It up to Everyone ˚ʚ♡ɞ˚"),
                cardStyle);
            }
        

        // ============================================================
        // ESP UI
        // ============================================================
        private void DrawESPPanel(float x, float y, float width)
        {
            float colW = (width - 10f) / 2f;
            DrawToggle(x, y, showNames, "PLAYER NAMES", v => showNames = v);
            DrawToggle(x, y + 32f, showDistance, "DISTANCE", v => showDistance = v);
            DrawToggle(x, y + 64f, showActorID, "ACTOR ID", v => showActorID = v);
            DrawToggle(x, y + 96f, hideSelf, "HIDE SELF", v => hideSelf = v);
            DrawToggle(x, y + 128f, tracersEnabled, "TRACERS", v => tracersEnabled = v);
            DrawToggle(x, y + 160f, visibilityCheck, "VISIBILITY CHECK", v => visibilityCheck = v);
            DrawToggle(x, y + 192f, offscreenArrows, "OFFSCREEN ARROWS", v => offscreenArrows = v);

            float rx = x + colW + 10f;
            DrawSlider(rx, y, "MAX DISTANCE", maxDistance, 10f, 1000f, v => maxDistance = v, "F0");
            DrawSlider(rx, y + 72f, "TRACER THICKNESS", tracerThickness, 1f, 5f, v => tracerThickness = v, "F1");

            GUI.Label(new Rect(rx, y + 150f, colW, 20f), new GUIContent("TRACER ORIGIN"), labelStyle);
            string origin = tracerOrigin == 0 ? "BOTTOM CENTER" : tracerOrigin == 1 ? "CENTER" : "TOP CENTER";
            if (GUI.Button(new Rect(rx, y + 174f, colW, 30f), new GUIContent(origin), buttonStyle))
                tracerOrigin = (tracerOrigin + 1) % 3;

            DrawToggle(rx, y + 214f, tracerDistanceFade, "DISTANCE FADE", v => tracerDistanceFade = v);
            DrawToggle(rx, y + 246f, scaleNames, "DISTANCE NAME SCALING", v => scaleNames = v);

            DrawESPColorControls(x, y + 290f, width);

            GUI.Label(new Rect(x, y + 420f, width, 55f), new GUIContent(
                "ESP : " +
                "Selected players and friends use their own colors."
            ), smallStyle);
        }

        private delegate void BoolSetter(bool value);
        private delegate void FloatSetter(float value);

        private void DrawToggle(float x, float y, bool value, String text, BoolSetter setter)
        {
            if (GUI.Button(new Rect(x, y, 26f, 26f), new GUIContent(value ? "♥" : "♡"), buttonStyle))
                setter(!value);
            GUI.Label(new Rect(x + 34f, y, 280f, 26f), new GUIContent(text), labelStyle);
        }

        private void DrawSlider(float x, float y, String text, float value, float min, float max, FloatSetter setter, String format)
        {
            GUI.Label(new Rect(x, y, 250f, 20f), new GUIContent(text), labelStyle);
            float v = GUI.HorizontalSlider(new Rect(x, y + 24f, 250f, 18f), value, min, max,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            setter(v);
            GUI.Label(new Rect(x + 260f, y + 20f, 80f, 24f), new GUIContent(v.ToString(format)), labelStyle);
        }

        // ============================================================
        // SPAWNER UI / LOGIC
        // ============================================================
        private void DrawSpawnerPanel(float x, float y, float width)
        {

            GUI.Label(new Rect(x, y, width - 120f, 22f), new GUIContent("SELECTED: " + GetSelectedPrefabName()), labelStyle);
            if (GUI.Button(new Rect(x + width - 110f, y - 2f, 110f, 28f), new GUIContent("RESCAN"), buttonStyle))
                RefreshPrefabs();
            y += 34f;

            Rect searchRect = new Rect(x, y, width, 30f);
            GUI.Box(searchRect, new GUIContent(""), GUI.skin.box);
            string display = string.IsNullOrEmpty(searchText) ? "CLICK TO SEARCH..." : searchText;
            GUI.Label(new Rect(x + 8f, y + 3f, width - 16f, 24f), new GUIContent(display), labelStyle);
            if (Event.current.type == EventType.MouseDown && searchRect.Contains(Event.current.mousePosition))
            {
                searchFocused = true;
                Event.current.Use();
            }
            else if (Event.current.type == EventType.MouseDown && !searchRect.Contains(Event.current.mousePosition))
                searchFocused = false;
            y += 36f;

            GUI.Label(new Rect(x, y, width, 22f), new GUIContent(prefabStatus + " | SHOWING " + filteredPrefabList.Count), smallStyle);
            y += 25f;

            float listH = 265f;
            GUI.Box(new Rect(x, y, width, listH), "");
            DrawPrefabList(x + 6f, y + 6f, width - 12f, listH - 12f);
            y += listH + 15f;

            GUI.Label(new Rect(x, y, 70f, 22f), new GUIContent("Amount"), labelStyle);
            amount = Mathf.Clamp(Mathf.RoundToInt(GUI.HorizontalSlider(new Rect(x + 70f, y + 4f, width - 130f, 18f), amount, 1f, 5f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb)), 1, 20);
            GUI.Label(new Rect(x + width - 50f, y, 50f, 22f), new GUIContent(amount.ToString()), labelStyle);
            y += 34f;

            GUI.Label(new Rect(x, y, 80f, 22f), new GUIContent("DISTANCE"), labelStyle);
            spawnDistance = GUI.HorizontalSlider(new Rect(x + 80f, y + 4f, width - 140f, 18f), spawnDistance, 1f, 10f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            GUI.Label(new Rect(x + width - 55f, y, 55f, 22f), new GUIContent(spawnDistance.ToString("0.0")), labelStyle);
            y += 38f;

            float gap = 8f;
            float bw = (width - gap) / 2f;
            if (GUI.Button(new Rect(x, y, bw, 38f), new GUIContent("Spawn"), buttonStyle)) Spawn();
            if (GUI.Button(new Rect(x + bw + gap, y, bw, 38f), new GUIContent("CLEAR ALL"), buttonStyle)) ClearAll();
            y += 44f;
            GUI.Label(new Rect(x, y, width, 24f), new GUIContent("STATUS: " + spawnStatus), smallStyle);
        }

        private void DrawPrefabList(float x, float y, float width, float height)
        {
            const int visibleItems = 7;
            const float itemHeight = 32f;
            if (GUI.Button(new Rect(x, y, 70f, 26f), new GUIContent("UP"), buttonStyle)) MovePrefabList(-1);
            if (GUI.Button(new Rect(x + 76f, y, 70f, 26f), new GUIContent("DOWN"), buttonStyle)) MovePrefabList(1);
            GUI.Label(new Rect(x + 155f, y, width - 155f, 26f), new GUIContent(filteredPrefabList.Count + " MATCHES"), smallStyle);
            y += 31f;
            for (int i = 0; i < visibleItems; i++)
            {
                int index = prefabListOffset + i;
                if (index >= filteredPrefabList.Count) break;
                PrefabEntry entry = filteredPrefabList[index];
                bool isFavorite = favoritePrefabNames.Contains(entry.Name);

                if (GUI.Button(
                    new Rect(x, y + i * itemHeight, 34f, itemHeight - 3f),
                    new GUIContent(isFavorite ? "★" : "☆"),
                    buttonStyle))
                {
                    if (isFavorite) favoritePrefabNames.Remove(entry.Name);
                    else favoritePrefabNames.Add(entry.Name);
                    SaveFavoritePrefabNames();
                    ApplySearch();
                }

                GUIStyle style = index == selectedPrefabIndex ? selectedButtonStyle : buttonStyle;
                if (GUI.Button(new Rect(x + 40f, y + i * itemHeight, width - 40f, itemHeight - 3f), new GUIContent(entry.Name), style))
                {
                    selectedPrefabIndex = index;
                    spawnStatus = "SELECTED " + entry.Name;
                    searchFocused = false;
                }
            }
        }

        private bool FindPreparePool()
        {
            try
            {
                preparePoolType = typeof(PreparePool);
                preparePoolInstanceField = preparePoolType.GetField("instance", BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                dynamicPrefabsField = preparePoolType.GetField("dynamicPrefabs", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                return preparePoolInstanceField != null && dynamicPrefabsField != null;
            }
            catch { return false; }
        }

        private void RefreshPrefabs()
        {
            prefabList.Clear();
            filteredPrefabList.Clear();
            selectedPrefabIndex = -1;
            prefabListOffset = 0;
            prefabStatus = "SCANNING...";

            if (preparePoolInstanceField == null || dynamicPrefabsField == null)
                if (!FindPreparePool()) { prefabStatus = "PREPAREPOOL NOT FOUND"; return; }

            try
            {
                object pool = preparePoolInstanceField.GetValue(null);
                if (pool == null) { prefabStatus = "POOL NOT READY"; return; }
                object raw = dynamicPrefabsField.GetValue(pool);
                IDictionary dict = raw as IDictionary;
                if (dict == null) { prefabStatus = "INVALID PREFAB DICTIONARY"; return; }

                foreach (DictionaryEntry entry in dict)
                {
                    string name = entry.Key as string;
                    if (string.IsNullOrWhiteSpace(name)) continue;
                    GameObject prefab = GetPrefabFromDynamicEntry(entry.Value);
                    if (prefab != null) prefabList.Add(new PrefabEntry(name, prefab));
                }

                prefabList.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase));
                ApplySearch();
                prefabStatus = prefabList.Count > 0 ? "FOUND " + prefabList.Count + " PREFABS" : "NO PREFABS FOUND";
            }
            catch (Exception ex)
            {
                prefabStatus = "SCAN FAILED";
                Logger.LogError("Prefab scan failed: " + ex);
            }
        }

        private GameObject GetPrefabFromDynamicEntry(object dynamicEntry)
        {
            try
            {
                IEnumerable list = dynamicEntry as IEnumerable;
                if (list == null) return null;
                GameObject best = null;
                foreach (object pair in list)
                {
                    if (pair == null) continue;
                    FieldInfo objField = pair.GetType().GetField("obj", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    if (objField == null) continue;
                    GameObject obj = objField.GetValue(pair) as GameObject;
                    if (obj != null) best = obj;
                }
                return best;
            }
            catch { return null; }
        }

        private void ApplySearch()
        {
            filteredPrefabList.Clear();
            string query = searchText == null ? "" : searchText.Trim();
            foreach (PrefabEntry entry in prefabList)
                if (query.Length == 0 || entry.Name.IndexOf(query, StringComparison.InvariantCultureIgnoreCase) >= 0)
                    filteredPrefabList.Add(entry);

            filteredPrefabList.Sort((a, b) =>
            {
                bool aFav = favoritePrefabNames.Contains(a.Name);
                bool bFav = favoritePrefabNames.Contains(b.Name);
                if (aFav != bFav)
                    return aFav ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);
            });

            prefabListOffset = 0;
            selectedPrefabIndex = filteredPrefabList.Count == 0 ? -1 : 0;
        }

        private void MovePrefabList(int direction)
        {
            const int visibleItems = 7;
            int maxOffset = Mathf.Max(0, filteredPrefabList.Count - visibleItems);
            prefabListOffset = Mathf.Clamp(prefabListOffset + direction, 0, maxOffset);
        }

        private PrefabEntry GetSelectedPrefab()
        {
            if (filteredPrefabList.Count == 0) return null;
            if (selectedPrefabIndex < 0 || selectedPrefabIndex >= filteredPrefabList.Count) selectedPrefabIndex = 0;
            return filteredPrefabList[selectedPrefabIndex];
        }

        private string GetSelectedPrefabName()
        {
            PrefabEntry e = GetSelectedPrefab();
            return e == null ? "NONE" : e.Name;
        }

        private void Spawn()
        {
            if (!PhotonNetwork.InRoom) { spawnStatus = "NOT IN ROOM"; return; }
            PrefabEntry entry = GetSelectedPrefab();
            if (entry == null) { spawnStatus = "NO PREFAB SELECTED"; return; }
            Camera cam = Camera.main;
            if (cam == null) { spawnStatus = "NO CAMERA"; return; }
            Vector3 position = cam.transform.position + cam.transform.forward * spawnDistance;
            int count = 0;
            for (int i = 0; i < amount; i++)
            {
                try
                {
                    GameObject obj = PhotonNetwork.Instantiate(entry.Name, position, Quaternion.identity, 0);
                    if (obj == null)
                    {
                        continue;
                    }
                    spawnedObjects.Add(obj); count++;
                }
                catch (Exception ex) { Logger.LogError("Spawn failed: " + ex); spawnStatus = "SPAWN FAILED"; break; }
            }
            if (count > 0) spawnStatus = "SPAWNED " + count + "x " + entry.Name;
        }

        private void ClearAll()
        {
            int cleared = 0;
            for (int i = spawnedObjects.Count - 1; i >= 0; i--)
            {
                GameObject obj = spawnedObjects[i];
                if (obj == null) { spawnedObjects.RemoveAt(i); continue; }
                try
                {
                    PhotonView view = obj.GetComponent<PhotonView>();
                    if (PhotonNetwork.InRoom && view != null) PhotonNetwork.Destroy(view);
                    else Destroy(obj);
                    cleared++;
                }
                catch (Exception ex) { Logger.LogError("Clear failed: " + ex); }
                spawnedObjects.RemoveAt(i);
            }
            spawnStatus = "CLEARED " + cleared + " OBJECTS";
        }

        // ============================================================
        // TELEPORT UI / LOGIC
        // ============================================================
        private void DrawTeleportPanel(float x, float y, float width, float maxHeight)
        {
            float leftWidth = width * .46f;
            float rightX = x + leftWidth + 12f;
            float rightWidth = width - leftWidth - 12f;

            GUI.Label(new Rect(x, y, leftWidth, 24f), new GUIContent("Players"), headerStyle);
            GUI.Label(new Rect(rightX, y, rightWidth - 130f, 24f), new GUIContent("Teleport"), headerStyle);

            string wpBtnLabel = waypointsPopupVisible
                ? "CLOSE WAYPOINTS"
                : ("WAYPOINTS (" + savedWaypoints.Count + ")");
            if (GUI.Button(new Rect(rightX + rightWidth - 128f, y - 2f, 128f, 26f),
                new GUIContent(wpBtnLabel), buttonStyle))
            {
                waypointsPopupVisible = !waypointsPopupVisible;
                if (waypointsPopupVisible)
                {
                    waypointsPopupRect.x = menuRect.xMax + 12f;
                    waypointsPopupRect.y = menuRect.y + 80f;
                    if (waypointsPopupRect.xMax > Screen.width - 8f)
                        waypointsPopupRect.x = Mathf.Max(8f, menuRect.x - waypointsPopupRect.width - 12f);
                }
            }

            y += 30f;

            // Cap height so empty box doesn't stretch into the status bar
            float listH = Mathf.Min(Mathf.Max(180f, maxHeight - 30f), 500f);
            DrawPlayerList(x, y, leftWidth, listH);
            DrawTeleportControls(rightX, y, rightWidth, listH);
        }

        private void DrawWaypointsPopup()
        {
            if (!waypointsPopupVisible)
                return;

            waypointsPopupRect.width = Mathf.Clamp(waypointsPopupRect.width, 280f, 480f);
            waypointsPopupRect.height = Mathf.Clamp(waypointsPopupRect.height, 280f, 620f);
            waypointsPopupRect.x = Mathf.Clamp(waypointsPopupRect.x, 4f, Mathf.Max(4f, Screen.width - waypointsPopupRect.width - 4f));
            waypointsPopupRect.y = Mathf.Clamp(waypointsPopupRect.y, 4f, Mathf.Max(4f, Screen.height - waypointsPopupRect.height - 4f));

            waypointsPopupRect = GUI.Window(
                9011,
                waypointsPopupRect,
                DrawWaypointsPopupWindow,
                GUIContent.none,
                windowStyle != null ? windowStyle : GUI.skin.window
            );
        }

        private void DrawWaypointsPopupWindow(int id)
        {
            float pad = 12f;
            float x = pad;
            float y = pad;
            float w = waypointsPopupRect.width - pad * 2f;
            Event e = Event.current;

            GUI.Label(new Rect(x, y, w - 70f, 22f), new GUIContent("Waypoints"), headerStyle);
            if (GUI.Button(new Rect(waypointsPopupRect.width - pad - 64f, y - 2f, 64f, 24f),
                new GUIContent("CLOSE"), buttonStyle))
            {
                waypointsPopupVisible = false;
                waypointNameFocused = false;
            }
            y += 28f;

            if (GUI.Button(new Rect(x, y, w, 30f),
                new GUIContent("SAVE CURRENT POS (1-CLICK)"), buttonStyle))
            {
                QuickSaveWaypoint();
            }
            y += 36f;

            Rect wpField = new Rect(x, y, w - 70f, 24f);
            GUI.Box(wpField, "");
            string wpShown = string.IsNullOrEmpty(newWaypointName) ? "name..." : newWaypointName;
            if (waypointNameFocused) wpShown += "|";
            GUI.Label(new Rect(wpField.x + 4f, wpField.y + 3f, wpField.width - 8f, 18f),
                new GUIContent(wpShown), labelStyle);
            if (e != null && e.type == EventType.MouseDown && wpField.Contains(e.mousePosition))
            {
                waypointNameFocused = true;
                e.Use();
            }
            if (waypointNameFocused && e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && newWaypointName.Length > 0)
                {
                    newWaypointName = newWaypointName.Substring(0, newWaypointName.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    SaveNamedWaypoint();
                    waypointNameFocused = false;
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape)
                {
                    waypointNameFocused = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && newWaypointName.Length < 24)
                {
                    newWaypointName += e.character;
                    e.Use();
                }
            }
            if (GUI.Button(new Rect(x + w - 66f, y, 66f, 24f), new GUIContent("SAVE"), buttonStyle))
                SaveNamedWaypoint();
            y += 32f;

            GUI.Label(new Rect(x, y, w, 18f),
                new GUIContent(savedWaypoints.Count + " saved"),
                smallStyle);
            y += 22f;

            float listH = Mathf.Max(80f, waypointsPopupRect.height - y - pad);
            Rect wpListRect = new Rect(x, y, w, listH);
            GUI.Box(wpListRect, "");
            List<string> wpNames = new List<string>(savedWaypoints.Keys);
            const float wpRow = 26f;
            float wpContent = wpNames.Count * wpRow;
            float wpMax = Mathf.Max(0f, wpContent - listH + 4f);
            if (e != null && e.type == EventType.ScrollWheel && wpListRect.Contains(e.mousePosition))
            {
                waypointListScroll.y = Mathf.Clamp(waypointListScroll.y + e.delta.y * 20f, 0f, wpMax);
                e.Use();
            }
            waypointListScroll.y = Mathf.Clamp(waypointListScroll.y, 0f, wpMax);
            GUI.BeginGroup(
                new Rect(wpListRect.x + 2f, wpListRect.y + 2f, wpListRect.width - 4f, wpListRect.height - 4f),
                GUIContent.none,
                GUIStyle.none);
            float wpy = -waypointListScroll.y;
            for (int i = 0; i < wpNames.Count; i++)
            {
                string n = wpNames[i];
                if (GUI.Button(new Rect(0f, wpy, wpListRect.width - 56f, 24f), new GUIContent(n), buttonStyle))
                    TeleportToWaypoint(n);
                if (GUI.Button(new Rect(wpListRect.width - 52f, wpy, 48f, 24f), new GUIContent("DEL"), buttonStyle))
                    savedWaypoints.Remove(n);
                wpy += wpRow;
            }
            if (wpNames.Count == 0)
                GUI.Label(new Rect(6f, 6f, w - 20f, 20f), new GUIContent("No waypoints yet."), smallStyle);
            GUI.EndGroup();

            GUI.DragWindow(new Rect(0f, 0f, waypointsPopupRect.width, 28f));
        }

        // ============================================================
        // TESTING TAB (host tools / ownership — fly/waypoints/softTP moved out)
        // ============================================================
        private void DrawTestingPanel(float x, float y, float width, float maxHeight)
        {
            float startY = y;
            float leftW = width * 0.52f;
            float gap = 12f;
            float rightX = x + leftW + gap;
            float rightW = width - leftW - gap;
        }

        private void DrawPlayerList(float x, float y, float width, float height)
        {
            GUI.Box(new Rect(x, y, width, height), "");
            if (!PhotonNetwork.InRoom)
            {
                GUI.Label(new Rect(x + 12f, y + 15f, width - 24f, 25f), new GUIContent("Not in a multiplayer room."), labelStyle);
                return;
            }

            Player[] players = PhotonNetwork.PlayerList;
            const float rowHeight = 38f;
            float viewportH = height - 10f;
            float contentH = players.Length * rowHeight;
            float maxScroll = Mathf.Max(0f, contentH - viewportH);
            Rect viewport = new Rect(x + 5f, y + 5f, width - 10f, viewportH);

            Event e = Event.current;
            if (e != null && e.type == EventType.ScrollWheel && viewport.Contains(e.mousePosition))
            {
                playerScroll.y = Mathf.Clamp(playerScroll.y + e.delta.y * 25f, 0f, maxScroll);
                e.Use();
            }

            playerScroll.y = Mathf.Clamp(playerScroll.y, 0f, maxScroll);
            GUI.BeginGroup(viewport, new GUIContent(""), GUIStyle.none);
            float rowY = -playerScroll.y;
            GameObject local = GetLocalPlayer();
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null) continue;
                string name = string.IsNullOrEmpty(p.NickName) ? "Player " + p.ActorNumber : p.NickName;
                GameObject obj = FindPlayerObject(p);
                float dist = local != null && obj != null ? Vector3.Distance(local.transform.position, obj.transform.position) : -1f;
                string text = name + "  #" + p.ActorNumber;
                if (dist >= 0f) text += "  " + dist.ToString("0.0") + "m";
                if (p.IsLocal) text += "  [YOU]";
                string note = GetPlayerNote(p);
                if (!string.IsNullOrEmpty(note)) text += "  📝";
                GUIStyle style = selectedActorId == p.ActorNumber ? selectedButtonStyle : buttonStyle;
                if (GUI.Button(new Rect(0f, rowY, viewport.width - 8f, 32f), new GUIContent(text), style))
                {
                    SelectPlayer(p);
                }
                rowY += rowHeight;
            }
            GUI.EndGroup();
        }

        private void DrawTeleportControls(float x, float y, float width, float height)
        {
            GUI.Box(new Rect(x, y, width, height), "");
            string selected = selectedPlayer == null ? "None" : GetPlayerName(selectedPlayer);
            GUI.Label(new Rect(x + 12f, y + 12f, width - 24f, 28f), new GUIContent("Selected: " + selected), labelStyle);
            bool target = selectedPlayer != null && !selectedPlayer.IsLocal;

            // Spectate controls
            float specY = y + 48f;
            GUI.Label(new Rect(x + 12f, specY, width - 24f, 22f), new GUIContent("Spectate"), headerStyle);
            bool canSpectate = target && FindPlayerObject(selectedPlayer) != null;
            float sbw = (width - 48f) / 3f;
            if (GUI.Button(new Rect(x + 12f, specY + 28f, sbw, 32f), new GUIContent(spectating ? "SPECTATING" : "SPECTATE"), canSpectate ? selectedButtonStyle : GUI.skin.button) && canSpectate)
                StartSpectating();
            if (GUI.Button(new Rect(x + 18f + sbw, specY + 28f, sbw, 32f), new GUIContent("◀ PREV"), buttonStyle) && PhotonNetwork.InRoom)
                CycleSpectatePlayer(-1);
            if (GUI.Button(new Rect(x + 24f + sbw * 2f, specY + 28f, sbw, 32f), new GUIContent("NEXT ▶"), buttonStyle) && PhotonNetwork.InRoom)
                CycleSpectatePlayer(1);
            if (GUI.Button(new Rect(x + 12f, specY + 66f, width - 24f, 30f), new GUIContent("STOP SPECTATING"), spectating ? buttonStyle : GUI.skin.button) && spectating)
                StopSpectating();

            float camY = specY + 104f;
            GUI.Label(new Rect(x + 12f, camY, 130f, 20f), new GUIContent("CAM HEIGHT: " + spectateCameraHeight.ToString("0.00") + "m"), labelStyle);
            spectateCameraHeight = GUI.HorizontalSlider(new Rect(x + 147f, camY + 2f, width - 24f - 147f, 18f), spectateCameraHeight, 0f, 5f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            camY += 26f;

            GUI.Label(new Rect(x + 12f, camY, 130f, 20f), new GUIContent("CAM DISTANCE: " + spectateCameraDistance.ToString("0.00") + "m"), labelStyle);
            spectateCameraDistance = GUI.HorizontalSlider(new Rect(x + 147f, camY + 2f, width - 24f - 147f, 18f), spectateCameraDistance, 1f, 10f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            camY += 26f;

            GUI.Label(new Rect(x + 12f, camY, 130f, 20f), new GUIContent("CAM ROTATION: " + Mathf.RoundToInt(spectateCameraRotation) + "°"), labelStyle);
            spectateCameraRotation = GUI.HorizontalSlider(new Rect(x + 147f, camY + 2f, width - 24f - 147f, 18f), spectateCameraRotation, 0f, 360f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            camY += 30f;

            float friendY = camY;
            if (target)
            {
                bool isFriend = friendActorIds.Contains(selectedPlayer.ActorNumber);
                if (GUI.Button(new Rect(x + 12f, friendY, width - 24f, 30f), new GUIContent(isFriend ? "★ Friend" : "☆ Friend"), buttonStyle))
                {
                    if (isFriend) friendActorIds.Remove(selectedPlayer.ActorNumber);
                    else friendActorIds.Add(selectedPlayer.ActorNumber);
                }

                if (!notesInputFocused)
                    notesInput = GetPlayerNote(selectedPlayer);

                Rect noteFieldRect = new Rect(x + 12f, friendY + 34f, width - 24f - 62f, 26f);
                GUI.Box(noteFieldRect, new GUIContent(""), GUI.skin.box);
                string noteDisplay = string.IsNullOrEmpty(notesInput) ? "CLICK TO ADD NOTE..." : notesInput;
                GUI.Label(new Rect(noteFieldRect.x + 6f, noteFieldRect.y + 3f, noteFieldRect.width - 12f, 20f), new GUIContent(noteDisplay), labelStyle);

                Event noteEvent = Event.current;
                if (noteEvent != null && noteEvent.type == EventType.MouseDown && noteFieldRect.Contains(noteEvent.mousePosition))
                {
                    notesInputFocused = true;
                    noteEvent.Use();
                }
                else if (noteEvent != null && noteEvent.type == EventType.MouseDown && !noteFieldRect.Contains(noteEvent.mousePosition))
                {
                    notesInputFocused = false;
                }

                if (notesInputFocused && noteEvent != null && noteEvent.type == EventType.KeyDown)
                {
                    if (noteEvent.keyCode == KeyCode.Backspace)
                    {
                        if (notesInput.Length > 0) notesInput = notesInput.Substring(0, notesInput.Length - 1);
                        noteEvent.Use();
                    }
                    else if (noteEvent.keyCode == KeyCode.Return || noteEvent.keyCode == KeyCode.KeypadEnter)
                    {
                        SetPlayerNote(selectedPlayer, notesInput);
                        notesInputFocused = false;
                        noteEvent.Use();
                    }
                    else if (noteEvent.keyCode == KeyCode.Escape)
                    {
                        notesInput = GetPlayerNote(selectedPlayer);
                        notesInputFocused = false;
                        noteEvent.Use();
                    }
                    else if (noteEvent.character != '\0' && !char.IsControl(noteEvent.character))
                    {
                        if (notesInput.Length < 60) notesInput += noteEvent.character;
                        noteEvent.Use();
                    }
                }

                if (GUI.Button(
                    new Rect(x + 12f + width - 24f - 56f, friendY + 34f, 56f, 26f),
                    new GUIContent("SAVE"),
                    buttonStyle))
                {
                    SetPlayerNote(selectedPlayer, notesInput);
                    notesInputFocused = false;
                }
            }

            // Teleport controls
            float teleY = friendY + (target ? 70f : 10f);
            GUI.Label(new Rect(x + 12f, teleY, width - 24f, 22f), new GUIContent("Teleport"), headerStyle);
            teleY += 26f;

            float tbw = (width - 48f) / 3f;
            if (GUI.Button(new Rect(x + 12f, teleY, tbw, 32f), new GUIContent("BEHIND"), target ? buttonStyle : GUI.skin.button) && target)
                TeleportBehindTarget();
            if (GUI.Button(new Rect(x + 18f + tbw, teleY, tbw, 32f), new GUIContent("IN FRONT"), target ? buttonStyle : GUI.skin.button) && target)
                TeleportInFrontOfTarget();
            if (GUI.Button(new Rect(x + 24f + tbw * 2f, teleY, tbw, 32f), new GUIContent("ABOVE"), target ? buttonStyle : GUI.skin.button) && target)
                TeleportAboveTarget();
            teleY += 40f;

            GUI.Label(new Rect(x + 12f, teleY, 100f, 20f), new GUIContent("BEHIND: " + behindDistance.ToString("0.0") + "m"), labelStyle);
            behindDistance = GUI.HorizontalSlider(new Rect(x + 115f, teleY + 2f, width - 140f, 18f), behindDistance, 0.5f, 15f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            teleY += 26f;

            GUI.Label(new Rect(x + 12f, teleY, 100f, 20f), new GUIContent("FRONT: " + frontDistance.ToString("0.0") + "m"), labelStyle);
            frontDistance = GUI.HorizontalSlider(new Rect(x + 115f, teleY + 2f, width - 140f, 18f), frontDistance, 0.5f, 15f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            teleY += 26f;

            GUI.Label(new Rect(x + 12f, teleY, 100f, 20f), new GUIContent("ABOVE: " + aboveDistance.ToString("0.0") + "m"), labelStyle);
            aboveDistance = GUI.HorizontalSlider(new Rect(x + 115f, teleY + 2f, width - 140f, 18f), aboveDistance, 0.5f, 20f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            teleY += 34f;

            string originStatus = originCaptured ? "CAPTURED" : "WAITING";
            GUI.Label(new Rect(x + 12f, teleY, width - 24f - 150f, 26f), new GUIContent("Origin: " + originStatus), labelStyle);
            if (GUI.Button(new Rect(x + width - 150f, teleY, 56f, 26f), new GUIContent("SAVE"), buttonStyle))
            {
                GameObject local = GetLocalPlayer();
                if (local != null)
                {
                    originPosition = local.transform.position;
                    originCaptured = true;
                }
            }
            // Wider button so "TELEPORT" is not clipped to "ELEPORT"
            if (GUI.Button(new Rect(x + width - 90f, teleY, 78f, 26f), new GUIContent("Teleport"), originCaptured ? buttonStyle : GUI.skin.button) && originCaptured)
                TeleportToOrigin();
            teleY += 34f;

            // Position restore after server-browser rejoin
            if (GUI.Button(new Rect(x + 12f, teleY, width - 24f, 28f),
                new GUIContent(browsePositionRestoreEnabled
                    ? "POS RESTORE AFTER BROWSE: ON"
                    : "POS RESTORE AFTER BROWSE: OFF"),
                buttonStyle))
            {
                browsePositionRestoreEnabled = !browsePositionRestoreEnabled;
                if (configBrowsePositionRestore != null)
                    configBrowsePositionRestore.Value = browsePositionRestoreEnabled;
                if (!browsePositionRestoreEnabled)
                {
                    browseRestoreActive = false;
                    browseHasSavedTransform = false;
                }
            }
            teleY += 32f;

        }

        private void DrawHostToolsPanel(float x, float y, float width, float maxHeight)
        {
            // Content ends near LEAVE ROOM; keep room for purge + room options.
            float panelH = Mathf.Min(Mathf.Max(280f, maxHeight), 560f);
            GUI.Box(new Rect(x, y, width, panelH), "");

            bool inRoom = PhotonNetwork.InRoom;
            bool isHost = inRoom && PhotonNetwork.IsMasterClient;

            if (!isHost)
            {
                kickConfirmationVisible = false;
                kickAllConfirmationVisible = false;
                pendingKickPlayer = null;
                banConfirmationVisible = false;
                pendingBanPlayer = null;
            }

            GUI.Label(
                new Rect(x + 12f, y + 10f, width - 24f, 24f),
                new GUIContent(
                    !inRoom
                        ? "HOST STATUS: NOT IN ROOM"
                        : isHost
                            ? "HOST STATUS: MASTER CLIENT"
                            : "HOST STATUS: NOT HOST"),
                headerStyle);

            if (!inRoom)
            {
                GUI.Label(
                    new Rect(x + 12f, y + 48f, width - 24f, 40f),
                    new GUIContent("Join a room first"),
                    labelStyle);
                return;
            }

            float gap = 12f;
            float leftWidth = width * 0.46f;
            float rightX = x + leftWidth + gap;
            float rightWidth = width - leftWidth - gap;

            GUI.Label(
                new Rect(x + 12f, y + 40f, leftWidth - 12f, 22f),
                new GUIContent("Players"),
                headerStyle);

            GUI.Label(
                new Rect(rightX, y + 40f, rightWidth - 12f, 22f),
                new GUIContent("Actions"),
                headerStyle);

            float playerListH = Mathf.Max(160f, panelH - 90f);
            DrawPlayerList(
                x + 12f,
                y + 68f,
                leftWidth - 12f,
                playerListH);

            Player selected = selectedPlayer;
            bool anyConfirmationVisible = kickConfirmationVisible || kickAllConfirmationVisible || banConfirmationVisible;
            bool canUseSelected =
                isHost &&
                selected != null &&
                !selected.IsLocal &&
                !anyConfirmationVisible;

            int otherPlayerCount =
                PhotonNetwork.PlayerList != null
                    ? Mathf.Max(0, PhotonNetwork.PlayerList.Length - 1)
                    : 0;

            bool kickAllOnCooldown = Time.unscaledTime < kickAllCooldownUntil;

            bool canKickAll =
                isHost &&
                otherPlayerCount > 0 &&
                !kickAllOnCooldown &&
                !anyConfirmationVisible;

            string selectedName =
                selected == null ? "None" : GetPlayerName(selected);

            GUI.Label(
                new Rect(rightX, y + 70f, rightWidth - 12f, 24f),
                new GUIContent("Selected: " + selectedName),
                labelStyle);

            if (GUI.Button(
                new Rect(rightX, y + 106f, rightWidth - 12f, 34f),
                new GUIContent("KICK SELECTED PLAYER"),
                canUseSelected ? buttonStyle : GUI.skin.button) &&
                canUseSelected)
            {
                RequestKickSelectedPlayer();
            }

            string kickAllLabel = kickAllOnCooldown
                ? "KICK ALL OTHERS (" + Mathf.CeilToInt(kickAllCooldownUntil - Time.unscaledTime) + "s)"
                : "KICK ALL OTHERS";

            if (GUI.Button(
                new Rect(rightX, y + 146f, rightWidth - 12f, 34f),
                new GUIContent(kickAllLabel),
                canKickAll ? buttonStyle : GUI.skin.button) &&
                canKickAll)
            {
                RequestKickAllOtherPlayers();
            }

            if (GUI.Button(
                new Rect(rightX, y + 186f, rightWidth - 12f, 34f),
                new GUIContent("TRANSFER MASTER CLIENT"),
                canUseSelected ? buttonStyle : GUI.skin.button) &&
                canUseSelected)
            {
                TransferMasterClient();
            }

            if (GUI.Button(
                new Rect(rightX, y + 226f, rightWidth - 12f, 34f),
                new GUIContent("BAN SELECTED PLAYER"),
                canUseSelected ? buttonStyle : GUI.skin.button) &&
                canUseSelected)
            {
                RequestBanSelectedPlayer();
            }

            if (GUI.Button(
                new Rect(rightX, y + 266f, rightWidth - 12f, 34f),
                new GUIContent("DELETE EXTRA KOBOLDS"),
                isHost ? buttonStyle : GUI.skin.button) &&
                isHost)
            {
                int n = PurgeOrphanKobolds();
                ShowToast(n > 0 ? ("DELETED " + n + " EXTRA KOBOLD(s)") : "No Exta Kobolds Found.");
            }

            Room room = PhotonNetwork.CurrentRoom;
            if (room == null)
                return;

            GUI.Label(
                new Rect(rightX, y + 312f, rightWidth - 12f, 22f),
                new GUIContent("ROOM OPTIONS"),
                headerStyle);

            float roomOptGap = 10f;
            float roomOptButtonWidth = (rightWidth - 12f - roomOptGap) / 2f;

            string roomOpenLabel = room.IsOpen ? "CLOSE ROOM" : "OPEN ROOM";
            string roomVisibleLabel = room.IsVisible ? "HIDE ROOM" : "SHOW ROOM";

            if (GUI.Button(
                new Rect(rightX, y + 338f, roomOptButtonWidth, 32f),
                new GUIContent(roomOpenLabel),
                isHost ? buttonStyle : GUI.skin.button) &&
                isHost)
            {
                ToggleRoomOpen();
            }

            if (GUI.Button(
                new Rect(rightX + roomOptButtonWidth + roomOptGap, y + 338f, roomOptButtonWidth, 32f),
                new GUIContent(roomVisibleLabel),
                isHost ? buttonStyle : GUI.skin.button) &&
                isHost)
            {
                ToggleRoomVisibility();
            }

            GUI.Label(new Rect(rightX, y + 382f, rightWidth - 12f, 22f), new GUIContent("ROOM SIZE: " + room.MaxPlayers), labelStyle);

            int maxPlayers = room.MaxPlayers > 0
                ? room.MaxPlayers
                : 32;

            int newMaxPlayers = Mathf.RoundToInt(GUI.HorizontalSlider(
                new Rect(rightX, y + 410f, rightWidth - 12f, 18f),
                maxPlayers,
                1f,
                32f,
                GUI.skin.horizontalSlider,
                GUI.skin.horizontalSliderThumb));

            if (isHost && newMaxPlayers != room.MaxPlayers)
                room.MaxPlayers = (byte)newMaxPlayers;

            string currentLabel = GetRoomLabel(room);

            GUI.Label(
                new Rect(rightX, y + 446f, rightWidth - 12f, 20f),
                new GUIContent("ROOM: " + room.Name + (string.IsNullOrEmpty(currentLabel) ? "" : "  (" + currentLabel + ")")),
                smallStyle);

            if (!roomLabelFocused)
                roomLabelInput = currentLabel;

            Rect labelFieldRect = new Rect(rightX, y + 468f, rightWidth - 12f - 62f, 26f);
            GUI.Box(labelFieldRect, new GUIContent(""), GUI.skin.box);
            string labelDisplay = string.IsNullOrEmpty(roomLabelInput) ? "CLICK TO SET ROOM LABEL..." : roomLabelInput;
            GUI.Label(new Rect(labelFieldRect.x + 6f, labelFieldRect.y + 3f, labelFieldRect.width - 12f, 20f), new GUIContent(labelDisplay), labelStyle);

            Event labelEvent = Event.current;
            if (labelEvent != null && labelEvent.type == EventType.MouseDown && labelFieldRect.Contains(labelEvent.mousePosition))
            {
                roomLabelFocused = true;
                labelEvent.Use();
            }
            else if (labelEvent != null && labelEvent.type == EventType.MouseDown && !labelFieldRect.Contains(labelEvent.mousePosition))
            {
                roomLabelFocused = false;
            }

            if (roomLabelFocused && labelEvent != null && labelEvent.type == EventType.KeyDown)
            {
                if (labelEvent.keyCode == KeyCode.Backspace)
                {
                    if (roomLabelInput.Length > 0) roomLabelInput = roomLabelInput.Substring(0, roomLabelInput.Length - 1);
                    labelEvent.Use();
                }
                else if (labelEvent.keyCode == KeyCode.Return || labelEvent.keyCode == KeyCode.KeypadEnter)
                {
                    if (isHost) SetRoomLabel(room, roomLabelInput);
                    roomLabelFocused = false;
                    labelEvent.Use();
                }
                else if (labelEvent.keyCode == KeyCode.Escape)
                {
                    roomLabelInput = currentLabel;
                    roomLabelFocused = false;
                    labelEvent.Use();
                }
                else if (labelEvent.character != '\0' && !char.IsControl(labelEvent.character))
                {
                    if (roomLabelInput.Length < 40) roomLabelInput += labelEvent.character;
                    labelEvent.Use();
                }
            }

            if (GUI.Button(
                new Rect(rightX + rightWidth - 12f - 56f, y + 468f, 56f, 26f),
                new GUIContent("Set"),
                isHost ? buttonStyle : GUI.skin.button) &&
                isHost)
            {
                SetRoomLabel(room, roomLabelInput);
                roomLabelFocused = false;
            }

            if (GUI.Button(
                new Rect(rightX, y + 500f, rightWidth - 12f, 30f),
                new GUIContent("LEAVE ROOM"),
                buttonStyle))
            {
                LeaveCurrentRoom();
            }

            // Recent events + ban list live on the Host Logs tab now.

            if (kickConfirmationVisible && pendingKickPlayer != null)
            {
                float dialogWidth = rightWidth - 20f;
                float dialogX = rightX + 5f;
                float dialogY = y + 95f;

                GUI.Box(
                    new Rect(dialogX, dialogY, dialogWidth, 112f),
                    "");

                GUI.Label(
                    new Rect(dialogX + 8f, dialogY + 8f, dialogWidth - 16f, 24f),
                    new GUIContent(
                        "Kick " + GetPlayerName(pendingKickPlayer) + "?"),
                    labelStyle);

                float buttonWidth = (dialogWidth - 24f) / 2f;

                if (GUI.Button(
                    new Rect(dialogX + 8f, dialogY + 48f, buttonWidth, 32f),
                    new GUIContent("YES"),
                    buttonStyle))
                {
                    ConfirmKickPlayer();
                }

                if (GUI.Button(
                    new Rect(
                        dialogX + 16f + buttonWidth,
                        dialogY + 48f,
                        buttonWidth,
                        32f),
                    new GUIContent("NO"),
                    buttonStyle))
                {
                    CancelKickPlayer();
                }
            }
            else if (kickAllConfirmationVisible)
            {
                float dialogWidth = rightWidth - 20f;
                float dialogX = rightX + 5f;
                float dialogY = y + 95f;

                GUI.Box(
                    new Rect(dialogX, dialogY, dialogWidth, 112f),
                    "");

                GUI.Label(
                    new Rect(dialogX + 8f, dialogY + 8f, dialogWidth - 16f, 24f),
                    new GUIContent("Kick ALL " + otherPlayerCount + " other player" + (otherPlayerCount == 1 ? "" : "s") + "?"),
                    labelStyle);

                float buttonWidth = (dialogWidth - 24f) / 2f;

                if (GUI.Button(
                    new Rect(dialogX + 8f, dialogY + 48f, buttonWidth, 32f),
                    new GUIContent("YES"),
                    buttonStyle))
                {
                    ConfirmKickAllOtherPlayers();
                }

                if (GUI.Button(
                    new Rect(
                        dialogX + 16f + buttonWidth,
                        dialogY + 48f,
                        buttonWidth,
                        32f),
                    new GUIContent("NO"),
                    buttonStyle))
                {
                    CancelKickAllOtherPlayers();
                }
            }
            else if (banConfirmationVisible && pendingBanPlayer != null)
            {
                float dialogWidth = rightWidth - 20f;
                float dialogX = rightX + 5f;
                float dialogY = y + 95f;

                GUI.Box(
                    new Rect(dialogX, dialogY, dialogWidth, 112f),
                    "");

                GUI.Label(
                    new Rect(dialogX + 8f, dialogY + 8f, dialogWidth - 16f, 24f),
                    new GUIContent(
                        "Ban " + GetPlayerName(pendingBanPlayer) + "?"),
                    labelStyle);

                float buttonWidth = (dialogWidth - 24f) / 2f;

                if (GUI.Button(
                    new Rect(dialogX + 8f, dialogY + 48f, buttonWidth, 32f),
                    new GUIContent("YES"),
                    buttonStyle))
                {
                    ConfirmBanPlayer();
                }

                if (GUI.Button(
                    new Rect(
                        dialogX + 16f + buttonWidth,
                        dialogY + 48f,
                        buttonWidth,
                        32f),
                    new GUIContent("NO"),
                    buttonStyle))
                {
                    CancelBanPlayer();
                }
            }
        }

        private void RequestKickAllOtherPlayers()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            kickAllConfirmationVisible = true;
        }

        private string GetRoomLabel(Room room)
        {
            if (room == null || room.CustomProperties == null)
                return "";

            object value;
            if (room.CustomProperties.TryGetValue(RoomLabelPropertyKey, out value) && value is string)
                return (string)value;

            return "";
        }

        private void SetRoomLabel(Room room, string label)
        {
            if (room == null || !PhotonNetwork.IsMasterClient)
                return;

            ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
            {
                { RoomLabelPropertyKey, label ?? "" }
            };

            room.SetCustomProperties(props);
        }

        private void RequestKickSelectedPlayer()
        {
            if (!PhotonNetwork.InRoom ||
                !PhotonNetwork.IsMasterClient ||
                selectedPlayer == null ||
                selectedPlayer.IsLocal)
            {
                return;
            }

            pendingKickPlayer = selectedPlayer;
            ConfirmKickPlayer();
        }

        private void TransferMasterClient()
        {
            if (!PhotonNetwork.InRoom ||
                !PhotonNetwork.IsMasterClient ||
                selectedPlayer == null ||
                selectedPlayer.IsLocal)
            {
                return;
            }

            PhotonNetwork.SetMasterClient(selectedPlayer);
        }

        private void ToggleRoomOpen()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            Room room = PhotonNetwork.CurrentRoom;
            if (room != null)
                room.IsOpen = !room.IsOpen;
        }

        private void ToggleRoomVisibility()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            Room room = PhotonNetwork.CurrentRoom;
            if (room != null)
                room.IsVisible = !room.IsVisible;
        }

        private void ShowToast(string msg)
        {
            if (string.IsNullOrEmpty(msg))
                return;
            // Queue so rapid actions don't overwrite each other
            toastQueue.Enqueue(msg);
            // If nothing showing, promote immediately
            if (string.IsNullOrEmpty(toastMessage) || Time.unscaledTime > toastUntil)
                PromoteNextToast();
        }

        private void PromoteNextToast()
        {
            while (toastQueue.Count > 0)
            {
                string next = toastQueue.Dequeue();
                if (string.IsNullOrEmpty(next))
                    continue;
                toastMessage = next;
                toastUntil = Time.unscaledTime + ToastDuration;
                return;
            }
            toastMessage = "";
        }

        private void DrawStatusToast()
        {
            if (Time.unscaledTime > toastUntil)
            {
                if (toastQueue.Count > 0)
                    PromoteNextToast();
                else if (!string.IsNullOrEmpty(toastMessage))
                    toastMessage = "";
            }
            if (string.IsNullOrEmpty(toastMessage) || Time.unscaledTime > toastUntil)
                return;

            float tw = Mathf.Min(520f, Screen.width - 40f);
            float th = 36f;
            float tx = (Screen.width - tw) * 0.5f;
            float ty = 18f;

            Color prev = GUI.color;
            GUI.color = new Color(0.05f, 0.07f, 0.1f, 0.92f);
            GUI.Box(new Rect(tx, ty, tw, th), "");
            GUI.color = Color.white;
            GUIStyle style = labelStyle != null ? labelStyle : GUI.skin.label;
            GUI.Label(new Rect(tx + 12f, ty + 6f, tw - 24f, 24f), new GUIContent(toastMessage), style);
            GUI.color = prev;
        }

        private void DrawModsPanel(float x, float y, float width, float maxHeight)
        {
            RefreshModListIfNeeded();

            Event e = Event.current;
            const float rowH = 20f;
            Color prevBg = GUI.backgroundColor;
            Color accentSel = GetMenuSelectionColor(0.92f);

            // Header row: title + counts
            GUI.Label(new Rect(x, y, width * 0.4f, 22f), new GUIContent("MODS"), headerStyle);
            GUI.Label(new Rect(x + width * 0.4f, y + 2f, width * 0.6f, 18f),
                new GUIContent(CountEnabledMods() + " selected  ·  " + quickLobbyModTitles.Count + " total"),
                smallStyle);
            y += 26f;

            // Search + clear on one line
            Rect modFilterRect = new Rect(x, y, width - 64f, 22f);
            GUI.Box(modFilterRect, "");
            string modFilterShown = string.IsNullOrEmpty(quickLobbyModFilter) ? "Search…" : quickLobbyModFilter;
            if (quickLobbyModFilterFocused) modFilterShown += "|";
            GUI.Label(new Rect(modFilterRect.x + 6f, modFilterRect.y + 2f, modFilterRect.width - 12f, 18f),
                new GUIContent(modFilterShown), labelStyle);
            if (e != null && e.type == EventType.MouseDown && modFilterRect.Contains(e.mousePosition))
            {
                quickLobbyModFilterFocused = true;
                modPresetNameFocused = false;
                e.Use();
            }
            if (quickLobbyModFilterFocused && e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && quickLobbyModFilter.Length > 0)
                {
                    quickLobbyModFilter = quickLobbyModFilter.Substring(0, quickLobbyModFilter.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    quickLobbyModFilterFocused = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && quickLobbyModFilter.Length < 48)
                {
                    quickLobbyModFilter += e.character;
                    e.Use();
                }
            }
            if (GUI.Button(new Rect(x + width - 60f, y, 60f, 22f), new GUIContent("✕"), buttonStyle))
            {
                quickLobbyModFilter = "";
                quickLobbyModFilterFocused = false;
            }
            y += 28f;

            // Lists
            float listH = Mathf.Min(300f, Mathf.Max(180f, maxHeight - 200f));
            float gap = 8f;
            float half = (width - gap) * 0.5f;
            int visibleModCount = CountModsMatchingFilter();

            GUI.Label(new Rect(x, y, half, 16f),
                new GUIContent("Available (" + visibleModCount + ")"), smallStyle);
            GUI.Label(new Rect(x + half + gap, y, half, 16f),
                new GUIContent("Selected (" + CountEnabledMods() + ")"), smallStyle);
            y += 16f;

            Rect modAllRect = new Rect(x, y, half, listH);
            Rect modSelRect = new Rect(x + half + gap, y, half, listH);
            GUI.Box(modAllRect, "");
            GUI.Box(modSelRect, "");

            float modContent = visibleModCount * rowH;
            float modMax = Mathf.Max(0f, modContent - (listH - 4f));
            if (e != null && e.type == EventType.ScrollWheel && modAllRect.Contains(e.mousePosition))
            {
                quickLobbyModScroll.y = Mathf.Clamp(quickLobbyModScroll.y + e.delta.y * 20f, 0f, modMax);
                e.Use();
            }
            quickLobbyModScroll.y = Mathf.Clamp(quickLobbyModScroll.y, 0f, modMax);

            GUI.BeginGroup(new Rect(modAllRect.x + 2f, modAllRect.y + 2f, half - 4f, listH - 4f), GUIContent.none, GUIStyle.none);
            float ay = -quickLobbyModScroll.y;
            if (quickLobbyModTitles.Count == 0)
            {
                GUI.Label(new Rect(4f, 4f, half - 12f, 36f), new GUIContent("Empty — hit Refresh"), smallStyle);
            }
            else if (visibleModCount == 0)
            {
                GUI.Label(new Rect(4f, 4f, half - 12f, 36f), new GUIContent("No matches"), smallStyle);
            }
            else
            {
                for (int i = 0; i < quickLobbyModTitles.Count; i++)
                {
                    if (!ModMatchesFilter(i))
                        continue;
                    bool on = i < quickLobbyModEnabled.Count && quickLobbyModEnabled[i];
                    if (on) GUI.backgroundColor = accentSel;
                    else GUI.backgroundColor = prevBg;
                    GUIStyle st = on && selectedButtonStyle != null ? selectedButtonStyle : buttonStyle;
                    if (GUI.Button(new Rect(0f, ay, half - 8f, 18f),
                        new GUIContent(quickLobbyModTitles[i]), st))
                    {
                        if (i < quickLobbyModEnabled.Count)
                            quickLobbyModEnabled[i] = !quickLobbyModEnabled[i];
                    }
                    ay += rowH;
                }
            }
            GUI.EndGroup();
            GUI.backgroundColor = prevBg;

            float selContent = CountEnabledMods() * rowH;
            float selMax = Mathf.Max(0f, selContent - (listH - 4f));
            if (e != null && e.type == EventType.ScrollWheel && modSelRect.Contains(e.mousePosition))
            {
                quickLobbyModSelectedScroll.y = Mathf.Clamp(
                    quickLobbyModSelectedScroll.y + e.delta.y * 20f, 0f, selMax);
                e.Use();
            }
            quickLobbyModSelectedScroll.y = Mathf.Clamp(quickLobbyModSelectedScroll.y, 0f, selMax);

            GUI.BeginGroup(new Rect(modSelRect.x + 2f, modSelRect.y + 2f, half - 4f, listH - 4f), GUIContent.none, GUIStyle.none);
            float sy = -quickLobbyModSelectedScroll.y;
            int drawn = 0;
            for (int i = 0; i < quickLobbyModTitles.Count; i++)
            {
                if (i >= quickLobbyModEnabled.Count || !quickLobbyModEnabled[i])
                    continue;
                GUI.backgroundColor = accentSel;
                GUIStyle st = selectedButtonStyle != null ? selectedButtonStyle : buttonStyle;
                if (GUI.Button(new Rect(0f, sy, half - 8f, 18f),
                    new GUIContent(quickLobbyModTitles[i]), st))
                {
                    quickLobbyModEnabled[i] = false;
                }
                sy += rowH;
                drawn++;
            }
            GUI.backgroundColor = prevBg;
            if (drawn == 0)
            {
                GUI.Label(new Rect(4f, 4f, half - 12f, 36f), new GUIContent("None"), smallStyle);
            }
            GUI.EndGroup();

            y += listH + 10f;

            // Actions — three equal buttons
            float bw = (width - 16f) / 3f;
            if (GUI.Button(new Rect(x, y, bw, 28f), new GUIContent("Refresh"), buttonStyle))
            {
                nextModListRefresh = 0f;
                RefreshModListIfNeeded(true);
                ShowToast(quickLobbyModTitles.Count > 0
                    ? (quickLobbyModTitles.Count + " mods")
                    : "No mods");
            }
            if (GUI.Button(new Rect(x + bw + 8f, y, bw, 28f), new GUIContent("Clear"), buttonStyle))
            {
                for (int i = 0; i < quickLobbyModEnabled.Count; i++)
                    quickLobbyModEnabled[i] = false;
            }
            string applyLabel = applyModsRunning ? "…" : "Apply";
            if (GUI.Button(new Rect(x + 2f * (bw + 8f), y, bw, 28f), new GUIContent(applyLabel), buttonStyle))
            {
                if (!applyModsRunning)
                    ApplySelectedMods();
            }
            y += 32f;
            if (!string.IsNullOrEmpty(applyModsStatus) && Time.unscaledTime < applyModsStatusUntil)
            {
                GUI.Label(new Rect(x, y, width, 18f), new GUIContent(applyModsStatus), smallStyle);
                y += 22f;
            }
            else
                y += 4f;

            // Presets — compact
            GUI.Label(new Rect(x, y, 48f, 18f), new GUIContent("Presets"), smallStyle);
            Rect pnRect = new Rect(x + 52f, y - 1f, width * 0.28f, 22f);
            GUI.Box(pnRect, "");
            string pnShown = string.IsNullOrEmpty(modPresetName) ? "name" : modPresetName;
            if (modPresetNameFocused) pnShown += "|";
            GUI.Label(new Rect(pnRect.x + 4f, pnRect.y + 2f, pnRect.width - 8f, 18f), new GUIContent(pnShown), labelStyle);
            Event pe = Event.current;
            if (pe != null && pe.type == EventType.MouseDown && pnRect.Contains(pe.mousePosition))
            {
                modPresetNameFocused = true;
                quickLobbyModFilterFocused = false;
                pe.Use();
            }
            if (modPresetNameFocused && pe != null && pe.type == EventType.KeyDown)
            {
                if (pe.keyCode == KeyCode.Backspace && modPresetName.Length > 0)
                { modPresetName = modPresetName.Substring(0, modPresetName.Length - 1); pe.Use(); }
                else if (pe.keyCode == KeyCode.Escape || pe.keyCode == KeyCode.Return)
                { modPresetNameFocused = false; pe.Use(); }
                else if (pe.character != '\0' && !char.IsControl(pe.character) && modPresetName.Length < 24)
                { modPresetName += pe.character; pe.Use(); }
            }
            float pbx = x + 52f + width * 0.28f + 6f;
            float pbw = 54f;
            if (GUI.Button(new Rect(pbx, y - 1f, pbw, 22f), new GUIContent("Save"), buttonStyle))
                SaveCurrentModsAsPreset(modPresetName);
            if (GUI.Button(new Rect(pbx + pbw + 4f, y - 1f, pbw, 22f), new GUIContent("Load"), buttonStyle) &&
                selectedModPreset >= 0 && selectedModPreset < modPresetNames.Count)
                ApplyModPreset(modPresetNames[selectedModPreset]);
            if (GUI.Button(new Rect(pbx + 2f * (pbw + 4f), y - 1f, pbw, 22f), new GUIContent("Del"), buttonStyle) &&
                selectedModPreset >= 0 && selectedModPreset < modPresetNames.Count)
                DeleteModPreset(modPresetNames[selectedModPreset]);
            y += 26f;

            float presetH = 40f;
            GUI.Box(new Rect(x, y, width, presetH), "");
            if (modPresetNames.Count == 0)
            {
                GUI.Label(new Rect(x + 6f, y + 10f, width - 12f, 18f),
                    new GUIContent("No presets"), smallStyle);
            }
            else
            {
                float px = 4f;
                for (int i = 0; i < modPresetNames.Count; i++)
                {
                    bool sel = i == selectedModPreset;
                    Color pb = GUI.backgroundColor;
                    if (sel) GUI.backgroundColor = accentSel;
                    float chip = Mathf.Min(110f, (width - 12f) / Mathf.Max(1, modPresetNames.Count));
                    if (GUI.Button(new Rect(x + px, y + 6f, chip - 4f, 26f), new GUIContent(modPresetNames[i]),
                        sel && selectedButtonStyle != null ? selectedButtonStyle : buttonStyle))
                    {
                        selectedModPreset = i;
                        modPresetName = modPresetNames[i];
                    }
                    GUI.backgroundColor = pb;
                    px += chip;
                    if (px > width - 40f) break;
                }
            }
        }

        /// <summary>
        /// Load the currently selected mods via ModManager.SetLoadedMods (game's real loader).
        /// </summary>
        private void ApplySelectedMods()
        {
            if (applyModsRunning)
            {
                ShowToast("Already applying");
                return;
            }
            if (CountEnabledMods() <= 0)
            {
                ShowToast("Nothing selected");
                return;
            }
            StartCoroutine(ApplySelectedModsRoutine());
        }

        private void SetApplyModsStatus(string msg)
        {
            applyModsStatus = msg ?? "";
            applyModsStatusUntil = Time.unscaledTime + 8f;
        }

        private System.Collections.IEnumerator ApplySelectedModsRoutine()
        {
            applyModsRunning = true;
            int n = CountEnabledMods();
            SetApplyModsStatus("Loading " + n + "…");
            ShowToast("Loading " + n + "…");

            string modsErr = null;
            object setModsEnum = null;
            try
            {
                setModsEnum = BeginSetLoadedModsForSelection(out modsErr);
            }
            catch (Exception ex)
            {
                modsErr = ex.Message;
            }

            if (setModsEnum == null)
            {
                string fail = string.IsNullOrEmpty(modsErr) ? "Apply failed" : modsErr;
                SetApplyModsStatus(fail);
                ShowToast(fail);
                applyModsRunning = false;
                yield break;
            }

            IEnumerator en = setModsEnum as IEnumerator;
            bool modsOk = false;
            float waitMods = Time.unscaledTime + 180f;
            float nextStatus = 0f;
            if (en != null)
            {
                while (Time.unscaledTime < waitMods)
                {
                    if (Time.unscaledTime >= nextStatus)
                    {
                        float left = Mathf.Max(0f, waitMods - Time.unscaledTime);
                        SetApplyModsStatus("Loading " + n + "…");
                        nextStatus = Time.unscaledTime + 1f;
                    }
                    bool moved = false;
                    try { moved = en.MoveNext(); }
                    catch (Exception ex)
                    {
                        modsErr = ex.Message;
                        break;
                    }
                    if (!moved)
                    {
                        modsOk = true;
                        break;
                    }
                    yield return en.Current;
                }
            }

            if (!string.IsNullOrEmpty(modsErr))
            {
                Logger.LogWarning("Apply mods: " + modsErr);
                SetApplyModsStatus("Failed: " + modsErr);
                ShowToast("Failed");
            }
            else if (modsOk)
            {
                bool someFailed = false;
                try
                {
                    Type mm = SafeGameType("ModManager");
                    MethodInfo failed = mm != null ? AccessTools.Method(mm, "GetFailedToLoadMods") : null;
                    if (failed != null)
                    {
                        object f = failed.Invoke(null, null);
                        if (f is bool && (bool)f)
                            someFailed = true;
                    }
                }
                catch { }

                if (someFailed)
                {
                    SetApplyModsStatus("Done — some failed");
                    ShowToast("Some failed");
                }
                else
                {
                    SetApplyModsStatus("Applied (" + n + ")");
                    ShowToast("Applied");
                }
            }
            else
            {
                SetApplyModsStatus("Timed out");
                ShowToast("Timed out");
            }
            applyModsRunning = false;
        }

        private int CountEnabledMods()
        {
            int n = 0;
            for (int i = 0; i < quickLobbyModEnabled.Count; i++)
                if (quickLobbyModEnabled[i]) n++;
            return n;
        }

        private bool ModMatchesFilter(int index)
        {
            if (string.IsNullOrEmpty(quickLobbyModFilter))
                return true;
            if (index < 0 || index >= quickLobbyModTitles.Count)
                return false;
            string f = quickLobbyModFilter.Trim();
            if (f.Length == 0)
                return true;
            string title = quickLobbyModTitles[index] ?? "";
            if (title.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                return true;
            if (index < quickLobbyModFolders.Count)
            {
                string folder = quickLobbyModFolders[index] ?? "";
                if (folder.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            if (index < quickLobbyModIds.Count)
            {
                string id = quickLobbyModIds[index] ?? "";
                if (id.IndexOf(f, StringComparison.OrdinalIgnoreCase) >= 0)
                    return true;
            }
            return false;
        }

        private int CountModsMatchingFilter()
        {
            if (string.IsNullOrEmpty(quickLobbyModFilter) || quickLobbyModFilter.Trim().Length == 0)
                return quickLobbyModTitles.Count;
            int n = 0;
            for (int i = 0; i < quickLobbyModTitles.Count; i++)
                if (ModMatchesFilter(i)) n++;
            return n;
        }

        private void RefreshModListIfNeeded(bool force = false)
        {
            if (!force && Time.unscaledTime < nextModListRefresh && quickLobbyModTitles.Count > 0)
                return;
            nextModListRefresh = Time.unscaledTime + 8f;

            System.Collections.Generic.Dictionary<string, bool> prevEnabled =
                new System.Collections.Generic.Dictionary<string, bool>();
            for (int i = 0; i < quickLobbyModIds.Count; i++)
            {
                string k = quickLobbyModIds[i] + "|" + (i < quickLobbyModFolders.Count ? quickLobbyModFolders[i] : "");
                if (i < quickLobbyModEnabled.Count)
                    prevEnabled[k] = quickLobbyModEnabled[i];
            }

            quickLobbyModTitles.Clear();
            quickLobbyModIds.Clear();
            quickLobbyModFolders.Clear();
            quickLobbyModEnabled.Clear();

            // 1) ModManager reflection — every static IEnumerable that looks like ModStubs
            try
            {
                Type mm = SafeGameType("ModManager");
                if (mm != null)
                {
                    string[] methodNames = new string[]
                    {
                        "GetModsWithLoadedAssets",
                        "GetPlayerConfig",
                        "GetAllMods",
                        "GetInstalledMods",
                        "GetSubscribedMods",
                        "GetAvailableMods"
                    };
                    for (int m = 0; m < methodNames.Length; m++)
                    {
                        MethodInfo mi = AccessTools.Method(mm, methodNames[m]);
                        if (mi == null) continue;
                        object listObj = null;
                        try { listObj = mi.Invoke(null, null); } catch { continue; }
                        AddModStubsFromEnumerable(listObj, prevEnabled);
                    }

                    // Static fields / props that are lists
                    foreach (FieldInfo fi in mm.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (fi.FieldType == typeof(string) || fi.FieldType.IsPrimitive) continue;
                        try
                        {
                            object val = fi.GetValue(null);
                            AddModStubsFromEnumerable(val, prevEnabled);
                        }
                        catch { }
                    }
                    foreach (PropertyInfo pi in mm.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
                    {
                        if (!pi.CanRead || pi.GetIndexParameters().Length > 0) continue;
                        try
                        {
                            object val = pi.GetValue(null, null);
                            AddModStubsFromEnumerable(val, prevEnabled);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("RefreshModList ModManager: " + ex.Message);
            }

            // 2) Disk scan — subscribed/local mods even if not enabled in-game
            try { ScanDiskForMods(prevEnabled); }
            catch (Exception ex) { Logger.LogWarning("ScanDiskForMods: " + ex.Message); }

            // 3) Current room modList
            try
            {
                if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
                    ImportModsFromRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties, false);
            }
            catch { }

            SortModListAlphabetical();
        }

        /// <summary>
        /// Sort ALL MODS lists by title (case-insensitive), keeping id/folder/enabled in sync.
        /// </summary>
        private void SortModListAlphabetical()
        {
            int n = quickLobbyModTitles.Count;
            if (n <= 1)
                return;

            // Parallel arrays → list of indices, sort by title, reorder all four lists
            int[] order = new int[n];
            for (int i = 0; i < n; i++)
                order[i] = i;

            System.Array.Sort(order, (a, b) =>
            {
                string ta = a < quickLobbyModTitles.Count ? (quickLobbyModTitles[a] ?? "") : "";
                string tb = b < quickLobbyModTitles.Count ? (quickLobbyModTitles[b] ?? "") : "";
                return string.Compare(ta, tb, StringComparison.OrdinalIgnoreCase);
            });

            System.Collections.Generic.List<string> titles = new System.Collections.Generic.List<string>(n);
            System.Collections.Generic.List<string> ids = new System.Collections.Generic.List<string>(n);
            System.Collections.Generic.List<string> folders = new System.Collections.Generic.List<string>(n);
            System.Collections.Generic.List<bool> enabled = new System.Collections.Generic.List<bool>(n);

            for (int i = 0; i < n; i++)
            {
                int src = order[i];
                titles.Add(src < quickLobbyModTitles.Count ? quickLobbyModTitles[src] : "");
                ids.Add(src < quickLobbyModIds.Count ? quickLobbyModIds[src] : "0");
                folders.Add(src < quickLobbyModFolders.Count ? quickLobbyModFolders[src] : "");
                enabled.Add(src < quickLobbyModEnabled.Count && quickLobbyModEnabled[src]);
            }

            quickLobbyModTitles.Clear();
            quickLobbyModIds.Clear();
            quickLobbyModFolders.Clear();
            quickLobbyModEnabled.Clear();
            quickLobbyModTitles.AddRange(titles);
            quickLobbyModIds.AddRange(ids);
            quickLobbyModFolders.AddRange(folders);
            quickLobbyModEnabled.AddRange(enabled);
        }

        private void AddModStubsFromEnumerable(object listObj,
            System.Collections.Generic.Dictionary<string, bool> prevEnabled)
        {
            if (listObj == null) return;
            System.Collections.IEnumerable en = listObj as System.Collections.IEnumerable;
            if (en == null) return;
            // Avoid iterating strings char-by-char
            if (listObj is string) return;
            foreach (object stub in en)
            {
                if (stub == null) continue;
                // Nested enumerables (dict values)
                if (stub is System.Collections.IEnumerable && !(stub is string))
                {
                    Type st = stub.GetType();
                    if (!st.IsValueType && st != typeof(string) && AccessTools.Field(st, "title") == null
                        && AccessTools.Field(st, "Title") == null && AccessTools.Field(st, "folderTitle") == null)
                    {
                        // might be a list of stubs
                        if (st.Name.IndexOf("ModStub", StringComparison.OrdinalIgnoreCase) < 0
                            && st.Name.IndexOf("KeyValue", StringComparison.OrdinalIgnoreCase) < 0)
                        {
                            try { AddModStubsFromEnumerable(stub, prevEnabled); } catch { }
                            continue;
                        }
                    }
                }
                TryAddModStub(stub, prevEnabled);
            }
        }

        private void ScanDiskForMods(
            System.Collections.Generic.Dictionary<string, bool> prevEnabled)
        {
            System.Collections.Generic.List<string> roots = new System.Collections.Generic.List<string>();

            try
            {
                string persistent = Application.persistentDataPath;
                if (!string.IsNullOrEmpty(persistent))
                {
                    roots.Add(Path.Combine(persistent, "mods"));
                    // parent user folders: .../KoboldKare/<id>/mods
                    DirectoryInfo pd = new DirectoryInfo(persistent);
                    if (pd.Parent != null)
                    {
                        foreach (DirectoryInfo sub in pd.Parent.GetDirectories())
                        {
                            roots.Add(Path.Combine(sub.FullName, "mods"));
                        }
                    }
                }
            }
            catch { }

            // LocalLow\Naelstrof\KoboldKare
            try
            {
                string localLow = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    "AppData", "LocalLow", "Naelstrof", "KoboldKare");
                if (Directory.Exists(localLow))
                {
                    roots.Add(Path.Combine(localLow, "mods"));
                    foreach (string dir in Directory.GetDirectories(localLow))
                        roots.Add(Path.Combine(dir, "mods"));
                }
            }
            catch { }

            // Steam workshop content/1102930
            try
            {
                string[] steamRoots = new string[]
                {
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "Steam"),
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Steam"),
                    @"C:\Program Files (x86)\Steam",
                    @"C:\Program Files\Steam",
                    @"D:\Steam",
                    @"D:\SteamLibrary",
                    @"E:\SteamLibrary"
                };
                for (int i = 0; i < steamRoots.Length; i++)
                {
                    if (string.IsNullOrEmpty(steamRoots[i])) continue;
                    string ws = Path.Combine(steamRoots[i], "steamapps", "workshop", "content", "1102930");
                    roots.Add(ws);
                    // libraryfolders.vdf sibling workshop paths
                    string steamapps = Path.Combine(steamRoots[i], "steamapps");
                    if (Directory.Exists(steamapps))
                    {
                        foreach (string lib in Directory.GetDirectories(steamapps, "appmanifest_*"))
                        { }
                    }
                }
                // Parse libraryfolders.vdf for extra libraries
                try
                {
                    string vdf = Path.Combine(
                        Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                        "Steam", "steamapps", "libraryfolders.vdf");
                    if (!File.Exists(vdf))
                        vdf = Path.Combine(@"C:\Program Files (x86)\Steam", "steamapps", "libraryfolders.vdf");
                    if (File.Exists(vdf))
                    {
                        foreach (string line in File.ReadAllLines(vdf))
                        {
                            // "path"		"D:\\SteamLibrary"
                            int p = line.IndexOf("\"path\"", StringComparison.OrdinalIgnoreCase);
                            if (p < 0) continue;
                            int q1 = line.IndexOf('"', p + 6);
                            if (q1 < 0) continue;
                            int q2 = line.IndexOf('"', q1 + 1);
                            if (q2 < 0) continue;
                            string libPath = line.Substring(q1 + 1, q2 - q1 - 1).Replace(@"\\", @"\");
                            roots.Add(Path.Combine(libPath, "steamapps", "workshop", "content", "1102930"));
                        }
                    }
                }
                catch { }
            }
            catch { }

            System.Collections.Generic.HashSet<string> seenFolders =
                new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);

            for (int r = 0; r < roots.Count; r++)
            {
                string root = roots[r];
                if (string.IsNullOrEmpty(root) || !Directory.Exists(root)) continue;
                string[] dirs;
                try { dirs = Directory.GetDirectories(root); }
                catch { continue; }

                for (int d = 0; d < dirs.Length; d++)
                {
                    string dir = dirs[d];
                    if (!seenFolders.Add(dir)) continue;

                    string folderName = Path.GetFileName(dir);
                    string title = folderName;
                    string idStr = folderName;

                    // workshop folders are numeric ids
                    bool numericId = true;
                    for (int c = 0; c < folderName.Length; c++)
                    {
                        if (!char.IsDigit(folderName[c])) { numericId = false; break; }
                    }

                    // Try read title from common metadata files
                    string[] metaNames = new string[] { "mod.json", "info.json", "package.json", "modinfo.json", "workshop.json" };
                    for (int m = 0; m < metaNames.Length; m++)
                    {
                        string metaPath = Path.Combine(dir, metaNames[m]);
                        if (!File.Exists(metaPath)) continue;
                        try
                        {
                            string json = File.ReadAllText(metaPath);
                            string t = ExtractJsonString(json, "title")
                                ?? ExtractJsonString(json, "name")
                                ?? ExtractJsonString(json, "Name");
                            if (!string.IsNullOrEmpty(t)) title = t;
                            string id = ExtractJsonString(json, "id")
                                ?? ExtractJsonString(json, "workshopId");
                            if (!string.IsNullOrEmpty(id)) idStr = id;
                        }
                        catch { }
                        break;
                    }

                    // Build a fake stub-like add via TryAddModFromDisk
                    TryAddModFromDisk(title, idStr, folderName, prevEnabled);
                }
            }
        }

        private void TryAddModFromDisk(string title, string idStr, string folder,
            System.Collections.Generic.Dictionary<string, bool> prevEnabled)
        {
            string idKey = string.IsNullOrEmpty(idStr) ? "0" : idStr;
            for (int i = 0; i < quickLobbyModIds.Count; i++)
            {
                if (quickLobbyModIds[i] == idKey &&
                    (i < quickLobbyModFolders.Count ? quickLobbyModFolders[i] : "") == (folder ?? ""))
                    return;
            }

            string key = idKey + "|" + (folder ?? "");
            bool en = false; // disk-found defaults OFF until user selects (not necessarily enabled in game)
            if (prevEnabled != null && prevEnabled.ContainsKey(key))
                en = prevEnabled[key];

            quickLobbyModTitles.Add(!string.IsNullOrEmpty(title) ? title : (folder ?? idKey));
            quickLobbyModIds.Add(idKey);
            quickLobbyModFolders.Add(folder ?? "");
            quickLobbyModEnabled.Add(en);
        }

        private void TryAddModStub(object stub, System.Collections.Generic.Dictionary<string, bool> prevEnabled)
        {
            if (stub == null) return;
            string title = null, folder = null, idStr = null;
            try
            {
                Type st = stub.GetType();
                // Try common field/property names (ModStub is often a struct)
                string[] titleNames = new string[] { "title", "Title", "name", "Name" };
                string[] folderNames = new string[] { "folderTitle", "FolderTitle", "folder", "Folder" };
                string[] idNames = new string[] { "id", "Id", "workshopId", "WorkshopId", "fileId" };

                for (int i = 0; i < titleNames.Length && string.IsNullOrEmpty(title); i++)
                {
                    FieldInfo f = AccessTools.Field(st, titleNames[i]);
                    if (f != null) title = f.GetValue(stub) as string;
                    if (string.IsNullOrEmpty(title))
                    {
                        PropertyInfo p = AccessTools.Property(st, titleNames[i]);
                        if (p != null) title = p.GetValue(stub, null) as string;
                    }
                }
                for (int i = 0; i < folderNames.Length && string.IsNullOrEmpty(folder); i++)
                {
                    FieldInfo f = AccessTools.Field(st, folderNames[i]);
                    if (f != null) folder = f.GetValue(stub) as string;
                    if (string.IsNullOrEmpty(folder))
                    {
                        PropertyInfo p = AccessTools.Property(st, folderNames[i]);
                        if (p != null) folder = p.GetValue(stub, null) as string;
                    }
                }
                for (int i = 0; i < idNames.Length && string.IsNullOrEmpty(idStr); i++)
                {
                    FieldInfo f = AccessTools.Field(st, idNames[i]);
                    object idVal = f != null ? f.GetValue(stub) : null;
                    if (idVal == null)
                    {
                        PropertyInfo p = AccessTools.Property(st, idNames[i]);
                        if (p != null) idVal = p.GetValue(stub, null);
                    }
                    if (idVal != null) idStr = idVal.ToString();
                }

                // Public instance fields via reflection scan
                if (string.IsNullOrEmpty(title) || string.IsNullOrEmpty(idStr))
                {
                    foreach (FieldInfo f in st.GetFields(BindingFlags.Public | BindingFlags.Instance))
                    {
                        object v = f.GetValue(stub);
                        if (v == null) continue;
                        string fn = f.Name.ToLowerInvariant();
                        if (string.IsNullOrEmpty(title) && v is string && (fn.Contains("title") || fn == "name"))
                            title = (string)v;
                        else if (string.IsNullOrEmpty(folder) && v is string && fn.Contains("folder"))
                            folder = (string)v;
                        else if (string.IsNullOrEmpty(idStr) && (fn == "id" || fn.Contains("workshop") || fn.Contains("fileid")))
                            idStr = v.ToString();
                    }
                }
            }
            catch { }

            if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(folder) && string.IsNullOrEmpty(idStr))
                return;

            // De-dupe
            string idKey = idStr ?? "0";
            for (int i = 0; i < quickLobbyModIds.Count; i++)
            {
                if (quickLobbyModIds[i] == idKey &&
                    (i < quickLobbyModFolders.Count ? quickLobbyModFolders[i] : "") == (folder ?? ""))
                    return;
            }

            string key = idKey + "|" + (folder ?? "");
            bool en = true;
            if (prevEnabled != null && prevEnabled.ContainsKey(key))
                en = prevEnabled[key];

            quickLobbyModTitles.Add(!string.IsNullOrEmpty(title) ? title : (folder ?? idKey));
            quickLobbyModIds.Add(idKey);
            quickLobbyModFolders.Add(folder ?? "");
            quickLobbyModEnabled.Add(en);
        }

        /// <summary>
        /// Parse room CustomProperties["modList"] JSON into the quick-lobby mod lists.
        /// </summary>
        private void ImportModsFromRoomProperties(ExitGames.Client.Photon.Hashtable props, bool selectAll)
        {
            if (props == null || !props.ContainsKey("modList"))
                return;
            object raw = props["modList"];
            string json = raw as string;
            if (string.IsNullOrEmpty(json))
                return;

            // Minimal array-of-objects parse: {"id":"...","folderTitle":"...","title":"..."}
            int pos = 0;
            while (pos < json.Length)
            {
                int objStart = json.IndexOf('{', pos);
                if (objStart < 0) break;
                int objEnd = json.IndexOf('}', objStart);
                if (objEnd < 0) break;
                string obj = json.Substring(objStart, objEnd - objStart + 1);
                pos = objEnd + 1;

                string id = ExtractJsonString(obj, "id");
                string folder = ExtractJsonString(obj, "folderTitle");
                string title = ExtractJsonString(obj, "title");
                if (string.IsNullOrEmpty(title) && string.IsNullOrEmpty(folder) && string.IsNullOrEmpty(id))
                    continue;

                // de-dupe
                bool exists = false;
                for (int i = 0; i < quickLobbyModIds.Count; i++)
                {
                    if (quickLobbyModIds[i] == (id ?? "0") &&
                        quickLobbyModFolders[i] == (folder ?? ""))
                    {
                        if (selectAll) quickLobbyModEnabled[i] = true;
                        exists = true;
                        break;
                    }
                }
                if (exists) continue;

                quickLobbyModTitles.Add(!string.IsNullOrEmpty(title) ? title : (folder ?? id ?? "?"));
                quickLobbyModIds.Add(id ?? "0");
                quickLobbyModFolders.Add(folder ?? "");
                quickLobbyModEnabled.Add(selectAll || true);
            }

            SortModListAlphabetical();
        }

        private static string ExtractJsonString(string obj, string key)
        {
            string pattern = "\"" + key + "\"";
            int k = obj.IndexOf(pattern, StringComparison.OrdinalIgnoreCase);
            if (k < 0) return null;
            int colon = obj.IndexOf(':', k + pattern.Length);
            if (colon < 0) return null;
            int q1 = obj.IndexOf('"', colon + 1);
            if (q1 < 0) return null;
            int q2 = obj.IndexOf('"', q1 + 1);
            if (q2 < 0) return null;
            return obj.Substring(q1 + 1, q2 - q1 - 1);
        }

        private void CopyModsFromCurrentRoom()
        {
            if (!PhotonNetwork.InRoom || PhotonNetwork.CurrentRoom == null)
            {
                ShowToast("Join a room first to copy its mods");
                return;
            }
            try
            {
                int before = quickLobbyModTitles.Count;
                ImportModsFromRoomProperties(PhotonNetwork.CurrentRoom.CustomProperties, true);
                // Select all that came from room
                for (int i = 0; i < quickLobbyModEnabled.Count; i++)
                    quickLobbyModEnabled[i] = true;
                int n = CountEnabledMods();
                ShowToast(n > 0
                    ? ("Copied " + n + " mod(s) from room → lobby maker")
                    : "Room has no modList property");
                Logger.LogInfo("CopyModsFromCurrentRoom: " + n + " mods (was " + before + ")");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("CopyModsFromCurrentRoom: " + ex.Message);
                ShowToast("Copy failed: " + ex.Message);
            }
        }

        private void LoadModPresetsFromConfig()
        {
            modPresetNames.Clear();
            if (configModPresets == null || string.IsNullOrEmpty(configModPresets.Value))
                return;
            string[] parts = configModPresets.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) continue;
                string name = parts[i].Substring(0, eq).Trim();
                if (!string.IsNullOrEmpty(name) && !modPresetNames.Contains(name))
                    modPresetNames.Add(name);
            }
        }

        private string GetModPresetPayload(string name)
        {
            if (configModPresets == null || string.IsNullOrEmpty(name)) return null;
            string[] parts = configModPresets.Value.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) continue;
                if (string.Equals(parts[i].Substring(0, eq).Trim(), name, StringComparison.OrdinalIgnoreCase))
                    return parts[i].Substring(eq + 1);
            }
            return null;
        }

        private void SaveCurrentModsAsPreset(string name)
        {
            if (string.IsNullOrEmpty(name) || configModPresets == null)
            {
                ShowToast("Need a name");
                return;
            }
            name = name.Trim().Replace("=", "").Replace(";", "");
            string json = BuildModListJson();
            // rewrite config
            System.Collections.Generic.List<string> entries = new System.Collections.Generic.List<string>();
            string[] parts = (configModPresets.Value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            bool replaced = false;
            for (int i = 0; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) continue;
                string n = parts[i].Substring(0, eq).Trim();
                if (string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                {
                    entries.Add(name + "=" + json);
                    replaced = true;
                }
                else
                    entries.Add(parts[i]);
            }
            if (!replaced)
                entries.Add(name + "=" + json);
            configModPresets.Value = string.Join(";", entries.ToArray());
            LoadModPresetsFromConfig();
            ShowToast("Saved: " + name);
        }

        private void ApplyModPreset(string name)
        {
            string payload = GetModPresetPayload(name);
            if (string.IsNullOrEmpty(payload))
            {
                ShowToast("Not found");
                return;
            }
            // Clear selection then import JSON as if room property
            for (int i = 0; i < quickLobbyModEnabled.Count; i++)
                quickLobbyModEnabled[i] = false;
            ExitGames.Client.Photon.Hashtable fake = new ExitGames.Client.Photon.Hashtable();
            fake["modList"] = payload;
            ImportModsFromRoomProperties(fake, true);
            // enable only those in payload
            ShowToast("Loaded: " + name);
        }

        private void DeleteModPreset(string name)
        {
            if (configModPresets == null || string.IsNullOrEmpty(name)) return;
            System.Collections.Generic.List<string> entries = new System.Collections.Generic.List<string>();
            string[] parts = (configModPresets.Value ?? "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < parts.Length; i++)
            {
                int eq = parts[i].IndexOf('=');
                if (eq <= 0) continue;
                string n = parts[i].Substring(0, eq).Trim();
                if (!string.Equals(n, name, StringComparison.OrdinalIgnoreCase))
                    entries.Add(parts[i]);
            }
            configModPresets.Value = string.Join(";", entries.ToArray());
            LoadModPresetsFromConfig();
            selectedModPreset = -1;
            ShowToast("Deleted: " + name);
        }

        private string BuildModListJson()
        {
            // NetworkManager.TryParseMods expects JSON array of { id, folderTitle, title }
            // Never advertise map packs here — joiners treat modList as Workshop content to download.
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.Append('[');
            bool first = true;
            for (int i = 0; i < quickLobbyModTitles.Count; i++)
            {
                if (i >= quickLobbyModEnabled.Count || !quickLobbyModEnabled[i])
                    continue;
                string id = i < quickLobbyModIds.Count ? quickLobbyModIds[i] : "0";
                string folder = i < quickLobbyModFolders.Count ? quickLobbyModFolders[i] : "";
                string title = quickLobbyModTitles[i];
                if (!first) sb.Append(',');
                first = false;
                sb.Append("{\"id\":\"");
                sb.Append(EscapeJson(id));
                sb.Append("\",\"folderTitle\":\"");
                sb.Append(EscapeJson(folder));
                sb.Append("\",\"title\":\"");
                sb.Append(EscapeJson(title));
                sb.Append("\"}");
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static string EscapeJson(string s)
        {
            if (string.IsNullOrEmpty(s)) return "";
            return s.Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private object BeginSetLoadedModsForSelection(out string error)
        {
            error = null;
            Type mm = SafeGameType("ModManager");
            if (mm == null)
            {
                error = "ModManager type missing";
                return null;
            }

            MethodInfo setLoaded = AccessTools.Method(mm, "SetLoadedMods");
            if (setLoaded == null)
            {
                error = "SetLoadedMods missing";
                return null;
            }

            // Nested ModStub type
            Type stubType = mm.GetNestedType("ModStub", BindingFlags.Public | BindingFlags.NonPublic);
            if (stubType == null)
                stubType = SafeGameType("ModManager+ModStub");
            if (stubType == null)
            {
                // Search all nested
                foreach (Type nt in mm.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic))
                {
                    if (nt.Name.IndexOf("ModStub", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        stubType = nt;
                        break;
                    }
                }
            }
            if (stubType == null)
            {
                error = "ModStub type missing";
                return null;
            }

            // List<ModStub>
            Type listType = typeof(System.Collections.Generic.List<>).MakeGenericType(stubType);
            object list = Activator.CreateInstance(listType);
            MethodInfo listAdd = listType.GetMethod("Add");

            Type publishedIdType = null;
            try { publishedIdType = AccessTools.TypeByName("Steamworks.PublishedFileId_t"); } catch { }
            if (publishedIdType == null)
            {
                foreach (Assembly asm in AppDomain.CurrentDomain.GetAssemblies())
                {
                    try
                    {
                        Type t = asm.GetType("Steamworks.PublishedFileId_t");
                        if (t != null) { publishedIdType = t; break; }
                    }
                    catch { }
                }
            }

            // ModSource enum (prefer Any)
            Type modSourceType = mm.GetNestedType("ModSource", BindingFlags.Public | BindingFlags.NonPublic);
            object modSourceAny = null;
            if (modSourceType != null && modSourceType.IsEnum)
            {
                try { modSourceAny = Enum.Parse(modSourceType, "Any"); }
                catch
                {
                    try { modSourceAny = Enum.GetValues(modSourceType).GetValue(0); } catch { }
                }
            }

            ConstructorInfo bestCtor = null;
            ConstructorInfo[] ctors = stubType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            // Prefer (string title, PublishedFileId_t id, ModSource source, string folderTitle)
            for (int c = 0; c < ctors.Length; c++)
            {
                ParameterInfo[] ps = ctors[c].GetParameters();
                if (ps.Length >= 2)
                {
                    bestCtor = ctors[c];
                    if (ps.Length == 4) break;
                }
            }

            int added = 0;
            for (int i = 0; i < quickLobbyModTitles.Count; i++)
            {
                if (i >= quickLobbyModEnabled.Count || !quickLobbyModEnabled[i])
                    continue;

                string title = quickLobbyModTitles[i];
                string folder = i < quickLobbyModFolders.Count ? quickLobbyModFolders[i] : "";
                string idStr = i < quickLobbyModIds.Count ? quickLobbyModIds[i] : "0";
                ulong idNum = 0;
                ulong.TryParse(idStr, out idNum);

                object stub = null;
                try
                {
                    if (bestCtor != null)
                    {
                        ParameterInfo[] ps = bestCtor.GetParameters();
                        object[] args = new object[ps.Length];
                        for (int p = 0; p < ps.Length; p++)
                        {
                            Type pt = ps[p].ParameterType;
                            if (pt == typeof(string))
                            {
                                // first string = title, later = folder
                                if (p == 0) args[p] = title ?? "";
                                else args[p] = folder ?? title ?? "";
                            }
                            else if (publishedIdType != null && pt == publishedIdType)
                            {
                                args[p] = Activator.CreateInstance(publishedIdType, new object[] { idNum });
                            }
                            else if (pt.IsEnum || (modSourceType != null && pt == modSourceType))
                            {
                                args[p] = modSourceAny ?? Activator.CreateInstance(pt);
                            }
                            else if (pt == typeof(ulong) || pt == typeof(long))
                            {
                                args[p] = idNum;
                            }
                            else if (pt == typeof(uint) || pt == typeof(int))
                            {
                                args[p] = (int)idNum;
                            }
                            else
                            {
                                args[p] = pt.IsValueType ? Activator.CreateInstance(pt) : null;
                            }
                        }
                        stub = bestCtor.Invoke(args);
                    }
                    else
                    {
                        stub = Activator.CreateInstance(stubType);
                        // try set fields
                        foreach (FieldInfo f in stubType.GetFields(BindingFlags.Public | BindingFlags.Instance))
                        {
                            string fn = f.Name.ToLowerInvariant();
                            if (fn.Contains("title") && f.FieldType == typeof(string) && !fn.Contains("folder"))
                                f.SetValue(stub, title);
                            else if (fn.Contains("folder") && f.FieldType == typeof(string))
                                f.SetValue(stub, folder);
                            else if ((fn == "id" || fn.Contains("workshop")) && publishedIdType != null && f.FieldType == publishedIdType)
                                f.SetValue(stub, Activator.CreateInstance(publishedIdType, new object[] { idNum }));
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Build ModStub " + title + ": " + ex.Message);
                }

                if (stub != null)
                {
                    listAdd.Invoke(list, new object[] { stub });
                    added++;
                }
            }

            if (added == 0)
            {
                error = "No ModStub instances could be built";
                return null;
            }

            // SetLoadedMods(List<ModStub>) or SetLoadedMods(IEnumerable)
            object result = null;
            try
            {
                result = setLoaded.Invoke(null, new object[] { list });
            }
            catch (Exception ex)
            {
                // try instance method on ModManager.instance
                try
                {
                    object mmInst = null;
                    PropertyInfo ip = AccessTools.Property(mm, "instance") ?? AccessTools.Property(mm, "Instance");
                    if (ip != null) mmInst = ip.GetValue(null, null);
                    if (mmInst != null)
                        result = setLoaded.Invoke(mmInst, new object[] { list });
                    else
                        throw ex;
                }
                catch (Exception ex2)
                {
                    error = "SetLoadedMods invoke: " + ex2.Message;
                    return null;
                }
            }

            Logger.LogInfo("SetLoadedMods started with " + added + " mod(s)");
            return result; // IEnumerator / IEnumerable coroutine
        }

        private int PurgeOrphanKobolds()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
            {
                ShowToast("Must be room host to purge orphans");
                return 0;
            }

            try
            {
                System.Collections.Generic.HashSet<int> liveActors =
                    new System.Collections.Generic.HashSet<int>();
                Player[] players = PhotonNetwork.PlayerList;
                if (players != null)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        if (players[i] != null)
                            liveActors.Add(players[i].ActorNumber);
                    }
                }

                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views == null)
                    return 0;

                int destroyed = 0;
                for (int i = 0; i < views.Length; i++)
                {
                    PhotonView view = views[i];
                    if (view == null || view.gameObject == null) continue;
                    if (GetKoboldOn(view.gameObject) == null && !IsValidPlayerKoboldObject(view.gameObject))
                        continue;

                    int ownerActor = 0;
                    try
                    {
                        if (view.Owner != null)
                            ownerActor = view.Owner.ActorNumber;
                        else if (view.OwnerActorNr > 0)
                            ownerActor = view.OwnerActorNr;
                        else if (view.CreatorActorNr > 0)
                            ownerActor = view.CreatorActorNr;
                    }
                    catch { }

                    // Orphan: no owner, or owner not in room
                    bool orphan = ownerActor <= 0 || !liveActors.Contains(ownerActor);
                    if (!orphan) continue;

                    try
                    {
                        Logger.LogInfo("Purge orphan kobold " + view.gameObject.name +
                                       " ownerActor=" + ownerActor + " view=" + view.ViewID);
                        PhotonNetwork.Destroy(view.gameObject);
                        destroyed++;
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Purge orphan destroy: " + ex.Message);
                    }
                }

                return destroyed;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PurgeOrphanKobolds: " + ex.Message);
                ShowToast("Purge failed: " + ex.Message);
                return 0;
            }
        }

        private void LeaveCurrentRoom()
        {
            if (!PhotonNetwork.InRoom)
                return;
            LeaveRoomSafe();
        }

        /// <summary>
        /// Leave the current Photon room. Optionally destroys local body first (QoL toggle).
        /// </summary>
        private void LeaveRoomSafe()
        {
            if (!PhotonNetwork.InRoom)
                return;

            if (destroyBodyOnLeave)
            {
                ShowToast("Leaving… destroying body first");
                // Destroy body first, wait a couple frames so Photon can send the destroy, then leave
                StartCoroutine(LeaveRoomAfterDestroyBodyRoutine());
                return;
            }

            try
            {
                PhotonNetwork.LeaveRoom();
                ShowToast("Left room");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("LeaveRoomSafe: " + ex.Message);
                ShowToast("Leave failed: " + ex.Message);
            }
        }

        private System.Collections.IEnumerator LeaveRoomAfterDestroyBodyRoutine()
        {
            try { DestroyLocalPlayerBodyForBrowse(); }
            catch (Exception ex) { Logger.LogWarning("LeaveRoom destroy body: " + ex.Message); }

            // Let Photon flush destroy messages while still InRoom
            yield return null;
            yield return null;
            yield return new WaitForSecondsRealtime(0.15f);

            // Second pass in case game respawned / delayed views
            try { DestroyLocalPlayerBodyForBrowse(); }
            catch { }

            yield return null;

            if (!PhotonNetwork.InRoom)
            {
                ShowToast("Left room (body destroyed)");
                yield break;
            }

            try
            {
                PhotonNetwork.LeaveRoom();
                ShowToast("Left room (body destroyed)");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("LeaveRoom after destroy: " + ex.Message);
                ShowToast("Leave failed: " + ex.Message);
            }
        }

        /// <summary>
        /// Community workaround: splashing water on a desynced kobold often resyncs
        /// visuals/physics after join. Spawns water near the target + soft body nudge.
        /// </summary>
        /// <summary>
        /// Auto water splash:
        ///  - You join a room  → splash everyone once, one toast "Splashed all".
        ///  - Someone else joins → splash everyone except the new player, one toast.
        /// Uses CharCon-style FluidProjectile + ScriptableReagent "Water" via reflection.
        /// </summary>
        private Coroutine autoSplashCoroutine;

        private void ScheduleSplashEveryoneOnJoin()
        {
            if (!autoSplashOnJoin || !PhotonNetwork.InRoom)
                return;
            if (Time.unscaledTime < nextAutoSplashAllowed)
                return;
            nextAutoSplashAllowed = Time.unscaledTime + 1.5f;
            StartAutoSplashRoom(excludeActorId: -1, waitSeconds: 0.75f);
        }

        private void ScheduleSplashNewPlayer(Player newPlayer)
        {
            if (!autoSplashOnJoin || newPlayer == null || newPlayer.IsLocal)
                return;
            if (!PhotonNetwork.InRoom)
                return;
            // Splash the whole room except the person who just joined
            StartAutoSplashRoom(excludeActorId: newPlayer.ActorNumber, waitSeconds: 0.6f);
        }

        private void StartAutoSplashRoom(int excludeActorId, float waitSeconds)
        {
            if (autoSplashCoroutine != null)
            {
                try { StopCoroutine(autoSplashCoroutine); } catch { }
                autoSplashCoroutine = null;
            }
            autoSplashCoroutine = StartCoroutine(AutoSplashRoomRoutine(excludeActorId, waitSeconds));
        }

        private System.Collections.IEnumerator AutoSplashRoomRoutine(int excludeActorId, float waitSeconds)
        {
            // Auto paths gate on autoSplashOnJoin upstream; manual Splash all always runs.
            if (waitSeconds > 0f)
                yield return new WaitForSecondsRealtime(waitSeconds);
            if (!PhotonNetwork.InRoom)
            {
                autoSplashCoroutine = null;
                yield break;
            }

            // Wait-for-bodies: poll until at least one splashable body exists, or 5s timeout
            const float bodyTimeout = 5f;
            float bodyDeadline = Time.unscaledTime + bodyTimeout;
            while (Time.unscaledTime < bodyDeadline)
            {
                if (!PhotonNetwork.InRoom)
                {
                    autoSplashCoroutine = null;
                    yield break;
                }
                int ready = 0;
                Player[] waitList = PhotonNetwork.PlayerList;
                if (waitList != null)
                {
                    for (int i = 0; i < waitList.Length; i++)
                    {
                        Player p = waitList[i];
                        if (p == null) continue;
                        if (excludeActorId > 0 && p.ActorNumber == excludeActorId)
                            continue;
                        if (p.IsLocal || FindPlayerObject(p) != null)
                            ready++;
                    }
                }
                if (ready > 0)
                    break;
                yield return new WaitForSecondsRealtime(0.2f);
            }

            int totalShots = 0;
            int targetsHit = 0;
            Player[] players = PhotonNetwork.PlayerList;
            if (players != null)
            {
                for (int i = 0; i < players.Length; i++)
                {
                    Player p = players[i];
                    if (p == null) continue;
                    if (excludeActorId > 0 && p.ActorNumber == excludeActorId)
                        continue;

                    int shots = 0;
                    for (int attempt = 0; attempt < 5 && shots == 0; attempt++)
                    {
                        if (FindPlayerObject(p) == null && !p.IsLocal)
                        {
                            yield return new WaitForSecondsRealtime(0.15f);
                            continue;
                        }
                        shots = SplashPlayerWithWater(p);
                        if (shots == 0)
                            yield return new WaitForSecondsRealtime(0.1f);
                    }

                    if (shots > 0)
                    {
                        totalShots += shots;
                        targetsHit++;
                    }
                    yield return null;
                }
            }

            if (targetsHit > 0)
            {
                autoSplashStatus = "Splashed all";
                ShowToast("Splashed all");
            }
            else
            {
                autoSplashStatus = "Splash: no targets";
            }
            autoSplashStatusUntil = Time.unscaledTime + 4f;
            Logger.LogInfo("Auto splash: targets=" + targetsHit + " shots=" + totalShots +
                           " exclude=" + excludeActorId);
            autoSplashCoroutine = null;
        }

        /// <summary>
        /// Fire a few Water FluidProjectiles at a player's body (CharCon fluid pistol path).
        /// </summary>
        private int SplashPlayerWithWater(Player target)
        {
            if (target == null || !PhotonNetwork.InRoom)
                return 0;

            try
            {
                GameObject bodyGo = target.IsLocal
                    ? (ResolveLocalPlayerBody() ?? GetLocalPlayer())
                    : FindPlayerObject(target);
                if (bodyGo == null)
                    return 0;

                Component kob = GetKoboldOn(bodyGo);
                if (kob == null)
                {
                    Type kt = SafeGameType("Kobold");
                    if (kt != null)
                        kob = bodyGo.GetComponentInChildren(kt, true);
                }

                return SpawnWaterFluidAt(bodyGo, kob, bodyGo.transform.position);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SplashPlayerWithWater: " + ex.Message);
                return 0;
            }
        }

        private int SpawnWaterFluidAt(GameObject bodyGo, Component kob, Vector3 targetPos)
        {
            int spawned = 0;
            try
            {
                Type reagentContentsType = SafeGameType("ReagentContents");
                Type scriptableReagentType = SafeGameType("ScriptableReagent");
                Type bitBufferType = SafeGameType("NetStack.Serialization.BitBuffer")
                    ?? SafeGameType("BitBuffer");
                Type halfPrecisionType = SafeGameType("NetStack.Quantization.HalfPrecision")
                    ?? SafeGameType("HalfPrecision");
                Type projectileType = SafeGameType("Projectile");

                if (reagentContentsType == null)
                {
                    Logger.LogWarning("Auto splash: ReagentContents type missing");
                    return 0;
                }
                if (bitBufferType == null)
                {
                    Logger.LogWarning("Auto splash: BitBuffer type missing");
                    return 0;
                }

                object waterReagent = ResolveWaterReagent(scriptableReagentType);
                if (waterReagent == null)
                {
                    Logger.LogWarning("Auto splash: Water reagent not found");
                    return TryInjectWaterFallback(kob, bodyGo);
                }

                MethodInfo setMaxVolume = AccessTools.Method(reagentContentsType, "SetMaxVolume", new Type[] { typeof(float) });
                MethodInfo addReagentContents = FindBitBufferWriteMethod(bitBufferType, "AddReagentContents");
                MethodInfo addUShort = AccessTools.Method(bitBufferType, "AddUShort", new Type[] { typeof(ushort) });
                MethodInfo quantize = halfPrecisionType != null
                    ? AccessTools.Method(halfPrecisionType, "Quantize", new Type[] { typeof(float) })
                    : null;

                // Build ReagentContents with Water using whatever AddMix overload exists
                // Tiny volume so splash triggers systems without filling belly
                System.Func<object> buildContents = () => BuildWaterReagentContents(reagentContentsType, waterReagent, 0.05f, setMaxVolume);
                object testContents = buildContents();
                if (testContents == null)
                {
                    Logger.LogWarning("Auto splash: could not build Water ReagentContents (GetReagent/AddMix)");
                    return TryInjectWaterFallback(kob, bodyGo);
                }
                if (addReagentContents == null)
                {
                    Logger.LogWarning("Auto splash: AddReagentContents extension not found — inject fallback");
                    return TryInjectWaterFallback(kob, bodyGo);
                }

                Vector3 spawnPos = targetPos + Vector3.up * 1.7f;
                Rigidbody kobBody = null;
                if (kob != null)
                {
                    try
                    {
                        FieldInfo bodyField = AccessTools.Field(kob.GetType(), "body");
                        if (bodyField != null)
                            kobBody = bodyField.GetValue(kob) as Rigidbody;
                    }
                    catch { }
                }
                if (kobBody == null && bodyGo != null)
                    kobBody = bodyGo.GetComponentInChildren<Rigidbody>();

                const float volume = 0.05f; // near-zero: contact only, no belly fill
                const float force = 8f;
                const int count = 3;

                for (int i = 0; i < count; i++)
                {
                    try
                    {
                        object contents = BuildWaterReagentContents(reagentContentsType, waterReagent, volume, setMaxVolume);
                        if (contents == null)
                        {
                            Logger.LogWarning("Water fluid: build contents failed");
                            continue;
                        }

                        Vector3 aim = (targetPos + Vector3.up * 0.5f - spawnPos).normalized;
                        if (aim.sqrMagnitude < 0.01f)
                            aim = Vector3.down;
                        Vector3 velocity = aim * force + UnityEngine.Random.insideUnitSphere * 1.25f;

                        object buffer;
                        try { buffer = Activator.CreateInstance(bitBufferType, new object[] { 16 }); }
                        catch { buffer = Activator.CreateInstance(bitBufferType); }
                        if (buffer == null) continue;

                        // Extension methods are static (buffer, contents)
                        if (addReagentContents.IsStatic)
                            addReagentContents.Invoke(null, new object[] { buffer, contents });
                        else
                            addReagentContents.Invoke(buffer, new object[] { contents });

                        if (addUShort != null && quantize != null)
                        {
                            object qx = quantize.Invoke(null, new object[] { velocity.x });
                            object qy = quantize.Invoke(null, new object[] { velocity.y });
                            object qz = quantize.Invoke(null, new object[] { velocity.z });
                            ushort ux = qx is ushort ? (ushort)qx : Convert.ToUInt16(qx);
                            ushort uy = qy is ushort ? (ushort)qy : Convert.ToUInt16(qy);
                            ushort uz = qz is ushort ? (ushort)qz : Convert.ToUInt16(qz);
                            addUShort.Invoke(buffer, new object[] { ux });
                            addUShort.Invoke(buffer, new object[] { uy });
                            addUShort.Invoke(buffer, new object[] { uz });
                        }

                        Quaternion rot = Quaternion.LookRotation(velocity.sqrMagnitude > 0.01f ? velocity : Vector3.down);
                        Vector3 pos = spawnPos + UnityEngine.Random.insideUnitSphere * 0.1f;
                        GameObject obj = null;
                        string[] prefabNames =
                        {
                            "FluidProjectile",
                            "bucketSplashProjectile",
                            "BucketSplashProjectile",
                            "projectileBlob"
                        };
                        string usedName = null;
                        for (int pn = 0; pn < prefabNames.Length && obj == null; pn++)
                        {
                            try
                            {
                                obj = PhotonNetwork.Instantiate(
                                    prefabNames[pn],
                                    pos,
                                    rot,
                                    0,
                                    new object[] { buffer });
                                if (obj != null)
                                    usedName = prefabNames[pn];
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning("Instantiate " + prefabNames[pn] + ": " + ex.Message);
                            }
                        }
                        if (obj == null)
                        {
                            Logger.LogWarning("All fluid prefab Instantiates returned null");
                            continue;
                        }
                        Logger.LogInfo("Spawned fluid prefab " + usedName);
                        spawned++;
                        spawnedObjects.Add(obj);

                        if (projectileType != null && kobBody != null)
                        {
                            try
                            {
                                Component proj = obj.GetComponent(projectileType)
                                    ?? obj.GetComponentInChildren(projectileType, true);
                                if (proj != null)
                                {
                                    MethodInfo launch = AccessTools.Method(projectileType, "LaunchFrom",
                                        new Type[] { typeof(Rigidbody) });
                                    if (launch != null)
                                        launch.Invoke(proj, new object[] { kobBody });
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.LogWarning("LaunchFrom: " + ex.Message);
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("Water fluid #" + i + ": " + ex.Message);
                    }
                }

                if (spawned == 0)
                    spawned = TryInjectWaterFallback(kob, bodyGo);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SpawnWaterFluidAt: " + ex);
            }
            return spawned;
        }

        private static MethodInfo FindInstanceMethod(Type type, string name, int paramCount)
        {
            if (type == null) return null;
            MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m == null || m.Name != name) continue;
                if (m.GetParameters().Length == paramCount)
                    return m;
            }
            return null;
        }

        /// <summary>
        /// AddReagentContents is often an extension method (same pattern as KoboldGenesBitBufferExtension).
        /// </summary>
        private static MethodInfo FindBitBufferWriteMethod(Type bitBufferType, string methodName)
        {
            if (bitBufferType == null || string.IsNullOrEmpty(methodName))
                return null;

            // KK: AddReagentContents is on ReagentContentsBitBufferExtension (static extension)
            Type preferred = SafeGameType("ReagentContentsBitBufferExtension");
            if (preferred != null)
            {
                MethodInfo pref = AccessTools.Method(preferred, methodName);
                if (pref != null)
                    return pref;
                // try all static methods with that name
                MethodInfo[] all = preferred.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < all.Length; i++)
                {
                    if (all[i] != null && all[i].Name == methodName)
                        return all[i];
                }
            }

            MethodInfo direct = AccessTools.Method(bitBufferType, methodName);
            if (direct != null)
                return direct;

            string[] guessNames =
            {
                "BitBufferReagentContentsExtension",
                "ReagentBitBufferExtension",
                "KoboldReagentBitBufferExtension",
                "BitBufferExtensions"
            };
            for (int i = 0; i < guessNames.Length; i++)
            {
                Type ext = SafeGameType(guessNames[i]);
                if (ext == null) continue;
                MethodInfo m = AccessTools.Method(ext, methodName);
                if (m != null) return m;
            }

            // Scan game assemblies for static method named methodName with BitBuffer as first arg
            try
            {
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < asms.Length; a++)
                {
                    Assembly asm = asms[a];
                    if (asm == null) continue;
                    string an = asm.GetName().Name ?? "";
                    if (an != "Assembly-CSharp" && an != "Assembly-CSharp-firstpass" &&
                        an.IndexOf("Kobold", StringComparison.OrdinalIgnoreCase) < 0 &&
                        an.IndexOf("NetStack", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;

                    Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtl) { types = rtl.Types; }
                    catch { continue; }
                    if (types == null) continue;

                    for (int t = 0; t < types.Length; t++)
                    {
                        Type ty = types[t];
                        if (ty == null) continue;
                        MethodInfo[] methods = null;
                        try
                        {
                            methods = ty.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        }
                        catch { continue; }
                        if (methods == null) continue;
                        for (int m = 0; m < methods.Length; m++)
                        {
                            MethodInfo mi = methods[m];
                            if (mi == null || mi.Name != methodName) continue;
                            ParameterInfo[] ps = mi.GetParameters();
                            if (ps == null || ps.Length < 1) continue;
                            if (ps[0].ParameterType == bitBufferType ||
                                bitBufferType.IsAssignableFrom(ps[0].ParameterType) ||
                                ps[0].ParameterType.Name == "BitBuffer")
                                return mi;
                        }
                    }
                }
            }
            catch { }

            return null;
        }

        /// <summary>
        /// Fallback: dump Water into kobold belly/container so fluid systems still run.
        /// </summary>
        private int TryInjectWaterFallback(Component kob, GameObject bodyGo)
        {
            try
            {
                if (kob == null && bodyGo != null)
                {
                    Type kt = SafeGameType("Kobold");
                    if (kt != null)
                        kob = bodyGo.GetComponentInChildren(kt, true);
                }
                if (kob == null) return 0;

                Type scriptableReagentType = SafeGameType("ScriptableReagent");
                Type reagentContentsType = SafeGameType("ReagentContents");
                object water = ResolveWaterReagent(scriptableReagentType);
                if (water == null || reagentContentsType == null) return 0;

                MethodInfo getReagentAmount = AccessTools.Method(water.GetType(), "GetReagent", new Type[] { typeof(float) });
                MethodInfo setMaxVolume = AccessTools.Method(reagentContentsType, "SetMaxVolume", new Type[] { typeof(float) });
                MethodInfo addMixOne = FindInstanceMethod(reagentContentsType, "AddMix", 1);
                if (getReagentAmount == null || addMixOne == null) return 0;

                object contents = Activator.CreateInstance(reagentContentsType, new object[] { 0.05f });
                if (setMaxVolume != null)
                    setMaxVolume.Invoke(contents, new object[] { 0.05f });
                object val = getReagentAmount.Invoke(water, new object[] { 0.05f });
                addMixOne.Invoke(contents, new object[] { val });

                // bellyContainer field on Kobold
                FieldInfo bellyField = AccessTools.Field(kob.GetType(), "bellyContainer");
                object belly = bellyField != null ? bellyField.GetValue(kob) : null;
                if (belly == null)
                {
                    // try property
                    PropertyInfo bellyProp = AccessTools.Property(kob.GetType(), "bellyContainer");
                    if (bellyProp != null)
                        belly = bellyProp.GetValue(kob, null);
                }
                if (belly == null) return 0;

                // AddMix(ReagentContents, InjectType) or ForceMixRPC
                Type injectType = SafeGameType("GenericReagentContainer+InjectType")
                    ?? SafeGameType("GenericReagentContainer.InjectType");
                // nested enum might be GenericReagentContainer.InjectType
                if (injectType == null)
                {
                    Type grc = SafeGameType("GenericReagentContainer");
                    if (grc != null)
                        injectType = grc.GetNestedType("InjectType", BindingFlags.Public | BindingFlags.NonPublic);
                }

                MethodInfo bellyAddMix = null;
                MethodInfo[] bellyMethods = belly.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                for (int i = 0; i < bellyMethods.Length; i++)
                {
                    MethodInfo m = bellyMethods[i];
                    if (m == null || m.Name != "AddMix") continue;
                    ParameterInfo[] ps = m.GetParameters();
                    if (ps != null && ps.Length == 2 && ps[0].ParameterType == reagentContentsType)
                    {
                        bellyAddMix = m;
                        break;
                    }
                }

                if (bellyAddMix != null)
                {
                    object injectVal = null;
                    if (injectType != null)
                    {
                        try { injectVal = Enum.Parse(injectType, "Inject"); }
                        catch
                        {
                            try { injectVal = Enum.ToObject(injectType, 0); }
                            catch { }
                        }
                    }
                    if (injectVal != null)
                        bellyAddMix.Invoke(belly, new object[] { contents, injectVal });
                    else
                        bellyAddMix.Invoke(belly, new object[] { contents, 0 });

                    Logger.LogInfo("Water inject fallback via bellyContainer.AddMix");
                    return 1;
                }

                // ForceMixRPC on container photon view
                PhotonView cv = null;
                try
                {
                    Component c = belly as Component;
                    if (c != null)
                        cv = c.GetComponent<PhotonView>() ?? c.GetComponentInParent<PhotonView>();
                }
                catch { }
                if (cv != null)
                {
                    Type bitBufferType = SafeGameType("NetStack.Serialization.BitBuffer") ?? SafeGameType("BitBuffer");
                    MethodInfo addReagentContents = FindBitBufferWriteMethod(bitBufferType, "AddReagentContents");
                    if (bitBufferType != null && addReagentContents != null)
                    {
                        object buffer;
                        try { buffer = Activator.CreateInstance(bitBufferType, new object[] { 16 }); }
                        catch { buffer = Activator.CreateInstance(bitBufferType); }
                        if (addReagentContents.IsStatic)
                            addReagentContents.Invoke(null, new object[] { buffer, contents });
                        else
                            addReagentContents.Invoke(buffer, new object[] { contents });

                        PhotonView sourcePv = kob.GetComponent<PhotonView>() ?? kob.GetComponentInParent<PhotonView>();
                        int sourceId = sourcePv != null ? sourcePv.ViewID : 0;
                        cv.RPC("ForceMixRPC", RpcTarget.All, buffer, sourceId, (byte)0);
                        Logger.LogInfo("Water inject fallback via ForceMixRPC");
                        return 1;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("TryInjectWaterFallback: " + ex.Message);
            }
            return 0;
        }

        /// <summary>
        /// Official KK API (github ReagentContents.cs):
        ///   new ReagentContents(float maxVolume)
        ///   AddMix(byte id, float volume)
        ///   ScriptableReagent.GetReagent(float) → Reagent { id, volume }
        ///   ReagentDatabase.GetID(ScriptableReagent)
        /// IL has no zero-arg ctor (optional param is compiler sugar).
        /// </summary>
        private object BuildWaterReagentContents(Type reagentContentsType, object waterReagent, float volume, MethodInfo setMaxVolume)
        {
            if (reagentContentsType == null || waterReagent == null)
                return null;

            try
            {
                // MUST pass float — Activator.CreateInstance() with 0 args fails
                object contents = null;
                try
                {
                    contents = Activator.CreateInstance(reagentContentsType, new object[] { volume > 0f ? volume : 20f });
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("new ReagentContents(float) failed: " + ex.Message);
                    try
                    {
                        ConstructorInfo ctor = AccessTools.Constructor(reagentContentsType, new Type[] { typeof(float) });
                        if (ctor != null)
                            contents = ctor.Invoke(new object[] { volume > 0f ? volume : 20f });
                    }
                    catch (Exception ex2)
                    {
                        Logger.LogWarning("ReagentContents ctor invoke failed: " + ex2.Message);
                    }
                }
                if (contents == null)
                    return null;

                if (setMaxVolume != null)
                {
                    try { setMaxVolume.Invoke(contents, new object[] { volume }); }
                    catch { }
                }

                // Resolve byte id for Water
                byte waterId = 0;
                bool gotId = false;

                Type reagentDb = SafeGameType("ReagentDatabase");
                if (reagentDb != null)
                {
                    MethodInfo getId = AccessTools.Method(reagentDb, "GetID", new Type[] { waterReagent.GetType() });
                    if (getId == null)
                    {
                        // try base Database.GetID
                        MethodInfo[] ms = reagentDb.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
                        for (int i = 0; i < ms.Length; i++)
                        {
                            if (ms[i] != null && ms[i].Name == "GetID" && ms[i].GetParameters().Length == 1)
                            {
                                getId = ms[i];
                                break;
                            }
                        }
                    }
                    if (getId != null)
                    {
                        try
                        {
                            object idObj = getId.Invoke(null, new object[] { waterReagent });
                            waterId = Convert.ToByte(idObj);
                            gotId = true;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("ReagentDatabase.GetID: " + ex.Message);
                        }
                    }
                }

                // Path A: GetReagent(float) → AddMix(Reagent)
                MethodInfo getReagent = AccessTools.Method(waterReagent.GetType(), "GetReagent", new Type[] { typeof(float) });
                if (getReagent != null)
                {
                    try
                    {
                        object reagentVal = getReagent.Invoke(waterReagent, new object[] { volume });
                        if (reagentVal != null)
                        {
                            // AddMix(Reagent, GenericReagentContainer = null)
                            MethodInfo[] methods = contents.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            for (int i = 0; i < methods.Length; i++)
                            {
                                MethodInfo m = methods[i];
                                if (m == null || m.Name != "AddMix") continue;
                                ParameterInfo[] ps = m.GetParameters();
                                if (ps == null || ps.Length < 1) continue;
                                if (ps[0].ParameterType.IsInstanceOfType(reagentVal) ||
                                    ps[0].ParameterType.IsAssignableFrom(reagentVal.GetType()))
                                {
                                    if (ps.Length == 1)
                                        m.Invoke(contents, new object[] { reagentVal });
                                    else
                                        m.Invoke(contents, new object[] { reagentVal, null });
                                    Logger.LogInfo("Water contents via GetReagent+AddMix(Reagent)");
                                    return contents;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("GetReagent/AddMix(Reagent): " + ex.Message);
                    }
                }

                // Path B: AddMix(byte id, float volume) — official primary API
                if (gotId || true)
                {
                    if (!gotId)
                    {
                        // last chance: Unity instance id low byte (weak)
                        UnityEngine.Object uo = waterReagent as UnityEngine.Object;
                        if (uo != null)
                            waterId = (byte)(uo.GetInstanceID() & 0xFF);
                    }

                    MethodInfo[] methods = contents.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                    for (int i = 0; i < methods.Length; i++)
                    {
                        MethodInfo m = methods[i];
                        if (m == null || m.Name != "AddMix") continue;
                        ParameterInfo[] ps = m.GetParameters();
                        if (ps == null || ps.Length < 2) continue;
                        if (ps[0].ParameterType != typeof(byte)) continue;
                        if (ps[1].ParameterType != typeof(float) && ps[1].ParameterType != typeof(Single))
                            continue;
                        try
                        {
                            if (ps.Length == 2)
                                m.Invoke(contents, new object[] { waterId, volume });
                            else
                                m.Invoke(contents, new object[] { waterId, volume, null });
                            Logger.LogInfo("Water contents via AddMix(byte id=" + waterId + ", vol=" + volume + ")");
                            return contents;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("AddMix(byte,float): " + ex.Message);
                        }
                    }

                    // OverrideReagent(byte, float)
                    MethodInfo ov = AccessTools.Method(contents.GetType(), "OverrideReagent", new Type[] { typeof(byte), typeof(float) });
                    if (ov != null)
                    {
                        try
                        {
                            ov.Invoke(contents, new object[] { waterId, volume });
                            Logger.LogInfo("Water contents via OverrideReagent id=" + waterId);
                            return contents;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("OverrideReagent: " + ex.Message);
                        }
                    }
                }

                Logger.LogWarning("BuildWater: failed after ctor ok, gotId=" + gotId + " id=" + waterId);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("BuildWaterReagentContents: " + ex.Message);
            }
            return null;
        }

        private static MethodInfo FindAddMixAccepting(Type contentsType, Type argType)
        {
            if (contentsType == null || argType == null) return null;
            MethodInfo[] methods = contentsType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            for (int i = 0; i < methods.Length; i++)
            {
                MethodInfo m = methods[i];
                if (m == null || m.Name != "AddMix") continue;
                ParameterInfo[] ps = m.GetParameters();
                if (ps == null || ps.Length != 1) continue;
                if (ps[0].ParameterType == argType || ps[0].ParameterType.IsAssignableFrom(argType))
                    return m;
            }
            return null;
        }

        private object ResolveWaterReagent(Type scriptableReagentType)
        {
            try
            {
                // Database<ScriptableReagent>.TryGetAsset("Water", out var)
                Assembly[] asms = AppDomain.CurrentDomain.GetAssemblies();
                for (int a = 0; a < asms.Length; a++)
                {
                    Assembly asm = asms[a];
                    if (asm == null) continue;
                    string an = asm.GetName().Name ?? "";
                    if (an != "Assembly-CSharp" && an != "Assembly-CSharp-firstpass" &&
                        an.IndexOf("Kobold", StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                    Type[] types = null;
                    try { types = asm.GetTypes(); }
                    catch (ReflectionTypeLoadException rtl) { types = rtl.Types; }
                    catch { continue; }
                    if (types == null) continue;
                    for (int t = 0; t < types.Length; t++)
                    {
                        Type ty = types[t];
                        if (ty == null || !ty.IsGenericTypeDefinition) continue;
                        if (ty.Name != "Database`1") continue;
                        try
                        {
                            Type closed = ty.MakeGenericType(scriptableReagentType);
                            MethodInfo tryGet = AccessTools.Method(closed, "TryGetAsset",
                                new Type[] { typeof(string), scriptableReagentType.MakeByRefType() });
                            if (tryGet == null) continue;
                            object[] args = new object[] { "Water", null };
                            object ok = tryGet.Invoke(null, args);
                            if (ok is bool && (bool)ok && args[1] != null)
                                return args[1];
                        }
                        catch { }
                    }
                }

                Type reagentDb = SafeGameType("ReagentDatabase");
                if (reagentDb != null)
                {
                    MethodInfo getReagent = AccessTools.Method(reagentDb, "GetReagent", new Type[] { typeof(string) });
                    if (getReagent != null)
                    {
                        try
                        {
                            object r = getReagent.Invoke(null, new object[] { "Water" });
                            if (r != null) return r;
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("ResolveWaterReagent: " + ex.Message);
            }
            return null;
        }

        private void ApplyLocalNickName(string name)
        {
            name = name != null ? name.Trim() : "";
            if (name.Length > 32)
                name = name.Substring(0, 32);

            try
            {
                PhotonNetwork.NickName = name;
                if (PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.NickName = name;

                nameEditStatus = string.IsNullOrEmpty(name) ? "Name cleared" : ("Name set: " + name);
                nameEditStatusUntil = Time.unscaledTime + 3f;
                nameEditText = name;

                // Refresh published player list if we're hosting
                if (PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient)
                    PublishRoomPlayerList(true);
            }
            catch (Exception ex)
            {
                nameEditStatus = "Name failed: " + ex.Message;
                nameEditStatusUntil = Time.unscaledTime + 4f;
                Logger.LogWarning("ApplyLocalNickName: " + ex.Message);
            }
        }

        private void UpdateRoomPlayersPublish()
        {
            // Always publish while host if toggle is on (lobby list needs host + room created with mod)
            if (!publishRoomPlayers)
                return;
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;
            if (Time.unscaledTime < nextRoomPlayersPublishTime)
                return;

            nextRoomPlayersPublishTime = Time.unscaledTime + 2.0f;
            PublishRoomPlayerList(false);
        }

        private void PublishRoomPlayerList(bool force)
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;
            if (!publishRoomPlayers && !force)
                return;

            try
            {
                Player[] players = PhotonNetwork.PlayerList;
                List<string> names = new List<string>();
                if (players != null)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        Player p = players[i];
                        if (p == null) continue;
                        string n = GetPlayerName(p);
                        if (string.IsNullOrEmpty(n))
                            n = "Player" + p.ActorNumber;
                        // Comma is delimiter — strip it from names
                        n = n.Replace(',', ' ').Replace(';', ' ').Trim();
                        if (n.Length > 48) n = n.Substring(0, 48);
                        names.Add(n);
                    }
                }

                string payload = string.Join(",", names.ToArray());
                if (!force && payload == lastPublishedRoomPlayers)
                    return;

                lastPublishedRoomPlayers = payload;
                Room room = PhotonNetwork.CurrentRoom;
                if (room == null) return;

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable
                {
                    { RoomPlayersPropertyKey, payload }
                };
                room.SetCustomProperties(props);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("PublishRoomPlayerList: " + ex.Message);
            }
        }

        private string GetRoomPlayersFromInfo(RoomInfo info)
        {
            if (info == null || info.CustomProperties == null)
                return "";
            object v;
            if (info.CustomProperties.TryGetValue(RoomPlayersPropertyKey, out v) && v != null)
                return v.ToString();
            return "";
        }

        private void ConfirmKickPlayer()
        {
            if (pendingKickPlayer == null ||
                !PhotonNetwork.InRoom ||
                !PhotonNetwork.IsMasterClient ||
                pendingKickPlayer.IsLocal)
            {
                CancelKickPlayer();
                return;
            }

            string playerName = GetPlayerName(pendingKickPlayer);
            int actorId = pendingKickPlayer.ActorNumber;

            PhotonNetwork.CloseConnection(pendingKickPlayer);

            AddRecentPlayerEvent(
                "KICKED: " + playerName + "  #" + actorId
            );

            if (selectedActorId == actorId)
            {
                selectedActorId = -1;
                selectedPlayer = null;
            }

            CancelKickPlayer();
        }

        private void CancelKickPlayer()
        {
            pendingKickPlayer = null;
            kickConfirmationVisible = false;
        }

        private void CancelKickAllPlayers()
        {
            kickAllConfirmationVisible = false;
        }

        private void KickAllOtherPlayers()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            Player[] players = PhotonNetwork.PlayerList;
            if (players == null)
                return;

            int kicked = 0;
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null || p.IsLocal)
                    continue;

                PhotonNetwork.CloseConnection(p);
                kicked++;
            }

            AddRecentPlayerEvent("KICKED ALL: " + kicked + " player" + (kicked == 1 ? "" : "s"));

            if (selectedPlayer != null && !selectedPlayer.IsLocal)
            {
                selectedActorId = -1;
                selectedPlayer = null;
            }
        }

        private void ConfirmKickAllOtherPlayers()
        {
            KickAllOtherPlayers();
            kickAllConfirmationVisible = false;
            kickAllCooldownUntil = Time.unscaledTime + KickAllCooldownSeconds;
        }

        private void CancelKickAllOtherPlayers()
        {
            kickAllConfirmationVisible = false;
        }

        private void RequestBanSelectedPlayer()
        {
            if (!PhotonNetwork.InRoom ||
                !PhotonNetwork.IsMasterClient ||
                selectedPlayer == null ||
                selectedPlayer.IsLocal)
            {
                return;
            }

            pendingBanPlayer = selectedPlayer;
            banConfirmationVisible = true;
        }

        private void ConfirmBanPlayer()
        {
            if (pendingBanPlayer == null ||
                !PhotonNetwork.InRoom ||
                !PhotonNetwork.IsMasterClient ||
                pendingBanPlayer.IsLocal)
            {
                CancelBanPlayer();
                return;
            }

            string playerName = GetPlayerName(pendingBanPlayer);
            int actorId = pendingBanPlayer.ActorNumber;

            RecordBannedPlayer(pendingBanPlayer);
            PhotonNetwork.CloseConnection(pendingBanPlayer);

            AddRecentPlayerEvent(
                "BANNED: " + playerName + "  #" + actorId
            );

            if (selectedActorId == actorId)
            {
                selectedActorId = -1;
                selectedPlayer = null;
            }

            CancelBanPlayer();
        }

        private void RecordBannedPlayer(Player player)
        {
            if (player == null)
                return;

            string key = !string.IsNullOrEmpty(player.UserId) ? player.UserId : GetPlayerName(player);
            if (string.IsNullOrEmpty(key))
                return;

            bannedUserIds.Add(key);
            SaveBannedUserIds();
        }

        private void AddRecentPlayerEvent(string message)
        {
            string line = DateTime.Now.ToString("HH:mm:ss") + "  " + message;
            recentPlayerEvents.Insert(0, line);
            if (recentPlayerEvents.Count > 40)
                recentPlayerEvents.RemoveAt(recentPlayerEvents.Count - 1);
        }

        private void CancelBanPlayer()
        {
            pendingBanPlayer = null;
            banConfirmationVisible = false;
        }

        // ============================================================
        // CONFIG PERSISTENCE
        // ============================================================
        private void SaveConfig()
        {
            if (configCameraHeight == null)
                return;

            configCameraHeight.Value = spectateCameraHeight;
            configCameraDistance.Value = spectateCameraDistance;
            configCameraRotation.Value = spectateCameraRotation;

            configBackgroundHue.Value = backgroundHue;
            configBackgroundOpacity.Value = backgroundOpacity;
            configBackgroundFPS.Value = BackgroundFramesPerSecond;
        }

        private void OnDestroy()
        {
            DestroyUITexture(ref uiWindowTexture);
            DestroyUITexture(ref uiSidebarTexture);
            DestroyUITexture(ref uiCardTexture);
            DestroyUITexture(ref uiButtonTexture);
            DestroyUITexture(ref uiButtonHoverTexture);
            DestroyUITexture(ref uiButtonActiveTexture);
            DestroyUITexture(ref uiAccentTexture);
            DestroyUITexture(ref uiMutedTexture);

            PhotonNetwork.RemoveCallbackTarget(this);

            SaveConfig();

            if (spectateHarmony != null)
            {
                spectateHarmony.UnpatchSelf();
                spectateHarmony = null;
            }

            Instance = null;

            cachedLocalPlayer = null;
            playerObjectCache.Clear();
            StopSpectating();
            if (tracerMaterial != null) Destroy(tracerMaterial);
            if (backgroundMaterial != null) Destroy(backgroundMaterial);
            if (menuBackground != null) Destroy(menuBackground);
            tracerMaterial = null; backgroundMaterial = null; menuBackground = null;
        }

        private bool TryCreateTextureFromBytes(byte[] data, out Texture2D tex)
        {
            tex = null;
            try
            {
                tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;

                // Try instance LoadImage(byte[], bool) or LoadImage(byte[])

                var inst = typeof(Texture2D).GetMethod("LoadImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(byte[]), typeof(bool) }, null);
                bool loaded = false;
                if (inst != null)
                {
                    loaded = (bool)inst.Invoke(tex, new object[] { data, false });
                }
                else
                {
                    inst = typeof(Texture2D).GetMethod("LoadImage", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new Type[] { typeof(byte[]) }, null);
                    if (inst != null)
                    {
                        loaded = (bool)inst.Invoke(tex, new object[] { data });
                    }
                    else
                    {
                        // Fallback: UnityEngine.ImageConversion static helper (some Unity versions)
                        Type ic = Type.GetType("UnityEngine.ImageConversion, UnityEngine");
                        if (ic != null)
                        {
                            var mi = ic.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]), typeof(bool) });
                            if (mi != null)
                            {
                                loaded = (bool)mi.Invoke(null, new object[] { tex, data, false });
                            }
                            else
                            {
                                mi = ic.GetMethod("LoadImage", new Type[] { typeof(Texture2D), typeof(byte[]) });
                                if (mi != null) loaded = (bool)mi.Invoke(null, new object[] { tex, data });
                            }
                        }
                    }
                }

                if (loaded)
                {
                    // Re-apply wrap/filter mode after LoadImage, since it can reset them.
                    tex.wrapMode = TextureWrapMode.Clamp;
                    tex.filterMode = FilterMode.Bilinear;
                    return true;
                }
            }
            catch { }
            if (tex != null) UnityEngine.Object.Destroy(tex);
            tex = null;
            return false;
        }

        // Replace DrawPlayerOverlay() with this right-aligned, spacer-free layout
        private void DrawPlayerOverlay()
        {
            Player[] players = PhotonNetwork.InRoom ? PhotonNetwork.PlayerList : new Player[0];
            Room currentRoom = PhotonNetwork.CurrentRoom;
            string serverName = currentRoom != null ? (string.IsNullOrEmpty(currentRoom.Name) ? "Unnamed Room" : currentRoom.Name) : "Not connected";

            float pad = 6f;
            float headerH = 20f;
            float serverLineH = 16f;
            float lineH = 20f;
            float footer = 6f;
            float minNameColW = 80f;

            float desiredH = pad * 2f + headerH + serverLineH + 6f + players.Length * lineH + footer;
            float maxH = Screen.height - 20f;
            bool needsScroll = desiredH > maxH;
            float drawH = needsScroll ? maxH : desiredH;

            float overlayW = playerOverlayRect.width;
            float rightMargin = 0f;
            float overlayX = Mathf.Clamp(Screen.width - overlayW - rightMargin, 2f, Screen.width - 2f);
            float overlayY = playerOverlayRect.y;

            if (!showPlayerOverlay)
            {
                float collapsedW = 92f;
                Rect collapsedRect = new Rect(overlayX + overlayW - collapsedW - 1f, overlayY, collapsedW, 22f);
                if (GUI.Button(collapsedRect, new GUIContent("Players"), buttonStyle))
                    showPlayerOverlay = true;
                return;
            }

            // close button reserves area on the far right so nothing draws under it
            float closeW = 16f;
            float closeGap = 4f;
            float usableRight = overlayX + overlayW - pad - closeW - closeGap;
            float nameX = overlayX + pad;
            float contentW = Mathf.Max(40f, usableRight - nameX);

            float y = overlayY + pad;

            GUI.Label(new Rect(nameX, y, contentW, headerH), new GUIContent("Players"), overlayHeaderStyle);
            y += headerH;

            string countLine = players.Length + (currentRoom != null && currentRoom.MaxPlayers > 0 ? "/" + currentRoom.MaxPlayers : "");

            GUIContent gcServer = new GUIContent(serverName);
            gcServer.tooltip = serverName;
            GUI.Label(new Rect(nameX, y, contentW - 40f, serverLineH * 2f), gcServer, overlayServerStyle);
            GUI.Label(new Rect(usableRight - 40f, y, 40f, serverLineH), new GUIContent(countLine), overlayInfoStyle);

            y += serverLineH * 2f + 4f;

            if (!needsScroll) playerOverlayScroll.y = 0f;
            float listH = drawH - (y - overlayY) - footer;
            Rect listScreenRect = new Rect(overlayX, y, overlayW, listH);
            Event ev = Event.current;
            if (ev != null && ev.type == EventType.ScrollWheel && listScreenRect.Contains(ev.mousePosition))
            {
                float contentHeight = players.Length * lineH;
                playerOverlayScroll.y = Mathf.Clamp(playerOverlayScroll.y + ev.delta.y * 25f, 0f, Mathf.Max(0f, contentHeight - listH));
                ev.Use();
            }

            float startY = y - playerOverlayScroll.y;
            Player master = PhotonNetwork.InRoom ? PhotonNetwork.MasterClient : null;

            // Single compact line: [Stop?] NAME | HOST | #ACTOR
            // While spectating: left-click a name to switch target; Stop button beside the current target
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null) continue;

                string name = string.IsNullOrEmpty(p.NickName) ? "Player " + p.ActorNumber : p.NickName;
                bool isHost = master != null && p.ActorNumber == master.ActorNumber;
                bool isSpecTarget = spectating && spectateActorId == p.ActorNumber;
                string colorTag = isSpecTarget ? "#ffcc66"
                    : (isHost ? "#ff6666" : (p.IsLocal ? "#66ccff" : "#d9d9d9"));
                string hostPart = isHost ? "HOST" : "-";
                string line = $"<color={colorTag}>{name}</color>  |  {hostPart}  |  #{p.ActorNumber}";

                float rowY = startY + i * lineH;
                float stopW = 56f;
                float stopGap = 4f;
                Rect stopRect = new Rect(nameX, rowY + 1f, stopW, lineH - 2f);
                Rect nameRect = isSpecTarget
                    ? new Rect(nameX + stopW + stopGap, rowY, contentW - stopW - stopGap, lineH)
                    : new Rect(nameX, rowY, contentW, lineH);
                Rect rowHit = new Rect(nameX, rowY, contentW, lineH);

                if (isSpecTarget)
                {
                    if (GUI.Button(stopRect, new GUIContent("Stop"), buttonStyle))
                        StopSpectating();
                }

                Event rowEvent = Event.current;
                if (rowEvent != null && rowEvent.type == EventType.MouseDown && rowHit.Contains(rowEvent.mousePosition))
                {
                    // Don't steal clicks from the Stop button
                    if (isSpecTarget && stopRect.Contains(rowEvent.mousePosition))
                    {
                        // button handles it
                    }
                    else if (rowEvent.button == 1)
                    {
                        SelectPlayer(p, force: true);
                        contextPlayer = p;
                        playerContextMenuPosition = rowEvent.mousePosition;
                        playerContextMenuVisible = true;
                        rowEvent.Use();
                    }
                    else if (rowEvent.button == 0)
                    {
                        // Left-click: select; if already spectating, switch to this player
                        SelectPlayer(p, force: true);
                        if (spectating && !p.IsLocal)
                            StartSpectating();
                        rowEvent.Use();
                    }
                }

                GUI.Label(nameRect, new GUIContent(line), overlayPlayerStyle);
            }

            if (GUI.Button(new Rect(overlayX + overlayW - closeW - closeGap, overlayY + 2f, closeW, closeW), new GUIContent("X"), buttonStyle))
                showPlayerOverlay = false;
        }

        // ============================================================
        // PLAYER CONTEXT MENU
        // ============================================================
        private void DrawPlayerContextMenu()
        {
            if (!playerContextMenuVisible || contextPlayer == null)
                return;

            const float w = 200f;
            const float pad = 8f;
            const float gap = 4f;
            const float bh = 26f;
            const float headerH = 36f;

            bool isHostBtn = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient &&
                             contextPlayer != null && !contextPlayer.IsLocal;
            // Spectate + tele + follow + friend + note + lock (+ kick/bring/freeze)
            int rows = 6 + (isHostBtn ? 3 : 0);
            float h = headerH + pad + rows * (bh + gap) + pad;

            float x = Mathf.Clamp(playerContextMenuPosition.x, 4f, Screen.width - w - 4f);
            float y = Mathf.Clamp(playerContextMenuPosition.y, 4f, Screen.height - h - 4f);

            // Card background
            BeginCardUI();
            GUI.Box(new Rect(x, y, w, h), GUIContent.none, GUI.skin.box);
            EndUIColor();

            // Accent header strip
            BeginAccentUI();
            GUI.Box(new Rect(x, y, w, headerH), GUIContent.none, GUI.skin.box);
            EndUIColor();

            // Support Unity rich-text nicknames (<color>, <b>, etc.) like the player list
            string pname = GetPlayerName(contextPlayer) ?? "";
            string plainName = StripRichText(pname);
            if (plainName.Length > 18)
            {
                // Don't cut mid-tag — fall back to plain truncated text
                pname = plainName.Substring(0, 16) + "…";
            }

            string role = contextPlayer.IsMasterClient ? "HOST" : (contextPlayer.IsLocal ? "YOU" : "");
            string sub = "#" + contextPlayer.ActorNumber + (string.IsNullOrEmpty(role) ? "" : " · " + role);

            GUIStyle nameStyle = overlayPlayerStyle != null ? new GUIStyle(overlayPlayerStyle) : new GUIStyle(labelStyle);
            nameStyle.richText = true;
            nameStyle.alignment = TextAnchor.MiddleLeft;
            nameStyle.normal.textColor = Color.white;
            GUI.Label(new Rect(x + pad, y + 3f, w - pad * 2f - 22f, 18f),
                new GUIContent(pname), nameStyle);
            GUI.Label(new Rect(x + pad, y + 18f, w - pad * 2f - 22f, 14f),
                new GUIContent(sub), smallStyle != null ? smallStyle : GUI.skin.label);

            // Close
            if (GUI.Button(new Rect(x + w - 26f, y + 6f, 20f, 20f), new GUIContent("×"), buttonStyle))
            {
                playerContextMenuVisible = false;
                return;
            }

            float by = y + headerH + pad;
            float fullW = w - pad * 2f;

            // Spectate — stop/switch live on the player list (Stop button + left-click name)
            bool isSpecThis = spectating && spectateActorId == contextPlayer.ActorNumber;
            if (GUI.Button(new Rect(x + pad, by, fullW, bh),
                new GUIContent(isSpecThis ? "Spectating…" : "Spectate"), buttonStyle))
            {
                if (!isSpecThis)
                {
                    SelectPlayer(contextPlayer, force: true);
                    StartSpectating();
                }
                playerContextMenuVisible = false;
            }
            by += bh + gap;

            // Teleport trio
            float tw = (fullW - gap * 2f) / 3f;
            if (GUI.Button(new Rect(x + pad, by, tw, bh), new GUIContent("Behind"), buttonStyle))
            {
                SelectPlayer(contextPlayer);
                TeleportBehindTarget();
                playerContextMenuVisible = false;
            }
            if (GUI.Button(new Rect(x + pad + tw + gap, by, tw, bh), new GUIContent("Front"), buttonStyle))
            {
                SelectPlayer(contextPlayer);
                TeleportInFrontOfTarget();
                playerContextMenuVisible = false;
            }
            if (GUI.Button(new Rect(x + pad + (tw + gap) * 2f, by, tw, bh), new GUIContent("Above"), buttonStyle))
            {
                SelectPlayer(contextPlayer);
                TeleportAboveTarget();
                playerContextMenuVisible = false;
            }
            by += bh + gap;

            // Follow
            bool following = followPlayerActorId == contextPlayer.ActorNumber;
            if (GUI.Button(new Rect(x + pad, by, fullW, bh),
                new GUIContent(following ? "Stop follow" : "Follow"), buttonStyle))
            {
                followPlayerActorId = following ? -1 : contextPlayer.ActorNumber;
                playerContextMenuVisible = false;
            }
            by += bh + gap;

            // Friend
            bool isFriend = friendActorIds.Contains(contextPlayer.ActorNumber);
            if (GUI.Button(new Rect(x + pad, by, fullW, bh),
                new GUIContent(isFriend ? "Unfriend" : "Add friend"), buttonStyle))
            {
                if (isFriend) friendActorIds.Remove(contextPlayer.ActorNumber);
                else friendActorIds.Add(contextPlayer.ActorNumber);
                playerContextMenuVisible = false;
            }
            by += bh + gap;

            // Note
            if (GUI.Button(new Rect(x + pad, by, fullW, bh), new GUIContent("Edit note"), buttonStyle))
            {
                SelectPlayer(contextPlayer);
                tab = 4;
                menuVisible = true;
                playerContextMenuVisible = false;
            }
            by += bh + gap;

            // Host actions
            if (isHostBtn)
            {
                if (GUI.Button(new Rect(x + pad, by, fullW, bh), new GUIContent("Kick"), buttonStyle))
                {
                    pendingKickPlayer = contextPlayer;
                    ConfirmKickPlayer();
                    playerContextMenuVisible = false;
                }
                by += bh + gap;

                if (GUI.Button(new Rect(x + pad, by, fullW, bh), new GUIContent("Bring to me"), buttonStyle))
                {
                    BringPlayerToMe(contextPlayer);
                    playerContextMenuVisible = false;
                }
                by += bh + gap;

                bool frozenCtx = IsPlayerFrozen(contextPlayer);
                if (GUI.Button(new Rect(x + pad, by, fullW, bh),
                    new GUIContent(frozenCtx ? "Unfreeze" : "Freeze"), buttonStyle))
                {
                    ToggleFreezePlayer(contextPlayer);
                    playerContextMenuVisible = false;
                }
                by += bh + gap;
            }

            // Lock
            bool locked = targetLocked && selectedActorId == contextPlayer.ActorNumber;
            if (GUI.Button(new Rect(x + pad, by, fullW, bh),
                new GUIContent(locked ? "Unlock target" : "Lock target"), buttonStyle))
            {
                SelectPlayer(contextPlayer);
                targetLocked = !locked;
                playerContextMenuVisible = false;
            }

            // Click outside closes
            Event e = Event.current;
            if (e != null && e.type == EventType.MouseDown && e.button == 0)
            {
                Rect r = new Rect(x, y, w, h);
                if (!r.Contains(e.mousePosition))
                {
                    playerContextMenuVisible = false;
                    e.Use();
                }
            }
        }

        // ============================================================
        // PLAYER RADAR (rounded, no title / no +/-)
        // ============================================================
        private void DrawPlayerRadar()
        {
            if (!PhotonNetwork.InRoom)
                return;

            if (!showPlayerRadar)
            {
                ClampRadar();

                float collapsedW = 72f;
                float collapsedH = 22f;

                Rect collapsedRect = new Rect(
                    playerRadarRect.x,
                    playerRadarRect.y,
                    collapsedW,
                    collapsedH
                );

                if (GUI.Button(
                    collapsedRect,
                    new GUIContent("Radar"),
                    buttonStyle))
                {
                    showPlayerRadar = true;
                }

                return;
            }

            playerRadarRect.width = Mathf.Clamp(playerRadarRect.width, RadarMinSize, RadarMaxSize);
            playerRadarRect.height = playerRadarRect.width;
            ClampRadar();

            // Transparent / chrome-free window so we can draw a circle ourselves.
            playerRadarRect = GUI.Window(
                9002,
                playerRadarRect,
                DrawPlayerRadarWindow,
                GUIContent.none,
                GUIStyle.none);
        }

        private void DrawPlayerRadarWindow(int id)
        {
            float size = playerRadarRect.width;
            float center = size * 0.5f;
            float radius = Mathf.Max(40f, center - 10f);

            // Circular background + ring
            Color discColor = GetMenuPanelColor(0.08f, 0.82f);
            Color ringColor = GetMenuAccentColor(0.75f);
            Color crossColor = GetMenuButtonTint(0.35f);

            DrawFilledCircle(center, center, radius, discColor);
            DrawCircleOutline(center, center, radius, ringColor, 2.0f);
            DrawCircleOutline(center, center, radius * 0.66f, crossColor, 1.0f);
            DrawCircleOutline(center, center, radius * 0.33f, crossColor, 1.0f);

            // Crosshair
            // DrawThickLine(
            // new Vector2(center - radius, center),
            // new Vector2(center + radius, center),
            // crossColor,
            // 1f);
            // DrawThickLine(
            // new Vector2(center, center - radius),
            // new Vector2(center, center + radius),
            // crossColor,
            // 1f);

            // Local player indicator (center) — follows menu hue
            GUI.color = GetMenuAccentColor(1f);
            GUI.Label(new Rect(center - 8f, center - 10f, 16f, 20f),
                new GUIContent("▲"), overlayHeaderStyle);
            GUI.color = Color.white;

            GameObject local = GetLocalPlayer();
            if (local != null)
            {
                Camera cam = Camera.main;
                float yaw = radarRotateWithCamera && cam != null ? cam.transform.eulerAngles.y : 0f;
                Quaternion inverseYaw = Quaternion.Euler(0f, -yaw, 0f);
                Player[] players = PhotonNetwork.PlayerList;

                for (int i = 0; i < players.Length; i++)
                {
                    Player p = players[i];
                    if (p == null || p.IsLocal)
                        continue;

                    GameObject obj = FindPlayerObject(p);
                    if (obj == null)
                        continue;

                    Vector3 worldOffset = obj.transform.position - local.transform.position;
                    float distance = new Vector2(worldOffset.x, worldOffset.z).magnitude;
                    if (distance > radarRange)
                        continue;

                    Vector3 relative = inverseYaw * worldOffset;
                    float px = center + (relative.x / radarRange) * radius;
                    float py = center - (relative.z / radarRange) * radius;

                    // Keep dots inside the circle
                    float dx = px - center;
                    float dy = py - center;
                    float distFromCenter = Mathf.Sqrt(dx * dx + dy * dy);
                    if (distFromCenter > radius - 4f)
                    {
                        float s = (radius - 4f) / distFromCenter;
                        px = center + dx * s;
                        py = center + dy * s;
                    }

                    Color color = GetESPColor(p);
                    bool selected = p.ActorNumber == selectedActorId;
                    string dot = selected ? "◆" : "●";
                    GUI.color = color;

                    float dotSize = selected ? 18f : 14f;
                    Rect dotRect = new Rect(px - dotSize * 0.5f, py - dotSize * 0.5f, dotSize, dotSize);
                    if (GUI.Button(dotRect, new GUIContent(dot), GUI.skin.label))
                    {
                        SelectPlayer(p);
                        targetLocked = selected ? targetLocked : false;
                    }

                    GUI.color = Color.white;
                    if (radarShowNames)
                    {
                        string text = GetPlayerName(p);
                        if (radarShowDistance)
                            text += " " + distance.ToString("0") + "m";

                        GUIStyle radarTextStyle = new GUIStyle(overlayInfoStyle);
                        radarTextStyle.alignment = TextAnchor.MiddleLeft;

                        const float labelWidth = 90f;
                        float labelX = px + 8f;
                        if (labelX + labelWidth > size - 4f)
                            labelX = px - labelWidth - 8f;

                        GUI.Label(
                            new Rect(labelX, py - 8f, labelWidth, 20f),
                            new GUIContent(text),
                            radarTextStyle);
                    }
                    else if (radarShowDistance)
                    {
                        GUIStyle radarTextStyle = new GUIStyle(overlayInfoStyle);
                        radarTextStyle.alignment = TextAnchor.MiddleLeft;

                        const float labelWidth = 48f;
                        float labelX = px + 8f;
                        if (labelX + labelWidth > size - 4f)
                            labelX = px - labelWidth - 8f;

                        GUI.Label(
                            new Rect(labelX, py - 8f, labelWidth, 20f),
                            new GUIContent(distance.ToString("0") + "m"),
                            radarTextStyle);
                    }
                }
            }

            // Compact footer + close only (no title, no +/-)
            GUI.Label(
                new Rect(10f, size - 22f, size - 40f, 18f),
                new GUIContent((radarRotateWithCamera ? "DIST" : "N") + "  " + radarRange.ToString("0") + "m"),
                smallStyle);

            if (GUI.Button(new Rect(size - 26f, 4f, 20f, 18f), new GUIContent("X"), buttonStyle))
                showPlayerRadar = false;

            // Drag anywhere on the radar
            GUI.DragWindow(new Rect(0f, 0f, size, size));
        }

        private void DrawFilledCircle(float cx, float cy, float radius, Color color)
        {
            EnsureTracerMaterial();
            if (tracerMaterial == null)
                return;

            tracerMaterial.SetPass(0);
            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);

            // Convert window-local coords to screen (window is already at playerRadarRect)
            float sx = playerRadarRect.x + cx;
            float sy = playerRadarRect.y + cy;

            GL.Begin(4);
            GL.Color(color);
            const int segments = 48;
            const float Pi = 3.14159265f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Pi * 2f;
                float a1 = ((i + 1) / (float)segments) * Pi * 2f;
                GL.Vertex3(sx, sy, 0f);
                GL.Vertex3(sx + Mathf.Cos(a0) * radius, sy + Mathf.Sin(a0) * radius, 0f);
                GL.Vertex3(sx + Mathf.Cos(a1) * radius, sy + Mathf.Sin(a1) * radius, 0f);
            }
            GL.End();
            GL.PopMatrix();
        }

        private void DrawCircleOutline(float cx, float cy, float radius, Color color, float thickness)
        {
            EnsureTracerMaterial();
            if (tracerMaterial == null)
                return;

            float sx = playerRadarRect.x + cx;
            float sy = playerRadarRect.y + cy;

            const int segments = 48;
            const float Pi = 3.14159265f;
            for (int i = 0; i < segments; i++)
            {
                float a0 = (i / (float)segments) * Pi * 2f;
                float a1 = ((i + 1) / (float)segments) * Pi * 2f;
                Vector2 p0 = new Vector2(sx + Mathf.Cos(a0) * radius, sy + Mathf.Sin(a0) * radius);
                Vector2 p1 = new Vector2(sx + Mathf.Cos(a1) * radius, sy + Mathf.Sin(a1) * radius);
                DrawThickLine(p0, p1, color, thickness);
            }
        }

        private void ClampRadar()
        {
            playerRadarRect.x = Mathf.Clamp(playerRadarRect.x, 2f, Mathf.Max(2f, Screen.width - playerRadarRect.width - 2f));
            playerRadarRect.y = Mathf.Clamp(playerRadarRect.y, 2f, Mathf.Max(2f, Screen.height - playerRadarRect.height - 2f));
        }

        private void UpdateFollowPlayer()
        {
            if (followPlayerActorId < 0 || !PhotonNetwork.InRoom)
                return;

            Player targetPlayer = GetPlayerByActorId(followPlayerActorId);
            GameObject local = GetLocalPlayer();
            GameObject target = targetPlayer == null ? null : FindPlayerObject(targetPlayer);

            if (targetPlayer == null || targetPlayer.IsLocal || local == null || target == null)
            {
                followPlayerActorId = -1;
                return;
            }

            Vector3 destination = target.transform.position - target.transform.forward * followDistance + Vector3.up * followHeight;
            Vector3 next = Vector3.Lerp(local.transform.position, destination, Mathf.Clamp01(Time.unscaledDeltaTime * 8f));
            TeleportLocalPlayer(next);
        }

        // ============================================================
        // SETTINGS PANEL (top-right SETTINGS button → tab 10)
        // ============================================================
        private void DrawSettingsPanel(float x, float y, float width, float maxHeight)
        {
            float startY = y;
            float leftW = width * 0.48f;
            float gap = 16f;
            float rightX = x + leftW + gap;
            float rightW = width - leftW - gap;

            // ---- LEFT: KEYBINDS (also still on Testing — left alone) ----
            GUI.Label(new Rect(x, y, leftW, 22f), new GUIContent("Binds"), headerStyle);
            y += 28f;

            string menuKeyLabel = menuToggleKey != null ? menuToggleKey.Value.ToString() : "Insert";
            string noclipKeyLabel = noclipToggleKey != null ? noclipToggleKey.Value.ToString() : "F1";
            string wpKeyLabel = waypointQuickSaveKey != null ? waypointQuickSaveKey.Value.ToString() : "F6";
            string flyUpLabel = flySpeedUpKey != null ? flySpeedUpKey.Value.ToString() : "F3";
            string flyDownLabel = flySpeedDownKey != null ? flySpeedDownKey.Value.ToString() : "F2";

            if (waitingForKeyRebind)
            {
                GUI.Label(new Rect(x, y, leftW, 28f),
                    new GUIContent("Press key…"),
                    accentLabelStyle != null ? accentLabelStyle : labelStyle);
                y += 36f;
            }
            else
            {
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Menu toggle: " + menuKeyLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "menu";
                }
                y += 32f;
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Fly noclip: " + noclipKeyLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "noclip";
                }
                y += 32f;
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Fly speed +: " + flyUpLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "flyUp";
                }
                y += 32f;
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Fly speed -: " + flyDownLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "flyDown";
                }
                y += 32f;
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Quick waypoint: " + wpKeyLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "waypoint";
                }
                y += 32f;
                string specNextLabel = spectateNextKey != null ? spectateNextKey.Value.ToString() : "]";
                string specPrevLabel = spectatePrevKey != null ? spectatePrevKey.Value.ToString() : "[";
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Spec next: " + specNextLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "specNext";
                }
                y += 32f;
                if (GUI.Button(new Rect(x, y, leftW, 28f), new GUIContent("Spec prev: " + specPrevLabel), buttonStyle))
                {
                    waitingForKeyRebind = true;
                    rebindTarget = "specPrev";
                }
                y += 36f;
            }

            GUI.Label(new Rect(x, y, leftW, 40f),
                new GUIContent("Click, then press a key · Esc cancels"),
                smallStyle);
            y += 48f;

            // Soft teleport (moved from Testing)
            GUI.Label(new Rect(x, y, leftW, 22f), new GUIContent("Soft TP"), headerStyle);
            y += 26f;
            if (GUI.Button(new Rect(x, y, leftW, 28f),
                new GUIContent(softTeleportEnabled ? "SOFT TELEPORT: ON" : "SOFT TELEPORT: OFF"), buttonStyle))
            {
                softTeleportEnabled = !softTeleportEnabled;
                if (configSoftTeleport != null)
                    configSoftTeleport.Value = softTeleportEnabled;
            }
            y += 34f;
            GUI.Label(new Rect(x, y, leftW, 20f),
                new GUIContent("DURATION: " + softTeleportDuration.ToString("0.00") + "s"), labelStyle);
            y += 22f;
            softTeleportDuration = GUI.HorizontalSlider(
                new Rect(x, y, leftW, 18f),
                softTeleportDuration, 0.05f, 1.5f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);

            // ---- RIGHT: MENU LOOK ----
            float ry = startY;
            GUI.Label(new Rect(rightX, ry, rightW, 22f), new GUIContent("Look"), headerStyle);
            ry += 28f;

            string hueState = menuColorGreyscale
                ? "greyscale"
                : (menuHueCycling ? "cycling" : "locked");
            string hueLabel = "HUE: " + Mathf.RoundToInt(backgroundHue * 360f) + "° · " + hueState;
            GUI.Label(new Rect(rightX, ry, rightW, 20f), new GUIContent(hueLabel), labelStyle);
            ry += 22f;

            float newHue = GUI.HorizontalSlider(
                new Rect(rightX, ry, rightW, 18f),
                backgroundHue, 0f, 1f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            if (!Mathf.Approximately(newHue, backgroundHue))
            {
                backgroundHue = newHue;
                lastStyledHue = -1f;
                if (configBackgroundHue != null)
                    configBackgroundHue.Value = backgroundHue;
            }
            ry += 28f;

            GUI.Label(new Rect(rightX, ry, rightW, 20f),
                new GUIContent("OPACITY: " + Mathf.RoundToInt(backgroundOpacity * 100f) + "%"), labelStyle);
            ry += 22f;
            float newOpacity = GUI.HorizontalSlider(
                new Rect(rightX, ry, rightW, 18f),
                backgroundOpacity, 0f, 1f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            if (!Mathf.Approximately(newOpacity, backgroundOpacity))
            {
                backgroundOpacity = newOpacity;
                if (configBackgroundOpacity != null)
                    configBackgroundOpacity.Value = backgroundOpacity;
            }
            ry += 28f;

            GUI.Label(new Rect(rightX, ry, rightW, 20f),
                new GUIContent("BG ANIM FPS: " + BackgroundFramesPerSecond.ToString("0")), labelStyle);
            ry += 22f;
            float newFps = GUI.HorizontalSlider(
                new Rect(rightX, ry, rightW, 18f),
                BackgroundFramesPerSecond, 1f, 60f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            if (!Mathf.Approximately(newFps, BackgroundFramesPerSecond))
            {
                BackgroundFramesPerSecond = newFps;
                if (configBackgroundFPS != null)
                    configBackgroundFPS.Value = BackgroundFramesPerSecond;
            }
            ry += 32f;

            float modeGap = 6f;
            float modeBtnW = (rightW - modeGap * 2f) / 3f;
            GUIStyle sel = selectedButtonStyle != null ? selectedButtonStyle : buttonStyle;
            GUIStyle nrm = buttonStyle;

            bool cycleActive = !menuColorGreyscale && menuHueCycling;
            if (cycleActive) BeginAccentUI(); else BeginButtonUI();
            if (GUI.Button(new Rect(rightX, ry, modeBtnW, 28f), new GUIContent("RGB CYCLE"), cycleActive ? sel : nrm))
            {
                menuColorGreyscale = false;
                menuHueCycling = true;
                lastStyledHue = -1f;
                if (configMenuGreyscale != null)
                    configMenuGreyscale.Value = false;
            }
            EndUIColor();

            bool lockActive = !menuColorGreyscale && !menuHueCycling;
            if (lockActive) BeginAccentUI(); else BeginButtonUI();
            if (GUI.Button(new Rect(rightX + modeBtnW + modeGap, ry, modeBtnW, 28f), new GUIContent("LOCK"), lockActive ? sel : nrm))
            {
                menuColorGreyscale = false;
                menuHueCycling = false;
                lastStyledHue = -1f;
                if (configBackgroundHue != null)
                    configBackgroundHue.Value = backgroundHue;
                if (configMenuGreyscale != null)
                    configMenuGreyscale.Value = false;
            }
            EndUIColor();

            if (menuColorGreyscale) BeginAccentUI(); else BeginButtonUI();
            if (GUI.Button(new Rect(rightX + (modeBtnW + modeGap) * 2f, ry, modeBtnW, 28f), new GUIContent("GREY"), menuColorGreyscale ? sel : nrm))
            {
                menuColorGreyscale = true;
                menuHueCycling = false;
                lastStyledHue = -1f;
                if (configBackgroundHue != null)
                    configBackgroundHue.Value = backgroundHue;
                if (configMenuGreyscale != null)
                    configMenuGreyscale.Value = true;
            }
            EndUIColor();
            ry += 34f;

            // Cycle speed (only meaningful while RGB CYCLE is on)
            GUI.Label(new Rect(rightX, ry, rightW, 18f),
                new GUIContent("Cycle " + Mathf.RoundToInt(menuHueCycleSeconds) + "s"), smallStyle);
            ry += 18f;
            float newCycle = GUI.HorizontalSlider(
                new Rect(rightX, ry, rightW, 16f),
                menuHueCycleSeconds, MenuHueCycleSecondsMin, MenuHueCycleSecondsMax,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            if (!Mathf.Approximately(newCycle, menuHueCycleSeconds))
                menuHueCycleSeconds = newCycle;
            ry += 28f;

            GUI.Label(new Rect(rightX, ry, rightW, 18f), new GUIContent("Config"), smallStyle);
            ry += 20f;
            float half = (rightW - 8f) * 0.5f;
            if (GUI.Button(new Rect(rightX, ry, half, 26f), new GUIContent("Export"), buttonStyle))
                ExportQoLConfig();
            if (GUI.Button(new Rect(rightX + half + 8f, ry, half, 26f), new GUIContent("Import"), buttonStyle))
                ImportQoLConfig();
        }

        /// <summary>
        /// Export binds, favorites, and mod presets to a single text file next to the plugin config.
        /// </summary>
        private void ExportQoLConfig()
        {
            try
            {
                string dir = Paths.ConfigPath;
                if (string.IsNullOrEmpty(dir))
                    dir = ".";
                string path = System.IO.Path.Combine(dir, "ZexQoLMenu_export.cfg");
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("# Zex QoL Menu export");
                sb.AppendLine("menu=" + (menuToggleKey != null ? menuToggleKey.Value.ToString() : ""));
                sb.AppendLine("noclip=" + (noclipToggleKey != null ? noclipToggleKey.Value.ToString() : ""));
                sb.AppendLine("flyUp=" + (flySpeedUpKey != null ? flySpeedUpKey.Value.ToString() : ""));
                sb.AppendLine("flyDown=" + (flySpeedDownKey != null ? flySpeedDownKey.Value.ToString() : ""));
                sb.AppendLine("waypoint=" + (waypointQuickSaveKey != null ? waypointQuickSaveKey.Value.ToString() : ""));
                sb.AppendLine("specNext=" + (spectateNextKey != null ? spectateNextKey.Value.ToString() : ""));
                sb.AppendLine("specPrev=" + (spectatePrevKey != null ? spectatePrevKey.Value.ToString() : ""));
                sb.AppendLine("favPrefabs=" + string.Join(",", favoritePrefabNames));
                sb.AppendLine("favRooms=" + string.Join(",", favoriteRoomNames));
                if (configModPresets != null)
                    sb.AppendLine("modPresets=" + (configModPresets.Value ?? ""));
                else
                {
                    // fallback from memory
                    System.Text.StringBuilder mp = new System.Text.StringBuilder();
                    for (int i = 0; i < modPresetNames.Count; i++)
                    {
                        if (i > 0) mp.Append(';');
                        mp.Append(modPresetNames[i]);
                    }
                    sb.AppendLine("modPresets=" + mp);
                }
                sb.AppendLine("autoSplash=" + autoSplashOnJoin);
                sb.AppendLine("welcome=" + welcomeMessageOnJoin);
                System.IO.File.WriteAllText(path, sb.ToString());
                ShowToast("Exported config");
                Logger.LogInfo("Exported QoL config to " + path);
            }
            catch (Exception ex)
            {
                ShowToast("Export failed");
                Logger.LogWarning("ExportQoLConfig: " + ex.Message);
            }
        }

        private void ImportQoLConfig()
        {
            try
            {
                string dir = Paths.ConfigPath;
                if (string.IsNullOrEmpty(dir))
                    dir = ".";
                string path = System.IO.Path.Combine(dir, "ZexQoLMenu_export.cfg");
                if (!System.IO.File.Exists(path))
                {
                    ShowToast("No export file");
                    return;
                }
                string[] lines = System.IO.File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    string line = lines[i];
                    if (string.IsNullOrEmpty(line) || line[0] == '#') continue;
                    int eq = line.IndexOf('=');
                    if (eq <= 0) continue;
                    string key = line.Substring(0, eq).Trim();
                    string val = line.Substring(eq + 1).Trim();
                    if (key == "menu" && menuToggleKey != null && TryParseKeyCode(val, out KeyCode k0))
                        menuToggleKey.Value = new KeyboardShortcut(k0);
                    else if (key == "noclip" && noclipToggleKey != null && TryParseKeyCode(val, out KeyCode k1))
                        noclipToggleKey.Value = k1;
                    else if (key == "flyUp" && flySpeedUpKey != null && TryParseKeyCode(val, out KeyCode k2))
                        flySpeedUpKey.Value = k2;
                    else if (key == "flyDown" && flySpeedDownKey != null && TryParseKeyCode(val, out KeyCode k3))
                        flySpeedDownKey.Value = k3;
                    else if (key == "waypoint" && waypointQuickSaveKey != null && TryParseKeyCode(val, out KeyCode k4))
                        waypointQuickSaveKey.Value = k4;
                    else if (key == "specNext" && spectateNextKey != null && TryParseKeyCode(val, out KeyCode k5))
                        spectateNextKey.Value = k5;
                    else if (key == "specPrev" && spectatePrevKey != null && TryParseKeyCode(val, out KeyCode k6))
                        spectatePrevKey.Value = k6;
                    else if (key == "favPrefabs")
                    {
                        favoritePrefabNames.Clear();
                        if (!string.IsNullOrEmpty(val))
                            foreach (string s in val.Split(','))
                                if (!string.IsNullOrEmpty(s.Trim()))
                                    favoritePrefabNames.Add(s.Trim());
                        SaveFavoritePrefabNames();
                    }
                    else if (key == "favRooms")
                    {
                        favoriteRoomNames.Clear();
                        if (!string.IsNullOrEmpty(val))
                            foreach (string s in val.Split(','))
                                if (!string.IsNullOrEmpty(s.Trim()))
                                    favoriteRoomNames.Add(s.Trim());
                        SaveFavoriteRoomNames();
                    }
                    else if (key == "modPresets" && configModPresets != null)
                    {
                        configModPresets.Value = val;
                        LoadModPresetsFromConfig();
                    }
                    else if (key == "autoSplash")
                    {
                        autoSplashOnJoin = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1";
                        if (configAutoSplashOnJoin != null)
                            configAutoSplashOnJoin.Value = autoSplashOnJoin;
                    }
                    else if (key == "welcome")
                    {
                        welcomeMessageOnJoin = val.Equals("true", StringComparison.OrdinalIgnoreCase) || val == "1";
                        if (configWelcomeMessageOnJoin != null)
                            configWelcomeMessageOnJoin.Value = welcomeMessageOnJoin;
                    }
                }
                ShowToast("Imported config");
                Logger.LogInfo("Imported QoL config from " + path);
            }
            catch (Exception ex)
            {
                ShowToast("Import failed");
                Logger.LogWarning("ImportQoLConfig: " + ex.Message);
            }
        }

        private static bool TryParseKeyCode(string s, out KeyCode code)
        {
            code = KeyCode.None;
            if (string.IsNullOrEmpty(s)) return false;
            try
            {
                code = (KeyCode)System.Enum.Parse(typeof(KeyCode), s, true);
                return code != KeyCode.None;
            }
            catch { return false; }
        }

        // ============================================================
        // QOL PANEL (sidebar QOL → tab 11)
        // ============================================================
        private void DrawQoLPanel(float x, float y, float width, float maxHeight)
        {
            float startY = y;
            float leftW = width * 0.48f;
            float gap = 16f;
            float rightX = x + leftW + gap;
            float rightW = width - leftW - gap;

            // ---- LEFT: FLIGHT ----
            GUI.Label(new Rect(x, y, leftW, 22f), new GUIContent("NOCLIP"), headerStyle);
            y += 28f;

            if (GUI.Button(new Rect(x, y, leftW, 32f),
                new GUIContent(flyingNoclipActive ? "FLY NOCLIP: ON" : "FLY NOCLIP: OFF"),
                flyingNoclipActive ? selectedButtonStyle : buttonStyle))
            {
                ToggleFlyingNoclip();
            }
            y += 38f;

            GUI.Label(new Rect(x, y, leftW, 20f),
                new GUIContent("SPEED: " + flySpeed.ToString("0")), labelStyle);
            y += 22f;
            float newFly = GUI.HorizontalSlider(
                new Rect(x, y, leftW, 18f),
                flySpeed, 5f, 500f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            if (!Mathf.Approximately(newFly, flySpeed))
            {
                flySpeed = newFly;
                if (configFlySpeed != null) configFlySpeed.Value = flySpeed;
            }
            y += 26f;

            if (GUI.Button(new Rect(x, y, leftW * 0.48f, 28f), new GUIContent("SPEED -10"), buttonStyle))
                AdjustFlySpeed(-10f);
            if (GUI.Button(new Rect(x + leftW * 0.52f, y, leftW * 0.48f, 28f), new GUIContent("SPEED +10"), buttonStyle))
                AdjustFlySpeed(10f);
            y += 34f;

            string flyUpHint = flySpeedUpKey != null ? flySpeedUpKey.Value.ToString() : "F3";
            string flyDownHint = flySpeedDownKey != null ? flySpeedDownKey.Value.ToString() : "F2";
            string flyToggleHint = noclipToggleKey != null ? noclipToggleKey.Value.ToString() : "F1";
            GUI.Label(new Rect(x, y, leftW, 56f),
                new GUIContent(
                    "WASD · Space up · Shift down.\n" +
                    "Hotkeys: toggle " + flyToggleHint + " · +" + flyUpHint + " · -" + flyDownHint + "\n" +
                    flyDebugStatus),
                smallStyle);
            y += 64f;

            GUI.Label(new Rect(x, y, leftW, 22f), new GUIContent("Player List"), headerStyle);
            y += 26f;
            if (GUI.Button(new Rect(x, y, leftW, 28f),
                new GUIContent(showPlayerOverlay ? "Player List: ON" : "Player List: OFF"), buttonStyle))
                showPlayerOverlay = !showPlayerOverlay;
            y += 34f;

            // ---- NAME + LEAVE QoL ----
            GUI.Label(new Rect(x, y, leftW, 22f), new GUIContent("Name(WIP) / Auto Desync Fix"), headerStyle);
            y += 26f;

            Event qe = Event.current;
            Rect nameRect = new Rect(x, y, leftW - 72f, 24f);
            GUI.Box(nameRect, "");
            string nameShown = string.IsNullOrEmpty(nameEditText) ? "nickname..." : nameEditText;
            if (nameEditFocused) nameShown += "|";
            GUI.Label(new Rect(nameRect.x + 4f, nameRect.y + 3f, nameRect.width - 8f, 18f),
                new GUIContent(nameShown), labelStyle);
            if (qe != null && qe.type == EventType.MouseDown && nameRect.Contains(qe.mousePosition))
            {
                nameEditFocused = true;
                qe.Use();
            }
            if (nameEditFocused && qe != null && qe.type == EventType.KeyDown)
            {
                if (qe.keyCode == KeyCode.Backspace && nameEditText.Length > 0)
                {
                    nameEditText = nameEditText.Substring(0, nameEditText.Length - 1);
                    qe.Use();
                }
                else if (qe.keyCode == KeyCode.Return || qe.keyCode == KeyCode.KeypadEnter)
                {
                    ApplyLocalNickName(nameEditText);
                    nameEditFocused = false;
                    qe.Use();
                }
                else if (qe.keyCode == KeyCode.Escape)
                {
                    nameEditFocused = false;
                    qe.Use();
                }
                else if (qe.character != '\0' && !char.IsControl(qe.character) && nameEditText.Length < 32)
                {
                    nameEditText += qe.character;
                    qe.Use();
                }
            }
            if (GUI.Button(new Rect(x + leftW - 68f, y, 68f, 24f), new GUIContent("Set"), buttonStyle))
            {
                ApplyLocalNickName(nameEditText);
                nameEditFocused = false;
            }
            y += 30f;

            if (!string.IsNullOrEmpty(nameEditStatus) && Time.unscaledTime < nameEditStatusUntil)
            {
                GUI.Label(new Rect(x, y, leftW, 18f), new GUIContent(nameEditStatus), smallStyle);
                y += 20f;
            }

            if (GUI.Button(new Rect(x, y, leftW, 28f),
                new GUIContent(destroyBodyOnLeave ? "Clean Left Users : ON" : "Clean Left Users : OFF"),
                buttonStyle))
            {
                destroyBodyOnLeave = !destroyBodyOnLeave;
                if (configDestroyBodyOnLeave != null)
                    configDestroyBodyOnLeave.Value = destroyBodyOnLeave;
            }
            y += 32f;

            float third = (leftW - 16f) / 3f;
            if (GUI.Button(new Rect(x, y, third, 28f),
                new GUIContent(autoSplashOnJoin ? "Splash auto" : "Splash off"),
                buttonStyle))
            {
                autoSplashOnJoin = !autoSplashOnJoin;
                if (configAutoSplashOnJoin != null)
                    configAutoSplashOnJoin.Value = autoSplashOnJoin;
            }
            if (GUI.Button(new Rect(x + third + 8f, y, third, 28f),
                new GUIContent("Splash all"), buttonStyle))
            {
                if (!PhotonNetwork.InRoom)
                    ShowToast("Not in a room");
                else
                    StartAutoSplashRoom(excludeActorId: -1, waitSeconds: 0.15f);
            }
            if (GUI.Button(new Rect(x + 2f * (third + 8f), y, third, 28f),
                new GUIContent(welcomeMessageOnJoin ? "Welcome ON" : "Welcome OFF"),
                buttonStyle))
            {
                welcomeMessageOnJoin = !welcomeMessageOnJoin;
                if (configWelcomeMessageOnJoin != null)
                    configWelcomeMessageOnJoin.Value = welcomeMessageOnJoin;
            }
            y += 32f;
            if (!string.IsNullOrEmpty(autoSplashStatus) && Time.unscaledTime < autoSplashStatusUntil)
            {
                GUI.Label(new Rect(x, y, leftW, 18f), new GUIContent(autoSplashStatus), smallStyle);
                y += 22f;
            }

            if (GUI.Button(new Rect(x, y, leftW, 28f),
                new GUIContent(publishRoomPlayers ? "Publish: ON" : "Publish: OFF"),
                buttonStyle))
            {
                publishRoomPlayers = !publishRoomPlayers;
                if (configPublishRoomPlayers != null)
                    configPublishRoomPlayers.Value = publishRoomPlayers;
                if (publishRoomPlayers && PhotonNetwork.IsMasterClient)
                    PublishRoomPlayerList(true);
            }
            y += 30f;
            GUI.Label(new Rect(x, y, leftW, 48f),
                new GUIContent(
                    "Hover names need: host runs mod + Publish ON.\n" +
                    "Best when the room was CREATED with this mod\n" +
                    "(lobby prop injected on CreateRoom)."),
                smallStyle);

            // ---- RIGHT: RADAR + REWARDS ----
            float ry = startY;
            GUI.Label(new Rect(rightX, ry, rightW, 22f), new GUIContent("Radar"), headerStyle);
            ry += 28f;

            if (GUI.Button(new Rect(rightX, ry, rightW * 0.48f, 28f),
                new GUIContent(showPlayerRadar ? "RADAR: ON" : "RADAR: OFF"), buttonStyle))
                showPlayerRadar = !showPlayerRadar;
            if (GUI.Button(new Rect(rightX + rightW * 0.52f, ry, rightW * 0.48f, 28f),
                new GUIContent(radarRotateWithCamera ? "ROTATE: CAM" : "ROTATE: N"), buttonStyle))
                radarRotateWithCamera = !radarRotateWithCamera;
            ry += 34f;

            if (GUI.Button(new Rect(rightX, ry, rightW * 0.48f, 28f),
                new GUIContent(radarShowNames ? "NAMES: ON" : "NAMES: OFF"), buttonStyle))
                radarShowNames = !radarShowNames;
            if (GUI.Button(new Rect(rightX + rightW * 0.52f, ry, rightW * 0.48f, 28f),
                new GUIContent(radarShowDistance ? "DIST: ON" : "DIST: OFF"), buttonStyle))
                radarShowDistance = !radarShowDistance;
            ry += 34f;

            GUI.Label(new Rect(rightX, ry, 90f, 20f),
                new GUIContent("RANGE: " + radarRange.ToString("0") + "m"), labelStyle);
            radarRange = GUI.HorizontalSlider(
                new Rect(rightX + 95f, ry + 2f, rightW - 95f, 18f),
                radarRange, 5f, 100f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb);
            ry += 36f;

            GUI.Label(new Rect(rightX, ry, rightW, 22f), new GUIContent("Money/Stars"), headerStyle);
            ry += 26f;

            float rewardButtonWidth = (rightW - 8f) / 2f;
            float rewardButtonHeight = 30f;

            if (GUI.Button(new Rect(rightX, ry, rewardButtonWidth, rewardButtonHeight),
                new GUIContent("MAX MONEY"), buttonStyle))
                GiveMyMaxMoney();
            if (GUI.Button(new Rect(rightX + rewardButtonWidth + 8f, ry, rewardButtonWidth, rewardButtonHeight),
                new GUIContent("MAX STARS"), buttonStyle))
                GiveMyMaxStars();
            ry += rewardButtonHeight + 6f;

            bool canRoomReward = PhotonNetwork.InRoom && PhotonNetwork.IsMasterClient;
            GUIStyle rewardButtonStyle = canRoomReward ? buttonStyle : GUI.skin.button;

            if (GUI.Button(new Rect(rightX, ry, rewardButtonWidth, rewardButtonHeight),
                new GUIContent("ALL MONEY"), rewardButtonStyle) && canRoomReward)
                GiveAllMaxMoney();
            if (GUI.Button(new Rect(rightX + rewardButtonWidth + 8f, ry, rewardButtonWidth, rewardButtonHeight),
                new GUIContent("ALL STARS"), rewardButtonStyle) && canRoomReward)
                StartGiveAllMaxStars();
            ry += rewardButtonHeight + 10f;

            if (!string.IsNullOrEmpty(rewardStatus) && Time.unscaledTime < rewardStatusUntil)
            {
                GUI.Label(new Rect(rightX, ry, rightW, 22f), new GUIContent(rewardStatus), smallStyle);
            }
            else if (!canRoomReward)
            {
                GUI.Label(new Rect(rightX, ry, rightW, 32f),
                    new GUIContent("ALL MONEY / ALL STARS need host."),
                    smallStyle);
            }
        }

        private void DrawMiscPanel(float x, float y, float width, float maxHeight)
        {
            GUI.Label(new Rect(x, y, width, 24f), new GUIContent("MISC"), headerStyle);
            y += 36f;

            GUI.Label(new Rect(x, y, width, 80f),
                new GUIContent(
                    "This tab is mostly retired.\n\n" +
                    "• Player Radar, Overlay, Rewards → QOL (sidebar)\n" +
                    "• Background color & Keybinds → SETTINGS (top-right)"),
                smallStyle);
            y += 100f;

            if (GUI.Button(new Rect(x, y, 160f, 32f), new GUIContent("OPEN QOL"), buttonStyle))
                tab = 11;
            if (GUI.Button(new Rect(x + 170f, y, 160f, 32f), new GUIContent("OPEN SETTINGS"), buttonStyle))
                tab = 10;
        }

        private void DrawHostLogsPanel(float x, float y, float width, float maxHeight)
        {
            float startY = y;

            GUI.Label(
                new Rect(x, y, width - 100f, 22f),
                new GUIContent("RECENT PLAYER EVENTS"),
                headerStyle);

            if (GUI.Button(
                new Rect(x + width - 90f, y - 2f, 90f, 26f),
                new GUIContent("CLEAR"),
                recentPlayerEvents.Count > 0 ? buttonStyle : GUI.skin.button) &&
                recentPlayerEvents.Count > 0)
            {
                recentPlayerEvents.Clear();
            }

            y += 30f;

            float eventsH = Mathf.Max(120f, (maxHeight - 60f) * 0.45f);
            Rect eventsViewRect = new Rect(x, y, width, eventsH);

            const float eventRowHeight = 20f;
            float eventsContentHeight = recentPlayerEvents.Count * eventRowHeight;
            float eventsMaxScroll = Mathf.Max(0f, eventsContentHeight - eventsH);

            Event eventsScrollEvent = Event.current;
            if (eventsScrollEvent != null &&
                eventsScrollEvent.type == EventType.ScrollWheel &&
                eventsViewRect.Contains(eventsScrollEvent.mousePosition))
            {
                recentEventsScroll.y = Mathf.Clamp(recentEventsScroll.y + eventsScrollEvent.delta.y * 25f, 0f, eventsMaxScroll);
                eventsScrollEvent.Use();
            }

            recentEventsScroll.y = Mathf.Clamp(recentEventsScroll.y, 0f, eventsMaxScroll);

            GUI.Box(eventsViewRect, new GUIContent(""), GUI.skin.box);
            GUI.BeginGroup(eventsViewRect, new GUIContent(""), GUIStyle.none);

            float eventY = -recentEventsScroll.y;
            if (recentPlayerEvents.Count == 0)
            {
                GUI.Label(new Rect(12f, 12f, width - 24f, 22f),
                    new GUIContent("No join/leave events yet."),
                    labelStyle);
            }
            else
            {
                for (int i = 0; i < recentPlayerEvents.Count; i++)
                {
                    GUI.Label(
                        new Rect(8f, eventY, eventsViewRect.width - 16f, eventRowHeight),
                        new GUIContent(recentPlayerEvents[i]),
                        smallStyle);
                    eventY += eventRowHeight;
                }
            }

            GUI.EndGroup();

            y += eventsH + 18f;

            GUI.Label(
                new Rect(x, y, width - 100f, 22f),
                new GUIContent("BANNED PLAYERS (" + bannedUserIds.Count + ")"),
                headerStyle);

            if (GUI.Button(
                new Rect(x + width - 90f, y - 2f, 90f, 26f),
                new GUIContent("CLEAR ALL"),
                bannedUserIds.Count > 0 ? buttonStyle : GUI.skin.button) &&
                bannedUserIds.Count > 0)
            {
                bannedUserIds.Clear();
                if (configBannedUserIds != null)
                    configBannedUserIds.Value = "";
            }

            y += 30f;

            float banListHeight = Mathf.Max(80f, maxHeight - (y - startY) - 8f);
            Rect banListRect = new Rect(x, y, width, banListHeight);

            if (bannedUserIds.Count == 0)
            {
                GUI.Box(banListRect, "");
                GUI.Label(
                    new Rect(x + 12f, y + 12f, width - 24f, 22f),
                    new GUIContent("No players banned this session."),
                    labelStyle);
            }
            else
            {
                List<string> bannedList = new List<string>(bannedUserIds);

                const float rowHeight = 34f;
                float contentHeight = bannedList.Count * rowHeight;
                float maxScroll = Mathf.Max(0f, contentHeight - banListHeight);

                Event banEvent = Event.current;
                if (banEvent != null &&
                    banEvent.type == EventType.ScrollWheel &&
                    banListRect.Contains(banEvent.mousePosition))
                {
                    bannedListScroll.y = Mathf.Clamp(bannedListScroll.y + banEvent.delta.y * 25f, 0f, maxScroll);
                    banEvent.Use();
                }

                bannedListScroll.y = Mathf.Clamp(bannedListScroll.y, 0f, maxScroll);

                GUI.Box(banListRect, "");
                GUI.BeginGroup(banListRect, new GUIContent(""), GUIStyle.none);

                float rowY = -bannedListScroll.y;
                for (int i = 0; i < bannedList.Count; i++)
                {
                    string entry = bannedList[i];

                    GUI.Label(
                        new Rect(8f, rowY + 6f, width - 100f, 24f),
                        new GUIContent(entry),
                        labelStyle);

                    if (GUI.Button(
                        new Rect(width - 84f, rowY + 4f, 76f, 26f),
                        new GUIContent("UNBAN"),
                        buttonStyle))
                    {
                        bannedUserIds.Remove(entry);
                    }

                    rowY += rowHeight;
                }

                GUI.EndGroup();
            }
        }

        private void SetRewardStatus(string message)
        {
            rewardStatus = message;
            rewardStatusUntil = Time.unscaledTime + 4f;
        }

        private void GiveMyMaxMoney()
        {
            GameObject local = GetLocalPlayer();
            if (local == null)
            {
                SetRewardStatus("Money: local player not found.");
                return;
            }

            MoneyHolder holder = local.GetComponentInChildren<MoneyHolder>(true);
            if (holder == null)
            {
                SetRewardStatus("Money: MoneyHolder not found.");
                return;
            }

            holder.SetMoney(MaxMoneyValue);
            SetRewardStatus("Money set to " + MaxMoneyValue.ToString("0") + ".");
        }

        private void GiveMyMaxStars()
        {
            if (!PhotonNetwork.InRoom)
            {
                SetRewardStatus("Stars: not in a room.");
                return;
            }

            int current = ObjectiveManager.GetStars();
            int amount = MaxStarsValue - current;
            if (amount <= 0)
            {
                SetRewardStatus("Stars are already at max.");
                return;
            }

            ObjectiveManager.GiveStars(amount);
            SetRewardStatus("Stars set to " + MaxStarsValue + ".");
        }

        private void GiveAllMaxMoney()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            Player[] players = PhotonNetwork.PlayerList;
            int changed = 0;

            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                GameObject obj = FindPlayerObject(player);
                if (obj == null)
                    continue;

                MoneyHolder holder = obj.GetComponentInChildren<MoneyHolder>(true);
                if (holder == null || holder.photonView == null)
                    continue;

                if (holder.photonView.IsMine)
                {
                    holder.SetMoney(MaxMoneyValue);
                    changed++;
                    continue;
                }

                float current = holder.GetMoney();
                float add = MaxMoneyValue - current;
                if (add > 0f)
                {
                    holder.photonView.RPC("AddMoney", holder.photonView.Owner, add);
                    changed++;
                }
            }

            SetRewardStatus("Max money requested for " + changed + " player(s).");
        }

        private void StartGiveAllMaxStars()
        {
            if (!PhotonNetwork.InRoom || !PhotonNetwork.IsMasterClient)
                return;

            StartCoroutine(GiveAllMaxStarsRoutine());
        }

        private IEnumerator GiveAllMaxStarsRoutine()
        {
            Player[] players = PhotonNetwork.PlayerList;
            int requested = 0;
            int skipped = 0;

            // ObjectiveManager.GiveStars() only works for the PhotonView owner.
            // Temporarily request ownership where possible, apply the reward,
            // then return ownership to the original player.

            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];

                GameObject obj =
                    FindPlayerObject(player);

                if (obj == null)
                {
                    skipped++;
                    continue;
                }

                ObjectiveManager objective =
                    obj.GetComponentInChildren<ObjectiveManager>(true);

                if (objective == null ||
                    objective.photonView == null)
                {
                    skipped++;
                    continue;
                }

                PhotonView view =
                    objective.photonView;

                Player originalOwner =
                    view.Owner;

                // Already ours.
                if (view.IsMine)
                {
                    int current =
                        ObjectiveManager.GetStars();

                    int amount =
                        MaxStarsValue - current;

                    if (amount > 0)
                        ObjectiveManager.GiveStars(amount);

                    requested++;
                    continue;
                }

                bool transferRequested = false;

                try
                {
                    view.TransferOwnership(
                        PhotonNetwork.LocalPlayer
                    );

                    transferRequested = true;
                }
                catch (Exception ex)
                {
                    Logger.LogWarning(
                        "Give All Max Stars: ownership transfer failed: " +
                        ex.Message
                    );

                    skipped++;
                    continue;
                }

                // Wait for Photon to process the ownership change.
                if (transferRequested)
                    yield return null;

                // Check ownership AFTER the yield.
                if (!view.IsMine)
                {
                    skipped++;
                    continue;
                }

                int ownedCurrent =
                    GetStarsFromObjective(objective);

                int ownedAmount =
                    MaxStarsValue - ownedCurrent;

                if (ownedAmount > 0)
                    ObjectiveManager.GiveStars(
                        ownedAmount
                    );

                requested++;

                // Return ownership to the original owner.
                if (originalOwner != null &&
                    originalOwner != PhotonNetwork.LocalPlayer)
                {
                    try
                    {
                        view.TransferOwnership(
                            originalOwner
                        );
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning(
                            "Give All Max Stars: failed to restore ownership: " +
                            ex.Message
                        );
                    }

                    // Yield OUTSIDE the try/catch.
                    yield return null;
                }
            }

            SetRewardStatus(
                "Max stars requested for " +
                requested +
                " player(s); skipped " +
                skipped +
                "."
            );
        }

        private int GetStarsFromObjective(ObjectiveManager objective)
        {
            if (objective == null)
                return 0;

            MethodInfo getStars = typeof(ObjectiveManager).GetMethod(
                "GetStars",
                BindingFlags.Public | BindingFlags.Static);

            if (getStars != null)
            {
                try
                {
                    object value = getStars.Invoke(null, null);
                    if (value is int)
                        return (int)value;
                }
                catch { }
            }

            FieldInfo starsField = typeof(ObjectiveManager).GetField(
                "stars",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (starsField != null && starsField.FieldType == typeof(int))
            {
                try
                {
                    return (int)starsField.GetValue(objective);
                }
                catch { }
            }

            return 0;
        }

        private IEnumerator LoadBackgroundAfterStartup()
        {
            yield return null;
            yield return new WaitForEndOfFrame();

            LoadMenuBackground();
        }

        private void LoadMenuBackground()
        {
            try
            {
                Assembly asm = Assembly.GetExecutingAssembly();
                string resourceName = null;
                string[] names = asm.GetManifestResourceNames();
                for (int i = 0; i < names.Length; i++)
                {
                    if (names[i].EndsWith("KoboldESP_Background.png", StringComparison.OrdinalIgnoreCase))
                    {
                        resourceName = names[i];
                        break;
                    }
                }

                if (resourceName == null)
                {
                    Logger.LogWarning("Menu background: embedded resource not found.");
                    return;
                }

                using (Stream stream = asm.GetManifestResourceStream(resourceName))
                {
                    if (stream == null) return;

                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    Texture2D tex;
                    if (TryCreateTextureFromBytes(data, out tex))
                        menuBackground = tex;
                    else
                        Logger.LogWarning("Menu background: failed to decode embedded PNG.");
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("Menu background load failed: " + ex);
            }
        }

        private static MethodInfo drawTextureMethod;
        private static bool drawTextureMethodResolved;

        private static void InvokeDrawTexture(Rect position, Texture image, ScaleMode scaleMode)
        {
            if (!drawTextureMethodResolved)
            {
                drawTextureMethodResolved = true;
                try
                {
                    drawTextureMethod = typeof(GUI).GetMethod(
                        "DrawTexture",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(Rect), typeof(Texture), typeof(ScaleMode) },
                        null);
                }
                catch { drawTextureMethod = null; }
            }

            if (drawTextureMethod != null)
            {
                try
                {
                    drawTextureMethod.Invoke(null, new object[] { position, image, scaleMode });
                }
                catch { }
            }
        }

        private static MethodInfo drawTextureWithTexCoordsMethod;
        private static bool drawTextureWithTexCoordsMethodResolved;

        private static void InvokeDrawTextureWithTexCoords(Rect position, Texture image, Rect texCoords)
        {
            if (!drawTextureWithTexCoordsMethodResolved)
            {
                drawTextureWithTexCoordsMethodResolved = true;
                try
                {
                    drawTextureWithTexCoordsMethod = typeof(GUI).GetMethod(
                        "DrawTextureWithTexCoords",
                        BindingFlags.Static | BindingFlags.Public,
                        null,
                        new Type[] { typeof(Rect), typeof(Texture), typeof(Rect) },
                        null);
                }
                catch { drawTextureWithTexCoordsMethod = null; }
            }

            if (drawTextureWithTexCoordsMethod != null)
            {
                try
                {
                    drawTextureWithTexCoordsMethod.Invoke(null, new object[] { position, image, texCoords });
                    return;
                }
                catch { }
            }

            // Fallback: draw the whole texture if tex-coord drawing isn't available.
            InvokeDrawTexture(position, image, ScaleMode.StretchToFill);
        }

        private void DrawMenuBackground()
        {
            if (menuBackground == null)
            {
                Color prev = GUI.color;
                Color tint = Color.HSVToRGB(backgroundHue, MenuSat(0.5f), 0.15f);
                tint.a = backgroundOpacity;
                GUI.color = tint;
                InvokeDrawTexture(new Rect(0f, 0f, menuRect.width, menuRect.height), Texture2D.whiteTexture, ScaleMode.StretchToFill);
                GUI.color = prev;
                return;
            }

            Color bgTint = Color.HSVToRGB(backgroundHue, MenuSat(0.6f), 1f);
            bgTint.a = backgroundOpacity;
            Color prevColor = GUI.color;
            GUI.color = bgTint;

            int frame = Mathf.FloorToInt(Time.unscaledTime * BackgroundFramesPerSecond) % BackgroundFrameCount;

            int col = frame % BackgroundColumns;   // 0-4
            int row = frame / BackgroundColumns;   // 0-11

            float frameW = 1f / BackgroundColumns; // 0.2
            float frameH = 1f / BackgroundRows;    // 0.0833...
            float u = col * frameW;
            float v = 1f - frameH - (row * frameH); // flipped because UV origin is bottom-left

            Rect texCoords = new Rect(u, v, frameW, frameH);

            // Each frame's aspect ratio differs from menuRect's aspect ratio, so cover the
            // menu area (like ScaleMode.ScaleAndCrop) instead of stretching, to avoid distortion.
            float frameAspect = (menuBackground.width * frameW) / (menuBackground.height * frameH);
            float targetAspect = menuRect.width / menuRect.height;

            float drawW = menuRect.width;
            float drawH = menuRect.height;
            float offsetX = 0f;
            float offsetY = 0f;

            if (frameAspect > targetAspect)
            {
                // Frame is wider than target: match height, overflow width, then center horizontally.
                drawW = menuRect.height * frameAspect;
                offsetX = (menuRect.width - drawW) / 2f;
            }
            else
            {
                // Frame is taller than target: match width, overflow height, then center vertically.
                drawH = menuRect.width / frameAspect;
                offsetY = (menuRect.height - drawH) / 2f;
            }

            InvokeDrawTextureWithTexCoords(new Rect(offsetX, offsetY, drawW, drawH), menuBackground, texCoords);
            GUI.color = prevColor;
        }

        private GUIStyle menuBackgroundStyle;
        private readonly Dictionary<int, GameObject> playerObjectCache = new Dictionary<int, GameObject>();

        // ============================================================
        // SPECTATE / PLAYER SELECTION
        // ============================================================
        private Player GetPlayerByActorId(int actorId)
        {
            if (!PhotonNetwork.InRoom || actorId < 0) return null;
            Player[] players = PhotonNetwork.PlayerList;
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null && players[i].ActorNumber == actorId) return players[i];
            return null;
        }

        private void SelectPlayer(Player p, bool force = false)
        {
            if (p == null) return;
            // targetLocked blocks casual selection; force=true for context-menu spectate switches
            if (!force && targetLocked && selectedActorId >= 0 && p.ActorNumber != selectedActorId)
                return;
            selectedActorId = p.ActorNumber;
            selectedPlayer = p;
            notesInputFocused = false;
            if (spectating) StartSpectating();
        }

        private void StartSpectating()
        {
            if (selectedPlayer == null || selectedPlayer.IsLocal) return;

            GameObject target = FindPlayerObject(selectedPlayer);
            Camera cam = Camera.main;
            // Only commit spectate state once we actually have a target
            if (target == null || cam == null) return;

            if (!cameraStateSaved)
            {
                savedCameraPosition = cam.transform.position;
                savedCameraRotation = cam.transform.rotation;
                cameraStateSaved = true;
            }

            spectateActorId = selectedPlayer.ActorNumber;
            spectateTarget = target.transform;
            spectating = true;
        }

        private void StopSpectating()
        {
            Camera cam = Camera.main;
            if (cam != null && cameraStateSaved)
            {
                cam.transform.position = savedCameraPosition;
                cam.transform.rotation = savedCameraRotation;
            }
            spectating = false;
            spectateTarget = null;
            spectateActorId = -1;
            cameraStateSaved = false;
        }

        private void CycleSpectatePlayer(int direction)
        {
            if (!PhotonNetwork.InRoom) return;
            Player[] players = PhotonNetwork.PlayerList;
            if (players == null || players.Length == 0) return;

            int current = -1;
            for (int i = 0; i < players.Length; i++)
                if (players[i] != null && players[i].ActorNumber == selectedActorId) { current = i; break; }

            for (int step = 1; step <= players.Length; step++)
            {
                int index = current < 0 ? (direction > 0 ? 0 : players.Length - 1) :
                    (current + direction * step) % players.Length;
                if (index < 0) index += players.Length;
                Player p = players[index];
                if (p == null || p.IsLocal) continue;
                if (FindPlayerObject(p) == null) continue;
                SelectPlayer(p);
                return;
            }
        }

        private Color GetESPColor(Player player)
        {
            if (player != null && player.ActorNumber == selectedActorId)
                return espColorOptions[Mathf.Clamp(selectedColorIndex, 0, espColorOptions.Length - 1)];
            if (player != null && friendActorIds.Contains(player.ActorNumber))
                return espColorOptions[Mathf.Clamp(friendColorIndex, 0, espColorOptions.Length - 1)];
            return espColorOptions[Mathf.Clamp(normalColorIndex, 0, espColorOptions.Length - 1)];
        }

        private void DrawESPColorControls(float x, float y, float width)
        {
            GUI.Label(new Rect(x, y, width, 22f), new GUIContent("Colors"), headerStyle);
            y += 26f;

            float colW = width / 3f;

            GUI.Label(new Rect(x, y, colW - 8f, 20f), new GUIContent("NORMAL: " + espColorNames[normalColorIndex]), labelStyle);
            if (GUI.Button(new Rect(x, y + 22f, colW - 8f, 26f), new GUIContent("CYCLE"), buttonStyle))
                normalColorIndex = (normalColorIndex + 1) % espColorOptions.Length;

            GUI.Label(new Rect(x + colW, y, colW - 8f, 20f), new GUIContent("SELECTED: " + espColorNames[selectedColorIndex]), labelStyle);
            if (GUI.Button(new Rect(x + colW, y + 22f, colW - 8f, 26f), new GUIContent("CYCLE"), buttonStyle))
                selectedColorIndex = (selectedColorIndex + 1) % espColorOptions.Length;

            GUI.Label(new Rect(x + colW * 2f, y, colW - 8f, 20f), new GUIContent("FRIEND: " + espColorNames[friendColorIndex]), labelStyle);
            if (GUI.Button(new Rect(x + colW * 2f, y + 22f, colW - 8f, 26f), new GUIContent("CYCLE"), buttonStyle))
                friendColorIndex = (friendColorIndex + 1) % espColorOptions.Length;
        }

        // ============================================================
        // PLAYER NOTES
        // ============================================================
        private string GetPlayerNoteKey(Player player)
        {
            if (player == null)
                return null;

            return !string.IsNullOrEmpty(player.UserId) ? player.UserId : GetPlayerName(player);
        }

        private string GetPlayerName(Player player)
        {
            return player == null ? "Unknown" : (string.IsNullOrEmpty(player.NickName) ? "Player " + player.ActorNumber : player.NickName);
        }

        /// <summary>
        /// Strip Unity rich-text tags (&lt;color&gt;, &lt;b&gt;, …) for length checks / plain display.
        /// </summary>
        private static string StripRichText(string s)
        {
            if (string.IsNullOrEmpty(s))
                return "";
            System.Text.StringBuilder sb = new System.Text.StringBuilder(s.Length);
            bool inTag = false;
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                if (c == '<') { inTag = true; continue; }
                if (c == '>' && inTag) { inTag = false; continue; }
                if (!inTag) sb.Append(c);
            }
            return sb.ToString();
        }

        private string GetPlayerNote(Player player)
        {
            string key = GetPlayerNoteKey(player);
            if (key == null)
                return "";

            string note;
            return playerNotes.TryGetValue(key, out note) ? note : "";
        }

        private void SetPlayerNote(Player player, string note)
        {
            string key = GetPlayerNoteKey(player);
            if (key == null)
                return;

            if (string.IsNullOrEmpty(note))
                playerNotes.Remove(key);
            else
                playerNotes[key] = note;
        }

        // ============================================================
        // BAN PERSISTENCE
        // ============================================================
        private void LoadBannedUserIds()
        {
            bannedUserIds.Clear();

            if (configBannedUserIds == null || string.IsNullOrEmpty(configBannedUserIds.Value))
                return;

            string[] parts = configBannedUserIds.Value.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    bannedUserIds.Add(trimmed);
            }
        }

        private void SaveBannedUserIds()
        {
            if (configBannedUserIds == null) return;
            configBannedUserIds.Value = string.Join(",", new List<string>(bannedUserIds).ToArray());
        }

        // ============================================================
        // TELEPORT EXECUTION
        // ============================================================
        private void TryCaptureOrigin()
        {
            if (originCaptured) return;
            GameObject local = GetLocalPlayer();
            if (local == null) return;
            originPosition = local.transform.position;
            originCaptured = true;
        }

        private void TeleportLocalPlayer(Vector3 destination)
        {
            TeleportLocalPlayer(destination, softTeleportEnabled);
        }

        private void TeleportLocalPlayer(Vector3 destination, bool soft)
        {
            GameObject local = GetLocalPlayer();
            if (local == null) return;

            if (soft && softTeleportDuration > 0.01f && local.activeInHierarchy)
            {
                if (softTeleportCoroutine != null)
                    StopCoroutine(softTeleportCoroutine);
                softTeleportCoroutine = StartCoroutine(SoftTeleportRoutine(local, destination));
                return;
            }

            SnapTeleportLocalPlayer(local, destination);
        }

        private void SnapTeleportLocalPlayer(GameObject local, Vector3 destination)
        {
            if (local == null) return;

            CharacterController controller = local.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
                local.transform.position = destination;
                controller.enabled = true;
            }
            else
            {
                local.transform.position = destination;
            }

            // Also zero rigidbodies so momentum doesn't fling you
            Rigidbody[] rbs = local.GetComponentsInChildren<Rigidbody>(true);
            if (rbs != null)
            {
                for (int i = 0; i < rbs.Length; i++)
                {
                    Rigidbody rb = rbs[i];
                    if (rb == null) continue;
                    try
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    catch { }
                }
            }
        }

        private IEnumerator SoftTeleportRoutine(GameObject body, Vector3 destination)
        {
            if (body == null)
            {
                softTeleportCoroutine = null;
                yield break;
            }

            Vector3 start = body.transform.position;
            float t = 0f;
            float dur = Mathf.Max(0.05f, softTeleportDuration);

            CharacterController controller = body.GetComponent<CharacterController>();
            bool hadCc = controller != null;
            if (hadCc) controller.enabled = false;

            while (t < 1f && body != null)
            {
                t += Time.unscaledDeltaTime / dur;
                float s = t * t * (3f - 2f * t); // smoothstep
                Vector3 pos = Vector3.Lerp(start, destination, Mathf.Clamp01(s));
                body.transform.position = pos;
                yield return null;
            }

            if (body != null)
            {
                body.transform.position = destination;
                if (hadCc && controller != null)
                    controller.enabled = true;
            }

            softTeleportCoroutine = null;
        }

        private GameObject GetLocalPlayer()
        {
            if (!PhotonNetwork.InRoom)
            {
                cachedLocalPlayer = null;
                return null;
            }

            // Drop stale / non-kobold cache (this was picking up bananas, doors, etc.)
            if (cachedLocalPlayer != null)
            {
                if (cachedLocalPlayer == null || !IsValidPlayerKoboldObject(cachedLocalPlayer))
                    cachedLocalPlayer = null;
                else
                    return cachedLocalPlayer;
            }

            // Prefer official TagObject (Kobold component or its GameObject)
            if (PhotonNetwork.LocalPlayer != null)
            {
                object tag = PhotonNetwork.LocalPlayer.TagObject;
                if (tag != null)
                {
                    ResolveGeneTypes();
                    if (koboldType != null && koboldType.IsInstanceOfType(tag))
                    {
                        cachedLocalPlayer = ((Component)tag).gameObject;
                        return cachedLocalPlayer;
                    }

                    Component asComp = tag as Component;
                    if (asComp != null && IsValidPlayerKoboldObject(asComp.gameObject))
                    {
                        Component kob = GetKoboldOn(asComp.gameObject);
                        cachedLocalPlayer = kob != null ? kob.gameObject : asComp.gameObject;
                        return cachedLocalPlayer;
                    }

                    GameObject asGo = tag as GameObject;
                    if (asGo != null && IsValidPlayerKoboldObject(asGo))
                    {
                        Component kob = GetKoboldOn(asGo);
                        cachedLocalPlayer = kob != null ? kob.gameObject : asGo;
                        return cachedLocalPlayer;
                    }
                }
            }

            // Scan owned PhotonViews that are actual Kobolds (never props)
            PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            if (views != null)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    PhotonView view = views[i];
                    if (view == null || !view.IsMine)
                        continue;
                    Component kob = GetKoboldOn(view.gameObject);
                    if (kob != null)
                    {
                        cachedLocalPlayer = kob.gameObject;
                        return cachedLocalPlayer;
                    }
                }
            }

            return null;
        }

        private void TeleportBehindTarget()
        {
            if (selectedPlayer == null) return;
            GameObject target = FindPlayerObject(selectedPlayer);
            if (target == null) return;
            Vector3 dest = target.transform.position - target.transform.forward * behindDistance;
            TeleportLocalPlayer(dest);
        }

        private void TeleportInFrontOfTarget()
        {
            if (selectedPlayer == null) return;
            GameObject target = FindPlayerObject(selectedPlayer);
            if (target == null) return;
            Vector3 dest = target.transform.position + target.transform.forward * frontDistance;
            TeleportLocalPlayer(dest);
        }

        private void TeleportAboveTarget()
        {
            if (selectedPlayer == null) return;
            GameObject target = FindPlayerObject(selectedPlayer);
            if (target == null) return;
            Vector3 dest = target.transform.position + Vector3.up * aboveDistance;
            TeleportLocalPlayer(dest);
        }

        private GameObject GetTagObject(Player player)
        {
            if (player == null || player.TagObject == null)
                return null;

            GameObject tagged = player.TagObject as GameObject;
            if (tagged != null)
                return tagged;

            Component component = player.TagObject as Component;
            return component != null ? component.gameObject : null;
        }

        private GameObject GetPlayerRoot(PhotonView view)
        {
            if (view == null)
                return null;

            return view.gameObject;
        }

        private void RefreshPlayerObjectCache()
        {
            if (Time.unscaledTime < nextPlayerCacheRefresh)
                return;

            nextPlayerCacheRefresh = Time.unscaledTime + PlayerCacheRefreshInterval;

            if (!PhotonNetwork.InRoom)
            {
                cachedLocalPlayer = null;
                playerObjectCache.Clear();
                return;
            }

            Player[] players = PhotonNetwork.PlayerList;

            // Fast path: use Photon TagObject whenever available.
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player == null)
                    continue;

                GameObject tagged = GetTagObject(player);
                if (tagged == null)
                    continue;

                playerObjectCache[player.ActorNumber] = tagged;

                if (player.IsLocal)
                    cachedLocalPlayer = tagged;
            }

            // Fallback: only map PhotonViews that are actual Kobolds (never bananas/doors/props).
            PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            if (views != null)
            {
                for (int i = 0; i < views.Length; i++)
                {
                    PhotonView view = views[i];
                    if (view == null)
                        continue;

                    GameObject root = GetPlayerRoot(view);
                    if (root == null || !IsValidPlayerKoboldObject(root))
                        continue;

                    Component kob = GetKoboldOn(root);
                    GameObject kobGo = kob != null ? kob.gameObject : root;

                    if (view.IsMine && (cachedLocalPlayer == null || !IsValidPlayerKoboldObject(cachedLocalPlayer)))
                        cachedLocalPlayer = kobGo;

                    if (view.Owner != null)
                    {
                        int actorId = view.Owner.ActorNumber;
                        if (!playerObjectCache.ContainsKey(actorId) || !IsValidPlayerKoboldObject(playerObjectCache[actorId]))
                            playerObjectCache[actorId] = kobGo;
                    }

                    if (view.Controller != null)
                    {
                        int actorId = view.Controller.ActorNumber;
                        if (!playerObjectCache.ContainsKey(actorId) || !IsValidPlayerKoboldObject(playerObjectCache[actorId]))
                            playerObjectCache[actorId] = kobGo;
                    }
                }
            }

            // Validate TagObject entries too
            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player == null) continue;
                GameObject tagged = GetTagObject(player);
                if (tagged != null && IsValidPlayerKoboldObject(tagged))
                {
                    Component kob = GetKoboldOn(tagged);
                    playerObjectCache[player.ActorNumber] = kob != null ? kob.gameObject : tagged;
                    if (player.IsLocal)
                        cachedLocalPlayer = playerObjectCache[player.ActorNumber];
                }
            }

            HashSet<int> validActors = new HashSet<int>();
            for (int i = 0; i < players.Length; i++)
            {
                if (players[i] != null)
                    validActors.Add(players[i].ActorNumber);
            }

            List<int> cachedActors = new List<int>(playerObjectCache.Keys);
            for (int i = 0; i < cachedActors.Count; i++)
            {
                int id = cachedActors[i];
                if (!validActors.Contains(id))
                {
                    playerObjectCache.Remove(id);
                    continue;
                }
                GameObject go;
                if (playerObjectCache.TryGetValue(id, out go) && !IsValidPlayerKoboldObject(go))
                    playerObjectCache.Remove(id);
            }

            if (cachedLocalPlayer != null && !IsValidPlayerKoboldObject(cachedLocalPlayer))
                cachedLocalPlayer = null;
        }

        private GameObject FindPlayerObject(Player player)
        {
            if (player == null)
                return null;

            GameObject tagged = GetTagObject(player);
            if (tagged != null && IsValidPlayerKoboldObject(tagged))
            {
                Component kob = GetKoboldOn(tagged);
                GameObject go = kob != null ? kob.gameObject : tagged;
                playerObjectCache[player.ActorNumber] = go;
                return go;
            }

            GameObject cached;
            if (playerObjectCache.TryGetValue(player.ActorNumber, out cached))
            {
                if (cached != null && IsValidPlayerKoboldObject(cached))
                    return cached;

                playerObjectCache.Remove(player.ActorNumber);
            }

            // Scan PhotonViews owned/created by this actor (TagObject is often null on join)
            try
            {
                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views != null)
                {
                    int actor = player.ActorNumber;
                    for (int i = 0; i < views.Length; i++)
                    {
                        PhotonView view = views[i];
                        if (view == null || view.gameObject == null) continue;

                        bool owned = false;
                        try
                        {
                            if (view.Owner != null && view.Owner.ActorNumber == actor)
                                owned = true;
                            else if (view.OwnerActorNr == actor)
                                owned = true;
                            else if (view.CreatorActorNr == actor)
                                owned = true;
                        }
                        catch { }

                        if (!owned) continue;
                        if (!IsValidPlayerKoboldObject(view.gameObject) && GetKoboldOn(view.gameObject) == null)
                            continue;

                        Component kob = GetKoboldOn(view.gameObject);
                        GameObject go = kob != null ? kob.gameObject : view.gameObject;
                        playerObjectCache[actor] = go;
                        return go;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("FindPlayerObject scan: " + ex.Message);
            }

            return null;
        }

        private void TeleportToOrigin()
        {
            if (!originCaptured) return;
            TeleportLocalPlayer(originPosition);
        }

        private void LoadFavoritePrefabNames()
        {
            favoritePrefabNames.Clear();

            if (configFavoritePrefabNames == null || string.IsNullOrEmpty(configFavoritePrefabNames.Value))
                return;

            string[] parts = configFavoritePrefabNames.Value.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    favoritePrefabNames.Add(trimmed);
            }
        }

        private void SaveFavoritePrefabNames()
        {
            if (configFavoritePrefabNames == null) return;
            configFavoritePrefabNames.Value = string.Join(",", new List<string>(favoritePrefabNames).ToArray());
        }

        private void LoadFavoriteRoomNames()
        {
            favoriteRoomNames.Clear();
            if (configFavoriteRoomNames == null || string.IsNullOrEmpty(configFavoriteRoomNames.Value))
                return;
            string[] parts = configFavoriteRoomNames.Value.Split(',');
            for (int i = 0; i < parts.Length; i++)
            {
                string trimmed = parts[i].Trim();
                if (!string.IsNullOrEmpty(trimmed))
                    favoriteRoomNames.Add(trimmed);
            }
        }

        private void SaveFavoriteRoomNames()
        {
            if (configFavoriteRoomNames == null) return;
            configFavoriteRoomNames.Value = string.Join(",", new List<string>(favoriteRoomNames).ToArray());
        }

        // ============================================================
        // TESTING HELPERS — offline host / ownership + world ping
        // ============================================================
        /// <summary>
        /// Solo offline room so you are always Master Client. Good for testing host tools.
        /// </summary>
        private void ForceOfflineSoloHost()
        {
            try
            {
                if (PhotonNetwork.InRoom)
                {
                    try { LeaveRoomSafe(); } catch { }
                }

                PhotonNetwork.OfflineMode = true;

                // OfflineMode auto-connects a fake local server; create/join a room.
                if (!PhotonNetwork.InRoom)
                {
                    RoomOptions opts = new RoomOptions
                    {
                        MaxPlayers = 8,
                        IsVisible = false,
                        IsOpen = true
                    };
                    PhotonNetwork.CreateRoom("ZexQoL_Offline_" + UnityEngine.Random.Range(1000, 9999), opts, TypedLobby.Default);
                }

                ownershipStatus = "OfflineMode ON · InRoom=" + PhotonNetwork.InRoom +
                                  " · Master=" + PhotonNetwork.IsMasterClient +
                                  " · (host tools should unlock)";
                ownershipStatusUntil = Time.unscaledTime + 8f;
                Logger.LogInfo("ForceOfflineSoloHost: " + ownershipStatus);
            }
            catch (Exception ex)
            {
                ownershipStatus = "Offline host failed: " + ex.Message;
                ownershipStatusUntil = Time.unscaledTime + 6f;
                Logger.LogWarning("ForceOfflineSoloHost: " + ex);
            }
        }

        /// <summary>
        /// Try to become Master Client. Works reliably offline / when already alone;
        /// online only the current master can transfer (Photon rule).
        /// </summary>
        private void TryClaimMasterClient()
        {
            try
            {
                if (!PhotonNetwork.InRoom)
                {
                    ownershipStatus = "Not in a room — use FORCE OFFLINE + ROOM first.";
                    ownershipStatusUntil = Time.unscaledTime + 5f;
                    return;
                }

                if (PhotonNetwork.IsMasterClient)
                {
                    ownershipStatus = "Already Master Client.";
                    ownershipStatusUntil = Time.unscaledTime + 4f;
                    return;
                }

                // Offline / single-player style rooms: this usually works.
                // Online multiplayer: only current master can SetMasterClient.
                bool ok = PhotonNetwork.SetMasterClient(PhotonNetwork.LocalPlayer);
                ownershipStatus = ok
                    ? ("SetMasterClient sent · now Master=" + PhotonNetwork.IsMasterClient)
                    : ("SetMasterClient returned false · still Master=" + PhotonNetwork.IsMasterClient +
                       " (online: only current host can transfer)");
                ownershipStatusUntil = Time.unscaledTime + 6f;
                Logger.LogInfo("TryClaimMasterClient: ok=" + ok + " isMaster=" + PhotonNetwork.IsMasterClient);
            }
            catch (Exception ex)
            {
                ownershipStatus = "Claim master error: " + ex.Message;
                ownershipStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("TryClaimMasterClient: " + ex);
            }
        }

        private void RequestLocalKoboldOwnership()
        {
            try
            {
                Component kob = FindLocalKobold();
                if (kob == null)
                {
                    ownershipStatus = "No local Kobold found.";
                    ownershipStatusUntil = Time.unscaledTime + 4f;
                    return;
                }

                PhotonView pv = kob.GetComponent<PhotonView>() ?? kob.GetComponentInParent<PhotonView>();
                if (pv == null)
                {
                    ownershipStatus = "Kobold has no PhotonView.";
                    ownershipStatusUntil = Time.unscaledTime + 4f;
                    return;
                }

                int before = pv.OwnerActorNr;
                bool wasMine = pv.IsMine;
                pv.RequestOwnership();

                ownershipStatus = "RequestOwnership · view=" + pv.ViewID +
                                  " wasMine=" + wasMine +
                                  " owner=" + before +
                                  " local=" + (PhotonNetwork.LocalPlayer != null ? PhotonNetwork.LocalPlayer.ActorNumber : -1);
                ownershipStatusUntil = Time.unscaledTime + 6f;
                Logger.LogInfo("RequestLocalKoboldOwnership: ViewID=" + pv.ViewID +
                               " wasMine=" + wasMine + " owner=" + before);
            }
            catch (Exception ex)
            {
                ownershipStatus = "Ownership error: " + ex.Message;
                ownershipStatusUntil = Time.unscaledTime + 5f;
                Logger.LogWarning("RequestLocalKoboldOwnership: " + ex);
            }
        }

        /// <summary>
        /// Request ownership on every PhotonView that currently reports IsMine or is on local kobold tree.
        /// </summary>
        private void RequestOwnershipAllLocalViews()
        {
            try
            {
                int n = 0;
                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views != null)
                {
                    for (int i = 0; i < views.Length; i++)
                    {
                        PhotonView pv = views[i];
                        if (pv == null) continue;
                        // Local-ish: already mine, or no owner, or on our kobold
                        bool localish = pv.IsMine;
                        if (!localish && PhotonNetwork.LocalPlayer != null &&
                            pv.OwnerActorNr == PhotonNetwork.LocalPlayer.ActorNumber)
                            localish = true;
                        if (!localish)
                        {
                            Component kob = GetKoboldOn(pv.gameObject);
                            if (kob != null)
                            {
                                Component mine = FindLocalKobold();
                                if (mine != null && (kob == mine || kob.transform.IsChildOf(mine.transform) ||
                                    mine.transform.IsChildOf(kob.transform)))
                                    localish = true;
                            }
                        }
                        if (!localish) continue;
                        pv.RequestOwnership();
                        n++;
                    }
                }

                ownershipStatus = "Requested ownership on " + n + " local-ish views · Master=" +
                                  PhotonNetwork.IsMasterClient;
                ownershipStatusUntil = Time.unscaledTime + 6f;
                Logger.LogInfo("RequestOwnershipAllLocalViews: " + n);
            }
            catch (Exception ex)
            {
                ownershipStatus = "Own-all error: " + ex.Message;
                ownershipStatusUntil = Time.unscaledTime + 5f;
            }
        }

        private void PlacePingMark()
        {
            Vector3 pos;
            GameObject local = GetLocalPlayer();
            if (local != null)
                pos = local.transform.position;
            else if (Camera.main != null)
                pos = Camera.main.transform.position;
            else
            {
                ownershipStatus = "Ping failed: no position.";
                ownershipStatusUntil = Time.unscaledTime + 3f;
                return;
            }

            pingMarkWorld = pos;
            pingMarkUntil = Time.unscaledTime + PingMarkDuration;
            pingMarkActive = true;
            Logger.LogInfo("Ping mark @ " + pos);
        }

        private void DrawPingMark()
        {
            if (!pingMarkActive)
                return;

            if (Time.unscaledTime > pingMarkUntil)
            {
                pingMarkActive = false;
                return;
            }

            Camera cam = Camera.main;
            if (cam == null)
                return;

            // Vertical beam in world → screen (ground to head height)
            Vector3 baseW = pingMarkWorld;
            Vector3 topW = pingMarkWorld + Vector3.up * 2.2f;
            Vector3 baseS = cam.WorldToScreenPoint(baseW);
            Vector3 topS = cam.WorldToScreenPoint(topW);

            if (baseS.z <= 0f && topS.z <= 0f)
                return;

            Vector2 baseGui = new Vector2(baseS.x, Screen.height - baseS.y);
            Vector2 topGui = new Vector2(topS.x, Screen.height - topS.y);

            float t = 1f - Mathf.Clamp01((pingMarkUntil - Time.unscaledTime) / PingMarkDuration);
            // pulse alpha
            float pulse = 0.55f + 0.45f * Mathf.Abs(Mathf.Sin(Time.unscaledTime * 6f));
            Color col = new Color(1f, 0.85f, 0.15f, pulse);

            if (baseS.z > 0f && topS.z > 0f)
                DrawTracerLine(baseGui, topGui, col);

            if (topS.z > 0f)
            {
                // Cross at tip
                float arm = 10f;
                DrawTracerLine(topGui + new Vector2(-arm, 0f), topGui + new Vector2(arm, 0f), col);
                DrawTracerLine(topGui + new Vector2(0f, -arm), topGui + new Vector2(0f, arm), col);

                float left = Mathf.Max(0f, pingMarkUntil - Time.unscaledTime);
                string label = "PING  " + left.ToString("0.0") + "s";
                GUIStyle st = smallStyle != null ? smallStyle : GUI.skin.label;
                Vector2 sz = st.CalcSize(new GUIContent(label));
                GUI.color = col;
                GUI.Label(new Rect(topGui.x - sz.x * 0.5f, topGui.y - sz.y - 4f, sz.x, sz.y), label, st);
                GUI.color = Color.white;
            }
        }

        // ============================================================
        // FLYING NOCLIP (CharCon-style: Kobold.body.velocity)
        // ============================================================
        private void ToggleFlyingNoclip()
        {
            if (flyingNoclipActive)
                DisableFlyingNoclip();
            else
                EnableFlyingNoclip();
        }

        /// <summary>
        /// CharCon / BepInEx UnityInput wrapper.
        /// CharCon uses: UnityInput.Current.GetKey("W"), GetKeyDown(KeyCode), etc.
        /// Resolved once via reflection so we work with BepInEx.Core / BepInEx.UnityInput.
        /// </summary>
        private static class ZexInput
        {
            private static bool resolved;
            private static object current; // UnityInput.Current instance
            private static MethodInfo miGetKeyString;
            private static MethodInfo miGetKeyKeyCode;
            private static MethodInfo miGetKeyDownKeyCode;
            private static MethodInfo miGetKeyUpKeyCode;

            private static void Ensure()
            {
                if (resolved) return;
                resolved = true;
                try
                {
                    Type t = SafeGameType("UnityInput")
                          ?? SafeGameType("BepInEx.UnityInput");
                    if (t == null) return;

                    PropertyInfo prop = AccessTools.Property(t, "Current")
                                     ?? AccessTools.Property(t, "current");
                    if (prop == null) return;

                    current = prop.GetValue(null, null);
                    if (current == null) return;

                    Type ct = current.GetType();
                    miGetKeyString = AccessTools.Method(ct, "GetKey", new Type[] { typeof(string) });
                    miGetKeyKeyCode = AccessTools.Method(ct, "GetKey", new Type[] { typeof(KeyCode) });
                    miGetKeyDownKeyCode = AccessTools.Method(ct, "GetKeyDown", new Type[] { typeof(KeyCode) });
                    miGetKeyUpKeyCode = AccessTools.Method(ct, "GetKeyUp", new Type[] { typeof(KeyCode) });
                }
                catch { }
            }

            public static bool GetKey(string name)
            {
                Ensure();
                try
                {
                    if (current != null && miGetKeyString != null)
                    {
                        object r = miGetKeyString.Invoke(current, new object[] { name });
                        if (r is bool b) return b;
                    }
                }
                catch { }
                // Fallback: map common CharCon strings
                try
                {
                    if (name == "W") return Input.GetKey(KeyCode.W);
                    if (name == "A") return Input.GetKey(KeyCode.A);
                    if (name == "S") return Input.GetKey(KeyCode.S);
                    if (name == "D") return Input.GetKey(KeyCode.D);
                    if (name == "Space") return Input.GetKey(KeyCode.Space);
                    if (name == "LeftShift") return Input.GetKey(KeyCode.LeftShift);
                    if (name == "RightShift") return Input.GetKey(KeyCode.RightShift);
                    if (name == "LeftControl") return Input.GetKey(KeyCode.LeftControl);
                }
                catch { }
                return false;
            }

            public static bool GetKey(KeyCode code)
            {
                if (code == KeyCode.None) return false;
                Ensure();
                try
                {
                    if (current != null && miGetKeyKeyCode != null)
                    {
                        object r = miGetKeyKeyCode.Invoke(current, new object[] { code });
                        if (r is bool b) return b;
                    }
                }
                catch { }
                try { return Input.GetKey(code); } catch { return false; }
            }

            public static bool GetKeyDown(KeyCode code)
            {
                if (code == KeyCode.None) return false;
                Ensure();
                try
                {
                    if (current != null && miGetKeyDownKeyCode != null)
                    {
                        object r = miGetKeyDownKeyCode.Invoke(current, new object[] { code });
                        if (r is bool b) return b;
                    }
                }
                catch { }
                try { return Input.GetKeyDown(code); } catch { return false; }
            }

            public static bool GetKeyUp(KeyCode code)
            {
                // Game's UnityEngine.Input has no GetKeyUp — UnityInput only.
                if (code == KeyCode.None) return false;
                Ensure();
                try
                {
                    if (current != null && miGetKeyUpKeyCode != null)
                    {
                        object r = miGetKeyUpKeyCode.Invoke(current, new object[] { code });
                        if (r is bool b) return b;
                    }
                }
                catch { }
                return false;
            }

            /// <summary>
            /// KeyboardShortcut down — CharCon uses shortcut.IsDown() for multi-key;
            /// we evaluate MainKey + modifiers through UnityInput so it stays consistent.
            /// </summary>
            public static bool ShortcutDown(KeyboardShortcut shortcut)
            {
                if (shortcut.MainKey == KeyCode.None)
                    return false;

                if (!GetKeyDown(shortcut.MainKey))
                    return false;

                try
                {
                    System.Collections.Generic.IEnumerable<KeyCode> mods = shortcut.Modifiers;
                    if (mods != null)
                    {
                        foreach (KeyCode mod in mods)
                        {
                            if (mod != KeyCode.None && !GetKey(mod))
                                return false;
                        }
                    }
                }
                catch
                {
                    // Older BepInEx: fall back to built-in IsDown
                    try { return shortcut.IsDown(); } catch { return false; }
                }
                return true;
            }
        }

        private Rigidbody ResolveKoboldBody()
        {
            if (flyCachedBody != null)
                return flyCachedBody;

            ResolveGeneTypes();
            Component kob = FindLocalKobold();
            if (kob == null)
            {
                flyDebugStatus = "no Kobold";
                return null;
            }

            if (koboldBodyField == null && koboldType != null)
            {
                koboldBodyField = AccessTools.Field(koboldType, "body");
                if (koboldBodyField == null)
                    koboldBodyField = AccessTools.Field(koboldType, "Body");
            }

            if (koboldBodyField != null)
            {
                try
                {
                    object v = koboldBodyField.GetValue(kob);
                    Rigidbody rb = v as Rigidbody;
                    if (rb != null)
                    {
                        flyCachedBody = rb;
                        flyDebugStatus = "body field OK";
                        return rb;
                    }
                }
                catch (Exception ex)
                {
                    flyDebugStatus = "body field err: " + ex.Message;
                }
            }

            Rigidbody direct = kob.GetComponent<Rigidbody>();
            if (direct != null)
            {
                flyCachedBody = direct;
                flyDebugStatus = "RB on Kobold";
                return direct;
            }
            Rigidbody child = kob.GetComponentInChildren<Rigidbody>();
            if (child != null)
            {
                flyCachedBody = child;
                flyDebugStatus = "RB in children";
                return child;
            }

            flyDebugStatus = "no Rigidbody";
            return null;
        }

        private void CacheAndDisableKoboldController(Component kob)
        {
            flyCachedKoboldController = null;
            if (kob == null) return;

            Type kccType = SafeGameType("KoboldCharacterController");
            if (kccType == null) return;

            Component c = kob.GetComponent(kccType);
            if (c == null)
                c = kob.GetComponentInChildren(kccType, true);
            if (c == null) return;

            Behaviour b = c as Behaviour;
            if (b == null) return;

            flyCachedKoboldController = b;
            flyKoboldControllerWasEnabled = b.enabled;
            b.enabled = false; // stop grounded/walk from overwriting velocity
        }

        private void EnableFlyingNoclip()
        {
            if (flyingNoclipActive) return;

            if (spectating)
                StopSpectating();
            followPlayerActorId = -1;

            flyCachedBody = null;
            Rigidbody body = ResolveKoboldBody();
            if (body == null)
            {
                Logger.LogWarning("Fly: could not find Kobold.body Rigidbody (" + flyDebugStatus + ")");
                return;
            }

            Component kob = FindLocalKobold();
            CacheAndDisableKoboldController(kob);

            // Physics setup — velocity only works on non-kinematic bodies
            try
            {
                if (body.isKinematic)
                    body.isKinematic = false;
            }
            catch { }

            flySavedPosition = body.position;
            // CharCon: body.detectCollisions = noclipCheck (old) then flip → false on enable
            body.detectCollisions = false;

            flyingNoclipActive = true;
            flyHasPendingVelocity = false;
            flyDebugStatus = "FLYING spd=" + flySpeed.ToString("0") + " body=" + body.name;
            Logger.LogInfo("Flying noclip ON — " + flyDebugStatus);
        }

        private void DisableFlyingNoclip()
        {
            if (!flyingNoclipActive) return;
            flyingNoclipActive = false;
            flyHasPendingVelocity = false;

            Rigidbody body = flyCachedBody != null ? flyCachedBody : ResolveKoboldBody();
            if (body != null)
            {
                body.detectCollisions = true;
                try { body.velocity = Vector3.zero; } catch { }
            }

            if (flyCachedKoboldController != null)
            {
                flyCachedKoboldController.enabled = flyKoboldControllerWasEnabled;
                flyCachedKoboldController = null;
            }

            flyCachedBody = null;
            flyDebugStatus = "off";
            Logger.LogInfo("Flying noclip OFF");
        }

        /// <summary>
        /// Exact CharCon CharFunc.Noclip (Update):
        ///   UnityInput.Current.GetKey("W"/"A"/"S"/"D"/"Space"/"LeftShift")
        ///   velocity = cam.rotation * axis; velocity.y += 0.2f; body.velocity = velocity
        /// Camera = OrbitCamera look (Camera.main).
        /// </summary>
        private void UpdateFlyingNoclipInput()
        {
            if (!flyingNoclipActive) return;

            Camera cam = Camera.main;
            if (cam == null) return;

            float speed = flySpeed;
            float x = 0f, y = 0f, z = 0f;

            // CharCon: UnityInput.Current.GetKey("D") etc.
            if (ZexInput.GetKey("D")) x += speed;
            if (ZexInput.GetKey("A")) x -= speed;
            if (ZexInput.GetKey("W")) z += speed;
            if (ZexInput.GetKey("S")) z -= speed;
            if (ZexInput.GetKey("LeftShift")) y -= speed;
            if (ZexInput.GetKey("Space")) y += speed;

            Vector3 axis = new Vector3(x, y, z);
            Vector3 moveThere = cam.transform.rotation * axis;
            moveThere.y += 0.2f;

            flyPendingVelocity = moveThere;
            flyHasPendingVelocity = true;
            ApplyFlyVelocity();
        }

        private void FixedUpdate()
        {
            if (flyingNoclipActive)
                ApplyFlyVelocity();
        }

        private void ApplyFlyVelocity()
        {
            if (!flyingNoclipActive || !flyHasPendingVelocity) return;

            Rigidbody body = flyCachedBody != null ? flyCachedBody : ResolveKoboldBody();
            if (body == null)
            {
                DisableFlyingNoclip();
                return;
            }

            if (flyCachedKoboldController != null && flyCachedKoboldController.enabled)
                flyCachedKoboldController.enabled = false;

            body.detectCollisions = false;
            try
            {
                if (body.isKinematic)
                    body.isKinematic = false;
                body.velocity = flyPendingVelocity;
            }
            catch (Exception ex)
            {
                flyDebugStatus = "vel err: " + ex.Message;
            }
        }

        // ============================================================
        // WAYPOINTS
        // ============================================================
        private void QuickSaveWaypoint()
        {
            GameObject local = GetLocalPlayer();
            Vector3 pos;
            if (local != null)
                pos = local.transform.position;
            else if (Camera.main != null)
                pos = Camera.main.transform.position;
            else
                return;

            string name = "WP" + waypointAutoIndex;
            while (savedWaypoints.ContainsKey(name))
            {
                waypointAutoIndex++;
                name = "WP" + waypointAutoIndex;
            }
            savedWaypoints[name] = pos;
            waypointAutoIndex++;
            newWaypointName = name;
            Logger.LogInfo("Waypoint saved: " + name + " @ " + pos);
        }

        private void SaveNamedWaypoint()
        {
            string name = string.IsNullOrEmpty(newWaypointName) ? null : newWaypointName.Trim();
            if (string.IsNullOrEmpty(name))
            {
                QuickSaveWaypoint();
                return;
            }

            GameObject local = GetLocalPlayer();
            if (local == null && Camera.main == null) return;
            Vector3 pos = local != null ? local.transform.position : Camera.main.transform.position;
            savedWaypoints[name] = pos;
        }

        private void TeleportToWaypoint(string name)
        {
            Vector3 pos;
            if (!savedWaypoints.TryGetValue(name, out pos)) return;
            TeleportLocalPlayer(pos);
        }

        // ============================================================
        // HOST: BRING / FREEZE
        // ============================================================
        private void BringPlayerToMe(Player target)
        {
            if (target == null || target.IsLocal) return;
            if (!PhotonNetwork.IsMasterClient) return;

            GameObject me = GetLocalPlayer();
            GameObject them = FindPlayerObject(target);
            if (me == null || them == null) return;

            Vector3 dest = me.transform.position + me.transform.forward * 1.5f;
            ApplyTeleportToBody(them, dest, them.transform.rotation);
            AddRecentPlayerEvent("BROUGHT: " + GetPlayerName(target) + " → me");
        }

        private void BringAllPlayersToMe()
        {
            if (!PhotonNetwork.IsMasterClient || !PhotonNetwork.InRoom) return;
            Player[] players = PhotonNetwork.PlayerList;
            if (players == null) return;
            for (int i = 0; i < players.Length; i++)
            {
                Player p = players[i];
                if (p == null || p.IsLocal) continue;
                BringPlayerToMe(p);
            }
            AddRecentPlayerEvent("BROUGHT ALL to host");
        }

        private void ToggleFreezePlayer(Player target)
        {
            if (target == null || target.IsLocal) return;
            int id = target.ActorNumber;
            if (frozenActorIds.Contains(id))
            {
                frozenActorIds.Remove(id);
                frozenPlayerPositions.Remove(id);
                AddRecentPlayerEvent("UNFROZE: " + GetPlayerName(target));
            }
            else
            {
                GameObject obj = FindPlayerObject(target);
                if (obj == null) return;
                frozenActorIds.Add(id);
                frozenPlayerPositions[id] = obj.transform.position;
                AddRecentPlayerEvent("FROZE: " + GetPlayerName(target));
            }
        }

        private void UpdateFrozenPlayers()
        {
            if (frozenActorIds.Count == 0) return;
            if (!PhotonNetwork.InRoom)
            {
                frozenActorIds.Clear();
                frozenPlayerPositions.Clear();
                return;
            }

            // Host (or anyone who can see the object) keeps shoving them back
            List<int> ids = new List<int>(frozenActorIds);
            for (int i = 0; i < ids.Count; i++)
            {
                int id = ids[i];
                Player p = GetPlayerByActorId(id);
                if (p == null)
                {
                    frozenActorIds.Remove(id);
                    frozenPlayerPositions.Remove(id);
                    continue;
                }

                GameObject obj = FindPlayerObject(p);
                if (obj == null) continue;

                Vector3 locked;
                if (!frozenPlayerPositions.TryGetValue(id, out locked))
                {
                    locked = obj.transform.position;
                    frozenPlayerPositions[id] = locked;
                }

                if ((obj.transform.position - locked).sqrMagnitude > 0.01f)
                    ApplyTeleportToBody(obj, locked, obj.transform.rotation);
            }
        }

        private bool IsPlayerFrozen(Player p)
        {
            return p != null && frozenActorIds.Contains(p.ActorNumber);
        }

        // ============================================================
        // COLLAPSIBLE SECTIONS HELPER
        // ============================================================
        private bool DrawCollapsibleHeader(string key, string title, float x, float y, float width)
        {
            if (!sectionCollapsed.ContainsKey(key))
                sectionCollapsed[key] = false;

            bool collapsed = sectionCollapsed[key];
            string label = (collapsed ? "▶ " : "▼ ") + title;
            if (GUI.Button(new Rect(x, y, width, 26f), new GUIContent(label), buttonStyle))
                sectionCollapsed[key] = !collapsed;

            return !sectionCollapsed[key];
        }

        private Texture2D CreateUIColor(Color color)
        {
            Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            texture.hideFlags = HideFlags.HideAndDontSave;
            texture.SetPixel(0, 0, color);
            texture.Apply();
            return texture;
        }

        private void CreateStyles()
        {
            if (stylesCreated)
                return;

            /*
             * IMPORTANT:
             * KoboldKare's referenced UnityEngine GUI API is more limited
             * than the normal Unity IMGUI API. In this project:
             *
             *   GUIStyle.border       -> read-only
             *   GUIStyle.padding      -> read-only
             *   GUIStyle.hover        -> unavailable
             *   GUIStyle.active       -> unavailable
             *   GUIStyleState.background -> unavailable
             *   RectOffset(4 args)    -> unavailable
             *
             * Therefore the UI uses only the GUIStyle members that this
             * game's actual references expose. Visual color/background
             * differences are applied with GUI.color in the drawing code.
             */

            Color accent = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), 0.72f, 1f);
            Color muted = Color.HSVToRGB(Mathf.Repeat(backgroundHue, 1f), 0.18f, 0.72f);
            lastStyledHue = backgroundHue;

            // Window — use box (not skin.window) to avoid the default black title bar chrome.
            windowStyle = new GUIStyle(GUI.skin.box);
            windowStyle.fontSize = 12;
            windowStyle.alignment = TextAnchor.UpperLeft;
            windowStyle.normal.textColor = Color.white;

            // Sidebar
            sidebarStyle = new GUIStyle(GUI.skin.button);
            sidebarStyle.fontSize = 12;
            sidebarStyle.alignment = TextAnchor.MiddleLeft;
            sidebarStyle.normal.textColor = new Color(0.72f, 0.74f, 0.80f);

            // Selected sidebar item.
            sidebarSelectedStyle = new GUIStyle(sidebarStyle);
            sidebarSelectedStyle.normal.textColor = Color.white;
            sidebarSelectedStyle.fontStyle = FontStyle.Bold;

            // Top bar.
            topBarStyle = new GUIStyle(GUI.skin.box);
            topBarStyle.fontSize = 11;
            topBarStyle.alignment = TextAnchor.MiddleLeft;
            topBarStyle.normal.textColor = new Color(0.78f, 0.80f, 0.86f);

            // Cards.
            cardStyle = new GUIStyle(GUI.skin.box);
            cardStyle.fontSize = 11;
            cardStyle.alignment = TextAnchor.UpperLeft;
            cardStyle.normal.textColor = new Color(0.88f, 0.89f, 0.93f);

            // Generic labels used by existing feature panels.
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 13;
            labelStyle.normal.textColor = Color.white;

            // Standard button.
            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 12;
            buttonStyle.alignment = TextAnchor.MiddleCenter;
            buttonStyle.normal.textColor = new Color(0.88f, 0.89f, 0.93f);

            // Selected button.
            selectedButtonStyle = new GUIStyle(buttonStyle);
            selectedButtonStyle.normal.textColor = Color.white;
            selectedButtonStyle.fontStyle = FontStyle.Bold;

            // Modern button.
            modernButtonStyle = new GUIStyle(buttonStyle);
            modernButtonStyle.fontSize = 11;

            modernSelectedButtonStyle = new GUIStyle(modernButtonStyle);
            modernSelectedButtonStyle.normal.textColor = Color.white;
            modernSelectedButtonStyle.fontStyle = FontStyle.Bold;

            // Headers.
            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 18;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = Color.white;

            sectionStyle = new GUIStyle(GUI.skin.label);
            sectionStyle.fontSize = 13;
            sectionStyle.fontStyle = FontStyle.Bold;
            sectionStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle(GUI.skin.label);
            valueStyle.fontSize = 12;
            valueStyle.fontStyle = FontStyle.Bold;
            valueStyle.normal.textColor = new Color(0.90f, 0.91f, 0.96f);

            accentLabelStyle = new GUIStyle(GUI.skin.label);
            accentLabelStyle.fontSize = 14;
            accentLabelStyle.fontStyle = FontStyle.Bold;
            accentLabelStyle.normal.textColor = accent;

            smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 11;
            smallStyle.normal.textColor = new Color(0.78f, 0.79f, 0.84f);

            modernSmallStyle = new GUIStyle(GUI.skin.label);
            modernSmallStyle.fontSize = 10;
            modernSmallStyle.normal.textColor = muted;

            // ESP.
            espStyle = new GUIStyle(GUI.skin.label);
            espStyle.fontSize = espFontSize;
            espStyle.fontStyle = FontStyle.Bold;
            espStyle.alignment = TextAnchor.MiddleCenter;
            espStyle.normal.textColor = Color.white;

            // Overlay styles.
            overlayHeaderStyle = new GUIStyle(GUI.skin.label);
            overlayHeaderStyle.fontSize = 14;
            overlayHeaderStyle.fontStyle = FontStyle.Bold;
            overlayHeaderStyle.alignment = TextAnchor.UpperRight;
            overlayHeaderStyle.normal.textColor = Color.white;

            overlayInfoStyle = new GUIStyle(GUI.skin.label);
            overlayInfoStyle.fontSize = 11;
            overlayInfoStyle.alignment = TextAnchor.MiddleRight;
            overlayInfoStyle.normal.textColor = new Color(0.90f, 0.90f, 0.93f);

            overlayRoleStyle = new GUIStyle(GUI.skin.label);
            overlayRoleStyle.fontSize = 12;
            overlayRoleStyle.alignment = TextAnchor.MiddleCenter;
            overlayRoleStyle.normal.textColor = new Color(0.90f, 0.90f, 0.93f);

            overlayPlayerStyle = new GUIStyle(GUI.skin.label);
            overlayPlayerStyle.fontSize = 13;
            overlayPlayerStyle.richText = true;
            overlayPlayerStyle.alignment = TextAnchor.MiddleRight;
            overlayPlayerStyle.normal.textColor = Color.white;

            overlayServerStyle = new GUIStyle(GUI.skin.label);
            overlayServerStyle.fontSize = 12;
            overlayServerStyle.fontStyle = FontStyle.Bold;
            overlayServerStyle.alignment = TextAnchor.UpperRight;
            overlayServerStyle.wordWrap = true;
            overlayServerStyle.normal.textColor = Color.white;

            stylesCreated = true;
        }

        private void DestroyUITexture(ref Texture2D texture)
        {
            if (texture != null)
            {
                UnityEngine.Object.Destroy(texture);
                texture = null;
            }
        }

        private GUIStyle GetEspStyle()
        {
            return espStyle;
        }

        // ============================================================
        // ESP RENDERING
        // ============================================================
        private void DrawESP(GUIStyle espStyle)
        {
            if (!PhotonNetwork.InRoom) return;
            Camera cam = Camera.main;
            if (cam == null) return;

            RefreshPlayerObjectCache();

            Vector2 origin = GetTracerOrigin();
            Player[] players = PhotonNetwork.PlayerList;

            for (int i = 0; i < players.Length; i++)
            {
                Player player = players[i];
                if (player == null || (hideSelf && player.IsLocal)) continue;

                GameObject obj = FindPlayerObject(player);
                if (obj == null) continue;

                Vector3 head = obj.transform.position + Vector3.up * NameHeight;
                float distance = Vector3.Distance(cam.transform.position, head);
                if (maxDistance > 0f && distance > maxDistance) continue;

                Vector3 screen = cam.WorldToScreenPoint(head);
                bool behind = screen.z <= 0f;
                Vector2 guiScreen = new Vector2(screen.x, Screen.height - screen.y);
                bool outside = guiScreen.x < 0f || guiScreen.x > Screen.width || guiScreen.y < 0f || guiScreen.y > Screen.height;
                bool visible = !visibilityCheck || IsVisible(cam, head);

                Color color = GetESPColor(player);
                if (visibilityCheck && !visible) color = hiddenColor;

                if (behind || outside)
                {
                    if (offscreenArrows) DrawOffscreenArrow(cam, head, color);
                    continue;
                }

                if (tracersEnabled)
                {
                    Color tracerColor = color;

                    if (tracerDistanceFade && maxDistance > 0f)
                    {
                        float alpha = 1f - Mathf.Clamp01(distance / maxDistance);
                        tracerColor.a = Mathf.Clamp(alpha, 0.15f, 1f);
                    }

                    DrawTracerLine(origin, guiScreen, tracerColor);
                }

                if (showNames || showDistance || showActorID)
                {
                    float scale = 1f;
                    if (scaleNames && maxDistance > 0f)
                        scale = Mathf.Lerp(MaxNameScale, MinNameScale, Mathf.Clamp01(distance / maxDistance));

                    string label = string.IsNullOrEmpty(player.NickName) ? "Player " + player.ActorNumber : player.NickName;
                    if (showActorID) label += "  #" + player.ActorNumber;
                    if (showDistance) label += "  " + distance.ToString("0") + "m";

                    GUI.color = color;
                    int prevSize = espFontSize;
                    espStyle.fontSize = Mathf.RoundToInt(espFontSize * scale);
                    Vector2 size = espStyle.CalcSize(new GUIContent(label));
                    GUI.Label(new Rect(guiScreen.x - size.x / 2f, guiScreen.y - size.y, size.x, size.y), label, espStyle);
                    espStyle.fontSize = prevSize;
                    GUI.color = Color.white;
                }
            }
        }

        private Vector2 GetTracerOrigin()
        {
            if (tracerOrigin == 0) return new Vector2(Screen.width / 2f, Screen.height);
            if (tracerOrigin == 1) return new Vector2(Screen.width / 2f, Screen.height / 2f);
            return new Vector2(Screen.width / 2f, 0f);
        }

        private void DrawTracerLine(Vector2 from, Vector2 to, Color color)
        {
            DrawThickLine(from, to, color, tracerThickness);
        }

        private bool IsVisible(Camera cam, Vector3 worldPos)
        {
            RaycastHit hit;
            Vector3 origin = cam.transform.position;
            Vector3 dir = worldPos - origin;
            float dist = dir.magnitude;
            if (dist <= 0.01f) return true;
            if (Physics.Raycast(origin, dir.normalized, out hit, dist))
                return Vector3.Distance(hit.point, worldPos) < 0.75f;
            return true;
        }

        private Texture2D arrowTexture;

        private Texture2D GetArrowTexture()
        {
            if (arrowTexture != null)
                return arrowTexture;

            const int size = 32;
            arrowTexture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            arrowTexture.wrapMode = TextureWrapMode.Clamp;
            arrowTexture.filterMode = FilterMode.Bilinear;

            Color32[] pixels = new Color32[size * size];
            int cx = size / 2;

            for (int y = 0; y < size; y++)
            {
                float t = y / (float)(size - 1); // 0 at bottom row, 1 at top row
                float halfWidth = (1f - t) * (size / 2f); // widest at base (bottom), narrows to apex (top)

                for (int x = 0; x < size; x++)
                {
                    bool inside = Mathf.Abs(x - cx) <= halfWidth;
                    pixels[y * size + x] = inside ? new Color32(255, 255, 255, 255) : new Color32(255, 255, 255, 0);
                }
            }

            arrowTexture.SetPixels32(pixels);
            arrowTexture.Apply();
            return arrowTexture;
        }

        private void DrawOffscreenArrow(Camera cam, Vector3 worldPos, Color color)
        {
            Vector3 screen = cam.WorldToScreenPoint(worldPos);
            Vector2 screenCenter = new Vector2(Screen.width * 0.5f, Screen.height * 0.5f);

            if (screen.z < 0f)
            {
                screen.x = -screen.x;
                screen.y = -screen.y;
            }

            Vector2 target = new Vector2(screen.x, Screen.height - screen.y);
            Vector2 direction = target - screenCenter;

            if (direction.sqrMagnitude < 0.001f)
                return;

            direction.Normalize();

            float halfWidth = Screen.width * 0.5f - OffscreenArrowMargin;
            float halfHeight = Screen.height * 0.5f - OffscreenArrowMargin;

            float scaleX = Mathf.Abs(direction.x) > 0.001f ? halfWidth / Mathf.Abs(direction.x) : float.MaxValue;
            float scaleY = Mathf.Abs(direction.y) > 0.001f ? halfHeight / Mathf.Abs(direction.y) : float.MaxValue;
            float scale = Mathf.Min(scaleX, scaleY);

            Vector2 arrowCenter = screenCenter + direction * scale;
            Vector2 perpendicular = new Vector2(-direction.y, direction.x);

            float size = OffscreenArrowSize;

            Vector2 tip = arrowCenter + direction * size;
            Vector2 left = arrowCenter - direction * (size * 0.65f) + perpendicular * (size * 0.65f);
            Vector2 right = arrowCenter - direction * (size * 0.65f) - perpendicular * (size * 0.65f);

            DrawThickLine(tip, left, color, 3f);
            DrawThickLine(tip, right, color, 3f);
            DrawThickLine(left, right, color, 3f);
        }

        private void EnsureTracerMaterial()
        {
            if (tracerMaterial != null)
                return;

            Shader shader = Shader.Find("Hidden/Internal-Colored");
            if (shader == null)
                return;

            tracerMaterial = new Material(shader);
            tracerMaterial.hideFlags = HideFlags.HideAndDontSave;

            tracerMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            tracerMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            tracerMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
            tracerMaterial.SetInt("_ZWrite", 0);
            tracerMaterial.SetInt("_ZTest", (int)UnityEngine.Rendering.CompareFunction.Always);
            tracerMaterial.renderQueue = 5000;
        }

        private void DrawGLLine(Vector2 start, Vector2 end, Color color)
        {
            EnsureTracerMaterial();

            if (tracerMaterial == null)
                return;

            tracerMaterial.SetPass(0);

            GL.PushMatrix();
            GL.LoadPixelMatrix(0f, Screen.width, Screen.height, 0f);
            GL.Begin(1); // GL.LINES
            GL.Color(color);
            GL.Vertex3(start.x, start.y, 0f);
            GL.Vertex3(end.x, end.y, 0f);
            GL.End();
            GL.PopMatrix();
        }

        private void DrawThickLine(Vector2 start, Vector2 end, Color color, float width)
        {
            float half = Mathf.Max(width * 0.5f, 0.5f);

            Vector2 delta = end - start;
            Vector2 dir = delta.sqrMagnitude > 0.0001f ? delta.normalized : Vector2.right;
            Vector2 perpendicular = new Vector2(-dir.y, dir.x);

            DrawGLLine(start - perpendicular * half, end - perpendicular * half, color);
            DrawGLLine(start + perpendicular * half, end + perpendicular * half, color);

            if (width > 2f)
                DrawGLLine(start, end, color);
        }

        // ============================================================
        // SERVER BROWSER UI + LOGIC
        // ============================================================
        private void DrawServersPanel(float x, float y, float width, float maxHeight)
        {
            float startY = y;
            GUI.Label(new Rect(x, y, width, 24f), new GUIContent("SERVERS (BSL)"), headerStyle);
            y += 28f;

            // Status + buttons
            GUI.Label(new Rect(x, y, width - 220f, 22f), new GUIContent("STATUS: " + serverListStatus), labelStyle);

            if (GUI.Button(new Rect(x + width - 210f, y - 2f, 100f, 28f), new GUIContent("Refresh"), buttonStyle))
                StartServerBrowse();

            if (GUI.Button(new Rect(x + width - 100f, y - 2f, 100f, 28f), new GUIContent("STOP"), buttonStyle))
                StopServerBrowse();

            y += 32f;

            // Current connection info
            string connInfo = PhotonNetwork.InRoom
                ? "IN ROOM: " + (PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "?")
                : PhotonNetwork.InLobby
                    ? "IN LOBBY"
                    : PhotonNetwork.IsConnected
                        ? "CONNECTED (not in lobby/room)"
                        : "NOT CONNECTED";

            GUI.Label(new Rect(x, y, width, 18f), new GUIContent(connInfo), smallStyle);
            y += 22f;

            // ---- Filters ----
            Event e = Event.current;

            // Name filter
            GUI.Label(new Rect(x, y, 50f, 20f), new GUIContent("Name"), smallStyle);
            Rect nameFilterRect = new Rect(x + 50f, y - 1f, Mathf.Min(220f, width * 0.35f), 22f);
            GUI.Box(nameFilterRect, "");
            string nameShown = string.IsNullOrEmpty(serverNameFilter) ? "contains..." : serverNameFilter;
            if (serverNameFilterFocused) nameShown += "|";
            GUI.Label(new Rect(nameFilterRect.x + 4f, nameFilterRect.y + 2f, nameFilterRect.width - 8f, 18f),
                new GUIContent(nameShown), labelStyle);
            if (e != null && e.type == EventType.MouseDown && nameFilterRect.Contains(e.mousePosition))
            {
                serverNameFilterFocused = true;
                e.Use();
            }
            if (serverNameFilterFocused && e != null && e.type == EventType.KeyDown)
            {
                if (e.keyCode == KeyCode.Backspace && serverNameFilter.Length > 0)
                {
                    serverNameFilter = serverNameFilter.Substring(0, serverNameFilter.Length - 1);
                    e.Use();
                }
                else if (e.keyCode == KeyCode.Escape || e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                {
                    serverNameFilterFocused = false;
                    e.Use();
                }
                else if (e.character != '\0' && !char.IsControl(e.character) && serverNameFilter.Length < 48)
                {
                    serverNameFilter += e.character;
                    e.Use();
                }
            }

            float fx = nameFilterRect.xMax + 8f;
            if (GUI.Button(new Rect(fx, y - 1f, 88f, 22f),
                new GUIContent(serverFilterOpenOnly ? "OPEN ONLY" : "ALL ROOMS"), buttonStyle))
                serverFilterOpenOnly = !serverFilterOpenOnly;
            fx += 92f;
            if (GUI.Button(new Rect(fx, y - 1f, 88f, 22f),
                new GUIContent(serverShowFavoritesOnly ? "FAVS ONLY" : "ALL/FAVS"), buttonStyle))
                serverShowFavoritesOnly = !serverShowFavoritesOnly;

            y += 26f;
            GUI.Label(new Rect(x, y, 90f, 18f), new GUIContent("Min players " + serverFilterMinPlayers), smallStyle);
            serverFilterMinPlayers = Mathf.RoundToInt(GUI.HorizontalSlider(
                new Rect(x + 95f, y + 2f, 120f, 16f), serverFilterMinPlayers, 0f, 32f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb));
            GUI.Label(new Rect(x + 230f, y, 90f, 18f), new GUIContent("Max " + (serverFilterMaxPlayers >= 255 ? "any" : serverFilterMaxPlayers.ToString())), smallStyle);
            serverFilterMaxPlayers = Mathf.RoundToInt(GUI.HorizontalSlider(
                new Rect(x + 320f, y + 2f, 120f, 16f), serverFilterMaxPlayers, 1f, 255f,
                GUI.skin.horizontalSlider, GUI.skin.horizontalSliderThumb));
            y += 24f;

            // Room list
            const float footerReserve = 110f;
            float listH = Mathf.Max(100f, maxHeight - (y - startY) - footerReserve);
            Rect listRect = new Rect(x, y, width, listH);
            GUI.Box(listRect, "");

            List<RoomInfo> rooms = GetFilteredServerRooms();

            const float rowH = 36f;
            float contentH = rooms.Count * rowH;
            float maxScroll = Mathf.Max(0f, contentH - listH + 10f);

            if (e != null && e.type == EventType.ScrollWheel && listRect.Contains(e.mousePosition))
            {
                serverListScroll.y = Mathf.Clamp(serverListScroll.y + e.delta.y * 30f, 0f, maxScroll);
                e.Use();
            }
            serverListScroll.y = Mathf.Clamp(serverListScroll.y, 0f, maxScroll);

            GUI.BeginGroup(
                new Rect(x + 4f, y + 4f, width - 8f, listH - 8f),
                GUIContent.none,
                GUIStyle.none
            );

            float rowY = -serverListScroll.y;

            if (rooms.Count == 0)
            {
                GUI.Label(new Rect(8f, 12f, width - 24f, 24f),
                    new GUIContent(isBrowsingServers ? "Waiting…" : "No rooms — Refresh"),
                    labelStyle);
            }
            else
            {
                for (int i = 0; i < rooms.Count; i++)
                {
                    RoomInfo room = rooms[i];
                    if (room == null) continue;

                    bool selected = room.Name == selectedRoomName;
                    bool joinable = room.IsOpen && room.PlayerCount < room.MaxPlayers;
                    bool isFav = favoriteRoomNames.Contains(room.Name);

                    string lockIcon = room.IsVisible ? "" : "🔒 ";
                    string favIcon = isFav ? "★ " : "";
                    string status = joinable ? "" : (room.IsOpen ? " [FULL]" : " [CLOSED]");
                    string label = GetRoomInfoLabel(room);
                    string labelPart = string.IsNullOrEmpty(label) ? "" : "  · " + label;
                    bool hasScan = !string.IsNullOrEmpty(room.Name) && peekedRoomPlayers.ContainsKey(room.Name);
                    bool hasPub = !string.IsNullOrEmpty(GetRoomPlayersFromInfo(room));
                    string whoIcon = hasPub ? " 👥" : (hasScan ? " 👁" : "");
                    string text = favIcon + lockIcon + room.Name + "   " + room.PlayerCount + "/" + room.MaxPlayers + status + labelPart + whoIcon;

                    GUIStyle style = selected ? selectedButtonStyle : buttonStyle;
                    if (!joinable) style = GUI.skin.button;

                    float btnW = width - 16f - 36f;
                    Rect rowBtnRect = new Rect(0f, rowY, btnW, 32f);
                    if (GUI.Button(rowBtnRect, new GUIContent(text), style))
                        selectedRoomName = room.Name;

                    // Hover → show published player list (ZexQoLPlayers room prop)
                    if (e != null && rowBtnRect.Contains(e.mousePosition))
                    {
                        string playersCsv = GetRoomPlayersFromInfo(room);
                        serverHoverRoomName = room.Name ?? "";
                        if (!string.IsNullOrEmpty(playersCsv))
                        {
                            serverHoverPlayersText = FormatPlayerListMultiline(playersCsv);
                        }
                        else if (!string.IsNullOrEmpty(room.Name) && peekedRoomPlayers.ContainsKey(room.Name))
                        {
                            serverHoverPlayersText = peekedRoomPlayers[room.Name] + "\n(scanned)";
                        }
                        else
                        {
                            serverHoverPlayersText =
                                "(no list — host publish or SCAN SELECTED)";
                        }
                        // Convert group-local mouse to screen GUI for tooltip after EndGroup
                        serverHoverGuiPos = new Vector2(
                            x + 4f + e.mousePosition.x + 14f,
                            y + 4f + e.mousePosition.y + 18f);
                    }

                    if (GUI.Button(new Rect(btnW + 2f, rowY, 32f, 32f), new GUIContent(isFav ? "★" : "☆"), buttonStyle))
                    {
                        if (isFav) favoriteRoomNames.Remove(room.Name);
                        else favoriteRoomNames.Add(room.Name);
                        SaveFavoriteRoomNames();
                    }

                    rowY += rowH;
                }
            }
            GUI.EndGroup();

            // Clear hover when mouse leaves the list
            if (e != null && e.type != EventType.Layout && !listRect.Contains(e.mousePosition))
            {
                serverHoverRoomName = "";
                serverHoverPlayersText = "";
            }

            y += listH + 10f;

            bool canJoin = !string.IsNullOrEmpty(selectedRoomName) &&
                           cachedRooms.ContainsKey(selectedRoomName) &&
                           cachedRooms[selectedRoomName].IsOpen;

            float joinW = Mathf.Min(180f, (width - 12f) * 0.5f);
            if (GUI.Button(new Rect(x, y, joinW, 32f),
                new GUIContent(canJoin ? "JOIN SELECTED" : "SELECT A ROOM"),
                canJoin ? buttonStyle : GUI.skin.button) && canJoin)
            {
                JoinSelectedServer();
            }

            float half = Mathf.Min(140f, (width - 16f) / 3f);
            bool busy = scanRunning;
            if (GUI.Button(new Rect(x + joinW + 8f, y, half, 32f),
                new GUIContent(busy ? "SCANNING…" : "SCAN 1"),
                (!busy && !string.IsNullOrEmpty(selectedRoomName)) ? buttonStyle : GUI.skin.button)
                && !busy && !string.IsNullOrEmpty(selectedRoomName))
            {
                EnqueueRoomScan(selectedRoomName, clearQueue: true);
            }
            if (GUI.Button(new Rect(x + joinW + 12f + half, y, half, 32f),
                new GUIContent(busy ? ("Q:" + scanQueue.Count) : "SCAN ALL"),
                !busy ? buttonStyle : GUI.skin.button) && !busy)
            {
                EnqueueScanAllOpenRooms();
            }
            if (busy && GUI.Button(new Rect(x + joinW + 16f + half * 2f, y, 56f, 32f),
                new GUIContent("STOP"), buttonStyle))
            {
                scanAbort = true;
                peekStatus = "Stopping scan…";
                peekStatusUntil = Time.unscaledTime + 3f;
            }

            y += 38f;
            string peekLine = (!string.IsNullOrEmpty(peekStatus) && Time.unscaledTime < peekStatusUntil)
                ? peekStatus
                : "SCAN 1 = selected room. SCAN ALL = every open joinable room. Hover row for names.";
            GUI.Label(new Rect(x, y, width, 40f),
                new GUIContent(
                    "Names need a free slot to join. Full rooms can only show names if host has this mod.\n" + peekLine),
                smallStyle);

            // Hover tooltip — draw rich-text names with real colors
            if (!string.IsNullOrEmpty(serverHoverRoomName) && !string.IsNullOrEmpty(serverHoverPlayersText))
            {
                DrawServerHoverTooltip(x, startY, width, maxHeight);
            }
        }

        private void DrawServerHoverTooltip(float panelX, float startY, float width, float maxHeight)
        {
            string body = serverHoverPlayersText ?? "";
            string header = serverHoverRoomName ?? "";

            // Split into lines (names already newline-separated)
            string[] rawLines = body.Split(new char[] { '\n' }, StringSplitOptions.None);
            List<string> lines = new List<string>();
            lines.Add(header);
            for (int i = 0; i < rawLines.Length; i++)
            {
                string ln = rawLines[i];
                if (ln == null) continue;
                // Keep empty lines out; keep "(scanned)" footer
                if (ln.Length == 0) continue;
                lines.Add(SanitizeUnityRichText(ln));
            }

            // Minimal style — this game's Unity IMGUI is stripped (no hover/active/padding).
            GUIStyle tipStyle = new GUIStyle(GUI.skin.label);
            tipStyle.richText = true;
            tipStyle.wordWrap = false;
            tipStyle.alignment = TextAnchor.UpperLeft;
            tipStyle.fontSize = 13;
            tipStyle.normal.textColor = Color.white;

            float tipW = Mathf.Clamp(width * 0.62f, 280f, Mathf.Min(520f, width));
            float lineH = 18f;
            float tipH = Mathf.Clamp(14f + lines.Count * lineH + 10f, 48f, Mathf.Min(360f, maxHeight * 0.85f));

            float tipX = Mathf.Clamp(serverHoverGuiPos.x, panelX, panelX + width - tipW);
            float tipY = Mathf.Clamp(serverHoverGuiPos.y, startY, startY + maxHeight - tipH);

            Color prev = GUI.color;
            GUI.color = new Color(0.05f, 0.06f, 0.09f, 0.97f);
            GUI.Box(new Rect(tipX, tipY, tipW, tipH), GUIContent.none, GUI.skin.box);
            GUI.color = Color.white;

            float ly = tipY + 8f;
            float lx = tipX + 10f;
            float lw = tipW - 20f;
            int maxLines = Mathf.Max(1, Mathf.FloorToInt((tipH - 16f) / lineH));

            for (int i = 0; i < lines.Count && i < maxLines; i++)
            {
                string line = lines[i];
                // Header (room name) in accent; footer muted; names with rich text
                if (i == 0)
                {
                    Color c = GUI.color;
                    GUI.color = new Color(0.75f, 0.85f, 1f, 1f);
                    GUI.Label(new Rect(lx, ly, lw, lineH), line, tipStyle);
                    GUI.color = c;
                }
                else if (line == "(scanned)" || line.StartsWith("(full") || line.StartsWith("(join"))
                {
                    Color c = GUI.color;
                    GUI.color = new Color(0.65f, 0.65f, 0.7f, 1f);
                    // plain text — strip any tags
                    GUI.Label(new Rect(lx, ly, lw, lineH), StripRichTextTags(line), tipStyle);
                    GUI.color = c;
                }
                else
                {
                    // Force richText path: draw sanitized <color> line
                    GUI.Label(new Rect(lx, ly, lw, lineH), new GUIContent(line), tipStyle);
                }
                ly += lineH;
            }

            GUI.color = prev;
        }

        /// <summary>
        /// Unity IMGUI only renders well-formed &lt;color=#RRGGBB&gt;…&lt;/color&gt;.
        /// Closes unclosed tags and drops broken fragments so colors actually show.
        /// </summary>
        private static string SanitizeUnityRichText(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            // Fast path: no tags
            if (input.IndexOf('<') < 0)
                return input;

            System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length + 32);
            int openColors = 0;
            int i = 0;
            while (i < input.Length)
            {
                if (input[i] == '<')
                {
                    int close = input.IndexOf('>', i);
                    if (close < 0)
                    {
                        // dangling '<' — escape rest as text
                        sb.Append(input.Substring(i));
                        break;
                    }

                    string tag = input.Substring(i, close - i + 1);
                    string tagLower = tag.ToLowerInvariant();

                    if (tagLower.StartsWith("<color=") && tag.EndsWith(">"))
                    {
                        // Normalize #RGB / #RRGGBB / #RRGGBBAA
                        string fixedTag = NormalizeColorTag(tag);
                        if (fixedTag != null)
                        {
                            sb.Append(fixedTag);
                            openColors++;
                            i = close + 1;
                            continue;
                        }
                        // bad color tag — skip it
                        i = close + 1;
                        continue;
                    }

                    if (tagLower == "</color>")
                    {
                        if (openColors > 0)
                        {
                            sb.Append("</color>");
                            openColors--;
                        }
                        i = close + 1;
                        continue;
                    }

                    // Other tags (b, i, size, …) — pass through if closed
                    sb.Append(tag);
                    i = close + 1;
                    continue;
                }

                sb.Append(input[i]);
                i++;
            }

            while (openColors > 0)
            {
                sb.Append("</color>");
                openColors--;
            }

            return sb.ToString();
        }

        private static string NormalizeColorTag(string tag)
        {
            // tag like <color=#80FFFF> or <color=#80FFFFFF> or <color=red>
            if (string.IsNullOrEmpty(tag) || tag.Length < 8)
                return null;

            int eq = tag.IndexOf('=');
            if (eq < 0) return null;
            string val = tag.Substring(eq + 1, tag.Length - eq - 2).Trim(); // strip <color= and >
            if (val.Length == 0) return null;

            if (val[0] == '#')
            {
                string hex = val.Substring(1);
                // Expand #RGB → #RRGGBB
                if (hex.Length == 3)
                {
                    hex = string.Concat(
                        hex[0], hex[0],
                        hex[1], hex[1],
                        hex[2], hex[2]);
                }
                // Keep only valid hex length 6 or 8
                if (hex.Length != 6 && hex.Length != 8)
                    return null;
                for (int h = 0; h < hex.Length; h++)
                {
                    char c = hex[h];
                    bool ok = (c >= '0' && c <= '9') || (c >= 'a' && c <= 'f') || (c >= 'A' && c <= 'F');
                    if (!ok) return null;
                }
                return "<color=#" + hex + ">";
            }

            // named colors Unity supports: red, green, blue, white, black, yellow, cyan, magenta, grey/gray
            string lower = val.ToLowerInvariant();
            switch (lower)
            {
                case "red":
                case "green":
                case "blue":
                case "white":
                case "black":
                case "yellow":
                case "cyan":
                case "magenta":
                case "grey":
                case "gray":
                    return "<color=" + lower + ">";
                default:
                    return null;
            }
        }

        private static string StripRichTextTags(string input)
        {
            if (string.IsNullOrEmpty(input) || input.IndexOf('<') < 0)
                return input;
            System.Text.StringBuilder sb = new System.Text.StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '<')
                {
                    int close = input.IndexOf('>', i);
                    if (close >= 0) { i = close; continue; }
                }
                sb.Append(input[i]);
            }
            return sb.ToString();
        }

        private static string FormatPlayerListMultiline(string csv)
        {
            if (string.IsNullOrEmpty(csv))
                return "";
            string[] parts = csv.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            List<string> lines = new List<string>();
            for (int i = 0; i < parts.Length; i++)
            {
                string p = parts[i].Trim();
                if (p.Length > 0)
                    lines.Add(SanitizeUnityRichText(p));
            }
            return string.Join("\n", lines.ToArray());
        }

        // ============================================================
        // ROOM PLAYER SCANNER (coroutine queue)
        // Photon only exposes nicknames after you join. We join, read
        // PlayerList, leave, then process the next queued room.
        // ============================================================

        private void EnqueueRoomScan(string roomName, bool clearQueue)
        {
            if (string.IsNullOrEmpty(roomName))
                return;

            if (clearQueue)
                scanQueue.Clear();

            // Already in that room — just snapshot
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null &&
                string.Equals(PhotonNetwork.CurrentRoom.Name, roomName, StringComparison.Ordinal))
            {
                int n = SnapshotRoomPlayers(roomName);
                peekStatus = "Cached " + n + " name(s) from current room";
                peekStatusUntil = Time.unscaledTime + 4f;
                serverListStatus = "SCANNED · " + roomName + " (" + n + ")";
                return;
            }

            // Skip full rooms (cannot join → cannot read names)
            if (cachedRooms.TryGetValue(roomName, out RoomInfo info) && info != null)
            {
                if (!info.IsOpen)
                {
                    peekStatus = roomName + " is closed — cannot scan";
                    peekStatusUntil = Time.unscaledTime + 3f;
                    return;
                }
                if (info.PlayerCount >= info.MaxPlayers)
                {
                    peekStatus = roomName + " is full — cannot join to read names";
                    peekStatusUntil = Time.unscaledTime + 3f;
                    peekedRoomPlayers[roomName] = "(full — " + info.PlayerCount + "/" + info.MaxPlayers + ", names unknown)";
                    return;
                }
            }

            if (!scanQueue.Contains(roomName))
                scanQueue.Enqueue(roomName);

            EnsureScanRunner();
        }

        private void EnqueueScanAllOpenRooms()
        {
            scanQueue.Clear();
            List<RoomInfo> rooms = GetFilteredServerRooms();
            int added = 0;
            for (int i = 0; i < rooms.Count; i++)
            {
                RoomInfo r = rooms[i];
                if (r == null || string.IsNullOrEmpty(r.Name)) continue;
                if (!r.IsOpen) continue;
                if (r.PlayerCount >= r.MaxPlayers)
                {
                    peekedRoomPlayers[r.Name] = "(full — " + r.PlayerCount + "/" + r.MaxPlayers + ", names unknown)";
                    continue;
                }
                // Prefer published list when already present
                if (!string.IsNullOrEmpty(GetRoomPlayersFromInfo(r)))
                    continue;
                scanQueue.Enqueue(r.Name);
                added++;
            }

            if (added == 0)
            {
                peekStatus = "Nothing to scan (all full, closed, or already published)";
                peekStatusUntil = Time.unscaledTime + 4f;
                return;
            }

            peekStatus = "Queued " + added + " room(s) to scan";
            peekStatusUntil = Time.unscaledTime + 4f;
            EnsureScanRunner();
        }

        private void EnsureScanRunner()
        {
            if (scanRunning)
                return;
            if (scanCoroutine != null)
            {
                StopCoroutine(scanCoroutine);
                scanCoroutine = null;
            }
            scanAbort = false;
            scanCoroutine = StartCoroutine(RoomScanQueueRoutine());
        }

        private IEnumerator RoomScanQueueRoutine()
        {
            scanRunning = true;
            peekInProgress = true;
            scanAbort = false;
            scanLastError = "";

            // Cancel browse auto-rejoin so we own the connection
            pendingRejoinPrevious = false;
            pendingJoinRoomName = "";
            browseRestoreActive = false;
            isBrowsingServers = false;
            if (rejoinCoroutine != null)
            {
                StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = null;
            }
            if (restoreBrowsePositionCoroutine != null)
            {
                StopCoroutine(restoreBrowsePositionCoroutine);
                restoreBrowsePositionCoroutine = null;
            }

            // Remember home room + position (same as REFRESH)
            scanHomeRoom = "";
            scanShouldRejoinHome = false;
            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null)
            {
                scanHomeRoom = PhotonNetwork.CurrentRoom.Name;
                previousRoomName = scanHomeRoom;
                scanShouldRejoinHome = true;
                CaptureBrowseTransform();
                SetScanStatus("Saved pos · leaving " + scanHomeRoom + "…");
                Logger.LogInfo("Scan: home room saved = " + scanHomeRoom);
                try
                {
                    if (destroyBodyOnLeave)
                        DestroyLocalPlayerBodyForBrowse();
                }
                catch { }
                try { PhotonNetwork.LeaveRoom(); } catch (Exception ex) { scanLastError = ex.Message; }

                float leaveDeadline = Time.unscaledTime + 4f;
                while (PhotonNetwork.InRoom && Time.unscaledTime < leaveDeadline)
                    yield return null;
            }
            else
            {
                // Scanning from menu/lobby — cannot rejoin a play session afterward
                SetScanStatus("Scan from lobby (no home room to return to)");
                Logger.LogInfo("Scan: not in a room at start — will not auto-rejoin a play room");
            }

            while (scanQueue.Count > 0 && !scanAbort)
            {
                string room = scanQueue.Dequeue();
                scanCurrentRoom = room;
                peekTargetRoom = room;
                scanLastError = "";

                // Skip known-full / closed from cache
                if (cachedRooms.TryGetValue(room, out RoomInfo preInfo) && preInfo != null)
                {
                    if (!preInfo.IsOpen)
                    {
                        peekedRoomPlayers[room] = "(closed)";
                        continue;
                    }
                    if (preInfo.PlayerCount >= preInfo.MaxPlayers)
                    {
                        peekedRoomPlayers[room] = "(full — " + preInfo.PlayerCount + "/" + preInfo.MaxPlayers + ")";
                        continue;
                    }
                }

                if (!PhotonNetwork.IsConnected)
                {
                    SetScanStatus("Disconnected — aborting scan");
                    break;
                }

                // Wait until fully out of previous room AND Photon is ready to send ops
                yield return StartCoroutine(WaitUntilScanCanJoin());

                peekStatus = "SCAN " + room + " (" + scanQueue.Count + " left)";
                peekStatusUntil = Time.unscaledTime + 3f;
                serverListStatus = peekStatus;

                // Up to 2 join attempts — first often fails if master wasn't ready yet
                bool joined = false;
                for (int attempt = 0; attempt < 2 && !joined && !scanAbort; attempt++)
                {
                    if (attempt > 0)
                    {
                        yield return StartCoroutine(WaitUntilScanCanJoin());
                        yield return new WaitForSecondsRealtime(0.15f);
                    }

                    scanLastError = "";
                    bool sent = false;
                    try
                    {
                        sent = PhotonNetwork.JoinRoom(room);
                    }
                    catch (Exception ex)
                    {
                        scanLastError = ex.Message;
                        sent = false;
                    }

                    if (!sent)
                    {
                        // JoinRoom returned false = client not ready; wait and retry
                        scanLastError = string.IsNullOrEmpty(scanLastError) ? "not ready" : scanLastError;
                        yield return new WaitForSecondsRealtime(0.2f);
                        continue;
                    }

                    float joinDeadline = Time.unscaledTime + 5f;
                    while (!PhotonNetwork.InRoom && Time.unscaledTime < joinDeadline && !scanAbort)
                    {
                        // OnJoinRoomFailed sets scanLastError
                        if (!string.IsNullOrEmpty(scanLastError) &&
                            scanLastError.IndexOf("not ready", StringComparison.OrdinalIgnoreCase) < 0)
                            break;
                        yield return null;
                    }

                    if (PhotonNetwork.InRoom)
                        joined = true;
                    else if (string.IsNullOrEmpty(scanLastError))
                        scanLastError = "timeout";
                }

                if (!joined)
                {
                    string why = string.IsNullOrEmpty(scanLastError) ? "unknown" : scanLastError;
                    peekedRoomPlayers[room] = "(join failed: " + why + ")";
                    peekStatus = room + " fail: " + why;
                    peekStatusUntil = Time.unscaledTime + 3f;
                    yield return new WaitForSecondsRealtime(0.1f);
                    continue;
                }

                // Grab names ASAP and leave before full spawn
                int best = 0;
                float readStart = Time.unscaledTime;
                float readDeadline = readStart + 0.55f;
                int expected = 0;
                if (cachedRooms.TryGetValue(room, out RoomInfo ri) && ri != null)
                    expected = ri.PlayerCount;
                int target = expected > 0 ? expected + 1 : 0;

                yield return null;
                best = SnapshotRoomPlayers(room);

                while (Time.unscaledTime < readDeadline && PhotonNetwork.InRoom && !scanAbort)
                {
                    int count = SnapshotRoomPlayers(room);
                    if (count > best)
                        best = count;

                    if (target > 0 && count >= target)
                        break;
                    if (expected > 0 && count >= expected)
                        break;
                    if (count > 0 && Time.unscaledTime > readStart + 0.12f)
                        break;

                    yield return null;
                }

                peekStatus = room + " → " + best + " (" + scanQueue.Count + " left)";
                peekStatusUntil = Time.unscaledTime + 3f;
                if (best <= 0 && !peekedRoomPlayers.ContainsKey(room))
                    peekedRoomPlayers[room] = "(no names read)";

                try
                {
                    if (PhotonNetwork.InRoom)
                        PhotonNetwork.LeaveRoom();
                }
                catch { }

                float leftDeadline = Time.unscaledTime + 4f;
                while (PhotonNetwork.InRoom && Time.unscaledTime < leftDeadline)
                    yield return null;

                // Brief settle so next JoinRoom isn't rejected
                yield return new WaitForSecondsRealtime(0.12f);
            }

            // Always try to return home (even if STOP / fail)
            scanCurrentRoom = "";
            peekTargetRoom = "";

            // CRITICAL: clear scanRunning BEFORE rejoin so OnJoinedRoom runs the normal
            // auto-rejoin + position restore path (not the scan snapshot early-out).
            string home = !string.IsNullOrEmpty(scanHomeRoom) ? scanHomeRoom : previousRoomName;
            bool wantHome = scanShouldRejoinHome && !string.IsNullOrEmpty(home);

            scanRunning = false;
            peekInProgress = false;
            scanQueue.Clear();

            if (wantHome)
            {
                previousRoomName = home;
                pendingRejoinPrevious = true;
                yield return StartCoroutine(ScanRejoinHomeAndRestore());
            }
            else
            {
                SetScanStatus("Scan done (no home room to rejoin)");
                if (!PhotonNetwork.InRoom)
                {
                    try { PhotonNetwork.JoinLobby(); } catch { }
                }
            }

            scanCoroutine = null;
            SetScanStatus(scanAbort
                ? "Scan stopped · " + (wantHome ? ("back in " + home) : "done")
                : "Scan finished · hover for names");
            serverListStatus = "SCAN DONE · " + peekedRoomPlayers.Count + " cached";
        }

        /// <summary>
        /// Rejoin home room after scan using the same path as REFRESH browse rejoin.
        /// </summary>
        private IEnumerator ScanRejoinHomeAndRestore()
        {
            string home = !string.IsNullOrEmpty(scanHomeRoom) ? scanHomeRoom : previousRoomName;
            if (string.IsNullOrEmpty(home))
            {
                SetScanStatus("No home room saved");
                yield break;
            }

            previousRoomName = home;
            pendingRejoinPrevious = true;

            // Leave any room we might still be sitting in
            if (PhotonNetwork.InRoom)
            {
                try { PhotonNetwork.LeaveRoom(); } catch { }
                float leaveUntil = Time.unscaledTime + 6f;
                while (PhotonNetwork.InRoom && Time.unscaledTime < leaveUntil)
                    yield return null;
            }

            if (!PhotonNetwork.IsConnected)
            {
                pendingRejoinPrevious = false;
                SetScanStatus("Disconnected — cannot rejoin " + home);
                yield break;
            }

            // Wait for master/lobby ready
            yield return StartCoroutine(WaitUntilScanCanJoin());

            // Use the same delayed rejoin coroutine as REFRESH
            if (rejoinCoroutine != null)
            {
                StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = null;
            }
            pendingRejoinPrevious = true;
            rejoinCoroutine = StartCoroutine(RejoinPreviousRoomAfterDelay());

            // Wait for that coroutine to finish + for InRoom
            float waitUntil = Time.unscaledTime + 10f;
            while (Time.unscaledTime < waitUntil)
            {
                if (PhotonNetwork.InRoom)
                {
                    string cur = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "";
                    if (string.Equals(cur, home, StringComparison.Ordinal))
                        break;
                }
                if (rejoinCoroutine == null && !pendingRejoinPrevious)
                {
                    // RejoinPreviousRoomAfterDelay finished without success — try direct join
                    break;
                }
                yield return null;
            }

            // Hard retries if still not home
            for (int attempt = 0; attempt < 3 && !(PhotonNetwork.InRoom &&
                    PhotonNetwork.CurrentRoom != null &&
                    string.Equals(PhotonNetwork.CurrentRoom.Name, home, StringComparison.Ordinal)); attempt++)
            {
                if (PhotonNetwork.InRoom)
                {
                    try { PhotonNetwork.LeaveRoom(); } catch { }
                    float w = Time.unscaledTime + 4f;
                    while (PhotonNetwork.InRoom && Time.unscaledTime < w)
                        yield return null;
                }

                yield return StartCoroutine(WaitUntilScanCanJoin());
                SetScanStatus("Rejoin try " + (attempt + 1) + " → " + home);
                serverListStatus = "SCAN · rejoin " + home + " (" + (attempt + 1) + "/3)";

                bool sent = false;
                try { sent = PhotonNetwork.JoinRoom(home); }
                catch (Exception ex)
                {
                    Logger.LogWarning("Scan hard rejoin: " + ex.Message);
                    sent = false;
                }

                float rj = Time.unscaledTime + 8f;
                while (!PhotonNetwork.InRoom && Time.unscaledTime < rj)
                    yield return null;
            }

            if (PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null &&
                string.Equals(PhotonNetwork.CurrentRoom.Name, home, StringComparison.Ordinal))
            {
                pendingRejoinPrevious = false;
                // Position restore (OnJoinedRoom may have started it; ensure it runs)
                if (browseHasSavedTransform && browsePositionRestoreEnabled)
                {
                    browseRestoreActive = true;
                    browseRestoreUntil = Time.unscaledTime + 6f;
                    if (restoreBrowsePositionCoroutine != null)
                        StopCoroutine(restoreBrowsePositionCoroutine);
                    restoreBrowsePositionCoroutine = StartCoroutine(RestoreBrowseTransformAfterSpawn());
                    SetScanStatus("Back in " + home + " · restoring pos");
                }
                else
                {
                    SetScanStatus(browseHasSavedTransform && !browsePositionRestoreEnabled
                        ? ("Back in " + home + " · pos restore OFF")
                        : ("Back in " + home));
                }
            }
            else
            {
                pendingRejoinPrevious = false;
                SetScanStatus("Rejoin failed — try joining " + home + " manually");
                serverListStatus = "SCAN · rejoin FAILED · " + home;
                // Do NOT JoinLobby here — that can dump to main menu in this game.
                // Stay on master so user can pick a room from the browser.
            }
        }

        private void SetScanStatus(string msg)
        {
            peekStatus = msg;
            peekStatusUntil = Time.unscaledTime + 6f;
            // Avoid LogInfo on hot path — only warn on real failures elsewhere
        }

        /// <summary>
        /// After LeaveRoom Photon is often "connecting to master" and JoinRoom returns false.
        /// Wait until we are out of a room and IsConnectedAndReady (or in lobby).
        /// </summary>
        private IEnumerator WaitUntilScanCanJoin()
        {
            float deadline = Time.unscaledTime + 5f;

            while (PhotonNetwork.InRoom && Time.unscaledTime < deadline)
                yield return null;

            if (!PhotonNetwork.IsConnected)
                yield break;

            // Prefer ConnectedAndReady; JoinLobby helps some builds reach a joinable state
            if (!PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
            {
                try { PhotonNetwork.JoinLobby(); } catch { }
            }

            while (Time.unscaledTime < deadline)
            {
                if (PhotonNetwork.InRoom)
                {
                    try { PhotonNetwork.LeaveRoom(); } catch { }
                    yield return null;
                    continue;
                }

                if (PhotonNetwork.IsConnectedAndReady || PhotonNetwork.InLobby)
                    yield break;

                yield return null;
            }
        }

        private int SnapshotRoomPlayers(string roomKey)
        {
            try
            {
                if (string.IsNullOrEmpty(roomKey) && PhotonNetwork.CurrentRoom != null)
                    roomKey = PhotonNetwork.CurrentRoom.Name;

                Player[] players = PhotonNetwork.PlayerList;
                List<string> names = new List<string>();
                if (players != null)
                {
                    for (int i = 0; i < players.Length; i++)
                    {
                        Player p = players[i];
                        if (p == null) continue;
                        // Prefer raw NickName (keeps <color> tags for hover rich text)
                        string n = p.NickName;
                        if (string.IsNullOrEmpty(n))
                            n = GetPlayerName(p);
                        if (string.IsNullOrEmpty(n))
                            n = "Player" + p.ActorNumber;
                        n = n.Replace(',', ' ').Replace(';', ' ').Trim();
                        n = SanitizeUnityRichText(n);
                        if (n.Length > 96) n = n.Substring(0, 96);
                        if (n.Length > 0)
                            names.Add(n);
                    }
                }

                string multiline = names.Count > 0
                    ? string.Join("\n", names.ToArray())
                    : "(empty / no names)";

                if (!string.IsNullOrEmpty(roomKey))
                    peekedRoomPlayers[roomKey] = multiline;

                // Skip network publish while bulk-scanning (extra lag)
                if (!scanRunning && PhotonNetwork.IsMasterClient && publishRoomPlayers)
                    PublishRoomPlayerList(true);

                return names.Count;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SnapshotRoomPlayers: " + ex.Message);
                return 0;
            }
        }

        // Compatibility shims (old callback paths)
        private void StartPeekSelectedRoom()
        {
            if (!string.IsNullOrEmpty(selectedRoomName))
                EnqueueRoomScan(selectedRoomName, clearQueue: true);
        }

        private void BeginPeekJoin() { }

        private void CachePeekNamesFromCurrentRoom(string roomKey)
        {
            SnapshotRoomPlayers(roomKey);
        }

        private void FinishPeekAndLeave()
        {
            if (PhotonNetwork.InRoom)
            {
                SnapshotRoomPlayers(scanCurrentRoom);
                try { PhotonNetwork.LeaveRoom(); } catch { }
            }
        }

        private void TryReturnToLobbyAfterPeek()
        {
            if (PhotonNetwork.InRoom) return;
            try { PhotonNetwork.JoinLobby(); } catch { }
        }

        private List<RoomInfo> GetFilteredServerRooms()
        {
            List<RoomInfo> rooms = new List<RoomInfo>();
            foreach (RoomInfo info in cachedRooms.Values)
            {
                if (info == null) continue;
                if (serverFilterOpenOnly && !info.IsOpen) continue;
                if (serverShowFavoritesOnly && !favoriteRoomNames.Contains(info.Name)) continue;
                if (info.PlayerCount < serverFilterMinPlayers) continue;
                if (serverFilterMaxPlayers < 255 && info.PlayerCount > serverFilterMaxPlayers) continue;
                if (!string.IsNullOrEmpty(serverNameFilter))
                {
                    string label = GetRoomInfoLabel(info);
                    string hay = (info.Name ?? "") + " " + (label ?? "");
                    if (hay.IndexOf(serverNameFilter, StringComparison.OrdinalIgnoreCase) < 0)
                        continue;
                }
                rooms.Add(info);
            }

            rooms.Sort((a, b) =>
            {
                bool aFav = favoriteRoomNames.Contains(a.Name);
                bool bFav = favoriteRoomNames.Contains(b.Name);
                if (aFav != bFav) return aFav ? -1 : 1;
                bool aJoinable = a.IsOpen && a.PlayerCount < a.MaxPlayers;
                bool bJoinable = b.IsOpen && b.PlayerCount < b.MaxPlayers;
                if (aJoinable != bJoinable) return aJoinable ? -1 : 1;
                return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
            });
            return rooms;
        }

        private string GetRoomInfoLabel(RoomInfo info)
        {
            if (info == null || info.CustomProperties == null) return "";
            object v;
            if (info.CustomProperties.TryGetValue(RoomLabelPropertyKey, out v) && v != null)
                return v.ToString();
            return "";
        }

        private void StartServerBrowse()
        {
            if (isBrowsingServers) return;

            isBrowsingServers = true;
            pendingRejoinPrevious = false;
            cachedRooms.Clear();
            selectedRoomName = "";
            previousRoomName = PhotonNetwork.InRoom && PhotonNetwork.CurrentRoom != null
                ? PhotonNetwork.CurrentRoom.Name
                : "";

            if (rejoinCoroutine != null)
            {
                StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = null;
            }
            if (restoreBrowsePositionCoroutine != null)
            {
                StopCoroutine(restoreBrowsePositionCoroutine);
                restoreBrowsePositionCoroutine = null;
            }

            serverListStatus = "STARTING...";

            // Stop spectating / following so we don't leave the player in a weird state
            if (spectating) StopSpectating();
            followPlayerActorId = -1;

            if (PhotonNetwork.InRoom)
            {
                // 1) Save pos (+ prefab/genes for clean respawn)
                CaptureBrowseTransform();
                // 2) Delete local body so nothing ghosts / fights the restore
                DestroyLocalPlayerBodyForBrowse();
                // 3) Leave → lobby → room list → auto rejoin → 4) restore pos
                //    (body already destroyed — skip LeaveRoomSafe double-destroy)
                pendingRejoinPrevious = !string.IsNullOrEmpty(previousRoomName);
                serverListStatus = browseHasSavedTransform
                    ? ("BODY GONE · leaving (pos " + browseSavedPosition.x.ToString("0.0") + ", " + browseSavedPosition.z.ToString("0.0") + ")")
                    : "LEAVING CURRENT ROOM… (no pos saved)";
                try { PhotonNetwork.LeaveRoom(); } catch (Exception ex) { Logger.LogWarning("Browse LeaveRoom: " + ex.Message); }
            }
            else if (PhotonNetwork.InLobby)
            {
                serverListStatus = "IN LOBBY - WAITING FOR LIST...";
                try { PhotonNetwork.JoinLobby(); } catch { }
            }
            else if (PhotonNetwork.IsConnected)
            {
                serverListStatus = "CONNECTED → JOINING LOBBY...";
                TryJoinLobbyForBrowse();
            }
            else
            {
                serverListStatus = "NOT CONNECTED TO PHOTON";
                isBrowsingServers = false;
            }
        }

        private void TryJoinLobbyForBrowse()
        {
            if (!isBrowsingServers) return;
            if (PhotonNetwork.InLobby) return;
            if (PhotonNetwork.InRoom) return;

            try
            {
                serverListStatus = "JOINING LOBBY...";
                PhotonNetwork.JoinLobby();
            }
            catch (Exception ex)
            {
                serverListStatus = "JOIN LOBBY FAILED: " + ex.Message;
                Logger.LogWarning("Server browse JoinLobby failed: " + ex);
            }
        }

        private void StopServerBrowse()
        {
            isBrowsingServers = false;
            pendingRejoinPrevious = false;
            if (rejoinCoroutine != null)
            {
                StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = null;
            }
            serverListStatus = cachedRooms.Count > 0
                ? "STOPPED • " + cachedRooms.Count + " rooms still cached"
                : "STOPPED";
        }

        private void JoinSelectedServer()
        {
            if (string.IsNullOrEmpty(selectedRoomName)) return;
            if (!cachedRooms.TryGetValue(selectedRoomName, out RoomInfo info)) return;
            if (!info.IsOpen)
            {
                serverListStatus = "ROOM IS CLOSED";
                return;
            }

            // Already in that room
            if (PhotonNetwork.InRoom &&
                PhotonNetwork.CurrentRoom != null &&
                PhotonNetwork.CurrentRoom.Name == selectedRoomName)
            {
                serverListStatus = "ALREADY IN THAT ROOM";
                return;
            }

            // Cancel any pending auto-rejoin — user is switching rooms intentionally
            isBrowsingServers = false;
            pendingRejoinPrevious = false;
            browseHasSavedTransform = false;
            browseRestoreActive = false;
            if (rejoinCoroutine != null)
            {
                StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = null;
            }
            if (restoreBrowsePositionCoroutine != null)
            {
                StopCoroutine(restoreBrowsePositionCoroutine);
                restoreBrowsePositionCoroutine = null;
            }

            pendingJoinRoomName = selectedRoomName;

            if (PhotonNetwork.InRoom)
            {
                // Must leave current room before joining another
                serverListStatus = "LEAVING → JOINING " + selectedRoomName + "...";
                LeaveRoomSafe();
            }
            else if (PhotonNetwork.InLobby || PhotonNetwork.IsConnectedAndReady)
            {
                serverListStatus = "JOINING " + selectedRoomName + "...";
                PhotonNetwork.JoinRoom(selectedRoomName);
            }
            else
            {
                serverListStatus = "NOT READY TO JOIN";
                pendingJoinRoomName = "";
            }
        }

        private IEnumerator RejoinPreviousRoomAfterDelay()
        {
            // Wait until we have rooms (or timeout) so the list isn't empty when we rejoin.
            float start = Time.unscaledTime;
            const float minWait = 1.25f;
            const float maxWait = 4.0f;

            while (Time.unscaledTime - start < maxWait)
            {
                if (!pendingRejoinPrevious || string.IsNullOrEmpty(previousRoomName))
                {
                    rejoinCoroutine = null;
                    yield break;
                }

                bool haveList = cachedRooms.Count > 0;
                bool minElapsed = Time.unscaledTime - start >= minWait;

                if (haveList && minElapsed)
                    break;

                yield return null;
            }

            if (!pendingRejoinPrevious || string.IsNullOrEmpty(previousRoomName))
            {
                rejoinCoroutine = null;
                yield break;
            }

            if (PhotonNetwork.InRoom)
            {
                pendingRejoinPrevious = false;
                rejoinCoroutine = null;
                yield break;
            }

            if (!PhotonNetwork.InLobby)
            {
                TryJoinLobbyForBrowse();
                yield return new WaitForSecondsRealtime(0.03f);
            }

            serverListStatus = "REJOINING " + previousRoomName + " • " + cachedRooms.Count + " rooms cached";
            pendingRejoinPrevious = false;

            try
            {
                PhotonNetwork.JoinRoom(previousRoomName);
            }
            catch (Exception ex)
            {
                serverListStatus = "REJOIN ERROR: " + ex.Message;
                Logger.LogWarning("Auto-rejoin failed: " + ex);
            }

            rejoinCoroutine = null;
        }

        // ============================================================
        // PHOTON CALLBACKS (required by the interfaces)
        // ============================================================
        public void OnConnected() { }

        public void OnConnectedToMaster()
        {
            // User picked a different room while in a room — finish the switch after a short delay
            if (!string.IsNullOrEmpty(pendingJoinRoomName) && !PhotonNetwork.InRoom)
            {
                StartCoroutine(JoinPendingRoomAfterDelay());
                return;
            }

            // Scanner coroutine owns connection flow
            if (scanRunning)
                return;

            if (peekInProgress && peekAwaitingTargetJoin && !string.IsNullOrEmpty(peekTargetRoom) && !PhotonNetwork.InRoom)
            {
                BeginPeekJoin();
                return;
            }

            if (!PhotonNetwork.InRoom && (peekNeedLobbyAfterLeave || (!peekInProgress && peekRejoinAfter)))
            {
                TryReturnToLobbyAfterPeek();
                return;
            }

            // After LeaveRoom during browse, Photon often lands here. Join lobby for the list.
            if (isBrowsingServers && !PhotonNetwork.InRoom && !PhotonNetwork.InLobby)
            {
                serverListStatus = "ON MASTER → JOINING LOBBY...";
                TryJoinLobbyForBrowse();
            }
        }

        private IEnumerator JoinPendingRoomAfterDelay()
        {
            if (joinPendingInProgress)
                yield break;

            string target = pendingJoinRoomName;
            if (string.IsNullOrEmpty(target))
                yield break;

            joinPendingInProgress = true;
            serverListStatus = "WAITING 0.03s→ JOINING " + target + "...";
            yield return new WaitForSecondsRealtime(0.03f);

            // Still the same pending target and not already in a room?
            if (pendingJoinRoomName != target || PhotonNetwork.InRoom)
            {
                joinPendingInProgress = false;
                yield break;
            }

            pendingJoinRoomName = "";
            serverListStatus = "JOINING " + target + "...";

            try
            {
                PhotonNetwork.JoinRoom(target);
            }
            catch (Exception ex)
            {
                serverListStatus = "JOIN ERROR: " + ex.Message;
                Logger.LogWarning("Join selected room failed: " + ex);
            }

            joinPendingInProgress = false;
        }

        public void OnDisconnected(DisconnectCause cause)
        {
            if (isBrowsingServers)
            {
                isBrowsingServers = false;
                pendingRejoinPrevious = false;
                if (rejoinCoroutine != null)
                {
                    StopCoroutine(rejoinCoroutine);
                    rejoinCoroutine = null;
                }
                serverListStatus = "DISCONNECTED: " + cause;
            }
        }
        public void OnRegionListReceived(RegionHandler regionHandler) { }
        public void OnCustomAuthenticationResponse(Dictionary<string, object> data) { }
        public void OnCustomAuthenticationFailed(string debugMessage) { }

        public void OnJoinedLobby()
        {
            if (!isBrowsingServers)
                return;

            serverListStatus = "IN LOBBY - RECEIVING ROOM LIST...";

            // Schedule auto-rejoin (waits for list first)
            if (pendingRejoinPrevious && !string.IsNullOrEmpty(previousRoomName))
            {
                if (rejoinCoroutine != null)
                    StopCoroutine(rejoinCoroutine);
                rejoinCoroutine = StartCoroutine(RejoinPreviousRoomAfterDelay());
            }
        }

        public void OnLeftLobby()
        {
            if (isBrowsingServers && !pendingRejoinPrevious)
                serverListStatus = "LEFT LOBBY";
        }

        public void OnRoomListUpdate(List<RoomInfo> roomList)
        {
            if (roomList == null) return;

            foreach (RoomInfo info in roomList)
            {
                if (info.RemovedFromList || !info.IsVisible)
                {
                    cachedRooms.Remove(info.Name);
                }
                else
                {
                    cachedRooms[info.Name] = info;
                }
            }

            lastRoomListUpdateTime = Time.unscaledTime;

            if (isBrowsingServers || cachedRooms.Count > 0)
            {
                if (pendingRejoinPrevious)
                    serverListStatus = "GOT LIST • " + cachedRooms.Count + " rooms • rejoining soon...";
                else if (PhotonNetwork.InRoom)
                    serverListStatus = "CACHED • " + cachedRooms.Count + " rooms (back in room)";
                else
                    serverListStatus = "LIVE • " + cachedRooms.Count + " rooms";
            }
        }

        public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics) { }

        public void OnFriendListUpdate(List<FriendInfo> friendList) { }
        public void OnCreatedRoom()
        {
            // New room: seed player list into lobby-visible props immediately
            if (publishRoomPlayers && PhotonNetwork.IsMasterClient)
            {
                lastPublishedRoomPlayers = "";
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
            }
        }
        public void OnCreateRoomFailed(short returnCode, string message) { }

        private IEnumerator PeekAfterJoinRoutine()
        {
            // Wait until PlayerList is populated (or timeout). Game + Photon can lag a bit.
            float deadline = Time.unscaledTime + 2.5f;
            int bestCount = 0;

            while (Time.unscaledTime < deadline && peekInProgress && PhotonNetwork.InRoom)
            {
                Player[] players = PhotonNetwork.PlayerList;
                int count = players != null ? players.Length : 0;
                if (count > bestCount)
                    bestCount = count;

                int expected = 0;
                if (!string.IsNullOrEmpty(peekTargetRoom) &&
                    cachedRooms.TryGetValue(peekTargetRoom, out RoomInfo info) &&
                    info != null)
                {
                    expected = info.PlayerCount;
                }

                if (expected > 0 && count >= expected)
                    break;
                if (count > 0 && Time.unscaledTime > deadline - 1.5f)
                    break;

                yield return new WaitForSecondsRealtime(0.1f);
            }

            yield return null;

            if (peekInProgress)
                FinishPeekAndLeave();

            peekCoroutine = null;
        }

        // ---- IInRoomCallbacks ----
        public void OnPlayerEnteredRoom(Player newPlayer)
        {
            if (publishRoomPlayers && PhotonNetwork.IsMasterClient)
            {
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
            }

            // New player joined → splash them with water (desync helper)
            if (autoSplashOnJoin && newPlayer != null && !newPlayer.IsLocal)
                ScheduleSplashNewPlayer(newPlayer);
        }

        public void OnPlayerLeftRoom(Player otherPlayer)
        {
            if (publishRoomPlayers && PhotonNetwork.IsMasterClient)
            {
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
            }

            // Clean up leftover kobold bodies when someone leaves (host can destroy)
            if (destroyBodyOnLeave && otherPlayer != null)
                CleanupBodiesForActor(otherPlayer.ActorNumber);
        }

        /// <summary>
        /// Destroy orphaned kobold PhotonViews that belonged to a player who left.
        /// Master client can destroy any view; others only destroy if somehow still IsMine.
        /// </summary>
        private void CleanupBodiesForActor(int actorNumber)
        {
            if (actorNumber <= 0)
                return;

            try
            {
                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views == null) return;

                int destroyed = 0;
                for (int i = 0; i < views.Length; i++)
                {
                    PhotonView view = views[i];
                    if (view == null || view.gameObject == null) continue;

                    bool match = false;
                    try
                    {
                        if (view.OwnerActorNr == actorNumber)
                            match = true;
                        else if (view.CreatorActorNr == actorNumber)
                            match = true;
                        else if (view.Owner != null && view.Owner.ActorNumber == actorNumber)
                            match = true;
                    }
                    catch { }

                    if (!match) continue;
                    if (GetKoboldOn(view.gameObject) == null && !IsValidPlayerKoboldObject(view.gameObject))
                        continue;

                    try
                    {
                        if (PhotonNetwork.IsMasterClient || view.IsMine)
                        {
                            Logger.LogInfo("CleanupBodiesForActor: destroy " + view.gameObject.name +
                                           " actor=" + actorNumber + " view=" + view.ViewID);
                            PhotonNetwork.Destroy(view.gameObject);
                            destroyed++;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("CleanupBodiesForActor destroy: " + ex.Message);
                    }
                }

                if (playerObjectCache.ContainsKey(actorNumber))
                    playerObjectCache.Remove(actorNumber);

                if (destroyed > 0)
                    Logger.LogInfo("CleanupBodiesForActor #" + actorNumber + " destroyed=" + destroyed);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("CleanupBodiesForActor: " + ex.Message);
            }
        }

        public void OnRoomPropertiesUpdate(ExitGames.Client.Photon.Hashtable propertiesThatChanged) { }

        public void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps) { }

        public void OnMasterClientSwitched(Player newMasterClient)
        {
            // Non-mod host left → we became master: start publishing names for the lobby
            if (newMasterClient != null && newMasterClient.IsLocal)
            {
                publishRoomPlayers = true;
                if (configPublishRoomPlayers != null)
                    configPublishRoomPlayers.Value = true;
                lastPublishedRoomPlayers = "";
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
                Logger.LogInfo("Became master — publishing room player list");
            }
        }

        public void OnJoinedRoom()
        {
            pendingJoinRoomName = "";
            joinPendingInProgress = false;

            // You joined → splash everyone (skip during room scan)
            if (welcomeMessageOnJoin && !scanRunning && !peekInProgress)
            {
                string room = (PhotonNetwork.CurrentRoom != null) ? PhotonNetwork.CurrentRoom.Name : "room";
                if (string.IsNullOrEmpty(room)) room = "room";
                ShowToast("You've Joined (" + room + ").");
            }
            if (autoSplashOnJoin && !scanRunning && !peekInProgress)
                ScheduleSplashEveryoneOnJoin();

            // Room scanner owns the join when scanRunning — do not start old peek routine
            if (scanRunning)
            {
                SnapshotRoomPlayers(PhotonNetwork.CurrentRoom != null
                    ? PhotonNetwork.CurrentRoom.Name
                    : scanCurrentRoom);
                return;
            }

            if (peekInProgress && !scanRunning)
            {
                if (peekCoroutine != null)
                    StopCoroutine(peekCoroutine);
                peekCoroutine = StartCoroutine(PeekAfterJoinRoutine());
                return;
            }

            // Auto-rejoin of previous room → keep the cached list + restore last position
            string joinedName = PhotonNetwork.CurrentRoom != null ? PhotonNetwork.CurrentRoom.Name : "";
            bool isAutoRejoin = !string.IsNullOrEmpty(previousRoomName) &&
                               !string.IsNullOrEmpty(joinedName) &&
                               string.Equals(joinedName, previousRoomName, StringComparison.Ordinal);

            Logger.LogInfo("OnJoinedRoom name=" + joinedName + " previous=" + previousRoomName +
                           " autoRejoin=" + isAutoRejoin + " hasSavedPos=" + browseHasSavedTransform);

            // Host: seed player list into room props for browser hover
            if (publishRoomPlayers && PhotonNetwork.IsMasterClient)
            {
                lastPublishedRoomPlayers = "";
                nextRoomPlayersPublishTime = 0f;
                PublishRoomPlayerList(true);
            }

            if (isAutoRejoin)
            {
                isBrowsingServers = false;
                pendingRejoinPrevious = false;
                serverListStatus = "CACHED • " + cachedRooms.Count + " rooms (back in your room)";

                if (browseHasSavedTransform && browsePositionRestoreEnabled)
                {
                    // Continuous restore for ~6s so spawn/network snaps lose
                    browseRestoreActive = true;
                    browseRestoreUntil = Time.unscaledTime + 6f;
                    browseRestoreHits = 0;
                    serverListStatus = "CACHED • restoring pos " +
                        browseSavedPosition.x.ToString("0.0") + "," +
                        browseSavedPosition.z.ToString("0.0") + " …";
                    Logger.LogInfo("Server browse: starting continuous restore to " + browseSavedPosition);

                    if (restoreBrowsePositionCoroutine != null)
                        StopCoroutine(restoreBrowsePositionCoroutine);
                    // Also run coroutine as a delayed first shove once body exists
                    restoreBrowsePositionCoroutine = StartCoroutine(RestoreBrowseTransformAfterSpawn());
                }
                else if (browseHasSavedTransform && !browsePositionRestoreEnabled)
                {
                    serverListStatus = "CACHED • " + cachedRooms.Count + " rooms (pos restore OFF)";
                    Logger.LogInfo("Server browse: position restore disabled — skipping restore");
                }
            }
            else
            {
                isBrowsingServers = false;
                pendingRejoinPrevious = false;
                browseHasSavedTransform = false;
                browseRestoreActive = false;
                serverListStatus = "JOINED " + (string.IsNullOrEmpty(joinedName) ? "ROOM" : joinedName);
            }
        }

        private GameObject ResolveLocalPlayerBody()
        {
            Component kob = FindLocalKobold();
            if (kob != null)
                return kob.gameObject;

            GameObject local = GetLocalPlayer();
            if (local != null)
                return local;

            return null;
        }

        private void CaptureBrowseTransform()
        {
            browseHasSavedTransform = false;
            browseSavedPrefabName = "";
            browseSavedSpeciesIndex = -1;
            browseSavedGenes = null;
            browseDidRespawnRestore = false;
            browseTriedRespawnRestore = false;
            try
            {
                GameObject local = ResolveLocalPlayerBody();
                if (local == null)
                {
                    Camera cam = Camera.main;
                    if (cam != null)
                    {
                        Vector3 p = cam.transform.position;
                        p.y = Mathf.Max(0.5f, p.y - 1.5f);
                        browseSavedPosition = p;
                        browseSavedRotation = Quaternion.Euler(0f, cam.transform.eulerAngles.y, 0f);
                        browseHasSavedTransform = true;
                        Logger.LogInfo("Server browse: saved camera-approx position " + browseSavedPosition);
                        return;
                    }
                    Logger.LogWarning("Server browse: could not find local player to save position");
                    return;
                }

                browseSavedPosition = local.transform.position;
                browseSavedRotation = local.transform.rotation;
                browseHasSavedTransform = true;

                // Prefab resource name (for respawn-at-pos after rejoin)
                string goName = local.name ?? "";
                if (goName.EndsWith("(Clone)"))
                    goName = goName.Substring(0, goName.Length - "(Clone)".Length).Trim();
                browseSavedPrefabName = goName;

                // Genes + species so official spawn path can rebuild body at saved coords
                try
                {
                    ResolveGeneTypes();
                    Component kob = GetKoboldOn(local) ?? FindLocalKobold();
                    if (kob != null && getGenesMethod != null)
                    {
                        object genes = getGenesMethod.Invoke(kob, null);
                        if (genes != null)
                        {
                            browseSavedGenes = CloneGenes(genes);
                            FieldInfo sp = AccessTools.Field(genes.GetType(), "species");
                            if (sp != null)
                            {
                                object v = sp.GetValue(genes);
                                if (v != null)
                                    browseSavedSpeciesIndex = Convert.ToInt32(v);
                            }
                        }
                    }

                    // Prefer official prefab key from Player DB when species is known
                    if (browseSavedSpeciesIndex >= 0)
                    {
                        object playerDb = GetGamePlayerDatabase();
                        List<object> infos = GetValidPrefabInfos(playerDb);
                        if (infos != null && browseSavedSpeciesIndex < infos.Count)
                        {
                            string key = GetPrefabInfoKey(infos[browseSavedSpeciesIndex]);
                            if (!string.IsNullOrEmpty(key))
                                browseSavedPrefabName = key;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("Server browse: genes/prefab capture: " + ex.Message);
                }

                Logger.LogInfo("Server browse: saved position " + browseSavedPosition +
                               " prefab=" + browseSavedPrefabName + " species=" + browseSavedSpeciesIndex);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("CaptureBrowseTransform failed: " + ex.Message);
                browseHasSavedTransform = false;
            }
        }

        private void DestroyLocalPlayerBodyForBrowse()
        {
            try
            {
                int destroyed = 0;

                // 1) Official PUN cleanup for everything owned by local player
                if (PhotonNetwork.InRoom && PhotonNetwork.LocalPlayer != null)
                {
                    try
                    {
                        PhotonNetwork.RemoveRPCs(PhotonNetwork.LocalPlayer);
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("RemoveRPCs: " + ex.Message);
                    }

                    try
                    {
                        PhotonNetwork.DestroyPlayerObjects(PhotonNetwork.LocalPlayer);
                        destroyed++;
                        Logger.LogInfo("DestroyPlayerObjects(LocalPlayer) issued");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogWarning("DestroyPlayerObjects: " + ex.Message);
                    }
                }

                // 2) Explicit IsMine kobold PhotonViews (belt and suspenders)
                PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
                if (views != null)
                {
                    for (int i = 0; i < views.Length; i++)
                    {
                        PhotonView view = views[i];
                        if (view == null || view.gameObject == null) continue;
                        if (!view.IsMine) continue;

                        bool isKobold = GetKoboldOn(view.gameObject) != null;
                        // Also catch player-tagged objects
                        bool isTagged = false;
                        try
                        {
                            if (PhotonNetwork.LocalPlayer != null &&
                                PhotonNetwork.LocalPlayer.TagObject != null)
                            {
                                object tag = PhotonNetwork.LocalPlayer.TagObject;
                                Component tagComp = tag as Component;
                                if (tagComp != null &&
                                    (tagComp.gameObject == view.gameObject ||
                                     tagComp.transform.IsChildOf(view.transform) ||
                                     view.transform.IsChildOf(tagComp.transform)))
                                    isTagged = true;
                            }
                        }
                        catch { }

                        if (!isKobold && !isTagged)
                            continue;

                        try
                        {
                            Logger.LogInfo("Destroying IsMine body view: " + view.gameObject.name + " id=" + view.ViewID);
                            PhotonNetwork.Destroy(view.gameObject);
                            destroyed++;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogWarning("PhotonNetwork.Destroy view failed: " + ex.Message);
                            try
                            {
                                UnityEngine.Object.Destroy(view.gameObject);
                                destroyed++;
                            }
                            catch { }
                        }
                    }
                }

                // 3) Local-only fallback if Photon destroy couldn't run
                Component kob = FindLocalKobold();
                if (kob != null && kob.gameObject != null)
                {
                    PhotonView pv = kob.GetComponent<PhotonView>() ?? kob.GetComponentInParent<PhotonView>();
                    if (pv == null || !PhotonNetwork.InRoom)
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(kob.gameObject);
                            destroyed++;
                            Logger.LogInfo("Local Destroy fallback on kobold");
                        }
                        catch { }
                    }
                }

                cachedLocalPlayer = null;
                if (PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.TagObject = null;

                Logger.LogInfo("DestroyLocalPlayerBodyForBrowse done, ops=" + destroyed);
                if (destroyed > 0)
                    ShowToast("Destroyed local body (" + destroyed + ")");
            }
            catch (Exception ex)
            {
                Logger.LogWarning("DestroyLocalPlayerBodyForBrowse: " + ex.Message);
            }
        }

        /// <summary>
        /// After rejoin the game spawns you at default. Destroy that body and respawn at saved coords
        /// (same approach as character swap) so position actually sticks.
        /// </summary>
        private bool TryRespawnAtBrowsePosition()
        {
            if (!browseHasSavedTransform)
                return false;

            try
            {
                Component kob = FindLocalKobold();
                if (kob == null)
                    return false;

                PhotonView pv = kob.GetComponent<PhotonView>() ?? kob.GetComponentInParent<PhotonView>();
                if (pv == null || !pv.IsMine)
                    return false;

                string photonName = browseSavedPrefabName;
                int speciesIndex = browseSavedSpeciesIndex;
                object genesObj = browseSavedGenes;

                // Fallback: read from the just-spawned body
                if (genesObj == null && getGenesMethod != null)
                {
                    object g = getGenesMethod.Invoke(kob, null);
                    if (g != null) genesObj = CloneGenes(g);
                }
                if (speciesIndex < 0 && genesObj != null)
                {
                    FieldInfo sp = AccessTools.Field(genesObj.GetType(), "species");
                    if (sp != null && sp.GetValue(genesObj) != null)
                        speciesIndex = Convert.ToInt32(sp.GetValue(genesObj));
                }
                if (string.IsNullOrEmpty(photonName) && speciesIndex >= 0)
                {
                    object playerDb = GetGamePlayerDatabase();
                    List<object> infos = GetValidPrefabInfos(playerDb);
                    if (infos != null && speciesIndex < infos.Count)
                        photonName = GetPrefabInfoKey(infos[speciesIndex]);
                }
                if (string.IsNullOrEmpty(photonName))
                {
                    string n = pv.gameObject.name ?? "";
                    if (n.EndsWith("(Clone)"))
                        n = n.Substring(0, n.Length - "(Clone)".Length).Trim();
                    photonName = n;
                }
                if (string.IsNullOrEmpty(photonName))
                {
                    Logger.LogWarning("TryRespawnAtBrowsePosition: no prefab name");
                    return false;
                }

                Vector3 pos = browseSavedPosition;
                Quaternion rot = browseSavedRotation;

                if (speciesIndex >= 0)
                    TrySetSelectedPlayerPrefab(speciesIndex, photonName);

                PhotonNetwork.Destroy(pv.gameObject);
                cachedLocalPlayer = null;
                if (PhotonNetwork.LocalPlayer != null)
                    PhotonNetwork.LocalPlayer.TagObject = null;

                if (speciesIndex >= 0 && TryOfficialSpawnPlayer(pos, rot, photonName, genesObj, speciesIndex))
                {
                    Logger.LogInfo("Server browse: official respawn at " + pos);
                    return true;
                }

                GameObject spawned = PhotonNetwork.Instantiate(photonName, pos, rot, 0);
                if (spawned == null)
                    return false;

                cachedLocalPlayer = spawned;
                Component newKob = GetKoboldOn(spawned);
                if (newKob != null)
                {
                    if (PhotonNetwork.LocalPlayer != null)
                        PhotonNetwork.LocalPlayer.TagObject = newKob;
                    if (genesObj != null && setGenesMethod != null)
                        setGenesMethod.Invoke(newKob, new object[] { CloneGenes(genesObj) });
                }

                Logger.LogInfo("Server browse: bare respawn at " + pos + " as " + photonName);
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogWarning("TryRespawnAtBrowsePosition: " + ex.Message);
                return false;
            }
        }

        private void ApplyTeleportToBody(GameObject body, Vector3 destination, Quaternion rotation)
        {
            if (body == null)
                return;

            CharacterController[] ccs = body.GetComponentsInChildren<CharacterController>(true);
            if (ccs != null)
            {
                for (int i = 0; i < ccs.Length; i++)
                    if (ccs[i] != null) ccs[i].enabled = false;
            }

            Rigidbody[] rbs = body.GetComponentsInChildren<Rigidbody>(true);
            if (rbs != null)
            {
                for (int i = 0; i < rbs.Length; i++)
                {
                    Rigidbody rb = rbs[i];
                    if (rb == null) continue;
                    try
                    {
                        rb.velocity = Vector3.zero;
                        rb.angularVelocity = Vector3.zero;
                    }
                    catch { }
                    rb.position = destination;
                    rb.rotation = rotation;
                }
            }

            body.transform.position = destination;
            body.transform.rotation = rotation;

            PhotonView pv = body.GetComponent<PhotonView>() ?? body.GetComponentInParent<PhotonView>();
            if (pv != null)
            {
                Transform root = pv.transform;
                root.position = destination;
                root.rotation = rotation;
            }

            if (ccs != null)
            {
                for (int i = 0; i < ccs.Length; i++)
                    if (ccs[i] != null) ccs[i].enabled = true;
            }
        }

        /// <summary>
        /// Strong teleport used after rejoin — hits every local IsMine kobold body.
        /// </summary>
        private void ForceTeleportLocalPlayer(Vector3 destination, Quaternion rotation)
        {
            GameObject primary = ResolveLocalPlayerBody();
            if (primary != null)
            {
                ApplyTeleportToBody(primary, destination, rotation);
                cachedLocalPlayer = primary;
            }

            // Belt-and-suspenders: any other IsMine PhotonView that hosts a Kobold
            PhotonView[] views = UnityEngine.Object.FindObjectsOfType<PhotonView>();
            if (views == null)
                return;

            for (int i = 0; i < views.Length; i++)
            {
                PhotonView view = views[i];
                if (view == null || !view.IsMine)
                    continue;
                Component k = GetKoboldOn(view.gameObject);
                if (k == null)
                    continue;
                if (primary != null && (k.gameObject == primary || view.gameObject == primary))
                    continue;
                ApplyTeleportToBody(k.gameObject, destination, rotation);
            }
        }

        private IEnumerator RestoreBrowseTransformAfterSpawn()
        {
            // Wait until a local body exists, then leave continuous Update loop to keep shoving
            float findDeadline = Time.unscaledTime + 8f;
            while (Time.unscaledTime < findDeadline)
            {
                if (!PhotonNetwork.InRoom)
                    break;
                if (ResolveLocalPlayerBody() != null)
                {
                    Logger.LogInfo("Server browse: local body found, continuous restore is active");
                    break;
                }
                yield return null;
            }

            if (ResolveLocalPlayerBody() == null)
            {
                Logger.LogWarning("Server browse: never found local body during restore wait");
                serverListStatus = "CACHED • " + cachedRooms.Count + " rooms (no body for pos restore)";
            }

            restoreBrowsePositionCoroutine = null;
        }

        public void OnJoinRoomFailed(short returnCode, string message)
        {
            pendingJoinRoomName = "";
            joinPendingInProgress = false;

            if (scanRunning)
            {
                // Include return code — GameClosed=32764, GameFull=32765, etc.
                scanLastError = message + " (" + returnCode + ")";
                peekStatus = "Join failed: " + scanLastError;
                peekStatusUntil = Time.unscaledTime + 3f;
                serverListStatus = "SCAN FAIL: " + scanLastError;
                // Do not JoinLobby here — scanner coroutine owns the next attempt
                return;
            }

            if (peekInProgress)
            {
                peekInProgress = false;
                peekTargetRoom = "";
                peekAwaitingTargetJoin = false;
                peekNeedLobbyAfterLeave = true;
                peekStatus = "Scan failed: " + message;
                peekStatusUntil = Time.unscaledTime + 4f;
                serverListStatus = "PEEK FAILED: " + message;
                TryReturnToLobbyAfterPeek();
            }
            else if (pendingRejoinPrevious)
            {
                pendingRejoinPrevious = false;
                serverListStatus = "REJOIN FAILED (" + message + ") • list still available (" + cachedRooms.Count + ")";
            }
            else
            {
                serverListStatus = "JOIN FAILED: " + message;
            }

            if (!PhotonNetwork.InLobby && PhotonNetwork.IsConnectedAndReady)
                TryJoinLobbyForBrowse();
        }

        public void OnJoinRandomFailed(short returnCode, string message) { }

        public void OnLeftRoom()
        {
            // Switching to a selected room: leave finished → wait for master / delay, then join
            if (!string.IsNullOrEmpty(pendingJoinRoomName))
            {
                serverListStatus = "LEFT ROOM → WAITING 1s → " + pendingJoinRoomName;
                if (PhotonNetwork.IsConnectedAndReady)
                    StartCoroutine(JoinPendingRoomAfterDelay());
                return;
            }

            // Scanner coroutine owns leave/join — do not interfere
            if (scanRunning)
                return;

            // Legacy peek paths
            if (peekInProgress && peekAwaitingTargetJoin && !string.IsNullOrEmpty(peekTargetRoom))
            {
                serverListStatus = "PEEK · left room → joining " + peekTargetRoom;
                if (PhotonNetwork.IsConnectedAndReady)
                    BeginPeekJoin();
                return;
            }

            if (peekInProgress)
            {
                peekInProgress = false;
                peekTargetRoom = "";
                peekAwaitingTargetJoin = false;
                TryReturnToLobbyAfterPeek();
                return;
            }

            if (peekNeedLobbyAfterLeave)
            {
                TryReturnToLobbyAfterPeek();
                return;
            }

            // Browse flow: left room → OnConnectedToMaster usually follows, then we JoinLobby there.
            if (isBrowsingServers)
            {
                serverListStatus = "LEFT ROOM → WAITING FOR MASTER...";
                if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InLobby)
                    TryJoinLobbyForBrowse();
            }
        }
    }
}

