using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;
using R2API;
using System.Linq;
using System.Collections.Generic;

namespace TPDespair.ZetArtifacts
{
	public static class ZetLoopifact
	{
		public static CombatDirector.EliteTierDef EarlyEliteDef;
		public static bool DisableEarlyEliteDef = true;

		public static EliteDef ImpaleElite;



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
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETLOOPIFACT_DESC", "Monsters, interactables and elites can appear earlier than usual.");

			SceneDirector.onPostPopulateSceneServer += OnScenePopulated;
			SceneExitController.onBeginExit += OnSceneExit;

			MinimumStageHook();
			DirectorMoneyHook();
			AddEarlyEliteDef();
		}



		private static void OnScenePopulated(SceneDirector sceneDirector)
		{
			if (Run.instance)
			{
				RebuildEliteTypeArray();
				DisableEarlyEliteDef = false;
			}
		}

		private static void OnSceneExit(SceneExitController sceneExitController)
		{
			if (Run.instance)
			{
				DisableEarlyEliteDef = true;
			}
		}

		private static void RebuildEliteTypeArray()
		{
			if (EarlyEliteDef != null)
			{
				SceneDef sceneDefForCurrentScene = SceneCatalog.GetSceneDefForCurrentScene();
				string sceneName = sceneDefForCurrentScene ? sceneDefForCurrentScene.baseSceneName : "";

				List<EliteDef> eliteDefs = new List<EliteDef>
                {
                    RoR2Content.Elites.Haunted,
                    RoR2Content.Elites.Poison
                };

				if (sceneName != "moon2" && ZetArtifactsPlugin.PluginLoaded("com.arimah.PerfectedLoop"))
				{
					eliteDefs.Add(RoR2Content.Elites.Lunar);
				}

				EquipmentIndex equipIndex;
				EliteDef eliteDef;

				if (ZetArtifactsPlugin.PluginLoaded("com.themysticsword.elitevariety"))
				{
					if (ImpaleElite == null)
					{
						equipIndex = EquipmentCatalog.FindEquipmentIndex("EliteVariety_AffixImpPlane");
						if (equipIndex != EquipmentIndex.None)
						{
							eliteDef = GetEquipmentEliteDef(EquipmentCatalog.GetEquipmentDef(equipIndex));
							if (eliteDef != null) ImpaleElite = eliteDef;
						}
					}

					if (ImpaleElite != null) eliteDefs.Add(ImpaleElite);
				}

				if (Enabled) Debug.LogWarning("ZetArtifact [ZetLoopifact] - RebuildEliteTypeArray : " + eliteDefs.Count);
				EarlyEliteDef.eliteTypes = eliteDefs.ToArray();
			}
		}

		public static EliteDef GetEquipmentEliteDef(EquipmentDef equipDef)
		{
			if (equipDef == null) return null;
			if (equipDef.passiveBuffDef == null) return null;
			return equipDef.passiveBuffDef.eliteDef;
		}



		private static void MinimumStageHook()
		{
			IL.RoR2.DirectorCard.CardIsValid += (il) =>
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
			CombatDirector.EliteTierDef[] combatDirectorEliteTiers = EliteAPI.GetCombatDirectorEliteTiers();
			CombatDirector.EliteTierDef eliteTierDef = combatDirectorEliteTiers.FirstOrDefault((CombatDirector.EliteTierDef tier) => tier.eliteTypes.Contains(RoR2Content.Elites.Poison) && tier.eliteTypes.Contains(RoR2Content.Elites.Haunted));
			
			if (eliteTierDef == null)
			{
				Debug.LogWarning("ZetArtifact [ZetLoopifact] - Could not find loop elites, Aborting!");
				return;
			}

			int index = Array.IndexOf(combatDirectorEliteTiers, eliteTierDef);

			Debug.LogWarning("ZetArtifact [ZetLoopifact] - DefineEliteTier : EarlyEliteDef");

			EarlyEliteDef = new CombatDirector.EliteTierDef();
			EarlyEliteDef.costMultiplier = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteCostMultiplier * 6f, 0.35f);
			EarlyEliteDef.damageBoostCoefficient = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteDamageBoostCoefficient * 3f, 0.35f);
			EarlyEliteDef.healthBoostCoefficient = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteHealthBoostCoefficient * 4.5f, 0.35f);
			EarlyEliteDef.eliteTypes = new EliteDef[]
			{
				RoR2Content.Elites.Poison,
				RoR2Content.Elites.Haunted
			};
			EarlyEliteDef.isAvailable = ((SpawnCard.EliteRules rules) => IsEarlyEliteDefAvailable(rules));

			Debug.LogWarning("ZetArtifact [ZetLoopifact] - AddCustomEliteTier : EarlyEliteDef");
			EliteAPI.AddCustomEliteTier(EarlyEliteDef, index);
		}

		private static bool IsEarlyEliteDefAvailable(SpawnCard.EliteRules rules)
		{
			if (DisableEarlyEliteDef || !Enabled) return false;

			if (rules == SpawnCard.EliteRules.Lunar && CombatDirector.IsEliteOnlyArtifactActive()) return true;
			if (rules == SpawnCard.EliteRules.Default && Run.instance.loopClearCount == 0) return true;

			return false;
		}
	}
}
