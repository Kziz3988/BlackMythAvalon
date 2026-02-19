using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour {
	public static EventManager Instance; //Singleton pattern
    readonly Dictionary<string, Action<string>> events = new Dictionary<string, Action<string>>();

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
		Initialize();
	}

	void Update() {
		if(Input.GetMouseButtonDown(0)) {
			TriggerEvent("OnMouseDown", null);
		}
		if(Input.anyKeyDown) {
			TriggerEvent("OnKeyDown", null);
		}
	}

	void Initialize() {
		events.Clear();
	}

	//Register an event listener
	public void AddListener(string name, Action<string> listener) {
		if(!events.ContainsKey(name)) {
			events[name] = listener;
		}
		else {
			events[name] += listener;
		}
	}

	//Remove an event listener
	public bool RemoveListener(string name, Action<string> listner) {
		if(events.ContainsKey(name)) {
			events[name] -= listner;
			return true;
		}
		else {
			return false;
		}
	}

	//Trigger an event
	public bool TriggerEvent(string name, string message) {
		if(events.ContainsKey(name) && events[name] != null) {
			//The event and its listners both exist
			events[name].Invoke(message);
			return true;
		}
		else {
			return false;
		}
	}
}
