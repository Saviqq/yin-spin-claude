using UnityEngine;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private IntegerValue score;

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private VisualElement overlay;
    private Label finalScoreLabel;
    private Button restartBtn;
    private Button exitBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        overlay = root.Q<VisualElement>("game-over-overlay");
        finalScoreLabel = root.Q<Label>("final-score-label");
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

    private void OnGameOver()
    {
        finalScoreLabel.text = $"SCORE  {score.Value}";
        overlay.style.display = DisplayStyle.Flex;
    }

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