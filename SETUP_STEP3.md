# Step 3 — Orb Spawning & Bouncing

Goal: Orbs spawn just off-screen on a random edge, move inward at a random angle, bounce off screen edges (angle of incidence = angle of reflection), are randomly black or white, and destroy themselves after a random lifetime.

---

## Overview of New Pieces

| Piece | Type | Purpose |
|-------|------|---------|
| `Orb.cs` | Script | Movement, bouncing, lifetime, color |
| `OrbSpawner.cs` | Script | Spawns orbs at intervals |
| `Orb` Prefab | GameObject | Reusable orb template |
| `OrbSpawner` GameObject | Scene object | Runs the spawn loop |

---

## Visual Note — Black Orbs on Black Background

Black orbs on a black camera background are invisible. The fix: every orb gets a **white outline** — a slightly larger white circle child rendered behind the main circle. This makes black orbs clearly visible (black disc with white ring) without affecting white orbs much (white disc with white ring = just a slightly sharper edge).

---

## 1. Create the Orb Prefab

### 1a. Base GameObject
- Hierarchy → right-click → **Create Empty**, name it `Orb`
- **Transform:** Position `(0, 0, 0)`, Scale `(0.4, 0.4, 0.4)`
  - This makes the orb noticeably smaller than the player (player = 1 unit diameter, orb = 0.4)

### 1b. Add Components to Orb
| Component | Settings |
|-----------|----------|
| **Sprite Renderer** | Sprite: circle (same Knob or Create → Sprites → Circle used in Step 1), Color: White, Order in Layer: `1` |
| **Rigidbody 2D** | Body Type: `Dynamic`, Gravity Scale: `0`, Collision Detection: `Continuous`, Freeze Rotation Z: ✅ |
| **Circle Collider 2D** | Is Trigger: ✅, Radius: `0.5` |
| **Orb** script | (created below) |

> Rigidbody2D is **Dynamic** (not Kinematic) so that trigger callbacks fire reliably with the player's Kinematic Rigidbody2D. Gravity Scale 0 means no gravity.

### 1c. Add the Outline Child
Inside the `Orb` GameObject, right-click → **Create Empty**, name it `Outline`.

| Component | Settings |
|-----------|----------|
| **Sprite Renderer** | Same circle sprite, Color: **White**, Order in Layer: `0` (renders behind parent) |

Set `Outline` Transform:
- Position: `(0, 0, 0)`
- Scale: `(1.2, 1.2, 1)` — 20% larger than parent in local space

The outline inherits the parent's world scale `(0.4 × 1.2 = 0.48)`, making it a thin visible ring behind any orb color.

### 1d. Save as Prefab
Drag the `Orb` GameObject from the Hierarchy into the **Project** window → **Original Prefab**. Then delete it from the scene.

---

## 2. Orb.cs

Create a new C# script named `Orb`, attach it to the `Orb` prefab.

```csharp
using UnityEngine;

public class Orb : MonoBehaviour
{
    [SerializeField] private float speed       = 3f;
    [SerializeField] private float minLifetime = 4f;
    [SerializeField] private float maxLifetime = 9f;

    public bool IsWhite { get; private set; }

    private Rigidbody2D rb;
    private float       orbRadius;
    private float       halfWidth;
    private float       halfHeight;

    void Awake()
    {
        rb         = GetComponent<Rigidbody2D>();
        // bounds.extents.x is the true world-space radius — already accounts for scale
        orbRadius  = GetComponent<CircleCollider2D>().bounds.extents.x;
        halfHeight = Camera.main.orthographicSize;
        halfWidth  = halfHeight * Camera.main.aspect;
    }

    // Called by OrbSpawner immediately after Instantiate
    public void Initialize(bool isWhite, Vector2 direction)
    {
        IsWhite = isWhite;
        GetComponent<SpriteRenderer>().color = isWhite ? Color.white : Color.black;
        rb.linearVelocity = direction.normalized * speed;

        Destroy(gameObject, Random.Range(minLifetime, maxLifetime));
    }

    void FixedUpdate()
    {
        Vector2 pos = rb.position;
        Vector2 vel = rb.linearVelocity;
        bool bounced = false;

        // Left / right walls
        if (pos.x - orbRadius < -halfWidth)
        {
            vel.x = Mathf.Abs(vel.x);
            pos.x = -halfWidth + orbRadius;
            bounced = true;
        }
        else if (pos.x + orbRadius > halfWidth)
        {
            vel.x = -Mathf.Abs(vel.x);
            pos.x = halfWidth - orbRadius;
            bounced = true;
        }

        // Top / bottom walls
        if (pos.y - orbRadius < -halfHeight)
        {
            vel.y = Mathf.Abs(vel.y);
            pos.y = -halfHeight + orbRadius;
            bounced = true;
        }
        else if (pos.y + orbRadius > halfHeight)
        {
            vel.y = -Mathf.Abs(vel.y);
            pos.y = halfHeight - orbRadius;
            bounced = true;
        }

        if (bounced)
        {
            rb.position = pos; // direct teleport — MovePosition applies a force on Dynamic bodies, not a position set
            rb.linearVelocity = vel;
        }
    }
}
```

