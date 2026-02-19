using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PropGenerator : MonoBehaviour {
	public static PropGenerator Instance; //The instance for the singleton pattern
    static float floorPropP; //Probability of props on floor

	//Prefabs of floors
	public GameObject[] floorPrefabs = new GameObject[11];
	//Prefab of ceilings
	public GameObject[] ceilingPrefabs = new GameObject[3];
	//Prefabs of walls
	public GameObject[] wallPrefabs = new GameObject[5];
	//Prefabs of doors
	public GameObject[] doorPrefabs = new GameObject[2];
	//Prefabs of doorframes
	public GameObject[] doorframePrefabs = new GameObject[2];
	//Prefabs of chests
	public GameObject[] chestPrefabs = new GameObject[3];
	//Prefabs of props on floor
	public GameObject[] floorPropPrefabs = new GameObject[12];

	//Data of indices of floor prefabs and additional rotation of each cells
	static List<List<Vector2Int>> cellPrefabIndices = new List<List<Vector2Int>>();
	//Data of indices of door prefabs of each cells
	static List<int> doorPrefabIndices = new List<int>();
	//Data of pivots of floor prefabs. This is unnecessary if the models have the same pivot of rotation
	static Dictionary<int, int> floorPivots = new Dictionary<int, int>();
	//Data of position offsets of floor prefabs. This is unnecessary if the models have the correct origin point
	static Dictionary<int, Vector3> floorOffsets = new Dictionary<int, Vector3>();
	//Data of valid floor props and position offsets of floors
	static List<List<KeyValuePair<int, Vector3>>> floorPropData = new List<List<KeyValuePair<int, Vector3>>>();

	public static List<float> chestP; //Probability of chests
    public static List<List<float>> chestItemP = new List<List<float>>(); //Loot table of chests
    public static List<KeyValuePair<float, int>> fillerItemP = new List<KeyValuePair<float, int>>(); //Loot table of 1x1 filler items
	public static Dictionary<int, Dictionary<int, float>> chestData = new Dictionary<int, Dictionary<int, float>>(); //The indices of cells with chests and its reciprocal probability
	public static List<KeyValuePair<float, string>> enemyP = new List<KeyValuePair<float, string>>(); //Probability and name of normal enemies

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
	}

	//Generate walls, doors, floors, ceilings...
	public void GenerateCells() {
		ItemManager.chestItems = new List<List<List<int>>>();
		AddEmptyChest();
		ItemManager.handItems = new List<GameObject>();
		for(int i = 0; i < GUIController.handSlotPositions.Count; i++) {
			ItemManager.handItems.Add(null);
		}
		int chestIndex = 1; //Index 0 is for player's inventory
		ObjectPool enemyPool = GameObject.Find("Enemy").GetComponent<ObjectPool>();
		int enemyIndex = 0;
		for(int floor = 0; floor < DungeonGenerator.Instance.floors; floor++) {
			GameObject floorContainer = new GameObject("Floor " + (floor + 1).ToString());
			for(int x = 0; x < DungeonGenerator.Instance.width; x++) {
				for(int z = 0; z < DungeonGenerator.Instance.height; z++) {
					Vector3Int cellData = DungeonGenerator.dungeon[floor][x][z];
					if(cellData.x > -1) {
						if(DungeonGenerator.stairs.ContainsKey(cellData.x) && cellData.y == DungeonGenerator.stairs[cellData.x].Value) {
							if(DungeonGenerator.stairs[cellData.x].Key) {
								DungeonGenerator.stairPos[floor].Key.Add(new Vector2Int(x, z));
							}
							else {
								DungeonGenerator.stairPos[floor].Value.Add(new Vector2Int(x, z));
							}
						}
						Vector2Int prefabData = cellPrefabIndices[cellData.x][cellData.y];
						Vector3Int pos = new Vector3Int(x, z, floor);
						Vector3 offset = floorOffsets.ContainsKey(prefabData.x) ? floorOffsets[prefabData.x] : Vector3.zero;
						if(prefabData.x > -1) {
							//Clone floors
							int pivot = floorPivots.ContainsKey(prefabData.x) ? floorPivots[prefabData.x] : 0;
							GameObject floorPrefab = floorPrefabs[prefabData.x];
							GameObject floorObj = ClonePrefab(pos, -cellData.z + prefabData.y, pivot, offset, Vector3.zero, floorPrefab, "Cell" + pos.ToString() + ":" + cellData.ToString(), floorContainer);
							floorObj.tag = "Floor";
							if(floor == 0 && GlobalVariables.Instance.startPos.Equals(new Vector2Int(x, z))) {
								//Initialize the position of player
								GameObject.Find("Player").transform.position = floorObj.transform.localPosition + floorObj.transform.localRotation * new Vector3(-2.5f, 0, 2.5f);
							}
							if(cellData.x == DungeonGenerator.FinalBossRoom && cellData.y == 45) {
								//Initialize the position of final boss
								GameObject.Find("Modred").transform.position = floorObj.transform.localPosition + floorObj.transform.localRotation * new Vector3(-2.5f, 0, 2.5f);
							}
						}
						if(floor < DungeonGenerator.Instance.floors - 1) {
							Vector3Int upstairCellData = DungeonGenerator.dungeon[floor + 1][x][z];
							if(upstairCellData.x == -1 || cellPrefabIndices[upstairCellData.x][upstairCellData.y].x > -1) {
								//Clone ceilings
								ClonePrefab(pos, cellData.z, 0, new Vector3(0, 5f, 0), Vector3.zero, ceilingPrefabs[0], "Ceiling" + pos.ToString(), floorContainer);
							}
						}
						else {
							ClonePrefab(pos, cellData.z, 0, new Vector3(0, 5f, 0), Vector3.zero, ceilingPrefabs[0], "Ceiling" + pos.ToString(), floorContainer);
						}
						//Clone walls
						GenerateWall(pos, 0, offset, floorContainer);
						GenerateWall(pos, 1, offset, floorContainer);
						GenerateWall(pos, 2, offset, floorContainer);
						GenerateWall(pos, 3, offset, floorContainer);

						//Clone chests
						if(chestData.ContainsKey(cellData.x) && chestData[cellData.x].ContainsKey(cellData.y) && GlobalVariables.Instance.GetRandom() < chestData[cellData.x][cellData.y]) {
							int i = 0;
							float rand = GlobalVariables.Instance.GetRandom();
							while(i < chestPrefabs.Length - 1) {
								if(rand < chestP[i]) {
									break;
								}
								i++;
							}
							GameObject chest = ClonePrefab(pos, cellData.z + 2, 0, Vector3.zero, new Vector3(-0.5f, 0.5f, 0), chestPrefabs[i], "Chest:" + pos.ToString(), floorContainer);
                            chest.AddComponent<ChestController>();
							ChestController cc = chest.GetComponent<ChestController>();
							cc.type = i;
							cc.index = chestIndex++;
							AddEmptyChest();
						}
						else if(floorPropData[cellData.x].Count > 0 && GlobalVariables.Instance.GetRandom() < floorPropP) {
							//Clone floor props
							int rand = Mathf.FloorToInt(GlobalVariables.Instance.GetRandom(0, floorPropData[cellData.x].Count));
							ClonePrefab(pos, cellData.z + 2, 0, Vector3.zero, floorPropData[cellData.x][rand].Value, floorPropPrefabs[floorPropData[cellData.x][rand].Key], "FloorProp:" + pos.ToString(), floorContainer);
						}
						else {
							//Clone enemies
							float random = GlobalVariables.Instance.GetRandom();
							int i = 0;
							while(i < enemyP.Count && enemyP[i].Key < random) {
								i++;
							}
							if(i < enemyP.Count) {
								GameObject obj = enemyPool.GetObject();
								obj.name = enemyP[i].Value;
								obj.transform.position = GetRotatedPosition(pos, cellData.z + 2, 0, Vector3.zero, new Vector3(-0.5f, 0.5f, 0));
								obj.GetComponent<EnemyController>().index = enemyIndex++;
							}
						}
					}
				}
			}
		}
	}

	//Generate an empty chest list
	void AddEmptyChest() {
		int width = GUIController.inventorySizes["Chest"][0];
		int height = GUIController.inventorySizes["Chest"][1];
		List<List<int>> items = new List<List<int>>();
		for(int i = 0; i < width; i++) {
			items.Add(new List<int>());
			for(int j = 0; j < height; j++) {
				items[i].Add(ItemManager.Empty);
			}
		}
		ItemManager.chestItems.Add(items);
	}

	//Get new position after rotation
	Vector3 GetRotatedPosition(Vector3Int pos, int dir, int pivot, Vector3 initOffset, Vector3 relOffset) {
		Vector3 position = initOffset;
		int normDir = (dir + pivot) < 0 ? ((dir + pivot) % 4 + Mathf.Abs(dir + pivot) * 4) % 4 : (dir + pivot) % 4;
		switch(normDir) {
			case 0:
				position += new Vector3((pos.x + relOffset.x) * 5, (pos.z + relOffset.z) * 5, (pos.y + relOffset.y) * 5);
				break;
			case 1:
				position += new Vector3((pos.x + relOffset.y - 1) * 5, (pos.z + relOffset.z) * 5, (pos.y - relOffset.x) * 5);
				break;
			case 2:
				position += new Vector3((pos.x - relOffset.x - 1) * 5, (pos.z + relOffset.z) * 5, (pos.y - relOffset.y + 1) * 5);
				break;
			case 3:
				position += new Vector3((pos.x - relOffset.y) * 5, (pos.z + relOffset.z) * 5, (pos.y + relOffset.x + 1) * 5);
				break;
		}
		return position;
	}

	//Instantiate a prefab
	GameObject ClonePrefab(Vector3Int pos, int dir, int pivot, Vector3 initOffset, Vector3 relOffset, GameObject prefab, string name, GameObject parent) {
		GameObject obj = Instantiate(prefab, GetRotatedPosition(pos, dir, pivot, initOffset, relOffset), Quaternion.Euler(0, dir * 90, 0));
		obj.name = name;
		obj.transform.SetParent(parent.transform);
		obj.isStatic = true;
		return obj;
	}

	//Instantiate a prefab of wall
	void GenerateWall(Vector3Int pos, int dir, Vector3 offset, GameObject parent) {
		if(!DungeonGenerator.wallDir[pos.z][pos.x][pos.y][dir]) {
			//Generate a wall
			int index = pos.x * DungeonGenerator.Instance.width + pos.y;
			GameObject obj = ClonePrefab(pos, dir, 0, offset, Vector3.zero, wallPrefabs[GlobalVariables.Instance.GetSeedSegment(index % 32, 1) % wallPrefabs.Length], "Wall" + pos.ToString() + ":" + dir.ToString(), parent);
			obj.AddComponent<BoxCollider>();
		}
		else {
			int cellIndex = DungeonGenerator.dungeon[pos.z][pos.x][pos.y].x;
			int doorPrefabIndex = doorPrefabIndices[cellIndex];
			if(DungeonGenerator.doorDir[pos.z][pos.x][pos.y][dir]) {
				//Generate a wall with a door
				ClonePrefab(pos, dir, 0, offset, Vector3.zero, doorframePrefabs[doorPrefabIndex], "Wall" + pos.ToString() + ":" + dir.ToString(), parent);
				ClonePrefab(pos, dir, 0, Vector3.zero, new Vector3(-0.5f, 0, 0), doorPrefabs[doorPrefabIndex], "Door" + pos.ToString() + ":" + dir.ToString(), parent).AddComponent<DoorController>();
				Vector3Int antiPos = pos + new Vector3Int((dir - 2) * (dir % 2), (dir - 1) * ((dir + 1) % 2), 0);
				int antiDir = (dir + 2) % 4;
				ClonePrefab(antiPos, antiDir, 0, offset, Vector3.zero, doorframePrefabs[doorPrefabIndex], "Wall" + antiPos.ToString() + ":" + antiDir.ToString(), parent);
			}
		}
	}

	//Initialize prefab data
	static PropGenerator() {
		//To avoid float precision problems:
		//If the probability of a situation is 0, its value shouble one less than the previous (for 0 it is -1)
		//And if it is need to ensure coverage of all situations (the sum of probabilities is 1), then the last value should be 2
        floorPropP = 0.8f;
		chestP = new List<float>{0.5f, 0.8f, 2}; //50% Common chest, 30% Uncommon chest, 20% Rare chest
        chestItemP.Add(new List<float>{0.2f, 0.275f, 0.3f, 0.35f, 0.425f, 0.5f}); //Common chest: 20% Sword, 7.5% Greatsword, 2.5% Hammer, 5% Dwarf sword, 7.5% Spear, 7.5% Axe
        chestItemP.Add(new List<float>{0.15f, 0.3f, 0.35f, 0.45f, 0.575f, 0.7f}); //Uncommon chest: 15% Sword, 15% Greatsword, 5% Hammer, 10% Dwarf sword, 12.5% Spear, 12.5% Axe
        chestItemP.Add(new List<float>{-1, 0.2f, 0.3f, 0.5f, 0.7f, 0.9f}); //Rare chest: 20% Greatsword, 10% Hammer, 20% Dwarf sword, 20% Spear, 20% Axe
        fillerItemP.Add(new KeyValuePair<float, int>(0.35f, 6)); //35% Apple

		enemyP.Add(new KeyValuePair<float, string>(0.2f, "Skeleton")); //Currently only one enemy (Skeleton) has been installed

		chestData.Add(0, new Dictionary<int, float>{{0, 0.2f}});
        chestData.Add(2, new Dictionary<int, float>{{0, 0.3f}, {1, 0.3f}});
        chestData.Add(3, new Dictionary<int, float>{{0, 2}});
        chestData.Add(4, new Dictionary<int, float>{{4, 2}});
		
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(8, 0)
		}); //Room 0
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(0, 0)
		}); //Room 1
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(6, 0), new Vector2Int(6, 0)
		}); //Room 2
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(8, 0)
		}); //Room 3
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(3, 2), new Vector2Int(5, 2), new Vector2Int(3, 3), new Vector2Int(3, 1), new Vector2Int(5, 0), new Vector2Int(3, 0)
		}); //Room 4
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(0, 0)
		}); //Room 5
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(3, 2), new Vector2Int(5, 2), new Vector2Int(3, 3), new Vector2Int(5, 1), new Vector2Int(4, 0), new Vector2Int(5, 3), new Vector2Int(3, 1), new Vector2Int(5, 0), new Vector2Int(3, 0)
		}); //Room 6
		List<Vector2Int> cellPrefabIndex = new List<Vector2Int>{
            new Vector2Int(3, 2)
        };
		for(int i = 0; i < 5; i++) {
			cellPrefabIndex.Add(new Vector2Int(5, 2));
		}
		cellPrefabIndex.Add(new Vector2Int(3, 3));
		for(int i = 0; i < 5; i++) {
			cellPrefabIndex.Add(new Vector2Int(5, 1));
			for(int j = 0; j < 5; j++) {
				cellPrefabIndex.Add(new Vector2Int(4, 0));
			}
			cellPrefabIndex.Add(new Vector2Int(5, 3));
		}
		cellPrefabIndex.Add(new Vector2Int(3, 1));
		for(int i = 0; i < 5; i++) {
			cellPrefabIndex.Add(new Vector2Int(5, 0));
		}
		cellPrefabIndex.Add(new Vector2Int(3, 0));
		cellPrefabIndices.Add(cellPrefabIndex); //Room 7
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(0, 0), new Vector2Int(9, 3), new Vector2Int(10, 3), new Vector2Int(-1, 0)
		}); //Room 8
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(-1, 0), new Vector2Int(-1, 0), new Vector2Int(-1, 0), new Vector2Int(0, 0)
		}); //Room 9
		cellPrefabIndices.Add(new List<Vector2Int>{
			new Vector2Int(0, 0)
		}); //Room 10

		doorPrefabIndices = new List<int>{0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0};
		floorPivots = new Dictionary<int, int>{
			{9, 2}, {10, 2}
		};
		floorOffsets = new Dictionary<int, Vector3>{
			{10, new Vector3(0, 2.5f, 0)}
		};

		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(0, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(2, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(5, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(6, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(8, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(9, new Vector3(-0.5f, 0.5f, 0)),
		}); //Room 0
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(2, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(10, new Vector3(-0.45f, 0.5f, 0)),
		}); //Room 1
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(0, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(2, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(5, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(6, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(8, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(9, new Vector3(-0.5f, 0.5f, 0)),
		}); //Room 2
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>()); //Room 3
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(0, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(2, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(5, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(6, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(8, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(9, new Vector3(-0.5f, 0.5f, 0)),
		}); //Room 4
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(10, new Vector3(-0.45f, 0.15f, 0)),
		}); //Room 5
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>{
			new KeyValuePair<int, Vector3>(0, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(1, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(2, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(3, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(4, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(5, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(6, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(7, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(8, new Vector3(-0.5f, 0.5f, 0)),
			new KeyValuePair<int, Vector3>(9, new Vector3(-0.5f, 0.5f, 0)),
		}); //Room 6
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>()); //Room 7
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>()); //Room 8
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>()); //Room 9
		floorPropData.Add(new List<KeyValuePair<int, Vector3>>()); //Room 10
	}
}
