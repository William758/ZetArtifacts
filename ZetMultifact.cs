using RoR2;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;
using System.Reflection;
using Mono.Cecil.Cil;
using UnityEngine;

namespace TPDespair.ZetArtifacts
{
	public static class ZetMultifact
	{
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
			State = ZetArtifactsPlugin.MultifactEnable.Value;

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETMULTIFACT_NAME", "Artifact of Multitudes");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETMULTIFACT_DESC", GetDescription());

			get_LPC_Method = typeof(Run).GetMethod("get_livingPlayerCount", flags);
			get_PPC_Method = typeof(Run).GetMethod("get_participatingPlayerCount", flags);

			GLPCH_Method = typeof(ZetMultifact).GetMethod(nameof(GetLivingPlayerCountHook), flags);
			GPPCH_Method = typeof(ZetMultifact).GetMethod(nameof(GetParticipatingPlayerCountHook), flags);

			// sometimes interactable cost would not reflect increased count
			SceneDirector.onPrePopulateSceneServer += UpdateDifficultyCoeff_Scene;
			appliedSceneCostFix = true;
			// combat shrines seem to have trouble selecting a spawn card if they are given too many credits ???
			On.RoR2.ShrineCombatBehavior.Start += ShrineCombatBehavior_Start;
			appliedCombatShrineFix = true;

			GoldFromKillHook();

			PlayerCountHook();
			PlayerTriggerHook();
		}



		private static string GetDescription()
		{
			string text;

			int countMult = Math.Max(2, ZetArtifactsPlugin.MultifactMultiplier.Value);

			if (countMult == 2) text = "Double";
			else if (countMult == 3) text = "Triple";
			else if (countMult == 4) text = "Quadruple";
			else text = "x" + countMult;

			text += " player count scaling.";

			if (ZetArtifactsPlugin.HoardifactMerge.Value)
			{
				if (ZetArtifactsPlugin.HoardifactDifficulty.Value)
				{
					text += "\nCollected items increase difficulty.";
				}

				if (ZetArtifactsPlugin.HoardifactSetupMoney.Value > 0.095f)
				{
					text += "\nInteractable spawns are increased.";
				}
			}

			return text;
		}



		private static void ShrineCombatBehavior_Start(On.RoR2.ShrineCombatBehavior.orig_Start orig, ShrineCombatBehavior self)
		{
			if (self.combatDirector)
			{
				float mult = Mathf.Clamp(0.7f + (0.3f * GetMultiplier()), 1f, 3f);
				self.combatDirector.maximumNumberToSpawnBeforeSkipping = Mathf.CeilToInt(self.combatDirector.maximumNumberToSpawnBeforeSkipping * mult);
				//ZetArtifactsPlugin.LogWarn("CombatShrine : CombatDirector.maximumNumberToSpawnBeforeSkipping : " + self.combatDirector.maximumNumberToSpawnBeforeSkipping);
			}

			orig(self);
		}

		private delegate int RunInstanceReturnInt(Run self);
		private static RunInstanceReturnInt origLivingPlayerCountGetter;
		private static RunInstanceReturnInt origParticipatingPlayerCountGetter;

		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

		private static MethodInfo get_LPC_Method;
		private static MethodInfo get_PPC_Method;

		private static MethodInfo GLPCH_Method;
		private static MethodInfo GPPCH_Method;

		private static int GetLivingPlayerCountHook(Run self) => origLivingPlayerCountGetter(self) * GetMultiplier();
		private static int GetParticipatingPlayerCountHook(Run self) => origParticipatingPlayerCountGetter(self) * GetMultiplier();

		public static bool appliedSceneCostFix = false;
		public static bool appliedCombatShrineFix = false;

		public static int GetMultiplier()
		{
			if (Enabled) return Math.Max(2, ZetArtifactsPlugin.MultifactMultiplier.Value);
			return 1;
		}



		private static void UpdateDifficultyCoeff_Scene(SceneDirector obj)
		{
			if (Run.instance)
			{
				ZetArtifactsPlugin.LogInfo("[ZetMultifact] - Forcing DifficultyCoeff Calc : PreScenePop");
				Run.instance.RecalculateDifficultyCoefficentInternal();
			}
		}



		private static void GoldFromKillHook()
		{
			IL.RoR2.DeathRewards.OnKilledServer += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchLdarg(0),
					x => x.MatchCallOrCallvirt<DeathRewards>("get_goldReward"),
					x => x.MatchStloc(2)
				);

				if (found)
				{
					c.Index += 3;

					c.Emit(OpCodes.Ldloc, 2);
					c.EmitDelegate<Func<uint, uint>>((reward) =>
					{
						int playerCount = Run.instance.participatingPlayerCount;
						int realPlayerCount = PlayerCharacterMasterController.instances.Count;

						if (playerCount != realPlayerCount)
						{
							if (reward > 1u)
							{
								int multiplier = playerCount / realPlayerCount;

								reward = (uint)(Mathf.Max(reward, playerCount * Mathf.CeilToInt(Mathf.Sqrt(multiplier * 2f))));
							}
						}

						return reward;
					});
					c.Emit(OpCodes.Stloc, 2);
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetMultifact] - GoldFromKillHook Failed!");
				}
			};
		}



		private static void PlayerCountHook()
		{
			var getLivingPlayerCountHook = new Hook(get_LPC_Method, GLPCH_Method);
			origLivingPlayerCountGetter = getLivingPlayerCountHook.GenerateTrampoline<RunInstanceReturnInt>();

			var getParticipatingPlayerCount = new Hook(get_PPC_Method, GPPCH_Method);
			origParticipatingPlayerCountGetter = getParticipatingPlayerCount.GenerateTrampoline<RunInstanceReturnInt>();
		}



		private static void PlayerTriggerHook()
		{
			IL.RoR2.AllPlayersTrigger.FixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCallOrCallvirt<Run>("get_livingPlayerCount")
				);

				if (found)
				{
					c.Index += 1;

					c.EmitDelegate<Func<int, int>>((livingPlayerCount) =>
					{
						return livingPlayerCount / GetMultiplier();
					});
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetMultifact] - PlayerTriggerHook:AllPlayersTrigger Failed!");
				}
			};

			IL.RoR2.MultiBodyTrigger.FixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCallOrCallvirt<Run>("get_livingPlayerCount")
				);

				if (found)
				{
					c.Index += 1;

					c.EmitDelegate<Func<int, int>>((livingPlayerCount) =>
					{
						return livingPlayerCount / GetMultiplier();
					});
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetMultifact] - PlayerTriggerHook:MultiBodyTrigger Failed!");
				}
			};
		}
	}
}
