using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;
//using R2API;
using System.Collections.Generic;

namespace TPDespair.ZetArtifacts
{
	public static class ZetLoopifact
	{
		public static CombatDirector.EliteTierDef earlyEliteDef;
		public static bool disableEarlyEliteDef = true;

		//public static EliteDef impaleElite;
		//private static bool attemptedFindImpaleDotIndex = false;
		//public static DotController.DotIndex impaleDotIndex = DotController.DotIndex.None;
		//public static bool impaleReduction = true;



		private static int state = 0;

		public static bool Enabled
		{
			get
			{
				if (state < 1) return false;
				else if (state > 1) return true;
				else
				{
					if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetLoopifact)) return true;

					return false;
				}
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.LoopifactEnable.Value;
			if (state < 1) return;

			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETLOOPIFACT_NAME", "Artifact of Escalation");
			//ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETLOOPIFACT_DESC", "Monster and interactable types can appear earlier than usual. Post-loop Elites begin to appear at monster level " + ZetArtifactsPlugin.LoopifactEliteLevel.Value + ".");
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETLOOPIFACT_DESC", "Monster and interactable types can appear earlier than usual.");

			//SceneDirector.onPostPopulateSceneServer += OnScenePopulated;
			//SceneExitController.onBeginExit += OnSceneExit;

			MinimumStageHook();
			DirectorMoneyHook();
			//AddEarlyEliteDef();
		}


