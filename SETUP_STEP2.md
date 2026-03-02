# Step 2 — Mouse Rotation & Split Circle Shader

Goal: Player circle rotates to face the mouse (free 360°) and visually renders as a black/white split circle driven by a custom HLSL shader. `colorRatio` is fixed at 0.5 for now — collection mechanic wires it up later.

---

## What Changes From Step 1

| Thing | Step 1 | Step 2 |
|-------|--------|--------|
| Visual | Sprite Renderer (white circle) | Quad mesh + custom shader material |
| Rotation | Frozen | Follows mouse via `rb.MoveRotation` |
| Rigidbody2D | Freeze Rotation Z: ✅ | Freeze Rotation Z: ❌ unchecked |
| PlayerController | Movement only | + rotation + material reference |

---

## 1. Create the Shader

In the Project window: right-click → **Create → Shader → Unlit Shader**. Name it `SplitCircle`.

Open it, **delete everything**, and paste this:

```hlsl
Shader "Custom/SplitCircle"
{
    Properties
    {
        _ColorRatio ("Color Ratio", Range(0,1)) = 0.5
    }
    SubShader
    {
        Tags
        {
            "Queue"           = "Transparent"
            "RenderType"      = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv     : TEXCOORD0;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv  : TEXCOORD0;
            };

            float _ColorRatio;

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv  = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // Shift UV origin to circle center
                float2 c = i.uv - 0.5;

                // Discard pixels outside the circle with a soft edge
                float dist  = length(c);
                float alpha = 1.0 - smoothstep(0.49, 0.5, dist);
                if (alpha < 0.001) discard;

                // Convert to polar angle, map to [0, 1]
                // t = 0 → left side   t = 0.5 → right side
                // increases counter-clockwise
                float angle = atan2(c.y, c.x);            // -PI to PI
                float t     = angle / UNITY_TWO_PI + 0.5; // 0 to 1

                // White sector occupies [0, _ColorRatio], black occupies the rest
                float isWhite = step(t, _ColorRatio);

                fixed4 col = lerp(fixed4(0,0,0,1), fixed4(1,1,1,1), isWhite);
                col.a = alpha;
                return col;
            }
            ENDCG
        }
    }
    FallBack "Transparent/VertexLit"
}
```

**How the shader works:**
1. Shifts UV so (0.5, 0.5) becomes the center
2. Clips to a circle using `smoothstep` on the distance from center
3. Converts each pixel's position to a polar angle, normalised to [0,1]
4. Pixels whose angle falls below `_ColorRatio` are white, the rest are black
5. The split line is hard-edged — crisp and clean, fits the black/white aesthetic

> The split is defined in **local UV space**. When the GameObject rotates (from mouse input), the mesh rotates and takes the split with it. The shader never needs to know the rotation — the Transform handles it automatically.

---

## 2. Create the Material

In the Project window: right-click → **Create → Material**. Name it `SplitCircleMat`.

In the Inspector:
- **Shader** dropdown → select `Custom/SplitCircle`
- **Color Ratio:** `0.5`

---

## 3. Update the Player GameObject

### 3a. Remove Sprite Renderer
Select Player → In the Inspector, right-click **Sprite Renderer** → **Remove Component**.

### 3b. Add Mesh Filter
- Add Component → **Mesh Filter**
- Click the **Mesh** field → search for `Quad` → select it

> Unity's built-in Quad is a 1×1 unit flat plane with UV (0,0)–(1,1). Matches the CircleCollider2D radius of 0.5 perfectly.

### 3c. Add Mesh Renderer
- Add Component → **Mesh Renderer**
- Expand **Materials** → slot 0 → assign `SplitCircleMat`

### 3d. Update Rigidbody 2D
- Under **Constraints**: uncheck **Freeze Rotation Z**
  - We froze it in Step 1 to block physics from spinning the player
  - We now rotate manually via code, so unfreeze it

---

## 4. Updated PlayerController.cs

Replace the entire script with this updated version:

```csharp
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float colorRatio = 0.5f; // wired up to collection mechanic later

    private Rigidbody2D  rb;
    private Material     splitMaterial;
    private float        leftBound;
    private float        rightBound;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();

        // Cache material instance (unique per player, not shared asset)
        splitMaterial = GetComponent<MeshRenderer>().material;
        splitMaterial.SetFloat("_ColorRatio", colorRatio);

        // World-space screen bounds, inset by player radius
        float playerRadius  = GetComponent<CircleCollider2D>().radius * transform.localScale.x;
        float camHalfWidth  = Camera.main.orthographicSize * Camera.main.aspect;
        leftBound  = -camHalfWidth + playerRadius;
        rightBound =  camHalfWidth - playerRadius;
    }

    void Update()
    {
        RotateTowardsMouse();
    }

    void FixedUpdate()
    {
        MoveHorizontal();
    }

    // --- Private ---

    void RotateTowardsMouse()
    {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector3 dir        = mouseWorld - transform.position;
        float   angle      = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        rb.MoveRotation(angle);
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

    // Called later by collection logic
    public void UpdateColorRatio(float newRatio)
    {
        colorRatio = Mathf.Clamp01(newRatio);
        splitMaterial.SetFloat("_ColorRatio", colorRatio);
    }

    // Called later by collision detection to determine which half was hit
    public float GetColorRatio() => colorRatio;
}
```

**What's new vs Step 1:**
- `RotateTowardsMouse()` — converts mouse screen position to world space, computes angle to player, applies via `rb.MoveRotation` (physics-safe rotation on a Kinematic body)
- `splitMaterial` — caches a **material instance** (`.material` not `.sharedMaterial`) so changing `_ColorRatio` only affects this player, not the asset on disk
- `UpdateColorRatio()` — public method ready for the collection mechanic in Step 4
- `GetColorRatio()` — public getter ready for collision angle-check in Step 4
- `Update` / `FixedUpdate` split — rotation in `Update` (input-responsive), movement in `FixedUpdate` (physics-safe)

---

## 5. Verify It Works

Hit **Play**. You should see:
- Circle in the center, rendered as a black/white split (right half white, left half black at start)
- Moving the mouse rotates the circle so the split follows the cursor
- A/D moves left and right, circle still clamps to screen edges
- Rotation and movement work simultaneously

**Quick sanity checks:**
| Test | Expected |
|------|----------|
| Mouse to the right | White half faces right |
| Mouse above player | Circle rotated so white half points up |
| Hold A | Moves left, stops at edge |
| Hold D + move mouse | Both work at the same time |

---

## Note on `colorRatio` and the Split Direction

At `colorRatio = 0.5` the circle is split exactly 50/50. The boundary is a straight line through the center. The "seam" of the polar angle mapping (where t wraps from ~1 back to ~0) always falls on one end of the split line — this is not an artifact, it is the split line.

As `colorRatio` changes from 0.5:
- Toward 1.0 → white sector grows, black shrinks
- Toward 0.0 → black sector grows, white shrinks
- At 0.0 or 1.0 → fully one color (player is extremely vulnerable)

---

## What's Deliberately Left Out of This Step
- Collection/collision logic (Step 4)
- `colorRatio` actually changing at runtime (Step 4)
- Orb spawning (Step 3)
- Health / GameManager (Step 5)

---

## File Index
| File | Status |
|------|--------|
| `PROJECT.md` | Design doc |
| `SETUP_STEP1.md` | Project setup + player movement |
| `SETUP_STEP2.md` | This file — shader + mouse rotation |
| `SETUP_STEP3.md` | _(next) Orb spawning + bouncing_ |
