using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private float shrinkStep = 0.6f;           // world units removed per event
    [SerializeField] private float minHalfWidthFraction = 0.4f; // floor = 40% of initial

    [Header("Timing")]
    [SerializeField] private FloatValue shrinkInterval;  // wait between shrinks
    [SerializeField] private FloatValue shrinkDuration;  // length of each transition

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private float initialHalfWidth;
    private float minHalfWidth;

    private enum ShrinkState { Waiting, Shrinking }
    private ShrinkState state;

    private float timer;
    private float shrinkFrom;
    private float shrinkTo;

    void Start()
    {
        initialHalfWidth = halfWidthPlayArea.Value;
        minHalfWidth = initialHalfWidth * minHalfWidthFraction;
        ResetState();
    }

    void OnEnable() => gameStartEvent.OnRaised += ResetState;
    void OnDisable() => gameStartEvent.OnRaised -= ResetState;

    void Update()
    {
        timer += Time.deltaTime;

        switch (state)
        {
            case ShrinkState.Waiting:
                if (halfWidthPlayArea.Value > minHalfWidth && timer >= shrinkInterval.Value)
                    BeginShrink();
                break;

            case ShrinkState.Shrinking:
                float t = Mathf.Clamp01(timer / shrinkDuration.Value);
                float newHalfWidth = Mathf.SmoothStep(shrinkFrom, shrinkTo, t);
                halfWidthPlayArea.Set(newHalfWidth);
                UpdateWalls(newHalfWidth);

                if (t >= 1f)
                {
                    state = ShrinkState.Waiting;
                    timer = 0f;
                }
                break;
        }
    }

    private void BeginShrink()
    {
        shrinkFrom = halfWidthPlayArea.Value;
        shrinkTo = Mathf.Max(minHalfWidth, shrinkFrom - shrinkStep);
        state = ShrinkState.Shrinking;
        timer = 0f;
    }

    private void UpdateWalls(float halfWidth)
    {
        if (leftWall != null) leftWall.position = new Vector3(-halfWidth, 0f, 0f);
        if (rightWall != null) rightWall.position = new Vector3(halfWidth, 0f, 0f);
    }

    private void ResetState()
    {
        state = ShrinkState.Waiting;
        timer = 0f;
        UpdateWalls(initialHalfWidth);
    }
}