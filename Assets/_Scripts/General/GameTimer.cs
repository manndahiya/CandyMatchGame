using System;
using TMPro;
using UnityEngine;

public class GameTimer : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timerText;
    [SerializeField] private float duration = 60f;

    public event Action OnTimerExpired;

    private float timeRemaining;
    private bool isRunning;

    public void StartTimer()
    {
        timeRemaining = duration;
        isRunning = true;
        UpdateDisplay();
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    private void Update()
    {
        if (!isRunning) return;

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            UpdateDisplay();
            OnTimerExpired?.Invoke();
            return;
        }

        UpdateDisplay();
    }

    private void UpdateDisplay()
    {
        int minutes = Mathf.FloorToInt(timeRemaining / 60f);
        int seconds = Mathf.FloorToInt(timeRemaining % 60f);
        timerText.text = $"{minutes}:{seconds:00}";
    }
}