		/*
		private static void OnScenePopulated(SceneDirector sceneDirector)
		{
			disableEarlyEliteDef = true;

			if (Run.instance)
			{
				//FindImpaleDotIndex();

				RebuildEliteTypeArray();
				disableEarlyEliteDef = false;
			}
		}

		private static void OnSceneExit(SceneExitController sceneExitController)
		{
			disableEarlyEliteDef = true;
		}
		*/
		/*
		private static void FindImpaleDotIndex()
		{
			if (!ZetArtifactsPlugin.PluginLoaded("com.themysticsword.elitevariety")) return;

			if (impaleDotIndex == DotController.DotIndex.None && !attemptedFindImpaleDotIndex)
			{
				BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.themysticsword.elitevariety"].Instance;
				Assembly PluginAssembly = Assembly.GetAssembly(Plugin.GetType());

				if (PluginAssembly != null)
				{
					BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

					Type type = Type.GetType("EliteVariety.Buffs.ImpPlaneImpaled, " + PluginAssembly.FullName, false);
					if (type != null)
					{
						FieldInfo indexField = type.GetField("dotIndex", Flags);
						if (indexField != null)
						{
							impaleDotIndex = (DotController.DotIndex)indexField.GetValue(type);

							Debug.LogWarning("ZetArtifact [ZetLoopifact] - Impale DotIndex : " + impaleDotIndex);
						}
						else
						{
							Debug.LogWarning("ZetArtifact [ZetLoopifact] - Could Not Find Field : ImpPlaneImpaled.dotIndex");
						}
					}
					else
					{
						Debug.LogWarning("ZetArtifact [ZetLoopifact] - Could Not Find Type : EliteVariety.Buffs.ImpPlaneImpaled");
					}
				}
				else
				{
					Debug.LogWarning("ZetArtifact [ZetLoopifact] - Could Not Find EliteVariety Assembly");
				}
			}

			attemptedFindImpaleDotIndex = true;
		}
		*/
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
				/*
				EquipmentIndex equipIndex;
				EliteDef eliteDef;
				
				if (ZetArtifactsPlugin.PluginLoaded("com.themysticsword.elitevariety"))
				{
					if (impaleElite == null)
					{
						equipIndex = EquipmentCatalog.FindEquipmentIndex("EliteVariety_AffixImpPlane");
						if (equipIndex != EquipmentIndex.None)
						{
							eliteDef = GetEquipmentEliteDef(EquipmentCatalog.GetEquipmentDef(equipIndex));
							if (eliteDef != null) impaleElite = eliteDef;
						}
					}

					if (impaleElite != null) eliteDefs.Add(impaleElite);
				}
				*/
				if (Enabled) Debug.LogWarning("ZetArtifact [ZetLoopifact] - Rebuild EarlyEliteTypeArray : " + eliteDefs.Count);
				earlyEliteDef.eliteTypes = eliteDefs.ToArray();
			}
		}
		/*
		public static EliteDef GetEquipmentEliteDef(EquipmentDef equipDef)
		{
			if (equipDef == null) return null;
			if (equipDef.passiveBuffDef == null) return null;
			return equipDef.passiveBuffDef.eliteDef;
		}
		*/


		internal static void ApplyEarlyEliteProperties()
		{
			EliteDef t2Elite = RoR2Content.Elites.Poison;

			CopyBasicAttributes(ZetArtifactsContent.Elites.PoisonEarly, RoR2Content.Elites.Poison);
			ApplyStatBoosts(ZetArtifactsContent.Elites.PoisonEarly, t2Elite);

			CopyBasicAttributes(ZetArtifactsContent.Elites.HauntedEarly, RoR2Content.Elites.Haunted);
			ApplyStatBoosts(ZetArtifactsContent.Elites.HauntedEarly, t2Elite);

			if (ZetArtifactsPlugin.PluginLoaded("com.arimah.PerfectedLoop"))
			{
				CopyBasicAttributes(ZetArtifactsContent.Elites.LunarEarly, RoR2Content.Elites.Lunar);
				ApplyStatBoosts(ZetArtifactsContent.Elites.LunarEarly, t2Elite);
			}

			Debug.LogWarning("ZetArtifact [ZetLoopifact] - ApplyEarlyEliteProperties");
		}

		private static void CopyBasicAttributes(EliteDef target, EliteDef copyFrom)
		{
			target.modifierToken = copyFrom.modifierToken;
			target.eliteEquipmentDef = copyFrom.eliteEquipmentDef;
			target.color = copyFrom.color;
			target.shaderEliteRampIndex = copyFrom.shaderEliteRampIndex;
		}

		private static void ApplyStatBoosts(EliteDef target, EliteDef copyFrom)
		{
			target.damageBoostCoefficient = Mathf.LerpUnclamped(1f, copyFrom.damageBoostCoefficient, 0.35f);
			target.healthBoostCoefficient = Mathf.LerpUnclamped(1f, copyFrom.healthBoostCoefficient, 0.35f);
		}




		private static void MinimumStageHook()
		{
			IL.RoR2.DirectorCard.IsAvailable += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchLdarg(0),
					x => x.MatchLdfld<DirectorCard>("minimumStageCompletions")
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<int, int>>((stage) =>
					{
						if (Enabled) return 0;

						return stage;
					});
				}
				else
				{
					Debug.LogWarning("ZetArtifact [ZetLoopifact] - MinimumStageHook failed!");
				}
			};
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
						if (run && run.stageClearCount < 5) value *= 1.1f;

						return value;
					});
				}
				else
				{
					Debug.LogWarning("ZetArtifact [ZetLoopifact] - DirectorMoneyHook Failed!");
				}
			};
		}

		private static void AddEarlyEliteDef()
		{
			/*
			CombatDirector.EliteTierDef[] combatDirectorEliteTiers = EliteAPI.GetCombatDirectorEliteTiers();
			CombatDirector.EliteTierDef eliteTierDef = combatDirectorEliteTiers.FirstOrDefault((CombatDirector.EliteTierDef tier) => tier.eliteTypes.Contains(RoR2Content.Elites.Poison) && tier.eliteTypes.Contains(RoR2Content.Elites.Haunted));

			if (eliteTierDef == null)
			{
				Debug.LogWarning("ZetArtifact [ZetLoopifact] - Could not find loop elites, Aborting!");
				return;
			}

			int index = Array.IndexOf(combatDirectorEliteTiers, eliteTierDef);

			Debug.LogWarning("ZetArtifact [ZetLoopifact] - DefineEliteTier : EarlyEliteDef");
			*/
			earlyEliteDef = new CombatDirector.EliteTierDef
			{
				costMultiplier = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteCostMultiplier * 6f, 0.35f),
				eliteTypes = new EliteDef[] { },
				isAvailable = ((SpawnCard.EliteRules rules) => IsEarlyEliteDefAvailable(rules)),
				canSelectWithoutAvailableEliteDef = false
			};
			/*
			Debug.LogWarning("ZetArtifact [ZetLoopifact] - AddCustomEliteTier : EarlyEliteTierDef");
			EliteAPI.AddCustomEliteTier(earlyEliteDef);
			*/
		}

		private static bool IsEarlyEliteDefAvailable(SpawnCard.EliteRules rules)
		{
			if (disableEarlyEliteDef || !Enabled) return false;

			if (!Run.instance || Run.instance.ambientLevel < ZetArtifactsPlugin.LoopifactEliteLevel.Value) return false;

			if (rules == SpawnCard.EliteRules.Lunar && CombatDirector.IsEliteOnlyArtifactActive()) return true;
			if (rules == SpawnCard.EliteRules.Default && Run.instance.loopClearCount == 0) return true;

			return false;
		}
	}
}
