# Implementation Step 6 — Pause Menu

## Scope

`Time.timeScale = 0` is used for both pause and game over. This makes `isActive` (Player) and `isSpawning` (OrbManager) redundant — physics, FixedUpdate, and deltaTime-based timers all stop naturally.

| File | Change |
|------|--------|
| `GameManager.cs` | ESC toggle; `timeScale` on pause + game over; `isPaused` + `isGameOver` flags |
| `Player.cs` | Remove `isActive` + `gameOverEvent`; trim `OnGameStart` |
| `OrbManager.cs` | Remove `isSpawning` + `gameOverEvent`; trim `OnGameStart` |
| `PauseMenuUI.cs` | New — `Scripts/UI/`; mirrors `GameOverUI` pattern |
| `HUD.uxml` | Add `pause-overlay` block |
| `HUD.uss` | No changes — reuses existing overlay/panel/menu-btn classes |
| `GamePausedEvent.asset` | New `GameEvent` instance |
| `GameResumedEvent.asset` | New `GameEvent` instance |

---

## Why `timeScale` replaces the flags

| | `isActive` / `isSpawning` (old) | `Time.timeScale = 0` (new) |
|---|---|---|
| Player movement | `FixedUpdate` guarded by flag | `FixedUpdate` doesn't run |
| Orb contacts | `OnTriggerEnter2D` guarded by flag | Physics frozen, no callbacks |
| Orb spawning | `Update` guarded by flag | `Time.deltaTime = 0`, timer doesn't advance |
| DifficultyManager | unchanged | `Time.deltaTime = 0`, timer doesn't advance |

Both pause and game over freeze via `timeScale = 0`. The difference is which overlay shows.

`isGameOver` local bool in `GameManager` remains — it blocks ESC from toggling pause while the game-over screen is up.

---

## `GameManager.cs`

```csharp
using UnityEngine;

public class GameManager : MonoBehaviour
{
    [Header("Play Area")]
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;

    [Header("Player")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private GameEvent gameResumedEvent;

    private bool isPaused;
    private bool isGameOver;

    void Awake()
    {
        OnGameStart();
    }

    void OnEnable()
    {
        health.OnChange += OnHealthChanged;
        gameStartEvent.OnRaised += OnGameStart;
        gameOverEvent.OnRaised += OnGameOver;
    }

    void OnDisable()
    {
        health.OnChange -= OnHealthChanged;
        gameStartEvent.OnRaised -= OnGameStart;
        gameOverEvent.OnRaised -= OnGameOver;
    }

    void Update()
    {
        if (isGameOver) return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isPaused) Resume();
            else Pause();
        }
    }

    private void Pause()
    {
        isPaused = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
        gamePausedEvent.Raise();
    }

    private void Resume()
    {
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;
        gameResumedEvent.Raise();
    }

    private void OnHealthChanged(int current)
    {
        if (current <= 0)
            gameOverEvent.Raise();
    }

    private void OnGameStart()
    {
        isGameOver = false;
        isPaused = false;
        Time.timeScale = 1f;
        Cursor.visible = false;

        health.Reset();
        score.Reset();
        halfHeightPlayArea.Set(Camera.main.orthographicSize);
        halfWidthPlayArea.Set(Camera.main.orthographicSize * Camera.main.aspect);
    }

    private void OnGameOver()
    {
        isGameOver = true;
        Time.timeScale = 0f;
        Cursor.visible = true;
    }
}
```

**`OnGameStart` resets `timeScale = 1` unconditionally** — covers restart from both pause and game over without needing to call `Resume()`.

**`OnGameOver` does not raise `gamePausedEvent`** — the freeze is silent; `gameOverEvent` itself drives the game-over overlay via `GameOverUI`.

---

## `Player.cs`

Removed: `isActive`, `gameOverEvent` field, `OnGameOver()`, `if (!isActive) return` guards.
`OnGameStart` keeps position and colorRatio reset; drops `isActive = true`.

```csharp
using UnityEngine;

[RequireComponent(typeof(PlayerMovement))]
public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private Material splitMaterial;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void FixedUpdate()
    {
        playerMovement.Handle();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);

        if (orb.IsWhite == hitWhiteHalf)
        {
            colorRatio += orb.IsWhite ? Constants.COLLECT_DELTA : -Constants.COLLECT_DELTA;
            colorRatio = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score.Set(score.Value + 1);
        }
        else
        {
            if (health.Value > 0)
                health.Set(health.Value - 1);
        }

        Destroy(other.gameObject);
    }

    private bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;
        float t = localAngle / (2f * Mathf.PI) + 0.5f;
        t = t - Mathf.Floor(t);
        return t < colorRatio;
    }

    private void OnGameStart()
    {
        transform.position = Vector2.zero;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}
```

---

## `OrbManager.cs`

