# Implementation Step 4 — Full WASD Movement

## Current State (as implemented)

The player refactor is already done:

| Script | Role |
|--------|------|
| `Player.cs` | Orb collision, color ratio, health/score, game events. Calls `playerMovement.Handle()` in `FixedUpdate` |
| `PlayerMovement.cs` | Movement and rotation. `Handle()` is the single entry point called by `Player` |
| `PlayerController.cs` | **Leftover — delete this file** |

`Player` enforces the dependency with `[RequireComponent(typeof(PlayerMovement))]`. `PlayerMovement` reads input and drives the Rigidbody directly.

---

## Scope of This Step

One file changes: **`PlayerMovement.cs`**.

- `HandleMovement` extended from horizontal-only (A/D) to full 4-direction + diagonal (W/A/S/D)
- `halfHeightPlayArea` added for Y-axis clamping
- `halfWidthPlayArea` reference already present — no change there

`Player.cs`, `PlayerController.cs` (delete it), and everything else — untouched.

---

## Updated Script — `PlayerMovement.cs`

```csharp
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    private Rigidbody2D rb;
    private float playerRadius;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
    }

    private void HandleMovement()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        else if (Input.GetKey(KeyCode.D)) x = 1f;

        if (Input.GetKey(KeyCode.W)) y = 1f;
        else if (Input.GetKey(KeyCode.S)) y = -1f;

        Vector2 input = new Vector2(x, y);
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
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f;
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f;

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    public void Handle()
    {
        HandleMovement();
        HandleRotation();
    }
}
```

**Why `input.normalized`:** pressing W+D simultaneously produces `(1, 1)` with magnitude √2, giving ~41% faster diagonal movement. Normalising to `(0.71, 0.71)` keeps speed consistent in all directions. The early `return` when input is zero avoids an unnecessary `MovePosition` call.

---

## Inspector Wiring

### PlayerMovement — new field
| Field | Value |
|-------|-------|
| Half Height Play Area | `HalfHeightPlayArea` asset |

All existing fields (`moveSpeed`, `rotateSpeed`, `halfWidthPlayArea`) stay the same.

---

## Cleanup

Delete `PlayerController.cs` — it is a leftover from before the refactor and is no longer referenced by anything.
