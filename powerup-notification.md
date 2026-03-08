# Powerup Notification — Pickup Banner

## Scope

| File | Change |
|------|--------|
| `Scripts/SO/StringEvent.cs` | New — `StringEvent : GameEvent<string>` |
| `Scripts/SO/Powerup/PowerupEffect.cs` | Add `powerupMessageEvent` + `message` fields + `RaiseMessage()` helper |
| `Scripts/SO/Powerup/GainHeartEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/GainScoreEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/ClearOrbsEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/BalanceColorEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/ExpandPlayerEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/ShrinkPlayerEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/SpawnBurstEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/FreezeOrbsEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/ExpandPlayAreaEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/SO/Powerup/SwitchOrbColorsEffect.cs` | Append `RaiseMessage()` in `Apply()` |
| `Scripts/UI/PowerupNotificationUI.cs` | New — queries `powerup-label`, drives show/hide |
| `UI/HUD.uxml` | Add `powerup-label` Label between the two `hud-section` children |
| `UI/HUD.uss` | Add `.powerup-label` rule |

---

## Design

### StringEvent

Same pattern as `IntegerEvent` — one line, inherits `GameEvent<string>`. One shared asset (`PowerupMessageEvent.asset`) is wired to every effect SO and to `PowerupNotificationUI`.

### PowerupEffect base changes

Two new serialized fields added to the abstract base so every concrete effect inherits them automatically:
- `powerupMessageEvent` — the shared `StringEvent` asset
- `message` — the short string set per-asset in the Inspector

`RaiseMessage()` is a protected helper that calls `powerupMessageEvent?.Raise(message)`. The null-guard means existing assets without the event wired continue to work (no-op).

Each concrete `Apply()` body gets one `RaiseMessage()` call appended at the end — after the effect fires, the notification goes out.

### PowerupNotificationUI

Subscribes to `StringEvent.OnRaised`, `gameStartEvent.OnRaised`, and `gamePausedEvent.OnRaised`.

On message received:
1. Set label text.
2. Make label visible (`DisplayStyle.Flex`).
3. Stop any running hide coroutine.
4. Start a new hide coroutine that waits `displayDuration` seconds then hides the label.

On restart or pause: stop any running coroutine and hide immediately.

The coroutine pattern is identical to the freeze/scale effects — `StopCoroutine` + `StartCoroutine` on re-trigger.

### UXML

A `Label` with name `powerup-label` and class `powerup-label` is inserted between `hearts-container` and `score-container` inside `top-bar`. With `justify-content: space-between` on the parent, `flex-grow: 1` on the label pushes hearts left and score right while the text centers in the remaining space.

### USS

`.powerup-label` — white bold 18px text, centered, hidden by default, `flex-grow: 1`.

---

## Message Strings (set per asset in Inspector)

| Effect | `message` field |
|--------|----------------|
| GainHeartEffect | `+1 Heart` |
| GainScoreEffect | `+5 Score` |
| ClearOrbsEffect | `Orbs Cleared` |
| BalanceColorEffect | `Balance Restored` |
| ExpandPlayerEffect | `Player Expanded` |
| ShrinkPlayerEffect | `Player Shrunk` |
| SpawnBurstEffect | `Orb Burst!` |
| FreezeOrbsEffect | `Orbs Frozen` |
| ExpandPlayAreaEffect | `Area Expanded` |
| SwitchOrbColorsEffect | `Colors Flipped` |

---

## `StringEvent.cs` *(new — `Scripts/SO/` folder)*

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "StringEvent", menuName = "Scriptable Objects/StringEvent")]
public class StringEvent : GameEvent<string> { }
```

---

## `PowerupEffect.cs` (updated)

```csharp
using UnityEngine;

public abstract class PowerupEffect : ScriptableObject
{
    [SerializeField] private StringEvent powerupMessageEvent;
    [SerializeField] protected string message;

    public abstract void Apply();

    protected void RaiseMessage() => powerupMessageEvent?.Raise(message);
}
```

---

## `GainHeartEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "GainHeartEffect", menuName = "Scriptable Objects/Powerup Effects/GainHeart")]
public class GainHeartEffect : PowerupEffect
{
    [SerializeField] private IntegerValue health;

    public override void Apply()
    {
        health.Set(Mathf.Min(health.Value + 1, Constants.MAX_HEALTH));
        RaiseMessage();
    }
}
```

---

## `GainScoreEffect.cs` (updated)

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
        RaiseMessage();
    }
}
```

---

