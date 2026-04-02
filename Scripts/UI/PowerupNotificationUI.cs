using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PowerupNotificationUI : MonoBehaviour
{
    [SerializeField] private StringEvent powerupMessageEvent;
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private float displayDuration = 2.5f;

    private Label label;
    private Coroutine activeHideCoroutine;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        label = root.Q<Label>("powerup-label");
        label.style.display = DisplayStyle.None;

        powerupMessageEvent.OnRaised += OnMessage;
        gameStartEvent.OnRaised += HideImmediate;
        gamePausedEvent.OnRaised += HideImmediate;
    }

    void OnDisable()
    {
        powerupMessageEvent.OnRaised -= OnMessage;
        gameStartEvent.OnRaised -= HideImmediate;
        gamePausedEvent.OnRaised -= HideImmediate;
    }

    private void OnMessage(string msg)
    {
        label.text = msg;
        label.style.display = DisplayStyle.Flex;

        if (activeHideCoroutine != null)
        {
            StopCoroutine(activeHideCoroutine);
        }

        activeHideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        label.style.display = DisplayStyle.None;
        activeHideCoroutine = null;
    }

    private void HideImmediate()
    {
        if (activeHideCoroutine != null)
        {
            StopCoroutine(activeHideCoroutine);
            activeHideCoroutine = null;
        }
        label.style.display = DisplayStyle.None;
    }
}