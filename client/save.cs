using System.Collections.Generic;
using HarmonyLib;
using Newtonsoft.Json;
using System.Reflection;
using System.IO;
using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using Newtonsoft.Json.Linq;

namespace tmosthap {
	[Serializable]
	public class APSaveData {
		public string room;
		public int team;
		public int slot;
		public APConnectionInfo quickConnect;
		public SaveData curSave;
		public List<string> seenEnvs;
		public List<string> completeEnvs;

		public APSaveData() {
			room = "";
			team = -1;
			slot = -1;
			quickConnect = new APConnectionInfo();
			curSave = new SaveData();
			seenEnvs = new List<string>();
			completeEnvs = new List<string>();
		}
	}

	[Serializable]
	public class APConnectionInfo {
		public string info;
		public string ip;
		public string slot;
		public int port;
		public string password;

		public APConnectionInfo() {
			ip = "localhost";
			slot = "Player1";
			port = 38281;
			password = "";
			info = "New AP Room";
		}
	}

	[Serializable]
	public class APClientOptions {
		public bool deathLink;
		public bool autoskipDialog;

		public APClientOptions() {
			deathLink = false;
			autoskipDialog = false;
		}
	}
	
	[Serializable]
	public class APSave {
		public APClientOptions client;
		public APSaveData[] saves;

		public APSave(int num) {
			saves = new APSaveData[num];
			for (int i = 0; i < num; i++) {
				saves[i] = new APSaveData();
			}
			client = new APClientOptions();
		}
	}

	
	static class APTitle {
		public static APSave file;

		public class APToggle : MonoBehaviour {
			public delegate void OnSet(bool v);
			public delegate bool OnGet();

			public OnSet onSet;
			public OnGet onGet;
			
			private void Awake() {
				GetComponent<Toggle>().onValueChanged.AddListener(OnValueChanged);
				int num = onGet() ? 1 : 0;
				GetComponent<Toggle>().SetIsOnWithoutNotify(num > 0);
			}
					
			private void OnEnable() {
				int num = onGet() ? 1 : 0;
				GetComponent<Toggle>().SetIsOnWithoutNotify(num > 0);
			}
					
			private void OnValueChanged(bool value) {
				onSet(value);
			}
		}
		
