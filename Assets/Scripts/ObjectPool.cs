using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectPool : MonoBehaviour {
	public GameObject prefab; //Gameobjects in the pool
	public int poolCount = 50; //Initial size of the pool
	int expansion; //The expansion capacity when the pool is empty
	Queue<GameObject> objectQueue; //Object pool

	void Awake() {
		//Instantiate gameobjects in the pool
		expansion = 10;
		objectQueue = new Queue<GameObject>();
		Expand(poolCount);
	}

	//Expand the pool
	void Expand(int capacity) {
		for(int i = 0; i < capacity; i++) {
			GameObject obj = Instantiate(prefab);
			obj.transform.SetParent(transform);
			obj.SetActive(false);
			objectQueue.Enqueue(obj);
		}
	}

	//Enable a gameobject from the pool
	public GameObject GetObject() {
		if(poolCount == 0) {
			Expand(expansion);
			poolCount += expansion;
		}
		GameObject obj = objectQueue.Dequeue();
		obj.SetActive(true);
		poolCount--;
		return obj;
	}

	//Disable a gameobject (Make sure the gameobject is from the pool!)
	//Remove all listeners of EventManager before calling to prevent memory leakage
	public void ReleaseObject(GameObject obj) {
		obj.SetActive(false);
		objectQueue.Enqueue(obj);
		poolCount++;
	}
}
