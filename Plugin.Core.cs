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
    /// <summary>
    /// Core plugin initialization and startup
    /// </summary>
    public partial class Plugin : BaseUnityPlugin, IConnectionCallbacks, ILobbyCallbacks, IMatchmakingCallbacks, IInRoomCallbacks, IOnEventCallback
    {
        private static Plugin Instance;
        private Harmony spectateHarmony;

        private void Awake()
        {
            Instance = this;
            PhotonNetwork.AddCallbackTarget(this);
            BindConfig();
            FindPreparePool();
            StartCoroutine(LoadBackgroundAfterStartup());
            spectateHarmony = new Harmony("com.zex.qolmenu.spectate");
            PatchOrbitCamera();
            PatchPhotonRoomCreation();
            StartCoroutine(DelayedPatchScanModSkip());
            StartCoroutine(InitialPrefabScan());
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
                Type orbitCameraType = SafeGameType("OrbitCamera");

                if (orbitCameraType == null)
                {
                    Logger.LogWarning("Spectate: OrbitCamera type was not found.");
                    return;
                }

                MethodInfo lateUpdate = AccessTools.Method(orbitCameraType, "LateUpdate");

                if (lateUpdate == null)
                {
                    Logger.LogWarning("Spectate: OrbitCamera.LateUpdate was not found.");
                    return;
                }

                spectateHarmony.Patch(
                    lateUpdate,
                    new HarmonyMethod(typeof(Plugin), nameof(OrbitCameraLateUpdatePrefix))
                );

                Logger.LogInfo("Spectate: OrbitCamera.LateUpdate patched.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Spectate patch failed: " + ex);
            }
        }

        /// <summary>
        /// Ensure ZexQoLPlayers is in CustomRoomProperties + CustomRoomPropertiesForLobby
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
                    new Type[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) });
                if (create != null)
                {
                    spectateHarmony.Patch(create, new HarmonyMethod(typeof(Plugin), nameof(PhotonCreateRoomPrefix)));
                    patched++;
                }

                // JoinOrCreateRoom(string, RoomOptions, TypedLobby, string[])
                MethodInfo joinOrCreate = AccessTools.Method(
                    photonNet,
                    "JoinOrCreateRoom",
                    new Type[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) });
                if (joinOrCreate != null)
                {
                    spectateHarmony.Patch(joinOrCreate, new HarmonyMethod(typeof(Plugin), nameof(PhotonJoinOrCreateRoomPrefix)));
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
                        spectateHarmony.Patch(m, new HarmonyMethod(typeof(Plugin), nameof(PhotonRoomOptionsPrefixGeneric)));
                        patched++;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("PatchPhotonRoomCreation failed: " + ex);
            }
        }
    }
}
