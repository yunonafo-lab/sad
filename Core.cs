using System;
using System.Collections;
using System.Reflection;
using HarmonyLib;
using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace ZexQoLMenu
{
    public partial class Plugin
    {
        private static Type SafeGameType(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;

            try
            {
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                foreach (Assembly asm in assemblies)
                {
                    if (asm == null) continue;
                    string an = asm.GetName().Name ?? "";
                    if (an.StartsWith("Unity", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("System", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("Mono.", StringComparison.OrdinalIgnoreCase) ||
                        an.StartsWith("mscorlib", StringComparison.OrdinalIgnoreCase))
                        continue;

                    bool prefer = an == "Assembly-CSharp" || an == "Assembly-CSharp-firstpass" ||
                        an.IndexOf("Kobold", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (!prefer) continue;

                    Type t = null;
                    try { t = asm.GetType(name, false); }
                    catch { }
                    if (t != null) return t;
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
                if (orbitCameraType == null) return;

                MethodInfo lateUpdate = AccessTools.Method(orbitCameraType, "LateUpdate");
                if (lateUpdate == null) return;

                spectateHarmony.Patch(lateUpdate,
                    new HarmonyMethod(typeof(Plugin), nameof(OrbitCameraLateUpdatePrefix)));
                Logger.LogInfo("Spectate: OrbitCamera.LateUpdate patched.");
            }
            catch (Exception ex)
            {
                Logger.LogError("Spectate patch failed: " + ex);
            }
        }

        private void PatchPhotonRoomCreation()
        {
            try
            {
                Type photonNet = typeof(PhotonNetwork);
                MethodInfo create = AccessTools.Method(photonNet, "CreateRoom",
                    new Type[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) });
                if (create != null)
                    spectateHarmony.Patch(create, new HarmonyMethod(typeof(Plugin), nameof(PhotonCreateRoomPrefix)));

                MethodInfo joinOrCreate = AccessTools.Method(photonNet, "JoinOrCreateRoom",
                    new Type[] { typeof(string), typeof(RoomOptions), typeof(TypedLobby), typeof(string[]) });
                if (joinOrCreate != null)
                    spectateHarmony.Patch(joinOrCreate, new HarmonyMethod(typeof(Plugin), nameof(PhotonJoinOrCreateRoomPrefix)));
            }
            catch (Exception ex)
            {
                Logger.LogError("PatchPhotonRoomCreation failed: " + ex);
            }
        }

        private static bool OrbitCameraLateUpdatePrefix() { return true; }
        private static bool PhotonCreateRoomPrefix() { return true; }
        private static bool PhotonJoinOrCreateRoomPrefix() { return true; }
        private static bool PhotonRoomOptionsPrefixGeneric() { return true; }

        private void FindPreparePool() { }
        private IEnumerator LoadBackgroundAfterStartup() { yield break; }
        private IEnumerator DelayedPatchScanModSkip() { yield break; }
        private IEnumerator InitialPrefabScan() { yield break; }
    }
}
