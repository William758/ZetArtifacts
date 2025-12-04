using RoR2;
using BepInEx;
using System;
using System.Reflection;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour.HookGen;
using System.Collections.Generic;

namespace TPDespair.ZetArtifacts
{
	public static class ZetBazaarifact
	{
		private static readonly BindingFlags Flags = BindingFlags.Instance | BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;

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
			State = ZetArtifactsPlugin.BazaarifactEnable.Value;

			if (State < 1) return;

			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETBAZAARIFACT_NAME", "Artifact of the Bazaar");
			ZetArtifactsPlugin.RegisterToken("ARTIFACT_ZETBAZAARIFACT_DESC", "The Bazaar Between Time contains a larger variety of interactables.");
		}

		internal static void LateSetup()
		{
			// we dont need to hook if the artifact is disabled or always enabled
			if (State == 1)
			{
				if (ZetArtifactsPlugin.PluginLoaded("com.MagnusMagnuson.BiggerBazaar")) BiggerBazaar();
				if (ZetArtifactsPlugin.PluginLoaded("com.MagnusMagnuson.BazaarPrinter")) BazaarPrinter();
				if (ZetArtifactsPlugin.PluginLoaded("com.NetherCrowCSOLYOO.BazaarExpand")) BazaarExpand();
				if (ZetArtifactsPlugin.PluginLoaded("KevinPione.BazaareScrapper")) BazaareScrapper();

				if (ZetArtifactsPlugin.PluginLoaded("com.zorp.ConfigurableBazaar")) ConfigurableBazaar();
				if (ZetArtifactsPlugin.PluginLoaded("com.Lunzir.BazaarIsMyHome")) BazaarIsMyHome();
				if (ZetArtifactsPlugin.PluginLoaded("Def.BazaarIsMyHaven")) BazaarIsMyHaven();
			}
		}



		private static void BiggerBazaar()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.MagnusMagnuson.BiggerBazaar"].Instance;

			MethodInfo methodInfo = Plugin.GetType().GetMethod("isCurrentStageBazaar", Flags);
			if (methodInfo != null)
			{
				HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)BiggerBazaarHook);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : BiggerBazaar.isCurrentStageBazaar");
			}
		}

		private static void BazaarPrinter()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.MagnusMagnuson.BazaarPrinter"].Instance;

			MethodInfo methodInfo = Plugin.GetType().GetMethod("SpawnPrinters", Flags);
			if (methodInfo != null)
			{
				HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : BazaarPrinter.SpawnPrinters");
			}
		}

		private static void BazaarExpand()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.NetherCrowCSOLYOO.BazaarExpand"].Instance;

			MethodInfo methodInfo = Plugin.GetType().GetMethod("SpawnExpand", Flags);
			if (methodInfo != null)
			{
				HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : BazaarExpand.SpawnExpand");
			}
		}

		private static void BazaareScrapper()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["KevinPione.BazaareScrapper"].Instance;

			MethodInfo methodInfo = Plugin.GetType().GetMethod("SpawnScrapper", Flags);
			if (methodInfo != null)
			{
				HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : BazaareScrapper.SpawnScrapper");
			}
		}

		private static void ConfigurableBazaar()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.zorp.ConfigurableBazaar"].Instance;
			Type PluginType = Plugin.GetType();

			List<string> methodNames = new List<string> { "SpawnScrapper", "SpawnCleansingPool", "SpawnPrinters" };

			foreach (string methodName in methodNames)
			{
				MethodInfo methodInfo = PluginType.GetMethod(methodName, Flags);
				if (methodInfo != null)
				{
					HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : ConfigurableBazaar." + methodName);
				}
			}
		}

		private static void BazaarIsMyHome()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["com.Lunzir.BazaarIsMyHome"].Instance;
			Type PluginType = Plugin.GetType();

			List<string> methodNames = new List<string> { "SpawnPrinters", "SpawnScrapper", "SpawnEquipment", "SpawnShrineCleanse", "SpawnShrineRestack", "SpawnShrineHealing" };

			if (!ZetArtifactsPlugin.BazaarHomeExtraCauldrons.Value)
			{
				methodNames.Add("SpawnLunarCauldron");
			}

			foreach (string methodName in methodNames)
			{
				MethodInfo methodInfo = PluginType.GetMethod(methodName, Flags);
				if (methodInfo != null)
				{
					HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : BazaarIsMyHome." + methodName);
				}
			}
		}

		private static void BazaarIsMyHaven()
		{
			BaseUnityPlugin Plugin = BepInEx.Bootstrap.Chainloader.PluginInfos["Def.BazaarIsMyHaven"].Instance;
			Type PluginType = Plugin.GetType();
			Assembly PluginAssembly = Assembly.GetAssembly(PluginType);

			List<string> classNames = new List<string> { "BazaarPrinter", "BazaarScrapper", "BazaarEquipment", "BazaarCleansingPool", "BazaarRestack", "BazaarDonate", "BazaarDecorate" };

			if (!ZetArtifactsPlugin.BazaarHomeExtraCauldrons.Value)
			{
				classNames.Add("BazaarCauldron");
			}

			if (!ZetArtifactsPlugin.BazaarHavenWanderingChef.Value)
			{
				classNames.Add("BazaarWanderingChef");
			}

			foreach (string className in classNames)
			{
				Type type = Type.GetType("BazaarIsMyHaven." + className +", " + PluginAssembly.FullName, false);
				if (type != null)
				{
					MethodInfo methodInfo = type.GetMethod("SetupBazaar", Flags);
					if (methodInfo != null)
					{
						HookEndpointManager.Modify(methodInfo, (ILContext.Manipulator)GenericReturnHook);
					}
					else
					{
						ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Method : " + className + ".SetupBazaar");
					}
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - Could Not Find Type : BazaarIsMyHaven." + className);
				}
			}
		}



		private static void BiggerBazaarHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			ILLabel label = null;

			bool found = c.TryGotoNext(
				x => x.MatchLdcI4(0),
				x => x.MatchRet()
			);

			if (found)
			{
				label = c.MarkLabel();

				c.Index = 0;

				c.EmitDelegate<Func<bool>>(() => { return Enabled; });
				c.Emit(OpCodes.Brfalse, label);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - BiggerBazaarHook Failed");
			}
		}

		private static void GenericReturnHook(ILContext il)
		{
			ILCursor c = new ILCursor(il);

			ILLabel label = null;

			bool found = c.TryGotoNext(
				x => x.MatchRet()
			);

			if (found)
			{
				label = c.MarkLabel();

				c.Index = 0;

				c.EmitDelegate<Func<bool>>(() => { return Enabled; });
				c.Emit(OpCodes.Brfalse, label);
			}
			else
			{
				ZetArtifactsPlugin.LogWarn("[ZetBazaarifact] - GenericReturnHook Failed");
			}
		}
	}
}
