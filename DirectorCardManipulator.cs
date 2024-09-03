using System;
using System.Collections.Generic;
using System.Reflection;
using RoR2;
using UnityEngine;
using MonoMod.RuntimeDetour;
using MonoMod.Cil;
using Mono.Cecil.Cil;

namespace TPDespair.ZetArtifacts
{
	public static class DirectorCardManipulator
	{
		public static bool Active = false;

		private static readonly BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance;

		private delegate int DirectorCardReturnInt(DirectorCard self);
		private static DirectorCardReturnInt origDirectorCardCostGetter;

		private static readonly List<string> MonsterCardNames = new List<string>();
		private static readonly List<string> InteractableCardNames = new List<string>();

		private static readonly Dictionary<int, int> MonsterCostCache = new Dictionary<int, int>();
		private static readonly Dictionary<int, int> InteractableCostCache = new Dictionary<int, int>();



		internal static void Init()
		{
			Active = ZetHoardifact.State > 0 || ZetLoopifact.State > 0;

			if (!Active) return;

			SceneDirector.onPrePopulateSceneServer += OnPrePopScene;
			Run.onRunDestroyGlobal += OnRunDestroyed;

			DirectorCardCostHook();
			IteractableCreditExpenditureHook();
		}

		internal static void LateSetup()
		{
			if (!Active) return;

			if (ZetLoopifact.State > 0)
			{
				On.RoR2.ClassicStageInfo.RebuildCards += GatherMonsterCardsHook;
			}

			if (ZetHoardifact.State > 0)
			{
				SceneDirector.onGenerateInteractableCardSelection += GatherInteractableCards;
			}
		}



		private static void OnPrePopScene(SceneDirector director)
		{
			MonsterCostCache.Clear();
			InteractableCostCache.Clear();
		}

		private static void OnRunDestroyed(Run run)
		{
			MonsterCostCache.Clear();
			InteractableCostCache.Clear();
		}



		private static void DirectorCardCostHook()
		{
			MethodInfo GC_Method = typeof(DirectorCard).GetMethod("get_cost", flags);
			MethodInfo GCH_Method = typeof(DirectorCardManipulator).GetMethod(nameof(GetCostHook), flags);

			Hook GC_Hook = new Hook(GC_Method, GCH_Method);
			origDirectorCardCostGetter = GC_Hook.GenerateTrampoline<DirectorCardReturnInt>();
		}

		private static int GetCostHook(DirectorCard directorCard)
		{
			int value = origDirectorCardCostGetter(directorCard);

			if (Active)
			{
				string spawnCardName = directorCard.spawnCard.name;

				if (ZetLoopifact.Enabled && MonsterCardNames.Contains(spawnCardName))
				{
					return GetMonsterCostOverride(value);
				}

				if (ZetHoardifact.Enabled && InteractableCardNames.Contains(spawnCardName))
				{
					return GetInteractableCostOverride(value);
				}
			}

			return value;
		}

		private static int GetMonsterCostOverride(int cost)
		{
			if (MonsterCostCache.ContainsKey(cost))
			{
				return MonsterCostCache[cost];
			}

			Run run = Run.instance;
			if (run && run.loopClearCount >= 2)
			{
				MonsterCostCache.Add(cost, cost);
				return cost;
			}

			int targetCost = Mathf.RoundToInt(Mathf.Max(ZetArtifactsPlugin.LoopifactMonsterCostTarget.Value, 1f));
			float costFactor = Mathf.Clamp(ZetArtifactsPlugin.LoopifactMonsterCostFactor.Value, 0f, 1f);

			if (costFactor > 0f && cost > targetCost)
			{
				int newCost = Mathf.FloorToInt(Mathf.Lerp(cost, targetCost, costFactor));
				ZetArtifactsPlugin.LogInfo("[Manipulator] - Override Monster DirectorCost : " + cost + " => " + newCost);

				MonsterCostCache.Add(cost, newCost);
				return newCost;
			}

			MonsterCostCache.Add(cost, cost);
			return cost;
		}

