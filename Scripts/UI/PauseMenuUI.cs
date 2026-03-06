using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private GameEvent gameResumedEvent;
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent resumeButtonClickedEvent;

    private VisualElement overlay;
    private Button resumeBtn;
    private Button restartBtn;
    private Button exitBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        overlay = root.Q<VisualElement>("pause-overlay");
        resumeBtn = root.Q<Button>("pause-resume-btn");
        restartBtn = root.Q<Button>("pause-restart-btn");
        exitBtn = root.Q<Button>("pause-exit-btn");

        resumeBtn.clicked += OnResumeClicked;
        restartBtn.clicked += OnRestartClicked;
        exitBtn.clicked += OnExitClicked;

        gamePausedEvent.OnRaised += OnPause;
        gameResumedEvent.OnRaised += OnResume;
        gameStartEvent.OnRaised += OnResume;

        overlay.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        resumeBtn.clicked -= OnResumeClicked;
        restartBtn.clicked -= OnRestartClicked;
        exitBtn.clicked -= OnExitClicked;

        gamePausedEvent.OnRaised -= OnPause;
        gameResumedEvent.OnRaised -= OnResume;
        gameStartEvent.OnRaised -= OnResume;
    }

    private void OnPause() => overlay.style.display = DisplayStyle.Flex;
    private void OnResume() => overlay.style.display = DisplayStyle.None;

    private void OnResumeClicked() => resumeButtonClickedEvent.Raise();
    private void OnRestartClicked() => gameStartEvent.Raise();

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}