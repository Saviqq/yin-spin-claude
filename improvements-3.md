# Implementation Step 3 — Score-Based Difficulty Scaling

## Scope

This step implements **Axis 2**: as score increases, spawn interval shortens and orb speed increases. Values update only when the score changes — not every frame.

### What changes
| File | Change |
|------|--------|
| `DifficultyManager.cs` | Subscribe to `score.OnChange`; update axis values in handler |
| `OrbManager.cs` | `spawnInterval` and `orbSpeed` fields: `float` → `FloatValue` |

### What stays the same
- Everything else — `Orb`, `PlayerController`, `GameManager`, `OrbSpawner`, walls

---

## No New Event SO Needed

`IntegerValue` already has `event Action<int> OnChange` which fires with the new value every time `score.Set(...)` is called — the same pattern `GameManager` uses to watch `health`. `DifficultyManager` subscribes to `score.OnChange` directly in `OnEnable`/`OnDisable`. No new ScriptableObject required.

**Bonus:** when `GameManager.OnGameStart` calls `score.Reset()`, that fires `score.OnChange(0)`, which triggers `OnScoreChanged(0)` in `DifficultyManager` — which sets `spawnInterval` and `orbSpeed` back to their base values automatically. `ResetState` doesn't need to touch them at all.

---

## New FloatValue Assets

| Asset | Default Value | Meaning |
|-------|--------------|---------|
| `SpawnInterval` | `5` | Runtime spawn interval (seconds); written by DifficultyManager |
| `MinSpawnInterval` | `1.5` | Spawn interval floor |
| `OrbSpeed` | `3` | Runtime orb speed (world units/sec); written by DifficultyManager |
| `MaxOrbSpeed` | `7` | Orb speed ceiling |

`SpawnInterval` and `OrbSpeed` are live runtime values. `MinSpawnInterval` and `MaxOrbSpeed` are read-only caps.

---

## Score Threshold vs Continuous Scaling

Two approaches are possible. Both use `score.OnChange` — the only difference is whether the handler acts on every score point or only at multiples of a threshold.

### Continuous (every point)
```csharp
private void OnScoreChanged(int newScore)
{
    spawnInterval.Set(Mathf.Max(minSpawnInterval.Value, baseSpawnInterval - newScore * spawnScaleFactor));
    orbSpeed.Set(Mathf.Min(maxOrbSpeed.Value, baseOrbSpeed + newScore * speedScaleFactor));
}
```
Difficulty curve is perfectly smooth. Changes are imperceptible on any individual score point. Player cannot feel individual jumps.

### Threshold (every N points)
```csharp
private void OnScoreChanged(int newScore)
{
    if (newScore % scoreThreshold != 0) return;

    spawnInterval.Set(Mathf.Max(minSpawnInterval.Value, baseSpawnInterval - newScore * spawnScaleFactor));
    orbSpeed.Set(Mathf.Min(maxOrbSpeed.Value, baseOrbSpeed + newScore * speedScaleFactor));
}
```
Difficulty holds steady for a stretch, then visibly steps up. Phase boundaries are legible to the player. Pairs naturally with visual/audio feedback at each step. More arcade-like.

**`scoreThreshold = 10` produces the same numbers at each multiple of 10 as the continuous version.** The end-game difficulty is identical; only the pacing differs.

Score `0` is always divisible by any threshold, so the reset case (`OnScoreChanged(0)`) works correctly in both approaches.

**Recommendation: threshold.** For a game jam arcade game, legible phase boundaries feel better than imperceptible micro-changes.

---

## Updated Script — `DifficultyManager.cs`

Score axis handled entirely in `OnScoreChanged`. `Update` only runs the time axis. `ResetState` only resets the shrink state machine — the score reset from `GameManager` propagates automatically via `score.OnChange(0)`.

The script below uses the threshold approach. To switch to continuous, remove the `if (newScore % scoreThreshold != 0) return;` line.

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
    [SerializeField] private FloatValue minSpawnInterval;
    [SerializeField] private float spawnScaleFactor = 0.05f;
    [SerializeField] private FloatValue orbSpeed;
    [SerializeField] private FloatValue maxOrbSpeed;
    [SerializeField] private float speedScaleFactor = 0.08f;
    [SerializeField] private int scoreThreshold = 10;

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
        if (newScore % scoreThreshold != 0) return;

        spawnInterval.Set(Mathf.Max(minSpawnInterval.Value, baseSpawnInterval - newScore * spawnScaleFactor));
        orbSpeed.Set(Mathf.Min(maxOrbSpeed.Value, baseOrbSpeed + newScore * speedScaleFactor));
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
```

**Scale reference (baseInterval = 5, min = 1.5, baseSpeed = 3, max = 7, threshold = 10):**
| Score | Spawn interval | Orb speed |
|-------|---------------|-----------|
| 0 | 5.0s | 3.0 |
| 10 | 4.5s | 3.8 |
| 20 | 4.0s | 4.6 |
| 30 | 3.5s | 5.4 |
| 50 | 2.5s | 7.0 (capped) |
| 70 | 1.5s (capped) | 7.0 |

---

## Updated Script — `OrbManager.cs`

Two field type changes only: `float spawnInterval` → `FloatValue`, `float orbSpeed` → `FloatValue`. Both read sites in `Update` get `.Value`.

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
        timer = 0f;
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
| Min Spawn Interval | `MinSpawnInterval` asset |
| Spawn Scale Factor | `0.05` |
| Orb Speed | `OrbSpeed` asset |
| Max Orb Speed | `MaxOrbSpeed` asset |
| Speed Scale Factor | `0.08` |
| Score Threshold | `10` |

### OrbManager — replace float fields with FloatValue assets
| Field | Was | Now |
|-------|-----|-----|
| Spawn Interval | `2` (float) | `SpawnInterval` asset |
| Orb Speed | `3` (float) | `OrbSpeed` asset |

---

## Reset Flow on Restart

1. `gameStartEvent.Raise()`
2. `GameManager.OnGameStart` → `score.Reset()` → fires `score.OnChange(0)`
3. `DifficultyManager.OnScoreChanged(0)` → `0 % 10 == 0` → resets `spawnInterval` and `orbSpeed` to base values
4. `DifficultyManager.ResetState` → resets shrink timer and state

`spawnInterval` and `orbSpeed` are reset as a side effect of the score reset — no explicit reset needed in `ResetState`.
