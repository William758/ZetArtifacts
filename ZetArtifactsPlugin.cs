using System;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using RoR2;
using RoR2.ContentManagement;
using R2API;
using R2API.Utils;
using R2API.Networking;

using System.Security;
using System.Security.Permissions;

[module: UnverifiableCode]
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete

namespace TPDespair.ZetArtifacts
{
	[BepInPlugin(ModGuid, ModName, ModVer)]
	[BepInDependency(R2API.R2API.PluginGUID)]
	[R2APISubmoduleDependency(nameof(LanguageAPI), nameof(EliteAPI), nameof(NetworkingAPI))]
	[BepInDependency("com.TPDespair.DiluvianArtifact", BepInDependency.DependencyFlags.SoftDependency)]

	public class ZetArtifactsPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.4.7";
		public const string ModName = "ZetArtifacts";
		public const string ModGuid = "com.TPDespair.ZetArtifacts";



		public static ManualLogSource logSource;

		public static ConfigEntry<int> BazaarifactEnable { get; set; }
		public static ConfigEntry<bool> BazaarHomeExtraCauldrons { get; set; }

		public static ConfigEntry<int> RivivifactEnable { get; set; }

		public static ConfigEntry<int> MultifactEnable { get; set; }
		public static ConfigEntry<int> MultifactMultiplier { get; set; }

		public static ConfigEntry<int> HoardifactEnable { get; set; }
		public static ConfigEntry<bool> HoardifactMerge { get; set; }
		public static ConfigEntry<bool> HoardifactDifficulty { get; set; }
		public static ConfigEntry<float> HoardifactT1Effect { get; set; }
		public static ConfigEntry<float> HoardifactT2Effect { get; set; }
		public static ConfigEntry<float> HoardifactT3Effect { get; set; }
		public static ConfigEntry<float> HoardifactBossEffect { get; set; }
		public static ConfigEntry<float> HoardifactLunarEffect { get; set; }
		public static ConfigEntry<float> HoardifactPlayerExponent { get; set; }
		public static ConfigEntry<float> HoardifactSetupMoney { get; set; }
		public static ConfigEntry<float> HoardifactInteractableCostTarget { get; set; }
		public static ConfigEntry<float> HoardifactInteractableCostFactor { get; set; }

		public static ConfigEntry<int> DropifactEnable { get; set; }
		public static ConfigEntry<bool> DropifactBypassGround { get; set; }
		public static ConfigEntry<bool> DropifactRemoveScrapper { get; set; }
		public static ConfigEntry<bool> DropifactBazaarScrapper { get; set; }
		public static ConfigEntry<bool> DropifactAltScrap { get; set; }
		public static ConfigEntry<bool> DropifactT1 { get; set; }
		public static ConfigEntry<bool> DropifactT2 { get; set; }
		public static ConfigEntry<bool> DropifactT3 { get; set; }
		public static ConfigEntry<bool> DropifactBoss { get; set; }
		public static ConfigEntry<bool> DropifactLunar { get; set; }
		public static ConfigEntry<bool> DropifactVoidT1 { get; set; }
		public static ConfigEntry<bool> DropifactVoidT2 { get; set; }
		public static ConfigEntry<bool> DropifactVoidT3 { get; set; }
		public static ConfigEntry<bool> DropifactVoidBoss { get; set; }
		public static ConfigEntry<bool> DropifactVoidLunar { get; set; }
		public static ConfigEntry<bool> DropifactVoid { get; set; }
		public static ConfigEntry<bool> DropifactUnique { get; set; }

		public static ConfigEntry<int> LoopifactEnable { get; set; }
		public static ConfigEntry<int> LoopifactEliteLevel { get; set; }
		public static ConfigEntry<bool> LoopifactEliteClassic { get; set; }
		public static ConfigEntry<float> LoopifactEliteReplacementChance { get; set; }
		public static ConfigEntry<float> LoopifactEliteReplacementFactor { get; set; }
		public static ConfigEntry<float> LoopifactCombatMoney { get; set; }
		public static ConfigEntry<float> LoopifactMonsterCostTarget { get; set; }
		public static ConfigEntry<float> LoopifactMonsterCostFactor { get; set; }

		public static ConfigEntry<int> EarlifactEnable { get; set; }
		public static ConfigEntry<bool> EarlifactMerge { get; set; }

		public static ConfigEntry<int> EclifactEnable { get; set; }



		public void Awake()
		{
			ConfigSetup(Config);
			logSource = Logger;

			ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;

			ZetBazaarifact.Init();

			ZetRevivifact.Init();

			ZetMultifact.Init();
			ZetHoardifact.Init();

			ZetDropifact.Init();

			ZetLoopifact.Init();
			ZetEarlifact.Init();

			ZetEclifact.Init();

			DirectorCardManipulator.Init();

			RoR2Application.onLoad += LateSetup;
			
			//On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
		}

		public void FixedUpdate()
		{
			ZetHoardifact.OnFixedUpdate();
		}

		private static void LateSetup()
		{
			ZetBazaarifact.LateSetup();

			ZetHoardifact.LateSetup();

			ZetDropifact.LateSetup();

			ZetLoopifact.LateSetup();

			DirectorCardManipulator.LateSetup();
		}



