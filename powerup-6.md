# Powerup Step 6 — Expand Play Area + Switch Orb Colors

## Scope

| File | Change |
|------|--------|
| `Scripts/SO/Powerup/ExpandPlayAreaEffect.cs` | New SO — raises `GameEvent` to trigger play area expand |
| `Scripts/SO/Powerup/SwitchOrbColorsEffect.cs` | New SO — raises `GameEvent` to flip all orb colors |
| `Scripts/DifficultyManager.cs` | Add `Expanding` state; subscribe to expand event; `BeginExpand()` |
| `Scripts/Orb.cs` | Add `FlipColor()` public method |
| `Scripts/OrbManager.cs` | Subscribe to switch colors event; `OnSwitchOrbColors()` handler |

---

## Design

### Expand Play Area

`ExpandPlayAreaEffect` raises a plain `GameEvent`. `DifficultyManager` subscribes and calls `BeginExpand()`, which mirrors `BeginShrink()` but in reverse — it targets `current + shrinkStep` clamped to `initialHalfWidth`. The existing SmoothStep animation loop handles it via a third `Expanding` state. No new coroutine, no new flags.

**Edge cases:**
- Picked up during shrink: `BeginExpand()` captures the mid-shrink value and expands from there — no jump.
- Picked up twice: timer resets, `expandFrom/To` recalculated from current value.
- Already at full width: `expandTo` clamps to `initialHalfWidth`, animation still runs (no visible change).
- Restart: `ResetState()` sets state → `Waiting` and calls `UpdateWalls(initialHalfWidth)` — walls snap to full width immediately.

### Switch Orb Colors

Pure fire-and-forget. `SwitchOrbColorsEffect` raises a plain `GameEvent`. `OrbManager` subscribes and calls `orb.FlipColor()` on every orb currently in `orbSet`. No coroutine, no timer. New orbs spawned after pickup use normal random colors.

**`Orb.FlipColor()`** — toggles `IsWhite` and updates the `SpriteRenderer` color. Keeps color state encapsulated in `Orb`, same as `Initialize()`.

**Edge cases:**
- Picked up twice: orbs flip back to original colors.
- Orbs spawned after flip: random color as normal.
- Restart: all orbs destroyed; new orbs spawn normally.

---

## `ExpandPlayAreaEffect.cs` *(new — `Scripts/SO/Powerup/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayAreaEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayArea")]
public class ExpandPlayAreaEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayAreaEvent;

    public override void Apply() => expandPlayAreaEvent.Raise();
}
```

---

## `SwitchOrbColorsEffect.cs` *(new — `Scripts/SO/Powerup/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SwitchOrbColorsEffect", menuName = "Scriptable Objects/Powerup Effects/SwitchOrbColors")]
public class SwitchOrbColorsEffect : PowerupEffect
{
    [SerializeField] private GameEvent switchOrbColorsEvent;

    public override void Apply() => switchOrbColorsEvent.Raise();
}
```

---

## `DifficultyManager.cs` (updated)

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
    [SerializeField] private GameEvent expandPlayAreaEvent;

    private float initialHalfWidth;
    private float minHalfWidth;
    private float baseSpawnInterval;
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
        baseSpawnInterval = spawnInterval.Value;
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
        spawnInterval.Set(baseSpawnInterval);
        orbSpeed.Set(baseOrbSpeed);

    }
}
```

---

## `Orb.cs` (updated — add FlipColor)

```csharp
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private OrbSet orbSet;

    [Header("Play Area")]
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;

    public bool IsWhite { get; private set; }

    private Rigidbody2D rb;
    private float orbRadius;
    private Vector2 storedVelocity;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        orbRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
    }

    void OnEnable() => orbSet.Add(this);

    void OnDisable() => orbSet.Remove(this);

    void FixedUpdate()
    {
        float halfHeight = halfHeightPlayArea.Value;
        float halfWidth = halfWidthPlayArea.Value;

        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool bounced = false;

        if (pos.x - orbRadius < -halfWidth)
        {
            vel.x = Mathf.Abs(vel.x);
            pos.x = -halfWidth + orbRadius;
            bounced = true;
        }
        else if (pos.x + orbRadius > halfWidth)
        {
            vel.x = -Mathf.Abs(vel.x);
            pos.x = halfWidth - orbRadius;
            bounced = true;
        }

        if (pos.y - orbRadius < -halfHeight)
        {
            vel.y = Mathf.Abs(vel.y);
            pos.y = -halfHeight + orbRadius;
            bounced = true;
        }
        else if (pos.y + orbRadius > halfHeight)
        {
            vel.y = -Mathf.Abs(vel.y);
            pos.y = halfHeight - orbRadius;
            bounced = true;
        }

        if (bounced)
        {
            rb.position = pos;
            rb.linearVelocity = vel;
        }
    }

    public void Initialize(bool isWhite, Vector2 direction, float speed)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;
    }

    public void FlipColor()
    {
        IsWhite = !IsWhite;
        GetComponent<SpriteRenderer>().color = IsWhite ? Color.white : Color.black;
    }

    public void Stop()
    {
        storedVelocity = rb.linearVelocity;
        rb.linearVelocity = Vector2.zero;
    }

    public void Resume()
    {
        rb.linearVelocity = storedVelocity;
    }
}
```