		private static int GetInteractableCostOverride(int cost)
		{
			if (InteractableCostCache.ContainsKey(cost))
			{
				return InteractableCostCache[cost];
			}

			Run run = Run.instance;
			if (run && run.loopClearCount >= 2)
			{
				InteractableCostCache.Add(cost, cost);
				return cost;
			}

			int targetCost = Mathf.RoundToInt(Mathf.Max(ZetArtifactsPlugin.HoardifactInteractableCostTarget.Value, 1f));
			float costFactor = Mathf.Clamp(ZetArtifactsPlugin.HoardifactInteractableCostFactor.Value, 0f, 1f);

			if (costFactor > 0f && cost > targetCost)
			{
				int newCost = Mathf.FloorToInt(Mathf.Lerp(cost, targetCost, costFactor));
				ZetArtifactsPlugin.LogInfo("[Manipulator] - Override Interactable DirectorCost : " + cost + " => " + newCost);

				InteractableCostCache.Add(cost, newCost);
				return newCost;
			}

			InteractableCostCache.Add(cost, cost);
			return cost;
		}



		// SceneDirector ignores interactable cost override if i dont hook PopulateScene ???
		private static void IteractableCreditExpenditureHook()
		{
			IL.RoR2.SceneDirector.PopulateScene += (il) =>
			{
				ILCursor c = new ILCursor(il);

				int index = -1;

				bool found = c.TryGotoNext(
					x => x.MatchLdloc(out index),
					x => x.MatchCallvirt<DirectorCard>("get_cost")
				);

				if (found)
				{
					c.Index += 2;

					c.Emit(OpCodes.Dup);
					c.Emit(OpCodes.Ldarg, 0);
					c.Emit(OpCodes.Ldloc, index);
					c.EmitDelegate<Action<int, SceneDirector, DirectorCard>>((cost, director, card) =>
					{
						ZetArtifactsPlugin.LogInfo("[Manipulator] - SceneDirector : Spending " + cost + " of " + director.interactableCredit + " credits on " + card.spawnCard.name);
					});
				}
				else
				{
					ZetArtifactsPlugin.LogWarn("[Manipulator] - IteractableCreditExpenditureHook failed!");
				}
			};
		}



		private static void GatherMonsterCardsHook(On.RoR2.ClassicStageInfo.orig_RebuildCards orig, ClassicStageInfo self, DirectorCardCategorySelection dccsMon, DirectorCardCategorySelection dccsInt)
		{
			orig(self, dccsMon, dccsInt);

			GatherMonsterCards(self);
		}

		private static void GatherMonsterCards(ClassicStageInfo stageInfo)
		{
			WeightedSelection<DirectorCard> selection = stageInfo.monsterSelection;
			if (selection != null)
			{
				for (int i = 0; i < selection.Count; i++)
				{
					WeightedSelection<DirectorCard>.ChoiceInfo choiceInfo = selection.GetChoice(i);

					DirectorCard directorCard = choiceInfo.value;
					SpawnCard spawnCard = directorCard.spawnCard;

					if (!MonsterCardNames.Contains(spawnCard.name))
					{
						MonsterCardNames.Add(spawnCard.name);
					}
				}
			}
		}

		private static void GatherInteractableCards(SceneDirector sceneDirector, DirectorCardCategorySelection dccs)
		{
			for (int i = 0; i < dccs.categories.Length; i++)
			{
				DirectorCardCategorySelection.Category catagory = dccs.categories[i];

				string catagoryName = catagory.name;

				//ZetArtifactsPlugin.LogInfo("[Manipulator] - DDCS Category : " + catagoryName);

				if (ApplyToCategory(catagoryName))
				{
					for (int j = catagory.cards.Length - 1; j >= 0; j--)
					{
						DirectorCard directorCard = catagory.cards[j];
						SpawnCard spawnCard = directorCard.spawnCard;

						if (!InteractableCardNames.Contains(spawnCard.name))
						{
							InteractableCardNames.Add(spawnCard.name);
						}

						/*
						ZetArtifactsPlugin.LogInfo("-----");
						ZetArtifactsPlugin.LogInfo(directorCard.spawnCard);
						ZetArtifactsPlugin.LogInfo("Cost : " + directorCard.spawnCard.directorCreditCost);
						ZetArtifactsPlugin.LogInfo("Weight : " + directorCard.selectionWeight);
						ZetArtifactsPlugin.LogInfo("Stages : " + directorCard.minimumStageCompletions);
						//*/
					}
				}
			}
		}

		private static bool ApplyToCategory(string name)
		{
			if (name == "Chests") return true;
			else if (name == "Shrines") return true;
			else if (name == "Drones") return true;
			else if (name == "Misc") return true;
			else if (name == "Rare") return true;
			else if (name == "Duplicator") return true;
			else if (name == "Void Stuff") return true;
			else return false;
		}
	}
}
