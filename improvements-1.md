# Implementation Step 1 — FloatValue, RuntimeSet & OrbManager Refactor

## What Was Built

- `FloatValue.cs` — generic reusable float SO (same shape as `IntegerValue`), in `Scripts/SO/`
- `RuntimeSet<T>.cs` — abstract generic SO for live object collections; `OrbSet : RuntimeSet<Orb>` — in `Scripts/SO/`
- `OrbManager.cs` — single MonoBehaviour root for all orb logic; creates and owns `OrbSpawner`
- `OrbSpawner.cs` — converted from MonoBehaviour to plain C# class; no scene presence
- `Orb.cs` — no lifetime, no `gameStartEvent`; self-registers into `OrbSet` via `OnEnable/OnDisable`
- `Constants.cs` — static class; currently `SPAWN_MARGIN = 0.3f`
- `GameManager.cs` — seeds `halfHeightPlayArea` and `halfWidthPlayArea` in `Awake`

### Key Differences From Plan

| Plan | Actual |
|------|--------|
| `FloatVariable` | Named **`FloatValue`** (consistent with `IntegerValue` naming) |
| `PlayAreaHalfWidth.asset` (single SO) | Two assets: **`halfHeightPlayArea`** + **`halfWidthPlayArea`** |
| `OrbManager` seeds FloatValue in `Start` | **`GameManager.Awake`** seeds both FloatValues (single source of truth for camera bounds) |
| `OrbManager` uses `Register`/`Unregister` + `List<Orb>` | **`OrbSet` SO** — orbs self-register via `OnEnable/OnDisable`; no explicit Register calls |
| `Orb` has `[SerializeField] OrbManager orbManager` + `OnDestroy` | Orb has `[SerializeField] OrbSet orbSet`; `OnEnable` → `orbSet.Add(this)`, `OnDisable` → `orbSet.Remove(this)` |
| `OrbSpawner.Tick(...)` returns `Orb` | `OrbSpawner.Spawn(orbSpeed)` called directly; timer lives in `OrbManager.Update` |
| `spawnMargin` serialized on `OrbManager` | Moved to `Constants.SPAWN_MARGIN` |

---

## Final Scripts

### `Scripts/SO/FloatValue.cs`
```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "FloatValue", menuName = "Scriptable Objects/FloatValue")]
public class FloatValue : ScriptableObject
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

### `Scripts/SO/RuntimeSet.cs`
```csharp
using System.Collections.Generic;
using UnityEngine;

public abstract class RuntimeSet<T> : ScriptableObject
{
    public List<T> Items = new List<T>();

    public void Add(T t)
    {
        if (t != null && !Items.Contains(t))
            Items.Add(t);
    }

    public void Remove(T t)
    {
        if (t != null && Items.Contains(t))
            Items.Remove(t);
    }
}
```

### `Scripts/SO/OrbSet.cs`
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "OrbSet", menuName = "Scriptable Objects/OrbSet")]
public class OrbSet : RuntimeSet<Orb> { }
```

### `Scripts/Constants.cs`
```csharp
public static class Constants
{
    public static float SPAWN_MARGIN = 0.3f;
}
```

### `Scripts/OrbSpawner.cs`
Plain C# class. No MonoBehaviour, no scene presence.
```csharp
using UnityEngine;

public class OrbSpawner
{
    private readonly Orb orbPrefab;
    private readonly FloatValue halfHeightPlayArea;
    private readonly FloatValue halfWidthPlayArea;

    public OrbSpawner(Orb orbPrefab, FloatValue halfHeightPlayArea, FloatValue halfWidthPlayArea)
    {
        this.orbPrefab = orbPrefab;
        this.halfHeightPlayArea = halfHeightPlayArea;
        this.halfWidthPlayArea = halfWidthPlayArea;
    }

    public Orb Spawn(float orbSpeed)
    {
        float hw = halfWidthPlayArea.Value;
        float hh = halfHeightPlayArea.Value;

        int edge = Random.Range(0, 4);
        Vector2 spawnPos;
        float angleMin, angleMax;

        switch (edge)
        {
            case 0: // top
                spawnPos = new Vector2(Random.Range(-hw, hw), hh + Constants.SPAWN_MARGIN);
                angleMin = 210f; angleMax = 330f;
                break;
            case 1: // right
                spawnPos = new Vector2(hw + Constants.SPAWN_MARGIN, Random.Range(-hh, hh));
                angleMin = 120f; angleMax = 240f;
                break;
            case 2: // bottom
                spawnPos = new Vector2(Random.Range(-hw, hw), -hh - Constants.SPAWN_MARGIN);
                angleMin = 30f; angleMax = 150f;
                break;
            default: // left
                spawnPos = new Vector2(-hw - Constants.SPAWN_MARGIN, Random.Range(-hh, hh));
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

### `Scripts/OrbManager.cs`
Timer lives here. `OrbSet` handles the live-orb list.
```csharp
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private float spawnInterval = 2f;   // replaced by DifficultyManager later
    [SerializeField] private float orbSpeed = 3f;        // replaced by DifficultyManager later

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
        if (timer >= spawnInterval)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed);
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

