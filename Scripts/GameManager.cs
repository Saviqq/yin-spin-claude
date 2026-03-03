using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    void Start()
    {
        Cursor.visible = false;
    }

    void OnEnable()
    {
        health.OnChange += OnHealthChanged;
        gameStartEvent.OnRaised += OnGameStart;
        gameOverEvent.OnRaised += OnGameOver;

    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChanged;
        gameStartEvent.OnRaised -= OnGameStart;
        gameOverEvent.OnRaised -= OnGameOver;
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
        score.Reset();
        Cursor.visible = false;
    }

    private void OnGameOver()
    {
        Cursor.visible = true;
    }

}