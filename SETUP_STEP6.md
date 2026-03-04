# Step 6 — Game Over & Restart (SO Event Architecture)

Goal: When health reaches 0, fire `GameOverEvent` — player stops, spawner stops, game over screen appears. Restart fires `GameStartEvent` — all systems reset themselves independently, orbs self-destruct, clean slate. No scene reload, no direct cross-system calls.

---

## Full Architecture

```
IntegerValue.OnChange (value == 0)
    → GameManager → GameOverEvent.Raise()

GameOverEvent ──► PlayerController   disable input
              ──► OrbSpawner         stop spawning
              ──► GameOverUI         show overlay

Restart button → GameStartEvent.Raise()

GameStartEvent ──► GameManager       health.Reset()
               ──► PlayerController  re-enable input
               ──► OrbSpawner        resume spawning, reset timer
               ──► GameOverUI        hide overlay
               ──► every live Orb    Destroy(gameObject)
```

`GameManager` is the only component that knows about both SOs — it's thin wiring only, no game logic.

---

## Files Created / Changed

| File | Change |
|------|--------|
| `GameEvent.cs` | New — reusable SO event |
| `GameOverEvent.asset` | New — instance |
| `GameStartEvent.asset` | New — instance |
| `GameManager.cs` | New — wiring between health, GameOver, GameStart |
| `GameOverUI.cs` | New — on HUD GameObject, handles overlay + buttons |
| `IntegerValue.cs` | Add `Reset()` method |
| `PlayerController.cs` | Subscribe to events, add `isActive` gate |
| `OrbSpawner.cs` | Subscribe to events, add `isSpawning` gate |
| `Orb.cs` | Subscribe to GameStartEvent, self-destruct |
| `HUD.uxml` | Add game-over overlay element |
| `HUD.uss` | Add game-over styles |

---

## 1. GameEvent.cs

```csharp
using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GameEvent", menuName = "Scriptable Objects/GameEvent")]
public class GameEvent : ScriptableObject
{
    public event Action OnRaised;

    public void Raise()
    {
        OnRaised?.Invoke();
    }
}
```

> Note: the class is named `GameEvent` (not `GameEventSO`). All references in `PlayerController`, `OrbSpawner`, `Orb`, `GameManager`, and `GameOverUI` use `GameEvent`.

Simple signal — no data, no logic. `Raise()` fires `OnRaised`. Subscribers register and unregister themselves.

---

## 2. Create the Event Assets

Right-click in `Assets/ScriptableObjects/` → **Create → Scriptable Objects → GameEvent** twice:
- Name one `GameOverEvent`
- Name one `GameStartEvent`

No Inspector fields to set — they're pure signals.

---

## 3. Update IntegerValue.cs — Add Reset()

Add this method to `IntegerValue`:

```csharp
public void Reset()
{
    Current = Value;
    OnChange?.Invoke(Current);
}
```

`Reset()` restores `Current` to the initial serialized `Value` and fires `OnChange` so all listeners (HealthUI) update immediately.

---

