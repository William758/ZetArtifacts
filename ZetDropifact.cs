using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.UI;
using System;
using UnityEngine;
using RoR2;
using UnityEngine.EventSystems;
using R2API.Utils;
using UnityEngine.Networking;
using MiniRpcLib.Action;
using MiniRpcLib;
using R2API;

namespace TPDespair.ZetArtifacts
{
    public class ZetDropHandler : MonoBehaviour, IPointerClickHandler
    {
        public Func<ItemIndex> GetItemIndex { get; set; }
        public Func<Inventory> GetInventory { get; set; }
        public bool EquipmentIcon { get; set; }

        public void OnPointerClick(PointerEventData eventData)
        {
            ZetDropifact.HandlePointerClick(this, eventData);
        }
    }



    public class DropItemMessage : MessageBase
    {
        public bool IsItem;
        public bool Scrap;
        public ItemIndex ItemIndex = ItemIndex.None;
        public EquipmentIndex EquipmentIndex = EquipmentIndex.None;



        public DropItemMessage(ItemIndex itemIndex, bool scrap)
        {
            ItemIndex = itemIndex;
            IsItem = true;
            Scrap = scrap;
        }

        public DropItemMessage(ItemIndex itemIndex)
        {
            ItemIndex = itemIndex;
            IsItem = true;
            Scrap = false;
        }

        public DropItemMessage(EquipmentIndex equipmentIndex)
        {
            EquipmentIndex = equipmentIndex;
            IsItem = false;
            Scrap = false;
        }



        public override void Serialize(NetworkWriter writer)
        {
            var isItem = ItemIndex != ItemIndex.None;
            writer.Write(isItem);
            writer.Write(Scrap);
            writer.Write(isItem ? (int)ItemIndex : (int)EquipmentIndex);
        }

        public override void Deserialize(NetworkReader reader)
        {
            IsItem = reader.ReadBoolean();
            Scrap = reader.ReadBoolean();
            var index = reader.ReadInt32();
            if (IsItem) ItemIndex = (ItemIndex)index;
            else EquipmentIndex = (EquipmentIndex)index;
        }
    }



    public static class ZetDropifact
    {
        internal static void Init()
        {
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETDROPIFACT_NAME", "Artifact of Tossing");
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETDROPIFACT_DESC", "Allows players to drop and scrap items.\n\n<style=cStack>LeftAlt + RMB to scrap</style>");

            ItemIconHook();
            EquipmentIconHook();

            RegisterDropItemCommand();

            ItemCatalog.availability.CallWhenAvailable(FindLunarScrapIndex);
        }



        public static ItemIndex lunarScrapIndex = ItemIndex.None;

        public static IRpcAction<DropItemMessage> DropItemCommand { get; set; }



        private static void ItemIconHook()
        {
            IL.RoR2.UI.ItemInventoryDisplay.AllocateIcons += (il) =>
            {
                ILCursor c = new ILCursor(il);

                c.GotoNext(
                    x => x.MatchStloc(1),
                    x => x.MatchLdarg(0),
                    x => x.MatchLdfld<ItemInventoryDisplay>("itemIcons")
                );

                c.EmitDelegate<Func<ItemIcon, ItemIcon>>((icon) =>
                {
                    AttachZetDropHandler(icon);

                    return icon;
                });
            };
        }

        private static void AttachZetDropHandler(ItemIcon icon)
        {
            if (icon.GetComponent<ZetDropHandler>() != null) return;
            var dropItemHandler = icon.transform.gameObject.AddComponent<ZetDropHandler>();
            dropItemHandler.GetItemIndex = () => icon.GetFieldValue<ItemIndex>("itemIndex");
            dropItemHandler.GetInventory = () => icon.rectTransform.parent.GetComponent<ItemInventoryDisplay>().GetFieldValue<Inventory>("inventory");
        }

