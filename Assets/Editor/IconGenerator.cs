using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;

public class IconGenerator : EditorWindow {
	[MenuItem("Assets/Generate Prefab Thumbnail")]
    static void GenerateIcon() {
		string path = EditorUtility.OpenFilePanel("Select Prefab File", "Assets", "prefab");
		if(string.IsNullOrEmpty(path)) return;
		string fileName = Path.GetFileNameWithoutExtension(path);
		string newFilePath1 = path.Replace("\\", "/");
        string newFilePath2 = newFilePath1.Replace("//", "/").Trim();
        newFilePath2 = newFilePath2.Replace("///", "/").Trim();
        newFilePath2 = newFilePath2.Replace("\\\\", "/").Trim();
		path = newFilePath2.Substring(newFilePath2.IndexOf("Assets"));
		GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
		if(prefab == null) return;
		Texture2D Tex = AssetPreview.GetAssetPreview(prefab);
		byte[] bytes = Tex.EncodeToPNG();
		File.WriteAllBytes(Application.dataPath + "/../Assets/Resources/Images/" + fileName + ".png", bytes);
		Debug.Log("Prefab icon saved to Resources/Images");
	}
}
