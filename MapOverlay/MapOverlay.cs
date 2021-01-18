using Harmony;
using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static GeyserConfigurator;
using static ProcGen.SubWorld;

namespace MapOverlay
{
    // Map Overlay mod for Oxygen Not Included
    // By Yannick M. Schmitt
    public class MapOverlay : OverlayModes.Mode
    {
        // Mod information
        public static readonly HashedString ID = nameof(MapOverlay);
        public const string Icon = "overlay_map";
        public const string Name = "Map Overlay";
        public const string Desc = "Displays a map view indicating positions of various POIs {Hotkey}";
        public const string Sound = "Temperature";
        public const string LocName = "MAPOVERLAY.TITLE";
        public const Action Hotkey = Action.Overlay15; // TODO: Check this with final DLC, which probably will use Overlay15 for the Radiation Overlay

        // Mode filters
        private const string ModeBiomes = "MapOverlayBiomes";
        private const string ModeBuildings = "MapOverlayBuildings";
        private const string ModeCritters = "MapOverlayCritters";
        private const string ModeGeysers = "MapOverlayGeysers";
        private const string ModeGeysersInclBuried = "MapOverlayGeysersInclBuried";
        private const string ModePlants = "MapOverlayPlants";
        private static string CurrentMode = ModeGeysers;
        public static readonly Dictionary<string, string> Modes = new Dictionary<string, string>() { { ModeBiomes, "Biomes" }, { ModeBuildings, "Buildings" }, { ModeCritters, "Critters" }, { ModeGeysers, "Geysers" }, { ModeGeysersInclBuried, "Geysers (incl. buried)" }, { ModePlants, "Plants" } };

        // Maps of things to map on the map
        private static readonly Dictionary<string, MapOverlayEntry> ColorMap = new Dictionary<string, MapOverlayEntry>();
        private static readonly List<string> BuildingList = new List<string>() { "CryoTank", "ExobaseHeadquartersComplete", "GeneShuffler", "HeadquartersComplete", "MassiveHeatSinkComplete", "WarpConduitReceiverComplete", "WarpConduitSenderComplete", "WarpPortal", "WarpReceiver" };
        // TODO: possibly several other POIs (lockers, vending machines, satellites, Gravitas stuff, ...), all with tag "RocketOnGround", Beetafinery, ...

        // Tech stuff
        private static int WorldIdForLegend = -1;
        private static readonly SHA256 HashGenerator = SHA256.Create();
        private static readonly int TargetLayer = LayerMask.NameToLayer("MaskedOverlay");
        private static readonly int CameraLayerMask = LayerMask.GetMask("MaskedOverlay", "MaskedOverlayBG");
        private readonly List<KMonoBehaviour> LayerTargets = new List<KMonoBehaviour>();
        private readonly OverlayModes.ColorHighlightCondition[] HighlightConditions = new OverlayModes.ColorHighlightCondition[] { new OverlayModes.ColorHighlightCondition(new Func<KMonoBehaviour, Color>(GetHighlightColor), new Func<KMonoBehaviour, bool>(DoHighlight)) };


        // Constructor
        public MapOverlay()
        {
            this.legendFilters = CreateDefaultFilters();
        }

        // Detect the relevant MapEntry on the cell
        // Information gathered here is used both for building the overlay legend and actually highlighting the objects
        public static void ProcessCell(int cell)
        {
            GameObject building = Grid.Objects[cell, (int) ObjectLayer.Building];
            GameObject pickupable = Grid.Objects[cell, (int) ObjectLayer.Pickupables];

            if (IsCurrentMode(ModeGeysers) && building != null && IsGeyserRevealed(cell) && building.GetComponent<Geyser>() != null)
            {
                UpdateMapEntry(building, building.GetComponent<Geyser>().configuration.GetElement());
            }
            else if (IsCurrentMode(ModeGeysers) && building != null && IsGeyserRevealed(cell) && building.HasTag(GameTags.OilWell))
            {
                UpdateMapEntry(building, SimHashes.CrudeOil);
            }
            else if (IsCurrentMode(ModeBuildings) && building != null && BuildingList.Contains(building.name))
            {
                UpdateMapEntry(building);
            }
            else if (IsCurrentMode(ModeCritters) && pickupable != null && pickupable.HasTag(GameTags.Creature))
            {
                UpdateMapEntry(pickupable);
            }
            else if (IsCurrentMode(ModePlants) && building != null && building.HasTag(GameTags.Plant))
            {
                UpdateMapEntry(building);
            }
            else if (IsCurrentMode(ModeBiomes))
            {
                ZoneType biome = World.Instance.zoneRenderData.worldZoneTypes[cell];
                UpdateMapEntry(biome.ToString(), Enum.GetName(typeof(ZoneType), biome), biome);
                // TODO: Now those are the code names. Would be good to get the real names. From the DLC on, the game knows them in STRINGS.SUBWORLDS, but how to map?
            }
        }

