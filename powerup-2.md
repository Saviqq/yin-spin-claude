# Powerup Step 2 — Gain Score Effect + Spawn Position Validation

## Scope

| File | Change |
|------|--------|
| `GainScoreEffect.cs` | New SO — adds 5 score on pickup |
| `PowerupManager.cs` | `SpawnPowerup` gains position validation: min distance from player and from existing powerups |

---

## `GainScoreEffect.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GainScoreEffect", menuName = "Scriptable Objects/Powerup Effects/GainScore")]
public class GainScoreEffect : PowerupEffect
{
    [SerializeField] private IntegerValue score;
    [SerializeField] private int amount = 5;

    public override void Apply()
    {
        score.Set(score.Value + amount);
    }
}
```

`amount` defaults to 5 but is a serialized field — tunable per asset without touching code.

---

## `PowerupManager.cs`

Two additions to the existing script:
- `[SerializeField] Transform player` — reference to the Player GameObject transform
- `[SerializeField] float minDistance = 1f` — single threshold used for both player distance and inter-powerup distance
- `SpawnPowerup` delegates to `TryGetValidPosition` before instantiating; skips spawn silently if no valid position found after 20 attempts

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

    [Header("Spawn Validation")]
    [SerializeField] private Transform player;
    [SerializeField] private float minDistance = 1f;

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
        if (!TryGetValidPosition(out Vector2 pos)) return;

        Powerup p = Instantiate(powerupPrefab, pos, Quaternion.identity);
        p.SetEffect(effects[Random.Range(0, effects.Length)]);
    }

    private bool TryGetValidPosition(out Vector2 pos)
    {
        float hw = halfWidthPlayArea.Value * 0.9f;
        float hh = halfHeightPlayArea.Value * 0.9f;

        const int maxAttempts = 20;
        for (int i = 0; i < maxAttempts; i++)
        {
            Vector2 candidate = new Vector2(Random.Range(-hw, hw), Random.Range(-hh, hh));

            if (Vector2.Distance(candidate, player.position) < minDistance)
                continue;

            bool tooClose = false;
            foreach (Powerup existing in powerupSet.Items)
            {
                if (Vector2.Distance(candidate, existing.transform.position) < minDistance)
                {
                    tooClose = true;
                    break;
                }
            }

            if (!tooClose)
            {
                pos = candidate;
                return true;
            }
        }

        pos = Vector2.zero;
        return false;
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

**Why 20 attempts then skip:** In a nearly-full play area (many powerups, player near center) there may genuinely be no valid spot. Skipping silently is better than an infinite loop or forcing a bad position. The timer resets to 0 regardless, so the next attempt comes after another full interval.

**`minDistFromPlayer` and `minDistFromPowerup` as serialized fields:** These are spawn-feel tuning values, not game logic constants — keeping them in the Inspector makes them easy to adjust without recompiling.

---

## New Asset

| Asset | Type | Wiring |
|-------|------|--------|
| `GainScoreEffect.asset` | GainScoreEffect | Score: `Score.asset`; Amount: `5` |

---

## Inspector Wiring

### PowerupManager — new fields
| Field | Value |
|-------|-------|
| Player | Player GameObject transform |
| Min Distance | `1` |

### PowerupManager — Effects array
Add `GainScoreEffect.asset` alongside `GainHeartEffect.asset`. Both are now randomly selected at spawn time.

---

## Verify

| Test | Expected |
|------|----------|
| Pick up score powerup | Score increases by 5 |
| Powerup always spawns > 2 units from player | Never appears right on top of player |
| Multiple powerups active | Each spawns > 1 unit away from all others |
| Play area very crowded | Spawn skipped silently for that interval, retried next interval |
