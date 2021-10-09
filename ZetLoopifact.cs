using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;
using R2API;
using System.Linq;
using System.Collections.Generic;
using BepInEx;
using System.Reflection;

namespace TPDespair.ZetArtifacts
{
	public static class ZetLoopifact
	{
		public static CombatDirector.EliteTierDef earlyEliteDef;
		public static bool disableEarlyEliteDef = true;

		public static EliteDef impaleElite;
		private static bool attemptedFindImpaleDotIndex = false;
		public static DotController.DotIndex impaleDotIndex = DotController.DotIndex.None;
		public static bool impaleReduction = true;



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
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETLOOPIFACT_DESC", "Monster and interactable types can appear earlier than usual. Post-loop Elites begin to appear at monster level " + ZetArtifactsPlugin.LoopifactEliteLevel.Value + ".");

			SceneDirector.onPostPopulateSceneServer += OnScenePopulated;
			SceneExitController.onBeginExit += OnSceneExit;

			ModifyImpale();

			MinimumStageHook();
			DirectorMoneyHook();
			AddEarlyEliteDef();
		}



		private static void OnScenePopulated(SceneDirector sceneDirector)
		{
			disableEarlyEliteDef = true;

			if (Run.instance)
			{
				FindImpaleDotIndex();

				RebuildEliteTypeArray();
				disableEarlyEliteDef = false;
			}
		}

		private static void OnSceneExit(SceneExitController sceneExitController)
		{
			disableEarlyEliteDef = true;
		}

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
					if (type != null) {
						FieldInfo indexField = type.GetField("dotIndex", Flags);
						impaleDotIndex = (DotController.DotIndex)indexField.GetValue(type);

						Debug.LogWarning("ZetArtifact [ZetLoopifact] - Impale DotIndex : " + impaleDotIndex);
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

		private static void RebuildEliteTypeArray()
		{
			if (earlyEliteDef != null)
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

				if (Enabled) Debug.LogWarning("ZetArtifact [ZetLoopifact] - RebuildEliteTypeArray : " + eliteDefs.Count);
				earlyEliteDef.eliteTypes = eliteDefs.ToArray();
			}
		}

		public static EliteDef GetEquipmentEliteDef(EquipmentDef equipDef)
		{
			if (equipDef == null) return null;
			if (equipDef.passiveBuffDef == null) return null;
			return equipDef.passiveBuffDef.eliteDef;
		}



		private static void ModifyImpale()
		{
			On.RoR2.DotController.InflictDot_GameObject_GameObject_DotIndex_float_float += (orig, attacker, victim, index, duration, damage) =>
			{
				if (impaleDotIndex != DotController.DotIndex.None && index == impaleDotIndex)
				{
					if (Run.instance && ZetArtifactsPlugin.LoopifactScaleImpale.Value && impaleReduction)
					{
						bool isPlayer = false;
						CharacterBody atkBody = attacker.GetComponent<CharacterBody>();
						if (atkBody && atkBody.teamComponent.teamIndex == TeamIndex.Player) isPlayer = true;

						float mult = Mathf.Clamp(Run.instance.ambientLevel / 90f, 0.05f, 1f);
						if (!isPlayer)
						{
							damage *= mult;
							duration *= mult;
						}
						else
						{
							mult = 1 - mult;
							mult *= mult;
							mult = 1 - mult;

							damage *= mult;
							duration *= mult;
						}

						duration = Mathf.Ceil(duration / 5f) * 5f + 0.1f;
					}
				}

				orig(attacker, victim, index, duration, damage);
			};
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

            earlyEliteDef = new CombatDirector.EliteTierDef
            {
                costMultiplier = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteCostMultiplier * 6f, 0.35f),
                damageBoostCoefficient = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteDamageBoostCoefficient * 3f, 0.35f),
                healthBoostCoefficient = Mathf.LerpUnclamped(1f, CombatDirector.baseEliteHealthBoostCoefficient * 4.5f, 0.35f),
                eliteTypes = new EliteDef[] { },
                isAvailable = ((SpawnCard.EliteRules rules) => IsEarlyEliteDefAvailable(rules))
            };

            Debug.LogWarning("ZetArtifact [ZetLoopifact] - AddCustomEliteTier : EarlyEliteDef");
			EliteAPI.AddCustomEliteTier(earlyEliteDef, index);
		}

		private static bool IsEarlyEliteDefAvailable(SpawnCard.EliteRules rules)
		{
			if (disableEarlyEliteDef || !Enabled) return false;

			if(!Run.instance || Run.instance.ambientLevel < ZetArtifactsPlugin.LoopifactEliteLevel.Value) return false;

			if (rules == SpawnCard.EliteRules.Lunar && CombatDirector.IsEliteOnlyArtifactActive()) return true;
			if (rules == SpawnCard.EliteRules.Default && Run.instance.loopClearCount == 0) return true;

			return false;
		}
	}
}
