using HarmonyLib;
using UnityEngine;
using MelonLoader;
using TMPro;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Archipelago.MultiClient.Net.Enums;
using Newtonsoft.Json;
using System.Linq;
using System.Threading.Tasks;
using Archipelago.MultiClient.Net.Models;

namespace tmosthap {
	public class HintMode {
		public class ScoutedLocation {
			public long id;
			public ItemFlags flags;
		}

		[System.Serializable]
		public class SerializedRunner {
			public int numLanes;
			public float speed;
			public int ringsToWin;
			public int[] tiles;
		}
		
		public static bool enabled = false;
		public static Ink.Runtime.Story apStory;
		public static System.Random rng;
		public static List<ScoutedLocation> locations;
		public static int numComplete;
		
		public static List<RunnerLevelData> hintRunners;
		public static List<Material> hintMat1;
		public static List<Material> hintMat2;
		public static List<UnityEngine.Color> hintCol1;
		public static List<UnityEngine.Color> hintCol2;

		[HarmonyPatch(typeof(RunnerGameManager), "Start")]
		private static class ManagerStartPatch {
			private static void Prefix(RunnerGameManager __instance, RunnerLevelsDB ___levelsDB) {
				MelonLogger.Msg("Level Names:");
				hintRunners = new List<RunnerLevelData>();
				hintMat1 = new List<Material>();
				hintMat2 = new List<Material>();
				hintCol1 = new List<UnityEngine.Color>();
				hintCol2 = new List<UnityEngine.Color>();
				int[] allowedRunners = {5,6,4,9,7,10,11,12,13,15,14,32,28,33,18,20,29,21,22,23,25,26,27,30,31};
				foreach (RunnerLevelData level in ___levelsDB.levels) {
					MelonLogger.Msg("\t{0}: {1}", level.databaseID, level.name);
					if (allowedRunners.Contains(level.databaseID)) {
						RunnerLevelData rld = new RunnerLevelData(level);
						rld.databaseID = -1;
						hintRunners.Add(rld);
						hintMat1.Add(level.backgroundMaterial1);
						hintMat2.Add(level.backgroundMaterial2);
						hintCol1.Add(level.tileColor1);
						hintCol2.Add(level.tileColor2);
					}
				}
			}
		}

		[HarmonyPatch(typeof(FinalPushManager), "OnEnable")]
		private static class FinalPushPatch {
			private static void Prefix(
				FinalPushManager __instance,
				RunnerLevelsDB ____runnerLevelsDB,
				TextMeshProUGUI ___debugLevelNameText,
				AsyncOperationHandle<GameObject> ___RunnerGameAsynchOperationHandle,
				GameObject ___runnerGameRoot,
				TrackManager ___runnerGameController,
				RunnerLevelData ____readyLevelData,
				bool ___initialized
			) {
				if (enabled) {
					__instance.enabled = false;
					HintGameManager hgm = __instance.gameObject.AddComponent<HintGameManager>();
					hgm._runnerLevelsDB = ____runnerLevelsDB;
					hgm.debugLevelNameText = ___debugLevelNameText;
					hgm.RunnerGameAsynchOperationHandle = ___RunnerGameAsynchOperationHandle;
					hgm.runnerGameRoot = ___runnerGameRoot;
					hgm.runnerGameController = ___runnerGameController;
					hgm._readyLevelData = ____readyLevelData;
					hgm.initialized = ___initialized;
				}
			}
		}