		static GameObject saveHLG;
		static List<SaveFileView> activeViews;
		public static void setupTab() {
			GameObject tabsParent = GameObject.Find("Canvas/ScreenRootLayer").transform.GetChild(4).Find("OverlayScreenBase/WindowRoot/Tabs").gameObject;
			HorizontalLayoutGroup tabsHLG = tabsParent.GetComponent<HorizontalLayoutGroup>();
			tabsHLG.childControlWidth = true;
			tabsHLG.childForceExpandWidth = true;
			GameObject tabTemplate = tabsParent.transform.GetChild(1).gameObject;
			GameObject optTemplate = tabsParent.transform.parent.GetChild(5).gameObject;
			
			GameObject leftBtn = tabsParent.transform.GetChild(0).gameObject;
			GameObject rightBtn = tabsParent.transform.GetChild(2).gameObject;
			activeViews = new List<SaveFileView>();

			var filesVLG = SaveDataManager.Instance.transform.Find("OverlayScreenBase/WindowRoot/FilesLayout").GetComponent<VerticalLayoutGroup>();
			filesVLG.childControlWidth = true;
			filesVLG.childForceExpandWidth = true;
			saveHLG = new GameObject("NextPrev", typeof(HorizontalLayoutGroup));
			saveHLG.transform.SetParent(SaveDataManager.Instance.transform.Find("OverlayScreenBase/WindowRoot/FilesLayout"), false);
			SettingsScreen.Instance.SwitchTab((SettingsScreen.NavigationTabType)1);
			GameObject savePrev = GameObject.Instantiate(leftBtn, saveHLG.transform);
			GameObject saveNext = GameObject.Instantiate(rightBtn, saveHLG.transform);
			SettingsScreen.Instance.SwitchTab((SettingsScreen.NavigationTabType)0);
			savePrev.transform.Find("AnimAnchor/Backing/Text (TMP)").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Previous Page";
			saveNext.transform.Find("AnimAnchor/Backing/Text (TMP)").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Next Page";
			Component.Destroy(saveNext.GetComponent<AssistModeActiveToggle>());
			savePrev.GetComponent<Button>().onClick.AddListener(() => {
				int startIdx = 0;
				for (int i = activeViews.Count-1; i >= 0; i--) {
					if (activeViews[i].gameObject.activeSelf) startIdx = i-1;
					activeViews[i].gameObject.SetActive(false);
				}
				if (startIdx <= -1) startIdx = activeViews.Count-1;
				for (int i = startIdx; i > startIdx-3; i--) {
					activeViews[i].gameObject.SetActive(true);
				}
			});
			saveNext.GetComponent<Button>().onClick.AddListener(() => {
				int startIdx = 0;
				for (int i = 0; i < activeViews.Count; i++) {
					if (activeViews[i].gameObject.activeSelf) startIdx = i+1;
					activeViews[i].gameObject.SetActive(false);
				}
				if (startIdx >= activeViews.Count) startIdx = 0;
				for (int i = startIdx; i < startIdx+3; i++) {
					activeViews[i].gameObject.SetActive(true);
				}
			});
			
			GameObject apTab = GameObject.Instantiate(tabTemplate, tabsParent.transform);
			apTab.transform.SetSiblingIndex(1);
			apTab.transform.Find("AnimAnchor/Backing/Text (TMP)").gameObject.GetComponent<TMPro.TextMeshProUGUI>().text = "Archipelago";
			
			GameObject apOptions = GameObject.Instantiate(optTemplate, tabsParent.transform.parent, false);
			GameObject.Destroy(apOptions.transform.GetChild(10).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(9).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(8).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(7).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(5).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(2).gameObject);
			GameObject.Destroy(apOptions.transform.GetChild(1).gameObject);

			apOptions.transform.Find("Invincibility/Label").GetComponent<TMPro.TextMeshProUGUI>().text = "Death Link";
			apOptions.transform.Find("NoFloorGaps/Label").GetComponent<TMPro.TextMeshProUGUI>().text = "Autoskip Dialog";
			Transform dlToggle = apOptions.transform.Find("Invincibility/Toggle");
			Transform asToggle = apOptions.transform.Find("NoFloorGaps/Toggle");
			Component.Destroy(dlToggle.GetComponent<SettingsToggle>());
			Component.Destroy(asToggle.GetComponent<SettingsToggle>());
			APToggle dlc = dlToggle.gameObject.AddComponent<APToggle>();
			APToggle asc = asToggle.gameObject.AddComponent<APToggle>();
			dlc.onSet = ((bool v) => {
				file.client.deathLink = v;
				saveAPSave();
				if (ModMain.currentSession == null) return;
				if (v) ModMain.deathLink.EnableDeathLink();
				else ModMain.deathLink.DisableDeathLink();
			});
			asc.onSet = ((bool v) => {file.client.autoskipDialog = v; saveAPSave();});
			dlc.onGet = (() => {return file.client.deathLink;});
			asc.onGet = (() => {return file.client.autoskipDialog;});
			
			var tabs = (List<SettingsScreen.TabNavigationData>)SettingsScreen.Instance.GetType().GetField("tabs", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SettingsScreen.Instance);
			tabs.Add(new SettingsScreen.TabNavigationData{
				Type = (SettingsScreen.NavigationTabType)3,
				Button = apTab.GetComponent<Button>(),
				Content = apOptions,
				SpriteSwap = apTab.transform.Find("AnimAnchor/Backing").gameObject.GetComponent<ImageBooleanStateSwap>()
			});
			apTab.GetComponent<Button>().onClick.AddListener(() => {
				SettingsScreen.Instance.SwitchTab((SettingsScreen.NavigationTabType)3);
			});

			foreach (SettingsScreen.TabNavigationData tab in tabs) {
				var text = tab.SpriteSwap.transform.Find("Text (TMP)").GetComponent<TMPro.TextMeshProUGUI>();
				text.fontSize *= 0.83333f;
			}
		}
		
		public static void loadAPSave() {
			if (File.Exists(Application.dataPath + "/../apdata.save")) {
				string content = File.ReadAllText(Application.dataPath + "/../apdata.save");
				file = JsonConvert.DeserializeObject<APSave>(content);
			} else {
				file = new APSave(12);
			}
		}
		
		public static SaveData createSaveState() {
			StoryManager.Instance.story.variablesState["PLAYER_NAME"] = file.saves[ModMain.curSlot].quickConnect.slot;
			return new SaveData {
				SaveTime = file.saves[ModMain.curSlot].quickConnect.info,
				StoryState = StoryManager.Instance.story.state.ToJson(),
				StageStage = StageManager.Instance.GetState(),
				GameState = GameManager.Instance.GetState(),
				AudioState = AudioManager.Instance.GetState(),
				DialogState = DialogView.Instance.GetState()
			};
		}
		