## 4. GameManager.cs

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [SerializeField] private IntegerValue health;
    [SerializeField] private GameEvent    gameOverEvent;
    [SerializeField] private GameEvent    gameStartEvent;

    void Start()
    {
        Cursor.visible = false;
    }

    void OnEnable()
    {
        health.OnChange          += OnHealthChanged;
        gameOverEvent.OnRaised   += OnGameOver;
        gameStartEvent.OnRaised  += OnGameStart;
    }

    void OnDisable()
    {
        health.OnChange          -= OnHealthChanged;
        gameOverEvent.OnRaised   -= OnGameOver;
        gameStartEvent.OnRaised  -= OnGameStart;
    }

    private void OnHealthChanged(int current)
    {
        if (current <= 0)
            gameOverEvent.Raise();
    }

    private void OnGameOver()
    {
        Cursor.visible = true;
    }

    private void OnGameStart()
    {
        health.Reset();
        Cursor.visible = false;
    }
}
```

`GameManager` does two things only:
1. Watches health → raises `GameOverEvent` when it hits 0
2. On `GameStartEvent` → resets health (which fires `OnChange` → HealthUI auto-refreshes)

---

## 5. GameManager GameObject

Hierarchy → **Create Empty** → name it `GameManager`.
- Add `GameManager` script
- Assign: `Health` → `HealthData`, `Game Over Event` → `GameOverEvent`, `Game Start Event` → `GameStartEvent`

---

## 6. GameOverUI.cs

Add this script to the **HUD GameObject** (alongside `HealthUI` — they share the same `UIDocument`).

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class GameOverUI : MonoBehaviour
{
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private VisualElement overlay;
    private Button        restartBtn;
    private Button        exitBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        overlay    = root.Q<VisualElement>("game-over-overlay");
        restartBtn = root.Q<Button>("restart-btn");
        exitBtn    = root.Q<Button>("exit-btn");

        restartBtn.clicked += OnRestartClicked;
        exitBtn.clicked    += OnExitClicked;

        gameOverEvent.OnRaised  += OnGameOver;
        gameStartEvent.OnRaised += OnGameStart;

        overlay.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        restartBtn.clicked -= OnRestartClicked;
        exitBtn.clicked    -= OnExitClicked;

        gameOverEvent.OnRaised  -= OnGameOver;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    private void OnGameOver()  => overlay.style.display = DisplayStyle.Flex;
    private void OnGameStart() => overlay.style.display = DisplayStyle.None;

    private void OnRestartClicked() => gameStartEvent.Raise();

    private void OnExitClicked()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
```

Assign `GameOverEvent` and `GameStartEvent` assets in the HUD Inspector.

> `OnRestartClicked` simply raises `GameStartEvent` — GameOverUI doesn't touch health or spawners directly. Every system resets itself.

---

## 7. Update HUD.uxml

Add the overlay element **after** the top-bar closing tag:

```xml
<ui:UXML xmlns:ui="UnityEngine.UIElements">
    <Style src="HUD.uss" />
    <ui:VisualElement name="top-bar" class="top-bar">

        <ui:VisualElement name="hearts-container" class="hud-section">
            <ui:Label name="heart-0" class="heart" text="♥" />
            <ui:Label name="heart-1" class="heart" text="♥" />
            <ui:Label name="heart-2" class="heart" text="♥" />
        </ui:VisualElement>

        <ui:VisualElement name="score-container" class="hud-section" />

    </ui:VisualElement>

    <!-- Game over overlay — hidden until GameOverEvent fires -->
    <ui:VisualElement name="game-over-overlay" class="game-over-overlay">
        <ui:VisualElement name="game-over-panel" class="game-over-panel">
            <ui:Label text="GAME OVER" class="game-over-title" />
            <ui:Button name="restart-btn" text="RESTART" class="game-btn" />
            <ui:Button name="exit-btn" text="EXIT" class="game-btn" />
        </ui:VisualElement>
    </ui:VisualElement>

</ui:UXML>
```

---

## 8. Update HUD.uss

Append to the existing stylesheet:

```css
/* ── Game Over Overlay ───────────────────────────────── */
.game-over-overlay {
    position: absolute;
    top: 0;
    left: 0;
    width: 100%;
    height: 100%;
    background-color: rgba(0, 0, 0, 0.8);
    align-items: center;
    justify-content: center;
    display: none;
}

.game-over-panel {
    width: 360px;
    padding: 48px 40px;
    border-width: 2px;
    border-color: rgb(255, 255, 255);
    background-color: rgb(0, 0, 0);
    align-items: center;
}

.game-over-title {
    font-size: 42px;
    color: rgb(255, 255, 255);
    -unity-text-align: middle-center;
    margin-bottom: 36px;
    -unity-font-style: bold;
}

.game-btn {
    width: 220px;
    height: 50px;
    margin-top: 14px;
    background-color: rgb(0, 0, 0);
    border-color: rgb(255, 255, 255);
    border-width: 2px;
    color: rgb(255, 255, 255);
    font-size: 18px;
    -unity-text-align: middle-center;
    cursor: pointer;
}

.game-btn:hover {
    background-color: rgb(255, 255, 255);
    color: rgb(0, 0, 0);
}
```

Note: `display: none` is set on `.game-over-overlay` in USS as the default state. `GameOverUI.OnEnable()` also sets it to `None` in code as a safety reset.

---

## 9. Update PlayerController.cs

Add fields:
```csharp
[SerializeField] private GameEvent gameOverEvent;
[SerializeField] private GameEvent gameStartEvent;

private bool isActive = true;
```

