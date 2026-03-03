# Step 5 — Health System & HUD (UI Toolkit)

Goal: 3-heart health system backed by a ScriptableObject, displayed in a top bar HUD using UI Toolkit. Wrong-color orb contact calls `TakeDamage()` on the SO, which fires an event the UI listens to. Score display slot is wired up but left empty — filled in Step 6.

---

## Architecture

```
PlayerController ──TakeDamage()──► HealthSO ──onHealthChanged──► HealthUI
                                       │
                                  onGameOver  (Step 6)
```

- `HealthSO` is a ScriptableObject **asset** — shared reference, no scene dependency
- `PlayerController` holds a reference → writes (TakeDamage)
- `HealthUI` holds a reference → reads (CurrentHearts) and listens (event)
- Neither system knows about the other — HealthSO is the only coupling point

---

## Files to Create

| File | Type |
|------|------|
| `HealthSO.cs` | ScriptableObject script |
| `HealthData.asset` | HealthSO instance (created in editor) |
| `HUD.uxml` | UI Toolkit layout |
| `HUD.uss` | UI Toolkit styles |
| `HealthUI.cs` | MonoBehaviour on the UIDocument GameObject |

Plus updates to `PlayerController.cs`.

Suggested folder layout (create if missing):
```
Assets/
  Scripts/
  UI/
    HUD.uxml
    HUD.uss
  ScriptableObjects/
    HealthData.asset
```

---

## 1. HealthSO.cs

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "HealthData", menuName = "Game/Health")]
public class HealthSO : ScriptableObject
{
    [SerializeField] private int maxHearts = 3;

    // Runtime-only — not serialized, resets each play session via OnEnable
    private int currentHearts;

    public int CurrentHearts => currentHearts;
    public int MaxHearts     => maxHearts;

    // Subscribers re-register each session from their own Start/OnEnable
    public event Action OnHealthChanged;
    public event Action OnGameOver;

    // Called automatically when the SO is loaded (entering Play mode)
    void OnEnable()
    {
        currentHearts = maxHearts;
    }

    public void TakeDamage()
    {
        if (currentHearts <= 0) return;

        currentHearts--;
        OnHealthChanged?.Invoke();

        if (currentHearts <= 0)
            OnGameOver?.Invoke();
    }
}
```

**Why `OnEnable` for reset:**
ScriptableObjects persist in memory across play sessions in the Editor. `OnEnable` fires each time the asset is loaded into memory, which includes entering Play mode. This guarantees `currentHearts` always starts at `maxHearts` — no manual initialization call needed.

> **Note:** If your project has **Enter Play Mode Settings → Domain Reload disabled** (Project Settings → Editor), `OnEnable` may not fire. If you see hearts not resetting, go to Edit → Project Settings → Editor and ensure `Reload Domain` is checked.

---

## 2. Create the HealthData Asset

In the Project window: right-click → **Create → Game → Health**.
Name it `HealthData`, save it inside `Assets/ScriptableObjects/`.

This asset is what both `PlayerController` and `HealthUI` will reference in the Inspector.

---

## 3. HUD.uxml

In the Project window: right-click inside `Assets/UI/` → **Create → UI Toolkit → UI Document**. Name it `HUD`.

Delete all default content and paste:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <ui:VisualElement name="top-bar" class="top-bar">

        <ui:VisualElement name="hearts-container" class="hud-section">
            <ui:Label name="heart-0" class="heart" text="♥" />
            <ui:Label name="heart-1" class="heart" text="♥" />
            <ui:Label name="heart-2" class="heart" text="♥" />
        </ui:VisualElement>

        <!-- Score section — added in Step 6 -->
        <ui:VisualElement name="score-container" class="hud-section" />

    </ui:VisualElement>
</ui:UXML>
```

Hearts are indexed 0–2 to match `CurrentHearts` comparisons in code (`i < currentHearts`).

---

## 4. HUD.uss

Right-click inside `Assets/UI/` → **Create → UI Toolkit → Style Sheet**. Name it `HUD`.

```css
/* ── Top bar ─────────────────────────────────────────── */
.top-bar {
    width: 100%;
    height: 10%;
    background-color: rgb(0, 0, 0);
    flex-direction: row;
    align-items: center;
    justify-content: space-between;
    padding-left: 24px;
    padding-right: 24px;
    border-bottom-width: 2px;
    border-bottom-color: rgb(255, 255, 255);
}

/* ── Section wrapper (hearts left, score right) ──────── */
.hud-section {
    flex-direction: row;
    align-items: center;
}

/* ── Individual heart label ──────────────────────────── */
.heart {
    font-size: 28px;
    color: rgb(255, 255, 255);
    margin-right: 6px;
    -unity-text-align: middle-center;
}

/* Lost heart — same symbol, dimmed */
.heart--lost {
    color: rgba(255, 255, 255, 0.2);
}
```

