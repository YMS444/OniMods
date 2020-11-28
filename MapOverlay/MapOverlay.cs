using STRINGS;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
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
        public const string Desc = "Displays a map view indicating positions of various POIs";
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
        // TODO: Buried mode would make sense for buildings (AETNs) and Critter (Hatches), too; it would be great to have one checkbox for all, rather than a separate radiobutton for each
        // Alternatively, it could be one mode "Show buried things" that shows (only) all buried geysers, critters, buildings
        // Or have a new reveal approach? No option, but always fully reveal partially revealed objects?

        // Maps of things to map on the map (note that Geysers [= Geysers, Vents, Volcanos, Fissures], Critters and Plants will be detected automatically)
        private static readonly Dictionary<string, MapOverlayEntry> ColorMap = new Dictionary<string, MapOverlayEntry>();
        private static readonly Dictionary<string, SimHashes> ExtraGeyserMap = new Dictionary<string, SimHashes>() { { "OilWell", SimHashes.CrudeOil }, { "Unobtanium", SimHashes.Unobtanium } };
        private static readonly List<string> BuildingList = new List<string>() { "ExobaseHeadquarters", "GeneShuffler", "HeadquartersComplete", "MassiveHeatSinkComplete", "WarpConduitReceiver", "WarpConduitSender", "WarpPortal", "WarpReceiver" }; // TODO: possibly several other POIs (lockers, vending machines, satellites, Gravitas stuff, ...)

        // Tech stuff
        private static readonly SHA256 HashGenerator = SHA256.Create();


        // Constructor
        public MapOverlay()
        {
            this.legendFilters = CreateDefaultFilters();
        }

        // Get the adjusted original color of the element
        private static Color GetElementColor(SimHashes hash)
        {
            return GetAdjustedColor(ElementLoader.FindElementByHash(hash)?.substance?.colour ?? Color.clear);
        }

        // Get the adjusted original color for the biome
        private static Color GetBiomeColor(ZoneType biome)
        {
            return GetAdjustedColor(World.Instance.GetComponent<SubworldZoneRenderData>().zoneColours[(int) biome]);
        }

        // Get a random but stable color for a specific text
        // Using SHA256 is basically a ransom choice, but giving nicer colors for base game critters/plants than MD5 and SHA1
        private static Color GetRandomColor(string input)
        {
            var hash = HashGenerator.ComputeHash(Encoding.UTF8.GetBytes(input));
            return GetAdjustedColor(new Color32(hash[0], hash[1], hash[2], 255));
        }

        // Adjust a color: Set alpha to fixed value, brigthen up dark colors, reduce palette to have reasonably different colors
        private static Color GetAdjustedColor(Color color)
        {
            // Reset the alpha value so it can be shown nicely
            color.a = 1f;

            // Reduce palette by rounding to .25 to make two very similar colors either reasonably different or completely the same so the legend entries will be merged
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

        // MOD'S MAIN MAGIC METHOD
        // Detect relevant MapEntry on cell
        public static MapOverlayEntry GetMapEntryAt(int cell)
        {
            MapOverlayEntry entry = null;
            Element element = Grid.Element[cell];
            bool isRevealed = (!element.IsSolid || IsVisible(ModeGeysersInclBuried));
            // Note: As long as there only is a buried mode for geysers, everything is will also be unrevealed this way

            GameObject building = isRevealed ? Grid.Objects[cell, (int) ObjectLayer.Building] : null;
            GameObject pickupable = isRevealed ? Grid.Objects[cell, (int) ObjectLayer.Pickupables] : null;
            ZoneType biome = World.Instance.zoneRenderData.worldZoneTypes[cell];

            if (IsVisible(ModeGeysers) || IsVisible(ModeGeysersInclBuried))
            {
                if (element.IsSolid && ExtraGeyserMap.ContainsKey(element.id.ToString()) && !ColorMap.TryGetValue(element.id.ToString(), out entry))
                {
                    // Neutronium
                    entry = new MapOverlayEntry { Name = element.name, Color = GetElementColor(ExtraGeyserMap[element.id.ToString()]) };
                    ColorMap.Add(element.id.ToString(), entry);
                }
                else if (building != null && !ColorMap.TryGetValue(building.name, out entry))
                {
                    if (building.GetComponent<Geyser>() != null)
                    {
                        // Generic Geysers
                        entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetElementColor(building.GetComponent<Geyser>().configuration.GetElement()) };
                    }
                    else if (ExtraGeyserMap.ContainsKey(building.name))
                    {
                        // Oil Well
                        entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetElementColor(ExtraGeyserMap[building.name]) };
                    }

                    if (entry != null)
                    {
                        ColorMap.Add(building.name, entry);
                    }
                }
            }
            else if (IsVisible(ModeBuildings) && building != null && BuildingList.Contains(building.name) && !ColorMap.TryGetValue(building.name, out entry))
            {
                entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetRandomColor(building.name) };
                ColorMap.Add(building.name, entry);
            }
            else if (IsVisible(ModeCritters) && pickupable != null && pickupable.HasTag(GameTags.Creature) && !ColorMap.TryGetValue(pickupable.name, out entry))
            {
                entry = new MapOverlayEntry { Name = pickupable.GetProperName(), Color = GetRandomColor(pickupable.name) };
                ColorMap.Add(pickupable.name, entry);
            }
            else if (IsVisible(ModePlants) && building != null && building.HasTag(GameTags.Plant) && !ColorMap.TryGetValue(building.name, out entry))
            {
                entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetRandomColor(building.name) };
                ColorMap.Add(building.name, entry);
            }
            else if (IsVisible(ModeBiomes) && !ColorMap.TryGetValue(biome.ToString(), out entry))
            {
                entry = new MapOverlayEntry { Name = Enum.GetName(typeof(ZoneType), biome), Color = GetBiomeColor(biome) };
                ColorMap.Add(biome.ToString(), entry);
                // TODO: Now those are the code names. Would be good to get the real names. From the DLC on, the game knows them in STRINGS.SUBWORLDS.
            }

            return entry;
        }

        // Build legend (this is done every time the overlay is opened or the mode is changed)
        public override List<LegendEntry> GetCustomLegendData()
        {
            // Re-build ColorMap, so that only the relevant items are displayed (present and allready discovered on this planetoid)
            ColorMap.Clear();

            for (int cell = 0; cell < Grid.CellCount; cell++)
            {
                GetMapEntryAt(cell);
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

        private static bool IsVisible(string mode)
        {
            return CurrentMode.Equals(mode);
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

// Good test seeds:
// OCAN-A-556153622-0 - all geysers except Hydrogen Vent
// VOLCA-520762030-0 - all geysers except Iron Volcano