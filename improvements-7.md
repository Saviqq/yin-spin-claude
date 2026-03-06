# Implementation Step 7 — Top Bar UI Rework

## Scope

| File | Change |
|------|--------|
| `HUD.uxml` | Hearts: 3 Labels → 5 VisualElements. Score: Label → VisualElement (star) wrapping a Label |
| `HUD.uss` | Replace heart label styles with sprite-based styles; add score-icon styles; remove old score-label |
| `HealthUI.cs` | `Label[3]` → `VisualElement[5]`; toggle `heart--filled` / `heart--lost` classes |
| `ScoreUI.cs` | No changes — `Q<Label>("score-label")` finds the nested label unchanged |
| `Health` SO asset | `DefaultValue` stays `3` — resets to 3 on restart |

Sprites to create (32×32 pixel art, imported as Sprite, Point filter, 32 PPU):
- `heart_filled.png` — mono white
- `heart_lost.png` — black with white border
- `star.png` — mono white

---

## Health — how 5 slots work

Always render all 5 slots. `health.Value` is current health (starts at 3, max 5).

| Slot index | State at start | State after powerup (+1 heart) |
|------------|---------------|-------------------------------|
| 0, 1, 2 | `heart--filled` | `heart--filled` |
| 3 | `heart--lost` | `heart--filled` |
| 4 | `heart--lost` | `heart--lost` |

`i < health.Value` → filled, otherwise → lost. No `maxHealth` concept needed yet — powerups simply call `health.Set(health.Value + 1)` clamped to 5.

---

## `HealthUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntegerValue health;

    private VisualElement[] heartElements;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        heartElements = new VisualElement[]
        {
            root.Q<VisualElement>("heart-0"),
            root.Q<VisualElement>("heart-1"),
            root.Q<VisualElement>("heart-2"),
            root.Q<VisualElement>("heart-3"),
            root.Q<VisualElement>("heart-4"),
        };

        health.OnChange += OnHealthChange;
        OnHealthChange(health.Value);
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChange;
    }

    private void OnHealthChange(int currentHealth)
    {
        for (int i = 0; i < heartElements.Length; i++)
        {
            bool filled = i < currentHealth;
            heartElements[i].EnableInClassList("heart--filled", filled);
            heartElements[i].EnableInClassList("heart--lost", !filled);
        }
    }
}
```

Both classes always explicitly set so there is never ambiguity about which sprite is showing.

---

## HUD.uxml

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements" editor-extension-mode="False">
    <Style src="project://database/Assets/UI/HUD.uss?..." />

    <ui:VisualElement name="top-bar" class="top-bar">

        <ui:VisualElement name="hearts-container" class="hud-section">
            <ui:VisualElement name="heart-0" class="heart" />
            <ui:VisualElement name="heart-1" class="heart" />
            <ui:VisualElement name="heart-2" class="heart" />
            <ui:VisualElement name="heart-3" class="heart" />
            <ui:VisualElement name="heart-4" class="heart" />
        </ui:VisualElement>

        <ui:VisualElement name="score-container" class="hud-section">
            <ui:VisualElement name="score-icon" class="score-icon">
                <ui:Label name="score-label" class="score-value" text="0" />
            </ui:VisualElement>
        </ui:VisualElement>

    </ui:VisualElement>

    <!-- game-over-overlay, pause-overlay, footer-bar unchanged -->
</ui:UXML>
```

Wire sprites inline after creating the assets (same pattern as footer keys):
```xml
<ui:VisualElement name="heart-0" class="heart heart--filled"
    style="background-image: url('...heart_filled.png');" />
```

Wait — sprites for hearts are **dynamic** (toggled by HealthUI), so do **not** wire them inline in UXML. Wire them in USS instead using the class selectors below.

The star sprite is static, so wire it inline on `score-icon`:
```xml
<ui:VisualElement name="score-icon" class="score-icon"
    style="background-image: url('project://database/Assets/Art/star.png?...');">
    <ui:Label name="score-label" class="score-value" text="0" />
</ui:VisualElement>
```

---

## HUD.uss

Remove old `.heart` and `.heart--lost` label rules. Remove `.score-label`. Add:

```css
/* ── Hearts ─────────────────────────────────────────── */

.heart {
    width: 48px;
    height: 48px;
    margin-right: 4px;
    -unity-background-scale-mode: scale-to-fit;
}

.heart--filled {
    background-image: url("project://database/Assets/Art/heart_filled.png?fileID=...&guid=...&type=3#heart_filled");
}

.heart--lost {
    background-image: url("project://database/Assets/Art/heart_lost.png?fileID=...&guid=...&type=3#heart_lost");
}

/* ── Score icon ──────────────────────────────────────── */

.score-icon {
    width: 64px;
    height: 64px;
    -unity-background-scale-mode: scale-to-fit;
    align-items: center;
    justify-content: center;
}

.score-value {
    font-size: 20px;
    color: rgb(0, 0, 0);
    -unity-text-align: middle-center;
    -unity-font-style: bold;
    position: absolute;
}
```

**Getting the correct `background-image` URL:** After importing the sprite in Unity, open `HUD.uss` in the UI Builder or a text editor and drag the sprite asset into the field — Unity fills in the full `project://database/...` URL with the correct GUID automatically. Alternatively copy the URL pattern from the existing footer key sprites in `HUD.uxml`.

---

## What does not change

- `ScoreUI.cs` — `Q<Label>("score-label")` searches the full tree and still finds the nested label. No changes needed.
- `Health` SO — `DefaultValue = 3`, resets to 3 on restart as before.
- `GameManager`, `Player`, `DifficultyManager`, `OrbManager` — untouched.

---

## Verify

| Test | Expected |
|------|----------|
| Start | 3 white heart sprites, 2 black-bordered heart sprites, star with `0` in center |
| Wrong-color hit | One filled heart becomes lost sprite |
| 3 hits | All 3 filled become lost; slots 3–4 already lost |
| Restart | Back to 3 filled, 2 lost |
| Score increments | Number inside star updates |
