using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;

namespace TPDespair.ZetArtifacts
{
	public static class ZetEclifact
	{
		private static int state = 0;

		public static bool Enabled
		{
			get
			{
				if (state < 1) return false;
				else if (state > 1) return true;
				else
				{
					if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetEclifact)) return true;

					return false;
				}
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.EclifactEnable.Value;
			if (ZetArtifactsPlugin.PluginLoaded("com.TPDespair.DiluvianArtifact")) state = 0;
			if (state < 1) return;

			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETECLIFACT_NAME", "Artifact of the Eclipse");
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETECLIFACT_DESC", "Enables all Eclipse modifiers.\n\n<style=cStack>>Ally Starting Health: <style=cDeath>-50%</style>\n>Teleporter Radius: <style=cDeath>-50%</style>\n>Ally Fall Damage: <style=cDeath>+100% and lethal</style>\n>Enemy Speed: <style=cDeath>+40%</style>\n>Ally Healing: <style=cDeath>-50%</style>\n>Enemy Gold Drops: <style=cDeath>-20%</style>\n>Enemy Cooldowns: <style=cDeath>-50%</style>\n>Allies receive <style=cDeath>permanent damage</style></style>");

			Eclipse1Hook();
			Eclipse2Hook();
			Eclipse3Hook();
			Eclipse4Hook();
			Eclipse5Hook();
			Eclipse6Hook();
			Eclipse7Hook();
			Eclipse8Hook();
		}



		private static void Eclipse1Hook()
		{
			IL.RoR2.CharacterMaster.OnBodyStart += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(3)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse1;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse1Hook Failed");
				}
			};
		}

		private static void Eclipse2Hook()
		{
			IL.RoR2.HoldoutZoneController.FixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(4)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse2;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse2Hook Failed");
				}
			};
		}

		private static void Eclipse3Hook()
		{
			IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(5)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse3;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse3Hook Failed");
				}
			};
		}

		private static void Eclipse4Hook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(6)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse4;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse4Hook Failed");
				}
			};
		}

		private static void Eclipse5Hook()
		{
			IL.RoR2.HealthComponent.Heal += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(7)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse5;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse5Hook Failed");
				}
			};
		}

		private static void Eclipse6Hook()
		{
			IL.RoR2.DeathRewards.OnKilledServer += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(8)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse6;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse6Hook Failed");
				}
			};
		}

		private static void Eclipse7Hook()
		{
			IL.RoR2.CharacterBody.RecalculateStats += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(9)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse7;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse7Hook Failed");
				}
			};
		}

		private static void Eclipse8Hook()
		{
			IL.RoR2.HealthComponent.TakeDamage += (il) =>
			{
				ILCursor c = new ILCursor(il);

				bool found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty"),
					x => x.MatchLdcI4(10)
				);

				if (found)
				{
					c.Index += 2;

					c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
					{
						if (Enabled) return DifficultyIndex.Eclipse8;

						return diffIndex;
					});
				}
				else
				{
					Debug.LogWarning("Eclipse8Hook Failed");
				}
			};
		}
	}
}
