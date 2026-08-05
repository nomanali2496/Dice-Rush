using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerDropdown : MonoBehaviour
{
    [Header("References")]
    public GameObject profilePrefab;          // Profile bg Prefab yahan drag karo
    public Transform contentParent;           // Content object yahan drag karo

    [System.Serializable]
    public class PlayerData
    {
        public string playerName;
        public int score;
        public Sprite avatar;                 // optional
    }

    public List<PlayerData> players = new List<PlayerData>();

    void Start()
    {
        RefreshList();
    }

    public void RefreshList()
    {
        // Pehle purane wale delete karo
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Naye players instantiate karo
        foreach (var player in players)
        {
            GameObject item = Instantiate(profilePrefab, contentParent);

            // Name set karo
            TextMeshProUGUI nameText = item.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = player.playerName;

            // Score set karo (agar ScoreText naam se banaya hai)
            TextMeshProUGUI scoreText = item.transform.Find("ScoreImage/ScoreText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText == null)
                scoreText = item.GetComponentInChildren<TextMeshProUGUI>(); // fallback

            // Better way: specific find
            Transform scoreTransform = item.transform.Find("ScoreText");
            if (scoreTransform != null)
                scoreTransform.GetComponent<TextMeshProUGUI>().text = player.score.ToString();

            // Avatar set karo (optional)
            Image avatarImage = item.transform.Find("Image")?.GetComponent<Image>();
            if (avatarImage != null && player.avatar != null)
                avatarImage.sprite = player.avatar;

            // Click event
            Button btn = item.GetComponent<Button>();
            if (btn == null)
                btn = item.AddComponent<Button>();

            string capturedName = player.playerName; // important for lambda
            int capturedScore = player.score;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnPlayerClicked(capturedName, capturedScore));
        }
    }

    void OnPlayerClicked(string name, int score)
    {
        Debug.Log($"Clicked: {name} | Score: {score}");
        
        // Yahan apna logic likho
        // Example: open profile panel, challenge player, etc.
    }

    // Runtime pe naya player add karne ke liye
    public void AddPlayer(string name, int score, Sprite avatar = null)
    {
        players.Add(new PlayerData { playerName = name, score = score, avatar = avatar });
        RefreshList();
    }
}