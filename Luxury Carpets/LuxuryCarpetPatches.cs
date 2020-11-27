using Harmony;
using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Luxury_Carpets
{
    public class LuxuryCarpetPatches
    {
        // Luxury Carepts mod for Oxygen not included
        // By Yannick M. Schmitt

        // Add assets needed for the mod to the game
        [HarmonyPatch(typeof(Db), "Initialize")]
        public static class Db_Initialize_Patch
        {
            public static void Postfix()
            {
                // Add sprite for overlay icon
                AddSpriteFromFile("tiles_luxury_carpet");
                AddSpriteFromFile("tiles_luxury_carpet_place");
            }

            private static void AddSpriteFromFile(string name)
            {
                string filename = Path.Combine(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "assets"), $"{name}.png");
                Texture2D texture = null;

                try
                {
                    byte[] data = File.ReadAllBytes(filename);
                    texture = new Texture2D(1, 1);
                    texture.LoadImage(data);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Could not load texture at {filename}");
                    Debug.LogException(e);
                }

                Assets.Sprites.Add(name, Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(texture.width / 2f, texture.height / 2f)));
            }
        }

        [HarmonyPatch(typeof(CarpetTileConfig))]  [HarmonyPatch("CreateBuildingDef")]
        public class CarpetTileConfig_CreateBuildingDef_Patch
        {
            public static void Postfix(BuildingDef __result)
            {
                __result.BlockTileAtlas = Assets.GetTextureAtlas("tiles_luxury_carpet");
                __result.BlockTilePlaceAtlas = Assets.GetTextureAtlas("tiles_luxury_carpet_place");
                //__result.BlockTileMaterial = Assets.GetMaterial("tiles_solid");
                //__result.DecorBlockTileInfo = Assets.GetBlockTileDecorInfo("tiles_carpet_tops_decor_info");
                //__result.DecorPlaceBlockTileInfo = Assets.GetBlockTileDecorInfo("tiles_carpet_tops_decor_place_info");
            }
        }
    }
}