        private static void EquipmentIconHook()
        {
            IL.RoR2.UI.ScoreboardStrip.SetMaster += (il) =>
            {
                ILCursor c = new ILCursor(il);

                c.GotoNext(
                    x => x.MatchCallvirt<ItemInventoryDisplay>("SetSubscribedInventory")
                );

                c.Index += 1;

                c.Emit(OpCodes.Ldarg, 0);
                c.EmitDelegate<Action<ScoreboardStrip>>((strip) =>
                {
                    if (strip.equipmentIcon != null)
                    {
                        AttachZetDropHandler(strip.equipmentIcon);
                    }
                });
            };
        }

        private static void AttachZetDropHandler(EquipmentIcon icon)
        {
            if (icon.GetComponent<ZetDropHandler>() != null) return;
            var dropItemHandler = icon.transform.gameObject.AddComponent<ZetDropHandler>();
            dropItemHandler.GetInventory = () => icon.targetInventory;
            dropItemHandler.EquipmentIcon = true;
        }



        private static void RegisterDropItemCommand()
        {
            DropItemCommand = ZetArtifactsPlugin.miniRpc.RegisterAction(Target.Server, (NetworkUser user, DropItemMessage dropItemMessage) =>
            {
                var master = user.master;
                if (master == null) return;

                var body = master.GetBody();
                var inventory = master.inventory;

                var pickupIndex = dropItemMessage.IsItem
                    ? PickupCatalog.FindPickupIndex(dropItemMessage.ItemIndex)
                    : PickupCatalog.FindPickupIndex(dropItemMessage.EquipmentIndex);

                if (dropItemMessage.IsItem)
                {
                    var itemDef = ItemCatalog.GetItemDef(dropItemMessage.ItemIndex);

                    if (itemDef.tier == ItemTier.NoTier) return;

                    if (dropItemMessage.Scrap)
                    {
                        switch (itemDef.tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Tier3:
                            case ItemTier.Boss:
                                break;
                            case ItemTier.Lunar:
                                if (lunarScrapIndex != ItemIndex.None) break;
                                else return;
                            default:
                                return;
                        }
                    }
                }
                else
                {
                    if (dropItemMessage.Scrap) return;
                }

                DropItem(body, inventory, pickupIndex, dropItemMessage.Scrap);
                CreateNotification(body, pickupIndex, dropItemMessage.Scrap);
            });
        }



        private static void FindLunarScrapIndex()
        {
            ItemIndex index = ItemCatalog.FindItemIndex("ScrapLunar");
            if (index != ItemIndex.None)
            {
                lunarScrapIndex = index;
            }
            Debug.LogWarning("LunarScrapIndex : " + lunarScrapIndex);
        }



        public static void HandlePointerClick(ZetDropHandler handler, PointerEventData eventData)
        {
            var inventory = handler.GetInventory();

            if (!inventory.hasAuthority) return;

            if (!RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetDropifact)) return;

            bool scrapper = Input.GetKey(KeyCode.LeftAlt) && eventData.button == PointerEventData.InputButton.Right;

            if (!NetworkServer.active)
            {
                // Client, send command
                DropItemMessage itemDropMessage;

                if (handler.EquipmentIcon)
                {
                    if (scrapper) return;

                    var equipmentIndex = inventory.GetEquipmentIndex();

                    itemDropMessage = new DropItemMessage(equipmentIndex);
                }
                else
                {
                    var itemIndex = handler.GetItemIndex();

                    if (ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.NoTier) return;

                    if (scrapper)
                    {
                        switch (ItemCatalog.GetItemDef(itemIndex).tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Tier3:
                            case ItemTier.Boss:
                                break;
                            case ItemTier.Lunar:
                                if (lunarScrapIndex != ItemIndex.None) break;
                                else return;
                            default:
                                return;
                        }
                    }

                    itemDropMessage = new DropItemMessage(itemIndex, scrapper);
                }

                DropItemCommand.Invoke(itemDropMessage);
            }
            else
            {
                // Server, execute command
                PickupIndex pickupIndex;

                if (handler.EquipmentIcon)
                {
                    if (scrapper) return;

                    var equipmentIndex = inventory.GetEquipmentIndex();

                    pickupIndex = PickupCatalog.FindPickupIndex(equipmentIndex);
                }
                else
                {
                    var itemIndex = handler.GetItemIndex();

                    if (ItemCatalog.GetItemDef(itemIndex).tier == ItemTier.NoTier) return;

                    if (scrapper)
                    {
                        switch (ItemCatalog.GetItemDef(itemIndex).tier)
                        {
                            case ItemTier.Tier1:
                            case ItemTier.Tier2:
                            case ItemTier.Tier3:
                            case ItemTier.Boss:
                                break;
                            case ItemTier.Lunar:
                                if (lunarScrapIndex != ItemIndex.None) break;
                                else return;
                            default:
                                return;
                        }
                    }

                    pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                }

                var body = inventory.GetComponent<CharacterMaster>().GetBody();

                DropItem(body, inventory, pickupIndex, scrapper);
                CreateNotification(body, pickupIndex, scrapper);
            }
        }

        