		private static void ConfigSetup(ConfigFile Config)
		{
			BazaarifactEnable = Config.Bind(
				"Artifacts", "bazaarifactEnable", 0,
				"Artifact of the Bazzar. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			BazaarHomeExtraCauldrons = Config.Bind(
				"Artifacts", "bazaarHomeExtraCauldrons", true,
				"Allow additional cauldrons from BazaarIsMyHome even if artifact is available and not being used."
			);

			RivivifactEnable = Config.Bind(
				"Artifacts", "rivivifactEnable", 1,
				"Artifact of Revival. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);

			MultifactEnable = Config.Bind(
				"Artifacts", "multifactEnable", 1,
				"Artifact of Multitudes. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			MultifactMultiplier = Config.Bind(
				"Artifacts", "multifactMultiplier", 2,
				"Player count multiplier. Whole numbers only."
			);

			HoardifactEnable = Config.Bind(
				"Artifacts", "hoardifactEnable", 1,
				"Artifact of Accumulation. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			HoardifactMerge = Config.Bind(
				"Artifacts", "hoardifactMerge", false,
				"Merge Accumulation into Multitudes. hoardifactEnable is ignored."
			);
			HoardifactDifficulty = Config.Bind(
				"Artifacts", "hoardifactDifficulty", true,
				"Enable difficulty increase from items."
			);
			HoardifactT1Effect = Config.Bind(
				"Artifacts", "hoardifactT1Effect", 10f,
				"Amount of added time per T1 item."
			);
			HoardifactT2Effect = Config.Bind(
				"Artifacts", "hoardifactT2Effect", 20f,
				"Amount of added time per T2 item."
			);
			HoardifactT3Effect = Config.Bind(
				"Artifacts", "hoardifactT3Effect", 60f,
				"Amount of added time per T3 item."
			);
			HoardifactBossEffect = Config.Bind(
				"Artifacts", "hoardifactBossEffect", 40f,
				"Amount of added time per Boss item."
			);
			HoardifactLunarEffect = Config.Bind(
				"Artifacts", "hoardifactLunarEffect", 40f,
				"Amount of added time per Lunar item."
			);
			HoardifactPlayerExponent = Config.Bind(
				"Artifacts", "hoardifactPlayerExponent", 0.5f,
				"Total time is divided by true player count, then is multiplied by playercount to the exponent. Example : (60 seconds / 2 players) * (2 players ^ 0.5 exponent) = 42.4 seconds."
			);
			HoardifactSetupMoney = Config.Bind(
				"Artifacts", "hoardifactSetupMoney", 0.2f,
				"Increase scene director interactable credits. 0.2 = 20% increased credits."
			);
			HoardifactInteractableCostTarget = Config.Bind(
				"Artifacts", "HoardifactInteractableCostTarget", 20f,
				"Set Interactable SpawnCard DirectorCost target. Will make interactable spawn costs above value cheaper."
			);
			HoardifactInteractableCostFactor = Config.Bind(
				"Artifacts", "HoardifactInteractableCostFactor", 0.20f,
				"Modify Interactable SpawnCard DirectorCost towards target. Will only decrease cost. 0 = don't modify cost, 1 = set to target cost."
			);

			DropifactEnable = Config.Bind(
				"Artifacts", "dropifactEnable", 1,
				"Artifact of Tossing. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			DropifactBypassGround = Config.Bind(
				"Artifacts", "dropifactBypassGround", false,
				"Drop items directly on contact with the ground. Bypass onDropletHitGroundServer event, which includes Artifact of Command."
			);
			DropifactRemoveScrapper = Config.Bind(
				"Artifacts", "dropifactRemoveScrapper", false,
				"Prevent scrappers from appearing while artifact is active."
			);
			DropifactBazaarScrapper = Config.Bind(
				"Artifacts", "dropifactBazaarScrapper", false,
				"Adds a scrapper to the bazaar while artifact is active."
			);
			DropifactAltScrap = Config.Bind(
				"Artifacts", "dropifactAltScrap", true,
				"Set to false to scrap with RMB instead of Alt+RMB."
			);
			DropifactT1 = Config.Bind(
				"Artifacts", "dropifactT1", true,
				"Allow dropping non-void T1 items."
			);
			DropifactT2 = Config.Bind(
				"Artifacts", "dropifactT2", true,
				"Allow dropping non-void T2 items."
			);
			DropifactT3 = Config.Bind(
				"Artifacts", "dropifactT3", true,
				"Allow dropping non-void T3 items."
			);
			DropifactBoss = Config.Bind(
				"Artifacts", "dropifactBoss", true,
				"Allow dropping non-void boss items."
			);
			DropifactLunar = Config.Bind(
				"Artifacts", "dropifactLunar", true,
				"Allow dropping non-void lunar items."
			);
			DropifactVoidT1 = Config.Bind(
				"Artifacts", "dropifactVoidT1", true,
				"Allow dropping void T1 items."
			);
			DropifactVoidT2 = Config.Bind(
				"Artifacts", "dropifactVoidT2", true,
				"Allow dropping void T2 items."
			);
			DropifactVoidT3 = Config.Bind(
				"Artifacts", "dropifactVoidT3", true,
				"Allow dropping void T3 items."
			);
			DropifactVoidBoss = Config.Bind(
				"Artifacts", "dropifactVoidBoss", true,
				"Allow dropping void boss items."
			);
			DropifactVoidLunar = Config.Bind(
				"Artifacts", "dropifactVoidLunar", true,
				"Allow dropping void lunar items."
			);
			DropifactVoid = Config.Bind(
				"Artifacts", "dropifactVoid", true,
				"Allow dropping ANY void items. Setting this to false will prevent ALL void items from being dropable."
			);
			DropifactUnique = Config.Bind(
				"Artifacts", "dropifactUnique", true,
				"Allow dropping WorldUnique items."
			);

			LoopifactEnable = Config.Bind(
				"Artifacts", "loopifactEnable", 1,
				"Artifact of Escalation. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			LoopifactEliteLevel = Config.Bind(
				"Artifacts", "loopifactEliteLevel", 10,
				"Ambient level for T2 elites to spawn during first loop. -1 to disable early T2 elites."
			);
			LoopifactEliteClassic = Config.Bind(
				"Artifacts", "loopifactEliteClassic", true,
				"Change cost, health, and damage values to 35% towards T2 elite values instead of equal to T1 elite values."
			);
			LoopifactEliteReplacementChance = Config.Bind(
				"Artifacts", "loopifactEliteReplacementChance", 0.1f,
				"Base chance that a T1 Elite will be changed into a random early T2 Elite. 0.1 = 10% chance."
			);
			LoopifactEliteReplacementFactor = Config.Bind(
				"Artifacts", "loopifactEliteReplacementFactor", 0.35f,
				"Every mod that adds t1 Elites increases replacment chance. Chance = 1 - ((1 - ReplacementChance) ^ (1 + (ModCount * ReplacementFactor)))"
			);
			LoopifactCombatMoney = Config.Bind(
				"Artifacts", "loopifactCombatMoney", 0.1f,
				"Increase combat director monster credits. 0.1 = 10% increased credits."
			);
			LoopifactMonsterCostTarget = Config.Bind(
				"Artifacts", "loopifactMonsterCostTarget", 200f,
				"Set Monster SpawnCard DirectorCost target. Will make monster spawn costs above value cheaper."
			);
			LoopifactMonsterCostFactor = Config.Bind(
				"Artifacts", "loopifactMonsterCostFactor", 0.20f,
				"Modify Monster SpawnCard DirectorCost towards target. Will only decrease cost. 0 = don't modify cost, 1 = set to target cost."
			);

			EarlifactEnable = Config.Bind(
				"Artifacts", "earlifactEnable", 1,
				"Artifact of Sanction 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			EarlifactMerge = Config.Bind(
				"Artifacts", "earlifactMerge", false,
				"Merge Sanction into Escalation. earlifactEnable is ignored."
			);

			EclifactEnable = Config.Bind(
				"Artifacts", "eclifactEnable", 1,
				"Artifact of the Eclipse. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
		}

		private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
		{
			addContentPackProvider(new ZetArtifactsContent());
		}

		internal static void LogInfo(object data)
		{
			logSource.LogInfo(data);
		}

		internal static void LogWarn(object data)
		{
			logSource.LogWarning(data);
		}

		internal static void LogError(object data)
		{
			logSource.LogError(data);
		}

		internal static void RegisterToken(string token, string text)
		{
			LanguageAPI.Add(token, text);
		}

		internal static bool ArtifactEnabled(ArtifactDef artifactDef)
		{
			return RunArtifactManager.instance && RunArtifactManager.instance.IsArtifactEnabled(artifactDef);
		}

		internal static bool PluginLoaded(string key)
		{
			return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(key);
		}



		internal static Sprite CreateSprite(byte[] resourceBytes, Color fallbackColor)
		{
			// Create a temporary texture, then load the texture onto it.
			var tex = new Texture2D(32, 32, TextureFormat.RGBA32, false);
			try
			{
				if (resourceBytes == null)
				{
					FillTexture(tex, fallbackColor);
				}
				else
				{
					tex.LoadImage(resourceBytes, false);
					tex.Apply();
					CleanAlpha(tex);
				}
			}
			catch (Exception e)
			{
				LogError(e.ToString());
				FillTexture(tex, fallbackColor);
			}

			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(31, 31));
		}

		private static Texture2D FillTexture(Texture2D tex, Color color)
		{
			var pixels = tex.GetPixels();
			for (var i = 0; i < pixels.Length; ++i)
			{
				pixels[i] = color;
			}

			tex.SetPixels(pixels);
			tex.Apply();

			return tex;
		}

		private static Texture2D CleanAlpha(Texture2D tex)
		{
			var pixels = tex.GetPixels();
			for (var i = 0; i < pixels.Length; ++i)
			{
				if (pixels[i].a < 0.05f)
				{
					pixels[i] = Color.clear;
				}
			}

			tex.SetPixels(pixels);
			tex.Apply();

			return tex;
		}
	}
}
