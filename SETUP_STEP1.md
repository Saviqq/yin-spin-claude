# Step 1 — Project Setup & Player Movement

Goal: Unity project running with a white circle in the center of the screen that moves left/right with A/D, clamped to screen edges.

---

## 1. Create the Unity Project

1. Open Unity Hub → **New Project**
2. Template: **2D (Built-in Render Pipeline)** ← important, not URP or HDRP
3. Name it whatever you like, e.g. `YinAndSpin`
4. Click **Create Project**

> Built-in render pipeline is chosen because we'll write a plain Unlit HLSL shader later. It requires less boilerplate than URP shaders.

---

## 2. Scene Setup

The default scene (`SampleScene`) already has a **Main Camera**. You only need to configure it:

1. Select **Main Camera** in the Hierarchy
2. In the Inspector:
   - **Projection:** Orthographic ← should already be set for 2D projects
   - **Size:** `5` (this means the camera shows 10 world units tall; adjust later if needed)
   - **Background:** set to solid black (`R:0 G:0 B:0`)

---

## 3. Create the Player GameObject

In the Hierarchy, right-click → **Create Empty**, name it `Player`.

Add these components in the Inspector:

### 3a. Sprite Renderer
- Add Component → **Sprite Renderer**
- **Sprite:** click the circle picker → type "Circle" → select `Knob` (Unity's built-in filled circle sprite)
  - Alternatively: right-click in Project → **Create → Sprites → Circle** to generate a clean circle sprite
- **Color:** White (`R:255 G:255 B:255`)
- **Order in Layer:** `0`

### 3b. Rigidbody 2D
- Add Component → **Rigidbody 2D**
- **Body Type:** `Kinematic`
- **Collision Detection:** `Continuous`
- Under **Constraints:** check **Freeze Rotation Z**

> Kinematic because we're controlling position manually. No gravity, no physics forces acting on the player.

### 3c. Circle Collider 2D
- Add Component → **Circle Collider 2D**
- **Is Trigger:** ✅ checked
- **Radius:** `0.5` (matches the default circle sprite size at scale 1,1,1)

### 3d. Transform
- **Position:** `(0, 0, 0)` — center of screen
- **Scale:** `(1, 1, 1)` — gives a circle with radius 0.5 world units (diameter = 1 unit)

---

## 4. Player Script

Create a new C# script: right-click in Project → **Create → C# Script**, name it `PlayerController`.

Attach it to the `Player` GameObject.

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;

    private Rigidbody2D rb;
    private float leftBound;
    private float rightBound;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        float playerRadius = GetComponent<CircleCollider2D>().radius * transform.localScale.x;
        float camHalfWidth = Camera.main.orthographicSize * Camera.main.aspect;

        leftBound  = -camHalfWidth + playerRadius;
        rightBound =  camHalfWidth - playerRadius;
    }

    void FixedUpdate()
    {
        float input = 0f;
        if (Input.GetKey(KeyCode.A)) input = -1f;
        else if (Input.GetKey(KeyCode.D)) input = 1f;

        Vector2 newPos = rb.position + Vector2.right * (input * moveSpeed * Time.fixedDeltaTime);
        newPos.x = Mathf.Clamp(newPos.x, leftBound, rightBound);

        rb.MovePosition(newPos);
    }
}
```

**What this does:**
- `Start` — computes world-space left/right screen bounds accounting for the player's radius so it never goes off-screen
- `FixedUpdate` — reads A/D input, moves the player horizontally by `moveSpeed * deltaTime`, then clamps to bounds
- `MovePosition` — moves the kinematic Rigidbody2D properly (respects physics, collision callbacks will fire correctly later)

**Inspector tunables:**
| Field | Default | Notes |
|-------|---------|-------|
| `moveSpeed` | `5` | World units per second; tweak to feel |

---

## 5. Verify It Works

Hit **Play**. You should see:
- White circle in the center of the black screen
- A/D moves it left and right
- Circle stops at screen edges and doesn't go off-screen
- No vertical drift

---

## What's Deliberately Left Out of This Step
- Mouse rotation (added in Step 2)
- Black/white split shader (added in Step 3)
- Orb spawning (added in Step 4)
- Health / GameManager (added in Step 5)

---

## File Index Update
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | This file — project creation + player movement |
| `SETUP_STEP2.md` | _(next) Mouse rotation + split circle shader_ |