Add subscribe/unsubscribe:
```csharp
void OnEnable()
{
    gameOverEvent.OnRaised  += OnGameOver;
    gameStartEvent.OnRaised += OnGameStart;
}

void OnDisable()
{
    gameOverEvent.OnRaised  -= OnGameOver;
    gameStartEvent.OnRaised -= OnGameStart;
}

private void OnGameOver() => isActive = false;

private void OnGameStart()
{
    isActive = true;
    transform.position = Vector2.zero;
    UpdateColorRatio(0.5f);
}
```

Gate input in `FixedUpdate` and `OnTriggerEnter2D`:
```csharp
void FixedUpdate()
{
    if (!isActive) return;
    MoveHorizontal();
    HandleRotation();
}

void OnTriggerEnter2D(Collider2D other)
{
    if (!isActive) return;
    // ... existing logic unchanged
}
```

Assign `GameOverEvent` and `GameStartEvent` assets in the Player Inspector.

---

## 10. Update OrbSpawner.cs

Add fields:
```csharp
[SerializeField] private GameEvent gameOverEvent;
[SerializeField] private GameEvent gameStartEvent;

private bool isSpawning = true;
```

Add subscribe/unsubscribe:
```csharp
void OnEnable()
{
    gameOverEvent.OnRaised  += OnGameOver;
    gameStartEvent.OnRaised += OnGameStart;
}

void OnDisable()
{
    gameOverEvent.OnRaised  -= OnGameOver;
    gameStartEvent.OnRaised -= OnGameStart;
}

private void OnGameOver() => isSpawning = false;

private void OnGameStart()
{
    isSpawning = true;
    timer = 0f;
}
```

Gate the spawn timer in `Update`:
```csharp
void Update()
{
    if (!isSpawning) return;
    timer += Time.deltaTime;
    if (timer >= spawnInterval)
    {
        timer = 0f;
        SpawnOrb();
    }
}
```

Assign assets in the OrbSpawner Inspector.

---

## 11. Update Orb.cs

Add field:
```csharp
[SerializeField] private GameEvent gameStartEvent;
```

Add subscribe/unsubscribe:
```csharp
void OnEnable()
{
    gameStartEvent.OnRaised += OnGameStart;
}

void OnDisable()
{
    gameStartEvent.OnRaised -= OnGameStart;
}

private void OnGameStart()
{
    Destroy(gameObject);
}
```

Assign `GameStartEvent` asset on the **Orb prefab** in the Inspector — every instantiated orb will inherit it.

> When `GameStartEvent.Raise()` fires, every live orb calls `Destroy(gameObject)`. `OnDisable()` fires as part of destruction, cleanly unsubscribing from the event before the object is gone.

---

## Restart Flow (Full Sequence)

1. Player takes 3rd wrong-color hit → `Health.Set(0)` → `IntegerValue.OnChange(0)` fires
2. `GameManager.OnHealthChanged(0)` → `GameOverEvent.Raise()`
3. Simultaneously:
   - `PlayerController`: `isActive = false`
   - `OrbSpawner`: `isSpawning = false`
   - `GameOverUI`: overlay shows
   - Orbs: keep bouncing (not subscribed to GameOverEvent)
4. Player clicks Restart → `GameStartEvent.Raise()`
5. Simultaneously:
   - `GameManager`: `health.Reset()` → `OnChange(3)` → HealthUI refreshes hearts
   - `PlayerController`: `isActive = true`
   - `OrbSpawner`: `isSpawning = true`, timer = 0
   - `GameOverUI`: overlay hides
   - Every live Orb: `Destroy(gameObject)`

---

## Verify It Works

| Test | Expected |
|------|----------|
| 3 wrong-color contacts | Game over overlay appears, hearts all dim |
| Player input during game over | No movement, no rotation |
| Orbs during game over | Keep bouncing behind overlay |
| Click Restart | Overlay disappears, hearts restore, orbs gone, new orbs spawn |
| Click Exit (in Editor) | Play mode stops |
| Click Restart multiple times | Works correctly each time, no stale subscriptions |

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
| `SETUP_STEP6.md` | This file — game over, restart, SO event architecture |
| `SETUP_STEP7.md` | _(next) Score display + difficulty ramp_ |