        public static void DropItem(CharacterBody body, Inventory inventory, PickupIndex pickupIndex)
        {
            DropItem(body, inventory, pickupIndex, false);
        }

        public static void DropItem(CharacterBody body, Inventory inventory, PickupIndex pickupIndex, bool scrap)
        {
            if (PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex != EquipmentIndex.None)
            {
                if (inventory.GetEquipmentIndex() != PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex) return;

                inventory.SetEquipmentIndex(EquipmentIndex.None);
            }
            else
            {
                ItemIndex ItemIndex = PickupCatalog.GetPickupDef(pickupIndex).itemIndex;

                if (inventory.GetItemCount(ItemIndex) <= 0) return;

                if (scrap)
                {
                    switch (ItemCatalog.GetItemDef(ItemIndex).tier)
                    {
                        case ItemTier.Tier1:
                            pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex);
                            break;
                        case ItemTier.Tier2:
                            pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapGreen.itemIndex);
                            break;
                        case ItemTier.Tier3:
                            pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapRed.itemIndex);
                            break;
                        case ItemTier.Boss:
                            pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapYellow.itemIndex);
                            break;
                        case ItemTier.Lunar:
                            if (lunarScrapIndex != ItemIndex.None) pickupIndex = PickupCatalog.FindPickupIndex(lunarScrapIndex);
                            break;
                        default:
                            break;
                    }
                }

                if (pickupIndex == PickupIndex.none) return;

                inventory.RemoveItem(ItemIndex, 1);
            }

            PickupDropletController.CreatePickupDroplet(pickupIndex, body.transform.position, Vector3.up * 20f + Vector3.right * 10f);
        }



        public static void CreateNotification(CharacterBody character, PickupIndex pickupIndex, bool scrap)
        {
            if (PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex != EquipmentIndex.None)
            {
                CreateNotification(character, PickupCatalog.GetPickupDef(pickupIndex).equipmentIndex);
            }
            else
            {
                CreateNotification(character, PickupCatalog.GetPickupDef(pickupIndex).itemIndex, scrap);
            }
        }

        private static void CreateNotification(CharacterBody character, EquipmentIndex equipmentIndex)
        {
            var equipmentDef = EquipmentCatalog.GetEquipmentDef(equipmentIndex);
            var description = Language.GetString(equipmentDef.nameToken);
            var texture = equipmentDef.pickupIconTexture;

            CreateNotification(character, "Equipment dropped", description, texture);
        }

        private static void CreateNotification(CharacterBody character, ItemIndex itemIndex, bool scrap)
        {
            var itemDef = ItemCatalog.GetItemDef(itemIndex);
            string title = scrap ? "Item scrapped" : "Item dropped";
            var description = Language.GetString(itemDef.nameToken);
            var texture = itemDef.pickupIconTexture;

            CreateNotification(character, title, description, texture);
        }

        private static void CreateNotification(CharacterBody character, string title, string description, Texture texture)
        {
            var notification = character.gameObject.AddComponent<Notification>();
            notification.transform.SetParent(character.transform);
            notification.SetPosition(new Vector3((float)(Screen.width * 0.8), (float)(Screen.height * 0.25), 0));
            notification.SetIcon(texture);
            notification.GetTitle = () => title;
            notification.GetDescription = () => description;
        }
    }
}
