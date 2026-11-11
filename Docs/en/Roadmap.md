# Barracks & Bolts – Priorities and Schedule

**English** | [Türkçe](../tr/Roadmap.md)

> **Status (11 November):** P0 (brief), P0+ and P1 are done. From P2: the tower, territory, villager construction, the full Sandbox, rally points, hunger, the effects pass, the farm, difficulty selection and shortcuts. 521 tests pass; SetPass is 16 in a 150-unit battle. **Next:** play Normal mode end to end once, a Windows build from a clean clone and a release, the README last.
> How the systems work in code: [Systems.md](Systems.md)

> **Deadline:** Friday, 13 November 2026
> **Main rule:** No priority level starts before the previous one is finished. The `main` branch always runs and always builds.
> Detailed design decisions: [GDD_Decisions.md](GDD_Decisions.md)

---

## P0 – Brief Requirements (must have)
When this level is done, every item of the brief is covered. **Target: Tuesday, 10 November, evening.** A build is tagged `v1.0-pdf` at this point.

- [x] Project skeleton: folder structure, scene hierarchy, naming rules, technical document (+ DI scopes, EventBus, simulation loop, Bootstrap/MainMenu/Gameplay scenes)
- [x] Grid system (A* and building placement share the same grid). A fixed or simple map at this stage
- [x] Camera: pan and zoom, clamped to the map, works with any aspect ratio (Cinemachine)
- [x] Data-driven building and unit definitions (ScriptableObject: name, visual, size, HP, damage). Visuals live in color sets (`TeamColorDefinition`)
- [x] **Production Menu:** infinite scroll view + object pooling
- [x] **Building placement:** preview, valid cells green, invalid cells red
- [x] Barracks (100 HP) and House (stands in for the Power Plant, 50 HP). At least 1–2 more buildings (Castle, Tower; their behavior comes in P2)
- [x] **Information Panel:** the selected building's picture. For a producing building, the list of units it can make
- [x] 3 soldiers produced from the Barracks (10 HP; 10 / 5 / 2 damage), no production time, no limit. Spawn point (nearest free cell if taken)
- [x] Selection: left click and box selection
- [x] **A\* pathfinding:** move with right click, walk around buildings, a group spreads around the target
- [x] **Attack:** right click a unit or building to attack it
- [x] Removal at 0 HP: selection and panel are cleared, grid cells are freed, attackers drop the target
- [x] **Minimal Sandbox:** placing enemy units and buildings. There is no friendly fire, so testing attacks depends on this
- [x] Main menu (starting the Sandbox and quitting are enough)
- [x] SetPass < 20 check: 10 in Stats during a battle; 16 in a 150-unit battle after every feature
- [x] Resizable Windows build
- [ ] README: how to play, controls, architecture overview, how the brief's items are covered

## P0+ – Requests From Playtesting (8 November)
Usability requests that came up while playing. They must work in the Sandbox too and come before the build.

- [x] **Floating text (generic):** Text that appears in the world, drifts up and fades. First use: a warning when a building is placed on invalid cells
- [x] **Building name on hover:** The name shows while the mouse is over a building, with a hover effect on it
- [x] **Information Panel size:** Sizes to its content between a minimum and a maximum. Content that doesn't fit scrolls, with a draggable scrollbar on the right. Anchored to the top, which also fixed overflow on wide screens
- [x] **Production Menu tooltip:** Hovering a cell shows the building's info (size, HP, what it produces). The cost line came with costs in P1
- [x] **Collapsible Production Menu:** Starts closed and slides in from the left with DOTween. Slides away while something is being placed and comes back afterwards, so it doesn't cover the map
- [x] **Esc and closing panels:** Each Esc closes the top layer: placement, selection, Production Menu. The Information Panel has a close button in its corner
- [x] **Resource bar:** Wood, stone, food, gold and population at the top. All ∞ in the Sandbox. The rest of the UI moved below the bar
- [x] **Map:** 64x64 island: water, foam, flat ground, three plateaus, stairs. Soldiers climb a plateau only by its stairs; buildings can go right up to a plateau's edge. Trees and resources came in P1
- [x] **Unit card:** Selecting soldiers shows a small card at the bottom center that doesn't catch the mouse. The big panel opens only for buildings

## P1 – Game Loop (with this level it becomes a "game")
**Target: Wednesday, 11 November, evening.**

