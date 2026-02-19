using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Navigator : MonoBehaviour {
	public static Navigator Instance; //The instance for the singleton pattern

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
	}

	//Get the absolute position of next move (It is the center of a cell, or the destination) to destination
	public Vector3 GetNextMove(Vector3 start, Vector3 target) {
		Vector3Int relStart = GetRelativePosition(start);
		Vector3Int relTarget = GetTargetRelativePosition(start, target);
		if(relStart.Equals(relTarget)) { //Start position and target position are in the same cell
			return relStart.x == GetRelativePosition(target).x ? target : GetAbsolutePosition(relTarget);
		}
		else {
			Dictionary<Vector2Int, Vector2Int> route = FindPath(relStart, relTarget);
			if(route != null) {
				Vector2Int lastCell = route[new Vector2Int(relStart.y, relStart.z)];
				Debug.Log("path found:" + relStart.ToString() + "->" + relTarget.ToString() + ":" + lastCell.ToString());
				return GetAbsolutePosition(new Vector3Int(relStart.x, lastCell.x, lastCell.y));
			}
			else {
				Debug.Log("cannot find path:" + relStart.ToString() + "->" + relTarget.ToString());
				return start;
			}
		}
	}

	//Get the absolute position of next waypoint (It is a door, a turn or the destination) to destination
	public Vector3 GetNextWaypoint(Vector3 start, Vector3 target) {
		Vector3Int relStart = GetRelativePosition(start);
		Vector3Int relTarget = GetRelativePosition(target);
		if(relStart.x != relTarget.x && DungeonGenerator.stairs.ContainsKey(DungeonGenerator.dungeon[relStart.x][relStart.y][relStart.z].x)) {
			//Currently on stairs
			return GetStairAbsolutePosition(start, target);
		}
		else {
			relTarget = GetTargetRelativePosition(start, target);
			if(relStart.Equals(relTarget)) { //Start position and target position are in the same cell
				return target;
			}
			else{
				Dictionary<Vector2Int, Vector2Int> route = FindPath(relStart, relTarget);
				if(route != null) {
					Vector2Int startPos = new Vector2Int(relStart.y, relStart.z);
					Vector2Int targetPos = new Vector2Int(relTarget.y, relTarget.z);
					Vector2Int currCell = route[startPos];
					Vector2Int lastCell = startPos;
					while(!currCell.Equals(targetPos) && !CheckTurnAt(lastCell, currCell, route[currCell]) && !CheckDoorAt(currCell, route[currCell], relStart.x)) {
						lastCell = currCell;
						currCell = route[currCell];
					}
					Debug.Log("path found:" + relStart.ToString() + "->" + relTarget.ToString() + ":" + currCell.ToString());
					return GetAbsolutePosition(new Vector3Int(relStart.x, currCell.x, currCell.y));
				}
				else {
					Debug.Log("cannot find path:" + relStart.ToString() + "->" + relTarget.ToString());
					return start;
				}
			}
		}
	}

	//Get relative position in the dungeon
	public Vector3Int GetRelativePosition(Vector3 pos) {
		//(x, y, z) -> (floor, x, z)
		return new Vector3Int(Mathf.FloorToInt(pos.y / 5), Mathf.CeilToInt(pos.x / 5), Mathf.FloorToInt(pos.z / 5));
	}

	//Get absolute position in the dungeon
	Vector3 GetAbsolutePosition(Vector3Int pos) {
		//(floor, x, z) -> (x, y, z)
		return new Vector3(pos.y * 5 - 2.5f, pos.x * 5, pos.z * 5 + 2.5f);
	}

	//Get relative position of actual target (which could be stairs)
	Vector3Int GetTargetRelativePosition(Vector3 start, Vector3 target) {
		Vector3Int relStart = GetRelativePosition(start);
		Vector3Int relTarget = GetRelativePosition(target);
		if(relStart.x == relTarget.x) { //Start position and target position are on the same floor
			return relTarget;
		}
		else { //Find upward or downward stairs
			Vector2Int startPos = new Vector2Int(relStart.y, relStart.z);
			List<Vector2Int> stairs = relStart.x < relTarget.x ? DungeonGenerator.stairPos[relStart.x].Key : DungeonGenerator.stairPos[relStart.x].Value;
			Vector2Int stair = stairs[0];
			float minDist = Vector2.Distance(stairs[0], startPos);
			for(int i = 1; i < stairs.Count; i++) {
				if(Vector2.Distance(stairs[i], startPos) < minDist) {
					stair = stairs[i];
					minDist = Vector2.Distance(stair, startPos);
				}
			}
			return new Vector3Int(relStart.x, stair.x, stair.y);
		}
	}

	//Get target position when start position is on stairs
	//Use this only when start position is on stairs, or may produce undefined behavior
	Vector3 GetStairAbsolutePosition(Vector3 start, Vector3 target) {
		Vector3Int relStart = GetRelativePosition(start);
		Vector3Int relTarget = GetRelativePosition(target);
		List<KeyValuePair<List<Vector2Int>, List<Vector2Int>>> stairPos = DungeonGenerator.stairPos;
		bool isEqual = relStart.x == relTarget.x;
		bool isUpward = relStart.x < relTarget.x;
		List<Vector2Int> stairs = isUpward ? stairPos[relStart.x + 1].Value : (isEqual ? stairPos[relStart.x].Key : stairPos[relStart.x - 1].Key);
		float dist = Vector2Int.Distance(stairs[0], new Vector2Int(relStart.y, relStart.z));
		Vector2Int targetPos = stairs[0];
		for(int i = 1; i < stairs.Count; i++) {
			float curDist = Vector2Int.Distance(stairs[i], new Vector2Int(relStart.y, relStart.z));
			if(curDist < dist) {
				dist = curDist;
				targetPos = stairs[i];
			}
		}
		relTarget = isUpward ? new Vector3Int(relStart.x + 1, targetPos.x, targetPos.y) : (isEqual ? new Vector3Int(relStart.x, targetPos.x, targetPos.y) : new Vector3Int(relStart.x - 1, targetPos.x, targetPos.y));
		return GetAbsolutePosition(relTarget);
	}

	//Check if there is a turn between two moves
	bool CheckTurnAt(Vector2Int lastPos, Vector2Int curPos, Vector2Int nextPos) {
		int curDeltaX = lastPos.x - curPos.x, curDeltaZ = lastPos.y - curPos.y;
		int nextDeltaX = curPos.x - nextPos.x, nextDeltaZ = curPos.y - nextPos.y;
		return curDeltaX != nextDeltaX && curDeltaZ != nextDeltaZ;
	}

	//Check if there is a door between two cells
	bool CheckDoorAt(Vector2Int pos1, Vector2Int pos2, int floor) {
		if(Mathf.Abs(pos1.x - pos2.x) > 1 || Mathf.Abs(pos1.y - pos2.y) > 1) {
			//Two cells are no adjacent
			return false;
		}
		if(DungeonGenerator.corridors.Contains(DungeonGenerator.dungeon[floor][pos1.x][pos1.y].x) && DungeonGenerator.corridors.Contains(DungeonGenerator.dungeon[floor][pos2.x][pos2.y].x)) {
			//Two corridors
			return false;
		}
		else {
			if(pos1.y == pos2.y) {
				//Check x-axis direction
				if(pos1.x < pos2.x) {
					return DungeonGenerator.doorDir[floor][pos1.x][pos1.y][3] || DungeonGenerator.doorDir[floor][pos2.x][pos2.y][1];
				}
				else {
					return DungeonGenerator.doorDir[floor][pos2.x][pos2.y][3] || DungeonGenerator.doorDir[floor][pos1.x][pos1.y][1];
				}
			}
			else {
				//Check z-axis direction
				if(pos1.y < pos2.y) {
					return DungeonGenerator.doorDir[floor][pos1.x][pos1.y][2] || DungeonGenerator.doorDir[floor][pos2.x][pos2.y][0];
				}
				else {
					return DungeonGenerator.doorDir[floor][pos2.x][pos2.y][2] || DungeonGenerator.doorDir[floor][pos1.x][pos1.y][0];
				}
			}
		}
	}

	//Check if there is a wall between two cells
	bool CheckWallAt(Vector2Int pos1, Vector2Int pos2, int floor) {
		if(Mathf.Abs(pos1.x - pos2.x) > 1 || Mathf.Abs(pos1.y - pos2.y) > 1) {
			//Two cells are no adjacent
			return false;
		}
		if(DungeonGenerator.corridors.Contains(DungeonGenerator.dungeon[floor][pos1.x][pos1.y].x) && DungeonGenerator.corridors.Contains(DungeonGenerator.dungeon[floor][pos2.x][pos2.y].x)) {
			//Two corridors
			return true;
		}
		else {
			if(pos1.y == pos2.y) {
				//Check x-axis direction
				if(pos1.x < pos2.x) {
					return !(DungeonGenerator.wallDir[floor][pos1.x][pos1.y][3] || DungeonGenerator.wallDir[floor][pos2.x][pos2.y][1]);
				}
				else {
					return !(DungeonGenerator.wallDir[floor][pos2.x][pos2.y][3] || DungeonGenerator.wallDir[floor][pos1.x][pos1.y][1]);
				}
			}
			else {
				//Check z-axis direction
				if(pos1.y < pos2.y) {
					return !(DungeonGenerator.wallDir[floor][pos1.x][pos1.y][2] || DungeonGenerator.wallDir[floor][pos2.x][pos2.y][0]);
				}
				else {
					return !(DungeonGenerator.wallDir[floor][pos2.x][pos2.y][2] || DungeonGenerator.wallDir[floor][pos1.x][pos1.y][0]);
				}
			}
		}
	}

	//Use the A-Star Algorithm to find path
	Dictionary<Vector2Int, Vector2Int> FindPath(Vector3Int start, Vector3Int target) {
		//start and target is (floor, x, z)
		Vector2Int startPos = new Vector2Int(start.y, start.z);
		Vector2Int targetPos = new Vector2Int(target.y, target.z);
		Dictionary<Vector2Int, Vector2Int> route = new Dictionary<Vector2Int, Vector2Int>{
			{startPos, startPos}
		};
		List<KeyValuePair<Vector2Int, int>> openList = new List<KeyValuePair<Vector2Int, int>>{
			//Add the start cell to the open list
			new KeyValuePair<Vector2Int, int>(startPos, 0)
        };
		Dictionary<Vector2Int, int> cost = new Dictionary<Vector2Int, int>{
			{startPos, 0}
		};
		while(openList.Count > 0) {
			KeyValuePair<Vector2Int, int> curCell = openList[0]; //The open list is sorted
			openList.RemoveAt(0); //Remove the current cell from the open list
			if(curCell.Key.Equals(targetPos)) { //The shortest path has been found
				break;
			}
			else {
				//Traverse adjacent cells of the current cell
				Vector3Int currentCell = new Vector3Int(curCell.Key.x, curCell.Key.y, start.x);
				UpdateAdjacentCost(currentCell, targetPos, 0, openList, cost, route);
				UpdateAdjacentCost(currentCell, targetPos, 1, openList, cost, route);
				UpdateAdjacentCost(currentCell, targetPos, 2, openList, cost, route);
				UpdateAdjacentCost(currentCell, targetPos, 3, openList, cost, route);
			}
		}
		if(!route.ContainsKey(targetPos)) {
			return null;
		}
		else {
			Dictionary<Vector2Int, Vector2Int> path = new Dictionary<Vector2Int, Vector2Int>();
			Vector2Int currCell = targetPos;
			while(!currCell.Equals(startPos)) {
				//Debug.Log(route[currCell].ToString()+"->"+currCell.ToString());
				path.Add(route[currCell], currCell);
				currCell = route[currCell];
			}
			return path;
		}
	}

	//Calculate the manhattan distance
	int Manhattan(Vector2Int start, Vector2Int target) {
		return Mathf.Abs(target.x - start.x) + Mathf.Abs(target.y - start.y);
	}

	//Update the cost function of the A-Star Algorithm
	//Only use this when traversing adjacent cells
	void UpdateAdjacentCost(Vector3Int currentCell, Vector2Int target, int dir, List<KeyValuePair<Vector2Int, int>> openList, Dictionary<Vector2Int, int> cost, Dictionary<Vector2Int, Vector2Int> route) {
		Vector2Int curCell = new Vector2Int(currentCell.x, currentCell.y);
		Vector2Int nxtCell = curCell + new Vector2Int(Mathf.RoundToInt(Mathf.Cos(dir * Mathf.PI / 2)), Mathf.RoundToInt(Mathf.Sin(dir * Mathf.PI / 2)));
		if(nxtCell.x >= 0 && nxtCell.x < DungeonGenerator.Instance.width && nxtCell.y >= 0 && nxtCell.y < DungeonGenerator.Instance.height) {
			//Within the dungeon area
			int curIndex = DungeonGenerator.dungeon[currentCell.z][curCell.x][curCell.y].x;
			int nextIndex = DungeonGenerator.dungeon[currentCell.z][nxtCell.x][nxtCell.y].x;
			if(curIndex > -1 && nextIndex > -1 && ((DungeonGenerator.corridors.Contains(curIndex) && DungeonGenerator.corridors.Contains(nextIndex)) || !CheckWallAt(curCell, nxtCell, currentCell.z) || CheckDoorAt(curCell, nxtCell, currentCell.z))) {
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
}