Removed: `isSpawning`, `gameOverEvent` field and subscription, `OnGameOver()`, `if (!isSpawning) return`.
`OnGameStart` keeps orb cleanup and timer reset; drops `isSpawning = true`.

```csharp
using UnityEngine;

public class OrbManager : MonoBehaviour
{
    [Header("Spawner config")]
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private FloatValue halfHeightPlayArea;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue spawnInterval;
    [SerializeField] private FloatValue orbSpeed;

    [Header("Orbs")]
    [SerializeField] private OrbSet orbSet;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;

    private OrbSpawner orbSpawner;
    private float timer;

    void Start()
    {
        orbSpawner = new OrbSpawner(orbPrefab, halfHeightPlayArea, halfWidthPlayArea);
        timer = spawnInterval.Value;
    }

    void OnEnable() => gameStartEvent.OnRaised += OnGameStart;
    void OnDisable() => gameStartEvent.OnRaised -= OnGameStart;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval.Value)
        {
            timer = 0f;
            orbSpawner.Spawn(orbSpeed.Value);
        }
    }

    private void OnGameStart()
    {
        for (int i = orbSet.Items.Count - 1; i >= 0; i--)
            Destroy(orbSet.Items[i].gameObject);

        timer = spawnInterval.Value;
    }
}
```

---

## `PauseMenuUI.cs`

```csharp
using UnityEngine;
using UnityEngine.UIElements;

public class PauseMenuUI : MonoBehaviour
{
    [SerializeField] private GameEvent gamePausedEvent;
    [SerializeField] private GameEvent gameResumedEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private VisualElement overlay;
    private Button resumeBtn;
    private Button restartBtn;
    private Button exitBtn;

    void OnEnable()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;

        overlay    = root.Q<VisualElement>("pause-overlay");
        resumeBtn  = root.Q<Button>("pause-resume-btn");
        restartBtn = root.Q<Button>("pause-restart-btn");
        exitBtn    = root.Q<Button>("pause-exit-btn");

        resumeBtn.clicked  += OnResumeClicked;
        restartBtn.clicked += OnRestartClicked;
        exitBtn.clicked    += OnExitClicked;

        gamePausedEvent.OnRaised  += OnPause;
        gameResumedEvent.OnRaised += OnResume;
        gameStartEvent.OnRaised   += OnResume;

        overlay.style.display = DisplayStyle.None;
    }

    void OnDisable()
    {
        resumeBtn.clicked  -= OnResumeClicked;
        restartBtn.clicked -= OnRestartClicked;
        exitBtn.clicked    -= OnExitClicked;

        gamePausedEvent.OnRaised  -= OnPause;
        gameResumedEvent.OnRaised -= OnResume;
        gameStartEvent.OnRaised   -= OnResume;
    }

    private void OnPause()  => overlay.style.display = DisplayStyle.Flex;
    private void OnResume() => overlay.style.display = DisplayStyle.None;

    private void OnResumeClicked()  => gameResumedEvent.Raise();
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

---

## HUD.uxml

Add inside the root alongside `game-over-overlay`:

```xml
<ui:VisualElement name="pause-overlay" class="overlay">
    <ui:VisualElement class="panel">
        <ui:Label class="panel-title" text="PAUSED" />
        <ui:Button name="pause-resume-btn"  class="menu-btn" text="RESUME"  />
        <ui:Button name="pause-restart-btn" class="menu-btn" text="RESTART" />
        <ui:Button name="pause-exit-btn"    class="menu-btn" text="QUIT"    />
    </ui:VisualElement>
</ui:VisualElement>
```

Reuses `overlay`, `panel`, `panel-title`, `menu-btn` — no USS changes needed.

---

## Inspector Wiring

### New assets
Create in `Assets/ScriptableObjects/`:
- `GamePausedEvent.asset` (GameEvent)
- `GameResumedEvent.asset` (GameEvent)

### GameManager
| Field | Value |
|-------|-------|
| Game Paused Event | `GamePausedEvent` |
| Game Resumed Event | `GameResumedEvent` |

Remove the `gameOverEvent` slot from Player and the `gameOverEvent` slot from OrbManager in the Inspector.

### HUD GameObject — add `PauseMenuUI` component
| Field | Value |
|-------|-------|
| Game Paused Event | `GamePausedEvent` |
| Game Resumed Event | `GameResumedEvent` |
| Game Start Event | `GameStartEvent` |

---

## Verify

| Test | Expected |
|------|----------|
| ESC during play | Freeze, pause overlay shows, cursor visible |
| ESC again / Resume btn | Resume, overlay hides, cursor hidden |
| Restart from pause | Overlay hides, full game reset, `timeScale = 1` |
| Game over | Freeze, game-over overlay shows, cursor visible |
| ESC during game over | Nothing — blocked by `isGameOver` |
| Restart from game over | Full reset, `timeScale = 1` |
| Quit from either menu | Application exits |
