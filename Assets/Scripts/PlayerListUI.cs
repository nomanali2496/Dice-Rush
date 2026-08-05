using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PlayerListUI : MonoBehaviour
{
    [Header("UI References")]
    public GameObject profilePrefab;
    public Transform contentParent;

    // Yeh list bahar se aayegi (GameManager / Lobby se)
    private List<PlayerInfo> currentPlayers = new List<PlayerInfo>();

    [System.Serializable]
    public class PlayerInfo
    {
        public string playerName;
        public int score;
        public Sprite avatar;
        public string playerId; // networking ke liye useful
    }

    public void UpdatePlayerList(List<PlayerInfo> players)
    {
        currentPlayers = players;
        RefreshUI();
    }

    void RefreshUI()
    {
        // Purane items delete karo
        foreach (Transform child in contentParent)
        {
            Destroy(child.gameObject);
        }

        // Naye players add karo
        foreach (var player in currentPlayers)
        {
            GameObject item = Instantiate(profilePrefab, contentParent);

            // Name
            var nameText = item.transform.Find("Text (TMP)")?.GetComponent<TextMeshProUGUI>();
            if (nameText != null)
                nameText.text = player.playerName;

            // Score
            var scoreText = item.transform.Find("ScoreText")?.GetComponent<TextMeshProUGUI>();
            if (scoreText != null)
                scoreText.text = player.score.ToString();

            // Avatar (optional)
            var avatarImg = item.transform.Find("Image")?.GetComponent<Image>();
            if (avatarImg != null && player.avatar != null)
                avatarImg.sprite = player.avatar;

            // Click
            Button btn = item.GetComponent<Button>();
            if (btn == null) btn = item.AddComponent<Button>();

            string name = player.playerName;
            string id = player.playerId;

            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnPlayerClicked(name, id));
        }
    }

    void OnPlayerClicked(string name, string id)
    {
        Debug.Log($"Player clicked: {name} (ID: {id})");
        // Yahan profile open / challenge / kick etc. ka logic likhna
    }
}