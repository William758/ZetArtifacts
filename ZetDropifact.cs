using System;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.EventSystems;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2;
using RoR2.UI;
using R2API;
using R2API.Utils;
using R2API.Networking;
using R2API.Networking.Interfaces;

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



	public class ZetDropReply : INetMessage
	{
		public CharacterBody Body;
		public int DropType;
		public int Index;

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(Body.gameObject);
			writer.Write(DropType);
			writer.Write(Index);
		}

		public void Deserialize(NetworkReader reader)
		{
			Body = reader.ReadGameObject().GetComponent<CharacterBody>();
			DropType = reader.ReadInt32();
			Index = reader.ReadInt32();
		}

		public void OnReceived()
		{
			if (!NetworkServer.active)
			{
				// Client display notification
				ZetDropifact.HandleServerReply(this);
			}
		}
	}

	public class ZetDropRequest : INetMessage
	{
		public CharacterBody Body;
		public int DropType;
		public int Index;

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(Body.gameObject);
			writer.Write(DropType);
			writer.Write(Index);
		}

		public void Deserialize(NetworkReader reader)
		{
			Body = reader.ReadGameObject().GetComponent<CharacterBody>();
			DropType = reader.ReadInt32();
			Index = reader.ReadInt32();
		}

		public void OnReceived()
		{
			if (NetworkServer.active)
			{
				// Request from client
				if (ZetDropifact.HandleClientRequest(this))
				{
					// Notify clients about success
					ZetDropReply dropReply = new ZetDropReply { Body = Body, DropType = DropType, Index = Index };
					dropReply.Send(NetworkDestination.Clients);
				}
			}
		}
	}



	public static class ZetDropifact
	{
		public static CharacterBody LocalBody;

		public static ItemIndex LunarScrapIndex = ItemIndex.None;
		public static ItemIndex ArtifactKeyIndex = ItemIndex.None;

		public static bool appliedVoidBearFix = false;



		private static int state = 0;

		public static bool Enabled
		{
			get
			{
				if (state < 1) return false;
				else if (state > 1) return true;
				else
				{
					if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetDropifact)) return true;

					return false;
				}
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.DropifactEnable.Value;
			if (state < 1) return;

			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETDROPIFACT_NAME", "Artifact of Tossing");
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETDROPIFACT_DESC", "Allows players to drop and scrap items.\n\n<style=cStack>LeftAlt + RMB to scrap</style>");

			NetworkingAPI.RegisterMessageType<ZetDropReply>();
			NetworkingAPI.RegisterMessageType<ZetDropRequest>();

			ItemCatalog.availability.CallWhenAvailable(FindIndexes);

			ItemIconHook();
			EquipmentIconHook();

			PreventVoidBearBuffBug();
			appliedVoidBearFix = true;

			SceneDirector.onGenerateInteractableCardSelection += RemoveScrapperCard;
		}



		internal static void HandlePointerClick(ZetDropHandler handler, PointerEventData eventData)
		{
			if (!Enabled) return;

			Inventory inventory = handler.GetInventory();
			if (!inventory || !inventory.hasAuthority) return;

			CharacterMaster master = inventory.GetComponent<CharacterMaster>();
			if (!master) return;
			CharacterBody body = master.GetBody();
			if (!body) return;

			bool scrap = Input.GetKey(KeyCode.LeftAlt) && eventData.button == PointerEventData.InputButton.Right;

			if (!NetworkServer.active)
			{
				// Client, send message
				ZetDropRequest dropMessage;

				if (handler.EquipmentIcon)
				{
					EquipmentIndex equipIndex = inventory.GetEquipmentIndex();

					if (!ValidDropRequest(equipIndex, scrap)) return;

					dropMessage = new ZetDropRequest { Body = body, DropType = 2, Index = (int)equipIndex };
				}
				else
				{
					ItemIndex itemIndex = handler.GetItemIndex();

					if (!ValidDropRequest(itemIndex, scrap)) return;

					dropMessage = new ZetDropRequest { Body = body, DropType = scrap ? 1 : 0, Index = (int)itemIndex };
				}

				// used to identify self when we recieve drop confirmation to display notification
				LocalBody = body;

				dropMessage.Send(NetworkDestination.Server);
			}
			else
			{
				// Server, execute action
				if (handler.EquipmentIcon)
				{
					EquipmentIndex equipIndex = inventory.GetEquipmentIndex();

					if (!ValidDropRequest(equipIndex, scrap)) return;

					if (DropItem(body, inventory, equipIndex)) CreateNotification(body, equipIndex);
				}
				else
				{
					ItemIndex itemIndex = handler.GetItemIndex();

					if (!ValidDropRequest(itemIndex, scrap)) return;

					if (DropItem(body, inventory, itemIndex, scrap)) CreateNotification(body, itemIndex, scrap);
				}
			}
		}

		internal static void HandleServerReply(ZetDropReply dropReply)
		{
			if (!Enabled) return;

			CharacterBody body = dropReply.Body;
			if (!body || !LocalBody || body != LocalBody) return;

			Inventory inventory = body.inventory;
			if (!inventory) return;

			if (dropReply.DropType == 2)
			{
				EquipmentIndex equipIndex = (EquipmentIndex)dropReply.Index;

				CreateNotification(body, equipIndex);
			}
			else
			{
				bool scrap = dropReply.DropType == 1;

				ItemIndex itemIndex = (ItemIndex)dropReply.Index;

				CreateNotification(body, itemIndex, scrap);
			}
		}

		internal static bool HandleClientRequest(ZetDropRequest dropRequest)
		{
			if (!Enabled) return false;

			CharacterBody body = dropRequest.Body;
			if (!body) return false;

			Inventory inventory = body.inventory;
			if (!inventory) return false;

			if (dropRequest.DropType == 2)
			{
				EquipmentIndex equipIndex = (EquipmentIndex)dropRequest.Index;

				if (!ValidDropRequest(equipIndex, false)) return false;

				if (DropItem(body, inventory, equipIndex)) return true;
			}
			else
			{
				bool scrap = dropRequest.DropType == 1;

				ItemIndex itemIndex = (ItemIndex)dropRequest.Index;

				if (!ValidDropRequest(itemIndex, scrap)) return false;

				if (DropItem(body, inventory, itemIndex, scrap)) return true;
			}

			return false;
		}



		private static bool ValidDropRequest(EquipmentIndex index, bool scrap)
		{
			if (index == EquipmentIndex.None) return false;

			EquipmentDef equipDef = EquipmentCatalog.GetEquipmentDef(index);

			if (!ZetArtifactsPlugin.DropifactLunar.Value && equipDef.isLunar) return false;

			if (scrap) return false;

			return true;
		}

		private static bool ValidDropRequest(ItemIndex index, bool scrap)
		{
			if (index == ItemIndex.None) return false;

			if (index == ArtifactKeyIndex && scrap) return false;

			ItemDef itemDef = ItemCatalog.GetItemDef(index);

			if (itemDef.tier == ItemTier.NoTier) return false;

			if (!ZetArtifactsPlugin.DropifactLunar.Value && itemDef.tier == ItemTier.Lunar) return false;
			if (!ZetArtifactsPlugin.DropifactVoid.Value && IsVoidTier(itemDef.tier)) return false;
			if (!ZetArtifactsPlugin.DropifactUnique.Value && itemDef.ContainsTag(ItemTag.WorldUnique)) return false;

			if (scrap) return IsScrapable(itemDef.tier);

			return true;
		}



		private static bool IsScrapable(ItemTier tier)
		{
			switch (tier)
			{
				case ItemTier.Tier1:
				case ItemTier.Tier2:
				case ItemTier.Tier3:
				case ItemTier.Boss:
					return true;
				case ItemTier.Lunar:
					if (LunarScrapIndex != ItemIndex.None) return true;
					else return false;
				case ItemTier.VoidTier1:
				case ItemTier.VoidTier2:
				case ItemTier.VoidTier3:
				case ItemTier.VoidBoss:
					return false;
				default:
					return false;
			}
		}

		private static bool IsVoidTier(ItemTier tier)
		{
			if (tier == ItemTier.VoidTier1) return true;
			if (tier == ItemTier.VoidTier2) return true;
			if (tier == ItemTier.VoidTier3) return true;
			if (tier == ItemTier.VoidBoss) return true;

			return false;
		}



		private static bool DropItem(CharacterBody body, Inventory inventory, EquipmentIndex index)
		{
			if (inventory.GetEquipmentIndex() != index) return false;

			inventory.SetEquipmentIndex(EquipmentIndex.None);

			CreateDroplet(index, body.transform.position);

			return true;
		}

		private static bool DropItem(CharacterBody body, Inventory inventory, ItemIndex index, bool scrap)
		{
			ItemIndex dropIndex = index;

			if (inventory.GetItemCount(index) <= 0) return false;

			if (scrap)
			{
				switch (ItemCatalog.GetItemDef(index).tier)
				{
					case ItemTier.Tier1:
						dropIndex = RoR2Content.Items.ScrapWhite.itemIndex;
						break;
					case ItemTier.Tier2:
						dropIndex = RoR2Content.Items.ScrapGreen.itemIndex;
						break;
					case ItemTier.Tier3:
						dropIndex = RoR2Content.Items.ScrapRed.itemIndex;
						break;
					case ItemTier.Boss:
						dropIndex = RoR2Content.Items.ScrapYellow.itemIndex;
						break;
					case ItemTier.Lunar:
						if (LunarScrapIndex != ItemIndex.None) dropIndex = LunarScrapIndex;
						break;
					default:
						return false;
				}
			}

			if (dropIndex == ItemIndex.None) return false;

			inventory.RemoveItem(index, 1);

			CreateDroplet(dropIndex, body.transform.position);

			return true;
		}



		private static void CreateDroplet(EquipmentIndex index, Vector3 pos)
		{
			CreateDroplet(PickupCatalog.FindPickupIndex(index), pos);
		}

		private static void CreateDroplet(ItemIndex index, Vector3 pos)
		{
			CreateDroplet(PickupCatalog.FindPickupIndex(index), pos);
		}

		private static void CreateDroplet(PickupIndex index, Vector3 pos)
		{
			PickupDropletController.CreatePickupDroplet(index, pos, Vector3.up * 20f + Vector3.right * 10f);
		}



		private static void CreateNotification(CharacterBody body, EquipmentIndex index)
		{
			var equipmentDef = EquipmentCatalog.GetEquipmentDef(index);
			var description = Language.GetString(equipmentDef.nameToken);
			var texture = equipmentDef.pickupIconTexture;

			CreateNotification(body, "Equipment dropped", description, texture);
		}

		private static void CreateNotification(CharacterBody body, ItemIndex index, bool scrap)
		{
			var itemDef = ItemCatalog.GetItemDef(index);
			string title = scrap ? "Item scrapped" : "Item dropped";
			var description = Language.GetString(itemDef.nameToken);
			var texture = itemDef.pickupIconTexture;

			CreateNotification(body, title, description, texture);
		}

		private static void CreateNotification(CharacterBody body, string title, string description, Texture texture)
		{
			var notification = body.gameObject.AddComponent<Notification>();
			if (notification)
			{
				notification.transform.SetParent(body.transform);
				float x = Screen.width * 0.8f;
				float y = Screen.height * 0.25f;
				notification.SetPosition(new Vector3(x, y, 0));
				notification.SetIcon(texture);
				notification.GetTitle = () => title;
				notification.GetDescription = () => description;

				UnityEngine.Object.Destroy(notification, 4.25f);
			}
		}



		private static void FindIndexes()
		{
			ItemIndex index = ItemCatalog.FindItemIndex("ScrapLunar");
			if (index != ItemIndex.None) LunarScrapIndex = index;
			index = ItemCatalog.FindItemIndex("ArtifactKey");
			if (index != ItemIndex.None) ArtifactKeyIndex = index;
		}



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
					if (strip.equipmentIcon != null) AttachZetDropHandler(strip.equipmentIcon);
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



		private static void PreventVoidBearBuffBug()
		{
			//On.RoR2.CharacterMaster.OnBodyStart += CharacterMaster_OnBodyStart;

			On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += CharacterBody_AddTimedBuff_BuffDef_float;
			On.RoR2.CharacterBody.RemoveBuff_BuffIndex += CharacterBody_RemoveBuff_BuffIndex;
		}
		/*
		private static void CharacterMaster_OnBodyStart(On.RoR2.CharacterMaster.orig_OnBodyStart orig, CharacterMaster self, CharacterBody body)
		{
			orig(self, body);

			if (NetworkServer.active)
			{
				Inventory inventory = self.inventory;
				if (inventory && body.isPlayerControlled)
				{
					inventory.GiveItem(DLC1Content.Items.BearVoid);
				}
			}
		}
		*/
		private static void CharacterBody_AddTimedBuff_BuffDef_float(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
		{
			if (NetworkServer.active)
			{
				BuffIndex buff = DLC1Content.Buffs.BearVoidCooldown.buffIndex;

				if (buffDef.buffIndex == buff && self.GetBuffCount(buff) < 1)
				{
					self.ClearTimedBuffs(buff);
					self.SetBuffCount(buff, 0);
				}
			}

			orig(self, buffDef, duration);
		}

		private static void CharacterBody_RemoveBuff_BuffIndex(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
		{
			if (NetworkServer.active)
			{
				BuffIndex buff = DLC1Content.Buffs.BearVoidCooldown.buffIndex;

				if (buffType == buff && self.GetBuffCount(buff) < 1) return;
			}

			orig(self, buffType);
		}



		private static void RemoveScrapperCard(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
		{
			if (Enabled && ZetArtifactsPlugin.DropifactRemoveScrapper.Value)
			{
				dccs.RemoveCardsThatFailFilter(new Predicate<DirectorCard>(NotScrapper));
			}
		}

		private static bool NotScrapper(DirectorCard card)
		{
			GameObject prefab = card.spawnCard.prefab;
			return !prefab.GetComponent<ScrapperController>();
		}

	}
}