        // Do not display highlight buried geysers unless explicitly requested (for all others, e.g. buried critters, don't apply this method)
        private static bool IsGeyserRevealed(int cell)
        {
            return (IsCurrentMode(ModeGeysersInclBuried) || !Grid.Element[cell].IsSolid);
        }

        // Creates or extends a ColorMap entry, if necessary
        private static void UpdateMapEntry(GameObject go, System.Object colorReference = null)
        {
            UpdateMapEntry(go.name, go.GetProperName(), colorReference ?? go.name, go);
        }

        // Creates or extends a ColorMap entry, if necessary
        private static void UpdateMapEntry(string key, string legend, System.Object colorReference, GameObject go = null)
        {
            if (!ColorMap.TryGetValue(key, out MapOverlayEntry entry))
            {
                entry = new MapOverlayEntry() { Name = legend, Color = GetColor(colorReference) };
                ColorMap.Add(key, entry);
            }
            
            if (go != null && !entry.GameObjects.ContainsKey(go.GetInstanceID()))
            {
                entry.GameObjects.Add(go.GetInstanceID(), go);
            }
        }

        // Get the color for an element, biome or object
        private static Color GetColor(System.Object obj)
        {
            Color color;

            if (obj is SimHashes elementHash)
            {
                // Elements (e.g. neutronium or geyser outputs): Get the original color of the element
                color = ElementLoader.FindElementByHash(elementHash)?.substance?.uiColour ?? Color.clear;
                // Note: Alternatively, use element.substance.colour. But uiColor seems to be more distinctive, and neutronium is a bright pink instead of an easy-to-miss white.
            }
            else if (obj is ZoneType)
            {
                // Biomes: Get the original color for the biome
                color = World.Instance.GetComponent<SubworldZoneRenderData>().zoneColours[(int) obj];
            }
            else
            {
                // Name strings: Get a random but stable color for a specific text
                // Using SHA256 is basically a ransom choice, but giving nicer colors for base game critters/plants than MD5 and SHA1
                var hash = HashGenerator.ComputeHash(Encoding.UTF8.GetBytes((string) obj));
                color = new Color32(hash[0], hash[1], hash[2], 255);
            }

            // Reset the alpha value so it can be shown nicely
            color.a = 1f;

            // Reduce palette (by rounding to .25) to make two very similar colors either reasonably different or completely the same so the legend entries will be merged
            color.r = (float) Math.Round(color.r * 4) / 4;
            color.g = (float) Math.Round(color.g * 4) / 4;
            color.b = (float) Math.Round(color.b * 4) / 4;

            // Brigthen up very dark colors that wouldn't be recognizable in the dark background
            // Tint them blue so black doesn't simply become dark grey
            if (color.maxColorComponent < 0.3f)
            {
                color.b += 0.25f;
                color.g += 0.1f;
                color.b += 0.1f;
            }

            return color;
        }

        // Build legend (this is done every time the overlay is opened or the mode is changed)
        public override List<LegendEntry> GetCustomLegendData()
        {
            // Re-build ColorMap, so that only the relevant items are displayed (present and already discovered on this planetoid)
            WorldIdForLegend = ClusterManager.Instance.activeWorldId;
            ColorMap.Clear();

            for (int cell = 0; cell < Grid.CellCount; cell++)
            {
                if (Grid.IsActiveWorld(cell))
                {
                    ProcessCell(cell);
                }
            }

            var colorSortedList = ColorMap.OrderBy(e => UI.StripLinkFormatting(e.Value.Name.text)).ToList();

            // Collect the legend entries
            var entries = new List<LegendEntry>();

            foreach (KeyValuePair<string, MapOverlayEntry> entry in colorSortedList)
            {
                // If multiple entries with the same color exist, merge them in one legend entry
                var existingLegendEntry = entries.Find(legend => legend.colour == entry.Value.Color);

                if (existingLegendEntry == null)
                {
                    entries.Add(new LegendEntry(entry.Value.Name, "", entry.Value.Color));
                }
                else
                {
                    existingLegendEntry.name = ((LocString) existingLegendEntry.name) + "\n" + entry.Value.Name;
                }
            }

            return entries;
        }

