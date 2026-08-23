using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Photon.Realtime;

namespace ZexQoLMenu
{
    /// <summary>
    /// Teleportation, waypoints, and movement system
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // TELEPORT
        // ============================================================
        protected Vector2 playerScroll = Vector2.zero;
        protected int selectedActorId = -1;
        protected Player selectedPlayer;
        protected float behindDistance = 3f;
        protected float frontDistance = 3f;
        protected float aboveDistance = 4f;
        protected bool originCaptured;
        protected Vector3 originPosition = Vector3.zero;

        protected readonly Dictionary<string, Vector3> savedWaypoints = new Dictionary<string, Vector3>();
        protected string newWaypointName = "";
        protected bool waypointNameFocused;
        protected Vector2 waypointListScroll = Vector2.zero;
        protected int waypointAutoIndex = 1;

        // Waypoints pop-out (opened from Teleport tab)
        protected bool waypointsPopupVisible;
        protected Rect waypointsPopupRect = new Rect(420f, 160f, 340f, 420f);

        // Soft teleport (lerp)
        protected bool softTeleportEnabled = true;
        protected float softTeleportDuration = 0.35f;
        protected Coroutine softTeleportCoroutine;

        // Flying noclip — CharCon-style: Kobold.body.velocity + OrbitCamera look
        protected bool flyingNoclipActive;
        protected float flySpeed = 25f;
        protected Vector3 flySavedPosition;
        protected FieldInfo koboldBodyField;
        protected int lastHotkeyFrame = -1;
        protected Rigidbody flyCachedBody;
        protected Behaviour flyCachedKoboldController;
        protected bool flyKoboldControllerWasEnabled = true;
        protected string flyDebugStatus = "";
        protected Vector3 flyPendingVelocity;
        protected bool flyHasPendingVelocity;

        // TESTING: ownership + world ping marker
        protected string ownershipStatus = "";
        protected float ownershipStatusUntil;
        protected bool pingMarkActive;
        protected Vector3 pingMarkWorld;
        protected float pingMarkUntil;
        protected const float PingMarkDuration = 8f;

        // Host freeze (best-effort: keep shoving target back to locked pos)
        protected readonly Dictionary<int, Vector3> frozenPlayerPositions = new Dictionary<int, Vector3>();
        protected readonly HashSet<int> frozenActorIds = new HashSet<int>();

        protected readonly Dictionary<string, string> playerNotes = new Dictionary<string, string>();
        protected string notesInput = "";
        protected bool notesInputFocused;

        // Keybinds — CharCon-style: UnityInput.Current for KeyCode / held strings
        protected ConfigEntry<KeyCode> noclipToggleKey;
        protected ConfigEntry<KeyCode> waypointQuickSaveKey;
        protected ConfigEntry<KeyCode> flySpeedUpKey;
        protected ConfigEntry<KeyCode> flySpeedDownKey;
        protected bool waitingForKeyRebind;
        protected string rebindTarget; // "noclip" | "waypoint" | "flyUp" | "flyDown"
    }
}
