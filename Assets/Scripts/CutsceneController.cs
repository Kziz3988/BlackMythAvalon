using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CutsceneController : MonoBehaviour {
	public static CutsceneController Instance; //The instance for the singleton pattern
	public Dictionary<string, KeyValuePair<Vector2Int, string>> cutscenes = new Dictionary<string, KeyValuePair<Vector2Int, string>>(); //The start and finish index of cutscenes
	public TextAsset plots;
	public Sprite[] scenes = new Sprite[5]; 
	List<string> lines = new List<string>();
	string currentAct = "";
	int currentScene;
	Image scene;
	Text narrator;

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
		cutscenes.Clear();
		lines.Clear();

		scene = GameObject.Find("BackgroundImage").GetComponent<Image>();
		narrator = GameObject.Find("Narrator").GetComponent<Text>();
		cutscenes.Add("Intro", new KeyValuePair<Vector2Int, string>(new Vector2Int(0, 3), "OnGameStart"));
		cutscenes.Add("Ending", new KeyValuePair<Vector2Int, string>(new Vector2Int(4, 4), "OnGameRestart"));
		string[] rawLines = plots.text.Split('\n');
		foreach(string rawLine in rawLines) {
			lines.Add(rawLine.Replace("\r", ""));
		}
		
		EventManager.Instance.AddListener("OnIntroStart", OnIntroStarted);
		EventManager.Instance.AddListener("OnGameStart", OnGameStarted);
		EventManager.Instance.AddListener("OnGameRestart", OnGameRestarted);
		Initialize();
	}

	void Update() {
		if(Input.GetMouseButtonDown(0) && currentAct != "") {
			FinishScene();
		}
	}

	void Initialize() {
		currentAct = "";
	}

	//Begin a new cutscene
	public void SetCurrentAct(string act) {
		currentAct = act;
		currentScene = cutscenes[currentAct].Key.x - 1;
		FinishScene();
	}

	//Switch to the next scene
	public void FinishScene() {
		currentScene++;
		if(currentScene <= cutscenes[currentAct].Key.y) {
			narrator.text = lines[currentScene];
			scene.sprite = scenes[currentScene];
		}
		else {
			EventManager.Instance.TriggerEvent(cutscenes[currentAct].Value, currentAct);
			currentAct = "";
		}
	}

	void OnIntroStarted(string message) {
		SetCurrentAct("Intro");
	}

	void OnGameStarted(string message) {
		StartCoroutine(LoadScene("Dungeon"));
	}

	void OnGameRestarted(string message) {
		StartCoroutine(LoadScene("MainMenu"));
	}

	//Async scene loading
	IEnumerator LoadScene(string scene) {
		AsyncOperation load = SceneManager.LoadSceneAsync(scene);
		while(!load.isDone) {
			yield return null;
		}
	}
}
