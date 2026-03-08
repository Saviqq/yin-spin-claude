# Powerup Step 5 — Spawn Burst + Freeze Orbs

## Scope

| File | Change |
|------|--------|
| `Scripts/SO/GameEvent.cs` | Append `GameEvent<T>` abstract generic SO base below existing class |
| `Scripts/SO/IntegerEvent.cs` | New: `IntegerEvent : GameEvent<int>` concrete SO |
| `Scripts/Orb.cs` | Add `Stop()` / `Resume()` public methods |
| `Scripts/OrbManager.cs` | Subscribe to spawn burst + freeze events; freeze coroutine; pause spawning during freeze |
| `Scripts/SO/Powerup/SpawnBurstEffect.cs` | New SO — raises `IntegerEvent` with random orb count |
| `Scripts/SO/Powerup/FreezeOrbsEffect.cs` | New SO — raises `GameEvent` to trigger freeze |

---

## Design

### Generic `GameEvent<T>`

Unity 6 supports generic `ScriptableObject` inheritance. `[CreateAssetMenu]` cannot be placed on a generic class, so only concrete subclasses carry it. The pattern is identical to the existing `GameEvent` but typed:

```
GameEvent<T>   (abstract, no CreateAssetMenu)
  └── IntegerEvent : GameEvent<int>   (concrete, has CreateAssetMenu)
```

### Spawn Burst

`SpawnBurstEffect` raises an `IntegerEvent` with a random count in `[minOrbs, maxOrbs]`. `OrbManager` subscribes and calls `orbSpawner.Spawn()` N times immediately using the current `orbSpeed.Value` — identical to the regular spawn path.

### Freeze Orbs

`FreezeOrbsEffect` raises a plain `GameEvent` (no data needed — freeze duration lives on `OrbManager`). `OrbManager` subscribes and runs a coroutine:

1. Call `orb.Stop()` on every orb currently in `orbSet`
2. Set `isFrozen = true` — suppresses the spawn timer in `Update`
3. `WaitForSeconds(freezeDuration)`
4. Call `orb.Resume()` on every orb still in `orbSet`
5. Set `isFrozen = false` — spawning resumes

**`Orb.Stop()` / `Orb.Resume()`** — Orb stores its own velocity internally. This keeps velocity state encapsulated in Orb and keeps OrbManager free of per-orb dictionaries.

**Edge cases:**
- Second freeze during active freeze: `StopCoroutine` + restart, same as PlayerMovement scale — orbs are re-stopped, timer resets to full duration.
- Orbs destroyed mid-freeze (ClearOrbs powerup): `Resume()` is never called on them — safe, they're gone.
- Restart during freeze: `OnGameStart` stops the coroutine and destroys all orbs; `isFrozen` reset to `false`.
- Pause: `WaitForSeconds` freezes with `Time.timeScale = 0` — timer pauses correctly.

---

## `Scripts/SO/GameEvent.cs` (updated — add generic base below existing class)

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Scriptable Objects/GameEvent")]
public class GameEvent : ScriptableObject
{
    public event Action OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}

public abstract class GameEvent<T> : ScriptableObject
{
    public event Action<T> OnRaised;

    public void Raise(T value)
    {
        OnRaised?.Invoke(value);
    }
}
```

---

## `IntegerEvent.cs` *(new — `Scripts/SO/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "IntegerEvent", menuName = "Scriptable Objects/IntegerEvent")]
public class IntegerEvent : GameEvent<int> { }
```

---

## `SpawnBurstEffect.cs` *(new — `Scripts/SO/Powerup/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SpawnBurstEffect", menuName = "Scriptable Objects/Powerup Effects/SpawnBurst")]
public class SpawnBurstEffect : PowerupEffect
{
    [SerializeField] private IntegerEvent spawnOrbEvent;
    [SerializeField] private int minOrbs = 3;
    [SerializeField] private int maxOrbs = 6;

    public override void Apply()
    {
        spawnOrbEvent.Raise(Random.Range(minOrbs, maxOrbs + 1));
    }
}
```

---

## `FreezeOrbsEffect.cs` *(new — `Scripts/SO/Powerup/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "FreezeOrbsEffect", menuName = "Scriptable Objects/Powerup Effects/FreezeOrbs")]
public class FreezeOrbsEffect : PowerupEffect
{
    [SerializeField] private GameEvent freezeOrbsEvent;

    public override void Apply() => freezeOrbsEvent.Raise();
}
```

---

## `Orb.cs` (updated — add Stop/Resume)

Add two public methods. `storedVelocity` is set in `Stop()` and consumed in `Resume()`.

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

    [Header("Freeze")]
    [SerializeField] private float freezeDuration = 5f;

    [Header("Orbs")]
    [SerializeField] private OrbSet orbSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private IntegerEvent spawnOrbEvent;
    [SerializeField] private GameEvent freezeOrbsEvent;

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
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        spawnOrbEvent.OnRaised -= OnSpawnBurst;
        freezeOrbsEvent.OnRaised -= OnFreezeOrbs;
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
| `SpawnOrbEvent.asset` | IntegerEvent | — |
| `FreezeOrbsEvent.asset` | GameEvent | — |
| `SpawnBurstEffect.asset` | SpawnBurstEffect | Spawn Orb Event: `SpawnOrbEvent.asset`, Min Orbs: `3`, Max Orbs: `6` |
| `FreezeOrbsEffect.asset` | FreezeOrbsEffect | Freeze Orbs Event: `FreezeOrbsEvent.asset` |

---

## Inspector Wiring

### OrbManager
| Field | Value |
|-------|-------|
| Freeze Duration | `5` |
| Spawn Orb Event | `SpawnOrbEvent.asset` |
| Freeze Orbs Event | `FreezeOrbsEvent.asset` |

### PowerupManager — Effects array
Add `SpawnBurstEffect.asset` and `FreezeOrbsEffect.asset`.

---

## Verify

| Test | Expected |
|------|----------|
| Pick up spawn burst | 3–6 new orbs appear immediately across the play area |
| Pick up freeze | All orbs stop moving; no new orbs spawn |
| After 5s | All orbs resume at their original velocities; spawning resumes |
| Pick up freeze twice | First freeze cancelled, timer resets, all orbs re-stopped |
| ClearOrbs during freeze | Orbs destroyed — no crash; freeze timer continues; spawning resumes after 5s |
| Restart during freeze | All orbs destroyed, spawning immediately resumes |
| Pause during freeze | 5s timer frozen; resumes on unpause |
