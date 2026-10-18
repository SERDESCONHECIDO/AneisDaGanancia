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
        internal static bool IsProcessingMultiple = false; // Trava para evitar multiplicação dupla

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
        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(Game1))
                .FirstOrDefault(m => m.Name == "createObjectDebris" 
                                  && m.GetParameters().Length >= 4 
                                  && m.GetParameters()[0].ParameterType == typeof(string));
        }

        static bool Prefix(string __0, int __1, int __2, int __3, int __4, float __5, GameLocation __6)
        {
            if (ModEntry.IsProcessingMultiple) return true; // Se já está multiplicando em lote, ignora

            if (ModEntry.TemAnelEquipado())
            {
                for (int i = 0; i < ModEntry.Multiplicador; i++)
                {
                    int offsetX = Game1.random.Next(-32, 33);
                    int offsetY = Game1.random.Next(-32, 33);
                    Game1.createObjectDebris(__0, __1 + offsetX, __2 + offsetY, __3, __4, __5, __6);
                }
                return false; 
            }
            return true;
        }
    }

    // 2. Patch para múltiplos itens de uma vez (ID texto)
    [HarmonyPatch]
    public static class PatchDropsMultiplosString
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(Game1))
                .FirstOrDefault(m => m.Name == "createMultipleObjectDebris" 
                                  && m.GetParameters().Length >= 4 
                                  && m.GetParameters()[0].ParameterType == typeof(string)
                                  && m.GetParameters()[3].ParameterType == typeof(int));
        }

        static void Prefix(ref int __3) // O 4º parâmetro (índice 3) é a quantidade de itens
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

    // 3. Patch para múltiplos itens de uma vez (ID numérico)
    [HarmonyPatch]
    public static class PatchDropsMultiplosInt
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(Game1))
                .FirstOrDefault(m => m.Name == "createMultipleObjectDebris" 
                                  && m.GetParameters().Length >= 4 
                                  && m.GetParameters()[0].ParameterType == typeof(int)
                                  && m.GetParameters()[3].ParameterType == typeof(int));
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