---

## `OrbManager.cs` (updated)

```csharp
using System.Collections;
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
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private IntegerEvent spawnOrbEvent;
    [SerializeField] private GameEvent freezeOrbsEvent;
    [SerializeField] private GameEvent switchOrbColorsEvent;

    [Header("Freeze")]
    [SerializeField] private float freezeDuration = 5f;

    private OrbSpawner orbSpawner;
    private float timer;
    private bool isFrozen;
    private Coroutine activeFreezeCoroutine;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        timer = spawnInterval.Value;
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
        spawnOrbEvent.OnRaised += OnSpawnBurst;
        freezeOrbsEvent.OnRaised += OnFreezeOrbs;
        switchOrbColorsEvent.OnRaised += OnSwitchOrbColors;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        spawnOrbEvent.OnRaised -= OnSpawnBurst;
        freezeOrbsEvent.OnRaised -= OnFreezeOrbs;
        switchOrbColorsEvent.OnRaised -= OnSwitchOrbColors;
    }

    void Update()
    {
        if (isFrozen) return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval.Value)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed.Value);
        }
    }

    private void OnSpawnBurst(int count)
    {
        for (int i = 0; i < count; i++)
            orbSpawner.Spawn(orbSpeed.Value);
    }

    private void OnFreezeOrbs()
    {
        if (activeFreezeCoroutine != null)
            StopCoroutine(activeFreezeCoroutine);

        activeFreezeCoroutine = StartCoroutine(FreezeCoroutine());
    }

    private IEnumerator FreezeCoroutine()
    {
        isFrozen = true;
        for (int i = 0; i < orbSet.Items.Count; i++)
            orbSet.Items[i].Stop();

        yield return new WaitForSeconds(freezeDuration);

        for (int i = 0; i < orbSet.Items.Count; i++)
            orbSet.Items[i].Resume();

        isFrozen = false;
        activeFreezeCoroutine = null;
    }

    private void OnSwitchOrbColors()
    {
        for (int i = 0; i < orbSet.Items.Count; i++)
            orbSet.Items[i].FlipColor();
    }

    private void OnGameStart()
    {
        if (activeFreezeCoroutine != null)
        {
            StopCoroutine(activeFreezeCoroutine);
            activeFreezeCoroutine = null;
        }

        isFrozen = false;

        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);

        timer = spawnInterval.Value;
    }
}
```

---

## New Assets

| Asset | Type | Wiring |
|-------|------|--------|
| `ExpandPlayAreaEvent.asset` | GameEvent | — |
| `SwitchOrbColorsEvent.asset` | GameEvent | — |
| `ExpandPlayAreaEffect.asset` | ExpandPlayAreaEffect | Expand Play Area Event: `ExpandPlayAreaEvent.asset` |
| `SwitchOrbColorsEffect.asset` | SwitchOrbColorsEffect | Switch Orb Colors Event: `SwitchOrbColorsEvent.asset` |

---

## Inspector Wiring

### DifficultyManager
| Field | Value |
|-------|-------|
| Expand Play Area Event | `ExpandPlayAreaEvent.asset` |

### OrbManager
| Field | Value |
|-------|-------|
| Switch Orb Colors Event | `SwitchOrbColorsEvent.asset` |

### PowerupManager — Effects array
Add `ExpandPlayAreaEffect.asset` and `SwitchOrbColorsEffect.asset`.

---

## Verify

| Test | Expected |
|------|----------|
| Pick up expand walls | Walls animate outward over `shrinkDuration` seconds, clamped to full width |
| Pick up expand while shrinking | Expands from mid-shrink position — no jump |
| Pick up expand twice | Timer resets, expands again from current position |
| After expand animation | DifficultyManager returns to Waiting, normal shrink resumes |
| Restart during expand | Walls snap to full width immediately |
| Pick up switch orb colors | All current orbs flip color instantly |
| Pick up switch twice | Orbs flip back to original colors |
| Orbs spawned after flip | Appear with normal random colors |
| Restart after flip | All orbs destroyed, new orbs spawn normally |
