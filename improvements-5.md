# Implementation Step 5 — Footer Controls Hint Bar

## Scope

One new UXML section, one USS block, one camera viewport change. No new C# scripts.

| Asset | Change |
|-------|--------|
| `HUD.uxml` | Add `footer-bar` below the existing `top-bar` |
| `HUD.uss` | Add footer styles + hint-group / key-icon classes |
| Main Camera | Viewport Rect `Y: 0 → 0.1`, `H: 0.9 → 0.8` |
| Pixel art key sprites | 16×16 px sprites for each key (W, A, S, D, ←, →, ESC) — created in your art tool, imported as Sprite |

---

## Layout

```
┌──────────────────────────────────────────────────────┐  10% top bar
│  ♥ ♥ ♥                              SCORE: 0         │
├──────────────────────────────────────────────────────┤
│                                                      │
│                  80% gameplay                        │
│                                                      │
├──────────────────────────────────────────────────────┤  10% footer
│  [W][A][S][D] Movement   [←][→] Rotate   [ESC] Pause │
└──────────────────────────────────────────────────────┘
```

Three hint groups, `justify-content: space-around` on the footer row.

Each hint group: `[key-icon(s)]  label-text` side by side.

---

## Camera Viewport Change

The footer takes the bottom 10%. Shift the camera up:

| Field | Was | Now |
|-------|-----|-----|
| Y | `0` | `0.1` |
| H | `0.9` | `0.8` |

`GameManager.OnGameStart` seeds `halfHeightPlayArea` and `halfWidthPlayArea` from `Camera.main.orthographicSize` and `Camera.main.aspect` — these automatically reflect the new viewport with no code changes.

---

## Sprite Import Settings

For each 16×16 key sprite:
- **Texture Type:** Sprite (2D and UI)
- **Pixels Per Unit:** 16
- **Filter Mode:** Point (no filter) — keeps pixel art crisp
- **Compression:** None

---

## HUD.uxml

Add the `footer-bar` block after the closing `</ui:VisualElement>` of `top-bar`:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="HUD.uss" />

    <ui:VisualElement name="top-bar" class="top-bar">
        <ui:VisualElement name="hearts-container" class="hud-section">
            <ui:Label name="heart-0" class="heart" text="♥" />
            <ui:Label name="heart-1" class="heart" text="♥" />
            <ui:Label name="heart-2" class="heart" text="♥" />
        </ui:VisualElement>
        <ui:VisualElement name="score-container" class="hud-section">
            <ui:Label name="score-label" class="score-label" text="0" />
        </ui:VisualElement>
    </ui:VisualElement>

    <ui:VisualElement name="footer-bar" class="footer-bar">

        <!-- Movement: W A S D -->
        <ui:VisualElement class="hint-group">
            <ui:VisualElement name="key-w" class="key-icon" />
            <ui:VisualElement name="key-a" class="key-icon" />
            <ui:VisualElement name="key-s" class="key-icon" />
            <ui:VisualElement name="key-d" class="key-icon" />
            <ui:Label class="hint-label" text="Move" />
        </ui:VisualElement>

        <!-- Rotate: ← → -->
        <ui:VisualElement class="hint-group">
            <ui:VisualElement name="key-left" class="key-icon" />
            <ui:VisualElement name="key-right" class="key-icon" />
            <ui:Label class="hint-label" text="Rotate" />
        </ui:VisualElement>

        <!-- Pause: ESC -->
        <ui:VisualElement class="hint-group">
            <ui:VisualElement name="key-esc" class="key-icon" />
            <ui:Label class="hint-label" text="Pause" />
        </ui:VisualElement>

    </ui:VisualElement>
</ui:UXML>
```

**Why `VisualElement` not `Image` for key icons:** `VisualElement` with `background-image` is the standard UI Toolkit approach. Assign the sprite via USS `background-image: url("...")` or in C# via `style.backgroundImage` — both work; Option B below uses C# for easier Inspector drag-and-drop.

---

## HUD.uss

Append to the existing file:

```css
/* ── Footer ─────────────────────────────────────────── */

.footer-bar {
    position: absolute;
    bottom: 0;
    left: 0;
    width: 100%;
    height: 10%;
    background-color: rgb(0, 0, 0);
    border-top-width: 2px;
    border-top-color: rgb(255, 255, 255);
    flex-direction: row;
    align-items: center;
    justify-content: space-around;
}

