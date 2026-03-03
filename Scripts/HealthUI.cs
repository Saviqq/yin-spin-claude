using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntegerValue health;

    private Label[] heartLabels;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        heartLabels = new Label[]
        {
            root.Q<Label>("heart-0"),
            root.Q<Label>("heart-1"),
            root.Q<Label>("heart-2"),
        };

        health.OnChange += OnHealthChange;
        OnHealthChange(health.Value);
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChange;
    }

    private void OnHealthChange(int currentHealth)
    {
        for (int i = 0; i < heartLabels.Length; i++)
        {
            bool filled = i < currentHealth;
            heartLabels[i].EnableInClassList("heart--lost", !filled);
        }
    }
}