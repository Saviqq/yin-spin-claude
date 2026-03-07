# Powerup System — Implementation Plan

## Architecture Overview

### Core idea
A single generic `Powerup` prefab holds a `PowerupEffect` ScriptableObject that encapsulates what happens when the powerup is picked up. `PowerupManager` spawns a random powerup every N seconds. `Powerup` self-registers into a `PowerupSet` RuntimeSet SO — same pattern as `Orb`/`OrbSet` — so `PowerupManager` can clean up on restart without maintaining its own list. Animation runs via Unity's `Animator` component, no code needed.

### New files

| File | Type | Role |
|------|------|------|
| `PowerupEffect.cs` | Abstract SO | Base class for all effects |
| `GainHeartEffect.cs` | Concrete SO | "+1 heart" effect (this step) |
| `PowerupSet.cs` | RuntimeSet SO | Tracks all live powerups; same pattern as OrbSet |
| `Powerup.cs` | MonoBehaviour | Self-registers into PowerupSet; pickup detection |
| `PowerupManager.cs` | MonoBehaviour | Spawns powerups on timer; cleans up on restart |

### Modified files

| File | Change |
|------|--------|
| `Constants.cs` | Add `MAX_HEALTH = 5` |

---

## Architecture Diagram

```
PowerupManager
  ├── timer → Instantiate(powerupPrefab, randomPos)
  ├── calls powerup.SetEffect(randomEffect)
  ├── PowerupSet.Items used for restart cleanup
  └── OnGameStart → destroy all items in PowerupSet

Powerup (prefab)
  ├── SpriteRenderer
  ├── Animator — looping AnimationClip, no code needed
  ├── CircleCollider2D (Is Trigger = true)
  ├── Rigidbody2D (Kinematic)
  ├── OnEnable  → powerupSet.Add(this)
  ├── OnDisable → powerupSet.Remove(this)
  └── OnTriggerEnter2D(Player) → effect.Apply() → Destroy(self)

PowerupEffect (abstract SO)
  └── abstract void Apply()

GainHeartEffect : PowerupEffect
  ├── [SerializeField] IntegerValue health
  └── Apply() → health.Set(Min(health.Value + 1, Constants.MAX_HEALTH))
```

---

## Timed effects — forward design note

Several future effects run for 5 seconds (bigger/slower player, freeze orbs, etc.). These need a coroutine host that outlives the destroyed `Powerup` GameObject. `PowerupManager` will serve as that host — it stays alive for the full game session.

When those effects are implemented, `Apply()` will be extended to `Apply(MonoBehaviour coroutineHost)`. For this step `Apply()` takes no parameters — `GainHeartEffect` only needs its own `[SerializeField]` reference to `health`.

---

## `Constants.cs` addition

```csharp
public static int MAX_HEALTH = 5;
```

---

## `PowerupSet.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "PowerupSet", menuName = "Scriptable Objects/PowerupSet")]
public class PowerupSet : RuntimeSet<Powerup> { }
```

---

## `PowerupEffect.cs`

```csharp
using UnityEngine;

public abstract class PowerupEffect : ScriptableObject
{
    public abstract void Apply();
}
```

---

## `GainHeartEffect.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GainHeartEffect", menuName = "Scriptable Objects/Powerup Effects/GainHeart")]
public class GainHeartEffect : PowerupEffect
{
    [SerializeField] private IntegerValue health;

    public override void Apply()
    {
        health.Set(Mathf.Min(health.Value + 1, Constants.MAX_HEALTH));
    }
}
```

---

## `Powerup.cs`

```csharp
using UnityEngine;

public class Powerup : MonoBehaviour
{
    [SerializeField] private PowerupSet powerupSet;

    private PowerupEffect effect;

    void OnEnable()  => powerupSet.Add(this);
    void OnDisable() => powerupSet.Remove(this);

    public void SetEffect(PowerupEffect powerupEffect)
    {
        effect = powerupEffect;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<Player>() == null) return;

        effect.Apply();
        Destroy(gameObject);
    }
}
```

No animation code — the `Animator` component handles it entirely.

---

## `PowerupManager.cs`

```csharp
using UnityEngine;

public class PowerupManager : MonoBehaviour
{
    [Header("Spawning")]
    [SerializeField] private Powerup powerupPrefab;
    [SerializeField] private PowerupEffect[] effects;
    [SerializeField] private FloatValue powerupSpawnInterval;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Tracking")]
    [SerializeField] private PowerupSet powerupSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private float timer;

