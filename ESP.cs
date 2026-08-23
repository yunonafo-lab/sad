using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;

namespace ZexQoLMenu
{
    /// <summary>
    /// Player ESP rendering, overlays, and visibility tracking
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // ESP
        // ============================================================
        protected bool showNames = true;
        protected bool showDistance = true;
        protected bool showActorID = false;
        protected bool hideSelf = true;
        protected bool tracersEnabled = false;
        protected bool visibilityCheck = false;
        protected bool offscreenArrows = true;
        protected float maxDistance = 240f;
        protected float tracerThickness = 1.0f;
        protected int tracerOrigin = 0;
        protected bool tracerDistanceFade = true;
        protected bool scaleNames = true;
        protected const float NameHeight = 1.7f;
        protected const float MinNameScale = 0.70f;
        protected const float MaxNameScale = 1.15f;
        protected const float OffscreenArrowSize = 18f;
        protected const float OffscreenArrowMargin = 35f;
        protected readonly Color playerColor = new Color(1f, .2f, 0f);
        protected readonly Color hiddenColor = new Color(.55f, .55f, .60f);
        protected GUIStyle espStyle;
        protected Material tracerMaterial;
        // Track current esp font size because GUIStyle.fontSize has no getter.
        protected int espFontSize = 14;

        protected int normalColorIndex;
        protected int selectedColorIndex = 1;
        protected int friendColorIndex = 2;

        protected readonly Color[] espColorOptions =
        {
            new Color(1f, .2f, 0f),
            new Color(.75f, .25f, 1f),
            new Color(.2f, 1f, .35f),
            new Color(1f, .85f, .15f),
            new Color(.2f, .75f, 1f),
            new Color(1f, .35f, .65f),
            Color.white
        };

        protected readonly string[] espColorNames =
        {
            "RED", "PURPLE", "GREEN", "YELLOW", "CYAN", "PINK", "WHITE"
        };

        protected HashSet<int> friendActorIds = new HashSet<int>();

        // ============================================================
        // PLAYER OVERLAY
        // ============================================================
        protected Rect playerOverlayRect = new Rect(10f, 10f, 300f, 220f);
        protected Vector2 playerOverlayScroll = Vector2.zero;
        protected bool showPlayerOverlay = true;

        // ============================================================
        // PLAYER CONTEXT MENU / RADAR
        // ============================================================
        protected Player contextPlayer;
        protected bool playerContextMenuVisible;
        protected Vector2 playerContextMenuPosition;
        protected bool targetLocked;

        protected int followPlayerActorId = -1;
        protected float followDistance = 3f;
        protected float followHeight = 0.5f;

        protected Rect playerRadarRect = new Rect(20f, 250f, 230f, 230f);
        protected bool showPlayerRadar = true;
        protected bool radarRotateWithCamera = true;
        protected bool radarShowNames = true;
        protected bool radarShowDistance = true;

        protected float radarRange = 30f;
        protected const float RadarMinSize = 170f;
        protected const float RadarMaxSize = 360f;
    }
}
