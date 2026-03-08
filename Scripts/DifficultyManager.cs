using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private float shrinkStep = 0.6f;
    [SerializeField] private float minHalfWidthFraction = 0.4f;

    [Header("Timing")]
    [SerializeField] private FloatValue shrinkInterval;
    [SerializeField] private FloatValue shrinkDuration;

    [Header("Score Scaling")]
    [SerializeField] private IntegerValue score;
    [SerializeField] private FloatValue spawnInterval;
    [SerializeField] private FloatValue orbSpeed;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private float initialHalfWidth;
    private float minHalfWidth;
    private float baseSpawnInterval;
    private float baseOrbSpeed;

    private enum ShrinkState { Waiting, Shrinking }
    private ShrinkState state;

    private float timer;
    private float shrinkFrom;
    private float shrinkTo;

    void Start()
    {
        initialHalfWidth = halfWidthPlayArea.Value;
        minHalfWidth = initialHalfWidth * minHalfWidthFraction;
        baseSpawnInterval = spawnInterval.Value;
        baseOrbSpeed = orbSpeed.Value;
        ResetState();
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += ResetState;
        score.OnChange += OnScoreChanged;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= ResetState;
        score.OnChange -= OnScoreChanged;
    }

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

    private void OnScoreChanged(int newScore)
    {
        if (newScore == 0 || newScore % Constants.SCORE_TRESHOLD != 0) return;

        float multiplier = newScore / Constants.SCORE_TRESHOLD;
        spawnInterval.Set(Mathf.Max(Constants.MIN_SPAWN_INTERVAL, baseSpawnInterval - (multiplier * Constants.SPAWN_SCALE_FACTOR)));
        orbSpeed.Set(Mathf.Min(Constants.MAX_ORB_SPEED, baseOrbSpeed + (multiplier * Constants.SPEED_SCALE_FACTOR)));
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
        spawnInterval.Set(baseSpawnInterval);
        orbSpeed.Set(baseOrbSpeed);

    }
}
