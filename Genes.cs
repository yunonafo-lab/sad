using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace ZexQoLMenu
{
    /// <summary>
    /// Character customization, gene editing, and character management
    /// </summary>
    public partial class Plugin
    {
        // ============================================================
        // GENES (local kobold character stats — reflection-based)
        // ============================================================
        private class GeneFieldDef
        {
            public string Label;
            public string[] FieldNames; // tried in order
            public float DefaultValue;
            public float Min;
            public float Max;

            public GeneFieldDef(string label, string[] names, float def, float min, float max)
            {
                Label = label;
                FieldNames = names;
                DefaultValue = def;
                Min = min;
                Max = max;
            }
        }

        protected readonly GeneFieldDef[] geneFieldDefs =
        {
            // Official KoboldGenes field names (github.com/naelstrof/KoboldKare)
            new GeneFieldDef("Energy",    new[] { "maxEnergy" }, 5f, 0.1f, 100f),
            new GeneFieldDef("Belly",    new[] { "bellySize" }, 20f, 0f, 200f),
            new GeneFieldDef("Meta",     new[] { "metabolizeCapacitySize" }, 20f, 0f, 200f),
            new GeneFieldDef("Grab",     new[] { "grabCount" }, 1f, 0f, 20f),
            new GeneFieldDef("Size",     new[] { "baseSize" }, 20f, 0.1f, 200f),
            new GeneFieldDef("Tits",     new[] { "breastSize" }, 0f, 0f, 200f),
            new GeneFieldDef("Fat",      new[] { "fatSize" }, 0f, 0f, 200f),
            new GeneFieldDef("Psize",    new[] { "dickSize" }, 10f, 0f, 200f),
            new GeneFieldDef("Balls",    new[] { "ballSize" }, 10f, 0f, 200f),
            new GeneFieldDef("Hue",      new[] { "hue" }, 0f, 0f, 255f),
            new GeneFieldDef("Bright",   new[] { "brightness" }, 128f, 0f, 255f),
            new GeneFieldDef("Satur",    new[] { "saturation" }, 128f, 0f, 255f),
            new GeneFieldDef("Cloth Hue", new[] { "clothingHue" }, 0f, 0f, 255f),
            new GeneFieldDef("Thick",    new[] { "dickThickness" }, 0.5f, 0f, 3f),
        };

        protected readonly float[] geneCurrent = new float[14];
        protected readonly float[] geneToSet = new float[14];
        protected readonly string[] geneToSetText = new string[14];
        protected int geneEditIndex = -1;
        protected string geneStatus = "Not loaded";
        protected float geneStatusUntil;
        protected Type koboldType;
        protected Type koboldGenesType;
        protected MethodInfo getGenesMethod;
        protected MethodInfo setGenesMethod;
        protected bool geneTypesResolved;
        protected Vector2 genesScroll = Vector2.zero;

        // Dick / species / thickness
        protected readonly List<string> dickOptions = new List<string>();
        protected int selectedDickIndex;
        protected Vector2 dickScroll = Vector2.zero;
        protected float cockThickness = 0.7f;
        protected int speciesId;
        protected string speciesName = "";
        protected string speciesEditText = "0";
        protected bool speciesEditing;

        // Modded character / avatar list (PlayerDatabase / PrefabDatabase)
        protected readonly List<string> characterOptions = new List<string>();
        protected int selectedCharacterIndex;
        protected Vector2 characterScroll = Vector2.zero;
        protected string characterFilter = "";
        protected bool characterFilterEditing;

        // Equipment / clothing (EquipmentDatabase + KoboldInventory)
        protected readonly List<string> equipNames = new List<string>(); // currently worn
        protected readonly List<string> equipCatalog = new List<string>(); // all from EquipmentDatabase
        protected int selectedCatalogEquip = -1;
        protected int selectedWornEquip = -1;
        protected Vector2 equipScroll = Vector2.zero;
        protected Vector2 equipCatalogScroll = Vector2.zero;
        protected string equipStatus = "";
        protected string equipFilter = "";
        protected bool equipFilterEditing;

        // Presets — full (char+genes+clothes) is primary; old stats/equip kept for compatibility
        protected readonly List<string> statsPresetNames = new List<string>();
        protected readonly Dictionary<string, string> statsPresetData = new Dictionary<string, string>();
        protected readonly List<string> equipPresetNames = new List<string>();
        protected readonly Dictionary<string, string> equipPresetData = new Dictionary<string, string>();
        protected readonly List<string> fullPresetNames = new List<string>();
        protected readonly Dictionary<string, string> fullPresetData = new Dictionary<string, string>();
        protected string newPresetName = "MyPreset";
        protected bool presetNameEditing;
        protected Vector2 presetScroll = Vector2.zero;
        protected int selectedStatsPreset = -1;
        protected int selectedEquipPreset = -1;
        protected int selectedFullPreset = -1;
        protected Coroutine applyFullPresetCoroutine;

        // Presets pop-out window (avoids crushing the genes panel layout)
        protected bool presetsPopupVisible;
        protected Rect presetsPopupRect = new Rect(420f, 120f, 360f, 480f);
        protected float equipStatusUntil;
    }
}
