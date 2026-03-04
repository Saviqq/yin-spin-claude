using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Player")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    void Awake()
    {
        OnGameStart();
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
        halfHeightPlayArea.Set(Camera.main.orthographicSize);
        halfWidthPlayArea.Set(Camera.main.orthographicSize * Camera.main.aspect);
        Cursor.visible = false;
    }

    private void OnGameOver()
    {
        Cursor.visible = true;
    }

}