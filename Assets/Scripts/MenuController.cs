using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuController : MonoBehaviour {

	static Dictionary<string, Dictionary<string, string>> allButtons = new Dictionary<string, Dictionary<string, string>>();
	Dictionary<string, GameObject> buttons = new Dictionary<string, GameObject>();
	public Sprite backgroundImage;
	GameObject bgImage;
	
	static MenuController() {
		Dictionary<string, string> buttons1 = new Dictionary<string, string> {
            { "StartButton", "OnIntroStart" },
            { "QuitButton", "OnGameQuit" }
        };
		allButtons.Add("MainMenu", buttons1);
		Dictionary<string, string> buttons2 = new Dictionary<string, string> {
            { "ResumeButton", "OnResume" },
            { "BackButton", "OnGameBack" }
        };
		allButtons.Add("PauseMenu", buttons2);
	}

	void Awake() {
		buttons.Clear();
		bgImage = GameObject.Find("BackgroundImage");
		if(allButtons.ContainsKey(gameObject.name)) {
			foreach(KeyValuePair<string, string> button in allButtons[gameObject.name]) {
				GameObject btn = GameObject.Find(button.Key);
				btn.GetComponent<Button>().onClick.AddListener(() => OnClick(btn.name));
				EventManager.Instance.AddListener(button.Value, OnClicked);
				buttons.Add(button.Key, btn);
			}
		}
	}

	void Start() {
		if(gameObject.name == "PauseMenu") {
			EventManager.Instance.AddListener("OnPause", OnPaused);
			Hide();
		}
		else if(GameConfig.isPaused) {
			GameConfig.Pause(null);
		}
	}

	//Display the menu
	void Show() {
		bgImage.SetActive(true);
		bgImage.GetComponent<Image>().sprite = backgroundImage;
		foreach(KeyValuePair<string, string> button in allButtons[gameObject.name]) {
			buttons[button.Key].SetActive(true);
		}
	}

	//Hide the menu
	void Hide() {
		bgImage.SetActive(false);
		foreach(KeyValuePair<string, string> button in allButtons[gameObject.name]) {
			buttons[button.Key].SetActive(false);
		}
	}

	void OnClick(string message) {
		//Debug.Log(message+" triggers "+allButtons[gameObject.name][message]);
		EventManager.Instance.TriggerEvent(allButtons[gameObject.name][message], allButtons[gameObject.name][message]);
	}

	void OnClicked(string message) {
		switch(message) {
			case "OnIntroStart":
				foreach(KeyValuePair<string, string> button in allButtons[gameObject.name]) {
					buttons[button.Key].SetActive(false);
				}
				GameObject.Find("Seed").SetActive(false);
				break;
			case "OnGameQuit":
				Application.Quit();
				break;
			case "OnResume":
				GameConfig.Pause("escape");
				break;
			case "OnGameBack":
				StartCoroutine(LoadScene("MainMenu"));
				break;
		}
	}

	void OnPaused(string message) {
		if(gameObject.name == "PauseMenu") {
			if(GameConfig.isPaused && message == "escape") {
				Show();
			}
			else {
				Hide();
			}
		}
	}

	//Async scene loading
	IEnumerator LoadScene(string scene) {
		AsyncOperation load = SceneManager.LoadSceneAsync(scene);
		while(!load.isDone) {
			yield return null;
		}
	}
}
