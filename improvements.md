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

`DifficultyManager` shrinks `halfWidthPlayArea` (a `FloatValue` asset) from the initial camera width to a minimum (~40%) over ~90 seconds. Two black rectangular sprite GameObjects (no colliders) move inward from screen edges to mark the current boundary. Visuals and physics stay locked together because both read the same `FloatValue`.

**Key numbers (all tunable):**
```
initialHalfWidth  = computed from camera on Awake (GameManager sets it)
minHalfWidth      = initialHalfWidth * 0.4f   (~3.6 units on 16:9)
shrinkRate        = 0.03f world-units/second
```

**Why not Camera.rect tween?**
`Camera.rect` only affects the rendered image in screen space. It does not change `orthographicSize * aspect`, so world-space bounds stay the same. Player clamp, orb bounce, and spawn positions would all remain at the original coordinates — player can walk behind the visual wall, orbs bounce off invisible boundaries. Visuals and physics decouple.

**Why not BoxCollider2D walls?**
Orbs use `Is Trigger = true`. Triggers ignore physics collisions entirely — the bounce in `Orb.FixedUpdate()` is manual math, not physics resolution. Static collider walls have zero effect on triggers.

**Conclusion:** Wall GameObjects are visual-only sprites. `DifficultyManager` drives both their position and `halfWidthPlayArea.Set(newValue)` from the same computed value each frame. `Orb.cs` and `OrbSpawner` read `.Value` at runtime — no stale cached bounds.

**Orb boundary handling during shrink:**
`Orb.cs` currently caches `halfWidth` once in `Awake`. Fix: replace cached `halfWidth` with a `[SerializeField] FloatValue halfWidthPlayArea` reference and read `.Value` in `FixedUpdate`. When the boundary shrinks past an orb's position, the bounce condition triggers on the next physics step and reflects the orb inward — no special handling needed. Shrink rate (~0.03 units/sec) is far smaller than orb travel per frame (~0.14 units at max speed), so orbs can never overshoot.

#### DifficultyManager

```csharp
// DifficultyManager.cs
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    [Header("Boundary")]
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private Transform leftWall;
    [SerializeField] private Transform rightWall;
    [SerializeField] private float shrinkRate = 0.03f;
    [SerializeField] private float minHalfWidthFraction = 0.4f;

    [Header("Spawn / Speed")]
    [SerializeField] private float baseInterval = 2f;
    [SerializeField] private float minInterval = 0.4f;
    [SerializeField] private float spawnScaleFactor = 0.04f;
    [SerializeField] private float baseSpeed = 3f;
    [SerializeField] private float maxSpeed = 7f;
    [SerializeField] private float speedScaleFactor = 0.08f;

    [Header("References")]
    [SerializeField] private IntegerValue score;
    [SerializeField] private GameEvent gameStartEvent;

    private float initialHalfWidth;
    private float minHalfWidth;
    private float elapsedTime;

    public float CurrentSpawnInterval { get; private set; }
    public float CurrentOrbSpeed { get; private set; }

    void Start()
    {
        initialHalfWidth = halfWidthPlayArea.Value; // GameManager.Awake already set this
        minHalfWidth = initialHalfWidth * minHalfWidthFraction;
        ResetDifficulty();
    }

    void OnEnable() => gameStartEvent.OnRaised += ResetDifficulty;
    void OnDisable() => gameStartEvent.OnRaised -= ResetDifficulty;

    void Update()
    {
        elapsedTime += Time.deltaTime;

        float newHalfWidth = Mathf.Max(minHalfWidth, initialHalfWidth - shrinkRate * elapsedTime);
        halfWidthPlayArea.Set(newHalfWidth);
        UpdateWalls(newHalfWidth);

        CurrentSpawnInterval = Mathf.Max(minInterval, baseInterval - score.Value * spawnScaleFactor);
        CurrentOrbSpeed = Mathf.Min(maxSpeed, baseSpeed + score.Value * speedScaleFactor);
    }

    private void UpdateWalls(float halfWidth)
    {
        if (leftWall != null)  leftWall.position  = new Vector3(-halfWidth, 0f, 0f);
        if (rightWall != null) rightWall.position = new Vector3( halfWidth, 0f, 0f);
    }

    private void ResetDifficulty()
    {
        elapsedTime = 0f;
        if (initialHalfWidth > 0) halfWidthPlayArea.Set(initialHalfWidth);
        CurrentSpawnInterval = baseInterval;
        CurrentOrbSpeed = baseSpeed;
    }
}
```

> **Wall sprite setup:** Two black rectangular sprites (`WallLeft`, `WallRight`), no colliders. Set Pivot to inner edge. Scale Y to cover full screen height. `DifficultyManager.UpdateWalls()` sets X position each frame.

#### Script Changes Required

**`Orb.cs`**
- Add `[SerializeField] FloatValue halfWidthPlayArea;`
- In `FixedUpdate`, replace `-halfWidth`/`halfWidth` with `-halfWidthPlayArea.Value`/`halfWidthPlayArea.Value`
- Remove `private float halfWidth;` cached field and its `Awake` assignment

