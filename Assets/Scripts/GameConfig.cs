using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public static class GameConfig {
	//Game difficulty
	public const int EasyGame = 0;
    public const int NormalGame = 1;
    public const int HardGame = 2;

	public static int gameDiff = EasyGame; //Game difficulty
	public static bool isPaused; //Whether the game is paused
	public static bool isNavigationEnabled = true; //Whether to display task navigation

	//Pause or resume the game
    public static bool Pause(string type) {
        isPaused = !isPaused;
        Time.timeScale = isPaused? 0f : 1f;
		Cursor.visible = isPaused;
		Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
        EventManager.Instance.TriggerEvent("OnPause", type);
        return isPaused;
    }
}
