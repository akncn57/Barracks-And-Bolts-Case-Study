# Barracks & Bolts – Missing Assets

> Priorities are the same as in [Roadmap.md](Roadmap.md).
> **Solution** column: 👤 Added by the user · 🛠 Made from existing assets · 💻 Solved in code or with a simple sprite · 🔍 To be found

## Existing UI Assets and Their Use (not missing)
| Asset | Use |
|---|---|
| `Texture_UI_Icons_Hammer` | Build / repair button |
| `Texture_UI_Icons_Wood`, `_Gold`, `_Meat` | Resource icons (wood, gold, food) |
| `Texture_UI_Icons_Sword`, `_Shield` | Attack / defense (stats on the information panel) |
| `Texture_UI_Icons_Arrow_Green`, `_Arrow_Orange` | Forward / back buttons |
| `Texture_UI_Icons_Cancel` | Cancel, close, invalid |
| `Texture_UI_Icons_Settings`, `_Info`, `_Music` | Settings, info, audio settings |
| `Avatars/<Color>/Texture_UI_Avatars_<Color>_<Unit>` | Unit portraits in both colors: Knight, Spearman, Archer, Monk, Villager. Assigned to the units in the color sets (there is no Monk unit yet) |
| `Texture_Cursors_Default`, `_Pointer`, `_Forbidden` | Default cursor, clickable (hand), forbidden |
| `Texture_Cursors_Selection` (corner frame) | Selection / target marker |
| `Texture_UI_Tools_Hammer`, `_Axe`, `_Sword`, `_Pickaxe` | Cursor states: build/repair, wood, attack, mining. 32x32 cursor copies were made of the axe, pickaxe and hammer: `Textures/Cursors/Texture_Cursors_Axe/Pickaxe/Hammer`; `Texture_Cursors_Sword` for Attack Move, from the UI sword icon |
| `Texture_UI_Bars_Big_*`, `_Small_*` | Health bar, progress bar |
| `Texture_UI_Ribbons_Small_<Color>_<Fork/Point>`, `_Banners_*`, `_Papers_*`, `_WoodTable_*` | Panel backgrounds, headers, slots |

---

## P0 – Needed for the Brief
| # | Missing | Why It's Needed | Solution |
|---|---|---|---|
| 1 | ~~Units in a second color~~ | ✅ Yellow set added (Warrior, Lancer, Archer, Monk, Pawn) | – |
| 2 | ~~Buildings in a second color~~ | ✅ Yellow set added (Castle, Barracks, House, Monastery, Tower, Archery) | – |
| 2a | ~~Yellow `Pawn_Interact_Axe`~~ | ✅ Added | – |
| 3 | ~~Placement preview (green/red cell)~~ | ✅ `Texture_Markers_PlacementCell`, the color comes from code | – |
| 4 | ~~Selection marker (ring/ellipse under the unit)~~ | ✅ Made: `Texture_Markers_SelectionRing` | – |
| 5 | ~~Box selection rectangle~~ | ✅ Made: `Texture_Markers_SelectionBox` (a sliced SpriteRenderer in the world) | – |
| 6 | ~~Move/attack order marker~~ | ✅ `Texture_Markers_SelectionRing`: green for move, red for attack | – |
| 7 | ~~Death and destruction effect~~ | ✅ Taken from the pack's Particle FX folder: `Texture_VFX_Dust_01/02`, `_Explosion_01/02`, `_Fire_01/02/03`, `_Water_Splash` (sliced frame by frame) | – |

## P1 – Game Loop
| # | Missing | Why It's Needed | Solution |
|---|---|---|---|
| 8 | ~~Stone icon~~ | ✅ From the pack's rocks (no shadow, outlined): `Texture_UI_Icons_Stone` | – |
| 9 | ~~Population icon~~ | ✅ From the grey villager avatar: `Texture_UI_Icons_Population` | – |
| 10 | Day / night / calendar icon | Calendar UI | 🔍 / 🛠 |
| 11 | ~~Raid warning icon and screen-edge direction arrow~~ | ✅ The pack's `Texture_UI_Icons_Arrow_Orange`, rotated, with `Texture_UI_Icons_Sword` in its middle (`Prefab_UI_Raid_Arrow`) | – |
| 12 | ~~Sheep cutting cursor~~ | ✅ The axe cursor on sheep too (`Texture_Cursors_Axe`) | – |
| 13 | ~~Villager carrying resources~~ | ✅ A small resource icon over the villager's head: `Textures/Markers/Texture_Markers_Carry_*` (scaled down from the UI icons, in the Gameplay atlas) | – |
| 14 | Gathering and damage numbers (e.g. "+5", "-10") | Feedback | 💻 The floating text system is ready (`FloatingTextSpawner`, used for warnings and "+5 Wood" on delivery); damage numbers not yet |
| 15 | ~~Main menu background~~ | ✅ See 31 | – |
| 16 | Victory / defeat screen art | Game over | 🛠 Banner + Ribbon |

