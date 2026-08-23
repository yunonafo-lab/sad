using System;
using System.Collections;
using System.Reflection;
using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Utility methods, helpers, and configuration loading
    /// </summary>
    public partial class Plugin
    {
        // Utility stubs - implement in derived class or keep as virtual
        protected virtual void LoadModPresetsFromConfig() { }
        protected virtual void LoadBannedUserIds() { }
        protected virtual void LoadFavoritePrefabNames() { }
        protected virtual void LoadFavoriteRoomNames() { }
        protected virtual void LoadGenePresetsFromConfig() { }
        protected virtual void TryAutoImportCharConConfig() { }
        protected virtual void DelayedPatchScanModSkip() { }

        // Harmony patches - implement in derived class
        protected virtual bool OrbitCameraLateUpdatePrefix() { return true; }
        protected virtual bool PhotonCreateRoomPrefix() { return true; }
        protected virtual bool PhotonJoinOrCreateRoomPrefix() { return true; }
        protected virtual bool PhotonRoomOptionsPrefixGeneric() { return true; }
    }
}
