# Barracks & Bolts – GDD Decisions

**English** | [Türkçe](../tr/GDD_Decisions.md)

> Source: the brief (PDF) of a "Game Developer – Strategy Game Demo" case study. Where the text says "brief", it means an item the brief requires.
> This file records the game design decisions. "Current state" notes describe what is in the game; the rest is the intended design.
> How the code works: [Systems.md](Systems.md)

## Decisions Made
- **Theme:** Medieval (the free asset pack at hand is medieval).
- **Castle = headquarters (HQ).** Losing conditions are in "Calendar and Waves".
- **Buildings in the pack:** Castle, House, archery range, tower, a church-like building. More can be added later.
- **Building sizes and values** were set while going through the asset pack. The brief's values are examples; the game may use its own.

### Game Modes
- **Normal Mode:** Resources, population and the calendar are on.
- **Sandbox Mode (Creative):** Similar to Minecraft's Creative mode. The player can put anything anywhere. It covers the brief's rules one to one and lets a reviewer test the mechanics easily.
  - Unlimited resources, no population limit, instant production and construction.
  - A placement palette: every building, every unit (player and enemy side), resources (trees, stone, mines, sheep).
  - No territory limit; buildings can go anywhere.
  - The calendar and raids can be switched on and off. A raid can also be started by hand.
  - A delete tool and time control (pause, speed up).
  - The player can set up their own battles: their army against enemy units and buildings.
  - **Current state:** The sandbox tools are in a single *Sandbox* panel under the "Production" button, in two groups: Settings (Side, Difficulty, Enemy AI, Debug) and Actions (Start Raid, +100 Units, Clear Map). Side switches the side of new buildings and units. The Production Menu has *Buildings*, *Units* and *Resources* tabs: buildings, units (Knight, Spearman, Archer, Villager) and resources (tree, rock, gold mine, sheep) go straight onto the map, several in a row with Shift. Production from the Barracks is there too (brief). Delete removes everything selected, or the resource under the mouse when nothing is selected. Pause and 1x/2x/3x are on the resource bar. "Enemy AI" makes enemies and idle soldiers attack on their own (off by default). "Debug" shows the grid, obstacles and the A* paths of the selected units. There is no calendar in the Sandbox; a raid comes with "Start Raid" and switches the AI on. "Clear Map" empties the map (two clicks). "+100 Units" sets up two armies and lets them fight. The stats panel at the bottom right (both modes, switched in the settings) shows FPS, A* search time and SetPass.
- **Difficulty (Normal Mode):** Easy, Normal, Hard. Affects the number of enemies, enemy strength and starting resources. The values live in a `DifficultySettings` ScriptableObject.
  > **Current state:** Chosen after Play on the main menu. Raid groups ×0.6 / ×1 / ×1.5, starting resources ×1.5 / ×1 / ×0.75, hunger 1 / 2 / 4 food. Enemy strength (HP, damage) doesn't change; the brief's values stay. There is no separate `DifficultySettings`: each value sits in the asset of its own area (`Data_Config_Raids`, `Data_Config_NormalMode`). In the Sandbox, the *Difficulty* button on the Sandbox panel changes the raid size.

### Population (the Power Plant's counterpart = House)
- The player starts with 1 population, which is 1 villager.
- Building Houses raises the population capacity.
- **Soldiers and villagers take up population.**
- House: 50 HP, no production menu. The exact counterpart of the brief's Power Plant.
- **Current state:** The player starts with 3 villagers; the Castle and each House give 5 capacity. The bar shows `used/capacity`. No limit in the Sandbox.

### Economy
- Resources: **Wood, Stone, Food, Gold** (others could come later).
- Used for: construction, feeding the population (food), building upgrades.

| Resource | Source on the Map | Use |
|---|---|---|
| Wood | Trees | Construction, upgrades |
| Stone | Rocks | Construction, upgrades |
| Food | **Sheep** (a villager cuts them) and the **farm** (one villager works it, it never runs out) | Feeding the population |
| Gold | **Gold mines** | Building upgrades (spent together with other resources) |

