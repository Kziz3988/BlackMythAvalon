using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatusSlider : MonoBehaviour {
	public float invulnerableTime = 2;
	public GameObject hpBackground;
	public GameObject armorBackground;
	public GameObject hpBar;
	public GameObject armorBar;
	public GameObject hpResidualBar;
	public GameObject armorResidualBar;

	public bool isInvulnerable;
	public float hpMax, armorMax, hp, armor;
	
	float hpWidthMax, armorWidthMax, hpHeight, armorHeight;
	Transform player;
	Canvas canvas;
	RectTransform hpBarRect, armorBarRect, hpResidualRect, armorResidualRect;

	void Start() {
		RectTransform hpRect = hpBackground.GetComponent<RectTransform>();
		RectTransform armorRect = armorBackground.GetComponent<RectTransform>();
		hpWidthMax = hpRect.rect.width;
		hpHeight = hpRect.rect.height;
		armorWidthMax = armorRect.rect.width;
		armorHeight = armorRect.rect.height;

		hpBarRect = hpBar.GetComponent<RectTransform>();
		armorBarRect = armorBar.GetComponent<RectTransform>();
		hpResidualRect = hpResidualBar.GetComponent<RectTransform>();
		armorResidualRect = armorResidualBar.GetComponent<RectTransform>();

		hpBarRect.anchoredPosition = hpRect.anchoredPosition;
		armorBarRect.anchoredPosition = armorRect.anchoredPosition;
		hpResidualRect.anchoredPosition = hpRect.anchoredPosition;
		armorResidualRect.anchoredPosition = armorRect.anchoredPosition;

		Initialize();
	}

	void Update() {
		if(gameObject.tag == "Enemy" || gameObject.tag == "Boss") {
			//Face player
			Vector3 dir = player.position - canvas.transform.position;
			dir.y = 0;
			canvas.transform.rotation = Quaternion.LookRotation(dir);
		}
	}
	
	void Initialize() {
		isInvulnerable = false;
		player = GameObject.Find("Player").transform;
		canvas = hpBackground.GetComponentInParent<Canvas>();
		Injure(0, 0, 0, null);
	}

	//Player get injured
	public void Injure(float dmgToHP, float dmgToArmor, float penetration, Transform source) {
		if(!isInvulnerable) {
			isInvulnerable = true;
			//Armor takes damage first, then HP takes damage after armor reduction
			float curArmor = Mathf.Max(0, armor - dmgToArmor);
			float curHP = Mathf.Max(0, hp - dmgToHP / (Mathf.Log10(curArmor + 1) * Mathf.Clamp(1 - penetration, 0, 1) + 1));
			armor = curArmor;
			hp = curHP;
			float targetHPWidth = hpWidthMax * curHP / hpMax;
			float targetArmorWidth = armorWidthMax * curArmor / armorMax;
			hpBarRect.sizeDelta = new Vector2(targetHPWidth, hpHeight);
			armorBarRect.sizeDelta = new Vector2(targetArmorWidth, armorHeight);
			StartCoroutine(Deduction(hpResidualRect, targetHPWidth, hpHeight));
			StartCoroutine(Deduction(armorResidualRect, targetArmorWidth, armorHeight));
			if(gameObject.tag == "Enemy" || gameObject.tag == "Boss") {
				gameObject.GetComponent<EnemyController>().GetHit(source);
			}
			if(curHP <= 0) {
				switch(gameObject.tag) {
					case "Enemy":
						EventManager.Instance.TriggerEvent("OnEnemyDefeat", gameObject.name + " " + gameObject.GetComponent<EnemyController>().index.ToString());
						break;
					case "Boss":
						EventManager.Instance.TriggerEvent("OnBossDefeat", gameObject.name);
						break;
				}
			}
		}
	}

	//HP residual deduction
	IEnumerator Deduction(RectTransform rt, float targetWidth, float height) {
		float time = 0;
		float startWidth = rt.rect.width;
		float deltaWidth = startWidth - targetWidth;
		while(invulnerableTime - time > 0) {
			//Trigonometric interpolation
			rt.sizeDelta = new Vector2(startWidth - deltaWidth * Mathf.Sin(time / (2 * invulnerableTime) * Mathf.PI), height);
			yield return null;
			time += Time.deltaTime;
		}
		rt.sizeDelta = new Vector2(targetWidth, height);
		isInvulnerable = false;
	}
}
