using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using HarmonyLib;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewValley;
using StardewValley.GameData.Objects;
using StardewValley.Objects;

namespace AnelDaGanancia
{
    public class ModEntry : Mod
    {
        internal static IMonitor ModMonitor;
        internal static int Multiplicador = 5;
        internal static bool IsProcessingMultiple = false;

        public override void Entry(IModHelper helper)
        {
            ModMonitor = this.Monitor;

            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(asset =>
                {
                    var data = asset.AsDictionary<string, ObjectData>().Data;
                    data["AnelDaGanancia_Item"] = new ObjectData
                    {
                        Name = "Anel da Ganancia",
                        DisplayName = "Anel da Ganancia",
                        Description = "Multiplica drasticamente os drops de recursos.",
                        Type = "Ring",
                        Category = -96,
                        Price = 1000,
                        Texture = "Maps/springobjects",
                        SpriteIndex = 526,
                        ExcludeFromShippingCollection = true,
                        CanBeGivenAsGift = true,
                        CanBeTrashed = true
                    };
                });
            }
        }

        private void OnSaveLoaded(object sender, SaveLoadedEventArgs e)
        {
            if (!Game1.player.Items.Any(item => item?.ItemId == "AnelDaGanancia_Item"))
            {
                var anel = new Ring("AnelDaGanancia_Item");
                Game1.player.addItemToInventory(anel);
                this.Monitor.Log("Anel da Ganancia adicionado ao inventario.", LogLevel.Info);
            }
        }

        internal static bool TemAnelEquipado()
        {
            if (Game1.player == null) return false;
            return Game1.player.leftRing.Value?.ItemId == "AnelDaGanancia_Item" ||
                   Game1.player.rightRing.Value?.ItemId == "AnelDaGanancia_Item";
        }
    }

    // 1. Patch para itens únicos caindo no chão
    [HarmonyPatch]
    public static class PatchDropsUnicos
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Game1)))
            {
                if (method.Name == "createObjectDebris")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length == 7 &&
                        parameters[1].ParameterType == typeof(int) &&
                        parameters[2].ParameterType == typeof(int))
                    {
                        yield return method;
                    }
                }
            }
        }

        static bool Prefix(object[] __args)
        {
            if (ModEntry.IsProcessingMultiple) return true;

            if (ModEntry.TemAnelEquipado() && __args != null && __args.Length == 7)
            {
                int x = (int)__args[1];
                int y = (int)__args[2];
                int ground = (int)__args[3];
                int quality = (int)__args[4];
                float velocity = (float)__args[5];
                GameLocation loc = __args[6] as GameLocation;

                for (int i = 0; i < ModEntry.Multiplicador; i++)
                {
                    int offsetX = Game1.random.Next(-32, 33);
                    int offsetY = Game1.random.Next(-32, 33);

                    if (__args[0] is string strId)
                        Game1.createObjectDebris(strId, x + offsetX, y + offsetY, ground, quality, velocity, loc);
                    else if (__args[0] is int intId)
                        Game1.createObjectDebris(intId.ToString(), x + offsetX, y + offsetY, ground, quality, velocity, loc);
                }
                return false;
            }
            return true;
        }
    }

    // 2. Patch para múltiplos itens de uma vez (madeira, pedra, etc.)
    [HarmonyPatch]
    public static class PatchDropsMultiplos
    {
        static IEnumerable<MethodBase> TargetMethods()
        {
            foreach (var method in AccessTools.GetDeclaredMethods(typeof(Game1)))
            {
                if (method.Name == "createMultipleObjectDebris")
                {
                    var parameters = method.GetParameters();
                    if (parameters.Length >= 4 &&
                        parameters[1].ParameterType == typeof(int) &&
                        parameters[2].ParameterType == typeof(int) &&
                        parameters[3].ParameterType == typeof(int))
                    {
                        yield return method;
                    }
                }
            }
        }

        static void Prefix(ref int __3)
        {
            if (ModEntry.TemAnelEquipado())
            {
                ModEntry.IsProcessingMultiple = true;
                __3 *= ModEntry.Multiplicador;
            }
        }

        static void Postfix()
        {
            ModEntry.IsProcessingMultiple = false;
        }
    }
}