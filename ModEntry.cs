using System;
using System.Collections.Generic;
using System.Linq;
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
    }

    // Patch corrigido com os nomes EXATOS das variáveis do jogo (xTile, yTile, velocityMultiplyer)
    [HarmonyPatch(typeof(Game1), nameof(Game1.createObjectDebris))]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(GameLocation) })]
    public static class PatchDrops
    {
        static bool Prefix(ref string id, int xTile, int yTile, int groundLevel, int itemQuality, float velocityMultiplyer, GameLocation location)
        {
            if (Game1.player == null) return true;
            
            bool temAnel = Game1.player.leftRing.Value?.ItemId == "AnelDaGanancia_Item" || 
                           Game1.player.rightRing.Value?.ItemId == "AnelDaGanancia_Item";

            if (temAnel)
            {
                for (int i = 0; i < ModEntry.Multiplicador; i++)
                {
                    int offsetX = Game1.random.Next(-32, 33);
                    int offsetY = Game1.random.Next(-32, 33);
                    // Usa os nomes de variáveis corretos
                    Game1.createObjectDebris(id, xTile + offsetX, yTile + offsetY, groundLevel, itemQuality, velocityMultiplyer, location);
                }
                return false; 
            }
            return true;
        }
    }
}