### `Scripts/Orb.cs`
Self-registers into `OrbSet`. No `gameStartEvent`, no lifetime.
```csharp
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private OrbSet orbSet;

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

    void OnEnable() => orbSet.Add(this);
    void OnDisable() => orbSet.Remove(this);

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool bounced = false;

        if (pos.x - orbRadius < -halfWidth)      { vel.x =  Mathf.Abs(vel.x); pos.x = -halfWidth + orbRadius; bounced = true; }
        else if (pos.x + orbRadius > halfWidth)   { vel.x = -Mathf.Abs(vel.x); pos.x =  halfWidth - orbRadius; bounced = true; }
        if (pos.y - orbRadius < -halfHeight)      { vel.y =  Mathf.Abs(vel.y); pos.y = -halfHeight + orbRadius; bounced = true; }
        else if (pos.y + orbRadius > halfHeight)  { vel.y = -Mathf.Abs(vel.y); pos.y =  halfHeight - orbRadius; bounced = true; }

        if (bounced) { rb.position = pos; rb.linearVelocity = vel; }
    }

    public void Initialize(bool isWhite, Vector2 direction, float speed)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;
    }
}
```

### `Scripts/GameManager.cs`
Seeds both `FloatValue` assets in `Awake` so all other scripts have valid values from the first frame.
```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Player")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    void Awake()
    {
        float halfHeight = Camera.main.orthographicSize;
        float halfWidth = halfHeight * Camera.main.aspect;
        halfHeightPlayArea.Set(halfHeight);
        halfWidthPlayArea.Set(halfWidth);
    }

    void Start() => Cursor.visible = false;

    void OnEnable()
    {
        health.OnChange += OnHealthChanged;
        gameStartEvent.OnRaised += OnGameStart;
        gameOverEvent.OnRaised += OnGameOver;
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChanged;
        gameStartEvent.OnRaised -= OnGameStart;
        gameOverEvent.OnRaised -= OnGameOver;
    }

    private void OnHealthChanged(int current) { if (current <= 0) gameOverEvent.Raise(); }
    private void OnGameStart() { health.Reset(); score.Reset(); Cursor.visible = false; }
    private void OnGameOver() => Cursor.visible = true;
}
```

---

## Assets to Create

| Asset | Type | Location | Notes |
|-------|------|----------|-------|
| `HalfHeightPlayArea` | FloatValue | SO folder | DefaultValue = 0; GameManager.Awake sets real value |
| `HalfWidthPlayArea` | FloatValue | SO folder | DefaultValue = 0; GameManager.Awake sets real value |
| `OrbSet` | OrbSet | SO folder | DefaultValue list is empty |

---

## Scene Setup

- Delete `OrbSpawner` GameObject from scene
- Add empty GameObject → **OrbManager** → add `OrbManager` component

### OrbManager Inspector
| Field | Value |
|-------|-------|
| Orb Prefab | `Orb` prefab |
| Half Height Play Area | `HalfHeightPlayArea` asset |
| Half Width Play Area | `HalfWidthPlayArea` asset |
| Spawn Interval | `2` |
| Orb Speed | `3` |
| Orb Set | `OrbSet` asset |
| Game Over Event | `GameOverEvent` asset |
| Game Start Event | `GameStartEvent` asset |

### GameManager Inspector
| Field | Value |
|-------|-------|
| Half Height Play Area | `HalfHeightPlayArea` asset |
| Half Width Play Area | `HalfWidthPlayArea` asset |

### Orb Prefab Inspector
| Field | Value |
|-------|-------|
| Orb Set | `OrbSet` asset |

---

## What the Next Step (Difficulty Scaling) Will Change

- Add `DifficultyManager.cs` — reads `halfWidthPlayArea`, writes shrunk value each frame; exposes `CurrentSpawnInterval` / `CurrentOrbSpeed`
- `OrbManager` — replace serialized `spawnInterval`/`orbSpeed` with `[SerializeField] DifficultyManager difficulty` reads
- `Orb.cs` — replace cached `halfWidth` with `[SerializeField] FloatValue halfWidthPlayArea` live read
- `PlayerController.cs` — replace cached `leftBound`/`rightBound` with live `halfWidthPlayArea.Value` clamp
- Wall sprites — two visual-only sprite GameObjects driven by `DifficultyManager`