/* One action group: keys + label side by side */
.hint-group {
    flex-direction: row;
    align-items: center;
}

/* Single key icon — square, 24×24 display size (scaled from 16px source) */
.key-icon {
    width: 24px;
    height: 24px;
    margin: 2px;
    -unity-background-scale-mode: scale-to-fit;
}

.hint-label {
    font-size: 14px;
    color: rgb(255, 255, 255);
    -unity-text-align: middle-left;
}
```

**`position: absolute; bottom: 0`:** The root VisualElement in a UIDocument is a full-screen flex column. `top-bar` takes 10% at the top naturally. Without absolute positioning the footer would sit right below it (in the gameplay area). Absolute + `bottom: 0` pins it to the screen bottom without affecting the camera-backed gameplay area.

---

## Wiring Sprites in the Inspector

After importing your 16×16 sprites, assign them via the Unity Inspector on the UIDocument's `VisualTreeAsset`:

There are two ways — pick one:

**Option A — USS (no C# needed):**
In `HUD.uss`, add per-key background-image rules using the asset path:
```css
#key-w     { background-image: url("project://database/Assets/UI/Keys/key_w.png"); }
#key-a     { background-image: url("project://database/Assets/UI/Keys/key_a.png"); }
#key-s     { background-image: url("project://database/Assets/UI/Keys/key_s.png"); }
#key-d     { background-image: url("project://database/Assets/UI/Keys/key_d.png"); }
#key-left  { background-image: url("project://database/Assets/UI/Keys/key_left.png"); }
#key-right { background-image: url("project://database/Assets/UI/Keys/key_right.png"); }
#key-esc   { background-image: url("project://database/Assets/UI/Keys/key_esc.png"); }
```

**Option B — C# (HintBarUI.cs):**
Create a minimal MonoBehaviour on the HUD GameObject if you want Inspector-assigned sprite slots:

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class HintBarUI : MonoBehaviour
{
    [Header("Key Sprites")]
    [SerializeField] private Sprite keyW;
    [SerializeField] private Sprite keyA;
    [SerializeField] private Sprite keyS;
    [SerializeField] private Sprite keyD;
    [SerializeField] private Sprite keyLeft;
    [SerializeField] private Sprite keyRight;
    [SerializeField] private Sprite keyEsc;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        SetSprite(root, "key-w",     keyW);
        SetSprite(root, "key-a",     keyA);
        SetSprite(root, "key-s",     keyS);
        SetSprite(root, "key-d",     keyD);
        SetSprite(root, "key-left",  keyLeft);
        SetSprite(root, "key-right", keyRight);
        SetSprite(root, "key-esc",   keyEsc);
    }

    private void SetSprite(VisualElement root, string elementName, Sprite sprite)
    {
        if (sprite == null) return;
        var el = root.Q<VisualElement>(elementName);
        if (el != null)
            el.style.backgroundImage = new StyleBackground(sprite);
    }
}
```

Add `HintBarUI` to the `HUD` GameObject alongside the existing `HealthUI` / `ScoreUI` components. Assign each sprite in the Inspector.

**Recommendation:** Option B — drag-and-drop in Inspector is faster to iterate when swapping pixel art.

---

## Inspector Wiring Summary

| Component | Field | Value |
|-----------|-------|-------|
| Main Camera → Viewport Rect | Y | `0.1` |
| Main Camera → Viewport Rect | H | `0.8` |
| HUD GameObject | `HintBarUI` component | (if using Option B) |
| HintBarUI | Key W | `key_w` sprite asset |
| HintBarUI | Key A | `key_a` sprite asset |
| HintBarUI | Key S | `key_s` sprite asset |
| HintBarUI | Key D | `key_d` sprite asset |
| HintBarUI | Key Left | `key_left` sprite asset |
| HintBarUI | Key Right | `key_right` sprite asset |
| HintBarUI | Key Esc | `key_esc` sprite asset |

---

## Verify

| Test | Expected |
|------|----------|
| Footer visible | Black strip at bottom, white top border, 3 groups centered with space-around |
| Key icons | 24×24 px pixel art, crisp (no blur) |
| ESC key | Wider than the others |
| Gameplay area | Orbs spawn/bounce within the 80% middle — no overlap with top or bottom bar |
| Game restart | Footer is static — no changes on restart (no script hooks needed) |
