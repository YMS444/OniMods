using STRINGS;
using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using static GeyserConfigurator;
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
        public const string ModeGeysers = "MapOverlayGeysers";
        public const string ModeGeysersInclBuried = "MapOverlayGeysersInclBuried";
        public const string ModeBiomes = "MapOverlayBiomes";
        public const string ModeCritters = "MapOverlayCritters";
        public const string ModePlants = "MapOverlayPlants";
        public const string ModeDuplicants = "MapOverlayDuplicants";
        public const string ModeBuildings = "MapOverlayBuildings";
        // TODO: Buried mode would make sense for buildings (AETNs) and Critter (Hatches), too; it would be great to have one checkbox for all, rather than a separate radiobutton for each
        // Alternatively, it could be one mode "Show buried things" that shows (only) all buried geysers, critters, buildings

        // Filter options
        public static bool ShowGeysers = true;
        public static bool ShowBuriedGeysers = false;
        public static bool ShowBiomes = false;
        public static bool ShowCritters = false;
        public static bool ShowBuildings = false;
        public static bool ShowPlants = false;
        public static bool ShowDuplicants = false;

        // Color maps
        public static Dictionary<string, MapOverlayEntry> ColorMap = new Dictionary<string, MapOverlayEntry>();
        public static Dictionary<int, MapOverlayEntry> MapEntryMap = new Dictionary<int, MapOverlayEntry>();

        // Note: Geysers (= Geysers, Vents, Volcanos, Fissures), Critters and Plants will be detected automatically
        private static readonly Dictionary<string, SimHashes> ExtraGeyserMap = new Dictionary<string, SimHashes>() { { "OilWell", SimHashes.CrudeOil }, { "Unobtanium", SimHashes.Unobtanium } };
        private static readonly List<string> BuildingList = new List<string>() { "GeneShuffler", "HeadquartersComplete", "MassiveHeatSinkComplete" }; // TODO: Porta-Pod, Teleporters, Warp Outputs/Inputs, possibly several POIs (lockers, vending machines, Gravitas stuff, ...)

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

        private static Color GetRandomColor(string input)
        {
            // TODO: It would probably be nicer if we don't have every possible color, but just a few steps (i.e. dont't use r=0,1,2,3, but only r=0,10,20,...)
            var hash = MD5.Create().ComputeHash(Encoding.UTF8.GetBytes(input));
            return new Color32(hash[0], hash[1], hash[2], 255);
        }

        // Adjust a color: Set alpha to fixed value, brigthen up dark colors
        private static Color GetAdjustedColor(Color color)
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
            bool isRevealed = (!element.IsSolid || MapOverlay.ShowBuriedGeysers); // TODO: buried mode only works for geysers this way
            GameObject building = isRevealed ? Grid.Objects[cell, (int) ObjectLayer.Building] : null;
            GameObject pickupable = isRevealed ? Grid.Objects[cell, (int) ObjectLayer.Pickupables] : null;
            ZoneType biome = World.Instance.zoneRenderData.worldZoneTypes[cell];

            if (MapOverlay.ShowGeysers)
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

                    // Set Carbon Dioxide Geyser color to Carbon Dioxide Vent color to merge the legend entries, as nobody cares about CO2 anyway, but liquid and gaseous CO2 have slightly different colors originally
                    //ObjectColorMap["GeyserGeneric_liquid_co2"].Color = ObjectColorMap["GeyserGeneric_hot_co2"].Color;

                    if (entry != null)
                    {
                        ColorMap.Add(building.name, entry);
                    }
                }
            }
            else if (MapOverlay.ShowBuildings && building != null && BuildingList.Contains(building.name) && !ColorMap.TryGetValue(building.name, out entry))
            {
                entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetRandomColor(building.name) };
                ColorMap.Add(building.name, entry);
            }
            else if (MapOverlay.ShowCritters && pickupable != null && pickupable.HasTag(GameTags.Creature) && !ColorMap.TryGetValue(pickupable.name, out entry))
            {
                entry = new MapOverlayEntry { Name = pickupable.GetProperName(), Color = GetRandomColor(pickupable.name) };
                ColorMap.Add(pickupable.name, entry);
            }
            else if (MapOverlay.ShowPlants && building != null && building.HasTag(GameTags.Plant) && !ColorMap.TryGetValue(building.name, out entry))
            {
                // TODO: For some reason, waterweed shows in a 1x2, 2x2 or even 3x2 field
                entry = new MapOverlayEntry { Name = building.GetProperName(), Color = GetRandomColor(building.name) };
                ColorMap.Add(building.name, entry);
            }
            else if (MapOverlay.ShowDuplicants && pickupable != null && pickupable.HasTag(GameTags.Minion) && !ColorMap.TryGetValue(pickupable.name, out entry))
            {
                // TODO: Incorrectly shows a Stinky, nothing else <- Not true, I have a dead Stinky on my test map. The others now(?) show up as well.
                entry = new MapOverlayEntry { Name = pickupable.GetProperName(), Color = GetRandomColor(pickupable.name) };
                ColorMap.Add(pickupable.name, entry);
            }
            else if (MapOverlay.ShowBiomes && !ColorMap.TryGetValue(biome.ToString(), out entry))
            {
                entry = new MapOverlayEntry { Name = Enum.GetName(typeof(ZoneType), biome), Color = GetBiomeColor(biome) };
                ColorMap.Add(biome.ToString(), entry);
                // TODO: Now those are the code names. Would be good to get the real names. The game knows them at least in the DLC.
                // Use name strings from STRINGS.SUBWORLDS
            }

            return entry;
        }

        // Build legend (this is done every time the overlay is opened or the mode is changed)
        public override List<LegendEntry> GetCustomLegendData()
        {
            MapEntryMap.Clear();

            // Check for discovery state before building the map, so it's possible to only display discovered/present objects/biomes
            var discoverySet = new HashSet<string>(); // TODO: Now we don't really need the discoverySet any more, as the ColorMap will only contain discovered values anyway
            ColorMap.Clear();

            for (int cell = 0; cell < Grid.CellCount; cell++)
            {
                var entry = GetMapEntryAt(cell);

                if (entry != null)
                {
                    MapEntryMap.Add(cell, entry);
                    discoverySet.Add(entry.Name);
                }
            }

            // Collect the legend entries
            var entries = new List<LegendEntry>();

            // TODO: Would be nice to sort the entries before adding them to the legend
            entries.AddRange(GetLegendEntries(ColorMap, discoverySet));

            return entries;
        }

        // Build a legend entry list
        private List<LegendEntry> GetLegendEntries(Dictionary<string, MapOverlayEntry> map, HashSet<string> discoverySet)
        {
            var entries = new List<LegendEntry>();

            foreach (KeyValuePair<string, MapOverlayEntry> entry in map)
            {
                if (!discoverySet.Contains(entry.Value.Name))
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
                // TODO: Alternatively, take care that all entries shown have sufficiently distinct colors

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

            filters.Add(ModeGeysers, ToolParameterMenu.ToggleState.On);
            filters.Add(ModeGeysersInclBuried, ToolParameterMenu.ToggleState.Off);
            filters.Add(ModeBuildings, ToolParameterMenu.ToggleState.Off);
            filters.Add(ModeCritters, ToolParameterMenu.ToggleState.Off); // TODO: Critters don't work well with the cached version of the mod, as they now move out of the colored spot
            filters.Add(ModePlants, ToolParameterMenu.ToggleState.Off);
            filters.Add(ModeDuplicants, ToolParameterMenu.ToggleState.Off);
            filters.Add(ModeBiomes, ToolParameterMenu.ToggleState.Off);
            // TODO: buried check for critters as well?
            // TODO: Also crop mode? (note, crops are in buildings layer) -> there's the crop overlay, but it does not differentiate between different plants

            return filters;
        }

        // Add the buried objects checkbox to the UI
        public override void Update()
        {
            //GameObject freeDiseaseUi = this.GetFreeDiseaseUI();
            //DiseaseOverlayWidget component1 = freeDiseaseUi.GetComponent<DiseaseOverlayWidget>();
            //OverlayModes.Disease.UpdateDiseaseInfo updateDiseaseInfo = new OverlayModes.Disease.UpdateDiseaseInfo(target.GetComponent<Modifiers>().amounts.Get(Db.Get().Amounts.ImmuneLevel), component1);
            //KAnimControllerBase component2 = target.GetComponent<KAnimControllerBase>();
            //Vector3 position = (UnityEngine.Object) component2 != (UnityEngine.Object) null ? component2.GetWorldPivot() : target.transform.GetPosition() + Vector3.down;
            //freeDiseaseUi.GetComponent<RectTransform>().SetPosition(position);
            //this.updateDiseaseInfo.Add(updateDiseaseInfo);

            //GameObject freeCropUi = this.GetFreeCropUI();
            //OverlayModes.Crop.UpdateCropInfo updateCropInfo = new OverlayModes.Crop.UpdateCropInfo(harvestable, freeCropUi);
            //Vector3 pos = Grid.CellToPos(Grid.PosToCell((KMonoBehaviour) harvestable), 0.5f, -1.25f, 0.0f);
            //freeCropUi.GetComponent<RectTransform>().SetPosition(Vector3.up + pos);
            //Add(updateCropInfo);

            //KToggle test = new KToggle();
            //Vector3 pos = Vector3.down;
            //test.GetComponent<RectTransform>().SetPosition(Vector3.up + pos);


            // a)
            //DefaultControls.Resources uiResources = new DefaultControls.Resources();
            //GameObject uiToggle = DefaultControls.CreateToggle(uiResources);
            //uiToggle.transform.SetParent(canvas.transform, false);

            // b)
            //GameObject uiToggle = Instantiate(togglePrefab) as GameObject;
            //uiToggle.transform.SetParent(canvas.transform, false);
            //Move to another position?
            //uiToggle.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(..., ..., ...);

            // c)
            //GameObject toggle = new GameObject("Toggle");
            //toggle.transform.SetParent(cnvs.transform);
            //toggle.layer = LayerMask.NameToLayer("UI");

            //Toggle toggleComponent = toggle.AddComponent<Toggle>();
            //toggleComponent.transition = Selectable.Transition.ColorTint;
            //toggleComponent.targetGraphic = bgImage;
            //toggleComponent.isOn = true;
            //toggleComponent.toggleTransition = Toggle.ToggleTransition.Fade;
            //toggleComponent.graphic = chmkImage;
            //toggle.GetComponent<RectTransform>().anchoredPosition3D = new Vector3(0, 0, 0);

        }

        // Legend filter behaviour
        public override void OnFiltersChanged()
        {
            ShowGeysers = (this.InFilter(ModeGeysers, this.legendFilters) || this.InFilter(ModeGeysersInclBuried, this.legendFilters));
            ShowBuriedGeysers = this.InFilter(ModeGeysersInclBuried, this.legendFilters);
            ShowBiomes = this.InFilter(ModeBiomes, this.legendFilters);
            ShowCritters = this.InFilter(ModeCritters, this.legendFilters);
            ShowBuildings = this.InFilter(ModeBuildings, this.legendFilters);
            ShowPlants = this.InFilter(ModePlants, this.legendFilters);
            ShowDuplicants = this.InFilter(ModeDuplicants, this.legendFilters);
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
// - Check if getColourFuncs() really has to use the MapOverlayEntry. Directly calling GetMapEntryAt() would have the benefit of live updates.
// - Print object name in the map
// - Make colors configurable, possibly also which objects to map
// - Optionally reveal even geysers hidden behind POW?
// - Minimap version of the mod, so the map can be shown while playing
// - Make configurable if buried option is available

// Good test seeds:
// OCAN-A-556153622-0 - all geysers except Hydrogen Vent
// VOLCA-520762030-0 - all geysers except Iron Volcano