		public static void saveAPSave() {
			string content = JsonConvert.SerializeObject(file, Formatting.Indented);
			File.WriteAllText(Application.dataPath + "/../apdata.save", content);
		}

		static bool isCarSelect;
		public static void openCarSelect() {
			if (ModMain.curSlot == -1) return;
			if (SaveDataManager.Instance.gameObject.activeSelf) return;
			isCarSelect = true;
			EventManager.TriggerEvent("OpenLoadMenu", null);
			var titleText = (TMPro.TextMeshProUGUI)SaveDataManager.Instance.GetType().GetField("titleText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SaveDataManager.Instance);
			var descriptionText = (TMPro.TextMeshProUGUI)SaveDataManager.Instance.GetType().GetField("descriptionText", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(SaveDataManager.Instance);
			titleText.text = "CAR SELECT";
			descriptionText.text = "Select the car to travel to";
		}

		class AutoResize : MonoBehaviour {
			public Vector2 scale = Vector2.one;
			
			private void Update() {
				RectTransform xfrm = ((RectTransform)gameObject.transform);
				xfrm.sizeDelta = ((RectTransform)xfrm.parent).sizeDelta * scale;
			}
		}
		
		[HarmonyPatch(typeof(TitleScreen), "Start")]
		private static class TitleScreenPatch {
			private static void Postfix(TitleScreen __instance) {
				ModMain.DisconnectFromArchipelago();
				loadAPSave();
				GameObject keyArt = __instance.gameObject.transform.Find("KeyArt").gameObject;
				GameObject connectWindow = new GameObject("AP Connect Window", typeof(VerticalLayoutGroup), typeof(Image));
				connectWindow.transform.SetParent(keyArt.transform, false);
				((RectTransform)connectWindow.transform).sizeDelta = ((RectTransform)keyArt.transform).sizeDelta / 2;
				Image wndImage = connectWindow.GetComponent<Image>();
				wndImage.sprite = GameObject.Find("Canvas/ScreenRootLayer").transform.GetChild(4).Find("OverlayScreenBase/WindowRoot").gameObject.GetComponent<Image>().sprite;
				wndImage.type = Image.Type.Sliced;
				wndImage.pixelsPerUnitMultiplier = 3;
				//	par.GetComponent<Image>().enabled = false;
				VerticalLayoutGroup vlg = connectWindow.GetComponent<VerticalLayoutGroup>();
				vlg.spacing = 4;
				vlg.childAlignment = TextAnchor.MiddleCenter;
				InputField[] connectInfo = new InputField[5];
				string[] connectText = {"IP", "Port", "Slot", "Password", "Room Information"};
				string[] defaultText = {"localhost", "38281", "Player1", "", "New Archipelago Room"};
				Text text;
				for (int i = 0; i < 5; i++) {
					GameObject row = new GameObject(string.Format("{0} Row", connectText[i]), typeof(HorizontalLayoutGroup));
					row.transform.SetParent(connectWindow.transform);
					HorizontalLayoutGroup hlg = row.GetComponent<HorizontalLayoutGroup>();
					hlg.spacing = 4;
					hlg.childAlignment = TextAnchor.MiddleCenter;

					GameObject textBacking = new GameObject(string.Format("{0} Text Backing", connectText[i]), typeof(RectTransform));
					textBacking.transform.SetParent(row.transform);
					GameObject inputBacking = new GameObject(string.Format("{0} Input Backing", connectText[i]), typeof(RectTransform));
					inputBacking.transform.SetParent(row.transform);
					
					GameObject textGO = new GameObject(string.Format("{0} Text", connectText[i]), typeof(Text), typeof(Outline), typeof(AutoResize));
					textGO.transform.SetParent(textBacking.transform);
					((RectTransform)textGO.transform).sizeDelta = ((RectTransform)textBacking.transform).sizeDelta;
					text = textGO.GetComponent<Text>();
					text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
					text.text = connectText[i];
					text.fontStyle = FontStyle.Bold;
					text.alignment = TextAnchor.MiddleCenter;
					text.resizeTextForBestFit = true;
					text.fontSize = 1000;

					GameObject inputGO = new GameObject(string.Format("{0} Input", connectText[i]), typeof(Text), typeof(InputField), typeof(Outline), typeof(AutoResize));
					inputGO.transform.SetParent(inputBacking.transform);
					((RectTransform)inputGO.transform).sizeDelta = ((RectTransform)inputBacking.transform).sizeDelta;
					text = inputGO.GetComponent<Text>();
					text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
					text.text = defaultText[i];
					text.fontStyle = FontStyle.Bold;
					text.alignment = TextAnchor.MiddleCenter;
					text.resizeTextForBestFit = true;
					text.fontSize = 1000;
					InputField input = inputGO.GetComponent<InputField>();
					input.text = defaultText[i];
					input.textComponent = text;
					if (connectText[i] == "Port") {
						input.characterLimit = 5;
						input.characterValidation = InputField.CharacterValidation.Integer;
					}
					connectInfo[i] = input;
				}
				GameObject connectButtonBacking = new GameObject("Manual Connect Button Backing", typeof(RectTransform));
				GameObject connectButton = new GameObject("Manual Connect Button", typeof(Button), typeof(Image), typeof(AutoResize));
				connectButton.transform.SetParent(connectButtonBacking.transform, false);
				connectButtonBacking.transform.SetParent(connectWindow.transform);
				Image btnImage = connectButton.GetComponent<Image>();
				connectButton.GetComponent<AutoResize>().scale = new Vector2(0.8f, 1);
				btnImage.sprite = wndImage.sprite;
				
				btnImage.color = new Color(0.75f, 0.75f, 0.75f);
				btnImage.type = Image.Type.Sliced;
				btnImage.pixelsPerUnitMultiplier = 3;
				Button btn = connectButton.GetComponent<Button>();
				btn.image = btnImage;
				btn.onClick.AddListener(() => {
					APConnectionInfo connect = new APConnectionInfo();
					connect.ip = connectInfo[0].text;
					connect.port = Int32.Parse(connectInfo[1].text);
					connect.slot = connectInfo[2].text;
					connect.password = connectInfo[3].text;
					connect.info = connectInfo[4].text;
					ModMain.ConnectToArchipelago(connect, true);
				});
				GameObject conButText = new GameObject("Manual Connect Text", typeof(Text), typeof(AutoResize));
				conButText.transform.parent = connectButton.transform;
				conButText.transform.localScale = Vector3.one;
				conButText.transform.localPosition = Vector3.zero;
				text = conButText.GetComponent<Text>();
				text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
				text.text = "Manual Connect";
				text.fontStyle = FontStyle.Bold;
				text.alignment = TextAnchor.MiddleCenter;
				text.resizeTextForBestFit = true;
				text.fontSize = 1000;

				GameObject hintButtonBacking = new GameObject("Manual Connect Button Backing", typeof(RectTransform));
				GameObject hintButton = new GameObject("Manual Connect Button", typeof(Button), typeof(Image), typeof(AutoResize));
				hintButton.transform.SetParent(hintButtonBacking.transform, false);
				hintButtonBacking.transform.SetParent(connectWindow.transform);
				Image hintButtonImage = hintButton.GetComponent<Image>();
				hintButton.GetComponent<AutoResize>().scale = new Vector2(0.8f, 1);
				hintButtonImage.sprite = wndImage.sprite;
				
				hintButtonImage.color = new Color(0.75f, 0.75f, 0.75f);
				hintButtonImage.type = Image.Type.Sliced;
				hintButtonImage.pixelsPerUnitMultiplier = 3;
				Button hintBtn = hintButton.GetComponent<Button>();
				hintBtn.image = hintButtonImage;
				hintBtn.onClick.AddListener(() => {
					APConnectionInfo connect = new APConnectionInfo();
					connect.ip = connectInfo[0].text;
					connect.port = Int32.Parse(connectInfo[1].text);
					connect.slot = connectInfo[2].text;
					connect.password = connectInfo[3].text;
					connect.info = connectInfo[4].text;
					ModMain.ConnectAsHintGame(connect);
				});
				GameObject hintButText = new GameObject("Manual Connect Text", typeof(Text), typeof(AutoResize));
				hintButText.transform.parent = hintButton.transform;
				hintButText.transform.localScale = Vector3.one;
				hintButText.transform.localPosition = Vector3.zero;
				text = hintButText.GetComponent<Text>();
				text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
				text.text = "Connect as Hint Game";
				text.fontStyle = FontStyle.Bold;
				text.alignment = TextAnchor.MiddleCenter;
				text.resizeTextForBestFit = true;
				text.fontSize = 1000;
			}
		}

		[HarmonyPatch(typeof(NameEntryScreen), "IsInappropriate")]
		private static class InappropriatePatch {
			private static bool Prefix(ref bool __result) {
				__result = false;
				return false;
			}
		}
		
		[HarmonyPatch(typeof(NameEntryScreen), "OnInputUpdate")]
		private static class NameEntryPatch {
			private static void Postfix(NameEntryScreen __instance, TMP_InputField ____inputField, ref string[] ___existingCharacters) {
				____inputField.text = file.saves[ModMain.curSlot].quickConnect.slot;
				___existingCharacters = new string[]{};
				__instance.OnNameSubmit();
			}
		}

		static bool isLoading;
		static bool loadedNonSave;
		static bool envDirty;
		[HarmonyPatch(typeof(EnvironmentView), "OnMoveToEnvironment")]
		private class EnvironmentPatch {
			private static bool Prefix(Dictionary<string, object> message) {
				string fromEnv = GameManager.Instance.GetEnvironmentName();
				if (HintMode.enabled) return true;
				if (ModMain.currentSession == null) return false;
				if (SlotData.entrances.Contains(fromEnv) && ((string)message["DestinationKey"] != "SafeRoom") && !isLoading) {
					message["DestinationKey"] = SlotData.entrances[SlotData.entrances.IndexOf(fromEnv)+1];
				}
				
				loadedNonSave = !isLoading;
				bool canContinue = !SlotData.carRando || isLoading;

				if (fromEnv == "Conductor_Car" || fromEnv == "LockdownDiningCar" || (string)message["DestinationKey"] == "SafeRoom") canContinue = true;
				if (fromEnv == "SafeRoom" && !isLoading) canContinue = false; // yeah I can tell this won't end well otherwise
				if (!canContinue) {
					ModMain.messages.Enqueue("Car Completed. Press the tilde button (~) to select a car and continue");
					return false;
				}
				envDirty = true;
				return true;
			}
		}

		[HarmonyPatch(typeof(EnvironmentView), "SwitchEnvironmentsCoroutine")]
		private static class PostEnvPatch {
			private static void Postfix() {
				isLoading = false;
			}
		}

		public static void tryStartAutosave() {
			if (envDirty) {
				envDirty = false;
				string curEnv = GameManager.Instance.GetEnvironmentName();
				if (curEnv == "") return;
				List<string> seenEnvs = file.saves[ModMain.curSlot].seenEnvs;
				bool seenEnv = false;
				foreach (string env in seenEnvs) {
					if (env == curEnv) seenEnv = true;
				}
				
				if (!seenEnv && SaveStates.states.ContainsKey(curEnv)) {
					seenEnvs.Add(curEnv);
				}
				if (loadedNonSave || !seenEnv) {
					loadedNonSave = false;
					file.saves[ModMain.curSlot].curSave = createSaveState();
				}
				saveAPSave();
				loadAPSave();
			}
		}

		[HarmonyPatch(typeof(DialogView), "OnDialogReadyToUpdate")]
		private static class DialogUpdatePatch {
			private static void Postfix() {
				DialogView dv = DialogView.Instance;
				APTitle.tryStartAutosave();
				dv.FadeIn(0);
				GameManager.Instance.ToggleHUD(true, 0);
				
				if (file.client.autoskipDialog) dv.GetType().GetMethod("OnClickContinue", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(dv, new object[]{});
			}
		}
		
		[HarmonyPatch(typeof(SaveDataManager), "WriteSaveDataToFile")]
		private static class WriteSavePatch {
			private static bool Prefix(List<SaveData> data) {
				for (int i = 0; i < file.saves.Length; i++) {
					data[i].SaveTime = file.saves[i].quickConnect.info;
					file.saves[i].curSave = data[i];
				}
				saveAPSave();
				return false;
			}
		}
		
		[HarmonyPatch(typeof(SaveDataManager), "ReadSaveDataFromFile")]
		private static class ReadSavePatch {
			private static bool Prefix(ref List<SaveData> __result) {
				loadAPSave();
				__result = new List<SaveData>();
				if (isCarSelect && ModMain.curSlot != -1) {
					List<string> hasCars = new List<string>();
					foreach (APItem item in APShared.items) {
						if (ModMain.itemState[(ItemIds)item.id] > 0) hasCars.Add(item.env);
					}
					foreach (string key in SaveStates.states.Keys) {
						bool hasCar = file.saves[ModMain.curSlot].seenEnvs.Contains(key) || hasCars.Contains(key);
						SaveData state = SaveStates.states[key];
						state.GameState.playerName = file.saves[ModMain.curSlot].quickConnect.slot;
						__result.Add(new SaveData{
							SaveTime = "",
							StoryState = hasCar ? state.StoryState : null,
							StageStage = state.StageStage,
							GameState = state.GameState,
							AudioState = state.AudioState,
							DialogState = state.DialogState
						});
					}
					isCarSelect = false;
				} else {
					foreach (APSaveData save in file.saves) {
						__result.Add(save.curSave);
					}
				}
				return false;
			}
		}

		[HarmonyPatch(typeof(SaveFileView), "Init")]
		private static class FileViewPatch {
			private static void Prefix(int index, ref UnityAction onClick) {
				if (onClick != null) {
					onClick = (() => {
						if (ModMain.awaitingSlotSelect) {
							ModMain.curSlot = index;
							ModMain.awaitingSlotSelect = false;
							file.saves[index] = new APSaveData();
							file.saves[index].quickConnect = ModMain.manualConnect;
							file.saves[index].room = ModMain.currentSession.RoomState.Seed;
							file.saves[index].team = ModMain.currentSession.ConnectionInfo.Team;
							file.saves[index].slot = ModMain.currentSession.ConnectionInfo.Slot;
							GameManager.Instance.StartNewGame();
							file.saves[index].curSave = createSaveState();
							saveAPSave();
							if (SlotData.carRando) {
								List<string> hasCars = new List<string>();
								foreach (APItem item in APShared.items) {
									if (ModMain.itemState[(ItemIds)item.id] > 0) hasCars.Add(item.env);
								}
								foreach (string key in SaveStates.states.Keys) {
									bool hasCar = file.saves[ModMain.curSlot].seenEnvs.Contains(key) || hasCars.Contains(key);
									if (hasCar) {
										SaveData state = SaveStates.states[key];
										state.GameState.playerName = file.saves[ModMain.curSlot].quickConnect.slot;
										SaveDataManager.Instance.LoadFile(new SaveData{
											SaveTime = "",
											StoryState = hasCar ? state.StoryState : null,
											StageStage = state.StageStage,
											GameState = state.GameState,
											AudioState = state.AudioState,
											DialogState = state.DialogState
										});
									}
								}
							}
						}
					}) + onClick;
				}
			}
		}
		
		[HarmonyPatch(typeof(SaveDataManager), "OnOpenSaveMenu")]
		private static class OpenSavePatch {
			private static bool Prefix() {
				if (ModMain.curSlot != -1) {
					SaveData state = createSaveState();
					file.saves[ModMain.curSlot].curSave = state;
					saveAPSave();
					EventManager.TriggerEvent("LoadedSaveState", null); // i hate this, but it looks safe enough
					return false;
				}
				return true;
			}
		}
		
		[HarmonyPatch(typeof(SaveDataManager), "ClearSaveFileViews")]
		private static class ClearViewsPatch {
			private static void Prefix() {
				saveHLG.SetActive(false);
				foreach (SaveFileView view in activeViews) {
					view.gameObject.SetActive(true);
				}
				activeViews.Clear();
			}
		}

		[HarmonyPatch(typeof(SaveDataManager), "DisplaySaveFiles")]
		private static class DisplayViewsPatch {
			private static void Postfix(SaveFileView ___saveFileTemplate) {
				saveHLG.SetActive(true);
				saveHLG.transform.SetAsLastSibling();

				foreach (Transform item in ___saveFileTemplate.transform.parent) {
					SaveFileView view = item.gameObject.GetComponent<SaveFileView>();
					if (view != null && view.gameObject.activeSelf) activeViews.Add(view);
				}
				int i = 0;
				foreach (SaveFileView view in activeViews) {
					if (i < 3) view.gameObject.SetActive(true);
					else view.gameObject.SetActive(false);
					i++;
				}
			}
		}
		
		[HarmonyPatch(typeof(SaveDataManager), "LoadFile")]
		private static class LoadFilePatch {
			private static void Prefix(SaveData data) {
				isLoading = true;
				for (int i = 0; i < file.saves.Length; i++) {
					if (file.saves[i].curSave == data) {
						if (i != ModMain.curSlot) ModMain.ConnectToArchipelago(file.saves[i].quickConnect, true);
					}
				}
				data.GameState.playerName = file.saves[ModMain.curSlot].quickConnect.slot;
			//	ModMain.inventoryListener.SetActive(true);
			}
		}
	}
}