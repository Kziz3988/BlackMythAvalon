using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DialogManager : MonoBehaviour {
	public static DialogManager Instance; //The instance for the singleton pattern
	//Dialogs of various plot lines
	static Dictionary<string, List<KeyValuePair<string, string>>> dialogs = new Dictionary<string, List<KeyValuePair<string, string>>>();
	//Dialogs of NPCs
	public static Dictionary<string, List<string>> npcPlots = new Dictionary<string, List<string>>();
	
	string currentPlot;
	int currentLine;
	public TextAsset allPlots;
	GameObject dialogFrame;
	Text text;

	static DialogManager() {
		npcPlots.Add("Bedivere", new List<string>{"Tutorial", "Bedivere"});
        npcPlots.Add("Modred", new List<string>{"ModredIntro", "ModredDefeated1", "ModredDefeated2"});
	}

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }

		dialogs.Clear();
		string[] lines = allPlots.text.Split('\n'); //Split dialog text into single lines
		string title = "";
		List<KeyValuePair<string, string>> plot = new List<KeyValuePair<string, string>>();
		foreach(string rawLine in lines) {
			string[] line = rawLine.Replace("\r", "").Split(' ');
			if(line.Length == 1) { //A new plot
				dialogs.Add(title, plot);
				title = line[0];
				plot = new List<KeyValuePair<string, string>>();
			}
			else { //Key: line, Value: condition
				plot.Add(new KeyValuePair<string, string>(line[0], line[1]));
			}
		}
		dialogs.Add(title, plot);
	}

	void Start() {
		text = GameObject.Find("DialogText").GetComponent<Text>();
		dialogFrame = GameObject.Find("Dialog");
		Initialize();
	}

	void Initialize() {
		dialogFrame.SetActive(false);
	}

	//Start a new dialog
	//Use this when or after Start() is called
	public void SetNewPlot(string name) {
		if(dialogs.ContainsKey(name)) {
			currentPlot = name;
			currentLine = 0;
			LoadLine();
			EventManager.Instance.TriggerEvent("OnDialogBegin", name);
		}
	}

	//Display dialog
	void LoadLine() {
		dialogFrame.SetActive(true);
		text.text = dialogs[currentPlot][currentLine].Key;
		EventManager.Instance.AddListener(dialogs[currentPlot][currentLine].Value, OnNextLine);
	}

	//Switch to the next dialog line
	void OnNextLine(string message) {
		EventManager.Instance.RemoveListener(dialogs[currentPlot][currentLine].Value, OnNextLine);
		currentLine++;
		if(currentLine < dialogs[currentPlot].Count) {
			LoadLine();
		}
		else { //No more lines
			EventManager.Instance.TriggerEvent("OnDialogFinish", currentPlot);
			dialogFrame.SetActive(false);
		}
	}
}