    void Start() => timer = 0f;

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= powerupSpawnInterval.Value)
        {
            timer = 0f;
            SpawnPowerup();
        }
    }

    private void SpawnPowerup()
    {
        float hw = halfWidthPlayArea.Value * 0.8f;
        float hh = halfHeightPlayArea.Value * 0.8f;
        Vector2 pos = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));

        Powerup p = Instantiate(powerupPrefab, pos, Quaternion.identity);
        p.SetEffect(effects[Random.Range(0, effects.Length)]);
    }

    private void OnGameStart()
    {
        for (int i = powerupSet.Items.Count - 1; i >= 0; i--)
        {
            if (powerupSet.Items[i] != null)
                Destroy(powerupSet.Items[i].gameObject);
        }
        timer = 0f;
    }
}
```

Cleanup iterates `powerupSet.Items` in reverse (same as `OrbManager.OnGameStart`) — `Destroy` triggers `OnDisable` which calls `powerupSet.Remove(this)`, keeping the list clean automatically.

---

## Powerup Prefab Setup

Create a new GameObject, name it `Powerup`, save as prefab.

### Components

| Component | Settings |
|-----------|----------|
| `SpriteRenderer` | Assign first frame as default sprite |
| `Animator` | Assign the `PowerupAnimator` controller (see below) |
| `CircleCollider2D` | `Is Trigger`: ✓; set radius to match sprite |
| `Rigidbody2D` | `Body Type`: Kinematic; `Gravity Scale`: 0 |
| `Powerup` (script) | Assign `PowerupSet.asset` |

### Animator setup

**Sprite sheet import:**
1. Select the sprite sheet asset → Inspector → `Sprite Mode: Multiple`
2. Open **Sprite Editor** → **Slice** by cell size matching your frame dimensions → Apply
3. Each sliced cell is now an individual `Sprite`

**AnimationClip:**
1. Create → Animation → `PowerupIdle.anim`
2. Open the Animation window, select the clip
3. Set **Sample Rate** to your desired fps (e.g. 8)
4. Add a `Sprite Renderer > Sprite` property curve
5. Drag each sliced frame sprite onto the timeline in order
6. Enable **Loop Time** on the clip in the Inspector

**AnimatorController:**
1. Create → Animator Controller → `PowerupAnimator.controller`
2. Open the Animator window → drag `PowerupIdle` into the graph
3. It becomes the default state (orange) — no transitions needed
4. Assign `PowerupAnimator.controller` to the `Animator` component on the prefab

The Animator runs automatically. No `Animator` reference needed in `Powerup.cs`.

---

## New Assets

| Asset | Type | Value |
|-------|------|-------|
| `PowerupSet.asset` | PowerupSet | — |
| `PowerupSpawnInterval.asset` | FloatValue | DefaultValue: `10` |
| `GainHeartEffect.asset` | GainHeartEffect | Health: `Health.asset` |

---

## Inspector Wiring

### PowerupManager GameObject
| Field | Value |
|-------|-------|
| Powerup Prefab | `Powerup` prefab |
| Effects | `[GainHeartEffect.asset]` |
| Powerup Spawn Interval | `PowerupSpawnInterval.asset` |
| Half Width Play Area | `HalfWidthPlayArea.asset` |
| Half Height Play Area | `HalfHeightPlayArea.asset` |
| Powerup Set | `PowerupSet.asset` |
| Game Start Event | `GameStartEvent.asset` |

### Powerup prefab
| Field | Value |
|-------|-------|
| Powerup Set | `PowerupSet.asset` |

---

## Verify

| Test | Expected |
|------|----------|
| Wait 10s | Powerup appears at random position, sprite animates in loop |
| Wait 20s | Second powerup spawns regardless of first |
| Walk into powerup | Powerup disappears; if health < 5, gain 1 heart |
| Walk into powerup at max health | Powerup disappears; health stays at 5 |
| Restart | All active powerups destroyed, timer resets |
| Pause | Animation freezes (Animator respects timeScale = 0) |

---

## Future Effects — Reference

| Effect | Type | Needs |
|--------|------|-------|
| Clear all orbs | Instant | `OrbSet` |
| +5 score | Instant | `IntegerValue score` |
| Bigger + slower (5s) | Timed | `PlayerMovement` speeds + scale, coroutine host |
| Smaller + faster (5s) | Timed | `PlayerMovement` speeds + scale, coroutine host |
| Balance color split | Instant | `Player.colorRatio` — needs exposure |
| Spawn 3–8 orbs | Instant | `OrbSpawner` reference |
| All orbs one color (5s) | Timed | `OrbSet`, coroutine host |
| Expand walls | Instant | `FloatValue halfWidthPlayArea` |
| Freeze orbs + spawning (5s) | Timed | `OrbSet` velocities, `OrbManager` flag, coroutine host |
