using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Text;
using System.Security.Cryptography;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GlobalVariables : MonoBehaviour {

	public static GlobalVariables Instance; //The instance for the singleton pattern
    public string initSeed;
    public string seed; //Random seed (after MD5 hashing)
    public System.Random random; //Random generator
    public float playerHP; //HP of player
    public float playerHPMax; //Max HP of player
    public float playerArmor; //Armor of player
    public float playerArmorMax; //Max armor of player
    public int playerItemCount; //Total number of player's items
    public Vector2Int startPos; //The initial position of player

    public HashSet<string> tagsWithHP; //Types of gameobjects that can be hit
    public Dictionary<string, List<int>> enemyData; //Name, HP (0), armor (1) and weapon (2) of enemy
    public List<List<float>> enemyWeapons; //Damage to HP (0), Damage to armor (1) and Penetration (2) of enemy weapons
     
    void Awake() {
		if(Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else {
            Destroy(gameObject);
        }

        Initialize();

        tagsWithHP = new HashSet<string>();
        enemyData = new Dictionary<string, List<int>>();
        enemyWeapons = new List<List<float>>();

        tagsWithHP.Add("Player");
        tagsWithHP.Add("Enemy");
        tagsWithHP.Add("Boss");

        //Player configs are below
		playerHPMax = 100f;
		playerArmorMax = 50f;

        //Enemy configs are below 
        enemyData.Add("Skeleton", new List<int>{5, 1, 0});
        enemyData.Add("Lancelot", new List<int>{200, 100, 1});
        enemyData.Add("Modred", new List<int>{300, 100, 2});
        enemyWeapons.Add(new List<float>{2, 1, 0.05f}); //Fists of Skeleton
        enemyWeapons.Add(new List<float>{15, 2, 0.2f}); //The Aroundight of Lancelot
        enemyWeapons.Add(new List<float>{20, 5, 0.25f}); //The Clarent of Modred
	}

    void Update() {
        if(SceneManager.GetActiveScene().name == "MainMenu") {
            GameObject seedInput = GameObject.Find("SeedInputField");
            if(seedInput != null) {
                initSeed = seedInput.GetComponent<InputField>().text;
                GenerateSeed(string.IsNullOrEmpty(initSeed) ? "test" : initSeed);
            }
        }
    }
    
    void Initialize() {
        GenerateSeed("test");
        playerHP = 100f;
        playerArmor = 50f;
        playerItemCount = 0;
    }

    //Generate MD5 seed
    public void GenerateSeed(string originalSeed) {
        using(MD5 md5 = MD5.Create()) {
            byte[] inputBytes = Encoding.ASCII.GetBytes(originalSeed);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            StringBuilder sb = new StringBuilder();
            for(int i = 0; i < hashBytes.Length; i++) {
                sb.Append(hashBytes[i].ToString("X2"));
            }
            seed = sb.ToString();
        }
        random = new System.Random(GetSeedSegment(0, 8));
    }

    //Get a decimal seed segment
    public int GetSeedSegment(int head, int length = 8) {
        int newLength = Mathf.Min(length, 8);
        int newHead = 32 - Mathf.Max(32 - Mathf.Max(0, head), newLength);
        return Mathf.Abs((int)System.Convert.ToUInt32(seed.Substring(newHead, newLength), 16));
    }

    //Get a random number
    public float GetRandom(float floor = 0, float ceiling = 1) {
        return (float)random.NextDouble() * Mathf.Abs(ceiling - floor) + Mathf.Min(floor, ceiling);
    }
}
