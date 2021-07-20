using HarmonyLib;
using Klei.AI;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace MidnightSun
{
    // Midnight Sun (Eternal Daylight) mod for Oxygen not included
    // By Yannick M. Schmitt
    public class MidnightSunPatches
    {
        [HarmonyPatch(typeof(TimeOfDay), "UpdateVisuals")]
        public static class TimeOfDay_UpdateVisuals_Patch
        {
            public static void Postfix()
            {
                Shader.SetGlobalVector("_TimeOfDay", new Vector4(0.0f, 1f, 0.0f, 0.0f));
            }
        }
    }
}
