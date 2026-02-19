using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TaskManager : MonoBehaviour {
	public static TaskManager Instance; //The instance for the singleton pattern
	public Dictionary<string, Task> tasks = new Dictionary<string, Task>();
	GameObject taskFrame;
	Text taskText;
	RectTransform taskRect;
	List<Task> currentTasks;

	void Awake() {
		if(Instance == null) {
            Instance = this;
        }
        else {
            Destroy(gameObject);
        }
		taskFrame = GameObject.Find("TaskMenu");
		GameObject tt = GameObject.Find("TaskText");
		taskText = tt.GetComponent<Text>();
		taskRect = tt.GetComponent<RectTransform>();
	}

	void Start() {
		Initialize();
	}

	void Initialize() {
		EventManager.Instance.AddListener("OnTaskBegin", OnTaskBegan);
		EventManager.Instance.AddListener("OnTaskComplete", OnTaskCompleted);
		EventManager.Instance.AddListener("OnTaskAdvance", OnTaskAdvanced);
		currentTasks = new List<Task>();
		taskFrame.SetActive(false);

		tasks.Clear();

		Task task0 = new Task("Tutorial", "传说的源点", true);
		task0.followUps.Add("DefeatModred");
		task0.followUps.Add("FindWeapons");
		task0.taskChain.Add(new SubTask("与贝德维尔交谈", new KeyValuePair<string, string>("OnDialogBegin", "Tutorial")));
		task0.taskChain.Add(new SubTask("遵循贝德维尔的指导", new KeyValuePair<string, string>("OnTalkFinish", "Bedivere")));
		task0.InitState();
		tasks.Add("Tutorial", task0);

		Task task1 = new Task("DefeatModred", "踏上征途", true);
		task1.taskChain.Add(new SubTask("找到你的对手", new KeyValuePair<string, string>("OnTalkFinish", "Modred")));
		task1.taskChain.Add(new SubTask("击败莫德雷德", new KeyValuePair<string, string>("OnBossDefeat", "Modred")));
		task1.taskChain.Add(new SubTask("与莫德雷德对话", new KeyValuePair<string, string>("OnTalkFinish", "Modred")));
		task1.taskChain.Add(new SubTask("彻底击败莫德雷德", new KeyValuePair<string, string>("OnBossDefeat", "Modred")));
		task1.InitState();
		tasks.Add("DefeatModred", task1);

		Task task2 = new Task("FindWeapons", "扩充武器库", false);
		task2.taskChain.Add(new SubTask("获得4把武器或其它道具", new KeyValuePair<string, string>("OnItemGet", "4")));
		task2.InitState();
		tasks.Add("FindWeapons", task2);

		EventManager.Instance.TriggerEvent("OnTaskBegin", "Tutorial");
	}

	void StartTask(string name) {
		if(tasks.ContainsKey(name)) {
			Task task = tasks[name];
			if(!task.isCompleted && !task.isStarted) {
				int i;
        		for (i = 0; i < task.preTasks.Count; i++) {
					if(!tasks[task.preTasks[i]].isCompleted) {
						break;
					}
				}
				if(i == task.preTasks.Count) {
					//All the pretasks are completed
					task.isStarted = true;
					if(task.isMainLine) {
						//Mainline tasks have the highest priorty
						currentTasks.Insert(0, task);
					}
					else {
						currentTasks.Add(task);
					}
					taskFrame.SetActive(true);
				}
			}
		}
	}

	//Skip task (Only for debugging)
	public void SkipTask() {
		if(currentTasks.Count > 0) {
			currentTasks[0].CompleteTask();
		}
	}


	//Display task information on GUI
	void ShowTaskInfo() {
		string text = "<size=20>当前任务</size>\n";
		for(int i = 0; i < currentTasks.Count; i++) {
			text += "<color=black>" + currentTasks[i].brief + (currentTasks[i].isMainLine ? "" : "（可选）") + "</color>\n";
			text += currentTasks[i].GetTaskInfo() + "\n\n";
		}
		taskText.text = text;
		taskRect.sizeDelta = new Vector2(180, currentTasks.Count * 60);
	}

	//Get current task place
	public Vector3? GetTaskPlace() {
		return currentTasks.Count == 0 ? null : currentTasks[0].GetTaskPlace();
	}

	void OnTaskBegan(string message) {
		StartTask(message);
		ShowTaskInfo();
		EventManager.Instance.TriggerEvent("OnNavigate", null);
	}

	void OnTaskCompleted(string message) {
		StartCoroutine(GUIController.Instance.Alert("“" + tasks[message].brief + "” 已完成！"));
		currentTasks.RemoveAll(task => task.taskName == message);
		for(int i = 0; i < tasks[message].followUps.Count; i++) {
			EventManager.Instance.TriggerEvent("OnTaskBegin", tasks[message].followUps[i]);
		}
		taskFrame.SetActive(currentTasks.Count > 0);
	}

	void OnTaskAdvanced(string message) {
		ShowTaskInfo();
	}
}
