using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NPCManager : MonoBehaviour {
	public bool canTalk;
	Transform player;
	int currentPlot;
	EnemyController enemyController;

	void Awake() {
		player = GameObject.Find("Player").transform;
		enemyController = gameObject.GetComponent<EnemyController>();
		EventManager.Instance.AddListener("OnTaskBegin", OnTaskInit);
		EventManager.Instance.AddListener("OnBossDefeat", OnBossDefeated);
	}

	void Start() {
		EventManager.Instance.AddListener("OnMouseDown", OnInteracted);
		EventManager.Instance.AddListener("OnDialogFinish", OnDialogFinished);
		Initialize();
	}

	void Update() {
		Vector3 dir = player.position - transform.position;
		dir.y = 0;
		if(dir != Vector3.zero) {
			transform.rotation = Quaternion.LookRotation(dir);
		}
	}

	void Initialize() {
		canTalk = true;
		currentPlot = 0;
		if(enemyController != null) {
			enemyController.isActive = false;
		}
	}

	void OnInteracted(string message) {
		PlayerController pc = player.GetComponent<PlayerController>();
		if(pc.npcTalkingTo == "" && pc.interactObj == gameObject && canTalk) {
			pc.npcTalkingTo = gameObject.name;
			DialogManager.Instance.SetNewPlot(DialogManager.npcPlots[gameObject.name][currentPlot]);
		}
	}

	void OnDialogFinished(string message) {
		PlayerController pc = player.GetComponent<PlayerController>();
		if(pc.npcTalkingTo == gameObject.name) {
			EventManager.Instance.TriggerEvent("OnTalkFinish", gameObject.name);
			pc.npcTalkingTo = "";
			List<string> plots = DialogManager.npcPlots[gameObject.name];
			if(enemyController != null) {
				if(currentPlot < plots.Count - 1) {
					//End talk and begin fight
					canTalk = false;
					enemyController.isActive = true;
					player.GetComponent<PlayerController>().bossFighting = gameObject.name;
					EventManager.Instance.TriggerEvent("OnBossFightBegin", gameObject.name);
				}
				else if(gameObject.name == "Modred") {
					//Game over
					CutsceneController.Instance.SetCurrentAct("Ending");
				}
			}
			currentPlot = Mathf.Min(plots.Count - 1, currentPlot + 1);
		}
	}

	//Update task places
	void OnTaskInit(string message) {
		if(message == "Tutorial") { //Task initialized
			if(gameObject.name == "Bedivere") {
				TaskManager.Instance.tasks["Tutorial"].taskChain[0].place = transform.position;
				TaskManager.Instance.tasks["Tutorial"].taskChain[1].place = transform.position;
			}
			else if(gameObject.name == "Modred") {
				TaskManager.Instance.tasks["DefeatModred"].taskChain[0].place = transform.position;
			}
		}
	}

	void OnBossDefeated(string message) {
		if(message == gameObject.name) {
			if(message == "Modred") {
				enemyController.Initialize();
				TaskManager.Instance.tasks["DefeatModred"].taskChain[2].place = transform.position;
				EventManager.Instance.TriggerEvent("OnNavigate", null);
			}
			//End fight and begin talk
			canTalk = true;
			enemyController.isActive = false;
			player.GetComponent<PlayerController>().bossFighting = "";
			EventManager.Instance.TriggerEvent("OnBossFightEnd", gameObject.name);
		}
	}
}
