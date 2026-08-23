using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using BepInEx.Configuration;
using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Main Plugin class - all functionality split into modular partial classes
    /// This file contains the BepInPlugin attribute and core lifecycle methods
    /// See corresponding .cs files for feature implementations
    /// </summary>
    [BepInEx.BepInPlugin("Zex_QOL_Menu", "Zex's QOL Menu", "1.0.0")]
    public partial class Plugin : BepInEx.BaseUnityPlugin, Photon.Realtime.IConnectionCallbacks, Photon.Realtime.ILobbyCallbacks, Photon.Realtime.IMatchmakingCallbacks, Photon.Realtime.IInRoomCallbacks, ExitGames.Client.Photon.IOnEventCallback
    {
        private static Plugin Instance;

        private void Awake()
        {
            Instance = this;
            Photon.Pun.PhotonNetwork.AddCallbackTarget(this);
            BindConfig();
            FindPreparePool();
            StartCoroutine(LoadBackgroundAfterStartup());
            
            spectateHarmony = new HarmonyLib.Harmony("com.zex.qolmenu.spectate");
            PatchOrbitCamera();
            PatchPhotonRoomCreation();
            StartCoroutine(DelayedPatchScanModSkip());
            StartCoroutine(InitialPrefabScan());
        }

        private void Update()
        {
            if (menuToggleKey != null && menuToggleKey.Value.IsDown())
            {
                menuVisible = !menuVisible;
            }
        }

        private void OnGUI()
        {
            if (!menuVisible) return;
            
            if (!stylesCreated)
            {
                CreateStyles();
                stylesCreated = true;
            }

            menuRect = GUILayout.Window(0, menuRect, OnMenuGUI, "Zex's QOL Menu");
        }

        protected virtual void OnMenuGUI(int windowId) { }
        protected virtual void CreateStyles() { }
    }
}
