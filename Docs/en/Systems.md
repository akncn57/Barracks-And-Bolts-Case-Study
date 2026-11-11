# Barracks & Bolts – Systems Guide

**English** | [Türkçe](../tr/Systems.md)

> Describes **how** every system of the game is built in code: what is done and what is planned.
> Rules and architecture: [Technical_Design.md](Technical_Design.md) · Game design: [GDD_Decisions.md](GDD_Decisions.md) · Priorities: [Roadmap.md](Roadmap.md) · Assets: [Missing_Assets.md](Missing_Assets.md)
>
> **Last updated:** 11 November 2026 · carried resource icon, Production Menu sliding away while placing, raid direction arrow, shortcuts (F/X/H, control groups, ., Home, F1) and the command bar, Controls page, difficulty choice, Sandbox panel, hunger notice fix, "!" only for calm soldiers · 521 EditMode tests, all passing
>
> **Status marks:** ✅ done · 🟡 partly · ⏳ planned

## How is this document kept up to date?
- When a system is added or changed, its section is updated in the same piece of work.
- When a planned system is done, it moves out of "Planned Systems" into its own section, marked ✅.
- Every section follows the same order: **what it does**, **flow**, **important rules**, **files, data and tests**.
- Numbers (HP, times, costs) appear here only as examples. The real values are in the definition and config assets.

---