`justify-content: space-between` on `.top-bar` means hearts sit at the left edge and the (future) score sits at the right edge automatically.

---

## 5. Create Panel Settings (if none exists)

In the Project window: right-click → **Create → UI Toolkit → Panel Settings Asset**. Name it `GamePanelSettings`.

In the Inspector:
- **Scale Mode:** `Scale With Screen Size`
- **Reference Resolution:** `1920 x 1080`
- **Screen Match Mode:** `Match Width Or Height`, slider at `0.5`

---

## 6. Create the HUD GameObject

In the Hierarchy: right-click → **Create Empty**, name it `HUD`.

Add **UI Document** component:
- **Panel Settings:** assign `GamePanelSettings`
- **Source Asset:** assign `HUD.uxml`

Then add the `HealthUI` script (created next) to the same GameObject.

---

## 7. HealthUI.cs

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private HealthSO healthData;

    private Label[] heartLabels;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        heartLabels = new Label[]
        {
            root.Q<Label>("heart-0"),
            root.Q<Label>("heart-1"),
            root.Q<Label>("heart-2"),
        };

        healthData.OnHealthChanged += RefreshHearts;
        RefreshHearts();
    }

    void OnDisable()
    {
        healthData.OnHealthChanged -= RefreshHearts;
    }

    void RefreshHearts()
    {
        for (int i = 0; i < heartLabels.Length; i++)
        {
            bool filled = i < healthData.CurrentHearts;
            heartLabels[i].EnableInClassList("heart--lost", !filled);
        }
    }
}
```

**Notes:**
- `OnEnable` / `OnDisable` for subscribe/unsubscribe — this is the correct pattern for event cleanup. Avoids stale subscribers if the GameObject is disabled and re-enabled.
- `EnableInClassList("heart--lost", !filled)` toggles the dim style class. The ♥ symbol stays the same — only opacity changes.
- `root.Q<Label>("heart-0")` queries the UXML by the `name` attribute.

---

## 8. Assign the USS to the UXML

Open `HUD.uxml` in the **UI Builder** (double-click it):
- In the StyleSheets panel (top left), click **+** → **Add Existing USS** → select `HUD.uss`
- Save

Alternatively, add this line at the top of `HUD.uxml` manually:
```xml
<Style src="HUD.uss" />
```
Place it as the first child of the root `<ui:UXML>` element.

---

## 9. Update PlayerController.cs

Three small changes only:

**Add field** (with the other SerializeFields at the top):
```csharp
[SerializeField] private HealthSO healthData;
```

**Replace the wrong-color `Debug.Log`** in `OnTriggerEnter2D`:
```csharp
// Was:
Debug.Log($"Wrong color! ...");

// Now:
healthData.TakeDamage();
```

**Assign in Inspector:** select the `Player` GameObject → drag `HealthData` asset into the `Health Data` slot.

Do the same for the `HUD` GameObject's `HealthUI` component.

---

## 10. Verify It Works

Hit **Play**.

| Test | Expected |
|------|----------|
| Top bar visible | Black strip with white bottom border, 3 white ♥ across the top-left |
| Wrong-color orb contact | One heart dims immediately |
| Second wrong contact | Two hearts dimmed |
| Third wrong contact | All three dimmed — `OnGameOver` fires (no response yet, Step 6) |
| Re-enter Play mode | All 3 hearts restored (OnEnable reset) |

Open the **UI Debugger** (`Window → UI Toolkit → Debugger`) and select the HUD panel if the layout looks off — it lets you inspect the element tree and live-edit styles.

---

## What's Deliberately Left Out of This Step
- Game over screen / restart (Step 6)
- Score display (Step 6)
- Difficulty ramp / spawn rate scaling (Step 6)

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | Shader + rotation |
| `SETUP_STEP3.md` | Orb spawning + bouncing |
| `SETUP_STEP4.md` | Collection mechanic + ratio shifting |
| `SETUP_STEP5.md` | This file — health SO + HUD |
| `SETUP_STEP6.md` | _(next) Score, game over, difficulty_ |
