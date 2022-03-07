using System;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
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
	[BepInDependency("com.arimah.PerfectedLoop", BepInDependency.DependencyFlags.SoftDependency)]
	[BepInDependency("com.TPDespair.DiluvianArtifact", BepInDependency.DependencyFlags.SoftDependency)]

	public class ZetArtifactsPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.3.0";
		public const string ModName = "ZetArtifacts";
		public const string ModGuid = "com.TPDespair.ZetArtifacts";



		public static ConfigEntry<int> RivivifactEnable { get; set; }
		public static ConfigEntry<int> MultifactEnable { get; set; }
		public static ConfigEntry<int> MultifactMultiplier { get; set; }
		public static ConfigEntry<int> DropifactEnable { get; set; }
		public static ConfigEntry<bool> DropifactRemoveScrapper { get; set; }
		public static ConfigEntry<bool> DropifactLunar { get; set; }
		public static ConfigEntry<bool> DropifactVoid { get; set; }
		public static ConfigEntry<bool> DropifactUnique { get; set; }
		public static ConfigEntry<int> LoopifactEnable { get; set; }
		public static ConfigEntry<int> LoopifactEliteLevel { get; set; }
		public static ConfigEntry<int> EclifactEnable { get; set; }



		public void Awake()
		{
			ConfigSetup(Config);

			ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;

			ZetRevivifact.Init();
			ZetMultifact.Init();
			ZetDropifact.Init();
			ZetLoopifact.Init();
			ZetEclifact.Init();

			if (LoopifactEnable.Value != 0)
			{
				RoR2Application.onLoad += ZetLoopifact.ApplyEarlyEliteProperties;
			}

			//On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
		}



		private static void ConfigSetup(ConfigFile Config)
		{
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
			DropifactEnable = Config.Bind(
				"Artifacts", "dropifactEnable", 1,
				"Artifact of Tossing. 0 = Disabled, 1 = Artifact Available, 2 = Always Active"
			);
			DropifactRemoveScrapper = Config.Bind(
				"Artifacts", "dropifactRemoveScrapper", false,
				"Prevent scrappers from appearing while artifact is active."
			);
			DropifactLunar = Config.Bind(
				"Artifacts", "dropifactLunar", true,
				"Allow dropping lunar items."
			);
			DropifactVoid = Config.Bind(
				"Artifacts", "dropifactVoid", true,
				"Allow dropping void items."
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
				"Ambient level for elites to spawn early."
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

		public static void RegisterLanguageToken(string token, string text)
		{
			LanguageAPI.Add(token, text);
		}

		public static Sprite CreateSprite(byte[] resourceBytes, Color fallbackColor)
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
				Debug.LogError(e.ToString());
				FillTexture(tex, fallbackColor);
			}

			return Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(31, 31));
		}

		public static Texture2D FillTexture(Texture2D tex, Color color)
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

		public static Texture2D CleanAlpha(Texture2D tex)
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



		public static bool PluginLoaded(string key)
		{
			return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(key);
		}
	}
}
