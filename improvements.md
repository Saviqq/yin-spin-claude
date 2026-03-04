# Game Improvements — Yin & Spin

---

## 1. Difficulty Scaling

Core loop uses two independent axes that compound naturally:

| Axis | Driver | Effect |
|------|--------|--------|
| **Time** (floor) | Elapsed play time | Play area width shrinks |
| **Score** (accelerant) | Score milestones | Spawn interval ↓, orb speed ↑ |

Time = spatial pressure (less room to dodge). Score = temporal pressure (more orbs, faster orbs). They compound without feeling redundant.

---

### Axis 1 — Play Area Width Shrinks Over Time

A `playAreaHalfWidth` value shrinks from full width to a minimum (~40% of original) over ~90 seconds. Two black rectangular sprite GameObjects (no colliders) move inward from screen edges to mark the current boundary. Visuals and physics stay locked together because both read the same `FloatVariable` SO.

**Key numbers (all tunable):**
```
initialHalfWidth  = computed from camera on Start
minHalfWidth      = initialHalfWidth * 0.4f   (~3.6 units on 16:9)
shrinkRate        = 0.03f world-units/second
```

**Why not Camera.rect tween?**
`Camera.rect` only affects the rendered image in screen space. It does not change `orthographicSize * aspect`, so world-space bounds stay the same. Player clamp, orb bounce, and spawn positions would all remain at the original coordinates — player can walk behind the visual wall, orbs bounce off invisible boundaries. Visuals and physics decouple.

**Why not BoxCollider2D walls?**
Orbs use `Is Trigger = true`. Triggers ignore physics collisions entirely — the bounce in `Orb.FixedUpdate()` is manual math, not physics resolution. Static collider walls have zero effect on triggers. Making orbs non-trigger breaks `PlayerController.OnTriggerEnter2D` and requires a full collision rewrite.

**Conclusion:** Wall GameObjects are visual-only sprites. `DifficultyManager` drives both their scale and `PlayAreaHalfWidth.asset` from the same computed value each frame. Orbs, spawner, and player all read `.Value` at runtime — no stale cached bounds.

**Orb boundary handling during shrink:**
Current scripts cache `halfWidth` once in `Awake()`/`Start()`. When bounds shrink at runtime, orbs would continue bouncing off the old invisible boundary. Fix: replace the cached field with a live read of `PlayAreaHalfWidth.Value` each `FixedUpdate`. When the boundary shrinks past an orb's position, the bounce condition triggers on the next physics step and reflects the orb inward — no special handling needed. Shrink rate (~0.03 units/sec) is far smaller than orb travel per frame (~0.14 units at max speed), so orbs can never overshoot.

#### New FloatVariable ScriptableObject

Mirrors the existing `IntegerValue` pattern exactly:

