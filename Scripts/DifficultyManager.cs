using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private float shrinkStep = 0.6f;
    [SerializeField] private float minHalfWidthFraction = 0.4f;
    [SerializeField] private FloatValue shrinkInterval;
    [SerializeField] private FloatValue shrinkDuration;

    [Header("Score Scaling")]
    [SerializeField] private IntegerValue score;
    [SerializeField] private IntegerValue spawnCount;
    [SerializeField] private FloatValue orbSpeed;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent expandPlayAreaEvent;

    private float initialHalfWidth;
    private float minHalfWidth;
    private int baseSpawnCount;
    private float baseOrbSpeed;

    private enum ShrinkState { Waiting, Shrinking, Expanding }
    private ShrinkState state;

    private float timer;
    private float shrinkFrom;
    private float shrinkTo;
    private float expandFrom;
    private float expandTo;

    void Start()
    {
        initialHalfWidth = halfWidthPlayArea.Value;
        minHalfWidth = initialHalfWidth * minHalfWidthFraction;
        baseSpawnCount = spawnCount.Value;
        baseOrbSpeed = orbSpeed.Value;
        ResetState();
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += ResetState;
        score.OnChange += OnScoreChanged;
        expandPlayAreaEvent.OnRaised += BeginExpand;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= ResetState;
        score.OnChange -= OnScoreChanged;
        expandPlayAreaEvent.OnRaised -= BeginExpand;
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

            case ShrinkState.Expanding:
                float te = Mathf.Clamp01(timer / shrinkDuration.Value);
                float newHalfWidthE = Mathf.SmoothStep(expandFrom, expandTo, te);
                halfWidthPlayArea.Set(newHalfWidthE);
                UpdateWalls(newHalfWidthE);

                if (te >= 1f)
                {
                    state = ShrinkState.Waiting;
                    timer = 0f;
                }
                break;
        }
    }

    private void OnScoreChanged(int newScore)
    {
        bool shouldIncreaseDifficulty = newScore != 0 && newScore % Constants.DIFFICULTY_TRESHOLD != 0;
        if (shouldIncreaseDifficulty)
        {
            IncreaseDifficulty(newScore / Constants.DIFFICULTY_TRESHOLD);
        }
    }

    private void IncreaseDifficulty(int difficulty)
    {
        spawnCount.Set(Mathf.Min(Constants.MAX_ORB_SPWAN, baseSpawnCount + difficulty));
        orbSpeed.Set(Mathf.Min(Constants.MAX_ORB_SPEED, baseOrbSpeed + (difficulty * Constants.SPEED_SCALE_FACTOR)));
    }

    private void BeginShrink()
    {
        shrinkFrom = halfWidthPlayArea.Value;
        shrinkTo = Mathf.Max(minHalfWidth, shrinkFrom - shrinkStep);
        state = ShrinkState.Shrinking;
        timer = 0f;
    }

    private void BeginExpand()
    {
        expandFrom = halfWidthPlayArea.Value;
        expandTo = Mathf.Min(initialHalfWidth, expandFrom + shrinkStep);
        state = ShrinkState.Expanding;
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
        spawnCount.Set(baseSpawnCount);
        orbSpeed.Set(baseOrbSpeed);

    }
}