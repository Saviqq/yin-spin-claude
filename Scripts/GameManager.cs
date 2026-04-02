using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class GameManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Player")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;
    // [SerializeField] private AudioClip backgroundMusic;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private GameEvent gameResumedEvent;
    [SerializeField] private GameEvent resumeButtonClickedEvent;

    private AudioSource audioSource;

    private bool isPaused = false;
    private bool isGameOver = false;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        OnGameStart();
    }

    void OnEnable()
    {
        health.OnChange += OnHealthChanged;
        gameStartEvent.OnRaised += OnGameStart;
        gameOverEvent.OnRaised += OnGameOver;
        resumeButtonClickedEvent.OnRaised += Resume;
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChanged;
        gameStartEvent.OnRaised -= OnGameStart;
        gameOverEvent.OnRaised -= OnGameOver;
        resumeButtonClickedEvent.OnRaised -= Resume;
    }

    void Update()
    {
        if (isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        gamePausedEvent.Raise();
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        gameResumedEvent.Raise();
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
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;

        audioSource.Play();
        health.Reset();
        score.Reset();
        halfHeightPlayArea.Set(Camera.main.orthographicSize);
        halfWidthPlayArea.Set(Camera.main.orthographicSize * Camera.main.aspect);
    }

    private void OnGameOver()
    {
        audioSource.Stop();
        isGameOver = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
    }
}