using System.Collections.Generic;
using UnityEngine;

namespace MapOverlay
{
    // An entry for a color map
    public class MapOverlayEntry
    {
        public LocString Name { get; set; }
        public Color Color { get; set; }
        public Dictionary<int, GameObject> GameObjects = new Dictionary<int, GameObject>();
    }
}