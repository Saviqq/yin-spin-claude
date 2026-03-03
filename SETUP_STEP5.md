# Step 5 — Health System & HUD (UI Toolkit)

Goal: 3-heart health system using a reusable `IntegerValue` ScriptableObject, displayed in a top bar HUD via UI Toolkit. Wrong-color orb contact decrements health. Also covers the camera viewport fix required to keep orb bounds aligned with the gameplay area below the HUD.

---

## Architecture

```
PlayerController ──Health.Set()──► IntegerValue ──OnChange──► HealthUI
```

- `IntegerValue` is a generic ScriptableObject — holds any integer value with a reset and a change event
- Reusable: the same class will be used for score later
- `PlayerController` writes → `HealthUI` reads and listens
- Neither knows about the other — `IntegerValue` is the only coupling point

---

## Files Created

| File | Type |
|------|------|
| `IntegerValue.cs` | Reusable ScriptableObject |
| `HealthData.asset` | IntegerValue instance for health (Value = 3) |
| `HUD.uxml` | UI Toolkit layout |
| `HUD.uss` | UI Toolkit styles |
| `HealthUI.cs` | MonoBehaviour on UIDocument GameObject |

Folder layout:
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

## 1. IntegerValue.cs

A generic reusable ScriptableObject for any tracked integer (health, score, etc.).

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "IntegerValue", menuName = "Scriptable Objects/IntegerValue")]
public class IntegerValue : ScriptableObject
{
    [SerializeField] private int Value;

    public int Current { get; private set; }

    public event Action<int> OnChange;

    void OnEnable()
    {
        Current = Value;
    }

    public void Set(int value)
    {
        Current = value;
        OnChange?.Invoke(Current);
    }
}
```

**Key design:**
- `Value` (serialized) — the initial/reset value, set in the Inspector on the asset
- `Current` (runtime, private set) — the live value during play
- `OnEnable()` resets `Current = Value` each time Play mode starts
- `OnChange` passes the new value directly (`Action<int>`) — subscribers get the value without needing to read it back from the SO
- `Set()` is the only write path — always fires the event, keeping UI in sync

**Reuse for score:** Create a second `IntegerValue` asset (e.g. `ScoreData.asset`, `Value = 0`) and wire it up the same way.

---

## 2. Create the HealthData Asset

Right-click in `Assets/ScriptableObjects/` → **Create → Scriptable Objects → IntegerValue**.
Name it `HealthData`. In the Inspector set **Value** to `3`.

---

## 3. HUD.uxml

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="HUD.uss" />
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

---

## 4. HUD.uss

```css
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

.hud-section {
    flex-direction: row;
    align-items: center;
}

.heart {
    font-size: 28px;
    color: rgb(255, 255, 255);
    margin-right: 6px;
    -unity-text-align: middle-center;
}

.heart--lost {
    color: rgba(255, 255, 255, 0.2);
}
```

---

## 5. Panel Settings & HUD GameObject

If no PanelSettings asset exists:
Right-click in Project → **Create → UI Toolkit → Panel Settings Asset**, name it `GamePanelSettings`.
- **Scale Mode:** `Scale With Screen Size`
- **Reference Resolution:** `1920 x 1080`

In the Hierarchy: **Create Empty** → name it `HUD`.
- Add **UI Document** component → assign `GamePanelSettings` and `HUD.uxml`
- Add **HealthUI** script (below) → assign `HealthData` asset

---

## 6. HealthUI.cs

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class HealthUI : MonoBehaviour
{
    [SerializeField] private IntegerValue Health;

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

        Health.OnChange += OnHealthChange;
        OnHealthChange(Health.Current);
    }

    void OnDisable()
    {
        Health.OnChange -= OnHealthChange;
    }

    private void OnHealthChange(int currentHealth)
    {
        for (int i = 0; i < heartLabels.Length; i++)
        {
            bool filled = i < currentHealth;
            heartLabels[i].EnableInClassList("heart--lost", !filled);
        }
    }
}
```

`OnHealthChange` receives the new value directly from the `Action<int>` event — no need to re-read from the SO.

---

## 7. PlayerController Changes

Add the `Health` field:
```csharp
[SerializeField] private IntegerValue Health;
```

In `OnTriggerEnter2D`, wrong-color branch:
```csharp
if (Health.Current > 0)
    Health.Set(Health.Current - 1);
```

Assign `HealthData` asset to the `Health` slot on the Player Inspector.

---

## 8. Camera Viewport Fix — Gameplay Area Bounds

### The Problem
The top bar takes 10% of screen height. `Orb.cs` and `OrbSpawner.cs` compute bounds using `Camera.main.orthographicSize`, which by default covers the **full screen**. This means:
- Orbs spawn and bounce at the very top of the screen, behind the UI bar
- The gameplay area is actually only 90% of the screen height

### The Fix — Camera Viewport Rect

**No code changes needed.** Unity's camera `Viewport Rect` restricts the camera's rendered region to a portion of the screen. When set to 90% height, `Camera.main.orthographicSize` and `Camera.main.aspect` automatically reflect the gameplay area dimensions — all existing bounds calculations in `Orb.cs`, `OrbSpawner.cs`, and `PlayerController.cs` become correct with zero changes.

**Steps:**
1. Select **Main Camera** in the Hierarchy
2. In the Inspector → **Camera** component → **Viewport Rect**:

| Field | Value |
|-------|-------|
| X | `0` |
| Y | `0` |
| W | `1` |
| H | `0.9` |

The camera now renders from the bottom of the screen up to 90% height. The top 10% is covered by the UI Toolkit HUD.

### Why This Works

`Camera.main.orthographicSize` is the world-space half-height of the camera's **rendered viewport**, not the screen. When viewport H = 0.9:

- `halfHeight = Camera.main.orthographicSize` → world-space top/bottom of gameplay area ✅
- `halfWidth = halfHeight * Camera.main.aspect` → `aspect` also updates to reflect the viewport proportions ✅
- Top spawn edge: `halfHeight + spawnMargin` → just above the gameplay area, behind the UI bar ✅
- Top bounce wall: `halfHeight - orbRadius` → bounces at the bottom of the UI bar ✅

### Note on Aspect Ratio
With viewport H=0.9, the gameplay area is slightly wider in world units than a full-screen 16:9 camera at the same orthographic size (same width, shorter height = wider aspect ratio). This is correct and expected — adjust orthographic size in the Camera Inspector if the gameplay area feels too wide.

---

## Verify It Works

| Test | Expected |
|------|----------|
| Top bar visible | Black strip, white border, 3 ♥ top-left |
| Wrong-color contact | One heart dims |
| 3 wrong contacts | All hearts dimmed |
| Re-enter Play | All 3 hearts restored |
| Orbs near top of screen | Bounce off the bottom of the UI bar, not off screen top |
| Orbs spawn from top edge | Appear from behind UI bar, not from screen top |

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | Shader + rotation |
| `SETUP_STEP3.md` | Orb spawning + bouncing |
| `SETUP_STEP4.md` | Collection mechanic + ratio shifting |
| `SETUP_STEP5.md` | This file — IntegerValue SO + HUD + camera viewport fix |
| `SETUP_STEP6.md` | _(next) Score, game over, difficulty_ |