- **Building upgrades:** Cost a set amount of gold and other resources.

### Upgrade Effects
| Building | Gains per Level |
|---|---|
| All buildings | More maximum HP |
| House | More population capacity |
| Tower | More damage and more archers on it |
| Monastery | Stronger healing for the Monks it trains |
| Storage | More storage capacity |
| Barracks | Higher level for the soldiers it produces |

- **Brief compliance:** Level 1 soldiers keep the brief's values (10 HP, 10 / 5 / 2 damage). Higher levels go above them.
- **Decision:** The Barracks has no separate soldier capacity. Only the population limit caps the army.
- **Decision:** A Barracks upgrade affects only soldiers produced afterwards. Level belongs to each soldier and shows on it (e.g. a rank badge). Barracks of different levels produce soldiers of different levels.
- **Current state:** 3 villagers at the start; the Castle produces villagers, only in Normal mode. A right click sends them to a resource. The only drop-off is the Castle. The Castle and each House give 5 population. The numbers are provisional: in `Data_Config_NormalMode`, `Data_Config_Raids` and the definition assets.
- **Gathering (villager):**
  1. The player selects a villager with a left click.
  2. Over a resource (tree, stone, food) the cursor becomes a gathering cursor. **Current state:** in Normal mode it changes even without a villager selected: an axe over trees and sheep, a pickaxe over stone and gold.
  3. The player clicks the resource. The villager walks there with A* and starts gathering.
  4. It carries the load to the nearest suitable storage (or the Castle) and comes back. Details in "Storage System".

### Calendar and Waves (Provisional Values)
These values are set in a ScriptableObject and can change later.
- **1 day = 3 minutes:** 2 minutes of day, 1 minute of night.
- **Total length:** 10 days.
- **Raid days:** 3, 6, 8 and 10. A raid can start at any moment of that day (day or night).
- **Time stops during a raid:** The day/night cycle doesn't move on. When every wave is beaten, it goes on from where it stopped.
- Every raid has 2–3 waves. Later raids bring more and stronger enemies.
- **Victory:** Beating the raid of day 10 wins the game.
- **Endless Mode:** The victory screen offers "Continue". If the player goes on, ever harder raids keep coming at intervals. The number of days survived is shown on screen.
- **Raid Notices:**
  1. On the morning of a raid day a "Raid today!" notice appears.
  2. When the raid starts, a "Raid!" warning appears and a horn plays. The direction the enemy comes from shows on the minimap and as arrows at the screen edge.
- **Defeat:** The game is lost if the Castle falls, or if the population drops to 0 and there isn't enough food for the Castle to produce a new villager.

### Units
| Unit | HP | Damage | Produced At | Note |
|---|---|---|---|---|
| Villager | 10 | 1 | Castle | Gathers resources |
| Knight | 10 | 10 | Barracks | Brief's Soldier 1 |
| Spearman | 10 | 5 | Barracks | Brief's Soldier 2 |
| Archer | 10 | 2 | Barracks | Brief's Soldier 3, ranged |
| Monk | 10 | – | Monastery | Extra unit, heals allies |

