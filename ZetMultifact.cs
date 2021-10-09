using RoR2;
using R2API.Utils;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace TPDespair.ZetArtifacts
{
	public static class ZetMultifact
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
					if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetMultifact)) return true;

					return false;
				}
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.MultifactEnable.Value;
			if (state < 1) return;

			int countMult = Math.Max(2, ZetArtifactsPlugin.MultifactMultiplier.Value);

			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETMULTIFACT_NAME", "Artifact of Multitudes");
			string str;
			if (countMult == 2) str = "Double";
			else if (countMult == 3) str = "Triple";
			else if (countMult == 4) str = "Quadruple";
			else str = "x" + countMult;
			ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETMULTIFACT_DESC", str + " player count scaling.");

			PlayerCountHook();
			PlayerTriggerHook();
		}



		private delegate int RunInstanceReturnInt(Run self);
		private static RunInstanceReturnInt origLivingPlayerCountGetter;
		private static RunInstanceReturnInt origParticipatingPlayerCountGetter;

		private static int GetLivingPlayerCountHook(Run self) => origLivingPlayerCountGetter(self) * GetMultiplier();
		private static int GetParticipatingPlayerCountHook(Run self) => origParticipatingPlayerCountGetter(self) * GetMultiplier();

		private static int GetMultiplier()
		{
			if (Enabled) return Math.Max(2, ZetArtifactsPlugin.MultifactMultiplier.Value);
			return 1;
		}



		private static void PlayerCountHook()
		{
			var getLivingPlayerCountHook = new Hook(typeof(Run).GetMethodCached("get_livingPlayerCount"), typeof(ZetMultifact).GetMethodCached(nameof(GetLivingPlayerCountHook)));
			origLivingPlayerCountGetter = getLivingPlayerCountHook.GenerateTrampoline<RunInstanceReturnInt>();

			var getParticipatingPlayerCount = new Hook(typeof(Run).GetMethodCached("get_participatingPlayerCount"), typeof(ZetMultifact).GetMethodCached(nameof(GetParticipatingPlayerCountHook)));
			origParticipatingPlayerCountGetter = getParticipatingPlayerCount.GenerateTrampoline<RunInstanceReturnInt>();
		}



		private static void PlayerTriggerHook()
		{
			IL.RoR2.AllPlayersTrigger.FixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				c.GotoNext(
					x => x.MatchCallOrCallvirt<Run>("get_livingPlayerCount")
				);

				c.Index += 1;

				c.EmitDelegate<Func<int, int>>((livingPlayerCount) => {
					return livingPlayerCount / GetMultiplier();
				});
			};

			IL.RoR2.MultiBodyTrigger.FixedUpdate += (il) =>
			{
				ILCursor c = new ILCursor(il);

				c.GotoNext(
					x => x.MatchCallOrCallvirt<Run>("get_livingPlayerCount")
				);

				c.Index += 1;

				c.EmitDelegate<Func<int, int>>((livingPlayerCount) => {
					return livingPlayerCount / GetMultiplier();
				});
			};
		}
	}
}
