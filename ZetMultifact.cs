using RoR2;
using R2API.Utils;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using System;

namespace TPDespair.ZetArtifacts
{
    public static class ZetMultifact
    {
        internal static void Init()
        {
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETMULTIFACT_NAME", "Artifact of Multitudes");
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETMULTIFACT_DESC", "Double player count scaling.");

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
            if (RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetMultifact)) return 2;
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