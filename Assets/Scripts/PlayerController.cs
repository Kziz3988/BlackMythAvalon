using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour {
	//Move states
	const int Idle = -1;
	const int Walking = 0;
	const int Running = 1;
	//Attack states
	const int LeftHandSlash = 0;
	const int RightHandSlash = 1;
	const int DoubleSlash = 2;
	const int TwoHandsSlash = 3;
	const int SpearStab = 4;
	const int AxeSlash = 5;

	float[] moveSpeed = new float[2]{2f, 5f};
	int moveState;
	public bool isJumping;
	public bool isAttacking;
	int attackState;
	bool isTwoHanded; //Whether player is holding a two-handed weapon
	float mouseSense = 5f; //Mouse sensitivity
	float reachDist = 3f; //The max distance from which player can interact with objects
    public GameObject interactObj; //The game object currently being watched by player
	public Dictionary<string, GameObject> weapons = new Dictionary<string, GameObject>(); //Weapons of player
	public string npcTalkingTo; //The NPC in conversation with player
	public string bossFighting; //The boss player is fighting against now
	GameObject interactBar;
	public Camera playerCamera;
	Dictionary<string, string> interactText = new Dictionary<string, string>();
	Vector3Int lastRelPos;

	void Awake() {
		interactText.Add("DoorClosed", "单击鼠标开门");
		interactText.Add("DoorOpened", "单击鼠标关门");
		interactText.Add("Chest", "单击鼠标打开箱子");
		interactText.Add("NPC", "单击鼠标对话");
	}

	void Start() {
		GameConfig.isPaused = true;
		interactBar = GameObject.Find("Interaction");
		Transform leftHand = GameObject.Find("Player_Hand_L").transform;
		for(int i = 0; i < leftHand.childCount; i++) {
			GameObject obj = leftHand.GetChild(i).gameObject;
			if(obj.name.StartsWith("Wep_")) {
				weapons.Add(obj.name, obj);
				obj.SetActive(false);
			}
		}
		Transform rightHand = GameObject.Find("Player_Hand_R").transform;
		for(int i = 0; i < rightHand.childCount; i++) {
			GameObject obj = rightHand.GetChild(i).gameObject;
			if(obj.name.StartsWith("Wep_")) {
				weapons.Add(obj.name, obj);
				obj.SetActive(false);
			}
		}
		playerCamera = GameObject.Find("PlayerCamera").GetComponent<Camera>();
		Initialize();
		Pause(true, "escape"); //Resume the game
		GameObject.Find("Bedivere").transform.position = transform.position + new Vector3(1.5f, 0, 1.5f);
	}
	
	void Update() {
		Pause(Input.GetKeyDown(KeyCode.Escape), "escape");
		Pause(Input.GetKeyDown(KeyCode.X) && !isAttacking, "inventory");
		Rotate();
		Move();
		OpenDoor();
		OpenChest();
		Attack();
		PlayAnim();
		SetTwoHandedState();
		if(Input.GetKeyDown(KeyCode.M)) {
			GameConfig.isNavigationEnabled = !GameConfig.isNavigationEnabled;
		}
		/*if(Input.GetKeyDown(KeyCode.Z)) {
			TaskManager.Instance.SkipTask();
		}*/
	}

	void Initialize() {
		moveState = Idle;
		isJumping = false;
		isAttacking = false;
		isTwoHanded = false;
		StatusSlider ss = GetComponent<StatusSlider>();
		ss.hpMax = GlobalVariables.Instance.playerHPMax;
		ss.hp = GlobalVariables.Instance.playerHPMax;
		ss.armorMax = GlobalVariables.Instance.playerArmorMax;
		ss.armor = GlobalVariables.Instance.playerArmorMax;
		npcTalkingTo = "";
		bossFighting = "";
		interactBar.SetActive(false);
		lastRelPos = Navigator.Instance.GetRelativePosition(transform.position);
	}

	//Pause and resume game
	void Pause(bool condition, string type) {
		if(condition) {
			GameConfig.Pause(type);
		}
	}

	//Rotate based on mouse pointer
	void Rotate() {
		if(!GameConfig.isPaused) {
			float mouseX = Input.GetAxis("Mouse X") * mouseSense;
			float mouseY = Input.GetAxis("Mouse Y") * mouseSense;
			transform.Rotate(Vector3.up, mouseX); //Rotate horizontally
			Transform cam = playerCamera.transform;
			cam.Rotate(-Vector3.right, mouseY); //Rotate vertically
			float xRot = cam.localEulerAngles.x > 180 ? cam.localEulerAngles.x - 360 : cam.localEulerAngles.x;
			cam.localEulerAngles = new Vector3(Mathf.Clamp(xRot, -80, 60), 0, 0);
			Ray sight = playerCamera.ScreenPointToRay(Input.mousePosition);
			RaycastHit hit;
			if(Physics.Raycast(sight, out hit) && Vector3.Distance(transform.position, hit.collider.gameObject.transform.position) - reachDist < 0) {
				interactObj = hit.collider.gameObject;
				if(interactText.ContainsKey(interactObj.tag)) {
					interactBar.SetActive(true);
					interactBar.transform.Find("InteractionText").GetComponent<Text>().text = interactText[interactObj.tag];
				}
				else {
					interactBar.SetActive(false);
				}
			}
			else {
				interactObj = null;
				interactBar.SetActive(false);
			}
		}
	}

	//Move based on keyboard input
	void Move() {
		if(!GameConfig.isPaused && !isAttacking) {
			//Jump
			if(Input.GetKeyDown(KeyCode.Space) && !isJumping) {
				EventManager.Instance.TriggerEvent("OnJump", null);
				isJumping = true;
				GetComponent<Rigidbody>().AddForce(0, 300f, 0);
			}

			float moveX = Input.GetAxis("Horizontal");
			float moveZ = Input.GetAxis("Vertical");
			if(Mathf.Abs(moveX - 0) < 0.01 && (Mathf.Abs(moveZ - 0) < 0.01)) {
				moveState = Idle;
			}
			else {
				EventManager.Instance.TriggerEvent("OnMove", null);
				if(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift)) {
					moveState = Running;
				}
				else {
					moveState = Walking;
				}
				transform.Translate(new Vector3(moveX, 0, moveZ) * moveSpeed[moveState] * Time.deltaTime);
				Vector3Int curRelPos = Navigator.Instance.GetRelativePosition(transform.position);
				if(!curRelPos.Equals(lastRelPos)) {
					lastRelPos = curRelPos;
					EventManager.Instance.TriggerEvent("OnNavigate", null);
				}
			}
		}
	}

	//Open or close doors
	void OpenDoor() {
		if(!GameConfig.isPaused && !isAttacking) {
			if(interactObj != null && interactObj.name.Contains("Door")) {
				DoorController dc = interactObj.GetComponent<DoorController>();
				if(Input.GetMouseButtonDown(0) && !dc.isRotating) {
					dc.StartCoroutine(dc.RotateDoor());
				}
			}
		}
	}

	//Open chests
	void OpenChest() {
		if(!GameConfig.isPaused && !isAttacking) {
			if(interactObj != null && interactObj.name.Contains("Chest")) {
				GameObject chest = interactObj.name.Contains("Lid") ? interactObj.transform.parent.gameObject : interactObj;
				ChestController cc = chest.GetComponent<ChestController>();
				if(Input.GetMouseButtonDown(0) && !ChestController.isRotating) {
					cc.StartCoroutine(cc.OpenChest());
				}
			}
		}
	}

	//Attack
	void Attack() {
		if(!GameConfig.isPaused) {
			List<GameObject> handItems = ItemManager.handItems;
			if(Input.GetKey(KeyCode.E) && !isJumping && !isAttacking && (handItems[GUIController.LeftHand] != null || handItems[GUIController.RightHand] != null)) {
				//Set attack state
				isAttacking = true;
				int weapon = SetTwoHandedState();
				if(isTwoHanded) {
					switch(weapon) {
						case ItemManager.Spear:
							attackState = SpearStab;
							break;
						case ItemManager.Axe:
							attackState = AxeSlash;
							break;
						default:
							attackState = TwoHandsSlash;
							break;
					}
				}
				else if(handItems[GUIController.LeftHand] == null) {
					attackState = RightHandSlash;
				}
				else if(handItems[GUIController.RightHand] == null) {
					attackState = LeftHandSlash;
				}
				else {
					attackState = DoubleSlash;
				}
				string leftName = GetWeaponName(GUIController.LeftHand);
				if(leftName != null) {
					weapons[leftName].GetComponent<MeshCollider>().enabled = true;
				}
				string rightName = GetWeaponName(GUIController.LeftHand);
				if(rightName != null) {
					weapons[rightName].GetComponent<MeshCollider>().enabled = true;
				}
			}
		}
	}

	//Play animations of player
	void PlayAnim() {
		Animator anim = GetComponent<Animator>();
		anim.SetBool("isJumping", isJumping);
		anim.SetInteger("moveState", moveState);
		anim.SetBool("isAttacking", isAttacking);
		anim.SetInteger("attackState", attackState);
		anim.SetBool("isTwoHanded", isTwoHanded);
	}

	//Get gameobject name of weapon in hand slots
	public static string GetWeaponName(int slot) {
		if(ItemManager.handItems[slot] != null) {
			int index = ItemManager.handItems[slot].GetComponent<ItemManager>().index;
			string name = ItemManager.itemName[index].Key;
			return ItemManager.itemTwoHanded[index] ? "Wep_" + name : "Wep_" + name + "_" + slot.ToString();
		}
		else {
			return null;
		}
	}

	//Determine whether player is holding a two-handed weapon
	int SetTwoHandedState() {
		bool isLeftHandEmpty = ItemManager.handItems[GUIController.LeftHand] == null;
		bool isRightHandEmpty = ItemManager.handItems[GUIController.RightHand] == null;
		bool isLeftTwoHanded = !isLeftHandEmpty && ItemManager.itemTwoHanded[ItemManager.handItems[GUIController.LeftHand].GetComponent<ItemManager>().index];
		bool isRightTwoHanded = !isRightHandEmpty && ItemManager.itemTwoHanded[ItemManager.handItems[GUIController.RightHand].GetComponent<ItemManager>().index];
		isTwoHanded = isLeftTwoHanded || isRightTwoHanded;
		if(!isLeftHandEmpty) {
			return ItemManager.handItems[GUIController.LeftHand].GetComponent<ItemManager>().index;
		}
		else if(!isRightHandEmpty) {
			return ItemManager.handItems[GUIController.RightHand].GetComponent<ItemManager>().index;
		}
		else {
			return ItemManager.Empty;
		}
	}

	//Get weapons just attack
	List<GameObject> GetWeapons() {
		switch(attackState) {
			case LeftHandSlash:
				return new List<GameObject>{
					ItemManager.handItems[GUIController.LeftHand]
				};
			case RightHandSlash:
				return new List<GameObject>{
					ItemManager.handItems[GUIController.RightHand]
				};
			case DoubleSlash:
				return new List<GameObject>{
					ItemManager.handItems[GUIController.LeftHand],
					ItemManager.handItems[GUIController.RightHand]
				};
			case TwoHandsSlash:
				return new List<GameObject>{
                    ItemManager.handItems[GUIController.LeftHand] ?? ItemManager.handItems[GUIController.RightHand]
                };
			case SpearStab:
				return new List<GameObject>{
                    ItemManager.handItems[GUIController.LeftHand] ?? ItemManager.handItems[GUIController.RightHand]
                };
			case AxeSlash:
				return new List<GameObject>{
                    ItemManager.handItems[GUIController.LeftHand] ?? ItemManager.handItems[GUIController.RightHand]
                };
		}
		return new List<GameObject>();
	}

	//Called after jump animation is played
	public void FinishJump() {
		isJumping = false;
	}

	//Called after attack animation is played
	public void FinishAttack() {
		isAttacking = false;
		GetComponent<Animator>().SetBool("isAttacking", isAttacking);
		List<GameObject> attacked = GetWeapons();
		foreach(GameObject weapon in attacked) {
			weapon.GetComponent<ItemManager>().Use();
		}
		string leftName = GetWeaponName(GUIController.LeftHand);
		if(leftName != null) {
			weapons[leftName].GetComponent<MeshCollider>().enabled = false;
		}
		string rightName = GetWeaponName(GUIController.LeftHand);
		if(rightName != null) {
			weapons[rightName].GetComponent<MeshCollider>().enabled = false;
		}
		EventManager.Instance.TriggerEvent("OnAttack", null);
	}

	void OnTriggerEnter(Collider other) {
		if(isAttacking && (other.gameObject.tag == "Enemy" || other.gameObject.tag == "Boss") && other.gameObject.GetComponent<EnemyController>().isActive) {
			List<GameObject> weapons = GetWeapons();
			StatusSlider ss = other.gameObject.GetComponent<StatusSlider>();
			List<List<float>> data = ItemManager.itemData;
			foreach(GameObject weapon in weapons) {
				int index = weapon.GetComponent<ItemManager>().index;
				ss.Injure(data[index][ItemManager.DamageToHP], data[index][ItemManager.DamageToArmor], data[index][ItemManager.Penetration], transform);
			}
		}
	}
}
