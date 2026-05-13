using System;
using UnityEngine;

public class TurnTimer : MonoBehaviour
{
    [SerializeField] private float turnDuration = 60f;

    public event Action OnTimerExpired;

    private float timeRemaining;
    private bool isRunning;

    public float TimeRemaining => timeRemaining;
    public bool IsRunning => isRunning;

    public void StartTimer()
    {
        timeRemaining = turnDuration;
        isRunning = true;
    }

    public void ResumeTimer()
    {
        isRunning = true;
    }

    public void StopTimer()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        StopTimer();
        timeRemaining = turnDuration;
    }

    private void Update()
    {
        if (!isRunning)
        {
            return;
        }

        timeRemaining -= Time.deltaTime;

        if (timeRemaining <= 0f)
        {
            timeRemaining = 0f;
            isRunning = false;
            OnTimerExpired?.Invoke();
        }
    }
}
