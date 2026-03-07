using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntegerValue health;

    private VisualElement[] heartElements;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        heartElements = new VisualElement[]
        {
            root.Q<VisualElement>("heart-0"),
            root.Q<VisualElement>("heart-1"),
            root.Q<VisualElement>("heart-2"),
            root.Q<VisualElement>("heart-3"),
            root.Q<VisualElement>("heart-4"),
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
        for (int i = 0; i < heartElements.Length; i++)
        {
            bool filled = i < currentHealth;
            heartElements[i].EnableInClassList("heart--filled", filled);
            heartElements[i].EnableInClassList("heart--lost", !filled);
        }
    }
}