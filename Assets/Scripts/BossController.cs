using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BossController : EnemyController {
	const int JumpSlash = 0;
	const int SpinSlash = 1;
	const int SpinJumpSlash = 2;

    readonly float sightDist = 100; //The max distance that enemy can see player

	void Start() {
		index = 0;
		enemyCamera = transform.Find("Camera").GetComponent<Camera>();
		enemyCamera.enabled = false;
		Initialize();
	}

	void Update() {
		if(!isActive) {
			LeaveBattle();
		}
		else if(DetectHostile(10)) {
			Rotate(2.5f);
			Run(3, 2);
			Attack(1.55f);
		}
		PlayAnim();
	}

	//Enemy get hit
	public new void GetHit(Transform source) {
		hostile = source ?? hostile;
		isAttacking = false;
		isRunning = false;
		isHit = true;
		PlayAnim();
		EventManager.Instance.TriggerEvent("OnBossHurt", gameObject.name);
	}
}