## P2 – Depth
| # | Missing | Why It's Needed | Solution |
|---|---|---|---|
| 17 | ~~Storage buildings (4)~~ | ✅ Made: the Archery shed (targets removed) + a single resource pile in the yard. `Texture_Buildings_<Color>_Storage_Wood/Food/Gold/Stone` | – |
| 18 | ~~Outpost~~ | ✅ Tower + a flag in the team color: `Texture_Buildings_<Color>_Outpost` | – |
| 19 | ~~Building under construction~~ | ✅ Grey copies of the buildings: `Texture_Buildings_<Color>_<Building>_Site` (House, Barracks, Tower, Outpost). Starts see-through and darkens with work. A progress bar over it: the pack's small bar frame was copied into the Gameplay atlas and a gold fill was made (`Texture_Markers_ProgressBar_Frame/Fill`) | – |
| 20 | ~~Damaged building effect~~ | ✅ `Texture_VFX_Fire_*` (not used yet). Death and destruction use `Texture_VFX_Dust_01/02` and `_Explosion_02` | – |
| 20a | ~~Health bar~~ | ✅ Made in code: `Textures/Markers/Texture_Markers_HealthBar_Frame` (24x8, sliced) and `_Fill` (white, colored by code) | – |
| 21 | Rank / level badge (soldier and building level) | Upgrade system | 🛠 / 💻 A small star or stripes |
| 22 | ~~Territory border art~~ | ✅ In code: a single texture over the map, `TerritoryOverlayView` | – |
| 22a | ~~Range and territory ring~~ | ✅ In code: `CoverageRingView` draws its own ring texture | – |
| 23 | Minimap frame | Minimap | 🛠 `Papers_*` / `WoodTable_*` 9-slice |
| 24 | ~~Archer on the tower~~ | ✅ `Archer` frames on the tower's platform, by team color | – |
| 25 | ~~Rally point flag~~ | ✅ Made from a ribbon: `Textures/Markers/<Color>/Texture_Markers_<Color>_RallyFlag` (added to the atlas) | – |
| – | ~~Debug path dot~~ | ✅ An 8x8 dot drawn in code: `Textures/Markers/Texture_Markers_PathDot` (in the atlas) | – |
| – | ~~Unit status icons~~ | ✅ Drawn in the units' palette: `Textures/Markers/Texture_Markers_Status_Idle`, `_EnemySpotted` (in the atlas through their folder) | – |

## P3 – Polish
| # | Missing | Why It's Needed | Solution |
|---|---|---|---|
| 26 | ~~Farm~~ | ✅ Seedlings from the pack's bush sprites, a row of wheat, surrounded by the Archery fence: `Textures/Buildings/Neutral/Texture_Buildings_Neutral_Farm` | – |
| 27 | **Music:** main menu, day, raid | Atmosphere | 🔍 (CC0: OpenGameArt, Pixabay) |
| 28 | **SFX:** UI click, building placement, construction, chopping, mining, sword hit, arrow shot, death, building collapse, horn, healing, victory, defeat, error | Feedback | 🔍 (Kenney CC0 packs) |
| 29 | Tutorial markers (highlight frame, arrow) | Tutorial | 🛠 Selection cursor + arrow icon |
| 30 | Save slot and ~~settings panel~~ art | Saving | 🛠 `Banners_Slots`, `Papers_*`. Settings panel ✅ made with `WoodTable` and red buttons |
| 31 | ~~Main menu background~~ | The menu looked flat | ✅ Composed from the game's own tiles and sprites: `Textures/Backgrounds/Texture_Backgrounds_MainMenu` |

---

## Most Important Gaps (these first)
No critical assets are missing for P0, P1 and P2. Still open: a day/night icon (the calendar is shown as text), victory and defeat screen art, a rank badge and a minimap frame (both would come with their systems), music and sound effects.

## Pack File Not Copied Into the Project
- `Shadow.png` (Tiny Swords, Terrain/Tileset): the shadow under high ground. The map doesn't use shadows, so it isn't needed.

## License Note
The source and license of every outside asset are in the "Credits" section of the README.
