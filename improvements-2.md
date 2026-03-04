# Implementation Step 2 — Time-Based Play Area Shrink

## Scope

This step implements **Axis 1 only**: as time passes, `halfWidthPlayArea` shrinks and two visual wall sprites close in from the screen edges. The score axis (spawn rate / orb speed) is left for the next step.

### What changes
| File | Change |
|------|--------|
| `DifficultyManager.cs` | **New** — owns the shrink logic |
| `Orb.cs` | Replace cached `halfWidth` with live `halfWidthPlayArea.Value` read |
| `PlayerController.cs` | Replace cached `leftBound`/`rightBound` with live `halfWidthPlayArea.Value` read |

### What stays the same
- `OrbManager` — `spawnInterval` and `orbSpeed` remain as serialized fields
- `OrbSpawner` — already reads `halfWidthPlayArea.Value` live, no change

### `GameManager.cs` change
`Awake` now simply calls `OnGameStart()`, which sets both FloatValues from the camera every time it runs — on initial load and on every restart. No separate `Start` method.

```csharp
void Awake() => OnGameStart();

private void OnGameStart()
{
    health.Reset();
    score.Reset();
    halfHeightPlayArea.Set(Camera.main.orthographicSize);
    halfWidthPlayArea.Set(Camera.main.orthographicSize * Camera.main.aspect);
    Cursor.visible = false;
}
```

---

## New Script — `DifficultyManager.cs`

`GameManager.Awake` runs before any `Start`, so `halfWidthPlayArea.Value` is already the real camera-derived value when `DifficultyManager.Start` reads it. `DifficultyManager` stores that as `initialHalfWidth` and never recomputes from the camera — single source of truth stays in `GameManager`.

### Shrink behaviour

The manager cycles between two states:

```
WAITING ──(shrinkInterval seconds pass)──► SHRINKING ──(shrinkDuration seconds pass)──► WAITING ──► ...
```

During **WAITING** the play area is static. During **SHRINKING** `halfWidthPlayArea` moves smoothly from its current value to `current − shrinkStep` using `Mathf.SmoothStep` (ease-in/out). Once the floor (`minHalfWidthFraction`) is reached the manager stays in WAITING indefinitely — no further events fire.

`shrinkInterval` and `shrinkDuration` are `FloatValue` assets so their defaults can be tuned in the SO inspector and, if needed, overwritten at runtime by other systems (e.g., a future score axis that accelerates the interval).

### Assets to create

| Asset | Type | Default Value | Meaning |
|-------|------|---------------|---------|
| `ShrinkInterval` | FloatValue | `10` | Y — seconds between shrink events |
| `ShrinkDuration` | FloatValue | `1` | X — seconds each shrink transition takes |

```csharp
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
    [SerializeField] private FloatValue shrinkInterval;  // Y seconds: wait between shrinks
    [SerializeField] private FloatValue shrinkDuration;  // X seconds: length of each transition

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
```

> **Note:** `ResetState` does not reset `halfWidthPlayArea` — `GameManager.OnGameStart` already resets it to the full camera width before `DifficultyManager.ResetState` runs (both subscribe to the same `gameStartEvent`). `DifficultyManager` just resets its timer and state.

**Tuning reference (interval = 10s, duration = 1s, step = 0.6, initial halfWidth ≈ 9 units, min = 40%):**
| Event # | Time elapsed | halfWidth |
|---------|-------------|-----------|
| 0 | 0s | 9.0 (100%) |
| 1 | 10s | 8.4 |
| 2 | 21s | 7.8 |
| 3 | 32s | 7.2 |
| ... | | |
| ~9 | ~92s | ~3.6 (floor) |

The floor check happens in `Waiting` — once `halfWidthPlayArea.Value <= minHalfWidth` the `BeginShrink` guard never triggers again.

---

## Modified Script — `Orb.cs`

