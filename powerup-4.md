# Powerup Step 4 — Expand + Shrink Player (Timed)

## Scope

| File | Change |
|------|--------|
| `ExpandPlayerEffect.cs` | New SO — raises `ExpandPlayerEvent` |
| `ShrinkPlayerEffect.cs` | New SO — raises `ShrinkPlayerEvent` |
| `PlayerMovement.cs` | Coroutine-based scale + speed modifier; resets on restart |

---

## Design

Both effects use the same event-raise pattern as `BalanceColorEffect`. `PlayerMovement` subscribes to both events and runs a coroutine that applies the multipliers, waits 5 seconds, then reverts to base values.

**Multipliers:**

| Effect | Scale | Move speed | Rotate speed |
|--------|-------|-----------|--------------|
| Expand | × 1.25 | × 0.75 | × 0.75 |
| Shrink | × 0.75 | × 1.25 | × 1.25 |

**Edge cases:**
- Picking up the same effect twice, or the opposite effect mid-duration: the active coroutine is stopped and a new one starts — always reverting to base values, never stacking.
- Restart while effect is active: coroutine cancelled, base values restored immediately.
- Pause: `WaitForSeconds` respects `Time.timeScale = 0` — the 5 second timer freezes correctly.

**`playerRadius` update:** `playerRadius` is used for movement clamping and is cached from `CircleCollider2D.bounds.extents.x` in `Start`. Since `bounds` is scale-aware, re-reading it after changing `transform.localScale` gives the correct world-space radius. It's updated at both apply and revert.

---

## `ExpandPlayerEffect.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ExpandPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ExpandPlayer")]
public class ExpandPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent expandPlayerEvent;

    public override void Apply() => expandPlayerEvent.Raise();
}
```

---

## `ShrinkPlayerEffect.cs`

```csharp
using UnityEngine;

[CreateAssetMenu(fileName = "ShrinkPlayerEffect", menuName = "Scriptable Objects/Powerup Effects/ShrinkPlayer")]
public class ShrinkPlayerEffect : PowerupEffect
{
    [SerializeField] private GameEvent shrinkPlayerEvent;

