using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace TPDespair.ZetArtifacts
{
	public static class ZetRevivifact
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
			State = ZetArtifactsPlugin.RivivifactEnable.Value;

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETREVIVIFACT_NAME", "Artifact of Revival");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETREVIVIFACT_DESC", "Dead players respawn after the boss is defeated.");

			SaveDeathPositionHook();
			RevivalHook();

			ArenaMissionController.onBeatArena += ArenaMissionController_onBeatArena;
		}

		

		private static void SaveDeathPositionHook()
		{
			On.RoR2.CharacterMaster.OnBodyDeath += (orig, self, body) =>
			{
				if (NetworkServer.active && Enabled)
				{
					self.transform.position = body.footPosition;
					self.transform.rotation = body.transform.rotation;
				}

				orig(self, body);
			};
		}

		private static void RevivalHook()
		{
			On.RoR2.Run.OnServerBossDefeated += (orig, self, bossGroup) =>
			{
				orig(self, bossGroup);

				if (NetworkServer.active && Enabled)
				{
					AttemptReviveDeadPlayers();
				}
			};
		}

		private static void ArenaMissionController_onBeatArena()
		{
			if (NetworkServer.active && Enabled)
			{
				AttemptReviveDeadPlayers();
			}
		}



		private static void AttemptReviveDeadPlayers()
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

		private static void RespawnAtDeathPoint(CharacterMaster master)
		{
			Vector3 position = master.transform.position;
			Quaternion rotation = master.transform.rotation;
			master.Respawn(position, rotation, true);
		}
	}
}
