using System;
using RoR2;
using RoR2.UI;
using UnityEngine;
using Mono.Cecil.Cil;
using MonoMod.Cil;

namespace TPDespair.ZetArtifacts
{
	public static class ZetHoardifact
	{
		private static float AddedElapsedTime = 0f;

		public static ItemTier LunarVoidTier = ItemTier.AssignedAtRuntime;

		private static GameObject DisplayPanel;
		private static GameObject DisplayTimeText;
		private static HGTextMeshProUGUI TimeTextMesh;
		private static string CurrentTimeText = "";

		private static bool Recalc = false;
		private static float RecalcTimer = 0.25f;

		internal static int State = 0;
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
			if (!ZetArtifactsPlugin.HoardifactMerge.Value)
			{
				State = ZetArtifactsPlugin.HoardifactEnable.Value;
			}
			else
			{
				State = ZetArtifactsPlugin.MultifactEnable.Value;
			}

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETHOARDIFACT_NAME", "Artifact of Accumulation");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETHOARDIFACT_DESC", GetDescription());

			IL.RoR2.Run.RecalculateDifficultyCoefficentInternal += DifficultyHook;

			Run.onRunStartGlobal += Run_onRunStartGlobal;
			Run.onRunDestroyGlobal += Run_onRunDestroyGlobal;
			CharacterBody.onBodyInventoryChangedGlobal += CharacterBody_onBodyInventoryChangedGlobal;

