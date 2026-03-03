# Step 4 — Orb Collection & Color Ratio Shifting

Goal: When an orb enters the player's collider, determine which half of the player circle was hit using the same angle math as the shader. Correct color → collect, shift `colorRatio`, update shader. Wrong color → `Debug.Log` for now (health system added in Step 5).

---

## What Changes From Step 3

Only `PlayerController.cs` is modified — no new GameObjects, no new scripts, no prefab changes.

| What | Change |
|------|--------|
| `PlayerController.cs` | Add `OnTriggerEnter2D`, `IsWhiteHalf()`, score field |
| Player `Start()` | Fix `playerRadius` to use `bounds.extents.x` (same fix as Step 3 orb) |

---

## The Angle Check — How It Works

This is the core of the mechanic and must match the shader exactly.

**The shader** (from Step 2) determines a pixel's color using:
```hlsl
float angle = atan2(c.y, c.x);       // c is position relative to circle center, local space
float t     = angle / TWO_PI + 0.5;  // normalised to [0, 1]
// white where t < _ColorRatio
```

**The C# collision code** must reproduce the same `t` value for the orb's contact point:

```
1. Get direction from player center → orb center (world space)
2. Convert to local space by subtracting the player's current rotation
3. Apply the same formula: t = atan2(y, x) / 2π + 0.5
4. Wrap with frac() to handle the [0,1] boundary cleanly
5. t < colorRatio → white half hit
```

The key step is **subtracting player rotation** before computing `t`. Without it, the collision check would use world-space angles while the shader uses local-space angles — they'd only agree when the player is unrotated.

---

## Updated PlayerController.cs

Replace the entire script with this:

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private IntegerValue Health;

    [SerializeField] private float moveSpeed    = 5f;
    [SerializeField] private float rotateSpeed  = 180f;
    [SerializeField] private float colorRatio   = 0.5f;
    [SerializeField] private float collectDelta = 0.1f;

    private Rigidbody2D rb;
    private Material    splitMaterial;
    private float       leftBound;
    private float       rightBound;
    private int         score = 0;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);

        // bounds.extents.x = true world-space radius, accounts for scale automatically
        float playerRadius = GetComponent<CircleCollider2D>().bounds.extents.x;
        float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;
        leftBound  = -camHalfWidth + playerRadius;
        rightBound =  camHalfWidth - playerRadius;
    }

    void FixedUpdate()
    {
        MoveHorizontal();
        HandleRotation();
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        Orb orb = other.GetComponent<Orb>();
        if (orb == null) return;

        bool hitWhiteHalf = IsWhiteHalf(other.transform.position);

        if (orb.IsWhite == hitWhiteHalf)
        {
            // Correct color — collect
            colorRatio += orb.IsWhite ? collectDelta : -collectDelta;
            colorRatio  = Mathf.Clamp01(colorRatio);
            splitMaterial.SetFloat("_ColorRatio", colorRatio);
            score++;
            Debug.Log($"Collected! Score: {score} | ColorRatio: {colorRatio:F2}");
        }
        else
        {
            if (Health.Current > 0)
                Health.Set(Health.Current - 1);
        }

        Destroy(other.gameObject);
    }

    // --- Private ---

    void HandleRotation()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.LeftArrow))       input =  1f; // counter-clockwise
        else if (Input.GetKey(KeyCode.RightArrow)) input = -1f; // clockwise

        rb.MoveRotation(rb.rotation + input * rotateSpeed * Time.fixedDeltaTime);
    }

    void MoveHorizontal()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        else if (Input.GetKey(KeyCode.D)) input =  1f;

        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);
        rb.MovePosition(newPos);
    }

    bool IsWhiteHalf(Vector3 orbWorldPos)
    {
        // Direction from player center to orb in world space
        Vector2 worldDir = (Vector2)(orbWorldPos - transform.position);

        // World-space angle of that direction (radians)
        float worldAngle = Mathf.Atan2(worldDir.y, worldDir.x);

        // Subtract player's rotation to convert to local space
        // rb.rotation is in degrees, shader works in local space
        float localAngle = worldAngle - rb.rotation * Mathf.Deg2Rad;

        // Same formula as the shader: t = angle / 2π + 0.5
        float t = localAngle / (2f * Mathf.PI) + 0.5f;

        // frac() — strip integer part to keep t in [0, 1] across wrap-around
        t = t - Mathf.Floor(t);

        // White sector is t < colorRatio — matches shader's step(t, _ColorRatio)
        return t < colorRatio;
    }

    // --- Public API (used by Step 5 systems) ---

    public void UpdateColorRatio(float newRatio)
    {
        colorRatio = Mathf.Clamp01(newRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    public float GetColorRatio() => colorRatio;
    public int   GetScore()      => score;
}
```

---

## Why the Orb Is Always Destroyed

Wrong-color contacts destroy the orb too (`Destroy(other.gameObject)` runs in both branches). If the orb were kept alive, it would sit inside the player's trigger collider and fire `OnTriggerEnter2D` every frame it re-enters — resulting in dozens of messages per second. Destroying it on wrong contact is the correct behaviour; the health penalty in Step 5 replaces the debug log.

---

## Inspector Tunables

| Field | Default | Effect |
|-------|---------|--------|
| `moveSpeed` | `5` | Horizontal speed in world units/sec |
| `colorRatio` | `0.5` | Starting split (0 = all black, 1 = all white) |
| `collectDelta` | `0.1` | How much ratio shifts per collection |

`collectDelta = 0.1` means 5 wrong-side collects in a row would go from 50/50 to fully one colour. Tune down for slower shifts, up for more chaos.

---

## Verify It Works

Hit **Play**, open the **Console** window (`Window → General → Console`).

| Test | Expected console output |
|------|------------------------|
| White orb hits white half | `Collected! Score: 1 \| ColorRatio: 0.60` |
| Black orb hits black half | `Collected! Score: 2 \| ColorRatio: 0.50` |
| White orb hits black half | `Wrong color! Orb: white \| Hit half: black` |
| Watch player circle | White arc visibly grows/shrinks as ratio shifts |
| Rotate player, then collect | Correct half still detected after rotation |

The last test is the important one — rotate the player 180° so the halves are flipped, then collect. The console should still correctly identify which half each orb hit.

---

## What's Deliberately Left Out of This Step
- Health system / hearts (Step 5)
- Score UI on screen (Step 5)
- Difficulty scaling / spawn rate ramp (Step 5)
- Game over state (Step 5)

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | Shader + mouse rotation |
| `SETUP_STEP3.md` | Orb spawning + bouncing |
| `SETUP_STEP4.md` | This file — collection mechanic + ratio shifting |
| `SETUP_STEP5.md` | _(next) Health, score UI, game over, difficulty_ |
