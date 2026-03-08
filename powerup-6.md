# Powerup 6 — Expand Play Area + Switch Orb Colors

## New Files

### `Scripts/SO/Powerup/ExpandPlayAreaEffect.cs`
```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayAreaEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayArea")]
public class ExpandPlayAreaEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayAreaEvent;

    public override void Apply() => expandPlayAreaEvent.Raise();
}
```

### `Scripts/SO/Powerup/SwitchOrbColorsEffect.cs`
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

## Modified Files

### `Scripts/DifficultyManager.cs` — changes
- `ShrinkState` enum: add `Expanding`
- Fields: add `expandPlayAreaEvent`, `expandFrom`, `expandTo`
- `OnEnable/OnDisable`: subscribe `expandPlayAreaEvent.OnRaised += BeginExpand`
- New method `BeginExpand()`: captures current value, targets `current + shrinkStep` clamped to `initialHalfWidth`, transitions to `Expanding`, resets timer
- `Update` switch: new `Expanding` case — SmoothStep from `expandFrom` to `expandTo`, returns to `Waiting` when done

### `Scripts/Orb.cs` — add method
```csharp
public void FlipColor()
{
    IsWhite = !IsWhite;
    GetComponent<SpriteRenderer>().color = IsWhite ? Color.white : Color.black;
}
```

### `Scripts/OrbManager.cs` — changes
- Add `[SerializeField] private GameEvent switchOrbColorsEvent`
- `OnEnable/OnDisable`: subscribe `switchOrbColorsEvent.OnRaised += OnSwitchOrbColors`
- New method:
```csharp
private void OnSwitchOrbColors()
{
    for (int i = 0; i < orbSet.Items.Count; i++)
        orbSet.Items[i].FlipColor();
}
```

---

## Unity Setup

### Create Assets
| Asset | Menu path |
|-------|-----------|
| `ExpandPlayAreaEvent.asset` | ScriptableObjects → GameEvent |
| `ExpandPlayAreaEffect.asset` | ScriptableObjects → Powerup Effects → ExpandPlayArea |
| `SwitchOrbColorsEvent.asset` | ScriptableObjects → GameEvent |
| `SwitchOrbColorsEffect.asset` | ScriptableObjects → Powerup Effects → SwitchOrbColors |

### Wire Assets
| Component | Field | Value |
|-----------|-------|-------|
| `ExpandPlayAreaEffect.asset` | Expand Play Area Event | `ExpandPlayAreaEvent.asset` |
| `SwitchOrbColorsEffect.asset` | Switch Orb Colors Event | `SwitchOrbColorsEvent.asset` |
| `DifficultyManager` | Expand Play Area Event | `ExpandPlayAreaEvent.asset` |
| `OrbManager` | Switch Orb Colors Event | `SwitchOrbColorsEvent.asset` |
| `PowerupManager` | Effects array | add both new Effect assets |