			HUDAwakeHook();
			DirectorMoneyHook();
		}

		internal static void LateSetup()
		{
			if (State < 1) return;

			ItemTierDef itemTierDef = ItemTierCatalog.FindTierDef("VoidLunarTierDef");
			if (itemTierDef)
			{
				LunarVoidTier = itemTierDef.tier;
			}
		}



		private static string GetDescription()
		{
			string text = "";

			if (ZetArtifactsPlugin.HoardifactDifficulty.Value)
			{
				text += "Collected items increase difficulty.";
			}

			if (ZetArtifactsPlugin.HoardifactSetupMoney.Value > 0.095f)
			{
				if (text != "") text += "\n";
				text += "Interactable spawns are increased.";
			}

			return text;
		}



		internal static void OnFixedUpdate()
		{
			if (Recalc)
			{
				RecalcTimer -= Time.fixedDeltaTime;
				if (RecalcTimer <= 0f)
				{
					Recalc = false;
					RecalcTimer = 0.25f;

					UpdateKleptoValue();
				}
			}
		}



		private static void DifficultyHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			bool found = c.TryGotoNext(
				x => x.MatchStloc(0)
			);

			if (found)
			{
				c.Index += 1;

				c.Emit(OpCodes.Ldloc, 0);
				c.EmitDelegate<Func<float, float>>((stopwatch) =>
				{
					if (Enabled)
					{
						stopwatch += AddedElapsedTime;
					}

					return stopwatch;
				});
				c.Emit(OpCodes.Stloc, 0);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetHoardifact] - DifficultyHook Failed");
			}
		}



		private static void Run_onRunDestroyGlobal(Run run)
		{
			ResetKleptoValue();
		}

		private static void Run_onRunStartGlobal(Run run)
		{
			if (Enabled && IsNormalRun() && ZetArtifactsPlugin.HoardifactDifficulty.Value)
			{
				UpdateKleptoValue();
			}
			else
			{
				ResetKleptoValue();
			}
		}

		private static void CharacterBody_onBodyInventoryChangedGlobal(CharacterBody body)
		{
			if (body.isPlayerControlled)
			{
				if (Enabled && IsNormalRun() && ZetArtifactsPlugin.HoardifactDifficulty.Value)
				{
					RequestKleptoValueUpdate();
				}
				else
				{
					ResetKleptoValue();
				}
			}
		}

		private static bool IsNormalRun()
		{
			Run run = Run.instance;

			if (run && !(run is InfiniteTowerRun)) return true;

			return false;
		}



		private static void RequestKleptoValueUpdate()
		{
			Recalc = true;
			RecalcTimer = 0.25f;
		}

		private static void ResetKleptoValue()
		{
			Recalc = false;
			RecalcTimer = 0.25f;

			AddedElapsedTime = 0f;

			UpdateUI();
		}

		private static void UpdateKleptoValue()
		{
			if (Run.instance)
			{
				float playerCount = 0f;
				float itemScore = 0f;

				float t1 = ZetArtifactsPlugin.HoardifactT1Effect.Value;
				float t2 = ZetArtifactsPlugin.HoardifactT2Effect.Value;
				float t3 = ZetArtifactsPlugin.HoardifactT3Effect.Value;
				float boss = ZetArtifactsPlugin.HoardifactBossEffect.Value;
				float lunar = ZetArtifactsPlugin.HoardifactLunarEffect.Value;

				foreach (PlayerCharacterMasterController pcmc in PlayerCharacterMasterController.instances)
				{
					CharacterMaster master = pcmc.master;
					if (master)
					{
						Inventory inventory = master.inventory;
						if (inventory)
						{
							playerCount += 1f;

							itemScore += t1 * inventory.GetTotalItemCountOfTier(ItemTier.Tier1);
							itemScore += t1 * inventory.GetTotalItemCountOfTier(ItemTier.VoidTier1);

							itemScore += t2 * inventory.GetTotalItemCountOfTier(ItemTier.Tier2);
							itemScore += t2 * inventory.GetTotalItemCountOfTier(ItemTier.VoidTier2);

							itemScore += t3 * inventory.GetTotalItemCountOfTier(ItemTier.Tier3);
							itemScore += t3 * inventory.GetTotalItemCountOfTier(ItemTier.VoidTier3);

							itemScore += boss * inventory.GetTotalItemCountOfTier(ItemTier.Boss);
							itemScore += boss * inventory.GetTotalItemCountOfTier(ItemTier.VoidBoss);

							itemScore += lunar * inventory.GetTotalItemCountOfTier(ItemTier.Lunar);
							if (LunarVoidTier != ItemTier.AssignedAtRuntime)
							{
								itemScore += lunar * inventory.GetTotalItemCountOfTier(LunarVoidTier);
							}
						}
					}
				}

				if (playerCount > 0f && itemScore > 0f)
				{
					itemScore /= playerCount;
					AddedElapsedTime = Mathf.Max(AddedElapsedTime, itemScore * Mathf.Pow(playerCount, ZetArtifactsPlugin.HoardifactPlayerExponent.Value));
				}

				UpdateUI();
			}
		}



		private static void HUDAwakeHook()
		{
			On.RoR2.UI.HUD.Awake += (orig, self) =>
			{
				orig(self);

				InitializeUI(self);
			};
		}

		internal static void InitializeUI(HUD hud)
		{
			DisplayPanel = new GameObject("HoardifactPanel");
			RectTransform panelTransform = DisplayPanel.AddComponent<RectTransform>();

			DisplayPanel.transform.SetParent(hud.gameModeUiInstance.transform);
			DisplayPanel.transform.SetAsLastSibling();

			DisplayTimeText = new GameObject("HoardifactTimeText");
			RectTransform timeTextTransform = DisplayTimeText.AddComponent<RectTransform>();
			TimeTextMesh = DisplayTimeText.AddComponent<HGTextMeshProUGUI>();

			DisplayTimeText.transform.SetParent(DisplayPanel.transform);

			panelTransform.localPosition = new Vector3(0, 0, 0);
			panelTransform.anchorMin = new Vector2(0, 0);
			panelTransform.anchorMax = new Vector2(0, 0);
			panelTransform.localScale = Vector3.one;
			panelTransform.pivot = new Vector2(0, 1);
			panelTransform.sizeDelta = new Vector2(80, 40);
			panelTransform.anchoredPosition = new Vector2(20, 84);
			panelTransform.eulerAngles = new Vector3(0, 9f, 0);

			timeTextTransform.localPosition = Vector3.zero;
			timeTextTransform.anchorMin = Vector2.zero;
			timeTextTransform.anchorMax = Vector2.one;
			timeTextTransform.localScale = Vector3.one;
			timeTextTransform.sizeDelta = new Vector2(-12, -12);
			timeTextTransform.anchoredPosition = Vector2.zero;

			TimeTextMesh.enableAutoSizing = false;
			TimeTextMesh.fontSize = 12;
			TimeTextMesh.faceColor = new Color(0.5f, 0.65f, 0.875f);
			TimeTextMesh.alignment = TMPro.TextAlignmentOptions.MidlineRight;
			TimeTextMesh.richText = true;

			TimeTextMesh.SetText("");

			UpdateUI();
		}

		internal static void UpdateUI()
		{
			if (TimeTextMesh != null)
			{
				string text = "";

				if (AddedElapsedTime > 0f) text = "+" + FormatTimer(AddedElapsedTime);

				if (text != CurrentTimeText)
				{
					CurrentTimeText = text;

					TimeTextMesh.SetText("<mspace=7>" + text + "</mspace>");
				}
			}
		}

		private static string FormatTimer(float time)
		{
			time = Mathf.Ceil(time * 100f);

			float a, b;

			a = Mathf.Floor(time / 6000f);
			b = Mathf.Floor((time % 6000f) / 100f);

			return a + ":" + b.ToString("00");
		}



		private static void DirectorMoneyHook()
		{
			On.RoR2.SceneDirector.PopulateScene += (orig, self) =>
			{
				self.interactableCredit = Mathf.CeilToInt(self.interactableCredit * Mathf.Max(1f, 1f + ZetArtifactsPlugin.HoardifactSetupMoney.Value));

				orig(self);
			};
		}
	}
}
