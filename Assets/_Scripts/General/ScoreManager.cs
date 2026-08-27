using System;
using TMPro;
using UnityEngine;

public class ScoreManager : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI scoreText;

    public int CurrentScore { get; private set; }
    public event Action<int> OnScoreChanged;

    public void ResetScore()
    {
        CurrentScore = 0;
        UpdateDisplay();
    }

    public void AddScore(int amount)
    {
        CurrentScore += amount;
        UpdateDisplay();
        OnScoreChanged?.Invoke(CurrentScore);
    }

    private void UpdateDisplay()
    {
        if (scoreText != null)
            scoreText.text = CurrentScore.ToString();
    }
}