**`OrbSpawner.cs`**
- Already reads `halfWidthPlayArea.Value` — no change needed
- Add `[SerializeField] DifficultyManager difficulty;` and replace the two serialized floats on `OrbManager` with reads from `difficulty.CurrentSpawnInterval` / `difficulty.CurrentOrbSpeed`

**`PlayerController.cs`**
- Add `[SerializeField] FloatValue halfWidthPlayArea;`
- Replace cached `leftBound`/`rightBound` with live computation in `MoveHorizontal`:
  ```csharp
  float hw = halfWidthPlayArea.Value - playerRadius;
  newPos.x = Mathf.Clamp(newPos.x, -hw, hw);
  ```
- Remove `leftBound`/`rightBound` fields

**`GameManager.cs`**
- No changes — `GameManager.Awake` already seeds `halfWidthPlayArea`; `DifficultyManager` overwrites it from `Start` onward. `gameStartEvent.Raise()` already fires on restart.

#### Resets on GameStart
| State | Who resets it |
|-------|--------------|
| `elapsedTime` | `DifficultyManager.ResetDifficulty` |
| `halfWidthPlayArea` | `DifficultyManager.ResetDifficulty` → `Set(initialHalfWidth)` |
| Wall sprites | `DifficultyManager.UpdateWalls` called same frame |
| `CurrentSpawnInterval` / `CurrentOrbSpeed` | `DifficultyManager.ResetDifficulty` |

---

### Axis 2 — Spawn Rate + Orb Speed Scale With Score

```
spawnInterval = Mathf.Max(minInterval, baseInterval - score * spawnScaleFactor)
orbSpeed      = Mathf.Min(maxSpeed,    baseSpeed    + score * speedScaleFactor)
```

Default values in `DifficultyManager` above. All tunable `SerializeField`s.

At score 25: interval ≈ 1.0s, speed ≈ 5.0
At score 40: interval ≈ 0.4s (capped), speed ≈ 7.0 (capped)

**Wire-up to OrbManager:** Replace `OrbManager`'s serialized `spawnInterval`/`orbSpeed` fields with a `[SerializeField] DifficultyManager difficulty` reference. In `Update`, read `difficulty.CurrentSpawnInterval` and pass `difficulty.CurrentOrbSpeed` to `orbSpawner.Spawn(...)`.

---

### Orb Lifetime — Already Removed ✓

Orbs bounce endlessly until collected. `OrbSet` SO + `OrbManager.OnGameStart` handles cleanup on restart.

Without lifetime culling, orb count grows continuously. At peak spawn rate (0.4s interval) that's ~150 orbs/min. This is intentional — the shrinking play area compresses them. If playtest feels too cluttered, add `OrbManager.maxOrbs` cap.

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

### 3a. High Score (PlayerPrefs)

On game over, check and persist high score. Best added to `GameOverUI.cs`:

```csharp
private void OnGameOver()
{
    int currentScore = score.Value;
    int highScore = PlayerPrefs.GetInt("HighScore", 0);
    if (currentScore > highScore)
    {
        highScore = currentScore;
        PlayerPrefs.SetInt("HighScore", highScore);
        PlayerPrefs.Save();
    }
    finalScoreLabel.text = $"SCORE  {currentScore}";
    highScoreLabel.text  = $"BEST   {highScore}";
    overlay.style.display = DisplayStyle.Flex;
}
```

**UXML change:** Add `<Label name="high-score-label">` below `final-score-label` in `GameOverUI.uxml`.
**`GameOverUI.cs` change:** Query `highScoreLabel` in `OnEnable` alongside `finalScoreLabel`.

---

### 3b. Audio

| Event | Sound |
|-------|-------|
| Correct orb collected | Short bright chime |
| Wrong-color hit | Low thud / buzz |
| Game over | Descending tone / sting |
| Background | Minimal looping ambient track |

`AudioManager` MonoBehaviour on a persistent GameObject. `AudioClip` fields for each sound. Wire to `IntegerValue.OnChange` (health), `GameEvent.OnRaised` (game over), and `AudioSource.loop = true` for background. No new SO architecture needed.

---

### 3c. Visual Juice

| Effect | Trigger | Implementation |
|--------|---------|----------------|
| Screen shake | Wrong-color hit | Manual camera position nudge over 0.2s (coroutine) |
| Particle burst | Any orb collected | `ParticleSystem` child on player; burst color matches collected orb |
| HUD flash | Wrong-color hit | Full-screen `VisualElement` overlay, alpha flash via USS transition |
| Wall pulse | Boundary shrinks | Wall sprites briefly scale/lighten when `halfWidthPlayArea.OnChange` fires |

Screen shake (no Cinemachine):
```csharp
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

## Implementation Order (next steps)

1. `DifficultyManager.cs` — new script; wire `halfWidthPlayArea` + wall sprites
2. Modify `Orb.cs` — replace cached `halfWidth` with `FloatValue halfWidthPlayArea` live read
3. Modify `OrbManager.cs` — add `DifficultyManager` reference; replace serialized `spawnInterval`/`orbSpeed` with `difficulty.Current*` reads
4. Modify `PlayerController.cs` — `FloatValue` live clamp
5. Wall sprites — scene setup
6. High score — `GameOverUI.cs` + UXML
7. Audio + visual juice — last, non-blocking
