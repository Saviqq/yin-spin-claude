using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private IntegerValue health;
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    void OnEnable()
    {
        health.OnChange += OnHealthChanged;
        gameStartEvent.OnRaised += OnGameStart;
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChanged;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    private void OnHealthChanged(int current)
    {
        if (current <= 0)
        {
            gameOverEvent.Raise();
        }
    }

    private void OnGameStart()
    {
        health.Reset();
    }
}