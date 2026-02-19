using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour {
	public static DungeonGenerator Instance; //The instance for the singleton pattern

	//Room data indices
	public const int RoomWidth = 0;
	public const int RoomHeight = 1;
	public const int AdjacentRoomOffsetX = 2;
	public const int AdjacentRoomOffsetZ = 3;
	public const int AdjacentRoomType = 4;
	public const int UpstairRoomType = 5;
	
	//Room type indices
	public const int NormalRoom = 0;
	public const int NormalCorridor = 1;
	public const int LongRoom = 2;
	public const int SmallTreasureRoom = 3;
	public const int BigTreasureRoom = 4;
	public const int OneDoorCorridor = 5;
	public const int BossRoom = 6;
	public const int FinalBossRoom = 7;
	public const int UpwardStairs = 8;
	public const int DownwardStairs = 9;
	public const int FinalBossCorridor = 10;

	public int width; //Num of cells in the x direction
    public int height; //Num of cells in the z direction
    public int floors; //Num of floors in the dungeon
	float excessDoorP; //Probability of excess doors
	public static List<List<int>> roomData = new List<List<int>>(); //Size (0, 1), the adjacent room's relative pos and type (2, 3, 4) and upstair (5) of rooms
    public static List<Dictionary<int, int>> roomNum = new List<Dictionary<int, int>>(); //Num of rooms on each floor
    public static List<HashSet<int>> doorData = new List<HashSet<int>>(); //Whether each cell can have doors
    public static HashSet<int> corridors; //Whether the room can be passed when the A-Star Algorithm is used to generate the dungeon
    public static Dictionary<int, KeyValuePair<bool, int>> stairs = new Dictionary<int, KeyValuePair<bool, int>>(); //Whether the room is stairs, whether it is upward stairs, and its platform index
	public static List<List<List<Vector3Int>>> dungeon = new List<List<List<Vector3Int>>>(); //Types, indices and directions of cells in the dungeon
    public static List<List<List<bool[]>>> wallDir = new List<List<List<bool[]>>>(); //Direction of walls in each room (True = no walls, False = a wall/a door)
    public static List<List<List<bool[]>>> doorDir = new List<List<List<bool[]>>>(); //Direction of walls in each room (True = a door, False = a wall/no walls)
	public static List<KeyValuePair<List<Vector2Int>, List<Vector2Int>>> stairPos; //Position of upward  and downward stairs

	static DungeonGenerator() {
		//The area of room upstairs should be included in which of the current room.
        //Otherwise it is possible that a legal generation of the dungeon cannot be found.
        //The room upstairs should not have its own adjacent rooms or rooms upstair.
        roomData.Add(new List<int>{1, 1, 0, 0, -1, -1}); //Room 0: Normal room
        roomData.Add(new List<int>{1, 1, 0, 0, -1, -1}); //Room 1: Normal corridor
        roomData.Add(new List<int>{1, 2, 0, 0, -1, -1}); //Room 2: Long room
        roomData.Add(new List<int>{1, 1, 0, 1, 5, -1}); //Room 3: Small treasure room
        roomData.Add(new List<int>{3, 2, 1, 1, 5, -1}); //Room 4: Big treasure room
        roomData.Add(new List<int>{1, 1, 0, 0, -1, -1}); //Room 5: One-door room corridor
        roomData.Add(new List<int>{3, 3, 1, 1, 5, -1}); //Room 6: Boss room
        roomData.Add(new List<int>{7, 7, 3, 1, 10, -1}); //Room 7: Final boss room
        roomData.Add(new List<int>{4, 1, 0, 0, -1, 9}); //Room 8: Upward stairs
        roomData.Add(new List<int>{4, 1, 0, 0, -1, -1}); //Room 9: Downward stairs
        roomData.Add(new List<int>{1, 1, 0, 0, -1, -1}); //Room 10: Final boss corridor

        roomNum.Add(new Dictionary<int, int>{
            {0, 10}, {2, 4}, {3, 5}, {4, 2}, {8, 3}
        }); //Floor 0: Initial floor
        roomNum.Add(new Dictionary<int, int>{
            {0, 10}, {2, 4}, {3, 3}, {4, 3}, {6, 1}, {8, 2}
        }); //Floor 1: Intermediate floor
        roomNum.Add(new Dictionary<int, int>{
            {0, 10}, {2, 4}, {3, 2}, {4, 1}, {6, 2}, {7, 1}
        }); //Floor 2: Final boss floor
        roomNum.Add(new Dictionary<int, int>{
            {4, 10}
        });
        roomNum.Add(new Dictionary<int, int>{
            {4, 4}
        });
        roomNum.Add(new Dictionary<int, int>{
            {4, 4}, {0, 10}
        });

		doorData.Add(new HashSet<int>{0});
        doorData.Add(new HashSet<int>{0});
        doorData.Add(new HashSet<int>{0, 1});
        doorData.Add(new HashSet<int>());
        doorData.Add(new HashSet<int>{1});
        doorData.Add(new HashSet<int>{0});
        doorData.Add(new HashSet<int>{1});
        doorData.Add(new HashSet<int>{3});
        doorData.Add(new HashSet<int>{0});
        doorData.Add(new HashSet<int>{3});
        doorData.Add(new HashSet<int>{0});

        corridors = new HashSet<int>{1, 5, 10};
		stairs.Add(8, new KeyValuePair<bool, int>(true, 0)); //Room 8 is upward stairs
		stairs.Add(9, new KeyValuePair<bool, int>(false, 3)); //Room 9 is doward stairs
	}

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
		floors = 3;
		excessDoorP = 0.2f;

		//Wait for tasks are initialized to update task places
		EventManager.Instance.AddListener("OnTaskBegin", OnTaskInit);
	}

	//Generate a list of door data for rooms
    List<bool> GenerateDoorInfo(List<bool> sublist, bool value, int count) {
        List<bool> list = new List<bool>(); //Do not modify the value of a reference parameter
        list.AddRange(sublist);
        list.AddRange(Enumerable.Repeat(value, count).ToList());
        return list;
    }

	//Initialize the dungeon
	void InitDungeon() {
        dungeon = new List<List<List<Vector3Int>>>();
        wallDir = new List<List<List<bool[]>>>();
        doorDir = new List<List<List<bool[]>>>();
		stairPos = new List<KeyValuePair<List<Vector2Int>, List<Vector2Int>>>();
		for(int i = 0; i < floors; i++) {
			stairPos.Add(new KeyValuePair<List<Vector2Int>, List<Vector2Int>>(new List<Vector2Int>(), new List<Vector2Int>()));
		}
    }

	void Start() {
		Initialize();
	}

	void Initialize() {
		InitDungeon(); //1. Initialize the dungeon
		GenerateRooms(); //2. Generate specific rooms
		GenerateWalls(); //3. Generate corridors, walls and doors
		GetComponent<PropGenerator>().GenerateCells(); //4. Generate props
	}

	//Generate position and rotation of rooms
	void GenerateRooms() {
		//X and Z range of the dungeon
		int xMin = 0, xMax = 0, zMin = 0, zMax = 0;
		//Types, positions and directions of the rooms
		List<List<KeyValuePair<int, Vector3Int>>> rooms = new List<List<KeyValuePair<int, Vector3Int>>> {
            new List<KeyValuePair<int, Vector3Int>>()
        };
		bool choseStart = false; //Whether the initial position of player has been chosen
		for(int floor = 0; floor < floors; floor++) {
			//Initialize rooms upstairs
			rooms.Add(new List<KeyValuePair<int, Vector3Int>>());
			//Get the list of rooms to be generate
			List<int> roomTypes = ShuffleRooms(roomNum[floor]);
			//Generate rooms
			List<KeyValuePair<int, Vector3Int>> curFloorRooms = rooms[floor];
			for(int i = 0; i < roomTypes.Count; i++) {
                int offsetDir = i % 4;
				Vector2Int curRoomSize = GetRoomSize(roomTypes[i]);
				int curRoomOffsetX = GlobalVariables.Instance.GetSeedSegment(i % 31, 1) / 4 + 2;
				int curRoomOffsetY = GlobalVariables.Instance.GetSeedSegment((i - 64) % 31, 1) / 4 + 2;
				Vector2Int curRoomRelPos = new Vector2Int(curRoomOffsetX * (offsetDir % 2) - curRoomOffsetY * (1 - offsetDir % 2), curRoomOffsetX * (1 - offsetDir % 2) + curRoomOffsetY * (offsetDir % 2));
				int curRoomDir = GlobalVariables.Instance.GetSeedSegment(i % 31, 1) % 4;
				Vector2Int curRoomPos = curRoomRelPos;
				//Traverse positions of rooms to be generated from inside out, making the dungeon as compact as possible
				int pointer = 0;
				while(pointer < curFloorRooms.Count) {
                    curRoomPos = GetRoomAbsPos(curRoomSize, curRoomRelPos, curRoomDir, offsetDir, curFloorRooms[pointer]);
                    //Check if rooms intersect
                    bool flag = CheckAllIntersections(curRoomPos, RotateRoom(curRoomSize, curRoomDir), curFloorRooms);
                    if (flag) { //There are some intersecting rooms
						//There must be a valid position for the new room, just keep searching
						offsetDir = (offsetDir + 1) % 4; //Try to rotate position offset first
						if(offsetDir == i % 4) { //Already go round
							pointer++; //Move outwards
						}
					}
					else { //Check if adjacent rooms intersect
						int adjacentRoom = roomData[roomTypes[i]][AdjacentRoomType];
						if(adjacentRoom > -1) { //Check if the adjacent room intersects with any existing room
							Vector2Int adjRoomSize = GetRoomSize(adjacentRoom);
							Vector2Int adjRoomPos = GetAdjacentRoomPos(roomTypes[i], curRoomPos, curRoomDir);
							flag = CheckAllIntersections(adjRoomPos, RotateRoom(adjRoomSize, curRoomDir), curFloorRooms);
							if(flag) { //There are some intersecting rooms
								offsetDir = (offsetDir + 1) % 4;
								if(offsetDir == i % 4) {
									pointer++;
								}
							}
							else { //A valid position has been found
								break;
							}
						}
						else { //A valid position has been found and there is no adjacent rooms
							break;
						}
					}
				}
				//Add the new room to the stack
				KeyValuePair<int, Vector3Int> curRoomData = new KeyValuePair<int, Vector3Int>(roomTypes[i], new Vector3Int(curRoomPos.x, curRoomPos.y, curRoomDir));
				rooms[floor].Add(curRoomData);
				UpdateDungeonRange(ref xMin, ref xMax, ref zMin, ref zMax, GetRoomRange(curRoomSize, curRoomPos, curRoomDir));

				//Add adjacent rooms if there are any
				//Adjacent rooms are not included in walk steps
				int adjRoom = roomData[roomTypes[i]][AdjacentRoomType];
				if(adjRoom > -1) {
					Vector2Int adjRoomSize = GetRoomSize(adjRoom);
					Vector2Int adjRoomPos = GetAdjacentRoomPos(roomTypes[i], curRoomPos, curRoomDir);
					KeyValuePair<int, Vector3Int> adjRoomData = new KeyValuePair<int, Vector3Int>(adjRoom, new Vector3Int(adjRoomPos.x, adjRoomPos.y, curRoomDir));
					rooms[floor].Add(adjRoomData);
					UpdateDungeonRange(ref xMin, ref xMax, ref zMin, ref zMax, GetRoomRange(adjRoomSize, adjRoomPos, curRoomDir));
				}

				//Add room upstairs if there are any
				//There is no need to update the range of the dungeon according to the config rule
				int upstairRoom = roomData[roomTypes[i]][UpstairRoomType];
				if(upstairRoom > -1) {
					//The position and direction of the upstair room are same as which of the current room
					KeyValuePair<int, Vector3Int> upstairData = new KeyValuePair<int, Vector3Int>(upstairRoom, curRoomData.Value);
					rooms[floor + 1].Add(upstairData);
				}

				//Update the initial position of player
				if(floor == 0 && !choseStart && roomTypes[i] == SmallTreasureRoom) { //Select the first room 3
					GlobalVariables.Instance.startPos = new Vector2Int(curRoomPos.x, curRoomPos.y);
					choseStart = true;
				}
			}
		}
		xMin -= 1;
		zMin -= 1;
		xMax += 1;
		zMax += 1;
		//Translate coordinate according to x and z range
		Vector2Int posOffset = new Vector2Int(-xMin, -zMin);
		width = xMax - xMin;
		height = zMax - zMin;
		GlobalVariables.Instance.startPos += posOffset;
		Debug.Log("size:("+width.ToString()+","+height.ToString()+")");
		for(int floor = 0; floor < floors; floor++) {
			dungeon.Add(new List<List<Vector3Int>>());
			wallDir.Add(new List<List<bool[]>>());
			doorDir.Add(new List<List<bool[]>>());
			for(int x = 0; x < width; x++) {
				dungeon[floor].Add(new List<Vector3Int>());
				wallDir[floor].Add(new List<bool[]>());
				doorDir[floor].Add(new List<bool[]>());
				for(int z = 0; z < height; z++) {
					//Initialize all rooms as corridors
					dungeon[floor][x].Add(new Vector3Int(-1, 0, 0));
					wallDir[floor][x].Add(new bool[4]{false, false, false, false});
					doorDir[floor][x].Add(new bool[4]{false, false, false, false});
				}
			}
			//Update cell data to the dungeon list
			foreach(KeyValuePair<int, Vector3Int> room in rooms[floor]) {
				Vector2Int size = RotateRoom(GetRoomSize(room.Key), room.Value.z);
				Vector2Int pos = new Vector2Int(room.Value.x, room.Value.y) + posOffset;
				//Debug.Log(room.Key.ToString()+":"+pos.ToString()+","+size.ToString());
				for(int x = 0; x < size.x; x++) {
					for(int z = 0; z > -size.y; z--) {
						Vector3Int cellData = new Vector3Int(room.Key, GetRotatedIndex(size, new Vector2Int(x, z), room.Value.z), room.Value.z);
						//Debug.Log(new Vector2Int(x+pos.x, z+pos.y).ToString()+":"+cellData.ToString());
						dungeon[floor][x + pos.x][z + pos.y] = cellData;
					}
				}
			}
		}
	}

	//Generate corridors, walls and doors
	void GenerateWalls() {
		for(int floor = 0; floor < floors; floor++) {
			//1. Find cells that can be vertices
			List<Vector2Int> vertices = new List<Vector2Int>();
			for(int x = 0; x < width; x++) {
				for(int z = 0; z < height; z++) {
					Vector3Int cellData = dungeon[floor][x][z];
					//Debug.Log(cellData);
					if(cellData.x > -1 && doorData[cellData.x].Contains(cellData.y)) {
						vertices.Add(new Vector2Int(x, z));
					}
				}
			}
			Debug.Log("vertices:"+vertices.Count.ToString());

			//2. Use the Bowyer-Watson Algorithm to compute the Delaunay triangulation
			List<Vector2Int> superTrianle = new List<Vector2Int>{
                new Vector2Int(-width, 0),
                new Vector2Int(width * 3, 0),
                new Vector2Int(0, height * 2)
            };
			List<List<Vector2Int>> triangles = new List<List<Vector2Int>>{
                superTrianle
            };
			/*List<Vector3> circles = new List<Vector3>{
                CalculateCircumCircle(triangles[0])
            };*/
			foreach(Vector2Int vertice in vertices) {
				//Check if the current triangulation is legal
				List<int> illegalTriangles = new List<int>();
				List<KeyValuePair<Vector2Int, Vector2Int>> newEdges = new List<KeyValuePair<Vector2Int, Vector2Int>>();
				for(int i = 0; i < triangles.Count; i++) {
					if(CheckTriangleInclusion(vertice, triangles[i])) {
						illegalTriangles.Add(i);
					}
				}
				//Find the edges of new triangles
				for(int i = 0; i < illegalTriangles.Count ; i++) {
					List<Vector2Int> illegalTriangle = triangles[illegalTriangles[i]];
					for(int j = 0; j < 3; j++) {
						KeyValuePair<Vector2Int, Vector2Int> edge = new KeyValuePair<Vector2Int, Vector2Int>(illegalTriangle[j], illegalTriangle[(j + 1) % 3]);
						List<KeyValuePair<Vector2Int, Vector2Int>> adjEdges = new List<KeyValuePair<Vector2Int, Vector2Int>>();
						for(int k = 0; k < illegalTriangles.Count; k++) {
							if(k != i) {
								List<Vector2Int> adjTriangle = triangles[illegalTriangles[k]];
								for(int l = 0; l < 3; l++) {
									adjEdges.Add(new KeyValuePair<Vector2Int, Vector2Int>(adjTriangle[l], adjTriangle[(l + 1) % 3]));
								}
							}
						}
						bool flag = true;
						foreach(KeyValuePair<Vector2Int, Vector2Int> adjEdge in adjEdges) {
							if((adjEdge.Key.Equals(edge.Key) && adjEdge.Value.Equals(edge.Value)) || (adjEdge.Key.Equals(edge.Value) && adjEdge.Value.Equals(edge.Key))) {
								flag = false;
								break;
							}
						}
						if(flag) {
							newEdges.Add(edge);
						}
					}
				}
				//Debug.Log(triangles.Count);
				//Remove illegal triangles
				for(int i = illegalTriangles.Count - 1; i > -1; i--) {
					triangles.RemoveAt(illegalTriangles[i]);
					//circles.RemoveAt(illegalTriangles[i]);
				}
				//Update triangulation
				foreach(KeyValuePair<Vector2Int, Vector2Int> edge in newEdges) {
					List<Vector2Int> newTriangle = new List<Vector2Int>{edge.Key, edge.Value, vertice};
					triangles.Add(newTriangle);
					//circles.Add(CalculateCircumCircle(newTriangle));
				}
			}
			//Remove triangles adjacent to the super triangle
			for(int i = triangles.Count - 1; i > -1; i--) {
				if(CheckAdjacence(superTrianle, triangles[i])) {
					triangles.RemoveAt(i);
				}
			}
			Debug.Log("triangles:"+triangles.Count.ToString());
			//Convert triangulation into edges
			List<KeyValuePair<Vector2Int, Vector2Int>> edges = new List<KeyValuePair<Vector2Int, Vector2Int>>();
			foreach(List<Vector2Int> triangle in triangles) {
				for(int j = 0; j < 3; j++) {
					Vector2Int p1 = triangle[j];
					Vector2Int p2 = triangle[(j + 1) % 3];
					KeyValuePair<Vector2Int, Vector2Int> newEdge = new KeyValuePair<Vector2Int, Vector2Int>(p1, p2);
					bool flag = true;
					int index = 0;
					float newLength = Vector2.Distance(newEdge.Key, newEdge.Value);
					for(index = 0; index < edges.Count; index++) {
						KeyValuePair<Vector2Int, Vector2Int> edge = edges[index];
						float length = Vector2.Distance(edge.Key, edge.Value);
						if(newLength - length > 1e-5) {
							continue;
						}
						else if(length - newLength > 1e-5) {
							break;
						}
						else if((edge.Key.Equals(newEdge.Key) && edge.Key.Equals(newEdge.Value)) || (edge.Key.Equals(newEdge.Value) && edge.Key.Equals(newEdge.Key))) {
							flag = false;
							break;
						}
					}
					if(flag) {
						//Use insertion sort to get sorted edges
						if(index == edges.Count) {
							edges.Add(newEdge);
						}
						else {
							edges.Insert(index, newEdge);
						}
					}
				}
			}
			Debug.Log("delaunay edges:"+edges.Count);

			//3. Use the Kruskal Algorithm to compute the MST
			List<List<Vector2Int>> trees = new List<List<Vector2Int>>();
			List<KeyValuePair<Vector2Int, Vector2Int>> mst = new List<KeyValuePair<Vector2Int, Vector2Int>>();
			List<KeyValuePair<Vector2Int, Vector2Int>> complement = new List<KeyValuePair<Vector2Int, Vector2Int>>();
			for(int i = 0; i < width; i++) {
				trees.Add(new List<Vector2Int>());
				for(int j = 0; j < height; j++) {
					trees[i].Add(new Vector2Int(i, j));
				}
			}
			for(int i = 0; i < edges.Count; i++) {
				if(AddEdge(edges[i].Key, edges[i].Value, trees)) {
					mst.Add(edges[i]);
				}
				else {
					complement.Add(edges[i]);
				}
			}
			Debug.Log("mst edges:"+mst.Count);

			//4. Randomly add some extra corridors
			for(int i = 0; i < complement.Count; i++) {
				if(GlobalVariables.Instance.GetRandom() < excessDoorP) {
					mst.Add(complement[i]);
				}
			}
			Debug.Log("dungeon edges:"+mst.Count);

			//5. Use the A-Star Algorithm to find the shortest path between the vertices
			foreach(KeyValuePair<Vector2Int, Vector2Int> edge in mst) {
				Dictionary<Vector2Int, Vector2Int> route = new Dictionary<Vector2Int, Vector2Int>{
					{edge.Key, edge.Key}
				};
				List<KeyValuePair<Vector2Int, int>> openList = new List<KeyValuePair<Vector2Int, int>>{
					//Add the start cell to the open list
					new KeyValuePair<Vector2Int, int>(edge.Key, 0)
                };
				Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>{
					{edge.Key, 0}
				};
				while(openList.Count > 0) {
					KeyValuePair<Vector2Int, int> curCell = openList[0]; //The open list is sorted
					openList.RemoveAt(0); //Remove the current cell from the open list
					if(curCell.Key.Equals(edge.Value)) { //The shortest path has been found
						break;
					}
					else {
						//Traverse adjacent cells of the current cell
						Vector3Int currentCell = new Vector3Int(curCell.Key.x, curCell.Key.y, floor);
						UpdateAdjacentCost(currentCell, edge.Value, new Vector2Int(-1, 0), openList, cost, route);
						UpdateAdjacentCost(currentCell, edge.Value, new Vector2Int(1, 0), openList, cost, route);
						UpdateAdjacentCost(currentCell, edge.Value, new Vector2Int(0, -1), openList, cost, route);
						UpdateAdjacentCost(currentCell, edge.Value, new Vector2Int(0, 1), openList, cost, route);
					}
				}
				//Get the shortest path and generate doors
				if(route.ContainsKey(edge.Value)) { //Find a path
					Vector2Int currCell = route[edge.Value];
					while(!currCell.Equals(edge.Key)) {
						if(dungeon[floor][currCell.x][currCell.y].x == -1) {
							dungeon[floor][currCell.x][currCell.y] = new Vector3Int(1, 0, GetCellRelDir(route[currCell], currCell));
						}
						currCell = route[currCell];
					}
				}
				/*else {
					Debug.Log("cannot find path:"+edge.Key.ToString()+","+edge.Value.ToString());
					foreach(KeyValuePair<Vector2Int, Vector2Int> path in route) {
						Debug.Log(path.Key.ToString()+"->"+path.Value.ToString());
					}
				}*/
			}

			//6. Update wall and door data
			for(int x = 0; x < width; x++) {
				for(int z = 0; z < height; z++) {
					Vector3Int curCell = dungeon[floor][x][z];
					if(curCell.x > -1) {
						//Update x-axis direction
						if(x < width - 1) {
							Vector3Int xCell = dungeon[floor][x + 1][z];
							if(xCell.x > -1 && ((doorData[curCell.x].Contains(curCell.y) && doorData[xCell.x].Contains(xCell.y)) || (!corridors.Contains(curCell.x) && !corridors.Contains(xCell.x)) || roomData[curCell.x][AdjacentRoomType] == xCell.x || roomData[xCell.x][AdjacentRoomType] == curCell.x)) {
								//Both rooms can connect to each other directly, or neither room is a corridor
								wallDir[floor][x][z][3] = true;
								wallDir[floor][x + 1][z][1] = true;
								//Set a door at the direction of where the corridor facing the room
								if(corridors.Contains(curCell.x) && !corridors.Contains(xCell.x)) {
									doorDir[floor][x][z][3] = true;
								}
								else if(corridors.Contains(xCell.x) && !corridors.Contains(curCell.x)) {
									doorDir[floor][x + 1][z][1] = true;
								}
							}
						}
						//Update z-axis direction
						if(z < height - 1) {
							Vector3Int zCell = dungeon[floor][x][z + 1];
							if(zCell.x > -1 && ((doorData[curCell.x].Contains(curCell.y) && doorData[zCell.x].Contains(zCell.y)) || (!corridors.Contains(curCell.x) && !corridors.Contains(zCell.x)) || roomData[curCell.x][AdjacentRoomType] == zCell.x || roomData[zCell.x][AdjacentRoomType] == curCell.x)) {
								//Both rooms can connect to each other directly, or neither room is a corridor
								wallDir[floor][x][z][2] = true;
								wallDir[floor][x][z + 1][0] = true;
								//Set a door at the direction of where the corridor facing the room
								if(corridors.Contains(curCell.x) && !corridors.Contains(zCell.x)) {
									doorDir[floor][x][z][2] = true;
								}
								else if(corridors.Contains(zCell.x) && !corridors.Contains(curCell.x)) {
									doorDir[floor][x][z + 1][0] = true;
								}
							}
						}
					}
				}
			}
		}
	}

	//Print dungeon data into the log for debugging
	void PrintDungeon() {
		for(int floor = 0; floor < floors; floor++) {
			for(int x = 0; x < width; x++) {
				System.Text.StringBuilder sb = new System.Text.StringBuilder();
				foreach (Vector3Int item in dungeon[floor][x]) {
					sb.Append(item.x.ToString()).Append(" ");
				}
				Debug.Log(sb.ToString());
			}
			Debug.Log('\n');
		}
	}

	//Get room size via its ref
	Vector2Int GetRoomSize(int index) {
		List<int> data = roomData[index];
		return new Vector2Int(data[RoomWidth], data[RoomHeight]);
	}

	//Update x and z ranges of the dungeon
	void UpdateDungeonRange(ref int xMin, ref int xMax, ref int zMin, ref int zMax, KeyValuePair<Vector2Int, Vector2Int> ranges) {
		xMax = Mathf.Max(ranges.Key.y, xMax);
		xMin = Mathf.Min(ranges.Key.x, xMin);
		zMax = Mathf.Max(ranges.Value.y, zMax);
		zMin = Mathf.Min(ranges.Value.x, zMin);
	}

	//Reorder room numbers with Fisher-Yates Algorithm
	List<int> ShuffleRooms(Dictionary<int, int> rooms) {
		//Get all rooms
		List<int> shuffledRooms = new List<int>();
		foreach(KeyValuePair<int, int> roomType in rooms) {
			for(int i = 0; i < roomType.Value; i++) {
				shuffledRooms.Add(roomType.Key);
			}
		}

		//Shuffle rooms
		int length = shuffledRooms.Count;
        for(int i = length - 1; i > 1; i--) {
            int k = GlobalVariables.Instance.GetSeedSegment(i % 24) % length; //The MD5 seed is a 32-digit hex number
			int t = shuffledRooms[k];
            shuffledRooms[k] = shuffledRooms[i];
            shuffledRooms[i] = t;
        }
		return shuffledRooms;
	}

	//Calculate the rotated room size (which position is fixed, only its size changes)
	Vector2Int RotateRoom(Vector2Int size, int dir) {
		if(dir % 2 == 0) {
			return size;
		}
		else {
			return new Vector2Int(size.y, size.x);
		}
	}

	//Get the index of room cells after rotation
	int GetRotatedIndex(Vector2Int rotSize, Vector2Int relPos, int dir) {
		int normDir = dir < 0 ? (dir % 4 + Mathf.Abs(dir) * 4) % 4 : dir % 4;
		switch(normDir) {
			case 0:
				return -relPos.y * rotSize.x + relPos.x;
			case 1:
				return (relPos.x + 1) * rotSize.y + relPos.y - 1;
			case 2:
				return (rotSize.y + relPos.y) * rotSize.x - relPos.x - 1;
			case 3:
				return (rotSize.x - relPos.x - 1) * rotSize.y - relPos.y;
			default:
				return 0;
		}
	}

	//Check if two rooms intersect
	bool CheckIntersection(Vector2Int pos1, Vector2Int size1, Vector2Int pos2, Vector2Int size2) {
		if(pos1.x - size2.x >= pos2.x || pos1.x + size1.x <= pos2.x || pos1.y - size1.y >= pos2.y || pos1.y + size2.y <= pos2.y) {
			return false;
		}
		else {
			return true;
		}
	}

	//Check if a room intersects with another room on the current floor
	bool CheckAllIntersections(Vector2Int curRoomPos, Vector2Int curRoomSize, List<KeyValuePair<int, Vector3Int>> curFloorRooms) {
		bool flag = false;
		for(int j = 0; j < curFloorRooms.Count; j++) {
			KeyValuePair<int, Vector3Int> lastRoom = curFloorRooms[j];
			Vector2Int lastRoomPos = new Vector2Int(lastRoom.Value.x, lastRoom.Value.y);
			Vector2Int lastRoomSize = RotateRoom(GetRoomSize(lastRoom.Key), lastRoom.Value.z);
			flag = CheckIntersection(lastRoomPos + new Vector2Int(-1, 1), lastRoomSize + new Vector2Int(2, 2), curRoomPos, curRoomSize);
			if(flag) {
				break;
			}
		}
		return flag;
	}

	//Get the position of the adjacent room
	Vector2Int GetAdjacentRoomPos(int index, Vector2Int pos, int dir) {
		List<int> data = roomData[index];
		int type = data[AdjacentRoomType];
		if(type < 0) {
			return Vector2Int.zero;
		}
		else {
			Vector2Int curSize = new Vector2Int(data[RoomWidth], data[RoomHeight]);
			Vector2Int adjRelPos = new Vector2Int(data[AdjacentRoomOffsetX], data[AdjacentRoomOffsetZ]);
			Vector2Int adjSize = GetRoomSize(type);
			Vector2Int adjRotRelPos = new Vector2Int(); //Relative position of adjacent room to current room after rotation
			Vector2Int curOffset = new Vector2Int(); //Offset due to rotation of current room
			Vector2Int adjOffset = new Vector2Int(); //Offset due to rotation of adjacent room
			int normDir = dir < 0 ? (dir % 4 + Mathf.Abs(dir) * 4) % 4 : dir % 4;
			switch(normDir) {
				case 0:
					adjRotRelPos = adjRelPos;
					curOffset = Vector2Int.zero;
					adjOffset = Vector2Int.zero;
					break;
				case 1:
					adjRotRelPos = new Vector2Int(-adjRelPos.y, adjRelPos.x);
					curOffset = new Vector2Int(0, -curSize.x + 1);
					adjOffset = new Vector2Int(0, adjSize.x - 1);
					break;
				case 2:
					adjRotRelPos = new Vector2Int(-adjRelPos.y, -adjRelPos.x);
					curOffset = new Vector2Int(curSize.x - 1, -curSize.y + 1);
					adjOffset = new Vector2Int(-adjSize.x + 1, adjSize.y - 1);
					break;
				case 3:
					adjRotRelPos = new Vector2Int(adjRelPos.y, -adjRelPos.x);
					curOffset = new Vector2Int(curSize.y - 1, 0);
					adjOffset = new Vector2Int(-adjSize.y + 1, 0);
					break;
			}
			return pos + adjRotRelPos + curOffset + adjOffset;
		}
	}

	//Get ranges of x and z of the room
	KeyValuePair<Vector2Int, Vector2Int> GetRoomRange(Vector2Int size, Vector2Int pos, int dir) {
		Vector2Int curSize = RotateRoom(size, dir);
		Vector2Int rangeX = new Vector2Int(pos.x - 1, pos.x + curSize.x + 1);
		Vector2Int rangeZ = new Vector2Int(pos.y - curSize.y - 1, pos.y + 1);
		return new KeyValuePair<Vector2Int, Vector2Int>(rangeX, rangeZ);
	}

	//Calculate absolute position via relative position
	Vector2Int GetRoomAbsPos(Vector2Int size, Vector2Int relPos, int dir, int offsetDir, KeyValuePair<int, Vector3Int> lastRoom) {
		Vector2Int lastRoomSize = RotateRoom(GetRoomSize(lastRoom.Key), lastRoom.Value.z);
		Vector2Int lastRoomPos = new Vector2Int(lastRoom.Value.x, lastRoom.Value.y);
		Vector2Int curRoomSize = RotateRoom(size, dir);
		Vector2Int absPos = relPos + lastRoomPos;
		switch(offsetDir) {
			case 0:
				absPos += new Vector2Int(lastRoomSize.x, 0);
				break;
			case 1:
				absPos += new Vector2Int(0, curRoomSize.y);
				break;
			case 2:
				absPos -= new Vector2Int(curRoomSize.x, 0);
				break;
			case 3:
				absPos -= new Vector2Int(0, lastRoomSize.y);
				break;
		}
		return absPos;
	}

	//Calculate the center and the radius of the circumscribed circle of a triangle
	//There is some errors about the formula. DO NOT use this function!
	Vector3 CalculateCircumCircle(List<Vector2Int> triangle) {
		Vector2Int p1 = triangle[0];
		Vector2Int p2 = triangle[1];
		Vector2Int p3 = triangle[2];
		if((p2.x - p1.x) * (p3.y - p1.y) == (p3.x - p1.x) * (p2.y - p1.y)) {
			//Three points are collinear
			return Vector3.zero;
		}
		else {
			float a = p1.x * (p2.y - p3.y) + p2.x * (p3.y - p1.y) + p3.x * (p1.y - p2.y);
			float b = (p1.x * p1.x + p1.y * p1.y) * (p2.y - p3.y) + (p2.x * p2.x + p2.y * p2.y) * (p3.y - p1.y) + (p3.x * p3.x + p3.y * p3.y) * (p1.y - p2.y);
			float c = (p1.x * p1.x + p1.y * p1.y) * (p2.x - p3.x) + (p2.x * p2.x + p2.y * p2.y) * (p3.x - p1.x) + (p3.x * p3.x + p3.y * p3.y) * (p1.x - p2.x);
			float d = (p1.x * p1.x + p1.y * p1.y) * (p2.x * p3.y - p2.y * p3.x) + (p2.x * p2.x + p2.y * p2.y) * (p3.x * p1.y - p3.y * p1.x) + (p3.x * p3.x + p2.y * p2.y) * (p1.x * p2.y - p1.y * p2.x);
			return new Vector3(b / (2 * a), -(c / (2 * a)), Mathf.Sqrt((b * b + c * c + 4 * a * d)/(2 * Mathf.Abs(a))));
		}
	}

	//Check if the point is within the circle
	//Due to errors of formula of circumcircle, DO NOT use this function!
	//Use CheckTriangleInclusion() instead.
	bool CheckInclusion(Vector2Int point, Vector3 circle) {
		Vector2 center = new Vector2(circle.x, circle.y);
		return Vector2.Distance(center, point) < circle.z;
	}

	//Check if the point is within the circumcircle of a triangle
	bool CheckTriangleInclusion(Vector2Int point, List<Vector2Int> triangle) {
		Vector2Int p1 = triangle[0];
		Vector2Int p2 = triangle[1];
		Vector2Int p3 = triangle[2];
		int a1 = ((p1.x - point.x) * (p2.y - point.y) - (p1.y - point.y) * (p2.x - point.x)) * ((p3.x - point.x) * (p3.x - point.x) + (p3.y - point.y) * (p3.y - point.y));
		int a2 = ((p2.x - point.x) * (p3.y - point.y) - (p2.y - point.y) * (p3.x - point.x)) * ((p1.x - point.x) * (p1.x - point.x) + (p1.y - point.y) * (p1.y - point.y));
		int a3 = ((p3.x - point.x) * (p1.y - point.y) - (p3.y - point.y) * (p1.x - point.x)) * ((p2.x - point.x) * (p2.x - point.x) + (p2.y - point.y) * (p2.y - point.y));
		return a1 + a2 + a3 > 0;
	}

	//Check if two triangles have common vertices
	bool CheckAdjacence(List<Vector2Int> triangle1, List<Vector2Int> triangle2) {
		if(triangle1[0].Equals(triangle2[0]) || triangle1[0].Equals(triangle2[1]) || triangle1[0].Equals(triangle2[2])) {
			return true;
		}
		else if(triangle1[1].Equals(triangle2[0]) || triangle1[1].Equals(triangle2[1]) || triangle1[1].Equals(triangle2[2])) {
			return true;
		}
		else if(triangle1[2].Equals(triangle2[0]) || triangle1[2].Equals(triangle2[1]) || triangle1[2].Equals(triangle2[2])) {
			return true;
		}
		else {
			return false;
		}
	}

	//Find the root of a subtree
	Vector2Int GetRootIndex(Vector2Int a, List<List<Vector2Int>> trees) {
		Vector2Int i = a;
		while(!trees[i.x][i.y].Equals(i)) {
			i = trees[i.x][i.y];
		}
		return i;
	}

	//Attempt to add an edge to the MST
	bool AddEdge(Vector2Int a, Vector2Int b, List<List<Vector2Int>> trees) {
		Vector2Int rootA = GetRootIndex(a, trees);
		Vector2Int rootB = GetRootIndex(b, trees);
		if(rootA.Equals(rootB)) { //A loop forms
			return false;
		}
		else { //Merge two subtrees
			trees[rootB.x][rootB.y] = rootA;
			return true;
		}
	}

	//Calculate the manhattan distance
	int Manhattan(Vector2Int start, Vector2Int target) {
		return Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y);
	}

	//Update the cost function of the A-Star Algorithm
	//Only use this when traversing adjacent cells
	void UpdateAdjacentCost(Vector3Int currentCell, Vector2Int target, Vector2Int offset, List<KeyValuePair<Vector2Int, int>> openList, Dictionary<Vector2Int, int> cost, Dictionary<Vector2Int, Vector2Int> route) {
		Vector2Int curCell = new Vector2Int(currentCell.x, currentCell.y);
		Vector2Int nxtCell = curCell + offset;
		if(nxtCell.x >= 0 && nxtCell.x < width && nxtCell.y >= 0 && nxtCell.y < height) {
			//Within the dungeon area
			int index = dungeon[currentCell.z][nxtCell.x][nxtCell.y].x;
			if(index == -1 || corridors.Contains(index) || nxtCell.Equals(target)) {
				//Can be passed
				int nextCost = cost[curCell] + Manhattan(curCell, nxtCell); //Distance function that is customizable. Normally it is 1
				if(!cost.ContainsKey(nxtCell) || nextCost < cost[nxtCell]) {
					cost[nxtCell] = nextCost;
					route[nxtCell] = curCell;
					int totalCost = nextCost + Manhattan(nxtCell, target); //The estimated cost from next cell to the destination
					//Maintain the sorted open list with insertion sort
					KeyValuePair<Vector2Int, int> curCost = new KeyValuePair<Vector2Int, int>(nxtCell, totalCost);
					for(int i = 0; i < openList.Count; i++) {
						if(openList[i].Value > totalCost) {
							openList.Insert(i, curCost);
							return;
						}
					}
					openList.Add(curCost);
				}
			}
		}
	}

	//Get the relative direction of target cell to current cell
	int GetCellRelDir(Vector2Int currentCell, Vector2Int targetCell) {
		Vector2 current = new Vector2(currentCell.x, currentCell.y).normalized;
		Vector2 target = new Vector2(targetCell.x, targetCell.y).normalized;
		float rotDir = Mathf.Sign(Vector3.Cross(current, target).z);
		float rotAng = Mathf.Acos(Vector2.Dot(current, target) / (current.magnitude * target.magnitude));
		return Mathf.RoundToInt(Mathf.Repeat(rotDir * rotAng, Mathf.PI * 2) / (Mathf.PI / 2)) % 4;
	}

	//Update task places
	void OnTaskInit(string message) {
		if(message == "Tutorial") { //Task initialized
			
		}
	}
}
