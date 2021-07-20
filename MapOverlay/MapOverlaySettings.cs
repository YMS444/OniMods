using Newtonsoft.Json;
using PeterHan;
using PeterHan.PLib.Core;
using PeterHan.PLib.Options;

namespace MapOverlay
{
    [ModInfo(url: "https://steamcommunity.com/sharedfiles/filedetails/?id=2271373459", image: "thumbnail.png")]
    [JsonObject(MemberSerialization.OptIn)]
    public class MapOverlaySettings
    {
        [Option("Reveal burieds geysers", "Whether completely buried geysers should be shown and identified in the map overlay")]
        [JsonProperty]
        public bool ShowBuriedGeysers { get; set; }

        [Option("Reveal burieds critters", "Whether buried critters (e.g. trapped ones, or shove voles) should be shown and identified in the map overlay")]
        [JsonProperty]
        public bool ShowBuriedCritters { get; set; }

        [Option("Count objects in legend", "Whether to display e.g. \"Cool Steam Vent (2)\"")]
        [JsonProperty]
        public bool CountObjects { get; set; }

        //[Option("Hotkey", "Hotkey to open the overlay in the game (see control settings); might not work if already used by the game or another mod")]
        //[JsonProperty]
        //public SelectedActions Hotkey { get; set; }

        [Option("\n\nNo restart required after\nchanging these options.", "Settings for this mod become effective immediately, no need to restart the game.")]
        public LocText NoRestartRequired { get; }

        public MapOverlaySettings()
        {
            // Default values
            ShowBuriedGeysers = true;
            ShowBuriedCritters = true;
            CountObjects = false;
            //Hotkey = SelectedActions.None;
        }

        //// Provide only a selection of the actions, for better overview, and because the dropdown gets to wide otherwise
        //public enum SelectedActions
        //{
        //    None = Action.NumActions,
        //    Overlay1 = Action.Overlay1,
        //    Overlay2 = Action.Overlay2,
        //    Overlay3 = Action.Overlay3,
        //    Overlay4 = Action.Overlay4,
        //    Overlay5 = Action.Overlay5,
        //    Overlay6 = Action.Overlay6,
        //    Overlay7 = Action.Overlay7,
        //    Overlay8 = Action.Overlay8,
        //    Overlay9 = Action.Overlay9,
        //    Overlay10 = Action.Overlay10,
        //    Overlay11 = Action.Overlay11,
        //    Overlay12 = Action.Overlay12,
        //    Overlay13 = Action.Overlay13,
        //    Overlay14 = Action.Overlay14,
        //    Overlay15 = Action.Overlay15
        //    // TODO: More - find ones that are not used currently
        //}
    }
}