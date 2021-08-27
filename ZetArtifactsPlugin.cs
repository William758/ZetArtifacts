using System;
using UnityEngine;
using BepInEx;
using R2API;
using R2API.Utils;
using MiniRpcLib;
using RoR2.ContentManagement;

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
	[R2APISubmoduleDependency(nameof(LanguageAPI), nameof(EliteAPI))]
	[BepInDependency(MiniRpcPlugin.Dependency)]
	[BepInDependency("com.TPDespair.DiluvianArtifact", BepInDependency.DependencyFlags.SoftDependency)]

	public class ZetArtifactsPlugin : BaseUnityPlugin
	{
		public const string ModVer = "1.2.0";
		public const string ModName = "ZetArtifacts";
		public const string ModGuid = "com.TPDespair.ZetArtifacts";



		public static MiniRpcInstance miniRpc;



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



		public static void RegisterLanguageToken(string token, string text)
		{
			LanguageAPI.Add(token, text);
		}



		public void Awake()
		{
			ContentManager.collectContentPackProviders += ContentManager_collectContentPackProviders;

			miniRpc = MiniRpc.CreateInstance(ModGuid);

			ZetRevivifact.Init();
			ZetMultifact.Init();
			ZetDropifact.Init();
			ZetLoopifact.Init();
			if (CreateEclipseArtifact()) ZetEclifact.Init();
		}

		private void ContentManager_collectContentPackProviders(ContentManager.AddContentPackProviderDelegate addContentPackProvider)
		{
			addContentPackProvider(new ZetArtifactsContent());
		}

		internal static bool CreateEclipseArtifact()
		{
			return !BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey("com.TPDespair.DiluvianArtifact");
		}



		public static bool PluginLoaded(string key)
		{
			return BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(key);
		}
	}
}
