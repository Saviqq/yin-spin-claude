# Step 7 — Score System

Goal: Score increments on every correct orb collection, displayed live in the top bar (right side). Final score shown on the game over screen. Resets on restart. Built entirely on existing `IntegerValue` and `GameEvent` patterns.

---

## What Reuses Existing Systems

| Existing | Reused as |
|----------|-----------|
| `IntegerValue.cs` | `ScoreData.asset` — same SO, Value = 0 |
| `HealthUI.cs` pattern | `ScoreUI.cs` — same subscribe/update pattern |
| `score-container` in UXML | Already a placeholder — just add content |
| `GameManager.OnGameStart` | Add `scoreData.Reset()` |
| `GameOverUI.OnGameOver` | Add final score label update |

No new architecture — score slots into what's already there.

---

## Files Created / Changed

| File | Change |
|------|--------|
| `ScoreData.asset` | New — IntegerValue instance, Value = 0 |
| `ScoreUI.cs` | New — live score label in top bar |
| `PlayerController.cs` | Swap local `score` field for `scoreData` SO |
| `GameManager.cs` | Add `scoreData.Reset()` on game start |
| `GameOverUI.cs` | Wire final score label on game over |
| `HUD.uxml` | Add score label to top bar + final score to game over panel |
| `HUD.uss` | Add score label styles |

---

## 1. Create ScoreData Asset

Right-click in `Assets/ScriptableObjects/` → **Create → Scriptable Objects → IntegerValue**.
Name it `ScoreData`. Set **Value** to `0` in the Inspector.

---

## 2. ScoreUI.cs

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class ScoreUI : MonoBehaviour
{
    [SerializeField] private IntegerValue scoreData;

    private Label scoreLabel;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        scoreLabel = root.Q<Label>("score-label");

        scoreData.OnChange += OnScoreChange;
        OnScoreChange(scoreData.Current);
    }

    void OnDisable()
    {
        scoreData.OnChange -= OnScoreChange;
    }

    private void OnScoreChange(int score)
    {
        scoreLabel.text = score.ToString();
    }
}
```

Add this script to the **HUD GameObject** alongside `HealthUI` and `GameOverUI`. Assign `ScoreData` asset in the Inspector.

---

## 3. Update PlayerController.cs

**Add field** next to the `health` field:
```csharp
[SerializeField] private IntegerValue scoreData;
```

**Replace** the local score field and increment:
```csharp
// Remove these:
private int score = 0;
// ...
score++;
Debug.Log($"Collected! Score: {score} | ColorRatio: {colorRatio:F2}");

// Replace with:
scoreData.Set(scoreData.Current + 1);
```

**Remove** `GetScore()` from the public API section — score is now read directly from the SO.

The correct-collection branch in `OnTriggerEnter2D` becomes:
```csharp
if (orb.IsWhite == hitWhiteHalf)
{
    colorRatio += orb.IsWhite ? collectDelta : -collectDelta;
    colorRatio = Mathf.Clamp01(colorRatio);
    splitMaterial.SetFloat("_ColorRatio", colorRatio);
    scoreData.Set(scoreData.Current + 1);
}
```

Assign `ScoreData` asset to the Player's Inspector slot.

---

## 4. Update GameManager.cs

Add field:
```csharp
[SerializeField] private IntegerValue scoreData;
```

Add `scoreData.Reset()` to `OnGameStart`:
```csharp
private void OnGameStart()
{
    health.Reset();
    scoreData.Reset();
    Cursor.visible = false;
}
```

`scoreData.Reset()` fires `OnChange(0)` → `ScoreUI` and the game over final score label both update immediately.

Assign `ScoreData` to the GameManager Inspector slot.

---

## 5. Update GameOverUI.cs

Add field:
```csharp
[SerializeField] private IntegerValue scoreData;
```

Add a reference to the final score label:
```csharp
private Label finalScoreLabel;
```

In `OnEnable`, query the label:
```csharp
finalScoreLabel = root.Q<Label>("final-score-label");
```

Update `OnGameOver` to write the final score:
```csharp
private void OnGameOver()
{
    finalScoreLabel.text = $"SCORE  {scoreData.Current}";
    overlay.style.display = DisplayStyle.Flex;
}
```

Assign `ScoreData` to the HUD GameObject's `GameOverUI` Inspector slot.

---

## 6. Update HUD.uxml

Two additions:

**A — Score label in the top bar** (fill the existing `score-container` placeholder):
```xml
<ui:VisualElement name="score-container" class="hud-section">
    <ui:Label name="score-label" class="score-label" text="0" />
</ui:VisualElement>
```

**B — Final score label in the game over panel** (add between title and buttons):
```xml
<ui:VisualElement name="game-over-panel" class="game-over-panel">
    <ui:Label text="GAME OVER" class="game-over-title" />
    <ui:Label name="final-score-label" class="final-score-label" text="SCORE  0" />
    <ui:Button name="restart-btn" text="RESTART" class="game-btn" />
    <ui:Button name="exit-btn" text="EXIT" class="game-btn" />
</ui:VisualElement>
```

---

## 7. Update HUD.uss

Append to the existing stylesheet:

```css
/* ── Score label (top bar, right side) ───────────────── */
.score-label {
    font-size: 28px;
    color: rgb(255, 255, 255);
    -unity-text-align: middle-right;
    -unity-font-style: bold;
}

/* ── Final score (game over panel) ───────────────────── */
.final-score-label {
    font-size: 24px;
    color: rgb(255, 255, 255);
    -unity-text-align: middle-center;
    margin-bottom: 24px;
    letter-spacing: 2px;
}
```

---

## Verify It Works

| Test | Expected |
|------|----------|
| Correct orb collected | Score label in top bar increments by 1 |
| Wrong orb collected | Score does not change |
| Game over triggered | Game over panel shows `SCORE  X` with the final score |
| Restart clicked | Score resets to `0` in top bar, final score label resets too |
| Multiple rounds | Score always starts from 0 each round |

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | Shader + rotation |
| `SETUP_STEP3.md` | Orb spawning + bouncing |
| `SETUP_STEP4.md` | Collection mechanic + ratio shifting |
| `SETUP_STEP5.md` | IntegerValue SO + HUD + camera viewport fix |
| `SETUP_STEP6.md` | Game over, restart, SO event architecture |
| `SETUP_STEP7.md` | This file — score system |
| `SETUP_STEP8.md` | _(next) Difficulty scaling_ |
