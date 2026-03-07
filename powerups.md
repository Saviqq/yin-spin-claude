Okay I implemented the plan, check the changes and update your docs.

Now let's begin work on last big system for the game -> powerups (previously boons/afflictions)

Slowly I want to implement this 10 powerups:
- clear all existing orbs (without getting score points)
- get +5 score immediately
- player circle is bigger but it's slower (movement and rotation) for 5 seconds
- player circle is smaller but it's faster (movement and rotation) for 5 seconds
- balance player color split by one color delta -> if split is 70/30 white/black it should be 60/40 after picking up this powerup
- gain 1 gearth
- spawn 3 - 8 orbs (random color for each)
- change all existing orbs to one color (black or white) for 5 seconds (than get back to it's previous color)
- expand the walls again (increase player area)
- stop all existing orbs and stop spawning them for 5 seconds

Basically how it's going to work. There should be some system/manager that will spawn random powerup every 10 seconds (modifiable)
somewhere randomly inside play area. If player picks it up, power-up game object is removed from the scene and power-up is applied.

For start, let's do the core work (spawning the power-up and it's pick up) and one effect -> gaining the hearth. 
Also I have already prepared sprite for powerup that has multiple frames, so I want to do animated sprite that will loop between
all of it's frames while it's alive. 
I want you to create implementation plan for this (and later others) power-ups in powerup-1.md.