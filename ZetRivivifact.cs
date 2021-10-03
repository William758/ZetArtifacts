using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace TPDespair.ZetArtifacts
{
	public static class ZetRevivifact
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
					if (RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(ZetArtifactsContent.Artifacts.ZetRevivifact)) return true;

					return false;
				}
			}
		}



		internal static void Init()
		{
			state = ZetArtifactsPlugin.RivivifactEnable.Value;
			if (state < 1) return;

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
					if (Enabled)
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
					if (Enabled)
					{
						if (Run.instance && Run.instance.livingPlayerCount > 0)
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