## Contents
**A. Foundations**
1. [Layers](#1-layers)
2. [Startup and Scopes](#2-startup-and-scopes)
3. [What Happens in a Frame?](#3-what-happens-in-a-frame)
4. [EventBus and the Event Map](#4-eventbus-and-the-event-map)
5. [Data and Game Modes](#5-data-and-game-modes)

**B. World**
6. [Map and Grid](#6-map-and-grid)
7. [Resources](#7-resources)
8. [Camera](#8-camera)
9. [Pathfinding](#9-pathfinding)

**C. Buildings**
10. [Placement](#10-placement)
11. [Building Model and View](#11-building-model-and-view)
12. [Territory and the Outpost](#12-territory-and-the-outpost)
13. [Tower Defense](#13-tower-defense)
14. [Coverage Rings](#14-coverage-rings)

**D. Units**
15. [Unit Production](#15-unit-production)
16. [Selection and Hover](#16-selection-and-hover)
17. [Commands and Movement](#17-commands-and-movement)
18. [Combat and Removal](#18-combat-and-removal)
19. [Unit View and Status Icons](#19-unit-view-and-status-icons)

**E. Normal Mode**
20. [Resources and Costs](#20-resources-and-costs)
21. [Population](#21-population)
22. [Gathering](#22-gathering)
22b. [Construction](#22b-construction)
22c. [Farm](#22c-farm)
22d. [Hunger](#22d-hunger)
23. [Calendar and Night](#23-calendar-and-night)
24. [Raids and Battle AI](#24-raids-and-battle-ai)
25. [Shelter](#25-shelter)
26. [Game Over](#26-game-over)

**F. Interface**
27. [Main Menu](#27-main-menu)
28. [Production Menu](#28-production-menu)
29. [Information Panel and Unit Card](#29-information-panel-and-unit-card)
30. [HUD](#30-hud)
31. [Feedback](#31-feedback)
32. [Esc Layers](#32-esc-layers)
32b. [Settings and Popups](#32b-settings-and-popups)
32c. [Text Materials and UI Animations](#32c-text-materials-and-ui-animations)
33. [Sandbox](#33-sandbox)
33b. [Time Control and Deleting](#33b-time-control-and-deleting)

**G. [Planned Systems](#g-planned-systems)**

**H. [Recipes: Adding Something New](#h-recipes-adding-something-new)**

**I. [Known Limits](#i-known-limits)**

---

# A. Foundations

## 1. Layers
**Status:** ✅

```
Definition (ScriptableObject)  ──reads──►  Service / System (plain C#)  ──publish──►  EventBus  ──►  View (MonoBehaviour)
                                               ▲                                                   │
                                               └──────── Input handler / UI calls ◄────────────────┘ (a View may read a service)
```
- **Definition:** Design data that doesn't change. Never changed at runtime.
- **Model:** Runtime state (`BuildingModel`, `UnitModel`, `ResourceNodeModel`). Plain C#.
- **Service:** Applies the rules, holds the models. Example: `BuildingService.TryPlace`.
- **System:** A service that runs every frame (`ISimulationSystem`). Example: `CombatSystem`.
- **View:** Draws the model. No rules. A service never talks to a View; the View reads the service or listens to events.
- **Input handler:** Turns the player's intent into a service call.
- Systems refer to each other's entities by `GameEntityId`, not by object. A dead target's id comes back empty from `TryGet`; no reference is left hanging.
- Assembly direction: `UI → Gameplay → Core → Utils`. Gameplay can't see UI; the compiler guards this rule.

## 2. Startup and Scopes
**Status:** ✅ · **Files:** `Core/Bootstrap/*`, `Core/Scenes/*`, `Core/Session/*`, `Gameplay/Bootstrap/GameplayLifetimeScope.cs`, `UI/Bootstrap/GameplayUiLifetimeScope.cs`

```
App starts ─► VContainerSettings builds the root scope prefab (SceneLoader, GameSessionSettings)
Bootstrap scene ─► BootstrapFlow.Start ─► MainMenu loads
MainMenu ─► Play / Sandbox ─► mode and a new seed written to GameSessionSettings ─► Gameplay loads
Gameplay ─► GameplayLifetimeScope (services, systems, world views)
         └► GameplayUiLifetimeScope (UI views), parent = GameplayLifetimeScope
```
- `GameSessionSettings` carries the mode, color, difficulty and seed from the menu into the game. If the Gameplay scene is opened directly in the editor, it runs with defaults (Sandbox, red, a random seed).
- `RegisterComponentInHierarchy<T>()` looks for that component in the scene; if it isn't found, the scope isn't built and the whole game stops. Adding a new scene view = putting it in the scene + registering it in the scope.
- `IStartable` runs after the scope is built; `IInitializable` during the build. So that views in the scene don't miss events, start-up work (`ResourceMapInitializer`, `NormalModeStarter`) is `IStartable`.
- `EventBus` lives in the Gameplay scope; when the scene closes, every subscription goes too.

## 3. What Happens in a Frame?
**Status:** ✅ · **Files:** `Core/Simulation/*`

```
1. SimulationRunner.Tick        (ITickable)      GameClock.Advance → systems in phase order
       Time(100)        CalendarService, HungerSystem
       AI(200)          RaidService, BattleAiSystem, ShelterSystem, AttackMoveSystem
       Movement(300)    UnitMovementSystem, ResourceRoamSystem
       Gathering(350)   GatheringSystem, ConstructionSystem, FarmingSystem
       Combat(400)      CombatSystem, BuildingDefenseSystem
       Cleanup(500)     DeathCleanupSystem
2. MonoBehaviour Update                          CameraController moves the camera
3. IPostTickable                                 Placement, Selection, Command, Hotkey, Hover, Delete, TimeControl, Escape input handlers, ControlsLauncher
4. LateUpdate                                    Views read the models and draw
```
- **`GameClock`** is the only owner of game time: `IsPaused`, `TimeScale`, `ElapsedTime`. A frame counts as 0.1 s at most. `Time.timeScale` never changes; UI and DOTween keep running while the game is paused.
- **Why input afterwards?** Handlers must see where the camera is this frame; otherwise the mouse lags a frame behind. An order enters the simulation the next frame, which isn't noticeable.
- **Why deaths in Cleanup?** So the order of two units hitting each other in the same frame can't change the result, and nothing is removed from a dictionary while it is being walked.
- **New system:** implement `ISimulationSystem`, pick a `Phase`, register it with `builder.RegisterSimulationSystem<T>()`.

## 4. EventBus and the Event Map
**Status:** ✅ · **Files:** `Core/Events/*`, `Utils/Disposables/*`

- An event is a `readonly struct` named in the past tense (`UnitDiedEvent`). An event is news; "do this" is a service method call.
- `Subscribe` returns an `IDisposable`; the subscriber collects it in a `CompositeDisposable` and releases it in `OnDestroy`/`Dispose`.
- Publishing walks a copy of the subscriber list; if one handler throws, the others still run.

| Event | Published by | Listened to by |
|---|---|---|
| `BuildingPlacedEvent` | `BuildingService` | `BuildingViewSpawner`, `TerritoryService`, `ResourceBarView` |
| `BuildingDestroyedEvent` | `BuildingService` | `BuildingViewSpawner`, `TerritoryService`, `SelectionService`, `GameOutcomeService`, `ResourceBarView` |
| `BuildingCompletedEvent` | `ConstructionSystem` | `TerritoryService`, `BuildingViewSpawner` (the archer appears), `InformationPanelView` |
| `BuildingFiredEvent` | `BuildingDefenseSystem` | `BuildingViewSpawner` (archer frame), `ProjectileSpawner` (arrow) |
| `GridChangedEvent` | `BuildingService`, `ResourceNodeService` | `UnitMovementSystem` (units whose path is affected find a new one) |
| `UnitSpawnedEvent` | `UnitService` | `UnitViewSpawner`, `ResourceBarView` |
| `UnitDiedEvent` | `UnitService` | `UnitViewSpawner`, `SelectionService`, `GameOutcomeService`, `ResourceBarView` |
| `UnitShelteredEvent` / `UnitLeftShelterEvent` | `UnitService` | `UnitViewSpawner`, `SelectionService` (on entering only) |
| `UnitOrderedEvent` | `UnitCommandService` | `ShelterSystem` (a player's order delays fleeing for 10 s) |
| `UnitAttackedEvent` | `CombatSystem` | `UnitViewSpawner` (attack frames), `ProjectileSpawner` (arrow, with `ReleaseDelay` and `FlightTime`) |
| `HungerChangedEvent` | `HungerSystem` | `ResourceBarView` (food in red) |
| `EntityDamagedEvent` | `CombatService` | Building and unit spawners (red flash), `InformationPanelView`, `UnitSelectionCardView` |
| `MoveCommandIssuedEvent` / `AttackCommandIssuedEvent` | `UnitCommandService` | `MoveMarkerView` |
| `SelectionChangedEvent` | `SelectionService` | Building and unit spawners, `InformationPanelView`, `UnitSelectionCardView`, `CommandBarView` |
| `HoverChangedEvent` | `HoverService` | `HoverLabelView` |
| `PlacementStartedEvent` / `PlacementEndedEvent` | `PlacementService` | `InputModeController`, `ProductionMenuView` (the menu slides away) |
| `ResourceNodePlacedEvent` / `ResourceNodeRemovedEvent` | `ResourceNodeService` | `ResourceNodeViewSpawner` |
| `ResourcesChangedEvent` | `ResourceService` | `ResourceBarView` |
| `DayStartedEvent` | `CalendarService` | `RaidService`, `ResourceRespawnService`, `HungerSystem`, `CalendarLabelView` |
| `RaidEndedEvent` | `RaidService` | `GameOutcomeService`, `CalendarLabelView` |
| `GameNoticeEvent` | `RaidService`, `ShelterSystem`, `HungerSystem` | `NoticeBannerView` |
| `GameEndedEvent` | `GameOutcomeService` | `GameEndView`, `TimeControlService`, `RaidArrowView` |
| `FloatingTextRequestedEvent` | `PlacementInputHandler`, `InformationPanelView`, `GatheringSystem`, `FarmingSystem`, `UnitCommandService`, `HotkeyInputHandler`, `DeleteInputHandler` | `FloatingTextSpawner` |
| `SandboxFactionChangedEvent` | `SandboxService` | `ProductionMenuView`, `InformationPanelView` |
| `SandboxSwitchesChangedEvent` | `SandboxService` | `SandboxSwitchesView` (`DebugOverlayView` and `BattleAiSystem` read it every frame) |
| `SandboxMapClearedEvent` | `DeletionService.ClearMap` | `RaidService` (stops a sandbox raid) |
| `GameSpeedChangedEvent` | `TimeControlService` | `TimeControlView` |

## 5. Data and Game Modes
**Status:** ✅ · **Files:** `Gameplay/Data/*` · **Data:** `GameDatas/`

- All definitions are collected in `Data_Database_Game`, which is what the scope registers. If a field is empty, the scope fails right away with a clear error.
- A building's sprite isn't in its definition but in the color set (`Data_TeamColor_Red`, `_Yellow`). Side (`Faction`: Player, Enemy) and color (`TeamColor`) are separate. The player picks a color, and the enemy takes the first one left (`TeamService`).
- Every definition's `OnValidate` pulls values such as size and HP into a valid range and warns about missing fields.

**Where do Sandbox and Normal mode differ?** The Sandbox is the game the brief asks for, and it isn't touched. Every new rule belongs to Normal mode; the split is made in few, well-defined places:

| Place | Sandbox | Normal |
|---|---|---|
| `ResourceService.IsUnlimited` | Every price passes | Resources are spent |
| `PopulationService` | No limit | Capacity check |
| `TerritoryService.IsEnabled` | Every cell is inside | Territory rule |
| `NormalModeStarter` | Does nothing | Castle, villagers, starting resources |
| `PlacementService.TryConfirm` | The building is ready at once | Placed as a construction site if `buildTime` > 0 |
| `ToolCursorView` | Normal cursor (a sword while Attack Move waits) | Tool cursors over resources and sites, a sword while Attack Move waits |
| `BattleAiSystem` | Switched on with a button (off by default) | Always on |
| `DeletionService` | Everything can be deleted, the resource under the mouse too | Only the player's units and buildings, never the Castle |
| Production Menu | Buildings, Units, Resources tabs | Buildings only |
| `BuildingDefinition.GetProducibles` | `producibles` | `normalModeProducibles` (the Castle produces villagers) |
| `BuildingDefinition.IsNormalModeOnly` | Not in the menu (Outpost) | In the menu |
| Calendar, shelter, game over | Off | On |
| Raids, battle AI | By hand only ("Start Raid", "Enemy AI") | With the calendar, always on |
| `SandboxService.ActiveFaction` | Changed with the Side button | Always the player |

---

# B. World

## 6. Map and Grid
**Status:** 🟡 (fixed ground, random resources) · **Files:** `Gameplay/Grid/*`, `Gameplay/Entities/*` · **Test:** `GridMapTests`

```
Tilemaps (visuals) ─► TilemapGridSource.BuildGridMap() ─► GridMap (logic)
```
- **1 cell = 1 Unity unit = 64 px.** Cell (0, 0) is the bottom-left corner.
- `GridMap` keeps flat arrays per cell: whether the ground is walkable, who is on it (building or resource id), the level (0 flat, 1 plateau), whether it is a ramp.
- `IsWalkable`: inside the map, walkable ground and empty. `CanStepBetween`: same level, or a ramp. `IsAreaFree`: one level, no ramp, empty, for buildings.
- `TryOccupy` checks every cell first and fills them afterwards; a failed placement never leaves the map half done. `Release` frees only the cells that belong to that id.
- What each tilemap says: `Tilemap_Ground` filled = walkable, `Tilemap_Elevated` = level 1, `Tilemap_Stairs` = ramp, `Tilemap_Blocking` (not drawn) = obstacle.
- The grid is built as soon as the scene opens; a broken tilemap fails in the first second.
- **Units aren't written to the grid.** If they were, they would block each other's paths. A unit's position is in `UnitModel.Position`.

## 7. Resources
**Status:** ✅ · **Files:** `Gameplay/ResourceNodes/*`, `Gameplay/Economy/ResourceRespawnService.cs` · **Data:** `Data_Resource_*`, `Data_Config_ResourceDistribution` · **Test:** `ResourceDistributorTests`, `VillagerBehaviourTests`

```
ResourceMapInitializer (IStartable) ─► ResourceDistributor (seed) ─► ResourceNodeService.TryPlace ─► ResourceNodePlacedEvent ─► ResourceNodeViewSpawner
```
- **Distribution (`ResourceDistributor`, plain C#):** `System.Random(seed)`; the same seed gives the same map. Free space for the start in the middle, a cluster of each kind near the start, stair entrances kept open. A flood fill runs before each resource; no resource that would cut the map in two is placed. Order from rare to common: gold, stone, sheep, forests, single trees.
- **Model:** `ResourceNodeModel` holds the amount left. A depleted resource is removed with `ResourceNodeService.Remove`, its cells are freed and `GridChangedEvent` goes out.
- **View:** `ResourceNodeViewSpawner` plays the frames of every tree and sheep in one loop; 200 trees don't get an `Update` each.
- **Sheep:** `ResourceRoamSystem` changes only the visual `Offset`; the grid cell stays the same. Dies in one cut (`IsOneCut`).
- **Respawning:** `ResourceRespawnService` adds sheep every morning (`DayStartedEvent`, not the first one), up to a limit. It picks their places with the distributor's rules.

## 8. Camera
**Status:** ✅ · **Files:** `Gameplay/CameraControl/*` · **Data:** `Data_Config_Camera` · **Test:** `CameraMathTests`

- Cinemachine 3 drives the real camera; our `CameraController` moves the `CameraTarget` object and writes the lens size (zoom).
- Pan with WASD/arrows, drag with the middle button, zoom towards the mouse with the wheel. Edge scrolling exists but is off in the settings.
- The bounds are applied every frame with `CameraMath.ClampCenter`; the view size is computed from the aspect ratio. When the window is resized, the camera comes back inside the bounds on its own.
- Uses `Time.unscaledDeltaTime`; the player can look around while the game is paused.
- Mouse→world conversion goes through `CameraController.ScreenToWorld` from our own center; `Camera.ScreenToWorldPoint` could give the previous frame.

## 9. Pathfinding
**Status:** ✅ · **Files:** `Gameplay/Pathfinding/*`, `Utils/Collections/MinHeap.cs` · **Test:** `AStarPathfinderTests`, `GridSearchTests`, `MinHeapTests`

- **A\*:** 8 directions, no corner cutting, octile heuristic, our own `MinHeap`. The arrays are allocated once and every search gets a new stamp; a search makes no garbage.
- **Unreachable target:** A path to the opened cell closest to the target is given (`PathStatus.Partial`). Walking up to a building to attack it works the same way.
- **`GridSearch`:**
  - `TryFindNearestWalkable`: the nearest free cell (when a spawn point is taken, when a building is clicked).
  - `CollectDestinations`: separate target cells for a group; a BFS over walkable cells only, so a group isn't split across a lake.
  - `TryFindCellBeside`: the nearest free cell next to an area, on the same level (villagers, attacking soldiers).

---

# C. Buildings

## 10. Placement
**Status:** ✅ · **Files:** `Gameplay/Placement/*`, `Gameplay/Input/InputModeController.cs` · **Test:** `PlacementServiceTests`, `TerritoryTests`, `UnitPlacementTests`

```
Production Menu click ─► PlacementService.Begin(building, side) ─► PlacementStartedEvent ─► InputModeController: Placement map on
Every frame: PlacementInputHandler ─► MoveTo(mouse) / HideArea (over UI) / TryConfirm(Shift?) / Cancel
TryConfirm ─► BuildingService.TryPlace (asSite in Normal mode) ─► price paid ─► BuildingPlacedEvent + GridChangedEvent
LateUpdate: PlacementPreviewView reads the service and draws
```
- `PlacementService` only holds the state: which building, which side, where the footprint is. It doesn't read input; tests drive it with `MoveTo(new Vector2(3, 3))`.
- **`Block`** says why an area is invalid: `NoArea`, `OutsideMap`, `Occupied`, `UnitInTheWay`, `OutsideTerritory`, `NotEnoughResources`. `PlacementInputHandler` picks the warning text.
- **`IsCellFree(cell)`** makes each square of the preview green or red: walkable, no unit on it, inside the territory.
- The footprint is centered on the mouse (`GridMap.GetAreaCenteredAt`); even sizes snap to a grid line. `Floor(x + 0.5)` instead of `RoundToInt`, so it doesn't jump at the boundary.
- The price is paid after the building is placed; if it can't be placed, no money goes. With Shift held, the mode stays on. The "Hold Shift to place several" line under the Production Menu tooltips says so (`TooltipText.PlaceSeveralHint`).
- **Unit mode (Sandbox):** `BeginUnit(unit, side)` opens the same mode for a unit: a one-cell footprint, the rule "is the cell walkable". Another unit in the cell is fine; the new one appears in the nearest free cell (`UnitService.TrySpawn`). No territory, no construction. Input maps, Esc, right click cancel and Shift for several in a row are the same as for buildings. The preview shows the unit's first idle frame and a one-cell green/red square. Only one of `Definition` and `Unit` is set at a time.
- Preview squares are copied from a hidden template; the ones opened for a big building aren't destroyed, they are switched off.

## 11. Building Model and View
**Status:** ✅ · **Files:** `Gameplay/Buildings/*` · **Test:** `BuildingServiceTests`

- `BuildingService` owns every building (`Dictionary<GameEntityId, BuildingModel>`). `TryPlace`, `Remove`, `TryGet`, `GetBuildingAt`.
- `BuildingModel`: `Id`, `Definition`, `Faction`, `Area`, `CurrentHp`, construction state (`BuildProgress`, `IsBuilt`), tower state (`DefenseTarget`, `ShotDelayLeft`).
- **Visible area:** The roof goes above the cells. `BuildingDefinition.VisualHeight` gives the visible height; selection, attack orders and hover accept clicks on the roof too. Paths and placement still use the footprint.
- **View:** A single prefab (`Prefab_Gameplay_Building`) serves every building; the sprite comes from the color set. The root sits at the bottom center of the footprint; `BuildingSpriteLayout.PlaceOnFootprint` puts the sprite's bottom center on the root.
- **Draw order:** The Sorting Group on the root sorts the building by the y of its base (Custom Axis 0, 1, 0). A unit behind the building stays behind it.
- A building that is hit turns red for a moment (`DamageFlash`, shared with units). When the flash ends it returns to its own tint; for a construction site that is a see-through grey.
- A construction site is drawn with a grey sprite (`siteSprite` in the color set) and switches to the colored one when finished (Section 22b).

## 12. Territory and the Outpost
**Status:** ✅ · **Files:** `Gameplay/Territory/*` · **Data:** `BuildingDefinition.TerritoryRadius` · **Test:** `TerritoryTests`

```
A player's territory building is finished or destroyed ─► TerritoryService.Rebuild ─► Version++
Placement: PlacementService.IsCellFree ─► TerritoryService.IsInside(cell)
Visuals: TerritoryOverlayView redraws when Version or the preview of the area to open changes
```
- One `bool[]` for the whole map. A construction site opens no territory; the Outpost opens it when finished. It is recomputed only when a source building is finished or destroyed; the per-frame query of placement is cheap.
- A cell is in the territory if its center is **inside** the circle (`IsInCircle`, a strict `<`). This avoids one-cell bumps on the sides of the circle.
- Every cell of the building must be in the territory. When the territory shrinks, buildings left outside stay and keep working.
- The service also scans the existing buildings when it is created, so it sees the Castle even if it was placed first.
- **Area to open:** `PlacementService.ShowsTerritoryPreview` is on while a territory building is about to be placed inside the territory. `PlacementPreviewView` passes the center and radius to the visuals; the shape is the same as the area that opens once it is placed.
- **Visuals:** One texture over the map, 8 pixels per cell: outside red, the border cream, the area to open green. Shown only during placement. `PlacementPreviewView` creates it next to itself in Normal mode.
- **The Outpost** is data, not code: `Data_Building_Outpost` (`territoryRadius`, `isNormalModeOnly`). Castle 12, Outpost 12, Tower 4.

## 13. Tower Defense
**Status:** ✅ · **Files:** `Gameplay/Combat/BuildingDefenseSystem.cs`, `Gameplay/Buildings/BuildingDefenderView.cs` · **Data:** the Defense header of `BuildingDefinition` · **Test:** `BuildingDefenseTests`

```
Combat phase: BuildingDefenseSystem ─► every building with HasDefense
   cooldown over? ─► target still valid and in range? ─► if not, CombatService.FindNearestHostileUnit
   ─► CombatService.ApplyDamage ─► BuildingFiredEvent ─► archer frames + arrow
```
- Until construction is finished, the tower doesn't fire and no archer shows on it; the archer appears with `BuildingCompletedEvent`.
- The archer isn't a unit but part of the building: it can't be selected, takes no population and doesn't die. The state is in `BuildingModel`, the numbers in the definition. A new defense building is added with data only.
- The target is kept until it dies or leaves range. Always switching to the nearest would spread the damage over many units.
- With no one around, it looks 4 times a second (`ScanInterval`). Hits units only. Works in every mode and for both sides.
- The tower archer's timing comes from the data of its `Defender` unit: the arrow leaves on the release frame, and the damage lands when the arrow arrives (Section 18).

## 14. Coverage Rings
**Status:** ✅ · **Files:** `Gameplay/Buildings/CoverageRingView.cs` · **Test:** `CoverageRingTests`

- When a building is selected, `BuildingViewSpawner` (`SelectionChangedEvent`) calls `BuildingView.SetSelected`: the orange ring is the attack range, the cream ring the territory. The range shows while a Tower is being placed too.
- The ring texture is drawn in code once; it isn't redrawn while the radius stays the same, and it is never created for a building that never shows it.
- Draw order -1: above the ground, below buildings, units and trees.
- On an enemy building only the range shows; the Sandbox has no territory ring.

---

# D. Units

## 15. Unit Production
**Status:** ✅ · **Files:** `Gameplay/Units/UnitService.cs`, `Gameplay/Buildings/Production/UnitProductionService.cs` · **Test:** `UnitServiceTests`, `UnitProductionServiceTests`, `GameLoopTests`

```
Soldier button on the panel ─► InformationPanelView ─► UnitProductionService.TryProduce
    GetBlock: NotAvailable / UnderConstruction / NotEnoughResources / NoHousing  ─► warning text
    spawn cell = Area.position + SpawnOffset ─► UnitService.TrySpawn (nearest free cell if taken)
    ─► UnitSpawnedEvent ─► UnitViewSpawner
```
- No production time; that's what the brief asks for. A unit takes its building's side.
- **Rally point (`RallyPointService`):** Only with a production building selected, a right click sets the point (`CommandInputHandler`: if the unit order doesn't apply, it tries the building). It must be the player's finished building that produces units in this mode. Every unit produced is sent to the point with `UnitProductionService.UnitProduced`, with the same order as a right click (`UnitCommandService.CommandAt`): ground walks, an enemy is attacked, a resource is gathered, a site is built. While the building is selected, a flag in the team color stands at the point (`BuildingView.rallyFlag`, with its own Sorting Group).

- "Is there a unit in this cell?" is answered from positions with `UnitService.HasUnitAt` (O(n), enough for these numbers).
- `UnitModel.Body`: a rectangle around the feet as big as the visible character. Clicks and box selection use it.

## 16. Selection and Hover
**Status:** ✅ · **Files:** `Gameplay/Selection/*`, `Gameplay/Hover/*` · **Test:** `SelectionServiceTests`, `HoverServiceTests`

```
Left button released ─► SelectionInputHandler
   dragged less than 8 px ─► SelectionService.SelectAt   (the front unit first, then a building, clear if nothing)
   more                   ─► UpdateBox, on release ConfirmBox (the player's units only)
─► SelectionChangedEvent ─► panel, card, selection rings, coverage rings
```
- The selection holds a list of ids; `Primary` is the first. The dead and destroyed leave the selection on their own.
- Selecting the same thing again sends no event; the panel isn't refreshed for nothing.
- **Hover** picks in the same order as a click; the frame is on whatever a click would select. It checks every frame, since a unit can walk under the mouse. Off during placement and over UI.
- The box, selection rings and hover frame are SpriteRenderers in the world; Gameplay doesn't depend on UI.
- **Control groups (`ControlGroupService`):** Ctrl+1–9 writes the selection to a group (only the player's units and buildings; a "Group 3" floating text), 1–9 selects the group (`SelectionService.Select(list)`), and a second press of the same number within 0.35 s moves the camera to the middle of the group. Ids are never reused, so a dead member simply stops being found; a sheltered villager is back in its group when it walks out.
- **Idle villager (`IdleVillagerFinder`):** "." or the "Idle: N" button at the bottom selects the next idle villager (in id order, starting over after the last) and moves the camera to it. Home moves the camera to the Castle.

## 17. Commands and Movement
**Status:** ✅ · **Files:** `Gameplay/Commands/*`, `Gameplay/Units/UnitMovementSystem.cs`, `UnitModel.cs` · **Test:** `UnitCommandServiceTests`, `UnitMovementTests`, `ShortcutTests`

```
Right click ─► CommandInputHandler ─► UnitCommandService.CommandAt(selection, point)
   enemy unit or building ─► Attack ─► UnitModel.SetAttackTarget          ─► AttackCommandIssuedEvent (red ring)
   own construction site  ─► Build  ─► villagers ConstructionSystem.TryOrder, soldiers walk next to it
   resource               ─► Gather ─► villagers GatheringSystem.TryOrder, soldiers walk next to it
   ground                 ─► MoveTo ─► a separate cell per unit ─► UnitMovementSystem.SendTo ─► MoveCommandIssuedEvent
   ─► UnitOrderedEvent
F (or the button on the command bar) ─► AttackMoveTargeting.Begin ─► sword cursor ─► left click ─► UnitCommandService.AttackMove
X ─► Stop        H ─► HoldPosition        (HotkeyInputHandler; the buttons call the same methods)
Movement phase: UnitMovementSystem ─► UnitModel.MoveAlongPath(speed * dt)
```
- Only the player's units take orders; friendly fire is prevented in `CombatService.GetHostileAt`. A right click on an own building is a move.
- **Spreading out:** `CollectDestinations` finds as many cells as there are units; cells where another unit stands or is walking to aren't taken. Targets from near to far, each given to the nearest unit left.
- **When the path changes:** On `GridChangedEvent`, only the units whose remaining path crosses the changed area look for a new one.
- When a waypoint is reached, the leftover distance carries over to the next one; in a long frame a unit doesn't overshoot and jitter.
- **Stances (`UnitModel.IsHoldingPosition`, `AttackMoveCell`):** Every new order of the player ends both (`ClearStance` inside `AnnounceOrder`).
  - **Stop:** path, target and work (gathering, construction, farm) are cancelled.
  - **Hold Position:** Stop + stays in place. `CombatSystem` drops a target out of range instead of chasing it; `BattleAiSystem` gives it only enemies within its own range. It doesn't run off when hit from afar either.
  - **Attack Move:** Spreads out like `MoveTo`, and every unit writes its own destination cell to `AttackMoveCell`. `AttackMoveSystem` (AI phase, 4 times a second, also while the sandbox AI is off, since it is the player's order) turns on enemies within 5 cells on the way, walks on when the enemy dies, and ends the stance on arrival. It also ends if there is no way closer, so A* isn't searched 4 times a second. A red ring shows.
  - Targeting mode (`AttackMoveTargeting`): while it is on, a left click doesn't select (on the frame it ends either, `IsUsingClicks`), a right click or Esc cancels, and it closes on its own if no player unit is left in the selection or placement starts.

## 18. Combat and Removal
**Status:** ✅ · **Files:** `Gameplay/Combat/*` · **Test:** `CombatTests`

```
Combat phase: CombatSystem, for every unit with a target
   target gone/dead ─► drop it, stop
   in range         ─► stop, face it, if the cooldown is over: UnitAttackedEvent (animation) + CombatService.ScheduleHit
                        damage on the hit frame (hitDelay); for archers an arrow leaves and the damage lands on arrival (+ distance / projectileSpeed)
   not in range     ─► walk next to the target (a new path at most every 0.25 s)
Cleanup phase: DeathCleanupSystem ─► CombatService.RemoveDead ─► UnitService.Remove / BuildingService.Remove
               ─► UnitDiedEvent / BuildingDestroyedEvent + GridChangedEvent
```
- `CombatService.TryGetTarget` puts unit and building targets in one place: distance, aim point, cell to walk to. The distance to a building is measured to its nearest edge; the Castle can be hit from its side.
- `ApplyDamage`: HP doesn't go below 0, a death is queued only once. **Fighting back:** an idle unit turns on whoever hits it; a busy one carries on.
- The cooldown starts at the attack; the first hit doesn't wait. Switching targets doesn't give faster hits.
- **Hit timing:** `UnitDefinition.hitDelay` is the time from the start of the attack animation to the hit frame (Knight 0.2, Spearman 0.1, Archer 0.5, Villager 0.2). For a unit with `projectileSpeed`, the damage lands when the arrow arrives (Archer 12 cells/s). If both are 0, the damage lands at once; the old behavior and tests stay that way.
- `CombatService.ScheduleHit` holds the scheduled hits, and `CombatSystem` advances them first every tick. If the attacker dies before the release, the hit is cancelled; an arrow already in the air still arrives.
- A ranged unit (`IsRanged`, range > 2) shoots pooled arrows. Arrows fly on the game clock (`ProjectileView`) and wait hidden until the release; they stay in sync with the damage through pausing and speed changes.
- **Death and destruction effects (`EffectSpawner`):** A small dust puff when a unit dies (or is deleted); an explosion sized to the building and a large dust cloud at its base when a building falls. Frames from `Textures/VFX` (Effects atlas), played on the game clock, renderers from a pool. No object in the scene: `GameplayLifetimeScope` builds `Prefab_Gameplay_Effect_Spawner` with `RegisterComponentInNewPrefab`.
- **Health bar (`HealthBarView`):** A thin frame with a fill in the side's color (player green, enemy red), over the head of a unit and above the roof of a building. Shown only when hurt or selected; on a construction site the progress bar is there instead. The fill is in whole pixels, at least one pixel while there is health (`GetFillWidth`, tested). Sorting Group order 158.

- On removal: the grid is freed, the selection lets go, attackers drop the target, the view is destroyed.

## 19. Unit View and Status Icons
**Status:** ✅ · **Files:** `Gameplay/Units/UnitView.cs`, `UnitViewSpawner.cs`

- **No Animator.** Frames come from the color set (`UnitSpriteSet`) and are computed from the game clock: `Floor(time * fps + phase) % frameCount`. They freeze when the game pauses. The phase comes from the unit id.
- Priority: attack (plays once) > construction (hammer) > gathering (axe or pickaxe) > running > idle. The sprite is set only when the frame changes.
- Pivots are at the feet; `flipX` turns the character around its own axis.
- The selection ring is under the feet, colored by side.
- **Status icons:** One `SpriteRenderer`, several sprites. A cream "Zz" over an idle villager (`UnitModel.IsIdle`); a red "!" for 1.5 s over a soldier that spots an enemy by itself or fights back when hit (`UnitModel.SpotEnemy`, `HasJustSpottedEnemy`). Not for the player's orders. Not for a soldier that fought in the last 3 seconds either: in a big battle each kill was followed by a new target, and the "!" never went out over any soldier; now it only marks the start of the fight. It has its own Sorting Group (`Sort At Root`, 160), in front of trees and buildings.
- **Carry icon:** A small icon of the resource a villager carries to a drop-off (wood, stone, meat, gold). On the same `SpriteRenderer`, with the priority "Zz" > "!" > load. The model says it through `UnitModel.CarriedKind`; `GatheringSystem` and `FarmingSystem` set it when the load is picked up and clear it on delivery or when the load is lost (cancel, no path, death). While fleeing the load stays in hand, and the icon too. The textures `Texture_Markers_Carry_*` are copies of the UI icons scaled down to 26 pixels, in the Gameplay atlas (the UI atlas in the world would need a separate draw). When the icon changes, it shrinks from big over 0.15 s (a pop).

---

# E. Normal Mode

## 20. Resources and Costs
**Status:** ✅ · **Files:** `Gameplay/Economy/ResourceService.cs`, `NormalModeStarter.cs`, `Gameplay/Data/ResourceCost.cs` · **Data:** `Data_Config_NormalMode`, `cost` in the definitions · **Test:** `NormalModeTests`

- `ResourceService`: wood, stone, food, gold. `CanAfford`, `TrySpend`, `Add`; `ResourcesChangedEvent` on every change. `IsUnlimited` in the Sandbox.
- `ResourceCost` is an amount of one resource; buildings, units and starting resources are written with it.
- **Start (`NormalModeStarter`, `IStartable`):** Resources are given (scaled by difficulty), the Castle goes to the middle of the map, villagers come out. There is no Castle in the Normal menu; the only Castle comes at the start.
- The cost shows on the `Cost:` line of the tooltip (Normal mode only).

## 21. Population
**Status:** ✅ · **Files:** `Gameplay/Economy/PopulationService.cs` · **Test:** `GameLoopTests`

- `Used` = the sum of the player's units' `PopulationCost` (sheltered ones included). `Capacity` = the sum of `PopulationCapacity` of **finished** buildings (Castle and House 5).
- No counter is kept; it is computed on every query, so it can't go out of sync when a building falls.
- The bar shows `used/capacity`. With no room, production stops with `NoHousing`.

## 22. Gathering
**Status:** ✅ · **Files:** `Gameplay/Gathering/GatheringSystem.cs` · **Test:** `NormalModeTests`, `VillagerBehaviourTests`

```
Right click a resource with a villager ─► UnitCommandService.Gather ─► GatheringSystem.TryOrder
   WalkingToNode ─► Harvesting (harvestDuration) ─► WalkingToDropOff (nearest IsDropOff building) ─► ResourceService.Add ─► WalkingToNode ...
```
- **Where it stands:** `TryPickWorkCell` adds penalties to the cells around the resource: side 0, below 3, above 6, corner 9, held by another villager 20. The last step goes to the point in the cell nearest the resource (`workDistance`), so the axe touches it.
- **When a resource runs out:** It moves on to a new one of the same kind within 12 cells; otherwise it drops its load, stops and shows "Zz".
- **When the order changes** (move, attack, fighting back) the work ends and the load is lost. **While fleeing** the work is kept with `Suspend` and continues with `Resume` on return.
- A sheep dies in one cut and all its food is carried at once. `Hold()` stops it while it is being cut.
- Work frames: axe for trees and sheep, pickaxe for stone and gold (`UnitModel.HarvestingKind`).
- The carried load is in `UnitModel.CarriedKind`; it shows as a small icon over the head (Section 19).

## 22b. Construction
**Status:** ✅ (no repair) · **Files:** `Gameplay/Construction/ConstructionSystem.cs`, `Gameplay/Buildings/BuildingModel.cs`, `BuildingCompletedEvent.cs` · **Data:** `BuildingDefinition.buildTime`, `siteSprite` in the color sets and the villager's `buildFrames` · **Test:** `ConstructionTests`, `VillagerBehaviourTests`

```
Placement in Normal mode ─► BuildingService.TryPlace(asSite) ─► BuildingModel.StartAsSite: progress 0, 10% of HP
Villager selected, right click the site ─► UnitCommandService.Build ─► ConstructionSystem.TryOrder
   Walking ─► a cell next to the building ─► last step to the foot of the wall ─► Working (hammer frames)
   Working ─► AddBuildProgress(dt / buildTime) every tick ─► after 4–7 s moves to another cell around it
   Progress 1 ─► BuildingCompletedEvent ─► the villager goes idle
```
- **Time:** `buildTime` is the time for one villager (House 20, Barracks 35, Tower 30, Outpost 30 s). Each villager adds its own share, so two villagers are twice as fast. The Castle is ready at the start; a building with `buildTime` 0 is always finished at once.
- **HP:** A site starts with 10% HP, the rest fills with the work and is full when finished (the last fraction is rounded; otherwise the floating point sum left the building 1 HP short and showed its health bar). Damage taken doesn't come back. A site can be attacked; enemies hit it like any building.
- **What doesn't work until finished:** Production (`ProductionBlock.UnderConstruction`), population capacity, tower fire, territory, drop-off. All look at `BuildingModel.IsBuilt`; each system applies the rule itself.
- **Where it stands:** The sides and below are preferred (above, the roof would cover the villager). A cell held by another villager gets a penalty; two villagers don't stand in the same place. The feet are 0.4 units from the wall; the hammer touches the building. When moving, a little randomness is added to the choice, so the villager doesn't go back and forth between the same two cells.
- **Division of work:** Gathering and construction are separate systems. An order that starts one cancels the other (`UnitCommandService.CancelJobs`). On sheltering both are suspended, and when the enemy is gone the villager goes back to whichever there is.
- **Look:** The grey sprite starts see-through and darkens with the work; when finished, the colored sprite comes (`BuildingView.UpdateLook`). The grey sprites are in the `Buildings` folder of the atlas, with no extra draw cost. The panel shows `Under construction 40%` under the name, with live HP.
- **Progress bar (`BuildProgressBarView`):** 0.3 units above the site's roof, the pack's small bar frame (sliced, 1–2 units by the building's width) with a gold fill. The fill is a one-pixel-wide column stretched with `localScale.x` and rounded to whole pixels (`GetFillWidth`, tested). The textures are in the Gameplay atlas (`Texture_Markers_ProgressBar_Frame/Fill`); using the original from the UI atlas in the world would need a separate draw. It has its own Sorting Group (`Sort At Root`, 155), so it doesn't hide behind trees and units. Off in the building prefab; on only for sites, off again when finished.
- **Sandbox:** No construction sites, buildings are ready at once (the brief's "no production time" item).

## 22c. Farm
**Status:** ✅ · **Files:** `Gameplay/Gathering/FarmingSystem.cs` · **Data:** `Data_Building_Farm` (`farmYield`, `farmHarvestDuration`, `farmWorkers`) · **Test:** `FarmHungerRallyTests`

```
Right click your own finished farm with a villager ─► UnitCommandService.Farm ─► FarmingSystem.TryOrder
   WalkingToFarm (a cell next to it, last step over the fence) ─► Working (axe frames, farmHarvestDuration)
   ─► WalkingToDropOff (nearest drop-off) ─► +farmYield food ─► WalkingToFarm ...
```
- **Rules:** 3x3, 40 wood, built in 15 s, 60 HP. One villager works it, it never runs out, 5 food every 6 seconds (slower than sheep but steady). In the menu next to the House. It is in the Sandbox too (for testing): ready at once, a villager works it; food is ∞ there so it isn't counted, and a drop-off (Castle) must be placed too for the farm to work.
- It is built first: a right click on the site sends the villager to build, on the finished farm to farm. A villager sent to a full farm walks next to it and shows "The farm has all the workers it can take".
- `GatheringSystem` is tied to the resource itself (amount, depletion, the sheep roaming); a farm is a building, so a separate small system was written. Picking the cell next to it works as in `ConstructionSystem`.
- **Working inside the fence:** The villager steps over the fence from the cell next to it and stands among the crops at a random point of the adjacent edge cell (0.45 in from the fence at the sides, 0.55 at the bottom). A new point is picked every time. Edge cells only: the way out is one step, and A* doesn't get stuck inside.
- Its feet are above the bottom edge of the farm, so by height it would sort behind the crops. That's why `UnitModel.IsInField` is on while inside and `UnitView` sets the root Sorting Group to 1; it goes back to 0 past the fence.
- Enemies hit from outside the fence: `CombatService` measures the distance to a villager inside from the nearest fence, and sends a chaser to the cell outside that fence. A Knight (range 1) reaches from the foot of the fence.
- Division of work: gathering, construction and farming don't happen together; an order cancels the others. On sheltering the farm work is suspended too, and the carried load counts at the Castle.
- The cursor is an axe over your own finished farm. The visual is colorless (`Texture_Buildings_Neutral_Farm`), the same in both color sets; its grey construction copy is `_Site`.

## 22d. Hunger
**Status:** ✅ · **Files:** `Gameplay/Economy/HungerSystem.cs` · **Data:** `Data_Config_NormalMode` > Hunger (Easy / Normal / Hard) · **Test:** `FarmHungerRallyTests`

- Every morning (except the first) each of the player's units eats: Easy 1, Normal 2, Hard 4. What's missing becomes a debt; the food there is still gets eaten.
- While there is a debt, every unit loses 1 HP at intervals: Easy 15, Normal 10, Hard 5 seconds. Incoming food pays the debt first, and the loss stops once it is paid.
- On starting, "Not enough food! Your people are starving"; on ending, "There is food again"; the food count on the resource bar turns red (`HungerChangedEvent`). "Was it starving before?" is read before that morning's debt is added; otherwise starving that begins that morning looked as if it had already been going on, and the notice never appeared.
- The damage has no attacker (`CombatService.ApplyDamage(..., GameEntityId.None)`); so the unit doesn't turn on anyone and no "!" shows. Deaths go through the normal death events, so if the last unit dies, the existing losing condition works.
- Difficulty is read from `GameSessionSettings.Difficulty`; it is chosen after Play on the main menu (Section 27). No hunger in the Sandbox.

## 23. Calendar and Night
**Status:** ✅ · **Files:** `Gameplay/Calendar/CalendarService.cs`, `UI/Hud/DayNightOverlayView.cs`, `CalendarLabelView.cs` · **Data:** `Data_Config_Raids`

- 120 seconds of day, 60 of night; it moves on the game clock in the Time phase. `DayStartedEvent` at the start of every day.
- `Darkness` changes over 15 seconds at dusk and dawn; `DayNightOverlayView` uses it as the alpha of a dark tint over the screen. No lighting; the extra draw cost is a single image.
- `Freeze` during a raid, `Resume` afterwards. Off in the Sandbox.

## 24. Raids and Battle AI
**Status:** ✅ · **Files:** `Gameplay/Raids/RaidService.cs`, `BattleAiSystem.cs` · **Data:** `Data_Config_Raids` · **Test:** `GameLoopTests`

```
Morning of a raid day: "Today is a raid day!" ─► starts between 20% and 80% of the day ─► the calendar stops
A wave appears ─► all of it dies ─► 8 s ─► the next wave ... ─► "Raid beaten!" ─► RaidEndedEvent ─► the calendar goes on
```
- Raid days are 3, 6, 8 and 10. Every raid has several waves; later ones are bigger and have more knights. No HP or damage multiplier; the brief's values stay.
- **Spawn point (`PickSpawnCell`):** A random coast cell, reachable on foot from the Castle, at least 20 cells away. Computed at that moment, since buildings may have changed the map. The direction is given in text ("from the north-east"), and an arrow at the screen edge points at a wave that is off screen (`RaidArrowView`, Section 30).
- Times come from the seed; the same seed gives the same raid times.
- **`BattleAiSystem`** picks targets 4 times a second; walking and hitting are in `CombatSystem`:
  - Enemies: their target is the Castle (in the Sandbox with no Castle, the player's nearest building or unit, at any distance). If a player unit or building is near (`aggroRange` 5), they turn on it; if it gets more than 10 cells away, they let go. Buildings are targets too, so a building blocking the way gets knocked down; there is no separate "break the obstacle" code.
  - The player's idle soldiers attack nearby enemies; a unit with a move order isn't disturbed.
- A wave is over when its own units are dead (`_waveUnits`); enemies placed by hand in the Sandbox don't hold back the next wave.
- **Difficulty:** At every wave, each group's count is multiplied with `RaidSettings.ScaleCount`: Easy ×0.6, Normal ×1, Hard ×1.5; halves round up, and a group never drops to zero. Difficulty is read again at every wave, so the sandbox's *Difficulty* button counts from the next wave on. Starting resources depend on difficulty too (`NormalModeSettings.ScaleStartingAmount`: ×1.5 / ×1 / ×0.75), and so does hunger (Section 22d). The values are in assets, not in code.
- Off by default in the Sandbox; switched on with the "Enemy AI" button (Section 33). **Sandbox raid:** "Start Raid" starts the next raid of the list at once with `RaidService.TryStartRaidNow` (starting over after the last) and switches the AI on. There is no calendar, and no `RaidEndedEvent` when it ends (the game isn't won). It stops when the map is cleared.

## 25. Shelter
**Status:** 🟡 (Castle only) · **Files:** `Gameplay/Raids/ShelterSystem.cs`, `UnitService.Shelter/BringOut` · **Test:** `VillagerBehaviourTests`

```
An enemy within 7 cells ─► work Suspended ─► runs next to the Castle ─► the load counts ─► goes in (UnitShelteredEvent)
No enemy left on the map ─► appears next to the Castle ─► back to work (Resume)
```
- Gathering or construction work is kept with `Suspend` and continues with `Resume` on return.
- A unit inside moves from `_units` to `_sheltered`. Selection, hover, combat and placement look at `_units`, so no other check is needed. Population counts both.
- A player's order (`UnitOrderedEvent`) comes before fleeing for 10 seconds.
- If its way is blocked, the villager goes back to work and tries again 10 seconds later.

## 26. Game Over
**Status:** ✅ · **Files:** `Gameplay/Economy/GameOutcomeService.cs`, `UI/Hud/GameEndView.cs` · **Test:** `GameLoopTests`

- **Defeat:** The player's Castle falls, or the last unit dies and there isn't enough food for a new villager.
- **Victory:** The last raid is beaten (`RaidEndedEvent.IsLastRaid`).
- `GameClock.SetPaused(true)` and `GameEndedEvent`. *Play Again* starts the same mode with a new seed, *Main Menu* goes back to the menu.

---

# F. Interface

## 27. Main Menu
**Status:** 🟡 (Play + difficulty choice, Sandbox, Settings + Controls, Quit; no color or seed choice) · **Files:** `UI/MainMenu/*` · **Image:** `Textures/Backgrounds/Texture_Backgrounds_MainMenu`

- **Play** first opens the "Choose Difficulty" window (`DifficultyPopup`, `Data_Popup_Difficulty`): Easy, Normal, Hard, each with what it changes underneath (`DifficultyText`). The choice is written to `GameSessionSettings.Difficulty` and a Normal game starts; Back cancels. **Sandbox** starts at once; its difficulty is changed with a button in the game. Both with a new seed. The buttons lock on press; `SceneLoader` also ignores a second request while loading.
- Settings opens the settings popup (Section 32b). Quit leaves Play mode in the editor, and the application in a build.
- **Background:** A single 1920x1080 picture composed from the game's own tiles and sprites (no sprites in the scene, one draw call): both sides' castles, towers, houses, forests and two small armies facing each other; the middle is left empty for the menu and the edges are slightly darkened. Outside the atlas (`Textures/Backgrounds`), since the UI atlas would bloat with a picture this size. It covers the screen at any aspect ratio (`MainMenuBackgroundView.FitPicture`). The title uses the *Outline Shadow* material. Prefab: `Prefab_UI_MainMenu_Background`, behind the panel in the canvas variant.
- Planned: color choice, seed input, loading saves (see G).

## 28. Production Menu
**Status:** ✅ · **Files:** `UI/Scroll/*`, `UI/Production/*`, `UI/Common/SlideOutPanel.cs` · **Test:** `RecyclingScrollMathTests`

- **Infinite scroll (`RecyclingScrollView`, generic):** Only visible rows + 1 worth of cells are created. The data index is `PositiveModulo(row * columns + column, itemCount)`; the list has no end in either direction. A cell is rebound only when the item it shows changes, and while the list is still, no RectTransform changes.
- The offset wraps around the loop length; even after hours of scrolling, float precision doesn't drift. Dragging, flinging and the wheel are in the component itself, with no `ScrollRect`.
- `ProductionMenuView` shows the buildings in `GameDatabase` order, in the active side's color; it hides the Castle in Normal mode and the Outpost in the Sandbox. A click calls `PlacementService.Begin`.
- **Tabs (Sandbox):** *Buildings*, *Units* and *Resources* under the header. The Resources tab lists trees, rocks, mines and sheep with their first frames; a click calls `PlacementService.BeginNode`. The Units tab lists `GameDatabase.Units` with their portraits; a click calls `PlacementService.BeginUnit`. An inactive tab is darkened. In Normal mode the tabs are hidden and the list grows into their space (`tabsSpace`); there units come only from buildings, for a price. A cell (`ProductionMenuCell`) doesn't know what it shows, only its place in the list (`Index`); the menu looks that up in the current tab's list.
- `SlideOutPanel` slides the menu in from the left with DOTween; the object isn't switched off, only its position changes.
- **Slides away while placing:** When something is picked from the menu (`PlacementStartedEvent`), the menu closes with the same animation as opening, freeing the map. When placing ends (`PlacementEndedEvent`: placed, right click or Esc), it opens again. While placing several in a row with Shift it stays closed and opens once at the end. Only a menu that placing closed comes back (`_reopensAfterPlacement`).

## 29. Information Panel and Unit Card
**Status:** ✅ · **Files:** `UI/Information/*`, `UI/Common/ContentFitPanel.cs`, `PanelSizeMath.cs` · **Test:** `PanelSizeMathTests`

- **Portraits by color:** `TeamColorDefinition.GetUnitPortrait(unit)` gives the portrait from the color set, falling back to the unit's own `Portrait`. The soldier buttons on the panel show the building's side's color, the unit card the unit's side's color, the Units tab the active side's color; an enemy soldier shows in the enemy color. Textures: `Textures/UI/Avatars/<Color>/Texture_UI_Avatars_<Color>_<Unit>`.
- **The panel shows buildings only:** picture (in the building's own color), name, HP text and bar, and the soldier list for a producing building of the active side. HP is live through `EntityDamagedEvent`.
- Its size follows its content between a minimum and a maximum; if it doesn't fit, it scrolls. It never goes off screen.
- On a construction site the production list is hidden and the progress percentage is under the name. There is no event for the percentage; the panel reads the percentage of the site it shows in `LateUpdate` and writes it when the number changes.
- Soldier cells are pooled by hand. The button in the corner clears the selection; the panel shows the selection, so it closes on its own.
- **The unit card** is at the bottom center: portrait, name, HP; for a group `5 Units` and the total HP. It doesn't catch the mouse; a right click passes through it.

## 30. HUD
**Status:** ✅ · **Files:** `UI/Hud/*` · **Test:** `OffScreenArrowMathTests`

| View | Shows |
|---|---|
| `ResourceBarView` | Wood, stone, food, gold and population at the top. ∞ in the Sandbox |
| `CalendarLabelView` | `Day 3 / 10` and the time of day |
| `DayNightOverlayView` | Night darkening |
| `NoticeBannerView` | `GameNoticeEvent` texts (raids, sheltering, hunger) |
| `GameEndView` | Victory / defeat, two buttons |
| `CommandBarView` | Over the unit card while the player's units are selected: *Attack Move (F)*, *Stop (X)*, *Hold (H)*. The same methods as the keys (`HotkeyInputHandler`); tooltips on hover. The Attack Move button turns orange while it waits for a point. `Prefab_UI_Command_Bar` |
| `IdleVillagerButtonView` | *Idle: N* left of the card, only while a villager is idle; a click or "." selects the next one. Counted 4 times a second. `Prefab_UI_Idle_Villager_Button` |
| `RaidArrowView` | While a raid wave is off screen, an orange arrow (a sword in its middle) at the screen edge points at the nearest raider (`RaidService.TryGetNearestRaider`). It pulses gently; a click jumps the camera there (`CameraController.LookAt`). Hidden when the raider comes on screen or the game ends. Its own prefab, `Prefab_UI_Raid_Arrow`, below the GameEnd screen. The edge math is `OffScreenArrowMath` (tested) |
| `TimeControlView` | Pause and 1x / 2x / 3x left of the calendar; the active one is bright, the keys are in the tooltips (Section 33b) |
| `StatsPanelView` | At the bottom right, the corner of the screen that is always free (`Prefab_UI_Stats_Panel`, on the DynamicCanvas; its text changes twice a second, so it mustn't rebuild the static canvas): FPS, unit and building counts, A* searches per second, average and longest time (ms); SetPass and draw calls in the editor. Switched by FPS Panel in the settings, in both modes |
| `SettingsButtonView` | The gear next to the time buttons on the bar (arranged by the bar's layout group); opens the settings (Section 32b) |

## 31. Feedback
**Status:** ✅ · **Files:** `UI/Feedback/*`, `UI/Tooltips/*`, `Gameplay/Feedback/*`, `Gameplay/Commands/MoveMarkerView.cs` · **Test:** `TooltipTests`

- **Floating text:** Any system publishes `FloatingTextRequestedEvent`, and `FloatingTextSpawner` in the UI draws it from a pool. Gameplay doesn't know TextMeshPro. A kind (`Info`, `Warning`) is given, the color is set in the Inspector.
- **Tooltip:** A single `TooltipView`; `TooltipText` builds the text and `TooltipPlacement` the position (plain, tested). A 0.25 s delay, doesn't catch the mouse, never goes off screen.
- **Hover label:** The name over the target (`Enemy Barracks`), its screen position updated every frame.
- **Tool cursor (`ToolCursorView`, `ToolCursorPicker`):** In Normal mode a tool appears over resources: an axe over trees and sheep, a pickaxe over stone and gold; a hammer over the player's construction site. The decision is in a plain function (tested); the view calls `Cursor.SetCursor` only when the cursor changes. While Attack Move waits for its point, a sword in both modes (`Texture_Cursors_Sword`). Textures: `Textures/Cursors/Texture_Cursors_Axe/Pickaxe/Hammer/Sword`, 32x32, import type *Cursor*, hotspot in the middle. The component is on `Prefab_Gameplay_Hover_Highlight`. The normal cursor comes back when the scene closes.
- **Command ring:** `MoveMarkerView` shrinks and fades at the clicked point: green for move, red for attack.

## 32. Esc Layers
**Status:** ✅ · **Files:** `UI/Common/EscapeInputHandler.cs`, `ICloseablePanel.cs` · **Test:** `EscapeInputHandlerTests`

Each Esc closes only the top layer: placement (on the Placement map, `PlacementInputHandler`) → an Attack Move waiting for its point → an open popup (settings, controls) → the selection → the Production Menu. With nothing left to close, it opens the settings. The order is read in one place (`CloseTopLayer`); the popup side is behind `IEscapeMenu`, with a fake in the tests.

## 32b. Settings and Popups
**Status:** ✅ · **Files:** `Core/Settings/*`, `UI/Settings/*`, `Popup/*` · **Data:** `GameDatas/UI/Data_Popup_Settings` · **Test:** `GameSettingsTests`, `EscapeInputHandlerTests`

```
Settings button on the main menu / the gear on the right of the resource bar in a game / Esc (with nothing to close)
   ─► SettingsButtonView ─► PopupSystem.Open(Data_Popup_Settings) ─► SettingsPopup
   click a row ─► GameSettings.Toggle/Cycle ─► written to PlayerPrefs at once ─► Changed
      ├─► DisplaySettingsApplier: window mode, VSync
      ├─► UiScaleApplier: canvas size
      ├─► CameraController: pan speed, edge scrolling (read every frame)
      └─► StatsPanelView: showing the FPS panel
```
- **Settings (`GameSettings`, Core, root scope):** Window (Fullscreen / Windowed), VSync, UI Size (Small 90% / Normal / Large 115%), Camera Speed (Slow 0.6x / Normal / Fast 1.6x), Edge Scrolling, FPS Panel. Each moves to its next value on click and is saved at once. An unknown saved value falls back to Normal. Storage is behind `ISettingsStorage`; tests use a dictionary.
- **UI size:** `UiScaleApplier` divides the canvas's reference resolution by the setting (1920x1080 / scale). The whole UI grows or shrinks together, with no per-element sizing.
- **Context (`ISettingsContext`):** Empty on the main menu. In a game (`GameplaySettingsContext`) the game pauses when the popup opens and goes on when it closes, if it was running before; the "Main Menu" row shows only in a game.
- **Popup module (`Popup/*`):** It was already in the project and is used here for the first time. `PopupSystem` is on the `PopupCanvas` inside `Prefab_UI_Canvas_Main` (its own Canvas, sort order 100; above every panel the scenes add). Popups are created through the scope's container, so `[Inject]` works. A modal popup puts a semi-transparent blocker behind itself; clicking the blocker closes the popup. The blocker stretches to its parent (anchors 0–1), not a fixed size; it covers the whole screen even if the UI size changes while the popup is open. OutBack scale animation on opening, InBack on closing (`PopupData`), also in a paused game (unscaled).
- **Esc:** Closes the open popup first, then the selection, then the Production Menu; with nothing to close, it opens the settings (`IEscapeMenu`, Section 32).
- **Controls (`ControlsPopup`, `Data_Popup_Controls`):** Every key in one grouped, scrollable list (`ControlsText`, in code, next to the bindings it describes). F1 opens and closes it in a game (`ControlsLauncher`); the *Controls* row of the settings window opens it on the main menu too (stacked over the settings). The game pauses while it is open in a game. Two seconds after a game starts, a "Press F1 to see the controls" notice shows once.
- **Keys in tooltips:** The view that owns a button adds the `HoverTooltip` component from code (`HoverTooltip.Attach`), with nothing to set up in the prefab: pause "Pause (P)", speeds "+ / -", the command and idle villager buttons.

## 32c. Text Materials and UI Animations
**Status:** ✅ · **Files:** `Materials/Font/MedievalSharp-Regular SDF - *.mat`, `UI/Common/ButtonFeedback.cs`, `ButtonFeedbackInstaller.cs`, `UI/MainMenu/MainMenuBackgroundView.cs`

- **Materials:** Three TMP presets on the same font atlas. *Outline* (dark brown outline): buttons, headers, cell names, the hover label. *Shadow* (soft shadow): HP texts, the FPS panel. *Outline Shadow* (both, the most readable): resource counts, the calendar, the notice banner, floating text, game and settings titles. Dark text on paper (tooltips, the information panel, the end screen) stays on the plain material; a dark outline on dark text hurts readability.
- **Button feel (`ButtonFeedback`):** Grows 5% on hover, shrinks 5% on press, comes back with OutBack on release. `ButtonFeedbackInstaller` on the main canvas adds it to every button of the scene at runtime, and popups add it to their own buttons; no prefab needs to carry it. Unscaled time.
- **Resource counts:** A count gets a short punch when its value changes (`ResourceBarView`); the first values of the game come in quietly.
- **Main menu:** The background picture zooms and drifts very slowly in a 24-second loop, the title drops in from the top with a bounce and then floats gently, and the buttons open one after another with OutBack. The buttons are in a layout group, so only their scale is animated, not their position.
- Already there: the Production Menu slide (`SlideOutPanel`), the notice banner, popup open/close.

## 33. Sandbox
**Status:** ✅ · **Files:** `Gameplay/Sandbox/SandboxService.cs`, `SandboxBattleSpawner.cs`, `UI/Sandbox/SandboxFactionToggleView.cs`, `SandboxSwitchesView.cs` (prefab: `Prefab_UI_Production_Menu` > `Panel_Sandbox`), `Gameplay/DebugView/DebugOverlayView.cs` · **Test:** `SandboxServiceTests`, `SandboxToolsTests`, `GameLoopTests`

- `SandboxService.ActiveFaction`: the side of new buildings and units. The Side button changes it; the menu shows that side's color. To produce enemy soldiers from an enemy Barracks, Side must be Enemy.
- Unlimited resources, no population or territory, no calendar, raids by hand only, buildings ready at once.
- **Production Menu tabs:** *Buildings*, *Units*, *Resources*. Units and resources (tree, rock, gold mine, sheep) go straight onto the map (Sections 10 and 28). Production from the Barracks (the brief's way) is still there. The resource list is collected from the `ResourceDistribution` groups, each definition once.
- **Sandbox panel** (`Panel_Sandbox`, `SandboxSwitchesView`): Under the "Production >" button, on the Information Panel's paper background, visible while the menu is closed too, sliding with the menu. Two groups: *Settings* (Side, Difficulty, Enemy AI, Debug) and *Actions* (Start Raid, +100 Units, Clear Map). Buttons are 50 high, and their text shrinks to fit. The buttons used to be lined up one by one along the left edge of the screen, and Side stood alone at the top center. In a normal game the whole panel is hidden.
- **Buttons:**
  - **Side:** `SandboxFactionToggleView` (on the same panel, its own component). Who new buildings and units belong to.
  - **Difficulty:** Cycles through Easy, Normal, Hard; the size of the next raid wave (Section 24). The tooltip says what it does.
  - **Enemy AI:** `SandboxService.IsBattleAiOn`. While on, `BattleAiSystem` runs: enemies walk to the player's nearest building or unit and hit whatever comes near on the way; the player's idle soldiers attack nearby enemies. Off by default, so the tester's orders stay as they are. A raid and the test battle switch it on themselves (`TurnBattleAiOn`).
  - **Debug:** `SandboxService.IsDebugViewOn`. `DebugOverlayView` draws the grid and the paths of the selected units (below).
  - **Start Raid:** The next raid of the list comes at once (Section 24). While a raid is on, the button is locked and shows "Raid 3: wave 2 of 4".
  - **Clear Map:** Deletes every unit, building and resource (`DeletionService.ClearMap`). Against a stray click, it asks for a second click ("Sure? Click again", 3 s). Deleting goes through the death events, so at most 48 effects play at once (`EffectSpawner`). A running raid stops.
  - **+100 Units:** `SandboxBattleSpawner` puts 50 mixed soldiers (Knight, Spearman, Archer) of the player left of the screen's middle and 50 of the enemy right of it, and switches the AI on. For trying pathfinding, combat and rendering in a crowd.
  - **Stats panel:** Not on the Sandbox panel, but at the bottom right and in both modes (`StatsPanelView`, Section 30). `AStarPathfinder` keeps the search time (`Stopwatch`, two reads per search, `TakeStats`).
- **Deleting:** The Delete key removes everything selected (both sides). With nothing selected, the resource under the mouse is removed (Section 33b).
- **Debug view (`DebugOverlayView`):**
  - One texture over the whole map, 8 pixels per cell: cell lines, cells unwalkable because of the ground (water, cliffs) dark, cells held by a building or resource red, stairs blue. Redrawn the next time it shows after a `GridChangedEvent`.
  - The remaining path of every selected unit on the move (`UnitModel.CopyRemainingPath`) is shown with dots 0.3 units apart, the target with a big dot. Player white, enemy red. The dots come from a pool, 800 at most.
  - Prefab: `Prefab_Gameplay_Debug_Overlay`. It isn't in the scene; `GameplayLifetimeScope` creates and injects it with `RegisterComponentInNewPrefab`. While off it draws nothing and never creates its texture.

## 33b. Time Control and Deleting
**Status:** ✅ · **Files:** `Gameplay/TimeControl/*`, `UI/Hud/TimeControlView.cs`, `Gameplay/Selection/DeletionService.cs`, `DeleteInputHandler.cs` · **Test:** `SandboxToolsTests`

- **In both modes.** *II, 1x, 2x, 3x* buttons left of the calendar on the resource bar; keys **P** (pause/resume), **+** and **-** (speed; bound by character, they work on every keyboard layout and on the numpad).
- `TimeControlService` only changes the `GameClock`; the calendar, the raid countdown and every system follow it, so no extra code is needed. Picking a speed also ends the pause. After the game ends (`GameEndedEvent`) the clock can't be started again.
- **Deleting (Delete key):** `DeletionService.CanDelete` keeps the rule in one place. In the Sandbox everything; in Normal mode only the player's units and buildings, never the Castle (deleting it by accident would end the game). Deleting publishes the same events as death (`UnitDiedEvent`, `BuildingDestroyedEvent`), so selection, panel, grid and attackers are cleared as usual. No refund. If something that can't be deleted is selected, "Can't delete this" shows.
- A deleted resource is emptied first; a villager working on it moves on to the next one as with a depleted resource.

---

# G. Planned Systems
How each would fit into the current structure. When done, it moves into its own section.

### Storage and Capacity ⏳
- **How:** `StorageService`, like `PopulationService`, sums the capacity from buildings on every query (`BuildingDefinition.storage`: kind + amount; a small starting capacity from the Castle). `ResourceService.Add` caps at the capacity; when full, `GatheringSystem` makes the villager wait and shows "Storage full". The drop-off becomes "the nearest building that takes that kind". When one falls, the excess is lost.
- The visuals are ready: `Texture_Buildings_<Color>_Storage_*`.

### Building Upgrades ⏳
- **How:** `BuildingModel.Level`. Per-level bonuses in separate data (`UpgradeDefinition`: cost, HP, population, tower damage and archer count, territory radius, storage capacity). The definition doesn't change; the places that read values read "definition + level" through one helper such as `BuildingStats`. `UpgradeService.TryUpgrade` takes the cost and runs `TerritoryService.Rebuild` if needed. The Barracks level passes to the soldiers it produces as `UnitModel.Level`.
- An upgrade button on the panel, a rank badge on units.

### Monastery and Monk ⏳
- **How:** A new `UnitDefinition` (heal amount, range, cooldown). In the Combat phase, `HealSystem` picks the most hurt ally in range, `CombatService.Heal`. The `Monk` and Heal VFX frames are added to the color sets. The building uses the `Monastery` textures, data only.

### Repair and Cancelling Construction ⏳
- **Repair:** `ConstructionSystem` runs the same loop on a damaged finished building: adds HP instead of progress, spending a little per point. The same right click rule: right click your own damaged building with a villager, the cursor is the hammer again.
- **Cancel:** A cancel button on the panel for a construction site; refunds half the cost and calls `BuildingService.Remove`.

### Sheltering in Houses and Capacity ⏳
- **How:** `BuildingDefinition.shelterCapacity`. `ShelterSystem` tracks how full each building is and picks the nearest one with room. When a building falls, it brings those inside out with `BringOut`.

### Minimap ⏳
- **How:** `MinimapView` in the UI: a single `Texture2D` from `GridMap` and the resources (the ground once, buildings and resources through events), units as dots every 0.25 s. The camera's view as a frame. A click focuses the `CameraController`, a right click calls `UnitCommandService.MoveTo`. No second camera (SetPass).

### Random Ground Generation ⏳
- **How:** `Map/MapGenerator` fills the tilemaps from the seed, and `TilemapGridSource` builds the grid afterwards; everything else works unchanged. Island shape, plateaus and stairs with noise; path validation the same way as the flood fill in `ResourceDistributor`. Settings in `MapGenerationSettings`.

### Saving ⏳
- **How:** `SaveService` in Core, JSON, `Application.persistentDataPath`, several slots. Seed + changes: buildings, units (with their orders), resource amounts, the day, resources. Definitions are written by their `Id`. Loading: the map is built from the seed and the save is applied on top.

### Endless Mode ⏳
- **Endless mode:** After the last raid, `RaidService` generates new raids from a formula. *Continue* on `GameEndView`; the days survived on the HUD.

### Sound ⏳
- **How:** `AudioService` in the root scope (music and pooled `AudioSource`s). A bridge in Gameplay turns events into sounds (`UnitAttackedEvent` → sword, `BuildingDestroyedEvent` → collapse, raid start → horn). Game code doesn't know about sound.

### Others ⏳
- A separation force while walking, a tutorial, enemy settlements, costs on Production Menu cells.

---

# H. Recipes: Adding Something New

### New building (no code)
1. Create `GameDatas/Buildings/Data_Building_<Name>`: size, `VisualHeight`, HP, cost, `buildTime`; production, defense and territory if needed.
2. Add the building's sprite and grey `siteSprite` to every `Data_TeamColor_*` asset (without a grey one, the site shows with the colored sprite).
3. Add it to the `Data_Database_Game > Buildings` list. It shows in the menu in this order.

### New unit (no code)
1. `Data_Unit_<Name>`: HP, damage, range, speed, population, cost, portrait.
2. Fill in the unit's frames in the color sets (idle, run, attack; axe and pickaxe for a villager).
3. Add it to the `producibles` (or `normalModeProducibles`) list of the building that produces it.

### New simulation system
1. Implement `ISimulationSystem` and pick the right `Phase`.
2. `builder.RegisterSimulationSystem<T>()` in `GameplayLifetimeScope`.
3. Keep it plain C#; drive it in tests with `SimulationRunner.Step` or `Tick` directly.

### New event
1. `readonly struct XxxHappenedEvent : IGameEvent`, read-only fields only.
2. The publisher calls `_eventBus.Publish(new ...)`. The listener calls `Subscribe<...>(...).AddTo(_subscriptions)` and `Dispose` when it goes away.
3. Add it to the event table in this document.

### New world view (in the scene)
1. A MonoBehaviour that gets services through `[Inject] Construct(...)` and reads them in `LateUpdate` or listens to events.
2. Make a prefab (`Prefab_Gameplay_<Category>_<Name>`) and put it in the scene under `----- WORLD -----`.
3. `RegisterComponentInHierarchy<T>()` in `GameplayLifetimeScope`. If forgotten, `[Inject]` doesn't run; if it isn't in the scene, the scope isn't built.

### New UI view
The same, but the prefab is `Prefab_UI_<Category>_<Name>`, it goes on the Gameplay canvas and is registered in `GameplayUiLifetimeScope`. If it changes often, put it on the `DynamicCanvas`.

### A rule for Normal mode only
Switch the rule off inside the service with a single flag such as `IsEnabled` / `IsUnlimited`; callers don't need to know the mode. The Sandbox behavior mustn't change, and there should be a test for that.

---

# I. Known Limits
- Units pass through each other while walking; they spread out only at their targets.
- A melee unit below a plateau's edge can hit a target on top; range doesn't look at levels.
- No damage numbers (e.g. "-4"); a hit shows as a red flash and on the health bar.
- The territory ring is a true circle, the territory layer is cell by cell; they can differ by up to one cell.
- The territory layer is redrawn from scratch whenever the footprint moves to another cell; if it stutters on a big map, only the changed area will be redrawn.
- At the farthest zoom, the top row of the map can sit under the resource bar.
- The archer count on towers and the Outpost's radius are fixed until the upgrade system comes.
- Construction can't be cancelled; there is no repair.
- The tool cursor is a 32x32 hardware cursor; it looks small at very high DPI.