## `ClearOrbsEffect.cs` (updated)

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
        RaiseMessage();
    }
}
```

---

## `BalanceColorEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "BalanceColorEffect", menuName = "Scriptable Objects/Powerup Effects/BalanceColor")]
public class BalanceColorEffect : PowerupEffect
{
    [SerializeField] private GameEvent balanceColorEvent;

    public override void Apply()
    {
        balanceColorEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `ExpandPlayerEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayer")]
public class ExpandPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayerEvent;

    public override void Apply()
    {
        expandPlayerEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `ShrinkPlayerEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ShrinkPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ShrinkPlayer")]
public class ShrinkPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent shrinkPlayerEvent;

    public override void Apply()
    {
        shrinkPlayerEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `SpawnBurstEffect.cs` (updated)

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
        RaiseMessage();
    }
}
```

---

## `FreezeOrbsEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "FreezeOrbsEffect", menuName = "Scriptable Objects/Powerup Effects/FreezeOrbs")]
public class FreezeOrbsEffect : PowerupEffect
{
    [SerializeField] private GameEvent freezeOrbsEvent;

    public override void Apply()
    {
        freezeOrbsEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `ExpandPlayAreaEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayAreaEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayArea")]
public class ExpandPlayAreaEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayAreaEvent;

    public override void Apply()
    {
        expandPlayAreaEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `SwitchOrbColorsEffect.cs` (updated)

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "SwitchOrbColorsEffect", menuName = "Scriptable Objects/Powerup Effects/SwitchOrbColors")]
public class SwitchOrbColorsEffect : PowerupEffect
{
    [SerializeField] private GameEvent switchOrbColorsEvent;

    public override void Apply()
    {
        switchOrbColorsEvent.Raise();
        RaiseMessage();
    }
}
```

---

## `PowerupNotificationUI.cs` *(new — `Scripts/UI/` folder)*

```csharp
using System.Collections;
using UnityEngine;
using UnityEngine.UIElements;

public class PowerupNotificationUI : MonoBehaviour
{
    [SerializeField] private StringEvent powerupMessageEvent;
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private float displayDuration = 2f;

    private Label label;
    private Coroutine activeHideCoroutine;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        label = root.Q<Label>("powerup-label");
        label.style.display = DisplayStyle.None;

        powerupMessageEvent.OnRaised += OnMessage;
        gameStartEvent.OnRaised += HideImmediate;
        gamePausedEvent.OnRaised += HideImmediate;
    }

    void OnDisable()
    {
        powerupMessageEvent.OnRaised -= OnMessage;
        gameStartEvent.OnRaised -= HideImmediate;
        gamePausedEvent.OnRaised -= HideImmediate;
    }

    private void OnMessage(string msg)
    {
        label.text = msg;
        label.style.display = DisplayStyle.Flex;

        if (activeHideCoroutine != null)
            StopCoroutine(activeHideCoroutine);

        activeHideCoroutine = StartCoroutine(HideAfterDelay());
    }

    private IEnumerator HideAfterDelay()
    {
        yield return new WaitForSeconds(displayDuration);
        label.style.display = DisplayStyle.None;
        activeHideCoroutine = null;
    }

    private void HideImmediate()
    {
        if (activeHideCoroutine != null)
        {
            StopCoroutine(activeHideCoroutine);
            activeHideCoroutine = null;
        }
        label.style.display = DisplayStyle.None;
    }
}
```

---

## `UI/HUD.uxml` (updated)

Add `powerup-label` Label between `hearts-container` and `score-container`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/UI/HUD.uss?fileID=7433441132597879392&amp;guid=b11ce83a02240394e899a7361217e753&amp;type=3#HUD"/>
    <ui:VisualElement name="top-bar" class="top-bar">
        <ui:VisualElement name="hearts-container" class="hud-section">
            <ui:VisualElement name="heart-0" class="heart"/>
            <ui:VisualElement name="heart-1" class="heart"/>
            <ui:VisualElement name="heart-2" class="heart" />
            <ui:VisualElement name="heart-3" class="heart" />
            <ui:VisualElement name="heart-4" class="heart" />
        </ui:VisualElement>
        <ui:Label name="powerup-label" class="powerup-label" text=""/>
        <ui:VisualElement name="score-container" class="hud-section">
            <ui:VisualElement name="score-icon" class="score-icon" style="background-image: url(&quot;project://database/Assets/Art/star.png?fileID=2800000&amp;guid=3766b0fc694c5ff41aba3971477bc52f&amp;type=3#star&quot;);">
                <ui:Label name="score-label" text="0" class="score-value"/>
            </ui:VisualElement>
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="game-over-overlay" class="menu-overlay">
        <ui:VisualElement name="game-over-panel" class="menu-panel">
            <ui:Label text="GAME OVER" class="menu-title"/>
            <ui:Label name="final-score-label" text="SCORE  0" class="final-score-label"/>
            <ui:Button name="restart-btn" text="RESTART" class="menu-btn"/>
            <ui:Button name="exit-btn" text="EXIT" class="menu-btn"/>
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="pause-overlay" class="menu-overlay">
        <ui:VisualElement class="menu-panel">
            <ui:Label text="PAUSED" class="menu-title"/>
            <ui:Button name="pause-resume-btn" text="RESUME" class="menu-btn"/>
            <ui:Button name="pause-restart-btn" text="RESTART" class="menu-btn"/>
            <ui:Button name="pause-exit-btn" text="QUIT" class="menu-btn"/>
        </ui:VisualElement>
    </ui:VisualElement>
    <ui:VisualElement name="footer-bar" class="footer-bar">
        <ui:VisualElement name="VisualElement" class="hint-group">
            <ui:VisualElement name="key-w" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/w.png?fileID=2800000&amp;guid=4e4a719737de9de409ffd1992a71735e&amp;type=3#w&quot;);"/>
            <ui:VisualElement name="key-a" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/a.png?fileID=2800000&amp;guid=9496cec9e63e1664bbb5c8ba633b552c&amp;type=3#a&quot;);"/>
            <ui:VisualElement name="key-s" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/s.png?fileID=2800000&amp;guid=be1e6e80e4c1a9544817ed2181ba7fa6&amp;type=3#s&quot;);"/>
            <ui:VisualElement name="key-d" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/d.png?fileID=2800000&amp;guid=106649617ee010e479d8b765c186cbfb&amp;type=3#d&quot;);"/>
            <ui:Label text="Move" class="hint-label"/>
        </ui:VisualElement>
        <ui:VisualElement class="hint-group">
            <ui:VisualElement name="key-left" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/left-arrow.png?fileID=2800000&amp;guid=683f3f764bb0aa14193ae5b49fa123bc&amp;type=3#left-arrow&quot;);"/>
            <ui:VisualElement name="key-right" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/right-arrow.png?fileID=2800000&amp;guid=d9a01ef0a3653a74f8f543dcf7141816&amp;type=3#right-arrow&quot;);"/>
            <ui:Label text="Rotate" class="hint-label"/>
        </ui:VisualElement>
        <ui:VisualElement class="hint-group">
            <ui:VisualElement name="key-esc" class="key-icon" style="background-image: url(&quot;project://database/Assets/Art/esc.png?fileID=2800000&amp;guid=6985e95e0ae7d41419c2efe7b5e3575e&amp;type=3#esc&quot;);"/>
            <ui:Label text="Pause" class="hint-label"/>
        </ui:VisualElement>
    </ui:VisualElement>
</ui:UXML>
```

---

## `UI/HUD.uss` (updated — append after `.hint-label`)

```css
/* ── Powerup notification ─────────────────────────────── */
.powerup-label {
    flex-grow: 1;
    font-size: 18px;
    color: rgb(255, 255, 255);
    -unity-font-style: bold;
    -unity-text-align: middle-center;
    display: none;
}
```

---

## New Assets

| Asset | Type | Notes |
|-------|------|-------|
| `PowerupMessageEvent.asset` | StringEvent | One shared event for all effects |

---

## Inspector Wiring

### Every effect SO (GainHeartEffect, GainScoreEffect, ClearOrbsEffect, BalanceColorEffect, ExpandPlayerEffect, ShrinkPlayerEffect, SpawnBurstEffect, FreezeOrbsEffect, ExpandPlayAreaEffect, SwitchOrbColorsEffect)

| Field | Value |
|-------|-------|
| Powerup Message Event | `PowerupMessageEvent.asset` |
| Message | *(see message table above)* |

### PowerupNotificationUI (component on the HUD GameObject)

| Field | Value |
|-------|-------|
| Powerup Message Event | `PowerupMessageEvent.asset` |
| Game Start Event | `GameStartEvent.asset` |
| Game Paused Event | `GamePausedEvent.asset` |
| Display Duration | `2` |

---

## Verify

| Test | Expected |
|------|----------|
| Pick up any powerup | Correct message appears centered in top bar |
| After 2s | Label hides automatically |
| Pick up second powerup before first fades | Timer resets, new message shows instantly |
| Restart during message | Label hidden immediately |
| Pause during message | Label hidden immediately |
| No powerup picked up | Label never visible |
