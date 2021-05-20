using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace TPDespair.ZetArtifacts
{
    public static class ZetRevivifact
    {
        internal static void Init()
        {
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETREVIVIFACT_NAME", "Artifact of Revival");
            ZetArtifactsPlugin.RegisterLanguageToken("ARTIFACT_ZETREVIVIFACT_DESC", "Dead players respawn after the boss is defeated.");

            SaveDeathPositionHook();
            RevivalHook();
        }



        private static void SaveDeathPositionHook()
        {
            On.RoR2.CharacterMaster.OnBodyDeath += (orig, self, body) =>
            {
                if (NetworkServer.active)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetRevivifact))
                    {
                        self.transform.position = body.footPosition;
                        self.transform.rotation = body.transform.rotation;
                    }
                }

                orig(self, body);
            };
        }

        private static void RevivalHook()
        {
            On.RoR2.Run.OnServerBossDefeated += (orig, self, bossGroup) =>
            {
                orig(self, bossGroup);

                if (NetworkServer.active)
                {
                    if (RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetRevivifact))
                    {
                        foreach (NetworkUser networkUser in NetworkUser.readOnlyInstancesList)
                        {
                            if (networkUser.isActiveAndEnabled)
                            {
                                CharacterMaster master = networkUser.master;

                                if (master.IsDeadAndOutOfLivesServer() || !master.GetBody() || !master.GetBody().healthComponent.alive)
                                {
                                    RespawnAtDeathPoint(master);
                                }
                            }
                        }
                    }
                }
            };
        }



        private static void RespawnAtDeathPoint(CharacterMaster master)
        {
            Vector3 position = master.transform.position;
            Quaternion rotation = master.transform.rotation;
            master.Respawn(position, rotation);
        }
    }
}
