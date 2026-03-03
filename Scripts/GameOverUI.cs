using UnityEngine;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private VisualElement overlay;
    private Button restartBtn;
    private Button exitBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        overlay = root.Q<VisualElement>("game-over-overlay");
        restartBtn = root.Q<Button>("restart-btn");
        exitBtn = root.Q<Button>("exit-btn");

        restartBtn.clicked += OnRestartClicked;
        exitBtn.clicked += OnExitClicked;

        gameOverEvent.OnRaised += OnGameOver;
        gameStartEvent.OnRaised += OnGameStart;

        overlay.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        restartBtn.clicked -= OnRestartClicked;
        exitBtn.clicked -= OnExitClicked;

        gameOverEvent.OnRaised -= OnGameOver;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    private void OnGameOver() => overlay.style.display = DisplayStyle.Flex;
    private void OnGameStart() => overlay.style.display = DisplayStyle.None;

    private void OnRestartClicked() => gameStartEvent.Raise();

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}