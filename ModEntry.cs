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
            
            // Usa o evento correto para editar os dados do jogo
            helper.Events.Content.AssetRequested += OnAssetRequested;
            helper.Events.GameLoop.SaveLoaded += OnSaveLoaded;

            var harmony = new Harmony(this.ModManifest.UniqueID);
            harmony.PatchAll();
        }

        private void OnAssetRequested(object sender, AssetRequestedEventArgs e)
        {
            // Edita o arquivo Data/Objects para adicionar o anel
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
            // Adiciona o anel ao inventário do jogador ao carregar o save
            if (!Game1.player.Items.Any(item => item?.ItemId == "AnelDaGanancia_Item"))
            {
                var anel = new Ring("AnelDaGanancia_Item");
                Game1.player.addItemToInventory(anel);
                this.Monitor.Log("Anel da Ganancia adicionado ao inventario.", LogLevel.Info);
            }
        }
    }

    // Patch corrigido para a assinatura real do método no jogo 1.6
    [HarmonyPatch(typeof(Game1), nameof(Game1.createObjectDebris))]
    [HarmonyPatch(new Type[] { typeof(string), typeof(int), typeof(int), typeof(int), typeof(int), typeof(float), typeof(GameLocation) })]
    public static class PatchDrops
    {
        static bool Prefix(ref string id, int x, int y, int groundLevel, int itemQuality, float velocity, GameLocation location)
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
                    // A chamada original também precisa do parâmetro velocity
                    Game1.createObjectDebris(id, x + offsetX, y + offsetY, groundLevel, itemQuality, velocity, location);
                }
                return false; 
            }
            return true;
        }
    }
}