- [x] **Random resources on the map:** seeded; forests, stone quarries, gold mines, sheep flocks
- [x] **First slice of Normal mode:** Play in the menu; start with a Castle, 3 villagers and starting resources; counters; building and soldier costs, a warning when short; villagers gather, carry to the Castle, resources run out
- [x] **Game loop (the items below):** built and played; not balanced yet
- [x] **Status icons:** a "Zz" bubble over idle villagers, a red "!" over a soldier that spots an enemy, a small resource icon over a villager carrying a load
- [x] Sheep breed: 3 new sheep every morning, 30 at most
- [x] **First playtest notes:** villagers work right next to the resource, run to the Castle when enemies come near, show an icon when idle; sheep roam and die in one cut; the name label sits over the target
- [x] Villager: produced from the Castle, only in Normal mode
- [ ] Building and unit costs shown on Production Menu cells (they are in the tooltip)
- [x] Population: the Castle and Houses give capacity, the bar shows `used/capacity`
- [x] Calendar: day/night cycle, night darkening, raid days, time stops during a raid
- [x] Enemy AI: marches on the Castle, attacks what is in its way, knocks down buildings that block the road. Idle soldiers attack nearby enemies too
- [x] Wave system and raid notices (no sound and no minimap; the direction is given in text, and a clickable arrow at the screen edge points at a wave that is off screen)
- [x] Win and lose conditions and the game-over screen
- [ ] Balancing the game loop (times, costs, wave sizes)
- [x] Test Runner: 520 tests pass (the 8 failures of the first run were fixed)
- [x] Final SetPass measurement: in the +100 Units battle (150 units) SetPass 16, 231 draw calls, 511 FPS in the editor

## P2 – Depth (as time allows, in this order)
**Target: Thursday, 12 November.**

1. [x] Tower: the archer on it fires on its own (archer count and strength would come with upgrades)
2. [ ] Building upgrades (HP, population, tower strength, soldier level, storage capacity)
3. [ ] Seeded random map generation with path validation (resources are already seeded; the ground is fixed)
4. [ ] Minimap
5. [ ] Monastery and Monk (heals on its own)
6. [x] Villager construction: in Normal mode a building is placed as a grey construction site and villagers finish it, with a progress bar over it. Tool cursors over resources and sites. (Repair and cancel later)
7. [ ] Storage buildings and capacity
8. [x] Hunger: every morning each unit eats; without enough food, slow HP loss; values by difficulty
9. [x] Territory and the Outpost (radius upgrade would come with upgrades). Range and territory rings on the selected building
10. [ ] Villagers sheltering in Houses too, with shelter capacity per building (sheltering in the Castle is done)
11. [x] Rally point: right click with a production building selected; new units walk there
12. [x] Full creative Sandbox: building, unit and resource tabs, Delete, time control, Enemy AI and Debug (grid + A* path) switches, raids on demand, clearing the map, a +100 unit test battle and a stats panel (FPS, A* time, SetPass)

## P3 – Polish (if there is time; otherwise listed under "Future Plans" in the README)
- [ ] Save system (manual saves, several slots)
- [x] Difficulty levels: chosen after Play; raid size, starting resources, hunger; a button in the Sandbox
- [ ] Endless mode
- [x] Settings menu (window mode, VSync, UI size, camera speed, edge scrolling, FPS panel; audio settings once there is audio)
- [x] UI polish: outlined and shadowed text materials, button hover/press animation, resource count punch, main menu background and entrance animations, FPS panel at the bottom right
- [ ] Sound and music
- [ ] Tutorial
- [x] Shortcuts: control groups, Attack Move (F), Stop (X), Hold Position (H), idle villager (.), camera to the Castle (Home); F1 Controls page, command buttons over the unit card, keys in tooltips
- [x] Farm: 3x3, 40 wood, one villager, never runs out, 5 food every 6 seconds
- [ ] Enemy settlements
- [x] Effects pass: damage on the attack's hit frame, on arrival for arrows, death and destruction effects, health bars (when hurt or selected)
- [x] Difficulty choice on the main menu

## Before Delivery
- [x] Scene separators: `----- CAMERAS -----` renamed, `----- WORLD -----` moved to the root
- [x] Company Name: `Akin Can Hancer`
- [x] Removing the one-off tool under `Assets/Editor` and the empty folders (Animations, Animators, Audio, Scripts/Editor)
- [ ] Fresh Windows build, zipped on a GitHub Release (without `_BurstDebugInformation_DoNotShip`), `v1.0-pdf` tag
- [ ] README (last)

---

## Daily Plan
| Day | Date | Goal |
|---|---|---|
| Saturday | 7 November | Finish the GDD, review the assets, technical document, project skeleton, grid |
| Sunday | 8 November | Camera, data definitions, Production Menu (infinite scroll + pooling), building placement, Information Panel |
| Monday | 9 November | Selection, A\*, movement, spawning, attack, HP and removal, minimal Sandbox |
| Tuesday | 10 November | P0 polish: edge cases, SetPass, resolution, README. **`v1.0-pdf` build** |
| Wednesday | 11 November | P1: resources, villagers, population, calendar, enemy AI, waves, winning and losing |
| Thursday | 12 November | P2 items in order. **Feature freeze in the evening** |
| Friday | 13 November | Bug fixes only, final build, README update, testing, **delivery by e-mail** |

## Risk Rules
- If P0 isn't done by Tuesday evening, P1 doesn't start. The missing P0 item is finished first.
- A new system that would be left half done isn't started. A half-done system is either switched off with a feature flag or not merged into `main`.
- A working Windows build is made at the end of every day.
- No new features after Thursday evening.
- Features that don't make it are listed under "Future Plans" in the README. That also shows the depth of the design.
