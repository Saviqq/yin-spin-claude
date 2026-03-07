# Powerup Step 3 — Clear Orbs + Balance Color Ratio

## Scope

| File | Change |
|------|--------|
| `ClearOrbsEffect.cs` | New SO — destroys all live orbs on pickup |
| `BalanceColorEffect.cs` | New SO — nudges colorRatio toward 0.5 by one COLLECT_DELTA |
| `Player.cs` | Add `balanceColorEvent` field + `OnBalanceColor` handler |

---

## `ClearOrbsEffect.cs`

`OrbSet` is a ScriptableObject, so it can be referenced directly from another SO.

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ClearOrbsEffect", menuName = "Scriptable Objects/Powerup Effects/ClearOrbs")]
public class ClearOrbsEffect : PowerupEffect
{
    [SerializeField] private OrbSet orbSet;

    public override void Apply()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);
    }
}
```

Iterates in reverse — same pattern as `OrbManager.OnGameStart`. `Destroy` triggers `Orb.OnDisable` which calls `orbSet.Remove(this)`, keeping the set clean.

---

## `BalanceColorEffect.cs`

`colorRatio` is private to `Player.cs`. Rather than exposing it directly or giving the SO a scene reference, the effect raises a `GameEvent` that `Player.cs` handles — consistent with how all cross-system communication works in this codebase.

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "BalanceColorEffect", menuName = "Scriptable Objects/Powerup Effects/BalanceColor")]
public class BalanceColorEffect : PowerupEffect
{
    [SerializeField] private GameEvent balanceColorEvent;

    public override void Apply()
    {
        balanceColorEvent.Raise();
    }
}
```

---

## `Player.cs`

Add `balanceColorEvent` to the Events header and wire `OnBalanceColor` in `OnEnable`/`OnDisable`.

```csharp
[Header("Events")]
[SerializeField] private GameEvent gameStartEvent;
[SerializeField] private GameEvent balanceColorEvent;

void OnEnable()
{
    gameStartEvent.OnRaised += OnGameStart;
    balanceColorEvent.OnRaised += OnBalanceColor;
}

void OnDisable()
{
    gameStartEvent.OnRaised -= OnGameStart;
    balanceColorEvent.OnRaised -= OnBalanceColor;
}
```

Handler — nudges colorRatio one step toward 0.5:

```csharp
private void OnBalanceColor()
{
    if (colorRatio > 0.5f)
        colorRatio -= Constants.COLLECT_DELTA;
    else if (colorRatio < 0.5f)
        colorRatio += Constants.COLLECT_DELTA;

    colorRatio = Mathf.Clamp01(colorRatio);
    splitMaterial.SetFloat("_ColorRatio", colorRatio);
}
```

If `colorRatio` is already exactly `0.5`, neither branch fires and nothing changes — correct behaviour.

---

## New Assets

| Asset | Type | Wiring |
|-------|------|--------|
| `ClearOrbsEffect.asset` | ClearOrbsEffect | Orb Set: `OrbSet.asset` |
| `BalanceColorEffect.asset` | BalanceColorEffect | Balance Color Event: `BalanceColorEvent.asset` |
| `BalanceColorEvent.asset` | GameEvent | — |

---

## Inspector Wiring

### Player
| Field | Value |
|-------|-------|
| Balance Color Event | `BalanceColorEvent.asset` |

### PowerupManager — Effects array
Add `ClearOrbsEffect.asset` and `BalanceColorEffect.asset` alongside existing effects.

---

## Verify

| Test | Expected |
|------|----------|
| Pick up clear orbs | All orbs instantly gone, no score awarded |
| Pick up balance color at 70/30 | colorRatio shifts by COLLECT_DELTA toward 0.5 (e.g. 0.7 → 0.6) |
| Pick up balance color at 50/50 | No visible change |
| Pick up balance color at 30/70 | colorRatio shifts by COLLECT_DELTA toward 0.5 (e.g. 0.3 → 0.4) |
| Shader updates immediately | Split line visually moves after balance pickup |
