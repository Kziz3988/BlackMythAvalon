using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ChestController : MonoBehaviour {
	public int type, index;
	public static bool isRotating = false; //Only one chest can be opened at any time
	public static int openedChest = 0;
	const float deltaTime = 0.5f;
	Transform lid;
	static ObjectPool pool;

	//Print cells to log for debugging
	public static void PrintCells(int index) {
		int width = GUIController.inventorySizes["Chest"].x;
		int height = GUIController.inventorySizes["Chest"].y;
		for(int j = 0; j < height; j++) {
			string items = "";
			for(int i = 0; i < width; i++) {
				items += ItemManager.chestItems[index][i][j].ToString() + " ";
			}
			Debug.Log(items);
		}
	}

	void Start() {
		gameObject.tag = "Chest";
		pool = GameObject.Find("Items").GetComponent<ObjectPool>();
		EventManager.Instance.AddListener("OnPause", OnPaused);
		lid = transform.GetChild(0);
		lid.tag = "Chest";
		//Generate items
		GameObject parent = GameObject.Find("Items");
		int width = GUIController.inventorySizes["Chest"].x;
		int height = GUIController.inventorySizes["Chest"].y;
		for(int i = 0; i < width; i++) {
			List<float> lootTable = PropGenerator.chestItemP[type];
			float rand = GlobalVariables.Instance.GetRandom();
			int j;
			for(j = 0; j < lootTable.Count(); j++) {
				if(rand < lootTable[j]) {
					break;
				}
			}
			Vector2Int pos = new Vector2Int(i, 0);
			if(j < lootTable.Count() && CellManager.CheckEmptyCells(j, pos, index)) { //Generate an item
				GameObject obj = pool.GetObject();
				obj.transform.SetParent(parent.transform);
				ItemManager im = obj.GetComponent<ItemManager>();
				im.index = j;
				im.owner = index;
				im.relPos = pos;
				CellManager.SetItem(j, pos, index);
			}
		}
		//PrintCells(index);
		//Debug.Log("---");
	}

	//Animation of opening or closing chests
	public IEnumerator OpenChest() {
		isRotating = true;
		gameObject.tag = "ChestMoving";
		lid.tag = "ChestMoving";
		float time = 0;
		float startAngle = lid.localEulerAngles.x;
		float targetAngle = 270 - lid.localEulerAngles.x;
		//Debug.Log(targetAngle);
		float deltaAngle = Mathf.Abs(startAngle) < 1 ? -90 : 90;
		while(deltaTime - time > 0) {
			//Trigonometric interpolation
			lid.localEulerAngles = new Vector3(startAngle + deltaAngle * Mathf.Sin(time / (2 * deltaTime) * Mathf.PI), 0, 0);
			yield return null;
			time += Time.deltaTime;
		}
		lid.localEulerAngles = new Vector3(targetAngle, 0, 0);
		isRotating = false;
		if(deltaAngle < 0) {
			Debug.Log("Chest opened: " + index.ToString());
			openedChest = index;
			GameConfig.Pause("chest");
			EventManager.Instance.TriggerEvent("OnChestOpen", index.ToString());
		}
		else {
			openedChest = -1;
			gameObject.tag = "Chest";
			lid.tag = "Chest";
		}
	}

	void OnPaused(string message) {
		if(!isRotating && lid.localEulerAngles.x > 180) {
			//Close the chest when closing inventory
			StartCoroutine(OpenChest());
		}
	}
}
