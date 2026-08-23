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
    public partial class Plugin
    {
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
    }
}
