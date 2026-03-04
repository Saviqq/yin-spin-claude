# Implementation Step 1 — FloatVariable & OrbManager Refactor

## What This Step Does

- Adds `FloatVariable.cs` — generic reusable SO (same pattern as `IntegerValue`)
- Converts `OrbSpawner` from MonoBehaviour to a plain C# class owned by `OrbManager`
- Creates `OrbManager.cs` — single MonoBehaviour root for all orb logic
- Updates `Orb.cs` — removes `gameStartEvent` self-cleanup (OrbManager handles it), removes lifetime timer, adds `speed` param to `Initialize`

After this step `OrbSpawner` is no longer in the scene. `OrbManager` is the only orb-related GameObject. `DifficultyManager` (next step) will drive spawn interval, orb speed, and play area width by writing into `FloatVariable` assets that both `OrbManager` and other scripts already hold references to.

---

## New Files

### `FloatVariable.cs`

Mirrors `IntegerValue` exactly. Reusable for any runtime float that needs to broadcast changes.

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatVariable", menuName = "Scriptable Objects/FloatVariable")]
public class FloatVariable : ScriptableObject
{
    [SerializeField] private float DefaultValue;

    public float Value { get; private set; }

    public event Action<float> OnChange;

    void OnEnable() => Value = DefaultValue;

    public void Set(float value)
    {
        Value = value;
        OnChange?.Invoke(Value);
    }

    public void Reset()
    {
        Value = DefaultValue;
        OnChange?.Invoke(Value);
    }
}
```

**Create asset:** Right-click in Project → Create → Scriptable Objects → FloatVariable → name it `PlayAreaHalfWidth`. Leave `DefaultValue` at 0 — `OrbManager.Start` writes the real camera-derived value on play.

---

### `OrbSpawner.cs`

Plain C# class. No MonoBehaviour. No `[SerializeField]`. No scene presence. `OrbManager` creates it via `new OrbSpawner(...)` in `Start` and calls `Tick` each frame from `Update`.

Reads `playAreaHalfWidth.Value` at spawn time so it's already wired for the difficulty scaling step — when `DifficultyManager` starts shrinking the boundary the spawner automatically picks positions within the current play area.

```csharp
using UnityEngine;

public class OrbSpawner
{
    private readonly Orb orbPrefab;
    private readonly FloatVariable playAreaHalfWidth;
    private readonly float halfHeight;
    private readonly float spawnMargin;

    private float timer;

    public OrbSpawner(Orb orbPrefab, FloatVariable playAreaHalfWidth, float halfHeight, float spawnMargin)
    {
        this.orbPrefab = orbPrefab;
        this.playAreaHalfWidth = playAreaHalfWidth;
        this.halfHeight = halfHeight;
        this.spawnMargin = spawnMargin;
    }

    public void Reset() => timer = 0f;

    // Called every frame by OrbManager.Update.
    // Returns the spawned Orb when the interval elapses, null otherwise.
    public Orb Tick(float deltaTime, float spawnInterval, float orbSpeed)
    {
        timer += deltaTime;
        if (timer < spawnInterval) return null;

        timer = 0f;
        return Spawn(orbSpeed);
    }

