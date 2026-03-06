using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private IntegerValue score;

    private Label scoreLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Q<Label>("score-label");

        score.OnChange += OnScoreChange;
        OnScoreChange(score.Value);
    }

    void OnDisable()
    {
        score.OnChange -= OnScoreChange;
    }

    private void OnScoreChange(int score)
    {
        scoreLabel.text = score.ToString();
    }
}