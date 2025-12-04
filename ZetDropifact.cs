using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Networking;
using R2API.Networking.Interfaces;
using R2API.Utils;
using RoR2;
using RoR2.Artifacts;
using RoR2.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;

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
		public float Angle;

		public void Serialize(NetworkWriter writer)
		{
			writer.Write(Body.gameObject);
			writer.Write(DropType);
			writer.Write(Index);
			writer.Write(Angle);
		}

		public void Deserialize(NetworkReader reader)
		{
			Body = reader.ReadGameObject().GetComponent<CharacterBody>();
			DropType = reader.ReadInt32();
			Index = reader.ReadInt32();
			Angle = reader.ReadSingle();
		}

		public void OnReceived()
		{
			if (NetworkServer.active)
			{
				// Request from client
				if (ZetDropifact.HandleClientRequest(this))
				{
					int dropType = this.DropType;
					if (dropType == 1 && !ZetArtifactsPlugin.DropifactAllowScrapping.Value) dropType = 0;

					// Notify clients about success
					ZetDropReply dropReply = new ZetDropReply { Body = Body, DropType = dropType, Index = Index };
					dropReply.Send(NetworkDestination.Clients);
				}
			}
		}
	}



	public static class ZetDropifact
	{
		public static CharacterBody LocalBody;

		public static ItemIndex ArtifactKeyIndex = ItemIndex.None;
		public static ItemIndex LunarScrapIndex = ItemIndex.None;

		public static ItemTier LunarVoidTier = ItemTier.AssignedAtRuntime;

		public static bool appliedVoidBearFix = false;



		private static int State = 0;
		internal static ArtifactDef ArtifactDef;

		public static bool Enabled
		{
			get
			{
				if (State < 1) return false;
				else if (State > 1) return true;

				return ZetArtifactsPlugin.ArtifactEnabled(ArtifactDef);
			}
		}



		internal static void Init()
		{
			State = ZetArtifactsPlugin.DropifactEnable.Value;

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETDROPIFACT_NAME", "Artifact of Tossing");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETDROPIFACT_DESC", BuildDescription());

			NetworkingAPI.RegisterMessageType<ZetDropReply>();
			NetworkingAPI.RegisterMessageType<ZetDropRequest>();

			ItemIconHook();
			EquipmentIconHook();

			SceneDirector.onGenerateInteractableCardSelection += RemoveScrapperCard;
			On.RoR2.BazaarController.Start += AddBazaarScrapper;
		}

		internal static void LateSetup()
		{
			if (State < 1) return;

			ItemIndex itemIndex = ItemCatalog.FindItemIndex("ArtifactKey");
			if (itemIndex != ItemIndex.None)
			{
				ArtifactKeyIndex = itemIndex;
			}
			itemIndex = ItemCatalog.FindItemIndex("ScrapLunar");
			if (itemIndex != ItemIndex.None)
			{
				LunarScrapIndex = itemIndex;
			}

			ItemTierDef itemTierDef = ItemTierCatalog.FindTierDef("VoidLunarTierDef");
			if (itemTierDef)
			{
				LunarVoidTier = itemTierDef.tier;
			}

			if (!ZetArtifactsPlugin.PluginLoaded("com.TPDespair.ZetAspects"))
			{
				On.RoR2.CharacterBody.AddTimedBuff_BuffDef_float += VoidBearFix_AddTimedBuff;
				On.RoR2.CharacterBody.RemoveBuff_BuffIndex += VoidBearFix_RemoveBuff;

				appliedVoidBearFix = true;
			}
		}



		public static string BuildDescription()
		{
			string str = "Allows players to drop";

			if (ZetArtifactsPlugin.DropifactAllowScrapping.Value) str += " and scrap";
			str += " items.";
			if (ZetArtifactsPlugin.DropifactAllowScrapping.Value)
			{
				str += "\n\n<style=cStack>";
				str += (ZetArtifactsPlugin.DropifactAltScrap.Value ? "LeftAlt + " : "") + "RMB to scrap</style>";
			}

			return str;
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

			bool scrap = ZetArtifactsPlugin.DropifactAllowScrapping.Value && (!ZetArtifactsPlugin.DropifactAltScrap.Value || Input.GetKey(KeyCode.LeftAlt)) && eventData.button == PointerEventData.InputButton.Right;

			float aimAngle = GetAimAngle(body);

			if (!NetworkServer.active)
			{
				// Client, send message
				ZetDropRequest dropMessage;

				if (handler.EquipmentIcon)
				{
					EquipmentIndex equipIndex = inventory.GetEquipmentIndex();

					if (!ValidDropRequest(equipIndex, false)) return;

					dropMessage = new ZetDropRequest { Body = body, DropType = 2, Index = (int)equipIndex, Angle = aimAngle };
				}
				else
				{
					ItemIndex itemIndex = handler.GetItemIndex();

					if (!ValidDropRequest(itemIndex, scrap)) return;

					dropMessage = new ZetDropRequest { Body = body, DropType = scrap ? 1 : 0, Index = (int)itemIndex, Angle = aimAngle };
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

					if (!ValidDropRequest(equipIndex, false)) return;

					if (DropItem(body, inventory, equipIndex, aimAngle)) CreateNotification(body, equipIndex);
				}
				else
				{
					ItemIndex itemIndex = handler.GetItemIndex();

					if (!ValidDropRequest(itemIndex, scrap)) return;

					if (DropItem(body, inventory, itemIndex, aimAngle, scrap)) CreateNotification(body, itemIndex, scrap);
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

				if (DropItem(body, inventory, equipIndex, dropRequest.Angle)) return true;
			}
			else
			{
				bool scrap = ZetArtifactsPlugin.DropifactAllowScrapping.Value && dropRequest.DropType == 1;

				ItemIndex itemIndex = (ItemIndex)dropRequest.Index;

				if (!ValidDropRequest(itemIndex, scrap)) return false;

				if (DropItem(body, inventory, itemIndex, dropRequest.Angle, scrap)) return true;
			}

			return false;
		}



		private static bool ValidDropRequest(EquipmentIndex index, bool scrap)
		{
			if (index == EquipmentIndex.None) return false;
			/*
			EquipmentDef equipDef = EquipmentCatalog.GetEquipmentDef(index);

			if (!ZetArtifactsPlugin.DropifactLunar.Value && equipDef.isLunar) return false;
			*/
			if (!ZetArtifactsPlugin.DropifactEquipment.Value) return false;

			if (scrap) return false;

			return true;
		}

		private static bool ValidDropRequest(ItemIndex index, bool scrap)
		{
			if (index == ItemIndex.None) return false;

			if (index == ArtifactKeyIndex && scrap) return false;

			ItemDef itemDef = ItemCatalog.GetItemDef(index);

			if (itemDef.tier == ItemTier.NoTier) return false;

			if (IsDropRestricted(itemDef.tier)) return false;

			if (!ZetArtifactsPlugin.DropifactUnique.Value && itemDef.ContainsTag(ItemTag.WorldUnique)) return false;

			if (scrap) return IsScrapable(itemDef.tier);

			return true;
		}



		private static bool IsScrapable(ItemTier tier)
		{
			if (LunarVoidTier != ItemTier.AssignedAtRuntime && tier == LunarVoidTier) return false;

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

		private static bool IsDropRestricted(ItemTier tier)
		{
			if (IsVoidTier(tier))
			{
				if (!ZetArtifactsPlugin.DropifactVoid.Value)
				{
					return true;
				}
				else
				{
					if (tier == ItemTier.VoidTier1) return !ZetArtifactsPlugin.DropifactVoidT1.Value;
					if (tier == ItemTier.VoidTier2) return !ZetArtifactsPlugin.DropifactVoidT2.Value;
					if (tier == ItemTier.VoidTier3) return !ZetArtifactsPlugin.DropifactVoidT3.Value;
					if (tier == ItemTier.VoidBoss) return !ZetArtifactsPlugin.DropifactVoidBoss.Value;

					if (LunarVoidTier != ItemTier.AssignedAtRuntime && tier == LunarVoidTier)
					{
						return !ZetArtifactsPlugin.DropifactVoidLunar.Value;
					}
				}
			}
			else
			{
				if (tier == ItemTier.Tier1) return !ZetArtifactsPlugin.DropifactT1.Value;
				if (tier == ItemTier.Tier2) return !ZetArtifactsPlugin.DropifactT2.Value;
				if (tier == ItemTier.Tier3) return !ZetArtifactsPlugin.DropifactT3.Value;
				if (tier == ItemTier.Boss) return !ZetArtifactsPlugin.DropifactBoss.Value;
				if (tier == ItemTier.Lunar) return !ZetArtifactsPlugin.DropifactLunar.Value;
			}

			return false;
		}

		private static bool IsVoidTier(ItemTier tier)
		{
			if (tier == ItemTier.VoidTier1) return true;
			if (tier == ItemTier.VoidTier2) return true;
			if (tier == ItemTier.VoidTier3) return true;
			if (tier == ItemTier.VoidBoss) return true;

			if (LunarVoidTier != ItemTier.AssignedAtRuntime && tier == LunarVoidTier) return true;

			return false;
		}



		private static int GetRealItemCount(Inventory inventory, ItemIndex itemIndex)
		{
			return inventory.permanentItemStacks.GetStackValue(itemIndex);
		}



		private static bool DropItem(CharacterBody body, Inventory inventory, EquipmentIndex index, float angle)
		{
			if (inventory.GetEquipmentIndex() != index) return false;

			inventory.SetEquipmentIndex(EquipmentIndex.None, true);

			CreateDroplet(index, body.transform.position, angle);

			return true;
		}

		private static bool DropItem(CharacterBody body, Inventory inventory, ItemIndex index, float angle, bool scrap)
		{
			ItemIndex dropIndex = index;

			if (GetRealItemCount(inventory, index) <= 0) return false;

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

			inventory.RemoveItemPermanent(index, 1);

			CreateDroplet(dropIndex, body.transform.position, angle);

			return true;
		}



		private static float GetAimAngle(CharacterBody body)
		{
			InputBankTest input = body.inputBank;
			if (input)
			{
				Vector3 lookDirection = input.aimDirection;
				Vector3 flatLookDirection = new Vector3(lookDirection.x, 0f, lookDirection.z);
				return Vector3.SignedAngle(Vector3.forward, flatLookDirection, Vector3.up);
			}

			return 0f;
		}



		private static void CreateDroplet(EquipmentIndex index, Vector3 pos, float angle)
		{
			CreateDroplet(PickupCatalog.FindPickupIndex(index), pos, angle);
		}

		private static void CreateDroplet(ItemIndex index, Vector3 pos, float angle)
		{
			CreateDroplet(PickupCatalog.FindPickupIndex(index), pos, angle);
		}

		private static void CreateDroplet(PickupIndex index, Vector3 pos, float angle)
		{
			CreatePickupDroplet(index, pos, Vector3.up * 20f + (Quaternion.AngleAxis(angle, Vector3.up) * (Vector3.forward * 10f)));
		}



		private static void CreatePickupDroplet(PickupIndex pickupIndex, Vector3 position, Vector3 velocity)
		{
			if (!NetworkServer.active)
			{
				ZetArtifactsPlugin.LogWarn("[Server] function 'System.Void TPDespair.ZetDropifact::CreatePickupDroplet(RoR2.PickupIndex, UnityEngine.Vector3, UnityEngine.Vector3)' called on client");
				return;
			}

			GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
			{
				pickup = new UniquePickup(pickupIndex),
				rotation = Quaternion.identity, 
				recycled = !ZetArtifactsPlugin.DropifactAllowRecycling.Value
			};

			if (!ZetArtifactsPlugin.DropifactBypassGround.Value && CommandArtifactManager.IsCommandArtifactEnabled)
			{
				pickupInfo.artifactFlag |= GenericPickupController.PickupArtifactFlag.COMMAND;
			}

			GameObject droplet = UnityEngine.Object.Instantiate(PickupDropletController.pickupDropletPrefab, position, Quaternion.identity);

			PickupDropletController controller = droplet.GetComponent<PickupDropletController>();
			if (controller)
			{
				controller.createPickupInfo = pickupInfo;
				controller.NetworkpickupState = pickupInfo.pickup;
			}

			Rigidbody rigidBody = droplet.GetComponent<Rigidbody>();
			rigidBody.velocity = velocity;
			rigidBody.AddTorque(UnityEngine.Random.Range(150f, 120f) * UnityEngine.Random.onUnitSphere);

			NetworkServer.Spawn(droplet);
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
			dropItemHandler.GetItemIndex = () => icon.itemIndex;
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

		private static void AddBazaarScrapper(On.RoR2.BazaarController.orig_Start orig, BazaarController self)
		{
			orig(self);

			if (NetworkServer.active && Enabled && ZetArtifactsPlugin.DropifactBazaarScrapper.Value)
			{
				ArtifactDef artifactDef = ArtifactCatalog.FindArtifactDef("Sacrifice");
				bool isSacrifice = RunArtifactManager.instance.IsArtifactEnabled(artifactDef);

				if (isSacrifice) RunArtifactManager.instance.SetArtifactEnabledServer(artifactDef, false);

				SpawnCard iscScrapper = LegacyResourcesAPI.Load<SpawnCard>("SpawnCards/InteractableSpawnCard/iscScrapper");
				DirectorPlacementRule directPlacement = new DirectorPlacementRule { placementMode = DirectorPlacementRule.PlacementMode.Direct };
				DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(iscScrapper, directPlacement, Run.instance.runRNG);

				GameObject gameObject = iscScrapper.DoSpawn(new Vector3(-85f, -23.75f, -5f), Quaternion.identity, spawnRequest).spawnedInstance;
				gameObject.transform.eulerAngles = new Vector3(0f, 165f, 0f);
				NetworkServer.Spawn(gameObject);

				if (isSacrifice) RunArtifactManager.instance.SetArtifactEnabledServer(artifactDef, true);
			}
		}



		private static void VoidBearFix_AddTimedBuff(On.RoR2.CharacterBody.orig_AddTimedBuff_BuffDef_float orig, CharacterBody self, BuffDef buffDef, float duration)
		{
			if (buffDef == null) return;

			if (NetworkServer.active)
			{
				BuffIndex buff = DLC1Content.Buffs.BearVoidCooldown.buffIndex;

				if (buffDef.buffIndex == buff && self.GetBuffCount(buff) < 1)
				{
					self.ClearTimedBuffs(buff);
					self.SetBuffCount(buff, 0);
				}

				// Unlike ZetAspects, BearVoidCooldown has not been set to stack so we don't need to apply the buff multiple times.
			}

			orig(self, buffDef, duration);
		}

		private static void VoidBearFix_RemoveBuff(On.RoR2.CharacterBody.orig_RemoveBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
		{
			if (NetworkServer.active)
			{
				BuffIndex buff = DLC1Content.Buffs.BearVoidCooldown.buffIndex;

				if (buffType == buff && self.GetBuffCount(buff) < 1) return;
			}

			orig(self, buffType);
		}
	}
}
