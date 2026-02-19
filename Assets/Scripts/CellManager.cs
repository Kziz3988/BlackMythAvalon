using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class CellManager : MonoBehaviour {
	public string type; //The type of the current cell
	public Vector2Int position;

	void Start() {
		GetComponent<RectTransform>().anchoredPosition3D = new Vector3(position.x, position.y, 0);
	}

	//Check if the cells are empty
	public static bool CheckEmptyCells(int item, Vector2Int pos, int chest) {
		List<List<int>> cells = ItemManager.chestItems[chest];
		int width = (int)ItemManager.itemData[item][ItemManager.Width];
		int height = (int)ItemManager.itemData[item][ItemManager.Height];
		bool flag = true;
		for(int i = pos.x; i < pos.x + width; i++) {
			for(int j = pos.y; j < pos.y + height; j++) {
				if(i < 0 || i >= cells.Count || j < 0 || j >= cells[0].Count || cells[i][j] != ItemManager.Empty) {
					flag = false;
					break;
				}
			}
		}
		return flag;
	}

	//Put item into cells
	public static void SetItem(int item, Vector2Int pos, int chest) {
		if(chest == ItemManager.Player) {
			GlobalVariables.Instance.playerItemCount++;
			EventManager.Instance.TriggerEvent("OnItemGet", GlobalVariables.Instance.playerItemCount.ToString());
		}
		List<List<int>> cells = ItemManager.chestItems[chest];
		int width = (int)ItemManager.itemData[item][ItemManager.Width];
		int height = (int)ItemManager.itemData[item][ItemManager.Height];
		//Debug.Log("item position:"+pos.ToString());
		//Debug.Log("item size:"+new Vector2Int(width, height).ToString());
		for(int i = pos.x; i < pos.x + width; i++) {
			for(int j = pos.y; j < pos.y + height; j++) {
				cells[i][j] = item;
			}
		}
		//Debug.Log("added item: " + GlobalVariables.Instance.itemName[item].Key);
		//ChestController.PrintCells(chest);
	}

	//Remove item from cells
	public static void RemoveItem(int item, Vector2Int pos, int chest) {
		if(chest == ItemManager.Player) {
			GlobalVariables.Instance.playerItemCount--;
		}
		List<List<int>> cells = ItemManager.chestItems[chest];
		int width = (int)ItemManager.itemData[item][ItemManager.Width];
		int height = (int)ItemManager.itemData[item][ItemManager.Height];
		for(int i = pos.x; i < pos.x + width; i++) {
			for(int j = pos.y; j < pos.y + height; j++) {
				cells[i][j] = ItemManager.Empty;
			}
		}
		//Debug.Log("removed item: " + GlobalVariables.Instance.itemName[item].Key);
		//ChestController.PrintCells(chest);
	}
}