        // Which color to use to highlight the object
        private static Color GetHighlightColor(KMonoBehaviour obj)
        {
            ColorMap.TryGetValue(obj.name, out MapOverlayEntry entry);
            return entry?.Color ?? Color.black;
        }

        // Whether to highlight this object
        private static bool DoHighlight(KMonoBehaviour obj)
        {
            return (obj != null && obj.gameObject != null && obj.gameObject.name != null && ColorMap.ContainsKey(obj.gameObject.name));
        }

        // Enable overlay - init mask
        public override void Enable()
        {
            base.Enable();

            Camera.main.cullingMask |= CameraLayerMask;
            SelectTool.Instance.SetLayerMask(SelectTool.Instance.GetDefaultLayerMask() | TargetLayer);
        }

        // Disable overlay - reset mask, reset LayerTargets and highlighting
        public override void Disable()
        {
            base.Disable();

            this.DisableHighlightTypeOverlay<KMonoBehaviour>((ICollection<KMonoBehaviour>) LayerTargets);
            LayerTargets.Clear();

            Camera.main.cullingMask &= ~CameraLayerMask;
            SelectTool.Instance.ClearLayerMask();
        }

        // Update every frame - apply highlighting
        public override void Update()
        {
            if (WorldIdForLegend != ClusterManager.Instance.activeWorldId)
            {
                // New world showing - refresh legend (and by this, the ColorMap)
                OverlayLegend.Instance.SetLegend(this, true);
            }

            Vector2I origin = new Vector2I(0, 0);
            OverlayModes.Mode.RemoveOffscreenTargets<KMonoBehaviour>((ICollection<KMonoBehaviour>) LayerTargets, origin, origin);

            foreach (KMonoBehaviour obj in ColorMap.Values.SelectMany(entry => entry.GameObjects.Values).Select(go => go.GetComponent<KMonoBehaviour>()).OfType<KMonoBehaviour>())
            {
                LayerTargets.Add(obj);
            }

            Grid.GetVisibleExtents(out Vector2I min, out Vector2I max);
            this.UpdateHighlightTypeOverlay<KMonoBehaviour>(min, max, (ICollection<KMonoBehaviour>) LayerTargets, null, HighlightConditions, OverlayModes.BringToFrontLayerSetting.Conditional, TargetLayer);
        }

        // Apply background coloring for biomes and neutronium
        public static Color GetBackgroundColor(int cell)
        {
            if (IsCurrentMode(ModeGeysers))
            {
                Element element = Grid.Element[cell];

                if (element != null && element.id.ToString().Equals("Unobtanium"))
                {
                    return GetColor(SimHashes.Unobtanium);
                }
            }
            else if (IsCurrentMode(ModeBiomes))
            {
                ZoneType biome = World.Instance.zoneRenderData.worldZoneTypes[cell];
                ColorMap.TryGetValue(biome.ToString(), out MapOverlayEntry entry);

                if (entry != null)
                {
                    return entry.Color;
                }
            }

            return Color.black;
        }

        // Build legend filter sections
        public override Dictionary<string, ToolParameterMenu.ToggleState> CreateDefaultFilters()
        {
            var filters = new Dictionary<string, ToolParameterMenu.ToggleState>();

            foreach (string mode in Modes.Keys)
            {
                filters.Add(mode, ToolParameterMenu.ToggleState.Off);
            }

            filters[CurrentMode] = ToolParameterMenu.ToggleState.On;

            return filters;
        }

        // Legend filter behaviour
        public override void OnFiltersChanged()
        {
            foreach (KeyValuePair<string, ToolParameterMenu.ToggleState> entry in legendFilters)
            {
                if (entry.Value == ToolParameterMenu.ToggleState.On)
                {
                    CurrentMode = entry.Key;
                }
            }
        }

        // Check if we currently are in that filter mode
        // Accepts "Geysers (incl. buried)" as "Geysers", but not vice versa
        private static bool IsCurrentMode(string modeToCheck)
        {
            return CurrentMode.Equals(modeToCheck) || (ModeGeysersInclBuried.Equals(CurrentMode) && ModeGeysers.Equals(modeToCheck));
        }

        // ID, as used internally by ONI to distinguish the overlays
        public override HashedString ViewMode()
        {
            return ID;
        }

        // Sound to play when opening the overlay
        public override string GetSoundName()
        {
            return Sound;
        }
    }
}

// TODO: Future improvement ideas
// - Print object name in the map
// - Make colors configurable, possibly also which objects to map and if buried option is available
// - Optionally reveal even geysers hidden behind POW?
// - Minimap version of the mod, so the map can be shown while playing