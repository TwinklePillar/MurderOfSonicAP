using UnityEngine;
using UnityEngine.UI;
using MelonLoader;
using HarmonyLib;
using Archipelago.MultiClient.Net;
using Archipelago.MultiClient.Net.Enums;
using Archipelago.MultiClient.Net.BounceFeatures.DeathLink;
using Archipelago.MultiClient.Net.MessageLog.Messages;
using Archipelago.MultiClient.Net.MessageLog.Parts;
using Archipelago.MultiClient.Net.Helpers;
using Archipelago.MultiClient.Net.Models;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System;
using Ink.Runtime;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;

[assembly: MelonInfo(typeof(tmosthap.ModMain), "TMOSTH-AP", "0.1.0", "TwinklePillar")]
[assembly: MelonGame("Sonic Social", "The Murder of Sonic The Hedgehog")]

namespace tmosthap {
	class ModMain : MelonMod {
		public enum QOLOption {
			Off = 0,
			On = 1,
			AfterFirstComplete = 2
		}
		
		public static ArchipelagoSession currentSession;
		public static int curSlot = -1;
		public static DeathLinkService deathLink;
		static Dictionary<string, object> slotDataDict;

		class DisplayedMessage {
			public GameObject go;
			public float timeLeft;
		}
		private static DisplayedMessage[] messageText;
		public static Queue<string> messages;

		public static GameObject inventoryListener;
		static GameObject closeButton;

		public static int lastItem;
		static bool invDirty;
		static bool deathlinkPending;
		
		public static string getAssetPath(string path) {
			return System.IO.Path.Combine(Application.dataPath, "../Mods/APTMOSTH_assets", path);
		}
		
		void loadTexture(ref Texture2D tex, string path) {
			tex = new Texture2D(1, 1);
			tex.hideFlags = HideFlags.DontUnloadUnusedAsset;
			byte[] bytes = System.IO.File.ReadAllBytes(getAssetPath(path));
			ImageConversion.LoadImage(tex, bytes);
		}
		
		public override void OnInitializeMelon() {
			MelonPreferences.CreateCategory("Archipelago");
			
			new SlotData();
			messages = new Queue<string>();
			messageText = new DisplayedMessage[5];
			for (int i = 0; i < messageText.Length; i++) {
				messageText[i] = new DisplayedMessage();
			}
			SaveStates.initStates();
		}
		
