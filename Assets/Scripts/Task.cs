using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Class of game tasks
public class Task {
	public string taskName; //Name of current task
	public string brief; //Brief introduction of current task
	public List<string> preTasks; //The tasks must be completed before accepting current task
	public List<string> followUps; //The tasks starts right after current task is completed
	public bool isMainLine;
	public bool isStarted;
	public bool isCompleted;
	public List<SubTask> taskChain; //The subtasks of current task
	int currentState;

	public Task(string taskName, string brief = "", bool isMainLine = false) {
		this.taskName = taskName;
		this.brief = brief;
		preTasks = new List<string>();
		followUps = new List<string>();
		this.isMainLine = isMainLine;
		isStarted = false;
		isCompleted = false;
		currentState = 0;
		taskChain = new List<SubTask>();
	}

	//Initialize the current state of task
	//Should be called after taskChain is initialized
	public void InitState() {
		if(taskChain.Count > 0) {
			EventManager.Instance.AddListener(taskChain[0].condition.Key, OnTaskCompleted);
		}
		else {
			CompleteTask();
		}
	}

	void OnTaskCompleted(string message) {
		if(taskChain[currentState].condition.Value == null || message == taskChain[currentState].condition.Value) {
			EventManager.Instance.RemoveListener(taskChain[currentState].condition.Key, OnTaskCompleted);
			currentState++;
			if(currentState == taskChain.Count) {
				CompleteTask();
			}
			else {
				EventManager.Instance.AddListener(taskChain[currentState].condition.Key, OnTaskCompleted);
			}
			EventManager.Instance.TriggerEvent("OnTaskAdvance", taskName);
		}
	}

	public void CompleteTask() {
		EventManager.Instance.TriggerEvent("OnTaskComplete", taskName);
		isCompleted = true;
	}

	//Get description of current subtask
	public string GetTaskInfo() {
		return taskChain[currentState].content;
	}

	//Get current task place
	public Vector3? GetTaskPlace() {
		return isCompleted ? null : taskChain[currentState].place;
	}
}

//Class of subtasks
public class SubTask {
	public string content;
	public KeyValuePair<string, string> condition; //(Event, Message (null represents any))
	public Vector3? place;
	public SubTask(string content, KeyValuePair<string, string> condition) {
		this.content = content;
		this.condition = condition;
		place = null;
	}
}
