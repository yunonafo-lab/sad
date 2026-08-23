using BepInEx.Configuration;
using Photon.Pun;
using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Configuration and keybind binding
    /// </summary>
    public partial class Plugin
    {
        private ConfigEntry<KeyboardShortcut> menuToggleKey;
        private ConfigEntry<KeyCode> noclipToggleKey;
        private ConfigEntry<KeyCode> waypointQuickSaveKey;
        private ConfigEntry<KeyCode> flySpeedUpKey;
        private ConfigEntry<KeyCode> flySpeedDownKey;
        private ConfigEntry<KeyCode> spectateNextKey;
        private ConfigEntry<KeyCode> spectatePrevKey;

        private ConfigEntry<bool> configSoftTeleport;
        private ConfigEntry<bool> configBrowsePositionRestore;
        private ConfigEntry<bool> configAutoSplashOnJoin;
        private ConfigEntry<bool> configWelcomeMessageOnJoin;
        private ConfigEntry<bool> configDestroyBodyOnLeave;
        private ConfigEntry<bool> configPublishRoomPlayers;
        private ConfigEntry<float> configFlySpeed;

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
        private ConfigEntry<string> configStatsPresets;
        private ConfigEntry<string> configEquipPresets;
        private ConfigEntry<string> configFullPresets;
        private ConfigEntry<string> configModPresets;

        private void BindConfig()
        {
            menuToggleKey = Config.Bind(
                "Controls", "Toggle_menu_visibility",
                new KeyboardShortcut(KeyCode.Insert),
                "Keybind to toggle QoL menu (supports modifiers).");

            noclipToggleKey = Config.Bind(
                "Controls", "Toggle_Noclip",
                KeyCode.F1,
                "Single key to toggle flying noclip (CharCon-style UnityInput).");

            waypointQuickSaveKey = Config.Bind(
                "Controls", "Quick_Waypoint",
                KeyCode.F6,
                "Single key to quick-save a waypoint.");

            flySpeedUpKey = Config.Bind(
                "Controls", "Fly_Speed_Up",
                KeyCode.F3,
                "Increase flying noclip speed by 10.");

            flySpeedDownKey = Config.Bind(
                "Controls", "Fly_Speed_Down",
                KeyCode.F2,
                "Decrease flying noclip speed by 10.");

            spectateNextKey = Config.Bind(
                "Keybinds", "SpectateNext",
                KeyCode.RightBracket,
                "Cycle spectate to next player.");

            spectatePrevKey = Config.Bind(
                "Keybinds", "SpectatePrev",
                KeyCode.LeftBracket,
                "Cycle spectate to previous player.");

            configSoftTeleport = Config.Bind(
                "Teleport", "SoftTeleport", true,
                "When true, teleports lerp smoothly instead of snapping.");

            configBrowsePositionRestore = Config.Bind(
                "Teleport", "BrowsePositionRestore", true,
                "After server-browser refresh/rejoin, restore your saved position (and optional respawn).");

            configAutoSplashOnJoin = Config.Bind(
                "QoL", "AutoSplashOnJoin", true,
                "On join: splash everyone. When someone else joins: splash everyone except them.");

            configWelcomeMessageOnJoin = Config.Bind(
                "QoL", "WelcomeMessageOnJoin", true,
                "When you join a room, show a toast: You've Joined (room name).");

            configDestroyBodyOnLeave = Config.Bind(
                "QoL", "DestroyBodyOnLeave", true,
                "Destroy your local body before leaving a room to reduce clutter.");

            configPublishRoomPlayers = Config.Bind(
                "Servers", "PublishRoomPlayers", true,
                "When host, publish player names into room properties for server-browser hover.");

            configFlySpeed = Config.Bind(
                "Movement", "FlySpeed", 25f,
                "Flying noclip speed (CharCon range ~5–500).");

            configCameraHeight = Config.Bind(
                "Spectate", "CameraHeight", 0.85f,
                "Default spectate camera height in meters.");

            configCameraDistance = Config.Bind(
                "Spectate", "CameraDistance", 3.25f,
                "Default spectate camera distance in meters.");

            configCameraRotation = Config.Bind(
                "Spectate", "CameraRotation", 0f,
                "Default spectate camera rotation in degrees.");

            configBackgroundHue = Config.Bind(
                "Background", "Hue", 0f,
                "Menu background hue (0-1).");

            configBackgroundOpacity = Config.Bind(
                "Background", "Opacity", 1f,
                "Menu background opacity (0-1).");

            configBackgroundFPS = Config.Bind(
                "Background", "FramesPerSecond", 24f,
                "Menu background animation speed in frames per second.");

            configMenuGreyscale = Config.Bind(
                "Background", "Greyscale", false,
                "When true, menu chrome is greyscale instead of RGB/hue-tinted.");

            configBannedUserIds = Config.Bind(
                "HostTools", "BannedUserIds", "",
                "Comma-separated list of banned Photon UserIds/names. Persists across sessions.");

            configFavoritePrefabNames = Config.Bind(
                "Spawner", "FavoritePrefabNames", "",
                "Comma-separated list of favorited/pinned prefab names. Persists across sessions.");

            configFavoriteRoomNames = Config.Bind(
                "Servers", "FavoriteRoomNames", "",
                "Comma-separated list of favorite room names. Persists across sessions.");

            configStatsPresets = Config.Bind(
                "Genes", "StatsPresets", "",
                "Saved gene/stat presets. Format: name=payload;name2=payload2");

            configEquipPresets = Config.Bind(
                "Genes", "EquipPresets", "",
                "Saved equipment presets. Format: name=payload;name2=payload2");

            configFullPresets = Config.Bind(
                "Genes", "FullPresets", "",
                "Full presets (character + genes + clothing). Format: name=payload;name2=payload2");

            configModPresets = Config.Bind(
                "Lobby", "ModPresets", "",
                "Named mod presets for quick lobby. Format: name=jsonarray;name2=jsonarray");

            ApplyConfigToFields();
            LoadModPresetsFromConfig();
            LoadBannedUserIds();
            LoadFavoritePrefabNames();
            LoadFavoriteRoomNames();
            LoadGenePresetsFromConfig();
            TryAutoImportCharConConfig();
        }

        private void ApplyConfigToFields()
        {
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
        }
    }
}