**How bouncing works:**
- Each `FixedUpdate`, check if the orb's edge has crossed any wall
- If it has, flip the relevant velocity component (`vel.x` or `vel.y`) and clamp position back inside bounds
- Using `Mathf.Abs` / `-Mathf.Abs` instead of just negating ensures correct direction even if the orb somehow overshoots by more than one frame
- `rb.velocity` is only overwritten on a bounce — otherwise the Dynamic rigidbody carries its own velocity normally

**`Initialize` is called by the spawner** right after `Instantiate`. Because it's in `Awake()` (not `Start()`), `rb` is guaranteed to be set before `Initialize` is called.

---

## 3. OrbSpawner.cs

Create a new C# script named `OrbSpawner`.

```csharp
using UnityEngine;

public class OrbSpawner : MonoBehaviour
{
    [SerializeField] private Orb orbPrefab;
    [SerializeField] private float      spawnInterval = 2f;
    [SerializeField] private float      spawnMargin   = 0.3f; // how far outside screen to spawn

    private float timer;
    private float halfWidth;
    private float halfHeight;

    void Start()
    {
        halfHeight = Camera.main.orthographicSize;
        halfWidth  = halfHeight * Camera.main.aspect;
    }

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            timer = 0f;
            SpawnOrb();
        }
    }

    void SpawnOrb()
    {
        int     edge  = Random.Range(0, 4);
        Vector2 spawnPos;
        float   angleMin, angleMax;

        // Pick edge, position along it, and the inward angle range
        switch (edge)
        {
            case 0: // top — orb must point downward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    halfHeight + spawnMargin
                );
                angleMin = 210f; angleMax = 330f;
                break;

            case 1: // right — orb must point leftward
                spawnPos = new Vector2(
                    halfWidth + spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = 120f; angleMax = 240f;
                break;

            case 2: // bottom — orb must point upward
                spawnPos = new Vector2(
                    Random.Range(-halfWidth, halfWidth),
                    -halfHeight - spawnMargin
                );
                angleMin = 30f; angleMax = 150f;
                break;

            default: // left — orb must point rightward
                spawnPos = new Vector2(
                    -halfWidth - spawnMargin,
                    Random.Range(-halfHeight, halfHeight)
                );
                angleMin = -60f; angleMax = 60f;
                break;
        }

        // Convert random angle to direction vector
        float   angleDeg = Random.Range(angleMin, angleMax);
        float   angleRad = angleDeg * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angleRad), Mathf.Sin(angleRad));

        bool isWhite = Random.value > 0.5f;
        Orb orb = Instantiate(orbPrefab, spawnPos, Quaternion.identity);
        orb.GetComponent<Orb>().Initialize(isWhite, direction);
    }
}
```

**Inspector tunables:**
| Field | Default | Notes |
|-------|---------|-------|
| `orbPrefab` | — | Drag the Orb prefab in here |
| `spawnInterval` | `2` | Seconds between spawns; reduce over time for difficulty later |
| `spawnMargin` | `0.3` | World units off-screen the orb spawns at |

---

## 4. Create the OrbSpawner GameObject

- Hierarchy → **Create Empty**, name it `OrbSpawner`
- Transform: `(0, 0, 0)` (position doesn't matter, it's just a logic object)
- Add Component → `OrbSpawner`
- Drag the `Orb` **prefab** from the Project into the **Orb Prefab** slot in the Inspector

---

## 5. Verify It Works

Hit **Play**. You should see:
- Orbs appearing from all four screen edges and moving inward
- Roughly 50% white orbs, 50% black orbs with white ring
- Orbs bouncing off edges with the angle preserved
- Orbs disappearing after their lifetime (between 4–9 seconds)
- Player circle unaffected (no collection logic yet — orbs pass through the player)

**Quick sanity checks:**
| Test | Expected |
|------|----------|
| Watch an orb hit a wall | Reflects, doesn't stick or escape |
| Watch a black orb | Visible as black disc with white ring |
| Orbs near a corner | Bounces off both walls cleanly |
| Wait 30+ seconds | Orbs disappear and don't accumulate infinitely |
| Player moves through orbs | Passes through, nothing happens yet |

---

## What's Deliberately Left Out of This Step
- Player collecting orbs / contact detection (Step 4)
- `colorRatio` shifting on collect (Step 4)
- Health loss on wrong-color contact (Step 4)
- Difficulty scaling / spawn rate increasing over time (Step 5)

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | Shader + mouse rotation |
| `SETUP_STEP3.md` | This file — orb spawning + bouncing |
| `SETUP_STEP4.md` | _(next) Collection mechanic + health_ |
