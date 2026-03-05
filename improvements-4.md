# Implementation Step 4 — PlayerInput + Full WASD Movement

## Scope

- Extract input reading into a new `PlayerInput` component on the Player GameObject
- Rename `PlayerController` → `Player`
- Extend movement from horizontal-only to full WASD (4-direction + diagonal)
- Left/right arrows remain as rotation keys

### What changes
| File | Change |
|------|--------|
| `PlayerInput.cs` | **New** — reads keyboard each frame, exposes `MoveInput` and `RotateInput` |
| `Player.cs` | Renamed from `PlayerController`; reads from `PlayerInput`; movement extended to 2D; adds `halfHeightPlayArea` for Y clamp |
| `PlayerController.cs` | **Deleted** |

### What stays the same
- All other scripts — no references to `PlayerController` exist outside the Player GameObject itself

---

## Architecture

Both scripts live on the same Player GameObject. `PlayerInput` reads raw input in `Update` and stores it as public properties. `Player` reads those properties in `FixedUpdate`. No cross-references needed — `Player` gets `PlayerInput` via `GetComponent` in `Start`.

`PlayerInput` is intentionally dumb: no game state awareness, no events, no SOs. It just reads keys. `Player` decides whether to act (via `isActive` guard).

Input is captured in `Update` (so no key presses are missed between fixed steps) and consumed in `FixedUpdate` (so physics stays frame-rate independent).

---

## New Script — `PlayerInput.cs`

```csharp
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    public Vector2 MoveInput { get; private set; }
    public float RotateInput { get; private set; }

    void Update()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        else if (Input.GetKey(KeyCode.D)) x = 1f;

        if (Input.GetKey(KeyCode.W)) y = 1f;
        else if (Input.GetKey(KeyCode.S)) y = -1f;

        MoveInput = new Vector2(x, y);

        float rotate = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) rotate = 1f;
        else if (Input.GetKey(KeyCode.RightArrow)) rotate = -1f;

        RotateInput = rotate;
    }
}
```

---

## New Script — `Player.cs`

Changes from `PlayerController`:
- `GetComponent<PlayerInput>()` in `Start`
- `MoveHorizontal` → `Move` using `playerInput.MoveInput` as a `Vector2`; diagonal input is normalised so speed is consistent in all directions
- `HandleRotation` reads `playerInput.RotateInput`
- `halfHeightPlayArea` added for Y-axis clamping — wired the same way as `halfWidthPlayArea`

```csharp
using UnityEngine;

public class Player : MonoBehaviour
{
    [Header("Stats")]
    [SerializeField] private IntegerValue health;
    [SerializeField] private IntegerValue score;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Events")]
    [SerializeField] private GameEvent gameOverEvent;
    [SerializeField] private GameEvent gameStartEvent;

    private Rigidbody2D rb;
    private Material splitMaterial;
    private float playerRadius;
    private PlayerInput playerInput;

    private float colorRatio = Constants.DEFAULT_COLOR_RATIO;
    private bool isActive = true;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
        playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
        playerInput = GetComponent<PlayerInput>();
    }

    void OnEnable()
    {
        gameOverEvent.OnRaised += OnGameOver;
        gameStartEvent.OnRaised += OnGameStart;
    }

    void OnDisable()
    {
        gameOverEvent.OnRaised -= OnGameOver;
        gameStartEvent.OnRaised -= OnGameStart;
    }

    void FixedUpdate()
    {
        if (!isActive) return;

        Move();
        HandleRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!isActive) return;

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

    private void Move()
    {
        Vector2 input = playerInput.MoveInput;
        if (input == Vector2.zero) return;

        Vector2 newPos = rb.position + input.normalized * moveSpeed * Time.fixedDeltaTime;

        float xBound = halfWidthPlayArea.Value - playerRadius;
        float yBound = halfHeightPlayArea.Value - playerRadius;
        newPos.x = Mathf.Clamp(newPos.x, -xBound, xBound);
        newPos.y = Mathf.Clamp(newPos.y, -yBound, yBound);

        rb.MovePosition(newPos);
    }

    private void HandleRotation()
    {
        rb.MoveRotation(rb.rotation + playerInput.RotateInput * rotateSpeed * Time.fixedDeltaTime);
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

    private void OnGameOver() => isActive = false;

    private void OnGameStart()
    {
        isActive = true;
        transform.position = Vector2.zero;
        colorRatio = Mathf.Clamp01(Constants.DEFAULT_COLOR_RATIO);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }
}
```

---

## Diagonal Movement Note

`input.normalized` ensures consistent speed in all directions. Without it, pressing W+D simultaneously would produce a `(1, 1)` vector with magnitude √2, giving ~41% faster diagonal movement. With normalisation `(1, 1).normalized = (0.71, 0.71)`, speed is uniform.

The early return `if (input == Vector2.zero) return` skips the `MovePosition` call entirely when no key is held — no physics call needed, marginally cleaner.

---

## Scene Setup

1. **Delete** `PlayerController.cs` from the project
2. **Create** `PlayerInput.cs` and `Player.cs`
3. On the Player GameObject:
   - Remove `PlayerController` component
   - Add `PlayerInput` component
   - Add `Player` component

### Player Inspector — new field
| Field | Value |
|-------|-------|
| Half Height Play Area | `HalfHeightPlayArea` asset |

All other fields carry over from `PlayerController` — wire them the same way.

> `PlayerInput` has no inspector fields to wire.
