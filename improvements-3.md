# Implementation Step 3 — Score-Based Difficulty Scaling

## Scope

This step implements **Axis 2**: as score increases, spawn interval shortens and orb speed increases. Values update only when the score changes — not every frame.

### What changed
| File | Change |
|------|--------|
| `DifficultyManager.cs` | Subscribe to `score.OnChange`; update axis values in handler; `ResetState` explicitly resets both values |
| `OrbManager.cs` | `spawnInterval` and `orbSpeed`: `float` → `FloatValue`; timer initialised to `spawnInterval.Value` on start/restart |
| `Constants.cs` | Added difficulty constants |

### What stayed the same
- Everything else — `Orb`, `PlayerController`, `GameManager`, `OrbSpawner`, walls

---

## No New Event SO Needed

`IntegerValue` already has `event Action<int> OnChange` which fires with the new value every time `score.Set(...)` is called — the same pattern `GameManager` uses to watch `health`. `DifficultyManager` subscribes to `score.OnChange` directly in `OnEnable`/`OnDisable`. No new ScriptableObject required.

---

## New FloatValue Assets

| Asset | Default Value | Meaning |
|-------|--------------|---------|
| `SpawnInterval` | `5` | Runtime spawn interval (seconds); written by DifficultyManager |
| `OrbSpeed` | `3` | Runtime orb speed (world units/sec); written by DifficultyManager |

`SpawnInterval` and `OrbSpeed` are live runtime values. Caps and scale factors live in `Constants.cs` — no separate cap assets.

---

## Constants Added

```csharp
// Orbs / Difficulty
public static float MIN_SPAWN_INTERVAL = 1.5f;
public static float SPAWN_SCALE_FACTOR = 0.5f;
public static float MAX_ORB_SPEED      = 5.5f;
public static float SPEED_SCALE_FACTOR = 0.5f;
// Score
public static int   SCORE_TRESHOLD     = 5;     // note: matches source spelling
```

Threshold every **5 points**. Scale factors **0.5** (steep — each step is meaningful). Max orb speed **5.5**. All tunable by editing `Constants.cs`.

---

## `DifficultyManager.cs`

Score axis handled entirely in `OnScoreChanged`. `Update` only runs the time axis. `OnScoreChanged` uses a `multiplier = newScore / SCORE_TRESHOLD` rather than raw score — so the formula steps in discrete increments rather than accumulating fractions. Guards with `newScore == 0` to skip the reset-propagation from `score.Reset()` (reset is handled explicitly in `ResetState`).

`ResetState` explicitly calls `.Set(base*)` to restore values — does not rely on the score-change side effect.

```csharp
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
```

**Scale reference (baseInterval = 5, min = 1.5, baseSpeed = 3, max = 5.5, threshold = 5, factor = 0.5):**
| Score | Multiplier | Spawn interval | Orb speed |
|-------|-----------|---------------|-----------|
| 0 | 0 | 5.0s | 3.0 |
| 5 | 1 | 4.5s | 3.5 |
| 10 | 2 | 4.0s | 4.0 |
| 15 | 3 | 3.5s | 4.5 |
| 20 | 4 | 3.0s | 5.0 |
| 25 | 5 | 2.5s | 5.5 (capped) |
| ~35 | 7 | 1.5s (capped) | 5.5 |

---

## `OrbManager.cs`

Two field type changes: `float` → `FloatValue` for `spawnInterval` and `orbSpeed`. One behaviour change: `timer` is initialised to `spawnInterval.Value` in both `Start` and `OnGameStart` — first orb spawns immediately on game start instead of waiting the full interval.

```csharp
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue spawnInterval;
    [SerializeField] private FloatValue orbSpeed;

    [Header("Orbs")]
    [SerializeField] private OrbSet orbSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private OrbSpawner orbSpawner;
    private float timer;
    private bool isSpawning;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        isSpawning = true;
        timer = spawnInterval.Value;
    }

    void OnEnable()
    {
        gameOverEvent.OnRaised += OnGameOver;
        gameStartEvent.OnRaised += OnGameStart;
    }

    void OnDisable()
    {
        gameOverEvent.OnRaised -= OnGameOver;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    void Update()
    {
        if (!isSpawning) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval.Value)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed.Value);
        }
    }

    private void OnGameOver() => isSpawning = false;

    private void OnGameStart()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);

        isSpawning = true;
        timer = spawnInterval.Value;
    }
}
```

---

## Inspector Wiring

### New fields on DifficultyManager
| Field | Value |
|-------|-------|
| Score | `Score` asset |
| Spawn Interval | `SpawnInterval` asset |
| Orb Speed | `OrbSpeed` asset |

### OrbManager — replaced float fields
| Field | Was | Now |
|-------|-----|-----|
| Spawn Interval | `2` (float) | `SpawnInterval` asset |
| Orb Speed | `3` (float) | `OrbSpeed` asset |

---

## Reset Flow on Restart

1. `gameStartEvent.Raise()`
2. `GameManager.OnGameStart` → `score.Reset()` → fires `score.OnChange(0)`
3. `DifficultyManager.OnScoreChanged(0)` → `newScore == 0` guard fires → **skipped**
4. `DifficultyManager.ResetState` → sets `spawnInterval` and `orbSpeed` back to base values explicitly; resets shrink state
5. `OrbManager.OnGameStart` → sets `timer = spawnInterval.Value` (now reset to 5s) → first orb spawns immediately
