using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyController : MonoBehaviour {
	const int Smash= 0;

	public const int HP = 0;
	public const int Armor = 1;
	public const int Weapon = 2;
	
	public const int DamageToHP = 0;
	public const int DamageToArmor = 1;
	public const int Penetration = 2;

	public int index;
	protected Camera enemyCamera;
	protected Transform player;
	public Transform hostile; //The current hostile creature (May be player, other monsters, etc.)
    readonly float sightDist = 100; //The max distance that enemy can see player
	protected bool isRunning, isHit;
	protected bool isAttacking;
	protected int attackState;
	public bool isActive; //Whether enemy is hunting its hostile
	
	void Start() {
		EventManager.Instance.AddListener("OnEnemyDefeat", OnDefeated);
		enemyCamera = transform.Find("Camera").GetComponent<Camera>();
		enemyCamera.enabled = false;
		Initialize();
	}

	void Update() {
		if(!isActive || !hostile.gameObject.activeSelf) { //Not active or the hostile has been defeated
			LeaveBattle();
		}
		else if(DetectHostile(10)) {
			Rotate(2.5f);
			Run(3, 0.5f);
			Attack(0.5f);
		}
		PlayAnim();
	}

	public void Initialize() {
		isActive = true;
		player = GameObject.Find("Player").transform;
		StatusSlider ss = GetComponent<StatusSlider>();
		ss.hpMax = GlobalVariables.Instance.enemyData[gameObject.name][HP];
		ss.hp = GlobalVariables.Instance.enemyData[gameObject.name][HP];
		ss.armorMax = GlobalVariables.Instance.enemyData[gameObject.name][Armor];
		ss.armor = GlobalVariables.Instance.enemyData[gameObject.name][Armor];
		GetComponent<Animator>().SetBool("isDefeated", false);
		LeaveBattle();
	}

	//Drop out of battle
	protected void LeaveBattle() {
		hostile = player;
		isAttacking = false;
		isRunning = false;
	}

	//Join battle
	protected void JoinBattle(Transform target) {
		hostile = target;
	}

	//Whether the enemy discovers its hostile
	protected bool DetectHostile(float minDist) {
		if(Vector3.Distance(transform.position, hostile.position) < minDist) {
			JoinBattle(hostile);
			return true;
		}
		else {
			Plane[] planes = GeometryUtility.CalculateFrustumPlanes(enemyCamera);
			if(GeometryUtility.TestPlanesAABB(planes, hostile.GetComponent<BoxCollider>().bounds)) {
				RaycastHit hit;
				//Ignore trigger type colliders
				if(Physics.Raycast(enemyCamera.transform.position, hostile.position - enemyCamera.transform.position, out hit, sightDist, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)) {
					if(hit.transform == hostile) {
						//Debug.Log("target detected");
						JoinBattle(hostile);
						return true;
					}
					else {
						//Hostile is blocked
						//Debug.Log("target blocked");
						LeaveBattle();
						return false;
					}
				}
				else {
					//Hostile is out of enemy's sight distance
					//Debug.Log("target is out of distance");
					LeaveBattle();
					return false;
				}
		}
		else {
			//Hostile is out of enemy's sight range
			//Debug.Log("target is out of range");
			LeaveBattle();
			return false;
		}
		}
	}

	//Rotate
	protected void Rotate(float minDist) {
		if(!isAttacking && !isHit) {
			//Vector3 nextPos = Navigator.Instance.GetNextPosition(transform.position, hostile.position);
			//Debug.Log(nextPos);
			//Vector3 dir = Vector3.Distance(transform.position, hostile.position) < minDist ? (hostile.position - transform.position).normalized : (nextPos - transform.position).normalized;
			Vector3 dir = (hostile.position - transform.position).normalized;
			dir.y = 0;
			if(!dir.Equals(Vector3.zero)) {
				transform.rotation = Quaternion.LookRotation(dir);
			}
		}
	}

	//Run if hostile is beyond a certain distance
	protected void Run(float startRunDist, float endRunDist) {
		if(!isAttacking && !isHit) {
			float dist = Vector3.Distance(transform.position, hostile.position);
			if(dist >= startRunDist) {
				isRunning = true;
			}
			else if(dist <= endRunDist) {
				isRunning = false;
			}
		}
	}

	protected void Attack(float range) {
		if(!isRunning && !isHit) {
			isAttacking = true;
			attackState = Smash;
		}
	}

	//Play animations of enemy
	protected void PlayAnim() {
		Animator anim = GetComponent<Animator>();
		anim.SetBool("isRunning", isRunning);
		anim.SetBool("isAttacking", isAttacking);
		anim.SetInteger("attackState", attackState);
		anim.SetBool("isHit", isHit);
	}

	//Enemy get hit
	public void GetHit(Transform source) {
		hostile = source ?? hostile;
		isAttacking = false;
		isRunning = false;
		isHit = true;
		PlayAnim();
		EventManager.Instance.TriggerEvent("OnEnemyHurt", gameObject.name);
	}

	protected void FinishAttack() {
		isAttacking = false;
		GetComponent<Animator>().SetBool("isAttacking", isAttacking);
	}

	protected void FinishHit() {
		isHit = false;
		GetComponent<Animator>().SetBool("isHit", isHit);
	}

	protected void OnTriggerEnter(Collider other) {
		if(isAttacking && GlobalVariables.Instance.tagsWithHP.Contains(other.tag) && !other.gameObject.Equals(gameObject) && (!(other.gameObject.tag == "Enemy" || other.gameObject.tag == "Boss") || other.gameObject.GetComponent<EnemyController>().isActive)) {
			List<float> weaponData = GlobalVariables.Instance.enemyWeapons[GlobalVariables.Instance.enemyData[gameObject.name][Weapon]];
			other.gameObject.GetComponent<StatusSlider>().Injure(weaponData[DamageToHP], weaponData[DamageToArmor], weaponData[Penetration], transform);
		}
	}

	//Play defeated animation
	void OnDefeated(string message) {
		string[] msg = message.Split(' ');
		if(msg[0] == gameObject.name && msg[1] == index.ToString()) {
			GetComponent<Animator>().SetBool("isDefeated", true);
		}
	}

	//Release current gameobject
	void Defeated() {
		EventManager.Instance.RemoveListener("OnEnemyDefeat", OnDefeated);
		GameObject.Find("Enemy").GetComponent<ObjectPool>().ReleaseObject(gameObject);
	}
}
