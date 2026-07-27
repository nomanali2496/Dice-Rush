using UnityEngine;
using TMPro;

public class LoserScoreScreen : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    public void Setup(int score)
    {
        if (scoreText != null)
        {
            // Use TextMeshPro's rich text alignment to push the score to the right
            scoreText.text = $"Score:<line-height=0>\n<align=\"right\">{score}</align>";
        }
    }
}