		[HarmonyPatch(typeof(FinalPushManager), "InkInitFinalPush")]
		private static class FinalPushInkInitFinalPushPatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(FinalPushManager), "StartLevel")]
		private static class FinalPushStartLevelPatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(FinalPushManager), "OnFinishGame")]
		private static class FinalPushOnFinishGamePatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(FinalPushManager), "SetTrainLevelValues")]
		private static class FinalPushSetTrainLevelValuesPatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(FinalPushManager), "KickflipTrain")]
		private static class FinalPushKickflipTrainPatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}
		[HarmonyPatch(typeof(FinalPushManager), "UnloadGame")]
		private static class FinalPushUnloadGamePatch {
			private static bool Prefix() {
				if (enabled) return false;
				return true;
			}
		}

		[HarmonyPatch(typeof(EnvironmentView), "SwitchEnvironmentsCoroutine")]
		private static class SwitchEnvironmentsPatch {
			private static void Prefix(ref string assetKey) {
				if (enabled) assetKey = "Environments/HintGame";
			}
		}

		[HarmonyPatch(typeof(InkParser), "ParseAsFunction")]
		private static class InkParsePatch {
			private static void Prefix(string text) {
				text = text.Replace(">>>>", "");
				string[] array = text.Split(new char[]{'('});
				if (array.Length != 0) {
				//	string text2 = "";
					if (array.Length > 1) {
						array[1] = array[1].Replace(")", "");
				//		text2 = array[1].Trim();
					}
					switch (array[0].Trim()) {
						case "ReturnFromTrap":
							ModMain.returnFromTrap();
							break;
					}
				}
			}
		}

		public static void OnSceneLoad() {
			apStory = (new Ink.Compiler(System.IO.File.ReadAllText(ModMain.getAssetPath("archipelago.ink")))).Compile();

			rng = new System.Random();
			
			Ink.Runtime.Story mainStory = StoryManager.Instance.story;
			mainStory.mainContentContainer.content.AddRange(apStory.mainContentContainer.content);
			foreach (Ink.Runtime.INamedContent item in apStory.mainContentContainer.namedContent.Values) {
				if (item.name == "global decl") continue;
				((Ink.Runtime.Object)item).parent = mainStory.mainContentContainer;
				mainStory.mainContentContainer.namedContent[item.name] = item;
				mainStory.mainContentContainer.namedOnlyContent[item.name] = (Ink.Runtime.Object)item;
			}
		}

		public static void OnHintConnect() {
			locations = new List<ScoutedLocation>();
			numComplete = 0;
			List<long> toScout = new List<long>();
			Hint[] hints = ModMain.currentSession.Hints.GetHints();
			foreach (long location in ModMain.currentSession.Locations.AllMissingLocations) {
				bool alreadyHinted = false;
				foreach (Hint hint in hints) {
					if (hint.LocationId == location) alreadyHinted = true;
				}
				if (!alreadyHinted) toScout.Add(location);
			}
			ModMain.currentSession.Locations.ScoutLocationsAsync(false, toScout.ToArray()).ContinueWith(HandleInitialScout);
			
			HintMode.enabled = true;
			EventManager.TriggerEvent("MoveToEnvironment", new Dictionary<string, object>{
				{ "DestinationKey", "Final_Push" },
				{ "GoToTopKnot", true }
			});
		}

		private static void HandleInitialScout(Task<Dictionary<long, ScoutedItemInfo>> scoutTask) {
			var scoutResult = scoutTask.Result;
			foreach (long key in scoutResult.Keys) {
				locations.Add(new ScoutedLocation{
					id = key,
					flags = scoutResult[key].Flags
				});
			}
		}
	}

	public class HintGameManager : MonoBehaviour {
		public RunnerLevelsDB _runnerLevelsDB;
		public TextMeshProUGUI debugLevelNameText;
		public AsyncOperationHandle<GameObject> RunnerGameAsynchOperationHandle;
		public GameObject runnerGameRoot;
		public TrackManager runnerGameController;
		public RunnerLevelData _readyLevelData;
		public bool initialized;
		
		private void OnEnable() {
			EventManager.StartListening("InitFinalPushMinigame", InkInitFinalPush);
			EventManager.StartListening("StartFinalPushLevel", StartLevel);
			EventManager.StartListening("MinigameFinished", OnFinishGame);
		}
		
		private void OnDisable() {
			EventManager.StopListening("InitFinalPushMinigame", InkInitFinalPush);
			EventManager.StopListening("StartFinalPushLevel", StartLevel);
			EventManager.StopListening("MinigameFinished", OnFinishGame);
		}
		
		private void OnDestroy() {
			UnloadGame(null);
		}
		
		private void InkInitFinalPush(Dictionary<string, object> message) {
			int id = (int)message["levelID"];
			RunnerLevelData levelData;
			levelData = HintMode.hintRunners[HintMode.rng.Next(HintMode.hintRunners.Count())];
			Init(levelData);
		}
		
		private void StartLevel(Dictionary<string, object> message) {
			int id = (int)message["levelID"];
			RunnerLevelData runnerLevelData;
			runnerLevelData = HintMode.hintRunners[HintMode.rng.Next(HintMode.hintRunners.Count())];
			int matidx = HintMode.rng.Next(HintMode.hintMat1.Count());
			int colidx = HintMode.rng.Next(HintMode.hintCol1.Count());
			runnerLevelData.backgroundMaterial1 = HintMode.hintMat1[matidx];
			runnerLevelData.backgroundMaterial2 = HintMode.hintMat2[matidx];
			runnerLevelData.tileColor1 = HintMode.hintCol1[colidx];
			runnerLevelData.tileColor2 = HintMode.hintCol2[colidx];
			debugLevelNameText.text = "Level: " + runnerLevelData.name;
			if (!initialized) {
				Init(runnerLevelData);
				return;
			}
			runnerGameController.StartLevel(runnerLevelData, finalPushLevel: true);
			Time.timeScale = PlayerPrefs.GetFloat("MinigameSpeed", 1f);
		}
		
		private void Init(RunnerLevelData levelData) {
			GameManager.Instance.ToggleHUD(flag: false);
			_readyLevelData = levelData;
			RunnerGameAsynchOperationHandle = Addressables.LoadAssetAsync<GameObject>("RunnerGame");
			RunnerGameAsynchOperationHandle.Completed += RunnerGameLoaded;
		}
		
		private void RunnerGameLoaded(AsyncOperationHandle<GameObject> obj) {
			switch (obj.Status) {
				case AsyncOperationStatus.Succeeded:
					if (obj.Result != null) {
						GameObject gameObject = Object.Instantiate(obj.Result);
						runnerGameRoot = gameObject;
						runnerGameRoot.transform.position = new Vector3(-15.01f, 0.14f, 0f);
						runnerGameController = runnerGameRoot.GetComponentInChildren<TrackManager>();
						runnerGameController.Init(_readyLevelData, finalPushLevel: true);
						_readyLevelData = null;
						Canvas componentInChildren = runnerGameRoot.GetComponentInChildren<Canvas>();
						if (componentInChildren != null) {
							componentInChildren.worldCamera = GetComponentInParent<Canvas>().worldCamera;
						}
						StageManager.Instance.FadeFromBlack(1f, delegate {
							StoryManager.Instance.Continue();
						});
						initialized = true;
					}
					break;
				case AsyncOperationStatus.Failed:
					Debug.LogError("Failed to load runner game");
					break;
				default:
					Debug.LogWarning("Tried to load addressable runner game, but nothing happened?");
					break;
			}
		}
		
		private void OnFinishGame(Dictionary<string, object> message) {
			bool flag = (bool)message["playerWon"];
			if (flag) {
				Time.timeScale = 1f;
				GameManager.Instance.ResumeConversationFromMinigame(flag);
				HintMode.numComplete += 1;
				if (HintMode.numComplete == 5) {
					HintMode.numComplete = 0;
					List<long> locationScout = new List<long>();
					foreach (HintMode.ScoutedLocation location in HintMode.locations) {
						if ((location.flags & ItemFlags.Advancement) != 0) locationScout.Add(location.id);
						else if ((location.flags & ItemFlags.NeverExclude) != 0) for (int i = 0; i < 2; i++) locationScout.Add(location.id);
						else for (int i = 0; i < 5; i++) locationScout.Add(location.id);
					}
					long toScout = locationScout[HintMode.rng.Next(locationScout.Count())];
					int idx = 0;
					foreach (HintMode.ScoutedLocation location in HintMode.locations) {
						if (location.id == toScout) break;
						idx += 1;
					}
					HintMode.locations.RemoveAt(idx);
					ModMain.currentSession.Hints.CreateHints(ModMain.currentSession.ConnectionInfo.Slot, locationIds: new long[]{toScout});
				}
			} else {
				runnerGameController.RestartFinalPushLevel();
			}
		}
		
		private void UnloadGame(Dictionary<string, object> message) {
			Object.Destroy(runnerGameRoot);
			if (RunnerGameAsynchOperationHandle.IsValid()) {
				Addressables.Release(RunnerGameAsynchOperationHandle);
			}
		}
		
		public void OnWin() {
			runnerGameController.ResetGameToEmpty();
			runnerGameController.StopLevel();
			OnFinishGame(new Dictionary<string, object> { { "playerWon", true } });
		}
	}
}