**What changes:** Both `halfWidth` and `halfHeight` are no longer cached — both are read live from FloatValue assets each `FixedUpdate`. Added `[Header("Play Area")]` with both `halfHeightPlayArea` and `halfWidthPlayArea` references. `Awake` no longer touches the camera at all.

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
}
```

---

## Modified Script — `PlayerController.cs`

**What changes:** `leftBound` and `rightBound` removed; `playerRadius` stored as a field; `MoveHorizontal` reads `halfWidthPlayArea.Value` live. `colorRatio` and `collectDelta` are no longer serialized fields — they use `Constants.DEFAULT_COLOR_RATIO` and `Constants.COLLECT_DELTA`. `OnGameStart` resets `colorRatio` directly without a helper method (`UpdateColorRatio` and `GetColorRatio` removed).

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private Material splitMaterial;
    private float playerRadius;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;
    private bool isActive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
        playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
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

    void FixedUpdate()
    {
        if (!isActive) return;

        MoveHorizontal();
        HandleRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);

        if (orb.IsWhite == hitWhiteHalf)
        {
            colorRatio += orb.IsWhite ? Constants.COLLECT_DELTA : -Constants.COLLECT_DELTA;
            colorRatio = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score.Set(score.Value + 1);
        }
        else
        {
            if (health.Value > 0)
                health.Set(health.Value - 1);
        }

        Destroy(other.gameObject);
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f;
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f;

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    private void MoveHorizontal()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        else if (Input.GetKey(KeyCode.D)) input = 1f;

        float bound = halfWidthPlayArea.Value - playerRadius;
        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, -bound, bound);
        rb.MovePosition(newPos);
    }

    private bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;
        float t = localAngle / (2f * Mathf.PI) + 0.5f;
        t = t - Mathf.Floor(t);
        return t < colorRatio;
    }

    private void OnGameOver() => isActive = false;

    private void OnGameStart()
    {
        isActive = true;
        transform.position = Vector2.zero;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}
```

---

## Wall Sprite Setup

Two black rectangle sprites that slide inward. `DifficultyManager` controls their X position — it sets the **inner edge** position. Set pivot to the **inner-facing edge** so `transform.position.x = ±halfWidth` places that edge exactly at the boundary.

### Create in scene
1. **Create → 2D Object → Sprite** → name `WallLeft`
2. Set sprite to a plain white square (Unity's default) → set `Color` to black
3. In Sprite Renderer, set `Order in Layer` high enough to draw over orbs
4. Set pivot to **Right** (for WallLeft) so the right edge is the inner boundary
5. Scale: X large enough to extend off-screen to the left (e.g., `10`), Y tall enough to cover the screen (e.g., `20`)
6. Duplicate → name `WallRight`, set pivot to **Left**, mirror X scale

### Pivot logic
```
WallLeft  pivot = Right  →  transform.position.x = -halfWidth  →  right edge sits at -halfWidth ✓
WallRight pivot = Left   →  transform.position.x = +halfWidth  →  left  edge sits at +halfWidth ✓
```

No colliders on either wall — orb bounce and player clamp are handled in code via `halfWidthPlayArea.Value`.

---

## Scene Setup

1. Create empty GameObject → name **DifficultyManager** → add `DifficultyManager` component
2. Create `WallLeft` and `WallRight` sprites as described above (can be children of DifficultyManager or standalone)

### DifficultyManager Inspector
| Field | Value |
|-------|-------|
| Half Width Play Area | `HalfWidthPlayArea` asset |
| Left Wall | `WallLeft` scene object |
| Right Wall | `WallRight` scene object |
| Shrink Step | `0.6` |
| Min Half Width Fraction | `0.4` |
| Shrink Interval | `ShrinkInterval` asset |
| Shrink Duration | `ShrinkDuration` asset |
| Game Start Event | `GameStartEvent` asset |

### PlayerController Inspector (new field)
| Field | Value |
|-------|-------|
| Half Width Play Area | `HalfWidthPlayArea` asset |

### Orb Prefab Inspector (new fields)
| Field | Value |
|-------|-------|
| Half Height Play Area | `HalfHeightPlayArea` asset |
| Half Width Play Area | `HalfWidthPlayArea` asset |

---

## Execution Order Note

Unity's default script execution order is fine here:

1. `GameManager.Awake` — seeds `halfWidthPlayArea` with real camera value
2. `DifficultyManager.Start` — reads `halfWidthPlayArea.Value` as `initialHalfWidth`
3. `DifficultyManager.Update` (frame 1+) — begins shrinking

`Awake` always runs before `Start` across all objects, so the FloatValue is guaranteed to have the correct initial value when `DifficultyManager.Start` reads it.
