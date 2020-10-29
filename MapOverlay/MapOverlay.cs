using STRINGS;
using System.Collections.Generic;
using UnityEngine;
using static ProcGen.SubWorld;

namespace MapOverlay
{
    // Map Overlay mod for Oxygen not included
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

        // Filter names
        public const string ModeObjects = "MapOverlayObjects";
        public const string ModeObjectsInclBuried = "MapOverlayObjectsInclBuried";
        public const string ModeBiomes = "MapOverlayBiomes";

        // Filter options
        public static bool ShowObjects = true;
        public static bool ShowBuriedObjects = false;
        public static bool ShowBiomes = false;

        // Color maps
        public static Dictionary<string, MapOverlayEntry> ObjectColorMap = new Dictionary<string, MapOverlayEntry>();
        public static Dictionary<string, MapOverlayEntry> BiomeColorMap = new Dictionary<string, MapOverlayEntry>();


        // Constructor
        public MapOverlay()
        {
            FillColorMaps();
            this.legendFilters = CreateDefaultFilters();
        }

        // Build the color maps used both for the legend and the overlay itself
        private void FillColorMaps()
        {
            // If static dictionaries are already filled (e.g. because now a second savegame is loaded), do nothing here
            if (ObjectColorMap.Count > 0)
            {
                return;
            }

            // POIs(and Neutronium pseudo - object) - use signal colors
            ObjectColorMap.Add("HeadquartersComplete", new MapOverlayEntry { Name = BUILDINGS.PREFABS.HEADQUARTERSCOMPLETE.NAME, Color = new Color(1f, 1f, 1f, 0.8f) }); // Printing Pod
            ObjectColorMap.Add("MassiveHeatSinkComplete", new MapOverlayEntry { Name = BUILDINGS.PREFABS.MASSIVEHEATSINK.NAME, Color = new Color(0f, 0f, 1f, 0.8f) }); // AETN
            ObjectColorMap.Add("GeneShuffler", new MapOverlayEntry { Name = BUILDINGS.PREFABS.GENESHUFFLER.NAME, Color = new Color(0f, 1f, 0f, 0.8f) }); // Neural Vacillator
            ObjectColorMap.Add("_Neutronium", new MapOverlayEntry { Name = ELEMENTS.UNOBTANIUM.NAME, Color = GetElementColor("Unobtanium") }); // Neutronium

            // Geysers/Vents/Volcanoes (and Oil Reservoirs) - use color of the gas or liquid icon
            ObjectColorMap.Add("GeyserGeneric_liquid_co2", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.LIQUID_CO2.NAME, Color = GetElementColor("LiquidCarbonDioxide") }); // Carbon Dioxide Geyser
            ObjectColorMap.Add("GeyserGeneric_hot_co2", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.HOT_CO2.NAME, Color = GetElementColor("CarbonDioxide") }); // Carbon Dioxide Vent
            ObjectColorMap.Add("GeyserGeneric_chlorine_gas", new MapOverlayEntry { Name = CREATURES.SPECIES.CHLORINEGEYSER.NAME, Color = GetElementColor("ChlorineGas") }); // Chlorine Gas Vent
            ObjectColorMap.Add("GeyserGeneric_slush_water", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.SLUSH_WATER.NAME, Color = GetElementColor("DirtyWater") }); // Cool Slush Geyser
            ObjectColorMap.Add("GeyserGeneric_steam", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.STEAM.NAME, Color = GetElementColor("Steam") }); // Cool Steam Vent
            ObjectColorMap.Add("GeyserGeneric_molten_copper", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.MOLTEN_COPPER.NAME, Color = GetElementColor("MoltenCopper") }); // Copper Volcano
            ObjectColorMap.Add("GeyserGeneric_molten_gold", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.MOLTEN_GOLD.NAME, Color = GetElementColor("MoltenGold") }); // Gold Volcano
            ObjectColorMap.Add("GeyserGeneric_hot_po2", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.HOT_PO2.NAME, Color = GetElementColor("ContaminatedOxygen") }); // Hot Polluted Oxygen Vent
            ObjectColorMap.Add("GeyserGeneric_hot_hydrogen", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.HOT_HYDROGEN.NAME, Color = GetElementColor("Hydrogen") }); // Hydrogen Vent
            ObjectColorMap.Add("GeyserGeneric_slimy_po2", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.SLIMY_PO2.NAME, Color = GetElementColor("ContaminatedOxygen") }); // Infectious Polluted Oxygen Vent
            ObjectColorMap.Add("GeyserGeneric_molten_iron", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.MOLTEN_IRON.NAME, Color = GetElementColor("MoltenIron") }); // Iron Volcano
            ObjectColorMap.Add("GeyserGeneric_oil_drip", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.OIL_DRIP.NAME, Color = GetElementColor("CrudeOil") }); // Leaky Oil Fissure
            ObjectColorMap.Add("GeyserGeneric_small_volcano", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.SMALL_VOLCANO.NAME, Color = GetElementColor("Magma") }); // Minor Volcano
            ObjectColorMap.Add("GeyserGeneric_methane", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.METHANE.NAME, Color = GetElementColor("Methane") }); // Natural Gas Geyser
            ObjectColorMap.Add("OilWell", new MapOverlayEntry { Name = CREATURES.SPECIES.OIL_WELL.NAME, Color = GetElementColor("CrudeOil") }); // Oil Reservoir
            ObjectColorMap.Add("GeyserGeneric_filthy_water", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.FILTHY_WATER.NAME, Color = GetElementColor("DirtyWater") }); // Polluted Water Vent
            ObjectColorMap.Add("GeyserGeneric_salt_water", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.SALT_WATER.NAME, Color = GetElementColor("SaltWater") }); // Salt Water Geyser
            ObjectColorMap.Add("GeyserGeneric_hot_steam", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.HOT_STEAM.NAME, Color = GetElementColor("Steam") }); // Steam Vent
            ObjectColorMap.Add("GeyserGeneric_big_volcano", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.BIG_VOLCANO.NAME, Color = GetElementColor("Magma") }); // Volcano
            ObjectColorMap.Add("GeyserGeneric_hot_water", new MapOverlayEntry { Name = CREATURES.SPECIES.GEYSER.HOT_WATER.NAME, Color = GetElementColor("Water") }); // Water Geyser
            ObjectColorMap.Add("_UnknownGeyser", new MapOverlayEntry { Name = "Unknown Geyser", Color = new Color(0.6f, 0.35f, 0.7f, 0.8f) }); // Any Geyser not in this list

            // Set Carbon Dioxide Geyser color to Carbon Dioxide Vent color to merge the legend entries, as nobody cares about CO2 anyway, but liquid and gaseous CO2 have slightly different colors originally
            ObjectColorMap["GeyserGeneric_liquid_co2"].Color = ObjectColorMap["GeyserGeneric_hot_co2"].Color;

            // Biomes - use original colors
            BiomeColorMap.Add(ZoneType.ToxicJungle.ToString(), new MapOverlayEntry { Name = "Caustic", Color = GetBiomeColor(ZoneType.ToxicJungle) });
            BiomeColorMap.Add(ZoneType.Forest.ToString(), new MapOverlayEntry { Name = "Forest", Color = GetBiomeColor(ZoneType.Forest) });
            BiomeColorMap.Add(ZoneType.FrozenWastes.ToString(), new MapOverlayEntry { Name = "Frozen", Color = GetBiomeColor(ZoneType.FrozenWastes) });
            BiomeColorMap.Add(ZoneType.OilField.ToString(), new MapOverlayEntry { Name = "Oil", Color = GetBiomeColor(ZoneType.OilField) });
            BiomeColorMap.Add(ZoneType.Rust.ToString(), new MapOverlayEntry { Name = "Rust", Color = GetBiomeColor(ZoneType.Rust) });
            BiomeColorMap.Add(ZoneType.Space.ToString(), new MapOverlayEntry { Name = "Space", Color = GetBiomeColor(ZoneType.Space) });
            BiomeColorMap.Add(ZoneType.BoggyMarsh.ToString(), new MapOverlayEntry { Name = "Swamp", Color = GetBiomeColor(ZoneType.BoggyMarsh) });
            BiomeColorMap.Add(ZoneType.Sandstone.ToString(), new MapOverlayEntry { Name = "Temperate", Color = GetBiomeColor(ZoneType.Sandstone) }); // Also includes the Barren biome on Badlands
            BiomeColorMap.Add(ZoneType.Ocean.ToString(), new MapOverlayEntry { Name = "Tide Pool", Color = GetBiomeColor(ZoneType.Ocean) });
            BiomeColorMap.Add(ZoneType.MagmaCore.ToString(), new MapOverlayEntry { Name = "Volcanic", Color = GetBiomeColor(ZoneType.MagmaCore) });
            BiomeColorMap.Add(ZoneType.CrystalCaverns.ToString(), new MapOverlayEntry { Name = "Unknown Biome", Color = GetBiomeColor(ZoneType.CrystalCaverns) }); // Not used in game currently, serves as fallback for biomes not in the list
        }

        // Get the adjusted original color of the element
        private Color GetElementColor(string name)
        {
            return GetAdjustedColor(ElementLoader.FindElementByName(name)?.substance?.colour ?? Color.clear);
        }

        // Get the adjusted original color for the biome
        private Color GetBiomeColor(ZoneType biome)
        {
            return GetAdjustedColor(World.Instance.GetComponent<SubworldZoneRenderData>().zoneColours[(int) biome]);
        }

        // Adjust a color: Set alpha to fixed value, brigthen up dark colors
        private Color GetAdjustedColor(Color color)
        {
            // Reset the alpha value so it can be shown nicely
            color.a = 0.8f;

            // Brigthen up too dark colors, as they wouldn't be recognizable in the dark background
            if (color.maxColorComponent < 0.2f)
            {
                color.b += 0.3f;
            }

            return color;
        }

        // MOD'S MAIN MAGIC METHOD
        // Detect relevant MapEntry on cell
        public static MapOverlayEntry GetMapEntryAt(int cell)
        {
            MapOverlayEntry entry = null;
            Element element = Grid.Element[cell];
            GameObject obj = Grid.Objects[cell, (int) ObjectLayer.Building];

            if (MapOverlay.ShowObjects && element.IsSolid && element.id.ToString().Equals("Unobtanium"))
            {
                // Color Neutronium
                entry = MapOverlay.ObjectColorMap["_Neutronium"];
            }
            else if (MapOverlay.ShowObjects && obj != null && (!element.IsSolid || MapOverlay.ShowBuriedObjects))
            {
                // Color objects, but ignore buried ones by default
                if (!MapOverlay.ObjectColorMap.TryGetValue(obj.name, out entry) && obj.GetType() == typeof(Geyser))
                {
                    // Fallback for unknown geysers (e.g. from mods, updates)
                    entry = MapOverlay.ObjectColorMap["_UnknownGeyser"];
                }
                // Note: If ShowObjects and ShowBiomes once are allowed together, this method has to be changed, as it currently would detect an object that is not a geyser and would then not check for a biome
            }
            else if (MapOverlay.ShowBiomes)
            {
                // Color biomes
                if (!MapOverlay.BiomeColorMap.TryGetValue(World.Instance.zoneRenderData.worldZoneTypes[cell].ToString(), out entry))
                {
                    // Fallback for unknown biomes (e.g. from mods, updates), using the unused crystal caverns biome color
                    entry = MapOverlay.BiomeColorMap[ZoneType.CrystalCaverns.ToString()];
                }
            }

            return entry;
        }

        // Build legend (this is done every time the overlay is opened or the mode is changed)
        public override List<LegendEntry> GetCustomLegendData()
        {
            // Check for discovery state before building the map, so it's possible to only display discovered/present objects/biomes
            var discoverySet = new HashSet<string>();

            for (int cell = 0; cell < Grid.CellCount; cell++)
            {
                var entry = GetMapEntryAt(cell);

                if (entry != null)
                {
                    discoverySet.Add(entry.Name);
                }
            }

            // Collect the legend entries
            var entries = new List<LegendEntry>();

            if (ShowObjects)
            {
                entries.AddRange(GetLegendEntries(ObjectColorMap, discoverySet));
            }

            if (ShowBiomes)
            {
                entries.AddRange(GetLegendEntries(BiomeColorMap, discoverySet));
            }

            return entries;
        }

        // Build a legend entries
        private List<LegendEntry> GetLegendEntries(Dictionary<string, MapOverlayEntry> map, HashSet<string> discoveryMap)
        {
            var entries = new List<LegendEntry>();

            foreach (KeyValuePair<string, MapOverlayEntry> entry in map)
            {
                if (!discoveryMap.Contains(entry.Value.Name))
                {
                    // Only show already discovered elements
                    // (Known inconvenience: If something is discovered while the overlay is opened, the according legend entry while only appear after closing and re-opening or switching modes)
                    continue;
                }

                // Use full alpha value in legend
                Color color = entry.Value.Color;
                color.a = 1f;

                // If multiple entries with the same color exist, merge them in one legend entry
                var existingLegendEntry = entries.Find(legend => legend.colour == color);

                if (existingLegendEntry == null)
                {
                    entries.Add(new LegendEntry(entry.Value.Name, "", color));
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

            filters.Add(ModeObjects, ToolParameterMenu.ToggleState.On);
            filters.Add(ModeObjectsInclBuried, ToolParameterMenu.ToggleState.Off);
            filters.Add(ModeBiomes, ToolParameterMenu.ToggleState.Off);

            return filters;
        }

        // Legend filter behaviour
        public override void OnFiltersChanged()
        {
            if (this.InFilter(ModeObjects, this.legendFilters))
            {
                ShowObjects = true;
                ShowBuriedObjects = false;
                ShowBiomes = false;
            }
            else if (this.InFilter(ModeObjectsInclBuried, this.legendFilters))
            {
                ShowObjects = true;
                ShowBuriedObjects = true;
                ShowBiomes = false;
            }
            else if (this.InFilter(ModeBiomes, this.legendFilters))
            {
                ShowObjects = false;
                ShowBuriedObjects = false;
                ShowBiomes = true;
            }
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
// - Still show the objects in biome mode and vice versa, just with lower alpha values?
// - Print object name in the map
// - Make colors configurable, possibly also which objects to map
// - Optionally reveal even geysers hidden behind POW?
// - Separate "Objects" group to buildings and geysers?
// - Optionally also highlight critters? (And plants possibly, though they have an own overlay)
// - Minimap version of the mod, so the map can be shown while playing
// - Tint buildings instead of coloring the whole tile (see  e.g. https://github.com/EtiamNullam/Etiam-ONI-Modpack/blob/master/src/MaterialColor/Painter.cs)

// Good test seeds:
// OCAN-A-556153622-0 - all geysers except Hydrogen Vent
// VOLCA-520762030-0 - all geysers except Iron Volcano