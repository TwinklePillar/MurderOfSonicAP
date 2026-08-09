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
		public static Dictionary<ItemIds, int> itemState;

		class DisplayedMessage {
			public GameObject go;
			public float timeLeft;
		}
		private static DisplayedMessage[] messageText;
		public static Queue<string> messages;

		public static GameObject inventoryListener;

		static int lastItem;
		static bool invDirty;
		static bool deathlinkPending;
		
		public override void OnInitializeMelon() {
			MelonPreferences.CreateCategory("Archipelago");
			
			new SlotData();
			messages = new Queue<string>();
			itemState = new Dictionary<ItemIds, int>();
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

			GameObject closeButton = invWndXfrm.Find("Close").gameObject;
			RectTransform arrowXfrm = (RectTransform)((GameObject)DialogView.Instance.GetType().GetField("nextArrow", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(DialogView.Instance)).transform;
			GameObject dialogClose = GameObject.Instantiate(closeButton, arrowXfrm.parent.parent, false);
			dialogClose.transform.position = new Vector3(arrowXfrm.position.x, ((RectTransform)arrowXfrm.parent).sizeDelta.y-arrowXfrm.position.y, 0);
			dialogClose.GetComponent<Button>().onClick.AddListener(() => {
				GameManager.Instance.EndConversation();
			});

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
			/*
				Transform rooms = GameObject.Find("Canvas/DebugMapScreen/WindowRoot").transform;
				List<string> envs = APTitle.file.saves[curSlot].seenEnvs;
				List<string> hasCars = new List<string>();
				foreach (APItem item in APShared.items) {
					if (itemState[(ItemIds)item.id] > 0) hasCars.Add(item.env);
				}
				for (int i = 0; i < rooms.childCount; i++) {
					GameObject room = rooms.GetChild(i).gameObject;
					ChangeEnvironmentButton ceb = room.GetComponent<ChangeEnvironmentButton>();
					if (ceb == null) {
						room.SetActive(false);
						continue;
					}
					FieldInfo fi = ceb.GetType().GetField("Destination", BindingFlags.NonPublic | BindingFlags.Instance);
					string dest = (string)fi.GetValue(ceb);
					bool hasString = hasCars.Contains(dest);
					foreach (string env in envs) {
						if (env == dest) hasString = true;
					}
					room.SetActive(hasString);
				}
				EventManager.TriggerEvent("ToggleMap", null);
			*/
			}
		}

		public static APConnectionInfo manualConnect;
		public static bool awaitingSlotSelect;
		public static void DisconnectFromArchipelago() {
			lastItem = -1;
			curSlot = -1;
			foreach (APItem item in APShared.items) {
				itemState[(ItemIds)item.id] = 0;
			}
			if (currentSession != null) {
				currentSession.Items.ItemReceived -= HandleItem;
				currentSession.MessageLog.OnMessageReceived -= OnMessageReceived;
				deathLink.DisableDeathLink();
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
					currentSession = null;
					return;
				}

				new SlotData(slotDataDict);
				currentSession.Items.ItemReceived += HandleItem;
				currentSession.MessageLog.OnMessageReceived += OnMessageReceived;
				deathLink = DeathLinkProvider.CreateDeathLinkService(currentSession);
				deathLink.OnDeathLinkReceived += HandleDeathLink;
				if (APTitle.file.client.deathLink) deathLink.EnableDeathLink();
				messages.Enqueue("Successful Connection to TMOSTH! You may now collect checks");
				messages.Enqueue("Press the tilde button (~) to travel to unlocked cars");
				int i = 0;
				foreach (ItemInfo item in currentSession.Items.AllItemsReceived) {
					onItem(item, true);
					i++;
				}
				lastItem = i;
				while (currentSession.Items.Any()) currentSession.Items.DequeueItem();
				
				for (i = 0; i < APTitle.file.saves.Length; i++) {
					APSaveData data = APTitle.file.saves[i];
					if (data.room == curRoom && data.team == currentSession.ConnectionInfo.Team && data.slot == currentSession.ConnectionInfo.Slot) {
						curSlot = i;
						data.quickConnect = connect;
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
				if (!itemState.ContainsKey((ItemIds)item.id)) continue;
				if (itemState[(ItemIds)item.id] > 0) inventory.AddItem(item.inventory);
			}
			return InvErr.Succ;
		}
		
		public static void HandleItem(ReceivedItemsHelper itemHandler) {
			while (itemHandler.Any()) {
				int oldIndex = itemHandler.Index;
				if (oldIndex <= lastItem) continue;
				ItemInfo item = itemHandler.DequeueItem();
				lastItem = oldIndex;
				onItem(item, false);
			}
		}

		private static void onItem(ItemInfo item, bool catchup)  {
			MelonLogger.Msg("Receiving {0} with ID {1}", item.ItemDisplayName, item.ItemId);
			itemState[(ItemIds)item.ItemId] += 1;
			invDirty = true;
		}
		
		private static void playerDiedListener(Dictionary<string, object> message) {
			if (message != null && message.ContainsKey("PlayerWon") && (bool)message["PlayerWon"]) return;
			sendDeathLink("was unable to THINK!");
		}
		
		public static bool isDeathLink = false;
		public static void sendDeathLink(string source) {
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