using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ItemManager : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler, IPointerEnterHandler, IPointerExitHandler {
	//Item indices
	public const int Empty = -1;
	public const int Sword = 0;
	public const int Greatsword = 1;
	public const int Hammer = 2;
	public const int DwarfSword = 3;
	public const int Spear = 4;
	public const int Axe = 5;
	public const int Apple = 6;
	//Item data indices
	public const int Width = 0;
	public const int Height = 1;
	public const int Durability = 2;
	public const int DamageToHP = 3;
	public const int DamageToArmor = 4;
	public const int Penetration = 5;
	public const int Range = 6;
	public const int Volatility = 7;

	public const int Player = 0;
	public const int HandSlot = -1;

	public static List<KeyValuePair<string, string>> itemName = new List<KeyValuePair<string, string>>(); //Name and discription of items
    public static List<List<float>> itemData = new List<List<float>>(); //Size (0, 1), Durability (2), Damage to HP (3), Damage to armor (4), Penetration (5), Range (6), Floating coefficient (7) of items
    public static List<float> itemVolatility; //Weights of the floating coefficient to values of item data
    public static List<bool> itemTwoHanded; //Whether items occupies two hand slots

	public static List<List<List<int>>> chestItems; //Indices of items in chests
    public static List<GameObject> handItems; //Items in hand slots

	public int index, owner;
	public Vector2Int relPos; //The relative position of the item in the inventory or chest
	public int durability, durabilityMax;
	public List<float> curItemData;

	static Vector2 itemOffset; //Offset between item and cell
	Canvas canvas;
	bool isDragging;
	bool isTwoHanded; //Whether the item occupies two hand slots
	Vector2 size;
	Vector2 offset;
	Vector2 lastPos;
	Vector2Int lastRelPos; //Due to the rounding error in floats, both absolute and relative positions need to be saved
	Image image;
	Sprite icon;
	RectTransform rectTransform;
	Image leftHandPart, rightHandPart;
	Sprite leftImage, rightImage;
	ItemInfo itemInfo;
	PlayerController playerController;
	static ObjectPool pool;

	static ItemManager() {
		itemOffset = new Vector2(7.5f, 7.5f);

		itemName.Add(new KeyValuePair<string, string>("骑士短剑", "一把普通的骑士短剑。"));
        itemName.Add(new KeyValuePair<string, string>("骑士长剑", "一把难以挥舞的长剑，拥有巨大的攻击范围。"));
        itemName.Add(new KeyValuePair<string, string>("战锤", "一把重型战锤。它可以对敌人和障碍物造成毁灭性打击。"));
        itemName.Add(new KeyValuePair<string, string>("符文短剑", "一把似乎是由矮人锻造的短剑，护手上的符文可以提供额外的穿甲伤害。"));
        itemName.Add(new KeyValuePair<string, string>("骑士长枪", "一把骑士长枪。骑在马上冲锋的骑士能够发挥其最大威力，可惜地牢里没有马。"));
        itemName.Add(new KeyValuePair<string, string>("战斧", "一把巨型战斧。这是矮人们的标志性武器，而非骑士们的。"));
        itemName.Add(new KeyValuePair<string, string>("苹果", "一颗来自阿瓦隆的原汁原味的苹果。"));

        itemData.Add(new List<float>{1, 1, 20, 10, 10, 0.2f, 0.9f, 0.2f}); //Sword
        itemData.Add(new List<float>{1, 2, 20, 15, 10, 0.2f, 1.6f, 0.2f}); //Greatsword
        itemData.Add(new List<float>{1, 2, 40, 30, 15, 0, 1.4f, 0.4f}); //Hammer
        itemData.Add(new List<float>{1, 1, 15, 15, 12.5f, 0.25f, 0.9f, 0.1f}); //Dwarf sword
        itemData.Add(new List<float>{1, 2, 15, 20, 12.5f, 0.3f, 2.0f, 0.25f}); //Spear
        itemData.Add(new List<float>{1, 2, 25, 25, 10, 0.2f, 1.4f, 0.3f}); //Axe
        itemData.Add(new List<float>{1, 1, 1, -20, 1, 1, 0, 0.1f}); //Apple (-20 means restores 20 HP of player)

        itemVolatility = new List<float>{0, 0, 2, 1, 1, 0.25f, 0.25f, 0};
        itemTwoHanded = new List<bool>{false, false, true, false, true, true, false};
	}

	void Start() {
		//Initialize basic data
		itemInfo = GameObject.Find("ItemInfo").GetComponent<ItemInfo>();
		playerController = GameObject.Find("Player").GetComponent<PlayerController>();
		canvas = GetComponentInParent<Canvas>();
		gameObject.name = itemName[index].Key;
		List<float> data = itemData[index];
		size = GetAbsoluteSize(new Vector2(data[Width], data[Height]));
		curItemData = new List<float>();
		for(int i = DamageToHP; i <= Range; i++) {
			float weight = itemVolatility[i];
			curItemData.Add(data[i] * GlobalVariables.Instance.GetRandom(1 - data[Volatility] * weight, 1 + data[Volatility] * weight));
		}
		//Penetration should be between 0 and 1
		curItemData[Penetration - DamageToHP] = Mathf.Clamp(curItemData[Penetration - DamageToHP], 0, 1);
		//Durability should be at least 1
		durabilityMax = Mathf.CeilToInt(data[Durability] * GlobalVariables.Instance.GetRandom(1 - data[Volatility] * itemVolatility[Durability], 1 + data[Volatility] * itemVolatility[Durability]));
		durability = durabilityMax;
		isTwoHanded = itemTwoHanded[index];
		
		//Initialize size and position
		float width = data[Width] * 75 - 15;
		float height = data[Height] * 75 - 15;
		rectTransform = GetComponent<RectTransform>();
		rectTransform.sizeDelta = new Vector2(data[Width] * 75 - 15, data[Height] * 75 - 15);
		Vector2 position = new Vector2(relPos.x * GUIController.margin.x + itemOffset.x, relPos.y * GUIController.margin.x + itemOffset.y) + GUIController.inventoryPositions["Chest"];
		rectTransform.anchoredPosition3D = new Vector3(position.x, position.y, 1);
		lastPos = rectTransform.anchoredPosition;
		lastRelPos = relPos;
		
		//Initialize images
		image = GetComponent<Image>();
		icon = Resources.Load<Sprite>("Images/" + index.ToString());
		image.sprite = icon;
		leftHandPart = transform.Find("LeftHandPart").GetComponent<Image>();
		leftImage = Resources.Load<Sprite>("Images/" + index.ToString() + "_l");
		rightHandPart = transform.Find("RightHandPart").GetComponent<Image>();
		rightImage = Resources.Load<Sprite>("Images/" + index.ToString() + "_r");;
		
		//Add event listeners
		EventManager.Instance.AddListener("OnChestOpen", OnChestOpened);
		EventManager.Instance.AddListener("OnPause", OnPaused);
		pool = GameObject.Find("Items").GetComponent<ObjectPool>();
		Hide();
		HideTwoParts();
	}

	//Use the item
	public void Use() {
		durability -= 1;
		if(durability <= 0) {
			//The item must be used in hand slots
			RemoveHandItem(index, relPos.x);
			//Remove the listeners to prevent memory leakage
			EventManager.Instance.RemoveListener("OnChestOpen", OnChestOpened);
			EventManager.Instance.RemoveListener("OnPause", OnPaused);
			//Destroy(gameObject);
			pool.ReleaseObject(gameObject);
		}
	}

	//Get absolute size of inventory or item
	Vector2 GetAbsoluteSize(Vector2 relSize) {
		return new Vector2(relSize.x * GUIController.margin.x, relSize.y * GUIController.margin.y);
	}

	//Check if two rectangle intersects
	bool CheckIntersect(Vector2 pos1, Vector2 size1, Vector2 pos2, Vector2 size2) {
		return pos1.x + size1.x > pos2.x && pos2.x + size2.x > pos1.x && pos1.y + size1.y > pos2.y && pos2.y + size2.y > pos1.y;
	}

	//Get relative position and absolute position of closest cell in inventory
	KeyValuePair<Vector2Int, Vector2> GetCellPosition(Vector2 pos, string type) {
		Vector2 margin = GUIController.margin;
		Vector2Int invPos = GUIController.inventoryPositions[type];
		Vector2Int invSize = GUIController.inventorySizes[type];
		int x = Mathf.Clamp(Mathf.RoundToInt((pos.x - invPos.x) / margin.x), 0, invSize.x - 1);
		int y = Mathf.Clamp(Mathf.RoundToInt((pos.y - invPos.y) / margin.y), 0, invSize.y - 1);
		return new KeyValuePair<Vector2Int, Vector2>(new Vector2Int(x, y), invPos + itemOffset + new Vector2(x * margin.x, y * margin.y));
	}

	//Get position in hand slots
	Vector2 GetHandSlotPos(int slot) {
		return GUIController.handSlotPositions[slot].Key + itemOffset + (GameConfig.isPaused ? GUIController.handSlotPauseOffset[slot] : new Vector2Int(0, 0));
	}

	void ShowInfo(string partName) {
		int part = ItemInfo.OneHanded;
		if(isTwoHanded) {
			part = ItemInfo.TwoHanded;
			if(partName == "LeftHandPart") {
				part = ItemInfo.LeftHandPart;
			}
			else if(partName == "RightHandPart") {
				part = ItemInfo.RightHandPart;
			}
		}
		itemInfo.SetInfoText(index, part, curItemData, durability);
	}

	//Put item into hand slots
	void SetHandItem(int index, int slot) {
		GlobalVariables.Instance.playerItemCount++;
		EventManager.Instance.TriggerEvent("OnItemGet", GlobalVariables.Instance.playerItemCount.ToString());
		handItems[slot] = gameObject;
		string name = PlayerController.GetWeaponName(slot);
		playerController.weapons[name].SetActive(true);
		SetItemSize(playerController.weapons[name].transform, curItemData[Range - DamageToHP]);
		EventManager.Instance.TriggerEvent("OnEquip", null);
	}

	//Remove item from hand slots
	void RemoveHandItem(int index, int slot) {
		GlobalVariables.Instance.playerItemCount--;
		handItems[slot] = null;
		string name = itemName[index].Key;
		if(itemTwoHanded[index]) {
			playerController.weapons["Wep_" + name].SetActive(false);
		}
		else {
			playerController.weapons["Wep_" + name + "_" + slot.ToString()].SetActive(false);
		}
	}

	//Set model size of item
	void SetItemSize(Transform obj, float size) {
		float scale = size / obj.GetComponent<MeshRenderer>().bounds.size.y;
		//obj.localScale *= scale;
		obj.localScale = new Vector3(obj.localScale.x, obj.localScale.y * scale, obj.localScale.z);
	}

	//Hide the item
	void Hide() {
		image.sprite = null;
		image.color = new Color(1, 1, 1, 0);
		image.raycastTarget = false;
	}

	//Show the item
	void Show() {
		image.sprite = icon;
		image.color = new Color(1, 1, 1, 1);
		image.raycastTarget = true;
	}

	//Hide two-handed parts
	void HideTwoParts() {
		leftHandPart.sprite = null;
		leftHandPart.color = new Color(1, 1, 1, 0);
		leftHandPart.raycastTarget = false;
		rightHandPart.sprite = null;
		rightHandPart.color = new Color(1, 1, 1, 0);
		rightHandPart.raycastTarget = false;
	}

	//Show two-handed parts
	void ShowTwoParts() {
		leftHandPart.sprite = leftImage;
		leftHandPart.color = new Color(1, 1, 1, 1);
		leftHandPart.raycastTarget = true;
		rightHandPart.sprite = rightImage;
		rightHandPart.color = new Color(1, 1, 1, 1);
		rightHandPart.raycastTarget = true;
	}

	//Revoke dragging
	void RevokeDragging() {
		isDragging = false;
		rectTransform.anchoredPosition = lastPos;
		relPos = lastRelPos;
		if(owner == HandSlot && isTwoHanded) {
			Hide();
			ShowTwoParts();
			leftHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.LeftHand) - rectTransform.anchoredPosition;
			rightHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.RightHand) - rectTransform.anchoredPosition;
		}	
	}

	//Stop dragging
	void StopDragging(Vector2 pos) {
		isDragging = false;
		if(owner == Player || owner == HandSlot || owner == ChestController.openedChest) {
			Dictionary<string, Vector2Int> sizes = GUIController.inventorySizes;
			Dictionary<string, Vector2Int> poses = GUIController.inventoryPositions;
			string type = "";
			//Check which inventory the item is dropped to
			foreach (KeyValuePair<string, Vector2Int> cells in sizes) {
				if(CheckIntersect(pos, size, poses[cells.Key], GetAbsoluteSize(cells.Value))) {
					type = cells.Key;
					break;
				}
			}
			//When chests are closed, only search player's inventory
			if(type != "" && (ChestController.openedChest > -1 || type == "Inventory")) {
				//Debug.Log(type);
				int tempOwner = type == "Inventory" ? Player : ChestController.openedChest;
				KeyValuePair<Vector2Int, Vector2> cellPos = GetCellPosition(pos, type);
				//Debug.Log(cellPos.Key);
				if(CellManager.CheckEmptyCells(index, cellPos.Key, tempOwner)) {
					//Debug.Log(lastPos);
					if(owner != HandSlot) {
						//Remove the item from original inventory
						CellManager.RemoveItem(index, lastRelPos, owner);
					}
					else {
						//Remove the item from hand slots
						RemoveHandItem(index, lastRelPos.x);
					}
					//Put the item into new inventory
					CellManager.SetItem(index, cellPos.Key, tempOwner);
					owner = tempOwner;
					rectTransform.anchoredPosition = cellPos.Value;
					relPos = cellPos.Key;
					lastPos = rectTransform.anchoredPosition;
					lastRelPos = relPos;
				}
				else {
					//Debug.Log(owner.ToString() + " " + tempOwner.ToString());
					//ChestController.PrintCells(owner);
					//ChestController.PrintCells(tempOwner);
					//Debug.Log("no free room");
					RevokeDragging();
				}
			}
			else {
				//Check if the item is dropped to hand slots
				int i;
				List<KeyValuePair<Vector2Int, Vector2Int>> handSlots = GUIController.handSlotPositions;
				for(i = 0; i < handSlots.Count; i++) {
					if(CheckIntersect(pos, size, handSlots[i].Key + GUIController.handSlotPauseOffset[i], handSlots[i].Value)) {
						break;
					}
				}
				if(i < handSlots.Count && handItems[i] == null && ((!isTwoHanded && (handItems[1 - i] == null || !itemTwoHanded[handItems[1 - i].GetComponent<ItemManager>().index])) || (isTwoHanded && handItems[1 - i] == null))) {
					if(owner != HandSlot) {
						CellManager.RemoveItem(index, lastRelPos, owner);
					}
					else {
						RemoveHandItem(index, lastRelPos.x);
					}
					SetHandItem(index, i);
					owner = HandSlot;
					rectTransform.anchoredPosition = handSlots[i].Key + GUIController.handSlotPauseOffset[i] + itemOffset;
					relPos = new Vector2Int(i, 0);
					lastPos = rectTransform.anchoredPosition;
					lastRelPos = relPos;
					if(owner == HandSlot && isTwoHanded) {
						leftHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.LeftHand) - rectTransform.anchoredPosition;
						rightHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.RightHand) - rectTransform.anchoredPosition;
						Hide();
						ShowTwoParts();
						if(relPos.x == GUIController.LeftHand) {
							ShowInfo("LeftHandPart");
						}
						else {
							ShowInfo("RightHandPart");
						}
					}
				}
				else {
					//Debug.Log("out of bound");
					RevokeDragging();
				}
			}
		}
	}

	public void OnPointerDown(PointerEventData eventData) {
		if(!isDragging) {
			isDragging = true;
			if(owner == HandSlot && isTwoHanded) {
				lastPos = eventData.pointerPressRaycast.gameObject.name == "LeftHandPart" ? GetHandSlotPos(GUIController.LeftHand) : GetHandSlotPos(GUIController.RightHand);
				rectTransform.anchoredPosition = lastPos;
			}
			else {
				lastPos = rectTransform.anchoredPosition;
			}
			lastRelPos = relPos;
			RectTransformUtility.ScreenPointToLocalPointInRectangle(
				canvas.transform as RectTransform,
				eventData.position,
				canvas.worldCamera,
				out offset
			);
			offset = lastPos - offset;
			HideTwoParts();
			Show();
		}
	}

	public void OnDrag(PointerEventData eventData) {
		Vector2 localPos;
		if(RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvas.transform as RectTransform,
			eventData.position,
			canvas.worldCamera,
			out localPos
		)) {
			rectTransform.anchoredPosition = localPos + offset;
		}
		ShowInfo(eventData.pointerDrag.gameObject.name);
	}

	public void OnPointerUp(PointerEventData eventData) {
		if(Input.GetMouseButtonUp(0)) {
			StopDragging(rectTransform.anchoredPosition);
		}
	}

	public void OnPointerEnter(PointerEventData eventData) {
		//Show information of the item
		ShowInfo(eventData.pointerCurrentRaycast.gameObject.name);
		itemInfo.Show();
	}

	public void OnPointerExit(PointerEventData eventData) {
		//Hide information of the item
		itemInfo.Hide();
	}

	void OnChestOpened(string message) {
		if(owner == ChestController.openedChest || owner == Player || owner == HandSlot) {
			//The owner is player or the current chest
			if(isTwoHanded && owner == HandSlot) {
				leftHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.LeftHand) - rectTransform.anchoredPosition;
				rightHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.RightHand) - rectTransform.anchoredPosition;
				Hide();
				ShowTwoParts();
			}
			else {
				Show();
			}
		}
	}

	void OnPaused(string message) {
		if(message == "inventory" && GameConfig.isPaused && owner == Player || owner == HandSlot) {
			Show();
			if(owner == HandSlot) {
				rectTransform.anchoredPosition = GUIController.handSlotPositions[relPos.x].Key + itemOffset + (GameConfig.isPaused ? GUIController.handSlotPauseOffset[relPos.x] : Vector2Int.zero);
				if(isTwoHanded) {
					leftHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.LeftHand) - rectTransform.anchoredPosition;
					rightHandPart.GetComponent<RectTransform>().anchoredPosition = GetHandSlotPos(GUIController.RightHand) - rectTransform.anchoredPosition;
					Hide();
					ShowTwoParts();
				}
			}
		}
		else {
			Hide();
			HideTwoParts();
		}
	}
}
