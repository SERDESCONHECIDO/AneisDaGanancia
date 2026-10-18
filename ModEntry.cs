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
    [HarmonyPatch(typeof(Game1), nameof(Game1.createObjectDebris))]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(GameLocation) })]
    public static class PatchDropsUnicos
    {
        static bool Prefix(ref string id, int xTile, int yTile, int groundLevel, int itemQuality, float velocityMultiplyer, GameLocation location)
        {
            if (ModEntry.IsProcessingMultiple) return true; // Se já está multiplicando em lote, não faz nada aqui

            if (ModEntry.TemAnelEquipado())
            {
                for (int i = 0; i < ModEntry.Multiplicador; i++)
                {
                    int offsetX = Game1.random.Next(-32, 33);
                    int offsetY = Game1.random.Next(-32, 33);
                    Game1.createObjectDebris(id, xTile + offsetX, yTile + offsetY, groundLevel, itemQuality, velocityMultiplyer, location);
                }
                return false; 
            }
            return true;
        }
    }

    // 2. Patch dinâmico para múltiplos itens (ID em texto)
    [HarmonyPatch]
    public static class PatchDropsMultiplosString
    {
        static MethodBase TargetMethod()
        {
            // Encontra o método dinamicamente sem depender da ordem exata dos parâmetros
            return AccessTools.GetDeclaredMethods(typeof(Game1))
                .FirstOrDefault(m => m.Name == "createMultipleObjectDebris" 
                                  && m.GetParameters().Length == 7 
                                  && m.GetParameters()[0].ParameterType == typeof(string));
        }

        static void Prefix(object[] __args)
        {
            if (ModEntry.TemAnelEquipado() && __args != null && __args.Length > 3)
            {
                ModEntry.IsProcessingMultiple = true; // Ativa a trava
                __args[3] = (int)__args[3] * ModEntry.Multiplicador; // Multiplica o parâmetro "number" (posição 3)
            }
        }

        static void Postfix()
        {
            ModEntry.IsProcessingMultiple = false; // Desativa a trava
        }
    }

    // 3. Patch dinâmico para múltiplos itens (ID numérico)
    [HarmonyPatch]
    public static class PatchDropsMultiplosInt
    {
        static MethodBase TargetMethod()
        {
            return AccessTools.GetDeclaredMethods(typeof(Game1))
                .FirstOrDefault(m => m.Name == "createMultipleObjectDebris" 
                                  && m.GetParameters().Length == 7 
                                  && m.GetParameters()[0].ParameterType == typeof(int));
        }

        static void Prefix(object[] __args)
        {
            if (ModEntry.TemAnelEquipado() && __args != null && __args.Length > 3)
            {
                ModEntry.IsProcessingMultiple = true;
                __args[3] = (int)__args[3] * ModEntry.Multiplicador;
            }
        }

        static void Postfix()
        {
            ModEntry.IsProcessingMultiple = false;
        }
    }
}