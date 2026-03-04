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
        float halfHeight = Camera.main.orthographicSize;
        float halfWidth = halfHeight * Camera.main.aspect;
        halfHeightPlayArea.Set(halfHeight);
        halfWidthPlayArea.Set(halfWidth);
    }

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