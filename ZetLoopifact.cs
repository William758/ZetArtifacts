using System;
using System.Collections.Generic;
using System.Linq;
using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using R2API;

namespace TPDespair.ZetArtifacts
{
	public static class ZetLoopifact
	{
		public static CombatDirector.EliteTierDef earlyEliteDef;
		public static bool disableEarlyEliteDef = true;

		private static bool AragonFinalized = false;
		private static bool BlightedFinalized = false;

		private static List<EquipmentDef> ReplaceableEliteTypes = new List<EquipmentDef>();
		private static int EliteModCount = 0;

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
			State = ZetArtifactsPlugin.LoopifactEnable.Value;
			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETLOOPIFACT_NAME", "Artifact of Escalation");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETLOOPIFACT_DESC", GetDescription());

			SceneDirector.onPrePopulateSceneServer += OnPreScenePopulated;
			SceneDirector.onPostPopulateSceneServer += OnPostScenePopulated;
			SceneExitController.onBeginExit += OnSceneExit;
			Run.onRunDestroyGlobal += OnRunDestroyed;

			AddEarlyEliteDef();
			DirectorMoneyHook();
			OverrideEliteDefHook();

			On.RoR2.EliteCatalog.Init += EliteCatalogInitHook;
		}

		internal static void LateSetup()
		{
			if (State < 1) return;

			BuildReplaceableElites();
			FinalizeEliteProperties();
		}



		private static string GetDescription()
		{
			string text = "";

			if (ZetArtifactsPlugin.EarlifactMerge.Value)
			{
				text += "Monster and Interactable types can appear earlier.";
			}

			if (ZetArtifactsPlugin.LoopifactEliteLevel.Value > 1)
			{
				if (text != "") text += "\n";
				text += "Post-loop Elites begin to appear at ambient level " + ZetArtifactsPlugin.LoopifactEliteLevel.Value + ".";
			}
			else if (ZetArtifactsPlugin.LoopifactEliteLevel.Value > -1)
			{
				if (text != "") text += "\n";
				text += "Monsters can spawn as Post-loop Elites.";
			}

			if (ZetArtifactsPlugin.LoopifactCombatMoney.Value > 0.095f)
			{
				if (text != "") text += "\n";
				text += "Monster spawns are increased.";
			}

			return text;
		}



		private static void OnPreScenePopulated(SceneDirector director)
		{
			disableEarlyEliteDef = true;
		}

		private static void OnPostScenePopulated(SceneDirector director)
		{
			disableEarlyEliteDef = true;

			if (Run.instance)
			{
				RebuildEliteTypeArray();

				if (ZetArtifactsPlugin.LoopifactEliteLevel.Value >= 0)
				{
					disableEarlyEliteDef = false;
				}
			}
		}

		private static void OnSceneExit(SceneExitController controller)
		{
			disableEarlyEliteDef = true;
		}

		private static void OnRunDestroyed(Run run)
		{
			disableEarlyEliteDef = true;
		}



		private static void RebuildEliteTypeArray()
		{
			if (earlyEliteDef != null)
			{
				SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
				string sceneName = sceneDefForCurrentScene ? sceneDefForCurrentScene.baseSceneName : "";

				List<EliteDef> eliteDefs = new List<EliteDef>
				{
					ZetArtifactsContent.Elites.HauntedEarly,
					ZetArtifactsContent.Elites.PoisonEarly
				};

				if (sceneName != "moon2" && ZetArtifactsPlugin.PluginLoaded("com.arimah.PerfectedLoop"))
				{
					eliteDefs.Add(ZetArtifactsContent.Elites.LunarEarly);
				}

				if (AragonFinalized)
				{
					eliteDefs.Add(ZetArtifactsContent.Elites.AragonEarly);
				}

				if (BlightedFinalized)
				{
					eliteDefs.Add(ZetArtifactsContent.Elites.BlightedEarly);
				}

				if (Enabled) ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - Rebuild EarlyEliteTypeArray : " + eliteDefs.Count);

				earlyEliteDef.eliteTypes = eliteDefs.ToArray();
			}
		}



		private static void AddEarlyEliteDef()
		{
			float cost = CombatDirector.baseEliteCostMultiplier;
			if (ZetArtifactsPlugin.LoopifactEliteClassic.Value)
			{
				cost = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteCostMultiplier * 6f, 0.35f);
			}

			earlyEliteDef = new CombatDirector.EliteTierDef
			{
				costMultiplier = cost,
				eliteTypes = new EliteDef[] { },
				isAvailable = (SpawnCard.EliteRules rules) => IsEarlyEliteDefAvailable(rules),
				canSelectWithoutAvailableEliteDef = false
			};

			ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - AddCustomEliteTier : EarlyEliteTierDef");
			EliteAPI.AddCustomEliteTier(earlyEliteDef);
		}

		private static bool IsEarlyEliteDefAvailable(SpawnCard.EliteRules rules)
		{
			if (disableEarlyEliteDef || !Enabled) return false;

			if (!Run.instance || Run.instance.ambientLevel < ZetArtifactsPlugin.LoopifactEliteLevel.Value) return false;

			if (rules == SpawnCard.EliteRules.Lunar && CombatDirector.IsEliteOnlyArtifactActive()) return true;
			if (rules == SpawnCard.EliteRules.Default && Run.instance.loopClearCount == 0) return true;

			return false;
		}



		private static void DirectorMoneyHook()
		{
			IL.RoR2.CombatDirector.Awake += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchStfld("RoR2.CombatDirector/DirectorMoneyWave", "multiplier")
				);

				if (found)
				{
					c.EmitDelegate<Func<float, float>>((value) =>
					{
						if (!Enabled) return value;

						Run run = Run.instance;
						if (run && run.loopClearCount < 2)
						{
							value *= Mathf.Max(1f, 1f + ZetArtifactsPlugin.LoopifactCombatMoney.Value);
						}

						return value;
					});
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - DirectorMoneyHook Failed!");
				}
			};
		}

		private static void OverrideEliteDefHook()
		{
			IL.RoR2.CombatDirector.AttemptSpawnOnTarget += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchStloc(4)
				);

				if (found)
				{
					c.Index += 1;

					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, 3);
					c.EmitDelegate<Func<CombatDirector, EliteDef, EliteDef>>((director, eliteDef) =>
					{
						if (!Enabled) return eliteDef;

						if (eliteDef == null) return eliteDef;

						if (RollReplaceEliteDef(eliteDef.eliteEquipmentDef))
						{
							if (IsEarlyEliteDefAvailable(director.currentMonsterCard.spawnCard.eliteRules))
							{
								EliteDef result = GetRandomEarlyEliteDef(director.rng);
								if (result != null)
								{
									return result;
								}
							}
						}

						return eliteDef;
					});
					c.Emit(OpCodes.Stloc, 3);
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - OverrideEliteDefHook Failed!");
				}
			};
		}

		private static bool RollReplaceEliteDef(EquipmentDef equip)
		{
			if (ReplaceableEliteTypes.Contains(equip))
			{
				float chance = ZetArtifactsPlugin.LoopifactEliteReplacementChance.Value;

				if (chance >= 1f) return true;
				if (chance <= 0f) return false;

				if (EliteModCount > 0)
				{
					float modExponent = 1f + (EliteModCount * ZetArtifactsPlugin.LoopifactEliteReplacementFactor.Value);

					if (modExponent > 1f) chance = 1f - Mathf.Pow(1f - chance, modExponent);
				}

				return UnityEngine.Random.value <= chance;
			}

			return false;
		}

		private static EliteDef GetRandomEarlyEliteDef(Xoroshiro128Plus rng)
		{
			List<EliteDef> elites = earlyEliteDef.eliteTypes.ToList();

			if (elites.Count > 0) return rng.NextElementUniform(elites);

			return null;
		}



		private static void EliteCatalogInitHook(On.RoR2.EliteCatalog.orig_Init orig)
		{
			orig();

			ApplyEarlyEliteProperties();
		}

		private static void ApplyEarlyEliteProperties()
		{
			CopyEliteEquipment(ZetArtifactsContent.Elites.PoisonEarly, RoR2Content.Elites.Poison);
			CopyBasicAttributes(ZetArtifactsContent.Elites.PoisonEarly, RoR2Content.Elites.Poison);
			ApplyStatBoosts(ZetArtifactsContent.Elites.PoisonEarly);

			CopyEliteEquipment(ZetArtifactsContent.Elites.HauntedEarly, RoR2Content.Elites.Haunted);
			CopyBasicAttributes(ZetArtifactsContent.Elites.HauntedEarly, RoR2Content.Elites.Haunted);
			ApplyStatBoosts(ZetArtifactsContent.Elites.HauntedEarly);

			CopyEliteEquipment(ZetArtifactsContent.Elites.LunarEarly, RoR2Content.Elites.Lunar);
			CopyBasicAttributes(ZetArtifactsContent.Elites.LunarEarly, RoR2Content.Elites.Lunar);
			ApplyStatBoosts(ZetArtifactsContent.Elites.LunarEarly);

			CopyEliteEquipment(ZetArtifactsContent.Elites.AragonEarly, RoR2Content.Elites.Poison);
			CopyBasicAttributes(ZetArtifactsContent.Elites.AragonEarly, RoR2Content.Elites.Poison);
			ApplyStatBoosts(ZetArtifactsContent.Elites.AragonEarly);

			CopyEliteEquipment(ZetArtifactsContent.Elites.BlightedEarly, RoR2Content.Elites.Haunted);
			CopyBasicAttributes(ZetArtifactsContent.Elites.BlightedEarly, RoR2Content.Elites.Haunted);
			ApplyStatBoosts(ZetArtifactsContent.Elites.BlightedEarly);

			ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - ApplyEarlyEliteProperties");
		}

		private static void BuildReplaceableElites()
		{
			ReplaceableEliteTypes.Clear();
			EliteModCount = 0;

			List<string> equipNames = new List<string> { "EliteFireEquipment", "EliteIceEquipment", "EliteLightningEquipment", "EliteEarthEquipment" };
			HandleReplaceableEliteSet(equipNames, false);

			if (ZetArtifactsPlugin.PluginLoaded("com.plasmacore.PlasmaCoreSpikestripContent"))
			{
				equipNames = new List<string> { "EQUIPMENT_AFFIXPLATED", "EQUIPMENT_AFFIXWARPED", "EQUIPMENT_AFFIXVEILED" };
				HandleReplaceableEliteSet(equipNames);
			}

			if (ZetArtifactsPlugin.PluginLoaded("com.KomradeSpectre.Aetherium"))
			{
				equipNames = new List<string> { "AETHERIUM_ELITE_EQUIPMENT_AFFIX_SANGUINE" };
				HandleReplaceableEliteSet(equipNames);
			}

			if (ZetArtifactsPlugin.PluginLoaded("com.PopcornFactory.WispMod"))
			{
				equipNames = new List<string> { "WARFRAMEWISP_ELITE_EQUIPMENT_AFFIX_NULLIFIER" };
				HandleReplaceableEliteSet(equipNames);
			}

			ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - Replaceable Elite Types : " + ReplaceableEliteTypes.Count );
		}

		private static void HandleReplaceableEliteSet(List<string> equipNames, bool count = true)
		{
			bool foundAny = false;

			foreach (string equipName in equipNames)
			{
				EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(equipName);
				if (index != EquipmentIndex.None)
				{
					EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(index);
					if (equip)
					{
						foundAny = true;

						if (!ReplaceableEliteTypes.Contains(equip))
						{
							ReplaceableEliteTypes.Add(equip);
						}
					}
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - Failed to set elite as replaceable! Could not find equipment : " + equipName);
				}
			}

			if (count && foundAny) EliteModCount += 1;
		}

		private static void FinalizeEliteProperties()
		{
			if (ZetArtifactsPlugin.PluginLoaded("com.plasmacore.PlasmaCoreSpikestripContent"))
			{
				if (FinalizeElite(ZetArtifactsContent.Elites.AragonEarly, "EQUIPMENT_AFFIXARAGONITE"))
				{
					AragonFinalized = true;
					ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - Added support for Aragonite Elites!");
				}
			}

			if (ZetArtifactsPlugin.PluginLoaded("com.Moffein.BlightedElites"))
			{
				if (FinalizeElite(ZetArtifactsContent.Elites.BlightedEarly, "AffixBlightedMoffein"))
				{
					BlightedFinalized = true;
					ZetArtifactsPlugin.LogInfo("[ZetLoopifact] - Added support for Blighted Elites!");
				}
			}
		}

		private static bool FinalizeElite(EliteDef targetElite, string equipmentName)
		{
			EquipmentIndex index = EquipmentCatalog.FindEquipmentIndex(equipmentName);
			if (index != EquipmentIndex.None)
			{
				EquipmentDef equip = EquipmentCatalog.GetEquipmentDef(index);
				if (equip)
				{
					EliteDef eliteDef = GetFirstEliteDefWithEquipment(equip);
					if (eliteDef)
					{
						CopyEliteEquipment(targetElite, eliteDef);
						CopyBasicAttributes(targetElite, eliteDef);

						return true;
					}
					else
					{
						ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - Failed to finalize elite! Could not find an eliteDef using equipment : " + equipmentName);
					}
				}
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - Failed to finalize elite! Could not find equipment : " + equipmentName);
			}

			return false;
		}

		private static EliteDef GetFirstEliteDefWithEquipment(EquipmentDef equip)
		{
			foreach (EliteIndex eliteIndex in EliteCatalog.eliteList)
			{
				EliteDef eliteDef = EliteCatalog.GetEliteDef(eliteIndex);
				if (eliteDef && eliteDef.eliteEquipmentDef == equip)
				{
					return eliteDef;
				}
			}

			return null;
		}

		private static void CopyEliteEquipment(EliteDef target, EliteDef copyFrom)
		{
			if (copyFrom.eliteEquipmentDef != null)
			{
				target.eliteEquipmentDef = copyFrom.eliteEquipmentDef;
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetLoopifact] - CopyEliteEquipment failed to copy from : " + copyFrom);
			}
		}

		private static void CopyBasicAttributes(EliteDef target, EliteDef copyFrom)
		{
			target.modifierToken = copyFrom.modifierToken;
			target.color = copyFrom.color;
			target.shaderEliteRampIndex = copyFrom.shaderEliteRampIndex;
		}

		private static void ApplyStatBoosts(EliteDef target)
		{
			if (ZetArtifactsPlugin.LoopifactEliteClassic.Value)
			{
				EliteDef t2Elite = RoR2Content.Elites.Poison;

				target.damageBoostCoefficient = Mathf.LerpUnclamped(1f, t2Elite.damageBoostCoefficient, 0.35f);
				target.healthBoostCoefficient = Mathf.LerpUnclamped(1f, t2Elite.healthBoostCoefficient, 0.35f);
			}
			else
			{
				EliteDef t1Elite = RoR2Content.Elites.Fire;

				target.damageBoostCoefficient = t1Elite.damageBoostCoefficient;
				target.healthBoostCoefficient = t1Elite.healthBoostCoefficient;
			}
		}
	}
}