		public override void OnSceneWasInitialized(int buildIndex, string sceneName) {
			APTitle.setupTab();
			
			GameObject ap = new GameObject("Archipelago");
			inventoryListener = new GameObject("Inventory Listener", typeof(OnInventoryChange));
			inventoryListener.transform.SetParent(ap.transform);
		//	inventoryListener.SetActive(false);
			GameObject canvasGO = new GameObject("APCanvas", typeof(Canvas), typeof(CanvasScaler));
			canvasGO.transform.SetParent(ap.transform);
			Canvas canvas = canvasGO.GetComponent<Canvas>();
			canvas.sortingOrder = 10;
			canvas.renderMode = RenderMode.ScreenSpaceOverlay;
			for (int i = 0; i < messageText.Count(); i++) {
				GameObject textGO = new GameObject(string.Format("APText {0}", i), typeof(Text), typeof(Outline));
				messageText[i].go = textGO;
				messageText[i].timeLeft = 0;
				textGO.SetActive(false);
				textGO.transform.SetParent(canvasGO.transform);
				Text text = textGO.GetComponent<Text>();
				text.GetType().GetMethod("AssignDefaultFont", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(text, new object[]{});
				text.text = "chat looks a bit dead";
				text.alignment = TextAnchor.MiddleCenter;
				text.resizeTextForBestFit = true;
				text.fontSize = 1000;
				text.supportRichText = true;
				RectTransform textRect = textGO.GetComponent<RectTransform>();
				textRect.localPosition = new Vector3(0, -((Screen.height/2)-(Screen.height/16)), 0);
				textRect.sizeDelta = new Vector2(Screen.width, Screen.height/8);
			}

			RectTransform invXfrm = InventoryScreen.Instance.gameObject.GetComponent<RectTransform>();
			RectTransform invWndXfrm = (RectTransform)invXfrm.Find("WindowRoot");
			invWndXfrm.sizeDelta = invWndXfrm.sizeDelta * new Vector2(1.6f, 1);
			RectTransform invLayout = (RectTransform)invWndXfrm.Find("InventoryLayout");
			float oldX = invLayout.rect.x;
			invLayout.sizeDelta = invLayout.sizeDelta * new Vector2(2, 1);
			invLayout.anchoredPosition = invLayout.anchoredPosition * new Vector2(1.5f, 1);
			RectTransform itemDetails = (RectTransform)invWndXfrm.Find("ItemDetails");
			itemDetails.anchoredPosition = itemDetails.anchoredPosition * new Vector2(1.5f, 1);

			closeButton = invWndXfrm.Find("Close").gameObject;
			RectTransform arrowXfrm = (RectTransform)((GameObject)DialogView.Instance.GetType().GetField("nextArrow", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(DialogView.Instance)).transform;
			GameObject dialogClose = GameObject.Instantiate(closeButton, arrowXfrm.parent.parent, false);
			dialogClose.transform.position = new Vector3(arrowXfrm.position.x, ((RectTransform)arrowXfrm.parent).sizeDelta.y-arrowXfrm.position.y, 0);
			dialogClose.GetComponent<Button>().onClick.AddListener(() => {
				GameManager.Instance.EndConversation();
			});

			GameObject hud = GameObject.Find("Canvas/HUD");
			GameObject menuButton = hud.transform.GetChild(0).gameObject;
			GameObject invButton = hud.transform.GetChild(2).gameObject;
			GameObject carSelectButton = GameObject.Instantiate(menuButton, hud.transform, false);
			Component.Destroy(carSelectButton.GetComponent<TriggerEmptyEvent>());
			carSelectButton.transform.position = invButton.transform.position + (invButton.transform.position - menuButton.transform.position);

			Button carButton = carSelectButton.GetComponent<Button>();
			carButton.onClick.AddListener(() => {
				APTitle.openCarSelect();
			});

			Texture2D tex = null;
			loadTexture(ref tex, "car.png");
			Image carImage = carSelectButton.transform.Find("AnimAnchor/Backing/Icon").GetComponent<Image>();
			carImage.sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(tex.width/2, tex.height/2));

			HintMode.OnSceneLoad();
			
			EventManager.StartListening("MinigameFinished", playerDiedListener);
			EventManager.StartListening("RunnerPlayerDied", playerDiedListener);
		}
		
		[HarmonyPatch(typeof(InventoryScreen), "Open")]
		private class InventoryScreenPatch {
			private static void Postfix (InventoryScreen __instance, List<InventoryItemThumbnailView> ___currentItems) {
				for (int i = ___currentItems.Count; i < 16; i++) {
					MethodInfo mi = __instance.GetType().GetMethod("CreateThumbnail", BindingFlags.NonPublic | BindingFlags.Instance);
					___currentItems.Add((InventoryItemThumbnailView)mi.Invoke(__instance, new object[]{""}));
				}
			}
		}
		
		public static bool isQOLEnabled(QOLOption qol) {
			switch (qol) {
				case QOLOption.Off: return false;
				case QOLOption.On: return true;
				case QOLOption.AfterFirstComplete:
					if (currentSession == null) return false;
					string curEnv = GameManager.Instance.GetEnvironmentName();
					List<string> envs = APTitle.file.saves[curSlot].completeEnvs;
					bool hasString = false;
					foreach (string env in envs) {
						if (env == curEnv) hasString = true;
					}
					return hasString;
			}
			return false;
		}

		public static bool hasItem(ItemIds item) {
			foreach (ItemInfo info in currentSession.Items.AllItemsReceived) {
				if ((ItemIds)info.ItemId == item) return true;
			}
			return false;
		}

		static string curPath;
		public static bool inTrap = false;
		public static void beginTrap(string path) {
			if (!inTrap) {
				if (GameManager.Instance.inConversation) curPath = StoryManager.Instance.story.state.currentPathString;
				else curPath = "ReturnToNoDialog";
			}
			inTrap = true;
			StoryManager.Instance.story.state.callStack.Push(PushPopType.Tunnel);
			StoryManager.Instance.story.ChoosePathString(path, false);
			StoryManager.Instance.story.Continue();
		}

		public static void returnFromTrap() {
			StoryManager.Instance.story.ChoosePathString(curPath, false);
			StoryManager.Instance.story.Continue();
			inTrap = false;
		}
		
		public override void OnUpdate() {
			foreach (DisplayedMessage dm in messageText) {
				dm.timeLeft -= Time.unscaledDeltaTime;
				if (dm.go) {
					if (dm.timeLeft < 0) dm.go.SetActive(false);
					else dm.go.SetActive(true);
				}
			}
			if (messages.Count() > 0 && messageText[0].go != null) {
				int index = -1;
				bool found = false;
				for (index = 0; index < messageText.Count(); index++) {
					if (!messageText[index].go.activeSelf) {
						found = true;
						break;
					}
				}
				if (!found) {
					float maxY = -100000;
					index = -1;
					for (int i = 0; i < messageText.Count(); i++) {
						RectTransform xfrm = messageText[i].go.GetComponent<RectTransform>();
						if (xfrm.localPosition.y > maxY) {
							maxY = xfrm.localPosition.y;
							index = i;
						}
					}
					messageText[index].timeLeft = -1;
				} else {
					RectTransform textRect = messageText[index].go.GetComponent<RectTransform>();
					textRect.localPosition = new Vector3(0, -((Screen.height/2)-(Screen.height/16)), 0);
					textRect.sizeDelta = new Vector2(Screen.width, Screen.height/8);
					for (int i = 0; i < messageText.Count(); i++) {
						if (!messageText[i].go.activeSelf) continue;
						RectTransform moveup = messageText[i].go.GetComponent<RectTransform>();
						moveup.localPosition = new Vector3(0, moveup.localPosition.y + moveup.sizeDelta.y/2, 0);
					}

					messageText[index].timeLeft = 5;
					Text text = messageText[index].go.GetComponent<Text>();
					text.text = string.Format("<b>{0}</b>", messages.Dequeue());
				}
			}

			if (currentSession == null) return;
			if (HintMode.enabled) return;

			if (inTrap) closeButton.SetActive(false);
			else closeButton.SetActive(true);
			
			string[] musicTracks = {
				"spagonia","mirage_saloon","saloon1","interrogationsaloon",
				"diningcar1","diningcar2","diningcar3","library1",
				"interrogationlibrary","casino","whiteboardheist","lounge1",
				"interrogationlounge","conductorcar1","conductorcar2","interrogationfinal",
				"conductorcarlockdown","lockdownchase","returntoconductorcar","finalboss",
				"saferoom1","saferoom2","diningcloset1","interrogationcloset",
				"messydiningcar1","diningcarlockdown","lockdowncaroptimistic","namingbarry"
			};
			while (currentSession.Items.Any()) {
				int oldIndex = currentSession.Items.Index;
				ItemInfo item = currentSession.Items.DequeueItem();
				if (oldIndex <= lastItem) continue;
				lastItem = oldIndex;
				MelonLogger.Msg("Receiving Item #{0}", lastItem);
				invDirty = true;
				
				ItemIds itemId = (ItemIds)item.ItemId;
				switch (itemId) {
					case ItemIds.DEAD_TRAP:
						beginTrap("DeadTrap");
						break;
					case ItemIds.FAKER_TRAP:
						beginTrap("FakerTrap");
						break;
					case ItemIds.MUSIC_TRAP:
						AudioManager.Instance.PlayAddressableMusic(musicTracks[HintMode.rng.Next(musicTracks.Count())]);
						break;
					case ItemIds.THINK_TRAP:
						beginTrap("ThinkTrap");
						break;
					default:
						break;
				}
			}
			
			if (invDirty) {
				string errMsg = "";
				switch (rebuildInventory()) {
					case InvErr.Succ:
						invDirty = false;
						break;
					case InvErr.NoInst:
						errMsg = "StoryManager.Instance is null";
						break;
					case InvErr.NoStor:
						errMsg = "StoryManager.Instance.story is null";
						break;
					case InvErr.NoVar:
						errMsg = "StoryManager.Instance.story.variablesState is null";
						break;
					case InvErr.NoInv:
						errMsg = "StoryManager.Instance.story.variablesState[\"Inventory\"] is null";
						break;
					case InvErr.NoOrigin:
						errMsg = "inventory.origins is null";
						break;
				}
				int numDM = 0;
				foreach (DisplayedMessage dm in messageText) {
					if (dm.timeLeft > 0) numDM += 1;
				}
				if (errMsg != "" && numDM < 3) {
					messages.Enqueue("<color=#FF0000>Unable to Rebuild Inventory!</color>");
					messages.Enqueue(string.Format("<color=#FF0000>{0}</color>", errMsg));
				}
			}
			if (deathlinkPending) {
				if (RunnerGameManager.Instance.gameObject.activeSelf) {
					isDeathLink = true;
					RunnerGameManager.Instance.OnLose();
				} else if (DialogView.Instance.gameObject.activeSelf) {
					GameManager.Instance.EndConversation();
				}
				deathlinkPending = false;
			}
			
			if (Input.GetKeyDown(KeyCode.BackQuote)) {
				APTitle.openCarSelect();
			}
		}

		public static APConnectionInfo manualConnect;
		public static bool awaitingSlotSelect;
		public static void DisconnectFromArchipelago() {
			lastItem = -1;
			inTrap = false;
			curSlot = -1;
			if (currentSession != null) {
				currentSession.MessageLog.OnMessageReceived -= OnMessageReceived;
				if (deathLink != null) deathLink.DisableDeathLink();
				currentSession.Socket.DisconnectAsync();
				currentSession = null;
				deathLink = null;
			}
			new SlotData();
		}
		
		public static void ConnectToArchipelago(APConnectionInfo connect, bool newServer) {
			if (newServer) {
				DisconnectFromArchipelago();
			}
			
			currentSession = ArchipelagoSessionFactory.CreateSession(connect.ip, connect.port);
			LoginResult result = currentSession.TryConnectAndLogin("The Murder of Sonic the Hedgehog", connect.slot, ItemsHandlingFlags.AllItems, password: connect.password);
			
			if (result.Successful) {
				slotDataDict = ((LoginSuccessful)result).SlotData;
				string curRoom = currentSession.RoomState.Seed;
				
				if ((long)slotDataDict["version"] != APShared.version) {
					string msg = string.Format("Client Version does not match APWorld (expected {0}, got {1})! Refusing connection", APShared.version, slotDataDict["version"]);
					MelonLogger.Error(msg);
					messages.Enqueue(msg);
					DisconnectFromArchipelago();
					return;
				}

				new SlotData(slotDataDict);
				currentSession.MessageLog.OnMessageReceived += OnMessageReceived;
				deathLink = DeathLinkProvider.CreateDeathLinkService(currentSession);
				deathLink.OnDeathLinkReceived += HandleDeathLink;
				if (APTitle.file.client.deathLink) deathLink.EnableDeathLink();
				messages.Enqueue("Successful Connection to TMOSTH! You may now collect checks");
				messages.Enqueue("Press the tilde button (~) to travel to unlocked cars");

				int i;
				for (i = 0; i < APTitle.file.saves.Length; i++) {
					APSaveData data = APTitle.file.saves[i];
					if (data.room == curRoom && data.team == currentSession.ConnectionInfo.Team && data.slot == currentSession.ConnectionInfo.Slot) {
						curSlot = i;
						data.quickConnect = connect;
						lastItem = data.lastItem;
						break;
					}
				}

				if (i == APTitle.file.saves.Length) {
					awaitingSlotSelect = true;
					manualConnect = connect;
					EventManager.TriggerEvent("OpenSaveMenu", null);
				} else {
					SaveDataManager.Instance.LoadFile(APTitle.file.saves[i].curSave);
				}
			} else {
				MelonLogger.Error("Error while connecting to Archipelago");
				messages.Enqueue("Error while connecting to Archipelago");
				foreach(string e in ((LoginFailure)result).Errors) {
					MelonLogger.Error(e);
					messages.Enqueue(e);
				}
				currentSession = null;
			}
		}

		public static void ConnectAsHintGame(APConnectionInfo connect) {
			ModMain.DisconnectFromArchipelago();
			
			currentSession = ArchipelagoSessionFactory.CreateSession(connect.ip, connect.port);
			LoginResult result = currentSession.TryConnectAndLogin("", connect.slot, ItemsHandlingFlags.AllItems, password: connect.password, tags: new string[]{"HintGame"});
			
			if (result.Successful) {
				currentSession.MessageLog.OnMessageReceived += OnMessageReceived;
				HintMode.OnHintConnect();
			} else {
				MelonLogger.Error("Error while connecting to Archipelago");
				messages.Enqueue("Error while connecting to Archipelago");
				foreach(string e in ((LoginFailure)result).Errors) {
					MelonLogger.Error(e);
					messages.Enqueue(e);
				}
				currentSession = null;
			}
		}

		public static void OnMessageReceived(LogMessage message) {
			StringBuilder sb = new StringBuilder("", 65536);
			MelonLogger.Msg("Message Part Count: {0}", message.Parts.Count());
			foreach (MessagePart part in message.Parts) {
				if (!part.IsBackgroundColor) sb.AppendFormat("<color=#{0:X2}{1:X2}{2:X2}>", part.Color.R, part.Color.G, part.Color.B);
				sb.Append(part.Text);
				if (!part.IsBackgroundColor) sb.Append("</color>");
			}
			messages.Enqueue(sb.ToString());
		}

		[HarmonyPatch(typeof(InventoryListener), "Evaluate")]
		private class InventoryListenerPatch {
			private static bool Prefix() {
				return false;
			}
		}
		
		class OnInventoryChange : InkVariableListener {
			public OnInventoryChange() {
				InkVariable = "Inventory";
			}
			
			protected override void Evaluate(string varName, object value) {
				invDirty = true;
			}
		}

		enum InvErr {
			Succ = 0,
			NoInst = 1,
			NoStor = 2,
			NoVar = 3,
			NoInv = 4,
			NoOrigin = 5
		};
		private static InvErr rebuildInventory() {
			if (!StoryManager.Instance) return InvErr.NoInst;
			if (!StoryManager.Instance.story) return InvErr.NoStor;
			if (StoryManager.Instance.story.variablesState == null) return InvErr.NoVar;
			if (StoryManager.Instance.story.variablesState["Inventory"] == null) return InvErr.NoInv;
			InkList inventory = (InkList)StoryManager.Instance.story.variablesState["Inventory"];
			if (inventory.origins == null) inventory.origins = StoryManager.Instance.story.listDefinitions.lists;
			inventory.Clear();
			foreach (APItem item in APShared.items) {
				if (item.inventory == "") continue;
				if (hasItem((ItemIds)item.id)) inventory.AddItem(item.inventory);
			}
			return InvErr.Succ;
		}
		
		private static void playerDiedListener(Dictionary<string, object> message) {
			if (message != null && message.ContainsKey("playerWon") && (bool)message["playerWon"]) return;
			sendDeathLink("was unable to THINK!");
		}
		
		public static bool isDeathLink = false;
		public static void sendDeathLink(string source) {
			if (curSlot == -1) return;
			string cause = string.Format("{0} {1}", APTitle.file.saves[curSlot].quickConnect.slot, source);
			if (deathLink != null && APTitle.file.client.deathLink && !isDeathLink) {
				deathLink.SendDeathLink(new DeathLink(APTitle.file.saves[curSlot].quickConnect.slot, cause));
			}
			isDeathLink = false;
		}

		public static void HandleDeathLink(DeathLink deathLink) {
			StringBuilder sb = new StringBuilder("", 65536);
			sb.Append("<color=#E02010>Death Link: ");
			if (deathLink.Cause != null) sb.Append(deathLink.Cause);
			else sb.Append(deathLink.Source);
			sb.Append("</color>");
			messages.Enqueue(sb.ToString());
			deathlinkPending = true;
		}
	}
}