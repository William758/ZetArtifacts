using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TPDespair.ZetArtifacts
{
	public class ZetArtifactsContent : IContentPackProvider
	{
		public ContentPack contentPack = new ContentPack();

		public string identifier
		{
			get { return "ZetArtifactsContent"; }
		}

		public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
		{
			Artifacts.Create();
			Elites.Create();

			AssignInternalArtifactDefReferences();

			contentPack.artifactDefs.Add(Artifacts.artifactDefs.ToArray());
			contentPack.eliteDefs.Add(Elites.eliteDefs.ToArray());

			args.ReportProgress(1f);
			yield break;
		}

		public IEnumerator GenerateContentPackAsync(GetContentPackAsyncArgs args)
		{
			ContentPack.Copy(contentPack, args.output);
			args.ReportProgress(1f);
			yield break;
		}

		public IEnumerator FinalizeAsync(FinalizeAsyncArgs args)
		{
			args.ReportProgress(1f);
			yield break;
		}



		public static void AssignInternalArtifactDefReferences()
		{
			ZetBazaarifact.ArtifactDef = Artifacts.ZetBazaarifact;

			ZetRevivifact.ArtifactDef = Artifacts.ZetRevivifact;

			ZetMultifact.ArtifactDef = Artifacts.ZetMultifact;
			ZetHoardifact.ArtifactDef = (!ZetArtifactsPlugin.HoardifactMerge.Value) ? Artifacts.ZetHoardifact : Artifacts.ZetMultifact;

			ZetDropifact.ArtifactDef = Artifacts.ZetDropifact;

			ZetLoopifact.ArtifactDef = Artifacts.ZetLoopifact;
			ZetEarlifact.ArtifactDef = (!ZetArtifactsPlugin.EarlifactMerge.Value) ? Artifacts.ZetEarlifact : Artifacts.ZetLoopifact;

			ZetEclifact.ArtifactDef = Artifacts.ZetEclifact;
		}



		public static class Artifacts
		{
			public static ArtifactDef ZetBazaarifact;
			public static ArtifactDef ZetRevivifact;
			public static ArtifactDef ZetMultifact;
			public static ArtifactDef ZetHoardifact;
			public static ArtifactDef ZetDropifact;
			public static ArtifactDef ZetLoopifact;
			public static ArtifactDef ZetEarlifact;
			public static ArtifactDef ZetEclifact;

			public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();

			public static void Create()
			{
				if (ZetArtifactsPlugin.BazaarifactEnable.Value == 1)
				{
					ZetBazaarifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetBazaarifact.cachedName = "ARTIFACT_ZETBAZAARIFACT";
					ZetBazaarifact.nameToken = "ARTIFACT_ZETBAZAARIFACT_NAME";
					ZetBazaarifact.descriptionToken = "ARTIFACT_ZETBAZAARIFACT_DESC";
					ZetBazaarifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetbazaar_selected, Color.magenta);
					ZetBazaarifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetbazaar_deselected, Color.gray);

					artifactDefs.Add(ZetBazaarifact);
				}

				if (ZetArtifactsPlugin.RivivifactEnable.Value == 1)
				{
					ZetRevivifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetRevivifact.cachedName = "ARTIFACT_ZETREVIVIFACT";
					ZetRevivifact.nameToken = "ARTIFACT_ZETREVIVIFACT_NAME";
					ZetRevivifact.descriptionToken = "ARTIFACT_ZETREVIVIFACT_DESC";
					ZetRevivifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetrevive_selected, Color.magenta);
					ZetRevivifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetrevive_deselected, Color.gray);

					artifactDefs.Add(ZetRevivifact);
				}

				if (ZetArtifactsPlugin.MultifactEnable.Value == 1)
				{
					ZetMultifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetMultifact.cachedName = "ARTIFACT_ZETMULTIFACT";
					ZetMultifact.nameToken = "ARTIFACT_ZETMULTIFACT_NAME";
					ZetMultifact.descriptionToken = "ARTIFACT_ZETMULTIFACT_DESC";
					ZetMultifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetmultitude_selected, Color.magenta);
					ZetMultifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetmultitude_deselected, Color.gray);

					artifactDefs.Add(ZetMultifact);
				}

				if (ZetArtifactsPlugin.HoardifactEnable.Value == 1 && !ZetArtifactsPlugin.HoardifactMerge.Value)
				{
					ZetHoardifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetHoardifact.cachedName = "ARTIFACT_ZETHOARDIFACT";
					ZetHoardifact.nameToken = "ARTIFACT_ZETHOARDIFACT_NAME";
					ZetHoardifact.descriptionToken = "ARTIFACT_ZETHOARDIFACT_DESC";
					ZetHoardifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zethoard_selected, Color.magenta);
					ZetHoardifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zethoard_deselected, Color.gray);

					artifactDefs.Add(ZetHoardifact);
				}

				if (ZetArtifactsPlugin.DropifactEnable.Value == 1)
				{
					ZetDropifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetDropifact.cachedName = "ARTIFACT_ZETDROPIFACT";
					ZetDropifact.nameToken = "ARTIFACT_ZETDROPIFACT_NAME";
					ZetDropifact.descriptionToken = "ARTIFACT_ZETDROPIFACT_DESC";
					ZetDropifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetdrop_selected, Color.magenta);
					ZetDropifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetdrop_deselected, Color.gray);

					artifactDefs.Add(ZetDropifact);
				}

				if (ZetArtifactsPlugin.LoopifactEnable.Value == 1)
				{
					ZetLoopifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetLoopifact.cachedName = "ARTIFACT_ZETLOOPIFACT";
					ZetLoopifact.nameToken = "ARTIFACT_ZETLOOPIFACT_NAME";
					ZetLoopifact.descriptionToken = "ARTIFACT_ZETLOOPIFACT_DESC";
					ZetLoopifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetloop_selected, Color.magenta);
					ZetLoopifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetloop_deselected, Color.gray);

					artifactDefs.Add(ZetLoopifact);
				}

				if (ZetArtifactsPlugin.EarlifactEnable.Value == 1 && !ZetArtifactsPlugin.EarlifactMerge.Value)
				{
					ZetEarlifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetEarlifact.cachedName = "ARTIFACT_ZETEARLIFACT";
					ZetEarlifact.nameToken = "ARTIFACT_ZETEARLIFACT_NAME";
					ZetEarlifact.descriptionToken = "ARTIFACT_ZETEARLIFACT_DESC";
					ZetEarlifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetearly_selected, Color.magenta);
					ZetEarlifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetearly_deselected, Color.gray);

					artifactDefs.Add(ZetEarlifact);
				}

				if (ZetArtifactsPlugin.EclifactEnable.Value == 1 && !ZetArtifactsPlugin.PluginLoaded("com.TPDespair.DiluvianArtifact"))
				{
					ZetEclifact = ScriptableObject.CreateInstance<ArtifactDef>();
					ZetEclifact.cachedName = "ARTIFACT_ZETECLIFACT";
					ZetEclifact.nameToken = "ARTIFACT_ZETECLIFACT_NAME";
					ZetEclifact.descriptionToken = "ARTIFACT_ZETECLIFACT_DESC";
					ZetEclifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zeteclipse_selected, Color.magenta);
					ZetEclifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zeteclipse_deselected, Color.gray);

					artifactDefs.Add(ZetEclifact);
				}
			}
		}

		public static class Elites
		{
			public static EliteDef PoisonEarly;
			public static EliteDef HauntedEarly;
			public static EliteDef LunarEarly;

			public static List<EliteDef> eliteDefs = new List<EliteDef>();

			public static void Create()
			{
				if (ZetArtifactsPlugin.LoopifactEnable.Value != 0)
				{
					PoisonEarly = ScriptableObject.CreateInstance<EliteDef>();
					eliteDefs.Add(PoisonEarly);

					HauntedEarly = ScriptableObject.CreateInstance<EliteDef>();
					eliteDefs.Add(HauntedEarly);

					LunarEarly = ScriptableObject.CreateInstance<EliteDef>();
					eliteDefs.Add(LunarEarly);
				}
			}
		}
	}
}
