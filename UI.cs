using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Menu UI rendering, styles, and visual management
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // MASTER UI
        // ============================================================
        protected bool menuVisible = true;
        protected int tab = 0; // 0 ESP, 1 SPAWNER, 2 TELEPORT, 3 HOST TOOLS, 4 MISC, 5 SERVERS, 6 SILLYS, 7 HOST LOGS, 8 GENES, 9 TESTING, 10 SETTINGS, 11 QOL, 12 MODS
        protected Rect menuRect = new Rect(90f, 45f, 1000f, 780f);

        // Modern menu shell inspired by the supplied Neverlose-style reference.
        protected GUIStyle windowStyle;
        protected GUIStyle sidebarStyle;
        protected GUIStyle sidebarSelectedStyle;
        protected GUIStyle topBarStyle;
        protected GUIStyle cardStyle;
        protected GUIStyle sectionStyle;
        protected GUIStyle valueStyle;
        protected GUIStyle accentLabelStyle;
        protected GUIStyle modernButtonStyle;
        protected GUIStyle modernSelectedButtonStyle;
        protected GUIStyle modernSmallStyle;
        protected Texture2D uiWindowTexture;
        protected Texture2D uiSidebarTexture;
        protected Texture2D uiCardTexture;
        protected Texture2D uiButtonTexture;
        protected Texture2D uiButtonHoverTexture;
        protected Texture2D uiButtonActiveTexture;
        protected Texture2D uiAccentTexture;
        protected Texture2D uiMutedTexture;
        protected bool sillysNameEditing;
        protected string sillysName = "Someone";

        protected GUIStyle labelStyle;
        protected GUIStyle buttonStyle;
        protected GUIStyle selectedButtonStyle;
        protected GUIStyle headerStyle;
        protected GUIStyle smallStyle;
        protected GUIStyle overlayHeaderStyle;
        protected GUIStyle overlayPlayerStyle;
        protected GUIStyle overlayInfoStyle;
        protected GUIStyle overlayRoleStyle;
        protected GUIStyle overlayServerStyle;
        protected bool stylesCreated;

        // Background from the ESP mod.
        protected Texture2D menuBackground;
        protected Material backgroundMaterial;

        // Background appearance controls
        protected float backgroundHue = 0f;
        protected float backgroundOpacity = 1f;
        // true = greyscale chrome (no saturation)
        protected bool menuColorGreyscale;
        // true = hue slowly cycles; false = stay on current hue (LOCK)
        protected bool menuHueCycling = true;
        // Full rainbow loop duration in seconds while RGB CYCLE is on
        protected float menuHueCycleSeconds = 20f;
        protected const float MenuHueCycleSecondsMin = 5f;
        protected const float MenuHueCycleSecondsMax = 120f;

        protected const float MaxMoneyValue = 999999f;
        protected const int MaxStarsValue = 999999;
        protected string rewardStatus = "";
        protected float rewardStatusUntil;

        // Animated sprite-sheet background
        protected const int BackgroundColumns = 5;
        protected const int BackgroundRows = 18;
        protected const int BackgroundFrameCount = BackgroundColumns * BackgroundRows;
        protected float BackgroundFramesPerSecond = 24f;

        // Global status toast queue (one visible at a time)
        protected string toastMessage = "";
        protected float toastUntil;
        protected System.Collections.Generic.Queue<string> toastQueue = new System.Collections.Generic.Queue<string>();
        protected const float ToastDuration = 2.8f;

        // Global search + collapsible sections
        protected string globalSearchText = "";
        protected bool globalSearchFocused;
        protected System.Collections.Generic.Dictionary<string, bool> sectionCollapsed = new System.Collections.Generic.Dictionary<string, bool>();

        // UI event stubs - implement in derived classes
        protected virtual void OnMenuGUI() { }
        protected virtual void CreateStyles() { }
        protected virtual void LoadBackgroundAfterStartup() { }
    }
}