    public override void Apply() => shrinkPlayerEvent.Raise();
}
```

---

## `PlayerMovement.cs`

```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D)), RequireComponent(typeof(CircleCollider2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float rotateSpeed = 180f;
    [SerializeField] private FloatValue halfWidthPlayArea;
    [SerializeField] private FloatValue halfHeightPlayArea;

    [Header("Scale Effect")]
    [SerializeField] private float scaleDuration = 5f;

    [Header("Events")]
    [SerializeField] private GameEvent gameStartEvent;
    [SerializeField] private GameEvent expandPlayerEvent;
    [SerializeField] private GameEvent shrinkPlayerEvent;

    private Rigidbody2D rb;
    private CircleCollider2D col;
    private float playerRadius;

    private float baseMoveSpeed;
    private float baseRotateSpeed;
    private float baseScale;

    private Coroutine activeScaleCoroutine;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CircleCollider2D>();
        playerRadius = col.bounds.extents.x;

        baseMoveSpeed = moveSpeed;
        baseRotateSpeed = rotateSpeed;
        baseScale = transform.localScale.x;
    }

    void OnEnable()
    {
        gameStartEvent.OnRaised += OnGameStart;
        expandPlayerEvent.OnRaised += OnExpand;
        shrinkPlayerEvent.OnRaised += OnShrink;
    }

    void OnDisable()
    {
        gameStartEvent.OnRaised -= OnGameStart;
        expandPlayerEvent.OnRaised -= OnExpand;
        shrinkPlayerEvent.OnRaised -= OnShrink;
    }

    private void OnExpand() => StartScaleEffect(1.25f, 0.75f);
    private void OnShrink() => StartScaleEffect(0.75f, 1.25f);

    private void StartScaleEffect(float scaleMult, float speedMult)
    {
        if (activeScaleCoroutine != null)
            StopCoroutine(activeScaleCoroutine);

        activeScaleCoroutine = StartCoroutine(ScaleEffect(scaleMult, speedMult));
    }

    private IEnumerator ScaleEffect(float scaleMult, float speedMult)
    {
        ApplyScaleValues(baseScale * scaleMult, baseMoveSpeed * speedMult, baseRotateSpeed * speedMult);

        yield return new WaitForSeconds(scaleDuration);

        ResetScaleEffect();
    }

    private void ApplyScaleValues(float scale, float move, float rotate)
    {
        transform.localScale = Vector3.one * scale;
        playerRadius = col.bounds.extents.x;
        moveSpeed = move;
        rotateSpeed = rotate;
    }

    private void ResetScaleEffect()
    {
        ApplyScaleValues(baseScale, baseMoveSpeed, baseRotateSpeed);
        activeScaleCoroutine = null;
    }

    private void OnGameStart()
    {
        if (activeScaleCoroutine != null)
        {
            StopCoroutine(activeScaleCoroutine);
            activeScaleCoroutine = null;
        }
        ResetScaleEffect();
    }

    private void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow)) input = 1f;
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f;

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    private void HandleMovement()
    {
        float x = 0f;
        float y = 0f;

        if (Input.GetKey(KeyCode.A)) x = -1f;
        else if (Input.GetKey(KeyCode.D)) x = 1f;

        if (Input.GetKey(KeyCode.W)) y = 1f;
        else if (Input.GetKey(KeyCode.S)) y = -1f;

        Vector2 moveInput = new Vector2(x, y);

        Vector2 newPos = rb.position + moveInput.normalized * moveSpeed * Time.fixedDeltaTime;
        float xBound = halfWidthPlayArea.Value - playerRadius;
        float yBound = halfHeightPlayArea.Value - playerRadius;
        newPos.x = Mathf.Clamp(newPos.x, -xBound, xBound);
        newPos.y = Mathf.Clamp(newPos.y, -yBound, yBound);

        rb.MovePosition(newPos);
    }

    public void Handle()
    {
        HandleMovement();
        HandleRotation();
    }
}
```

**`ApplyScaleValues` helper:** Both apply and revert go through the same function — no risk of forgetting to update one of the three values in either path.

**`baseMoveSpeed` / `baseRotateSpeed`:** Captured from the serialized Inspector values in `Start`. This means the base is always the design-time value, not whatever the current modified value is — stacking is impossible by construction.

---

## New Assets

| Asset | Type | Wiring |
|-------|------|--------|
| `ExpandPlayerEvent.asset` | GameEvent | — |
| `ShrinkPlayerEvent.asset` | GameEvent | — |
| `ExpandPlayerEffect.asset` | ExpandPlayerEffect | Expand Player Event: `ExpandPlayerEvent.asset` |
| `ShrinkPlayerEffect.asset` | ShrinkPlayerEffect | Shrink Player Event: `ShrinkPlayerEvent.asset` |

---

## Inspector Wiring

### PlayerMovement
| Field | Value |
|-------|-------|
| Scale Duration | `5` |
| Game Start Event | `GameStartEvent.asset` |
| Expand Player Event | `ExpandPlayerEvent.asset` |
| Shrink Player Event | `ShrinkPlayerEvent.asset` |

### PowerupManager — Effects array
Add `ExpandPlayerEffect.asset` and `ShrinkPlayerEffect.asset`.

---

## Verify

| Test | Expected |
|------|----------|
| Pick up expand | Player visually bigger, moves and rotates noticeably slower |
| After 5s | Returns to normal size and speed |
| Pick up shrink | Player visually smaller, moves and rotates noticeably faster |
| After 5s | Returns to normal |
| Pick up expand then shrink mid-duration | Expand immediately cancelled, shrink applied from base values |
| Pick up expand twice | First coroutine cancelled, fresh 5s timer starts from base values |
| Restart during effect | Instantly resets to base size and speed |
| Pause during effect | 5s timer frozen; resumes on unpause |
