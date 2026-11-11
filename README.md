# Barracks & Bolts

**English** | [Türkçe](README.tr.md)

A 2D medieval real-time strategy game made in Unity 6: build a base, run a villager economy, survive ten days of raids, or play freely in a creative sandbox.

It started as a one-week "strategy game demo" case study for a game developer role. Every item of that brief is covered (see [Brief Coverage](#brief-coverage)), and the rest of the week went into turning the demo into a small, complete game with a clean, tested architecture.

<!-- Screenshots / GIF: add images under Docs/Images and link them here. -->

**Unity 6.3 LTS (6000.3.16f1)** · URP 2D · VContainer · UniTask · DOTween · Cinemachine 3 · Input System · **521 EditMode tests** · **16 SetPass in a 150-unit battle**

---

## Highlights
- **Two modes:** a *Normal* game with resources, population, a day/night calendar and raid waves, and a *Sandbox* where anything can be placed anywhere, for either side.
- **RTS controls:** box selection, right-click context orders (move, attack, gather, build, farm), Attack Move, Stop, Hold Position, control groups, idle villager key, rally points.
- **Villager economy:** gathering wood, stone, gold and food, carrying loads to drop-offs, construction sites built by villagers, farms, hunger, sheltering in the Castle during raids.
- **Combat:** A\* pathfinding on a grid with plateaus and stairs, groups spreading around their target, hit frames and flying arrows, towers that fire on their own, an enemy AI that marches on the Castle and breaks through walls.
- **Difficulty:** Easy, Normal and Hard change raid size, starting resources and hunger. All values live in data assets.
- **Polish:** health bars, death and destruction effects, status icons over units, a raid arrow at the screen edge, tooltips with keys, a Controls page (F1), settings saved on the spot.

## Play
- **Windows build:** download the latest zip from the [Releases page](https://github.com/akncn57/Barracks-And-Bolts-Case-Study/releases), unzip and run `Barracks & Bolts.exe`. The window can be resized freely.
- **From source:** open the project with **Unity 6000.3.16f1**. Packages, including VContainer from its Git URL, come in through the Package Manager. Open `Assets/Scenes/Bootstrap.unity` and press Play. Opening `Gameplay.unity` directly also works; it starts a Sandbox game with default settings.

### Game modes
| Mode | What it is |
|---|---|
| **Play** (Normal) | Pick a difficulty, start with a Castle and 3 villagers, gather and build, and survive the raids of days 3, 6, 8 and 10. Lose the Castle and the game is over; beat the raid of day 10 and you win. |
| **Sandbox** | Unlimited resources, instant buildings. Place any building, unit or resource for either side, start raids by hand, switch the enemy AI and a debug view on and off, spawn a 100-unit test battle, clear the map. |

## Controls
Every key is also listed in game on the **Controls** page (F1, or Settings → Controls).

| Input | Action |
|---|---|
| W A S D / arrow keys, mouse at the screen edge, middle mouse drag | Move the camera |
| Mouse wheel | Zoom |
| Left click / left drag | Select a unit or building / select your units in a box |
| Right click | Move, attack, gather, build or farm, depending on what is under the mouse. With a production building selected: set its rally point |
| **F**, then left click | Attack Move: walk there and fight every enemy met on the way |
| **X** / **H** | Stop / Hold Position |
| **Ctrl + 1–9** / **1–9** | Save the selection as a group / select a group (press twice to move the camera to it) |
| **.** (period) | Select the next idle villager |
| **Home** | Move the camera to the Castle |
| **Delete** | Remove what is selected |
| **Shift** + left click | Place several buildings in a row |
| **P** / **+** **-** | Pause / game speed (1x, 2x, 3x) |
| **Esc** | Close the top layer (placement, window, selection, menu); open the settings when nothing is left |
| **F1** | Controls page |

## Brief Coverage
| Requirement | Where it is |
|---|---|
| Production menu as an **infinite scroll view with object pooling** | `RecyclingScrollView`: only the visible rows + 1 exist; rows are recycled, the offset wraps so float precision never drifts. No `ScrollRect`. |
| Building placement with a **green / red preview** | `PlacementService` + `PlacementPreviewView`: every footprint cell is checked (map bounds, occupied, units in the way, territory, cost). |
| **Barracks** (100 HP) and **Power Plant** (50 HP) | Barracks, and the **House** in the Power Plant's role (population, no production). Castle, Tower, Outpost and Farm on top. |
| **Information panel** with the building's picture and its units | `InformationPanelView`: picture, name, live HP, the list of units to produce. |
| **3 soldiers**, 10 HP, 10 / 5 / 2 damage, no production time | Knight, Spearman, Archer, values in data assets; spawned at the building's spawn point, or the nearest free cell. |
| Selection and **right-click movement with A\*** | Own `AStarPathfinder` (8 directions, no corner cutting, allocation-free); groups spread over separate cells around the target. |
| **Right-click attack** on units and buildings, removal at 0 HP | `CombatService` / `CombatSystem`; on death the grid, selection, panel and attackers are cleared through events. |
| Data-driven definitions (ScriptableObjects) | `BuildingDefinition`, `UnitDefinition`, color sets for the sprites, a `GameDatabase`. |
| **Resizable window**, **SetPass < 20** | Resizable windowed build; 16 SetPass / 231 draw calls in a 150-unit battle. |

## Architecture
```
Definition (ScriptableObject) ──reads──► Service / System (plain C#) ──publish──► EventBus ──► View (MonoBehaviour)
                                              ▲                                                   │
                                              └────────── Input handlers / UI call ◄──────────────┘
```
- **Logic in plain C#, Unity only for views.** Models (`UnitModel`, `BuildingModel`) and services have no MonoBehaviour dependency, which is what makes 521 EditMode tests possible without scenes.
- **Dependency injection with VContainer.** A root scope for the whole app, one per scene, and a child UI scope for the gameplay canvas. Assembly definitions enforce the direction `UI → Gameplay → Core → Utils`; Gameplay can't see UI.
- **One central tick.** `SimulationRunner` runs every system in fixed phases (Time → AI → Movement → Gathering → Combat → Cleanup) on a `GameClock` that owns pause and game speed. `Time.timeScale` is never touched, so UI keeps animating while the game is paused, and deaths are handled together at the end of a tick.
- **Typed event bus.** `readonly struct` events, disposable subscriptions, and publishing doesn't allocate. Events report what happened; commands are plain method calls.
- **Ids instead of references.** Entities refer to each other by `GameEntityId`, so a dead target is never a dangling reference.
- **No Animator.** Units play sprite frames from their color set on the game clock. Side (`Faction`) and color (`TeamColor`) are independent, so any color can be the enemy.
- **Data first.** HP, damage, costs, build times, raid waves, difficulty multipliers and hunger all live in assets. A new building or unit needs no code.

More: [Technical Design](Docs/en/Technical_Design.md) · [Systems Guide](Docs/en/Systems.md)

## Performance
- Every in-game sprite is in a few Sprite Atlases with one sprite material. Health bars, progress bars and status icons are SpriteRenderers in the gameplay atlas, not world-space canvases.
- Static and dynamic UI are on separate canvases; text effects are TMP material presets on one font atlas.
- Arrows, floating text and effects are pooled. A\* and BFS searches reuse their buffers.
- **150 units fighting: 16 SetPass, 231 draw calls.** A\* searches take well under 0.01 ms on average. The in-game stats panel (Settings → FPS Panel) shows FPS, A\* timing and SetPass live.

## Tests
**521 EditMode tests** across the grid, pathfinding, placement, territory, construction, combat, the gathering loop, raids, difficulty, hunger, shortcuts and UI math.
Run them from *Window → General → Test Runner → EditMode → Run All*.

## Project Structure
```
Assets/Scripts/
├── Core/       EventBus, simulation loop, game clock, scenes, session and player settings
├── Gameplay/   grid, pathfinding, buildings, units, combat, economy, raids, camera, input
├── UI/         menus, panels, HUD, infinite scroll, tooltips, settings
├── Popup/      popup system
├── Utils/      MinHeap, CompositeDisposable
└── Tests/      EditMode tests
```

## Documentation
Every document comes in English and Turkish, with the same file names.

| Document | English | Türkçe |
|---|---|---|
| Game design decisions, with "current state" notes | [GDD_Decisions](Docs/en/GDD_Decisions.md) | [GDD_Decisions](Docs/tr/GDD_Decisions.md) |
| Technical design: architecture, rules, naming | [Technical_Design](Docs/en/Technical_Design.md) | [Technical_Design](Docs/tr/Technical_Design.md) |
| Systems guide: how each system works in code | [Systems](Docs/en/Systems.md) | [Systems](Docs/tr/Systems.md) |
| Priorities and the one-week schedule | [Roadmap](Docs/en/Roadmap.md) | [Roadmap](Docs/tr/Roadmap.md) |
| Assets: what was missing and how it was made | [Missing_Assets](Docs/en/Missing_Assets.md) | [Missing_Assets](Docs/tr/Missing_Assets.md) |

To find your way around the code, start with the [Systems Guide](Docs/en/Systems.md): every section names its files and tests.

## Future Plans
Designed but not built yet; the [Systems Guide](Docs/en/Systems.md#g-planned-systems) describes how each would fit in.
- Building upgrades and unit ranks
- Storage buildings and resource capacity
- Monastery and the healing Monk
- Repairing buildings and cancelling construction
- Sheltering in Houses with capacity per building
- Minimap (one texture from grid data, no second camera)
- Procedural ground generation from the seed
- Saving and loading
- Endless mode
- Sound and music
- A separation force between walking units
- Tutorial

## Credits
- **Art:** [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) by Pixel Frog. The storage buildings, Outpost, farm, construction sites, icons, cursors and markers were made from the pack's own sprites. The art is used under the pack's license and isn't covered by anything in this repository.
- **Font:** MedievalSharp, SIL Open Font License.
- **Libraries:** [VContainer](https://github.com/hadashiA/VContainer) (MIT), [UniTask](https://github.com/Cysharp/UniTask) (MIT), [DOTween](http://dotween.demigiant.com/) (Demigiant), Unity's Cinemachine, Input System, URP, Test Framework and TextMesh Pro.

## Author
Akın Can Hançer
