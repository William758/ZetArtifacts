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



		private static void EnableEffects()
		{
			IL.RoR2.CharacterMaster.OnBodyStart += Eclipse1Hook;
			IL.RoR2.HoldoutZoneController.FixedUpdate += Eclipse2Hook;
			IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer += Eclipse3Hook;
			IL.RoR2.CharacterBody.RecalculateStats += Eclipse4Hook;
			IL.RoR2.HealthComponent.Heal += Eclipse5Hook;
			IL.RoR2.DeathRewards.OnKilledServer += Eclipse6Hook;
			IL.RoR2.CharacterBody.RecalculateStats += Eclipse7Hook;
			IL.RoR2.HealthComponent.TakeDamage += Eclipse8Hook;
		}

		private static void DisableEffects()
		{
			IL.RoR2.CharacterMaster.OnBodyStart -= Eclipse1Hook;
			IL.RoR2.HoldoutZoneController.FixedUpdate -= Eclipse2Hook;
			IL.RoR2.GlobalEventManager.OnCharacterHitGroundServer -= Eclipse3Hook;
			IL.RoR2.CharacterBody.RecalculateStats -= Eclipse4Hook;
			IL.RoR2.HealthComponent.Heal -= Eclipse5Hook;
			IL.RoR2.DeathRewards.OnKilledServer -= Eclipse6Hook;
			IL.RoR2.CharacterBody.RecalculateStats -= Eclipse7Hook;
			IL.RoR2.HealthComponent.TakeDamage -= Eclipse8Hook;
		}



		private static void OnArtifactEnabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
		{
			if (artifactDef == ZetArtifactsContent.Artifacts.ZetEclifact)
			{
				EnableEffects();
			}
		}

		private static void OnArtifactDisabled(RunArtifactManager runArtifactManager, ArtifactDef artifactDef)
		{
			if (artifactDef == ZetArtifactsContent.Artifacts.ZetEclifact)
			{
				DisableEffects();
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.EclifactEnable.Value;
			if (ZetArtifactsPlugin.PluginLoaded("com.TPDespair.DiluvianArtifact")) state = 0;
			if (state < 1) return;

			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETECLIFACT_NAME", "Artifact of the Eclipse");
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETECLIFACT_DESC", "Enables all Eclipse modifiers.\n\n<style=cStack>>Ally Starting Health: <style=cDeath>-50%</style>\n>Teleporter Radius: <style=cDeath>-50%</style>\n>Ally Fall Damage: <style=cDeath>+100% and lethal</style>\n>Enemy Speed: <style=cDeath>+40%</style>\n>Ally Healing: <style=cDeath>-50%</style>\n>Enemy Gold Drops: <style=cDeath>-20%</style>\n>Enemy Cooldowns: <style=cDeath>-50%</style>\n>Allies receive <style=cDeath>permanent damage</style></style>");

			if (state == 1)
			{
				RunArtifactManager.onArtifactEnabledGlobal += OnArtifactEnabled;
				RunArtifactManager.onArtifactDisabledGlobal += OnArtifactDisabled;
			}
			else
			{
				EnableEffects();
			}
		}



		private static void Eclipse1Hook(ILContext il)
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
					return DifficultyIndex.Eclipse1;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(3),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse1;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(1) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(1) Failed!");
			}
		}

		private static void Eclipse2Hook(ILContext il)
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
					return DifficultyIndex.Eclipse2;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(4),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse2;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(2) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(2) Failed!");
			}
		}

		private static void Eclipse3Hook(ILContext il)
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
					return DifficultyIndex.Eclipse3;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(5),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse3;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(3) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(3) Failed!");
			}
		}

		private static void Eclipse4Hook(ILContext il)
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
					return DifficultyIndex.Eclipse4;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(6),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse4;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(4) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(4) Failed!");
			}
		}

		private static void Eclipse5Hook(ILContext il)
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
					return DifficultyIndex.Eclipse5;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(7),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse5;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(5) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(5) Failed!");
			}
		}

		private static void Eclipse6Hook(ILContext il)
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
					return DifficultyIndex.Eclipse6;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(8),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse6;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(6) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(6) Failed!");
			}
		}

		private static void Eclipse7Hook(ILContext il)
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
					return DifficultyIndex.Eclipse7;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchStloc(88)
				);

				if (found)
				{
					found = c.TryGotoNext(
						x => x.MatchCall<Run>("get_instance"),
						x => x.MatchCallvirt<Run>("get_selectedDifficulty")
					);

					if (found)
					{
						int matchIndex = c.Index;

						found = c.TryGotoNext(
							x => x.MatchLdcI4(9),
							x => x.MatchBlt(out _)
						);

						if (found)
						{
							int offset = c.Index - matchIndex;

							if (offset <= 8)
							{
								c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
								{
									return DifficultyIndex.Eclipse7;
								});
							}
							else
							{
								Debug.LogWarning("EclipseHook(7) Failed! - LdcI4 Offset [" + offset + "]");
							}
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(7) Failed!");
			}
		}

		private static void Eclipse8Hook(ILContext il)
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
					return DifficultyIndex.Eclipse8;
				});
			}
			else
			{
				found = c.TryGotoNext(
					x => x.MatchCall<Run>("get_instance"),
					x => x.MatchCallvirt<Run>("get_selectedDifficulty")
				);

				if (found)
				{
					int matchIndex = c.Index;

					found = c.TryGotoNext(
						x => x.MatchLdcI4(10),
						x => x.MatchBlt(out _)
					);

					if (found)
					{
						int offset = c.Index - matchIndex;

						if (offset <= 8)
						{
							c.EmitDelegate<Func<DifficultyIndex, DifficultyIndex>>((diffIndex) =>
							{
								return DifficultyIndex.Eclipse8;
							});
						}
						else
						{
							Debug.LogWarning("EclipseHook(8) Failed! - LdcI4 Offset [" + offset + "]");
						}
					}
				}
			}

			if (!found)
			{
				Debug.LogWarning("EclipseHook(8) Failed!");
			}
		}
	}
}
