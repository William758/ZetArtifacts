using RoR2;
using RoR2.ContentManagement;
using System.Collections;
using UnityEngine;

namespace TPDespair.ZetArtifacts
{
    class ZetArtifactsContent : IContentPackProvider
    {
        public ContentPack contentPack = new ContentPack();

        public string identifier
        {
            get { return "ZetArtifactsContent"; }
        }

		public IEnumerator LoadStaticContentAsync(LoadStaticContentAsyncArgs args)
		{
			Artifacts.Create();

			contentPack.artifactDefs.Add(Artifacts.artifactDefs);
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



		public static class Artifacts
		{
			public static ArtifactDef ZetRevivifact;
			public static ArtifactDef ZetMultifact;
			public static ArtifactDef ZetDropifact;
			public static ArtifactDef ZetEclifact;

			public static ArtifactDef[] artifactDefs;

			public static void Create()
			{
				ZetRevivifact = ScriptableObject.CreateInstance<ArtifactDef>();
				ZetRevivifact.cachedName = "ARTIFACT_ZETREVIVIFACT";
				ZetRevivifact.nameToken = "ARTIFACT_ZETREVIVIFACT_NAME";
				ZetRevivifact.descriptionToken = "ARTIFACT_ZETREVIVIFACT_DESC";
				ZetRevivifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetrevive_selected, Color.magenta);
				ZetRevivifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetrevive_deselected, Color.gray);

				ZetMultifact = ScriptableObject.CreateInstance<ArtifactDef>();
				ZetMultifact.cachedName = "ARTIFACT_ZETMULTIFACT";
				ZetMultifact.nameToken = "ARTIFACT_ZETMULTIFACT_NAME";
				ZetMultifact.descriptionToken = "ARTIFACT_ZETMULTIFACT_DESC";
				ZetMultifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetmulti_selected, Color.magenta);
				ZetMultifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetmulti_deselected, Color.gray);

				ZetDropifact = ScriptableObject.CreateInstance<ArtifactDef>();
				ZetDropifact.cachedName = "ARTIFACT_ZETDROPIFACT";
				ZetDropifact.nameToken = "ARTIFACT_ZETDROPIFACT_NAME";
				ZetDropifact.descriptionToken = "ARTIFACT_ZETDROPIFACT_DESC";
				ZetDropifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetdrop_selected, Color.magenta);
				ZetDropifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zetdrop_deselected, Color.gray);

				ZetEclifact = ScriptableObject.CreateInstance<ArtifactDef>();
				ZetEclifact.cachedName = "ARTIFACT_ZETECLIFACT";
				ZetEclifact.nameToken = "ARTIFACT_ZETECLIFACT_NAME";
				ZetEclifact.descriptionToken = "ARTIFACT_ZETECLIFACT_DESC";
				ZetEclifact.smallIconSelectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zeteclipse_selected, Color.magenta);
				ZetEclifact.smallIconDeselectedSprite = ZetArtifactsPlugin.CreateSprite(Properties.Resources.zeteclipse_deselected, Color.gray);

				artifactDefs = new ArtifactDef[] { ZetRevivifact, ZetMultifact, ZetDropifact, ZetEclifact };
			}
		}
	}
}
