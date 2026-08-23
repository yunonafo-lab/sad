using System;
using UnityEngine;
using HarmonyLib;

namespace ZexQoLMenu
{
    /// <summary>
    /// Spectator mode and player camera control
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // QOL / STATUS / SPECTATE / PLAYER COLORS
        // ============================================================
        protected float fpsValue;
        protected float objectCountTimer;
        protected int sceneObjectCount;

        protected bool spectating;
        protected Transform spectateTarget;
        protected Vector3 savedCameraPosition;
        protected GameObject cachedLocalPlayer;
        protected float nextPlayerCacheRefresh;
        protected const float PlayerCacheRefreshInterval = 1f;
        protected Quaternion savedCameraRotation;
        protected bool cameraStateSaved;
        protected float spectateCameraHeight = 0.85f; // Adjustable camera height (0.0 - 5.0 m)
        protected float spectateCameraDistance = 3.25f; // Adjustable camera distance (1.0 - 10.0 m)
        protected int spectateActorId = -1; // Actor ID of the player being spectated
        protected float spectateCameraRotation; // Adjustable rotation around player (0 - 360 degrees)

        protected float spectateYaw;
        protected float spectatePitch = 10f;
        protected const float SpectateMouseSensitivity = 3f;
        protected const float SpectateMinPitch = -75f;
        protected const float SpectateMaxPitch = 75f;

        protected ConfigEntry<KeyCode> spectateNextKey;
        protected ConfigEntry<KeyCode> spectatePrevKey;

        // Keybind rebinding
        protected string rebindTarget_spectate; // "specNext" | "specPrev"
    }
}
