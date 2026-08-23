using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Prefab spawning and object management
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // SPAWNER
        // ============================================================
        private class PrefabEntry
        {
            public string Name;
            public GameObject Prefab;
            public PrefabEntry(string name, GameObject prefab) { Name = name; Prefab = prefab; }
        }

        protected readonly List<PrefabEntry> prefabList = new List<PrefabEntry>();
        protected readonly List<PrefabEntry> filteredPrefabList = new List<PrefabEntry>();
        protected int selectedPrefabIndex = -1;
        protected int prefabListOffset;
        protected string prefabStatus = "NOT SCANNED";
        protected string searchText = "";
        protected bool searchFocused;
        protected int amount = 1;
        protected float spawnDistance = 2f;
        protected string spawnStatus = "WAITING";
        protected readonly List<GameObject> spawnedObjects = new List<GameObject>();
        protected Type preparePoolType;
        protected FieldInfo preparePoolInstanceField;
        protected FieldInfo dynamicPrefabsField;
        protected readonly HashSet<string> favoritePrefabNames = new HashSet<string>();

        // Stubs for prefab operations
        protected virtual void FindPreparePool() { }
        protected virtual void InitialPrefabScan() { }
    }
}
