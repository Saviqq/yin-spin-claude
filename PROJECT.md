# Game Jam Project — "Black and White"

## Meta
- **Theme:** Black and White
- **Scope:** 48-hour game jam
- **Engine:** Unity (developed on personal device, code examples created here as .md files)
- **Workflow:** All design docs and code examples are written here; no actual Unity project files on this machine

---

## Core Concept

**Working title:** *Yin & Spin* (placeholder)

The player controls a circle split 50/50 black and white. Smaller circles (black or white) spawn at screen edges, bounce around, and despawn after a timer. The player must match the correct half of their circle to collect each small circle — white half collects white circles, black half collects black circles.

**The twist:** Every collection shifts the color split on the player's circle. Collecting white expands the white area (shrinks black). Collecting black expands the black area (shrinks white). The player must balance both colors to stay effective, while avoiding wrong-color contacts.

---

## Player Controls
| Input | Action |
|-------|--------|
| A / D | Move left / right (horizontal only, no vertical movement) |
| ← / → | Rotate the player circle left / right |

---

## Core Mechanic — Color Balance

- Player circle is divided into two halves by a diameter line (think yin-yang split, but clean 50/50 initially)
- The split ratio is represented as a single float: `colorRatio` (0.0 = fully black, 1.0 = fully white, 0.5 = balanced)
- Collecting a **white** circle: `colorRatio += collectDelta`
- Collecting a **black** circle: `colorRatio -= collectDelta`
- The visible split angle follows `colorRatio` — the arc of white vs black changes accordingly
- If `colorRatio` approaches 0 or 1, the player is mostly one color and vulnerable to the opposite

### Contact Rules
- Correct color contact → collect (score + ratio shift)
- Wrong color contact → lose 1 heart

### Health
- Player starts with **3 hearts**
- Wrong-color orb contact → −1 heart
- 0 hearts → game over

---

## Small Circle (Orb) Behavior
- Spawn at a random point on the edge of the screen (any side)
- Move in a random direction inward
- Bounce off all four screen edges (simple reflection)
- Despawn after a set lifetime (e.g., 5–8 seconds)
- Each orb is purely black or purely white
- Size is smaller than the player circle

---

## Spawning System
- **Independent random spawns** — each orb is independently black or white (50/50 random)
- Spawn rate increases over time as part of the difficulty curve
- Max number of active orbs on screen at once — TBD (balancing)

---

## Win / Lose Conditions
- **Loop:** Endless arcade survival — no win state, game gets progressively harder over time
- **Score:** Points per orb correctly collected
- **Lose:** Reach 0 hearts (3 wrong-color contacts)
- **Goal:** High score

---

## Visual Style
- Stark black and white only — no color, no grey (on theme)
- Clean geometric shapes (circles only?)
- Minimal UI — just the ratio indicator and score
- The player circle's split visually shifts as ratio changes (arc fill)

---

## Audio (TBD)
- Simple SFX: collect (white), collect (black), wrong contact, spawn
- Music: optional, low priority for jam scope

---

## Technical Notes (Unity)
- 2D project
- **Split circle visual:** Unlit HLSL shader — polar coordinate angle comparison against `colorRatio` + player rotation, `smoothstep` edge
- **Split circle collision:** Single static `CircleCollider2D` on player; on orb contact, compute angle from player center to orb position, compare against current `colorRatio` and `player.rotation` to determine which color half was hit — no dynamic collider geometry needed
- Physics: bouncing orbs via `Rigidbody2D` with velocity reflection off screen edges
- Camera: static/fixed — the play area is the full screen
- Scene: single scene, game loop managed by a `GameManager`

---

## Open Questions / To Discuss
1. ~~Wrong-color contact consequence~~ → −1 heart (3 hearts total)
2. ~~Player circle radius~~ → fixed radius, never changes
3. ~~Spawn balance~~ → independent random spawns (50/50 per orb)
4. ~~Timer or endless loop~~ → endless arcade, progressively harder
5. ~~Shader approach~~ → Unlit HLSL shader + angle-check collision
6. ~~Rotation~~ → free 360°

---

## File Index (to be created as project grows)
| File | Contents |
|------|----------|
| `PROJECT.md` | This file — overview and design doc |
| `MECHANICS.md` | Deep-dive on balance system, numbers, formulas |
| `ARCHITECTURE.md` | Unity scene structure, scripts overview |
| `CODE_EXAMPLES.md` | Unity C# snippets for key systems |
| `SHADER.md` | Split-circle shader implementation |