### Building Roles
| Building | Role |
|---|---|
| Castle | Headquarters. Produces villagers; resources are carried here. Losing it loses the game. |
| Barracks | Produces knights, spearmen and archers. Soldiers appear at its spawn point. |
| House | Raises population capacity. No production menu (the Power Plant's counterpart). |
| Tower | The archers on it guard an area of a set radius and fire at enemies in range on their own. They are part of the building, take no population and can't be selected one by one. **Current state:** one archer, range 7, 3 damage, one shot per second (provisional). Hits units only, not buildings. Keeps its target until it dies or leaves range. Works in every mode and for both sides. The archer count would grow with upgrades. |
| Monastery | Trains Monks. |
| Lumber Storage | Stores wood. Villagers drop wood here. |
| Stone Storage | Stores stone. |
| Granary | Stores food. |
| Treasury | Stores gold. |
| Outpost | Extends the territory. Upgrading it grows the radius. |

### Storage System
- Every resource has its own storage building. Where the pack has none, it is made from existing assets.
- **Resource capacity:** At most as much of a resource as the total capacity of its storages can be held. When full, villagers stop gathering and wait, and the UI says "Storage full".
- **The Castle** gives a small starting capacity for every resource and is a drop-off for all of them, so the game is playable without storage at the start.
- A villager carries its load to the **nearest** storage that takes that resource (or the Castle). This makes building storage near resources a strategic choice.
- If a storage falls, the capacity drops. Resources above the capacity are lost.
- **Sandbox Mode:** Unlimited capacity.

## Asset Mapping (Tiny Swords – 64px tiles)
| Game Element | Asset | Sprite (px) | Suggested Footprint (cells) |
|---|---|---|---|
| Castle | `Texture_Buildings_Castle` | 320x256 | 5x3 |
| Barracks | `Texture_Buildings_Barracks` | 192x256 | 3x3 |
| Monastery | `Texture_Buildings_Monastery` | 192x320 | 3x3 |
| Tower | `Texture_Buildings_Tower` | 128x256 | 2x2 |
| House | `Texture_Buildings_House` | 128x192 | 2x2 |
| Storage (Wood, Food, Gold, Stone) | `Texture_Buildings_<Color>_Storage_*` (Archery shed + a resource pile in the yard, made) | 192x256 | 3x3 (to be confirmed) |
| Villager | `Pawn` (Idle, Run, Axe, Pickaxe, Hammer) | 192 frames | 1x1 |
| Knight | `Warrior` | 192 frames | 1x1 |
| Spearman | `Lancer` | 320 frames | 1x1 |
| Archer | `Archer` (+ Arrow) | 192 frames | 1x1 |
| Monk | `Monk` (+ Heal VFX) | 192 frames | 1x1 |
| Sheep | `Sheep` | 128 frames | 1x1 |
| Wood | `Tree1–4` (animated) → runs out, a `Stump` stays | 192x256 frames | 1x1 |
| Gold | `Gold Stone 5–6` (+ Highlight) | 128x128 | 2x1 |
| Stone | `Rock1–4`. A few rocks together form a quarry | 64x64 | 1x1 (each rock) |
| Ground | `Tilemap_color1–5`, `Water Background`, `Water Foam`, `Water Rocks` | 64 tiles | – |

- **Folders are by color:** `Buildings/<Color>/Texture_Buildings_<Color>_<Building>`, `Units/<Color>/<Unit>/Texture_Units_<Color>_<Unit>_<Animation>`. Colorless assets have their own folders: `Units/Sheep`, `Units/Projectiles` (arrow), `VFX`.
- Asset names in the table are written without the color prefix (e.g. `Castle` → `Texture_Buildings_Red_Castle`).
- The **footprint** is the area a building takes on the grid. The top of the sprite (the roof) goes beyond the footprint and covers the cells above it (pivot at the bottom).
- What the pack didn't have was made: storage buildings (4), the Outpost, the farm. Sound and music are still missing. The second color (yellow) of units and buildings was added.
- In-game sprites use 64 PPU (1 tile = 1 Unity unit = 1 grid cell). UI and cursor textures stay at 100.
- **Archery:** Doesn't produce soldiers; to match the brief, the Barracks produces all three (knight, spearman, archer). The Archery's shed, with its targets removed, became the shared look of the four storage buildings. The Outpost is a Tower with a flag in the team color.
- There is water on the ground for sure: it is unwalkable. If the tileset has raised ground and cliffs, they can be used too.

## Gameplay Rules

### Construction
- **Normal Mode:** A building appears as a scaffold (half built) the moment it is placed, and its cost is paid then. Selected villagers walk to it with A* and build it over time. More villagers build faster.
- A building under construction starts with low HP and can be attacked. Its HP fills as the work goes on.
- Until finished, a building can't produce and gives no population.
- Cancelling construction refunds part of the cost (e.g. 50%).
- **Sandbox Mode:** Construction is instant. This covers the brief's "no production time" item.
- **Current state:** A building is placed as a grey, see-through construction site with 10% HP. With a villager selected, a right click on the site (the cursor turns into a hammer) sends it to work right next to the building with a hammer; every 4–7 seconds it moves to another spot around it. Times for one villager: House 20, Barracks 35, Tower 30, Outpost 30 seconds (`buildTime`). No production, population, tower fire or territory until it is finished. When done, the villager goes idle. Cancelling and repair aren't there yet.

### Hunger
- Food is eaten every morning, per unit of population.
- Without enough food, hunger starts and every unit slowly loses HP at intervals.
- When food comes again, the HP loss stops.
- Hunger can bring the population down to 0. See "Calendar and Waves" for the losing condition.
- The UI warns when food runs short.
- **Current state:** Values by difficulty (`Data_Config_NormalMode` > Hunger): Easy 1 food per unit per day and 1 HP every 15 seconds, Normal 2 and 10 s, Hard 4 and 5 s. Nobody eats on the first morning. Missing food becomes a debt; new food pays the debt first, and the loss stops.

> **Current state:** Enemies come from the coast. Later raids are bigger and have more knights; there is no HP or damage multiplier, the brief's values stay. Difficulty selection is done; endless mode, sound and the minimap aren't.

### Enemy AI
- When a raid starts, enemies appear in waves at spawn points on the map's edge.
- They walk to the Castle with A*. On the way they attack player units or buildings that come into range.
- When their target dies or leaves range, they go on to the Castle.
- If walls and buildings block the way, they knock down the nearest obstacle (edge case: the Castle is walled in completely).

### Resource Depletion
- Trees, rocks and gold mines hold a limited amount. A depleted resource is removed from the map and its grid cells become walkable (A* updates).
- A set number of sheep come back every morning, up to a maximum on the map. **Current state:** 3 sheep every morning except the first, at most 30 (`Data_Config_NormalMode`).
- A sheep moves around in small steps inside its own cell. It dies in one cut and all its food (40) is carried at once, regardless of the villager's carry capacity.
- A villager whose resource runs out moves on to a nearby resource of the same kind. If there is none near, it waits idle.

## Map (Procedural Generation)

### General
- A new map is generated at the start of every game. Generation is **seed** based: the same seed gives the same map, which makes debugging and testing easier.
- A seed can be typed in on the main menu, or "Random" chosen.
- The map is grid based, and A* and building placement share the same grid. Default size: 64x64 cells (adjustable).
- The ground is drawn with Unity's **Tilemap**. A tilemap with one material and one atlas keeps SetPass low.

> **Current state:** The scene has a fixed map: an island in a 64x64 area, two ponds, three irregular plateaus (one with a cliff facing the sea). No shadows. The ground is fixed; random ground generation is P2. **The resources, however, are spread randomly by seed in every game:** free space for the Castle in the middle, one of each resource near it; stair entrances stay open, and no resource can cut the map in two.

### Generation Steps
1. **Ground:** Perlin noise makes grass, dirt and (if any) water/cliff areas. Water and cliffs are unwalkable.
2. **Castle:** Placed on flat, open ground near the middle of the map, with guaranteed free building space around it.
3. **Starting resources (fair start):** Within a set radius of the Castle there is always at least 1 forest, 1 rock group, 1 gold mine and a few sheep.
4. **Outer resources:** Forests are spread in clusters with Perlin noise. Stone and gold go to random points at a minimum distance from each other and from the Castle (Poisson-disk-like). Far resources can be richer, which pushes the player to expand.
5. **Enemy spawn points:** 2–4 points on the map's edges, each at a minimum distance from the Castle.
6. **Validation:** Flood fill / A* checks that every spawn point can walk to the Castle and to all starting resources. If not, the obstacle is cleared or the map is generated again with a new seed.

### Edge Case Notes
- Building and resource placement checks every grid bound (nothing goes off the map).
- Resources can't block spawn points or the area around the Castle.
- Generation must be deterministic: `System.Random(seed)` is used, never the global state of `UnityEngine.Random`.
- Settings (size, resource density, spawn count) live in a `MapGenerationSettings` ScriptableObject.

### The Enemy Side and Color Choice
- Enemy troops are the same as the player's: Knight, Spearman, Archer and Monk. Only their colors differ.
- **Color doesn't mean side:** The player picks their color in the new game settings, and the enemy takes one of the rest. The code never assumes "red = enemy". Side (`Faction`) and color (`TeamColor`) are separate concepts.
- Every color has a **color set** (ScriptableObject): that color's building and unit sprites. A new color needs only a new folder and a new set.
- Colors available: **Red** and **Yellow** (both complete sets).
- The running-with-an-axe animation isn't used. The villager always runs with the normal `Run` animation.
- The color sets differ a little: Yellow has `Warrior_Guard`, Red doesn't. If an animation is missing, the code must fall back to the default one (Idle / Run).
- **No friendly fire:** The player can't order an attack on their own units or buildings.
- **Fighting back:** An idle unit attacks whoever hits it. A unit that is walking or already has a target carries on.
- **Status icons:** A cream "Zz" bubble sits over an idle villager. When a player's soldier spots an enemy by itself, or is hit and fights back, a red "!" shows over it for 1.5 seconds; not for an attack the player ordered, and not for a soldier that fought in the last 3 seconds (in a big battle it only marks the start of the fight). A villager carrying a load to a drop-off has a small icon of the resource (wood, stone, meat, gold) over it, which disappears on delivery. Icons don't hide behind trees and buildings.
- **Enemy settlements (to be discussed later):** The map could have small random enemy settlements. The brief's "attack a building" item would then be covered through enemy buildings too.

### Moving Together
- Several units sent to the same point don't stack. They spread over the free cells around the target.

### Spawn Point and Rally Point
- Every production building (Castle, Barracks, Monastery) has a spawn point.
- If the spawn cell is taken or blocked, the unit appears in the nearest free cell.
- A building can't be placed on cells with units on them; the preview shows those cells red.
- A right click with a building selected sets a rally point. New units walk there on their own. **Current state:** Done. If the point is on a resource or an enemy, the new unit acts as on a right click (a villager gathers, a soldier attacks). While the building is selected, a flag in the team color stands at the point.

### Villagers Fleeing
> **Current state:** During a raid a villager keeps working; when an enemy comes within 7 cells, it runs into the Castle. Its load counts at that moment. When no enemies are left on the map, it comes out and goes back to work. An order from the player comes before fleeing for 10 seconds. There is no sheltering in Houses and no building capacity; losing the Castle ends the game, so nobody has to come out of it.

- A villager under attack drops its work and runs to the nearest House or the Castle.
- Inside, it is hidden and can't be attacked. When the danger is over (the raid is beaten or no enemy is near), it comes out and returns to its previous work on its own.
- If the building it shelters in falls, the villagers inside come out.
- Every building has a shelter capacity. If a building is full, the villager goes to the next nearest one.

### Territory System
- There is a **territory** of a set radius around the Castle. The player can place buildings only inside their own territory.
- **Outpost:** A new building that extends the territory. Placed inside the territory near its edge, it opens a new territory around itself. Upgrading it grows the radius. Look: a Tower + a flag in the team color (`Texture_Buildings_<Color>_Outpost`). Its name and flag on the information panel tell it apart from the defense tower.
- **The Tower** opens a small territory too, which ties defense and expansion together.
- In placement mode the territory border shows on the ground. Outside the territory is marked red as invalid (consistent with the brief's red-area item).
- If an Outpost or Tower falls, the territory shrinks. Buildings left outside stay and keep working, but nothing new can be placed there.
- Gathering and unit movement aren't limited by territory; the whole map can be walked.
- Territory is computed on the grid: every cell knows whether it is inside. This is recomputed only when a territory source (Castle, Outpost, Tower) is added or removed.
- **Area to be opened:** While an Outpost or Tower is being placed, the area it would open shows green on the ground before it is placed. Not shown outside the territory, since it can't be placed there.
- **Coverage ring:** When a building is selected, and while a Tower is being placed, its reach shows as rings: orange for attack range, cream for territory. On an enemy building only the range shows; the Sandbox has no territory ring.
- **Current state:** Radius: Castle 12, Outpost 12, Tower 4 (provisional, set by `territoryRadius` in the definition asset). The whole building must be inside the territory. The Sandbox has no territory and no Outpost in its menu. The Outpost upgrade would come with the upgrade system.

### Minimap
- A minimap in a corner of the screen. It shows the ground, resources, buildings, units (player and enemy in different colors), territory borders and the camera's view.
- Clicking the minimap moves the camera there. A right click sends the selected units there.
- **SetPass note:** The minimap isn't drawn with a second camera but with a single `Texture2D` built from grid data. A second camera would draw the whole scene again and double the draw calls. Units are dots on the texture, updated at intervals.

### Monk
- Picks and heals hurt allies in range **on its own**. The player doesn't need to point at a target.

### Repair
- Villagers can repair damaged buildings that haven't fallen. Repair costs resources.
- **Manual:** Select a villager and right click the damaged building.

### Night
- No effect other than raids for now. Only the scene darkens.

### Game Flow
1. **Main Menu:** New Game, Continue / Load (if there is a save), Settings, Quit.
2. **New Game:** First the mode (Normal or Sandbox), then the game settings (seed etc.). Then the world is built and the game starts.
3. **In game:** The pause menu gives Continue, Save, Settings and Main Menu.
4. **Game Over:** A Victory or Defeat screen with stats, Play Again and Main Menu.

### Save System
- A save holds the seed, the day and time, resources, buildings (type, level, HP, position), units (type, level, HP, position, current order) and the amounts left in the map's resources.
- The map is rebuilt from the seed and the changes are applied on top, which keeps the save file small.
- The format is JSON, written under `Application.persistentDataPath`.
- **Manual saves** only, no autosave. **Several save slots.**

### Settings
- The usual settings: volume (master, music, effects), resolution, window mode, VSync, edge scrolling on/off. Extras to be discussed later.
- **Current state:** Opened by the Settings button on the main menu, the gear on the right of the resource bar in a game, and Esc. Window mode, VSync, UI size (90% / 100% / 115%), camera speed, edge scrolling and the FPS panel; every change is saved at once. In a game the game pauses while it is open, and the way back to the main menu is here. Audio settings will come with audio.

### Sound
- No sound or effects yet. To be added later, found or made.

### Tutorial
- A full tutorial is planned. Its steps will be set once the game is settled.

### General Notes
- Every number (HP, damage, attack speed, range, costs, times) lives in ScriptableObjects and is set while moving to code.
- The UI layout is discussed separately, based on the user's sketch.
- Folder structure and coding standards are in a separate technical document.
- SetPass < 20, the resizable window and the Canvas settings are checked throughout development.

## Input Map (Unity Input System)
The keys follow the usual shortcuts of RTS games such as Age of Empires, StarCraft and Warcraft III.
Items tagged **[Brief]** are inputs the brief requires.

### Action Map: `Camera`
| Action | Key | Type | Note |
|---|---|---|---|
| `Pan` | WASD / Arrow keys | Value (Vector2) | |
| `EdgePan` | Mouse at the screen edge | Value (Vector2) | Off by default; can be turned on in the settings |
| `DragPan` | Hold middle button + drag | Button + Delta | |
| `Zoom` | Mouse wheel | Value (float) | Min/max zoom limits |
| `FocusSelection` | Space | Button | Centers the camera on the selected unit |
| `FocusCastle` | H | Button | Centers the camera on the Castle |

### Action Map: `Gameplay`
| Action | Key | Type | Note |
|---|---|---|---|
| `Select` | Left click | Button | **[Brief]** Selects a unit or building |
| `BoxSelect` | Hold left click + drag | Button + Position | Selects only our own units |
| `AddToSelection` | Shift + Left click | Modifier | Adds to or removes from the selection |
| `SelectAllOfType` | Double left click / Ctrl + Left click | Button | Selects all units of that type on screen |
| `Command` | Right click | Button | **[Brief]** Depends on context: move on ground, attack on an enemy, gather on a resource (with a villager selected) |
| `QueueCommand` | Shift + Right click | Modifier | Adds a waypoint |
| `AttackMove` | A + Left click | Button | Moves on, attacking enemies met on the way |
| `Stop` | S | Button | Cancels the current order |
| `HoldPosition` | X | Button | Stays in place, hits only what comes into range |
| `ControlGroupAssign` | Ctrl + 1–9 | Button | Assigns the selection to a control group |
| `ControlGroupSelect` | 1–9 | Button | Selects the group. Pressed twice, the camera goes to the group |
| `IdleVillager` | . (period) | Button | Selects an idle villager and moves the camera to it (as in AoE) |
| `DeleteSelected` | Delete | Button | Destroys the selected own building or unit (asks for confirmation) |

### Action Map: `Placement` (active in building placement mode)
| Action | Key | Type | Note |
|---|---|---|---|
| `Confirm` | Left click | Button | **[Brief]** Places the building on a valid area. Invalid areas show red |
| `ConfirmAndContinue` | Shift + Left click | Modifier | Keeps placing the same building after placing one |
| `Cancel` | Right click / Esc | Button | Leaves placement mode |

### Action Map: `UI` / General
| Action | Key | Type | Note |
|---|---|---|---|
| `Pause` | Esc / P | Button | Opens the pause menu. Cancels the active mode first |
| `GameSpeed` | + / − | Button | Game speed 1x, 2x, 3x (optional) |
| `ToggleHealthBars` | Alt (held) | Button | Shows every health bar |

> **Current state:** Done: `Pan`, `DragPan`, `Zoom`, `EdgePan` (off in the settings), `Select`, `BoxSelect`, `Command`, all three of the Placement map, closing layers with Esc, `DeleteSelected` (Delete; in Normal mode only own units and buildings, never the Castle, no confirmation), `Pause` (P, pausing only, no in-game menu), `GameSpeed` (+/-, 1x–3x), control groups (Ctrl+1–9, 1–9, a double press moves the camera), `IdleVillager` (.), camera to the Castle (**Home**, not H). The camera pans with WASD, so A and S aren't free; that's why **AttackMove is F** (then left click), **Stop X**, **HoldPosition H**. These can also be given with the mouse through the buttons over the unit card, whose tooltips show the keys. The full list is on the Controls page that F1 opens (also in the settings). Not done: `AddToSelection`, `SelectAllOfType`, `QueueCommand`, `FocusSelection`, `ToggleHealthBars`.

### Edge Case Notes
- While the mouse is over UI (`EventSystem.IsPointerOverGameObject`), world inputs (Select, Command, Placement) don't work.
- While placement mode is on, the `Gameplay` map is off. Switching between modes is handled in one place.
- During pause only the `UI` map stays active.
- When the window is resized, edge pan and box select are recomputed in screen coordinates.
- Every key is defined in the Input Actions asset. Rebinding in the settings could come later.

## Open Questions
1. ~~Asset pack: which building is the Barracks, sizes, HP and costs.~~ Decided; the values are in the definition assets (provisional, may change with balancing).
2. ~~Farm rules.~~ Decided and done: 3x3, 40 wood, one villager works it, never runs out, 5 food every 6 seconds.
3. Upgrade costs and the maximum number of levels.
4. Calendar and wave values will be confirmed after testing.