    private Orb Spawn(float orbSpeed)
    {
        float hw = playAreaHalfWidth.Value;

        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        float angleMin, angleMax;

        switch (edge)
        {
            case 0: // top
                spawnPos = new Vector2(Random.Range(-hw, hw), halfHeight + spawnMargin);
                angleMin = 210f; angleMax = 330f;
                break;
            case 1: // right
                spawnPos = new Vector2(hw + spawnMargin, Random.Range(-halfHeight, halfHeight));
                angleMin = 120f; angleMax = 240f;
                break;
            case 2: // bottom
                spawnPos = new Vector2(Random.Range(-hw, hw), -halfHeight - spawnMargin);
                angleMin = 30f; angleMax = 150f;
                break;
            default: // left
                spawnPos = new Vector2(-hw - spawnMargin, Random.Range(-halfHeight, halfHeight));
                angleMin = -60f; angleMax = 60f;
                break;
        }

        float angleDeg = Random.Range(angleMin, angleMax);
        float angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        bool isWhite = Random.value > 0.5f;
        Orb orb = Object.Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.Initialize(isWhite, direction, orbSpeed);
        return orb;
    }
}
```

---

### `OrbManager.cs`

The single MonoBehaviour for all orb logic. Creates and ticks `OrbSpawner`. Tracks live orbs. Destroys them all on `GameStartEvent`. Stops spawning on `GameOverEvent`.

`spawnInterval` and `orbSpeed` are serialized for now. In the difficulty scaling step they'll be replaced with reads from `DifficultyManager`.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatVariable playAreaHalfWidth;
    [SerializeField] private float spawnMargin = 0.3f;
    [SerializeField] private float spawnInterval = 2f;   // replaced by DifficultyManager later
    [SerializeField] private float orbSpeed = 3f;        // replaced by DifficultyManager later

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private OrbSpawner spawner;
    private readonly List<Orb> activeOrbs = new();
    private bool isSpawning;

    void Start()
    {
        float halfHeight = Camera.main.orthographicSize;
        float halfWidth = halfHeight * Camera.main.aspect;

        // Seed the FloatVariable with the real camera value.
        // DifficultyManager will overwrite this each frame once it exists.
        playAreaHalfWidth.Set(halfWidth);

        spawner = new OrbSpawner(orbPrefab, playAreaHalfWidth, halfHeight, spawnMargin);
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

        Orb orb = spawner.Tick(Time.deltaTime, spawnInterval, orbSpeed);
        if (orb != null) Register(orb);
    }

    public void Register(Orb orb) => activeOrbs.Add(orb);

    public void Unregister(Orb orb) => activeOrbs.Remove(orb);

    private void OnGameOver() => isSpawning = false;

    private void OnGameStart()
    {
        foreach (Orb orb in activeOrbs.ToArray())
            if (orb != null) Destroy(orb.gameObject);

        activeOrbs.Clear();
        spawner.Reset();
        isSpawning = true;
    }
}
```

---

## Modified Files

### `Orb.cs` — full replacement

Changes from current version:
- `gameStartEvent` field + `OnEnable`/`OnDisable` subscriptions + `OnGameStart()` removed — `OrbManager.OnGameStart` destroys orbs now
- `private float speed` serialized field removed — speed comes in via `Initialize`
- `minLifetime`, `maxLifetime` fields removed; `Destroy(gameObject, ...)` call in `Initialize` removed
- `Initialize` signature: `(bool isWhite, Vector2 direction)` → `(bool isWhite, Vector2 direction, float speed)`
- `[SerializeField] OrbManager orbManager` added; `OnDestroy` calls `Unregister`
- `halfWidth` stays cached from `Awake` — `FloatVariable` live-read is the difficulty scaling step

```csharp
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private OrbManager orbManager;

    public bool IsWhite { get; private set; }

    private Rigidbody2D rb;
    private float orbRadius;
    private float halfWidth;
    private float halfHeight;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        orbRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
        halfHeight = Camera.main.orthographicSize;
        halfWidth = halfHeight * Camera.main.aspect;
    }

    void OnDestroy()
    {
        orbManager?.Unregister(this);
    }

    void FixedUpdate()
    {
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

## Scene Setup

### Remove
- **OrbSpawner GameObject** — delete it from the scene entirely

### Add
- Create empty GameObject → name it **OrbManager**
- Add the `OrbManager` component

### Inspector wiring — OrbManager
| Field | Value |
|-------|-------|
| Orb Prefab | `Orb` prefab |
| Play Area Half Width | `PlayAreaHalfWidth` asset |
| Spawn Margin | `0.3` |
| Spawn Interval | `2` |
| Orb Speed | `3` |
| Game Over Event | `GameOverEvent` asset |
| Game Start Event | `GameStartEvent` asset |

### Inspector wiring — Orb prefab
| Field | Value |
|-------|-------|
| Orb Manager | `OrbManager` scene object |

> Assign the scene reference on the prefab by dragging the scene `OrbManager` into the prefab field. Unity allows scene object references on prefabs as long as the prefab is only ever instantiated in that scene.

---

## What Stays Unchanged

- `FloatVariable` asset `DefaultValue = 0` is fine — `OrbManager.Start` writes the real value before any spawn occurs
- `PlayerController.cs` — no changes in this step
- `GameManager.cs` — no changes in this step
- All other scripts — no changes

---

## What the Next Step (Difficulty Scaling) Will Add

- `DifficultyManager.cs` — writes to `PlayAreaHalfWidth` asset each frame (shrink over time); exposes `CurrentSpawnInterval` and `CurrentOrbSpeed`
- `OrbManager` — replace the two serialized floats with reads from `DifficultyManager`
- `Orb.cs` — replace cached `halfWidth` with `playAreaHalfWidth.Value` read each `FixedUpdate` (asset already referenced via `OrbManager` scope — or add a direct `[SerializeField] FloatVariable` to Orb)
- Wall sprites — visual representation of shrinking boundary
