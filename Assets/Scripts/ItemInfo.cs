using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ItemInfo : MonoBehaviour {
	public const int OneHanded = 0;
	public const int TwoHanded = 1;
	public const int LeftHandPart = 2;
	public const int RightHandPart = 3;

	RectTransform rectTransform;
	Image image;
	Text text;
	Canvas canvas;
	List<string> partName = new List<string>{"", "(双手)", "(左手)", "(右手)"};
	List<string> dataName = new List<string>{"基础伤害：", "对护甲伤害：", "穿甲系数：", "攻击范围："};

	void Start() {
		rectTransform = GetComponent<RectTransform>();
		image = GetComponent<Image>();
		text = transform.GetChild(0).GetComponent<Text>();
		canvas = GetComponentInParent<Canvas>();
		EventManager.Instance.AddListener("OnPause", OnPaused);
		Hide();
	}

	void Update() {
		Vector2 mousePos;
		RectTransformUtility.ScreenPointToLocalPointInRectangle(
			canvas.transform as RectTransform,
			Input.mousePosition,
			canvas.worldCamera,
			out mousePos
		);
		rectTransform.anchoredPosition = mousePos + new Vector2(-100 * Mathf.Sign(mousePos.x), -75 * Mathf.Sign(mousePos.y));
	}

	//Update information text to the information frame
	public void SetInfoText(int index, int part, List<float> data, float durability) {
		string name = ItemManager.itemName[index].Key + partName[part];
		string discription = ItemManager.itemName[index].Value;
		string values = "";
		List<float> basicData = ItemManager.itemData[index].GetRange(ItemManager.DamageToHP, ItemManager.Range - ItemManager.DamageToHP + 1);
		for(int i = 0; i < data.Count; i++) {
			values += data[i] < basicData[i] ? " <color=red>" + dataName[i] + (Mathf.Round(data[i] * 100) / 100).ToString() + "</color> " + '\n' : " <color=green>" + dataName[i] + (Mathf.Round(data[i] * 100) / 100).ToString() + "</color> " + '\n';
		}
		text.text = name + '\n' + discription + '\n' + values + "剩余耐久：" + durability;
	}

	public void Show() {
		image.color = new Color(0.3f, 0.3f, 0.3f, 1);
	}

	public void Hide() {
		image.color = new Color(1, 1, 1, 0);
		text.text = "";
	}

	void OnPaused(string message) {
		if(!GameConfig.isPaused) {
			Hide();
		}
	}
}
