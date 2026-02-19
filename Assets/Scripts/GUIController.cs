using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GUIController : MonoBehaviour {
	public static GUIController Instance; //The instance for the singleton pattern
	public const int LeftHand = 0;
	public const int RightHand = 1;
	const float alertTime = 1;
	const float alertDeltaTime = 0.5f;
	const float alertWidth = 500;

    public static Vector2Int margin;
	public static Dictionary<string, Vector2Int> inventoryPositions = new Dictionary<string, Vector2Int>(); //Initial position inventory and chests
    public static Dictionary<string, Vector2Int> inventorySizes = new Dictionary<string, Vector2Int>(); //Max num of inventory and chests
	public static List<KeyValuePair<Vector2Int, Vector2Int>> handSlotPositions; //Positions and sizes of hand slots
    public static List<Vector2Int> handSlotPauseOffset; //Offsets of hand slots in pause menu

	public GameObject hud;
	public GameObject backpack;
	public GameObject dialog;
	public GameObject cell;
	public GameObject alert;
	public GameObject waypoint;
	public GameObject bossStatus;

	Transform inventory;
	Transform chest;
	Transform player;
	RectTransform leftHandRect;
	RectTransform rightHandRect;
	RectTransform alertFrameRect;
	RectTransform alertTextRect;
	RectTransform waypointRect;
	Canvas canvas;
	Text waypointDist;
	Vector3? waypointPos;

	static GUIController() {
		margin = new Vector2Int(75, 75);
		inventorySizes.Add("Inventory", new Vector2Int(5, 2));
        inventorySizes.Add("Chest", new Vector2Int(5, 2));
        inventoryPositions.Add("Inventory", new Vector2Int(-25, -160));
        inventoryPositions.Add("Chest", new Vector2Int(-25, 60));
		
		handSlotPositions = new List<KeyValuePair<Vector2Int, Vector2Int>> {
            new KeyValuePair<Vector2Int, Vector2Int>(new Vector2Int(-250, -160), new Vector2Int(75, 75)), //Left hand slot
            new KeyValuePair<Vector2Int, Vector2Int>(new Vector2Int(250, -160), new Vector2Int(75, 75)), //Right hand slot
        };
        handSlotPauseOffset = new List<Vector2Int> {
            new Vector2Int(0 ,0), //Left hand slot
            new Vector2Int(-385, 0) //Right hand slot
        };
	}

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }

		EventManager.Instance.AddListener("OnPause", OnPaused);
		EventManager.Instance.AddListener("OnNavigate", OnNavigated);
		EventManager.Instance.AddListener("OnBossFightBegin", OnBossFightBegan);
		EventManager.Instance.AddListener("OnBossFightEnd", OnBossFightEnded);
		player = GameObject.Find("Player").transform;
		canvas = gameObject.GetComponent<Canvas>();
		waypointDist = waypoint.transform.Find("WaypointText").GetComponent<Text>();
		alertFrameRect = alert.transform.Find("MessageBar").GetComponent<RectTransform>();
		alertTextRect = alert.transform.Find("MessageText").GetComponent<RectTransform>();
		waypointRect = waypoint.GetComponent<RectTransform>();
		inventory = backpack.transform.Find("Inventory");
		chest = backpack.transform.Find("ChestSlots");
		leftHandRect = backpack.transform.Find("HandSlots").Find("LeftHandSlot").gameObject.GetComponent<RectTransform>();
		leftHandRect.anchoredPosition = handSlotPositions[LeftHand].Key;
		rightHandRect = backpack.transform.Find("HandSlots").Find("RightHandSlot").gameObject.GetComponent<RectTransform>();
		rightHandRect.anchoredPosition = handSlotPositions[RightHand].Key;
		GenerateInventory(cell, "Inventory", inventory);
		GenerateInventory(cell, "Chest", chest);
		Initialize();
	}

	void Update() {
		if(waypointPos != null && GameConfig.isNavigationEnabled) {
			if(Vector3.Dot(waypointPos.Value - Camera.main.transform.position, Camera.main.transform.forward) > 0) {
				waypoint.SetActive(true);
				Vector3 scrPos = RectTransformUtility.WorldToScreenPoint(Camera.main, waypointPos.Value + new Vector3(0, 1.5f, 0));
				Vector2 uiPos;
				RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, scrPos, canvas.worldCamera, out uiPos);
				waypointRect.anchoredPosition = uiPos;
				waypointDist.text = "任务距离：" + (Mathf.Round(Vector3.Distance(player.position, TaskManager.Instance.GetTaskPlace() ?? player.position) * 10) / 10).ToString() + "m";
			}
			else { //Waypoint is behind player
				waypoint.SetActive(false);
			}
		}
		else {
			waypoint.SetActive(false);
		}
	}

	void Initialize() {
		alert.SetActive(false);
		bossStatus.SetActive(false);
	}

	//Handle pause events
	void OnPaused(string message) {
		inventory.gameObject.SetActive(GameConfig.isPaused && message != "escape");
		chest.gameObject.SetActive(GameConfig.isPaused && message == "chest");
		rightHandRect.anchoredPosition = handSlotPositions[RightHand].Key + (GameConfig.isPaused ? handSlotPauseOffset[RightHand] : Vector2.zero);
		if(message == "inventory") {
			EventManager.Instance.TriggerEvent("OnBackpackOpen", null);
		}
	}

	//Update navigation waypoint
	void OnNavigated(string message) {
		Vector3? curTaskPlace = TaskManager.Instance.GetTaskPlace();
		if(curTaskPlace != null) {
			waypointPos = Navigator.Instance.GetNextWaypoint(player.position, curTaskPlace.Value);
		}
		else {
			waypointPos = null;
		}
	}

	//Clone inventory cells
	void GenerateInventory(GameObject cell, string type, Transform parent) {
		Vector2Int pos = inventoryPositions[type];
		Vector2Int size = inventorySizes[type];
		for(int x = 0; x < size.x; x++) {
			for(int y = 0; y < size.y; y++) {
				GameObject obj = Instantiate(cell);
				obj.name = type + new Vector2Int(x, y).ToString();
				obj.transform.SetParent(parent);
				obj.transform.SetAsLastSibling();
				CellManager cm = obj.GetComponent<CellManager>();
				cm.type = type;
				cm.position = new Vector2Int(pos.x + x * margin.x, pos.y + y * margin.y);
			}
		}
	}

	void OnBossFightBegan(string message) {
		bossStatus.transform.Find("BossName").GetComponent<Text>().text = player.GetComponent<PlayerController>().bossFighting;
		bossStatus.SetActive(true);
	}

	void OnBossFightEnded(string message) {
		bossStatus.SetActive(false);
	}

	//Display alert message
	public IEnumerator Alert(string message) {
		alert.SetActive(true);
		alertTextRect.gameObject.GetComponent<Text>().text = message;
		alertFrameRect.sizeDelta = new Vector2(0, alertFrameRect.rect.height);
		alertTextRect.sizeDelta = new Vector2(0, alertTextRect.rect.height);
		float time = 0;
		while(alertDeltaTime - time > 0) {
			//Trigonometric interpolation
			alertFrameRect.sizeDelta = new Vector2(alertWidth * Mathf.Sin(time / (2 * alertDeltaTime) * Mathf.PI), alertFrameRect.rect.height);
			alertTextRect.sizeDelta = new Vector2(alertWidth * Mathf.Sin(time / (2 * alertDeltaTime) * Mathf.PI), alertTextRect.rect.height);
			yield return null;
			time += Time.unscaledDeltaTime;
		}
		alertFrameRect.sizeDelta = new Vector2(alertWidth, alertFrameRect.rect.height);
		alertTextRect.sizeDelta = new Vector2(alertWidth, alertTextRect.rect.height);	
		time = 0;
		while(alertTime - time > 0) {
			yield return null;
			time += Time.unscaledDeltaTime;
		}
		time = alertDeltaTime;
		while(2 * alertDeltaTime - time > 0) {
			//Trigonometric interpolation
			alertFrameRect.sizeDelta = new Vector2(alertWidth * Mathf.Sin(time / (2 * alertDeltaTime) * Mathf.PI), alertFrameRect.rect.height);
			alertTextRect.sizeDelta = new Vector2(alertWidth * Mathf.Sin(time / (2 * alertDeltaTime) * Mathf.PI), alertTextRect.rect.height);
			yield return null;
			time += Time.unscaledDeltaTime;
		}
		alertFrameRect.sizeDelta = new Vector2(0, alertFrameRect.rect.height);
		alertTextRect.sizeDelta = new Vector2(0, alertTextRect.rect.height);
		alert.SetActive(false);
	}
}