```csharp
// FloatVariable.cs
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

Create asset: `PlayAreaHalfWidth.asset` — `DefaultValue` left at 0 (DifficultyManager writes the real value on Start).

#### DifficultyManager

```csharp
// DifficultyManager.cs
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] private FloatVariable playAreaHalfWidth;
    [SerializeField] private Transform leftWall;   // visual wall sprite
    [SerializeField] private Transform rightWall;  // visual wall sprite
    [SerializeField] private float shrinkRate = 0.03f;
    [SerializeField] private float minHalfWidthFraction = 0.4f;

    [Header("Spawn / Speed")]
    [SerializeField] private float baseInterval = 2f;
    [SerializeField] private float minInterval = 0.4f;
    [SerializeField] private float spawnScaleFactor = 0.04f;
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float speedScaleFactor = 0.08f;

    [Header("Events")]
    [SerializeField] private IntegerValue score;
    [SerializeField] private GameEvent gameStartEvent;

    private float initialHalfWidth;
    private float minHalfWidth;
    private float elapsedTime;

    public float CurrentSpawnInterval { get; private set; }
    public float CurrentOrbSpeed { get; private set; }

    void Start()
    {
        initialHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        minHalfWidth = initialHalfWidth * minHalfWidthFraction;
        Reset();
    }

    void OnEnable() => gameStartEvent.OnRaised += Reset;
    void OnDisable() => gameStartEvent.OnRaised -= Reset;

    void Update()
    {
        elapsedTime += Time.deltaTime;

        // Axis 1: time → boundary shrink
        float newHalfWidth = Mathf.Max(minHalfWidth, initialHalfWidth - shrinkRate * elapsedTime);
        playAreaHalfWidth.Set(newHalfWidth);
        UpdateWalls(newHalfWidth);

        // Axis 2: score → spawn interval + orb speed
        CurrentSpawnInterval = Mathf.Max(minInterval, baseInterval - score.Value * spawnScaleFactor);
        CurrentOrbSpeed = Mathf.Min(maxSpeed, baseSpeed + score.Value * speedScaleFactor);
    }

    private void UpdateWalls(float halfWidth)
    {
        // Walls are sprites anchored at screen edges; scale their X to cover the gap.
        // Adjust these transforms to match your actual wall sprite setup.
        if (leftWall != null)  leftWall.position  = new Vector3(-halfWidth, 0f, 0f);
        if (rightWall != null) rightWall.position = new Vector3( halfWidth, 0f, 0f);
    }

    private void Reset()
    {
        elapsedTime = 0f;
        playAreaHalfWidth.Set(initialHalfWidth > 0 ? initialHalfWidth : Camera.main.orthographicSize * Camera.main.aspect);
        CurrentSpawnInterval = baseInterval;
        CurrentOrbSpeed = baseSpeed;
    }
}
```

> **Note on wall sprite setup:**
> Create two black rectangular sprites (`WallLeft`, `WallRight`) as children of an empty GameObject. Set Pivot to the inner edge. Scale Y to cover full screen height. `DifficultyManager.UpdateWalls()` sets X position each frame. No colliders — purely visual.

#### Script Changes Required

**`Orb.cs`**
- Add `[SerializeField] FloatVariable playAreaHalfWidth;`
- In `FixedUpdate`, replace `-halfWidth`/`halfWidth` with `-playAreaHalfWidth.Value`/`playAreaHalfWidth.Value`
- Remove `private float halfWidth;` cached field
- Remove `speed` serialized field; add `float speed` parameter to `Initialize(bool isWhite, Vector2 direction, float speed)`
- Remove `minLifetime`, `maxLifetime` fields and `Destroy(gameObject, ...)` call from `Initialize`
- Remove `gameStartEvent` field and `OnEnable`/`OnDisable` subscriptions (cleanup handled by OrbManager — see §3)

**`OrbSpawner.cs`**
- Add `[SerializeField] FloatVariable playAreaHalfWidth;`
- Add `[SerializeField] DifficultyManager difficulty;`
- Replace `halfWidth` cached field reads with `playAreaHalfWidth.Value`
- Replace fixed `spawnInterval` field with `difficulty.CurrentSpawnInterval` in `Update`
- Pass `difficulty.CurrentOrbSpeed` as third arg to `orb.Initialize(...)`
- Register spawned orb with OrbManager (see §3)

**`PlayerController.cs`**
- Add `[SerializeField] FloatVariable playAreaHalfWidth;`
- Replace cached `leftBound`/`rightBound` with live computation in `MoveHorizontal`:
  ```csharp
  float hw = playAreaHalfWidth.Value - playerRadius;
  newPos.x = Mathf.Clamp(newPos.x, -hw, hw);
  ```
- Remove `leftBound`/`rightBound` fields

**`GameManager.cs`**
- No changes needed — `gameStartEvent.Raise()` already fires; `DifficultyManager` subscribes to that.

#### Resets on GameStart
| State | Reset |
|-------|-------|
| `DifficultyManager.elapsedTime` | → 0 |
| `PlayAreaHalfWidth.asset` | → `initialHalfWidth` |
| Wall sprites | → full width positions |
| `CurrentSpawnInterval` | → `baseInterval` |
| `CurrentOrbSpeed` | → `baseSpeed` |

---

### Axis 2 — Spawn Rate + Orb Speed Scale With Score

Formula:
```
spawnInterval = Mathf.Max(minInterval, baseInterval - score * spawnScaleFactor)
orbSpeed      = Mathf.Min(maxSpeed,    baseSpeed    + score * speedScaleFactor)
```

Default values in DifficultyManager above. All tunable `SerializeField`s.

At score 25: interval ≈ 1.0s, speed ≈ 5.0
At score 40: interval ≈ 0.4s (capped), speed ≈ 7.0 (capped)

---

### Orb Lifetime — Removed

Orbs no longer self-destruct after a timer. They bounce endlessly until collected. On restart, OrbManager destroys all live orbs (see §3).

Without lifetime culling, orb count grows continuously. At peak spawn rate (0.4s interval) that's ~150 orbs/min. This is intentional — shrinking play area compresses them into a dense, chaotic space. If playtest feels too cluttered, add `OrbManager.maxOrbs` cap as a tuning knob.

---

## 2. Boons & Afflictions

**(Stub — not yet designed)**

Placeholder for a future system that can grant temporary advantages (boons) or impose temporary handicaps (afflictions) based on play events. Ideas to explore:

- Boon: brief slow-motion window after a correct-color streak
- Boon: magnetism — correct-color orbs briefly pulled toward player
- Affliction: inverted controls after wrong-color hit
- Affliction: colorRatio drifts toward 0.5 passively over time

---

## 3. General Improvements

### 3a. OrbManager

Central registry for all live orbs. Removes `gameStartEvent` from the Orb prefab — orbs become pure physics objects. Cleanup is handled in one place.

```csharp
// OrbManager.cs
using System.Collections.Generic;
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [SerializeField] private GameEvent gameStartEvent;

    private readonly List<Orb> activeOrbs = new();

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    public void Register(Orb orb) => activeOrbs.Add(orb);

    public void Unregister(Orb orb) => activeOrbs.Remove(orb);

    private void OnGameStart()
    {
        // Iterate over a copy — Destroy triggers Unregister which modifies the list
        foreach (var orb in activeOrbs.ToArray())
        {
            if (orb != null) Destroy(orb.gameObject);
        }
        activeOrbs.Clear();
    }
}
```

**Wire-up:**
- `OrbSpawner` gets `[SerializeField] OrbManager orbManager` → calls `orbManager.Register(orb)` after `Instantiate`
- `Orb.OnDestroy()` calls `orbManager.Unregister(this)` — add `[SerializeField] OrbManager orbManager` to Orb

---

### 3b. High Score (PlayerPrefs)

On game over, check and persist high score:
```csharp
// In GameOverUI.OnGameOver() (or GameManager.OnGameOver()):
int currentScore = score.Value;
int highScore = PlayerPrefs.GetInt("HighScore", 0);
if (currentScore > highScore)
{
    highScore = currentScore;
    PlayerPrefs.SetInt("HighScore", highScore);
    PlayerPrefs.Save();
}
finalScoreLabel.text    = $"SCORE      {currentScore}";
highScoreLabel.text     = $"BEST       {highScore}";
```

**UXML change:** Add a `<Label name="high-score-label">` below `final-score-label` in `GameOverUI.uxml`.
**`GameOverUI.cs` change:** Query and populate `highScoreLabel` alongside the existing `finalScoreLabel`.

---

### 3c. Audio

| Event | Sound |
|-------|-------|
| Correct orb collected | Short bright chime |
| Wrong-color hit | Low thud / buzz |
| Game over | Descending tone / sting |
| Background | Minimal looping ambient track |

**Implementation:** `AudioManager` MonoBehaviour on a persistent GameObject. `AudioClip` fields for each sound. Wire to:
- `IntegerValue.OnChange` (health down → wrong-color buzz; could also check score)
- `GameEvent.OnRaised` for game over sting
- `AudioSource.loop = true` for background music, play on Start

No new SO architecture needed — just an `AudioSource` + `MonoBehaviour` with serialized clips.

---

### 3d. Visual Juice

| Effect | Trigger | Implementation |
|--------|---------|----------------|
| Screen shake | Wrong-color hit | Manual camera position nudge over 0.2s (coroutine), or Cinemachine Impulse |
| Particle burst | Any orb collected | `ParticleSystem` child on player; burst color matches collected orb |
| HUD flash | Wrong-color hit | Brief red tint on a full-screen `VisualElement` overlay (alpha flash via USS transition) |
| Wall pulse | Boundary shrinks | Wall sprites briefly scale/lighten when boundary moves inward |

Screen shake sketch (no Cinemachine dependency):
```csharp
// ShakeCamera.cs — call ShakeCamera.instance.Shake(0.15f, 0.2f)
IEnumerator DoShake(float magnitude, float duration)
{
    Vector3 origin = transform.localPosition;
    float elapsed = 0f;
    while (elapsed < duration)
    {
        float x = Random.Range(-1f, 1f) * magnitude;
        float y = Random.Range(-1f, 1f) * magnitude;
        transform.localPosition = origin + new Vector3(x, y, 0f);
        elapsed += Time.deltaTime;
        yield return null;
    }
    transform.localPosition = origin;
}
```

---

## Implementation Order (suggested)

1. `FloatVariable.cs` + `PlayAreaHalfWidth.asset` — new SO, no dependencies
2. `OrbManager.cs` — new script, simplifies Orb prefab before other changes
3. `DifficultyManager.cs` — new script
4. Modify `Orb.cs` — remove lifetime, GameEvent, cached bounds; add speed param, OrbManager ref, FloatVariable ref
5. Modify `OrbSpawner.cs` — FloatVariable, DifficultyManager, OrbManager wiring
6. Modify `PlayerController.cs` — FloatVariable live clamp
7. Wall sprites — scene setup
8. High score — `GameOverUI.cs` + UXML
9. Audio + visual juice — last, non-blocking
