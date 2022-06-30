using System;
using RoR2;
using MonoMod.Cil;

namespace TPDespair.ZetArtifacts
{
	public static class ZetEarlifact
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
			if (!ZetArtifactsPlugin.EarlifactMerge.Value)
			{
				State = ZetArtifactsPlugin.EarlifactEnable.Value;
			}
			else
			{
				State = ZetArtifactsPlugin.LoopifactEnable.Value;
			}

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETEARLIFACT_NAME", "Artifact of Sanction");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETEARLIFACT_DESC", "Monster and Interactable types can appear earlier.");

			MinimumStageHook();
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
					ZetArtifactsPlugin.LogWarn("[ZetEarlifact] - MinimumStageHook failed!");
				}
			};
		}
	}
}
