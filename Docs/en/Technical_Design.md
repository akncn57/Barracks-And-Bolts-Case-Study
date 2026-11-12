# Barracks & Bolts – Technical Design Document

**English** | [Türkçe](../tr/Technical_Design.md)

> Game design: [GDD_Decisions.md](GDD_Decisions.md) · How the systems work: [Systems.md](Systems.md) · Priorities: [Roadmap.md](Roadmap.md) · Assets: [Missing_Assets.md](Missing_Assets.md)
> Unity 6000.3.16f1 (6.3 LTS) · URP 2D · Input System · VContainer · UniTask · DOTween · Cinemachine 3
>
> This document covers the rules and the architecture. For "how does the code work", system by system, see [Systems.md](Systems.md).

---

## 1. Core Decisions
| Topic | Decision |
|---|---|
| Architecture | Game logic lives in **plain C# services**; MonoBehaviours only handle the **view** and input. VContainer wires them together. |
| Communication | Systems notify each other through a **central event bus** with typed messages. |
| Updates | Units and systems are updated by **one central tick**, in a fixed order. MonoBehaviour `Update` isn't used for game logic. |
| Animation | **No Animator.** Units play the sprite frames of their color set on the game clock themselves (see Section 13). |
| Scenes | **Bootstrap → MainMenu → Gameplay** |
| Tests | **EditMode unit tests** for the important systems |
| Language | Code, comments, commit messages and the README are in **English**. The documents come in two languages: `Docs/en` (English) and `Docs/tr` (Turkish), with the same file names. |
| Git | Only the `main` branch. Every commit compiles and runs. |
| Camera | **Cinemachine 3** drives the real camera. Input, panning, zoom and map bounds are in our `CameraController`; it moves the follow target and sets the lens size. |
| Async | **UniTask** instead of coroutines. **DOTween** for UI animations and feedback effects. |

---

## 2. Folder Structure
```
Assets/
├── Atlases/                    Atlases_Gameplay, Atlases_UI, Atlases_Effects
├── Fonts/
├── GameDatas/                  ScriptableObject data (popup data under UI/: Data_Popup_<Name>)
│   ├── Buildings/              Data_Building_<Name>.asset
│   ├── Units/                  Data_Unit_<Name>.asset
│   ├── ResourceNodes/          Data_Resource_<Name>.asset
│   ├── TeamColors/             Data_TeamColor_<Color>.asset
│   └── Configs/                Data_Config_<Name>.asset (map, calendar, difficulty...)
├── Materials/                  TMP presets under Font/ (Outline, Shadow, Outline Shadow)
├── Plugins/                    Third-party libraries (not touched)
├── Prefabs/
│   ├── Core/                   Prefab_Core_<Name>LifetimeScope
│   ├── Gameplay/               Prefab_Gameplay_<Name>
│   │   ├── Buildings/
│   │   ├── Units/
│   │   ├── ResourceNodes/
│   │   ├── Combat/
│   │   ├── Placement/
│   │   ├── Selection/
│   │   ├── Commands/
│   │   ├── Hover/
│   │   └── Territory/
│   └── UI/                     Prefab_UI_<Category>_<Name>, Canvases/ (Main + MainMenu and Gameplay variants), Popups/
├── Resources/                  Only files that libraries require (e.g. DOTweenSettings). Everything in a folder with this name goes into the build, so it isn't used for anything else
├── Scenes/                     Bootstrap, MainMenu, Gameplay
├── Scripts/                    (see Section 3)
├── Settings/                   URP, Renderer2D, Input Actions
├── Textures/                   Texture_<Folder path>_<Name>.png (by color: Red/, Yellow/)
│   ├── Backgrounds/            Menu background; outside UI/ so it stays out of the UI atlas
│   └── Markers/                Markers in the world (selection ring, placement cell, status and carry icons); colored ones in Markers/<Color>/
│   (Buildings/Neutral/ for buildings without a team color, e.g. the farm)
├── Tiles/
│   ├── Palettes/               Palette_<Name>.prefab
│   └── <Category>/             Tile_<Category>_<Name>.asset
└── TextMesh Pro/
```

---

## 3. Code Modules and Assembly Definitions
Each module has its own `.asmdef`. This shortens compile times and **enforces the direction of dependencies** (UI knows Gameplay; Gameplay can't know UI).

```
Scripts/
├── Core/        BarracksAndBolts.Core       Infrastructure: EventBus, simulation loop, game clock, scene loading, session settings, player settings, root scope
├── Gameplay/    BarracksAndBolts.Gameplay   Game logic and world views
├── UI/          BarracksAndBolts.UI         Main menu, panels, HUD, settings, infinite scroll, tooltips, floating text
├── Popup/       BarracksAndBolts.Popup      A ready-made popup system (the settings window uses it)
├── Utils/       BarracksAndBolts.Utils      Helpers with no Unity dependency (MinHeap, CompositeDisposable)
└── Tests/
    └── EditMode/ BarracksAndBolts.Tests.EditMode   521 tests
```
- Player settings are in `Core/Settings` (`GameSettings`, PlayerPrefs). There is no save system yet; when it comes, it goes into Core.
- One-off editor tools, when needed, go under `Assets/Editor/` without an asmdef and are deleted once done (the prefab conversion tool was deleted). The audio and animation folders were empty and were removed; animation frames are in the color sets, there is no Animator (Section 13).

**Dependency direction** (arrows mean "depends on"):
```
UI ──► Gameplay ──► Core ──► Utils
 └──► Popup (doesn't depend on our modules)
Tests.EditMode ──► Gameplay, UI, Core, Utils
```

### Gameplay layout (namespace = folder)
```
Gameplay/
├── Bootstrap/      GameplayLifetimeScope
├── Data/           BuildingDefinition, UnitDefinition, ResourceNodeDefinition, TeamColorDefinition, GameDatabase,
│                   NormalModeSettings, RaidSettings, ResourceDistributionSettings, ResourceCost
├── Entities/       GameEntityId, GameEntityIdGenerator
├── Grid/           GridMap, TilemapGridSource, GridChangedEvent
├── Pathfinding/    AStarPathfinder, PathStatus, GridSearch
├── Placement/      PlacementService, PlacementInputHandler, PlacementPreviewView, Placement*Event
├── Buildings/      BuildingService, BuildingModel, BuildingView, BuildingViewSpawner, BuildingSpriteLayout,
│                   BuildingDefenderView, CoverageRingView, BuildProgressBarView, BuildingCompletedEvent,
│                   Production/ (UnitProductionService)
├── Units/          UnitService, UnitModel, UnitMovementSystem, UnitView, UnitViewSpawner, Unit*Event
├── Territory/      TerritoryService, TerritoryOverlayView
├── Combat/         CombatService, CombatSystem, BuildingDefenseSystem, DeathCleanupSystem, AttackMoveSystem, DamageFlash,
│                   ProjectileSpawner, ProjectileView, HealthBarView, CombatEvents
├── Effects/        EffectSpawner (death and destruction effects, built by the scope from a prefab)
├── Hover/          HoverService, HoverInputHandler, HoverHighlightView, ToolCursor, ToolCursorPicker, ToolCursorView
├── Feedback/       FloatingTextRequestedEvent (drawn by UI/Feedback/FloatingTextSpawner)
├── Selection/      SelectionService, SelectionInputHandler, BoxSelectionView, DeletionService, DeleteInputHandler,
│                   ControlGroupService, IdleVillagerFinder
├── Commands/       UnitCommandService, CommandInputHandler, HotkeyInputHandler, AttackMoveTargeting, MoveMarkerView
├── Sandbox/        SandboxService (side, Enemy AI and Debug switches)
├── TimeControl/    TimeControlService, TimeControlInputHandler (both modes)
├── DebugView/      DebugOverlayView (Sandbox: grid and A* paths)
├── Teams/          Faction, TeamService (TeamColor is in Core.Session, since it travels from the menu)
├── ResourceNodes/  ResourceNodeService, ResourceNodeModel, ResourceDistributor, ResourceMapInitializer,
│                   ResourceRoamSystem, ResourceNodeView, ResourceNodeViewSpawner
├── Economy/        ResourceService, PopulationService, NormalModeStarter, ResourceRespawnService, GameOutcomeService, HungerSystem
├── Gathering/      GatheringSystem, FarmingSystem
├── Construction/   ConstructionSystem (construction sites; the state is in BuildingModel, BuildingCompletedEvent)
├── Calendar/       CalendarService (views: UI/Hud/DayNightOverlayView, CalendarLabelView)
├── Raids/          RaidService, BattleAiSystem, ShelterSystem
├── Input/          GameInputActions (generated), InputModeController
└── CameraControl/  CameraController, CameraMath, CameraConfig
```
Planned folders: `Map/` (ground generation), `Upgrades/`, `Storage/`, `Minimap/`, `Save/`. Details: [Systems.md](Systems.md).

### UI layout
```
UI/
├── Bootstrap/      GameplayUiLifetimeScope
├── MainMenu/       MainMenuLifetimeScope, MainMenuView, MainMenuBackgroundView, DifficultyPopup
├── Common/         SlideOutPanel, ContentFitPanel, PanelSizeMath, EscapeInputHandler, ICloseablePanel, IEscapeMenu,
│                   UiScaleApplier, ButtonFeedback, ButtonFeedbackInstaller, DifficultyText
├── Settings/       SettingsPopup, SettingsButtonView, ISettingsContext (MainMenu / Gameplay), ControlsPopup, ControlsLauncher, ControlsText
├── Scroll/         RecyclingScrollView, RecyclingScrollCell, RecyclingScrollMath
├── Production/     ProductionMenuView, ProductionMenuCell
├── Information/    InformationPanelView, ProducibleUnitCell, UnitSelectionCardView
├── Hud/            ResourceBarView, CalendarLabelView, DayNightOverlayView, NoticeBannerView, GameEndView, TimeControlView, StatsPanelView, RaidArrowView, OffScreenArrowMath, CommandBarView, IdleVillagerButtonView
├── Feedback/       FloatingTextSpawner, FloatingTextView, HoverLabelView
├── Tooltips/       TooltipView, TooltipPlacement, TooltipText, HoverTooltip
└── Sandbox/        SandboxFactionToggleView, SandboxSwitchesView
```

---

## 4. Architectural Layers
```
 ┌──────────────┐  reads   ┌──────────────────┐  publish   ┌───────────┐
 │  Definitions  │ ───────► │  Services (C#)   │ ─────────► │ EventBus  │
 │ (ScriptableO.)│          │  Model + Logic   │            └─────┬─────┘
 └──────────────┘          └────────▲─────────┘                  │ subscribe
                                    │ calls (intent)              ▼
                           ┌────────┴─────────┐         ┌──────────────────┐
                           │ Input / UI Views │         │  Views (Mono)    │
                           └──────────────────┘         │ Sprites, frames  │
                                                        └──────────────────┘
```
- **Definition (ScriptableObject):** Design data that doesn't change. Name, visual, size, HP, damage, cost. **Never** changed at runtime.
- **Model (plain C#):** Runtime state. Example: `BuildingModel` (Id, Definition, Faction, Area, CurrentHp). No Unity dependency, testable.
- **Service (plain C#):** Applies the rules and manages the models. Examples: `BuildingService.TryPlace(...)`, `CombatService.ApplyDamage(...)`. Publishes the results as events.
- **View (MonoBehaviour):** Shows the model on screen (sprite, animation, health bar). No logic. Listens to events or reads the model.
- **Input / UI:** Passes the player's intent to the services (e.g. "send these units there"). Doesn't apply the result itself.

**Rule:** A service never reaches a View directly. Views may reach services.

### Identities
Every building and unit has a `GameEntityId` (an increasing `int`). Services refer to each other's entities by `GameEntityId` instead of object references. So:
- No reference to a destroyed target is left hanging. The target is checked with `TryGet` every time.
- A save system can write the ids directly.

---

## 5. VContainer Scopes
| Scope | Scene | Lifetime | Registrations |
|---|---|---|---|
| `ProjectLifetimeScope` | Root prefab | Whole application (DontDestroyOnLoad) | `SceneLoader`, `GameSessionSettings`, `GameSettings` + `PlayerPrefsSettingsStorage`, `DisplaySettingsApplier`. Planned: `SaveService`, `AudioService` |
| `BootstrapLifetimeScope` | Bootstrap | Scene | `BootstrapFlow` (loads the MainMenu) |
| `MainMenuLifetimeScope` | MainMenu | Scene | `MainMenuView`, `UiScaleApplier`, `PopupSystem`, `SettingsButtonView`, `MainMenuSettingsContext` |
| `GameplayLifetimeScope` | Gameplay | Scene | `EventBus`, `GameClock`, `SimulationRunner`, every gameplay service and system, world views (`RegisterComponentInHierarchy`) |
| `GameplayUiLifetimeScope` | Gameplay (canvas) | Scene, parent = Gameplay | Every UI view, `PopupSystem`, `SettingsButtonView` (`IEscapeMenu`), `GameplaySettingsContext`, `EscapeInputHandler` and `ControlsLauncher`. A separate scope because UI types can't be seen from the Gameplay assembly |

- `EventBus` is registered in the **Gameplay scope**. When the scene closes, every subscription goes with it; nothing leaks between scenes.
- `GameSessionSettings` carries the new game settings (mode, color, difficulty, seed) from the menu into Gameplay.
- `ProjectLifetimeScope` is a prefab set as the **Root Lifetime Scope** in the `VContainerSettings` asset. VContainer creates it automatically when any scene opens. So when the Gameplay scene is started directly in the editor, the project services are still there and it runs with defaults (Sandbox, Red, a random seed).

---

## 6. Event Bus
```csharp
public interface IEventBus
{
    void Publish<TEvent>(in TEvent gameEvent) where TEvent : struct, IGameEvent;
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;
}
```
- Events are `readonly struct`s (no GC). Naming: **`<Subject><PastTenseVerb>Event`**, e.g. `BuildingPlacedEvent`, `UnitDiedEvent`, `SelectionChangedEvent`.
- `Subscribe` returns an `IDisposable`. Subscribers release it in `OnDestroy`/`Dispose`, with the help of a `CompositeDisposable`.
- Publishing walks a copy of the subscriber list, so a list that changes during a publish (e.g. a handler removing another subscription) causes no trouble.
- **An event isn't a command.** "Do this" is a service method call. An event only says "this happened".

---

## 7. Simulation Loop (Central Tick)
- `SimulationRunner` implements VContainer's `ITickable` and calls the systems every frame in a **fixed order** (`SimulationPhase`):

| Phase | Value | Systems |
|---|---|---|
| `Input` | 0 | Empty. Input handlers run outside the simulation, as `IPostTickable` |
| `Time` | 100 | `CalendarService`, `HungerSystem` |
| `AI` | 200 | `RaidService`, `BattleAiSystem`, `ShelterSystem`, `AttackMoveSystem` |
| `Movement` | 300 | `UnitMovementSystem`, `ResourceRoamSystem` |
| `Gathering` | 350 | `GatheringSystem`, `ConstructionSystem`, `FarmingSystem` |
| `Combat` | 400 | `CombatSystem`, `BuildingDefenseSystem` |
| `Cleanup` | 500 | `DeathCleanupSystem` |
| `Presentation` | 600 | Empty. Views read the models in their own `LateUpdate` |

- Systems implement `ISimulationSystem.Tick(float deltaTime)` and are registered with `builder.RegisterSimulationSystem<T>()`.
- **Frame order:** `SimulationRunner.Tick` (ITickable) → MonoBehaviour `Update` (camera) → input handlers (`IPostTickable`, they see where the camera is this frame) → the views' `LateUpdate`. A player's order this frame enters the simulation the next frame.
- `GameClock` holds game time: **pause** (`IsPaused`) and **game speed** (`TimeScale`) are applied only here. `Time.timeScale` isn't used, so UI animations aren't affected by pausing.
- Deaths are processed together at the end of the tick. The order in which units damaged in the same frame are handled can't change the result, and nothing is removed from a collection while it is being walked.

---

## 8. Grid and Pathfinding
- **1 cell = 1 Unity unit = 64px** (64 PPU for in-game sprites). The visual side runs on Unity's `Grid` + `Tilemap`, the logic on `GridMap`.
- `GridMap` keeps, for every cell: whether the ground is walkable, which building or resource is on it (`GameEntityId`), its level, and whether it is a ramp. Territory lives in a separate array in `TerritoryService`; it doesn't change the grid.
- **Height:** Every cell has a level (0 flat ground, 1 plateau). Two neighboring cells connect only on the same level or over a ramp. A building must stand on one level and can't go on a ramp.
- **Units don't occupy cells.** Only buildings and resources block. This way units can't lock each other in. `GridSearch.CollectDestinations` keeps them from stacking at their targets; there is no light separation force yet, so units can pass through each other while walking. A building can't be placed on a cell with a unit on it; otherwise the unit would end up inside the building.
- **A\***:
  - 8-directional movement. No corner cutting (no diagonal step between two blocked sides).
  - Octile distance heuristic.
  - Our own `MinHeap` for the open list (.NET Standard 2.1 has no `PriorityQueue`).
  - Node buffers are cleared after a search and reused in the next one; no new memory per search.
  - If the target can't be walked on, the nearest walkable cell is used. If there is no path at all, the unit goes to the nearest reachable cell.
- **Group movement:** `GridSearch.CollectDestinations` starts from the free cell nearest the target, spreads with a BFS over walkable cells only, and gives every unit its own cell.
- **Dynamic obstacles:** Placing or removing a building publishes `GridChangedEvent`. Units whose path crosses the change look for a new path.

---

## 9. Data Definitions (ScriptableObject)
| Type | Main Fields |
|---|---|
| `BuildingDefinition` | `Id`, `DisplayName`, `Size`, `VisualHeight`, `MaxHp`, `Cost`, `IsDropOff`, `PopulationCapacity`, `Producibles`, `NormalModeProducibles`, `SpawnOffset`; Defense: `Defender`, `DefenderPosition`, `DefenseRange`, `DefenseDamage`, `DefenseCooldown`; `TerritoryRadius`, `IsNormalModeOnly`. No sprite: it depends on the color, so it is in `TeamColorDefinition` |
| `UnitDefinition` | `Id`, `DisplayName`, `Portrait`, `MaxHp`, `Damage`, `AttackRange`, `AttackCooldown`, `MoveSpeed`, `PopulationCost`, `Cost`, `CanGather` |
| `ResourceNodeDefinition` | `Id`, `DisplayName`, `Kind` (Wood, Stone, Food, Gold), `Size`, `VisualHeight`, `Amount`, `WorkDistance`, `Frames`, `RoamRadius`, `WalkFrames`, `IsOneCut` |
| `TeamColorDefinition` | `Color`, building → sprite and grey construction sprite, unit → portrait and frames (idle, run, attack, axe, pickaxe, hammer) |
| `GameDatabase` | `Buildings` (Production Menu order), `Units`, `TeamColors`, `ResourceDistribution`, `NormalMode`, `Raids` |
| `NormalModeSettings` | Castle, villager, starting villagers and resources, carry capacity, work times, sheep numbers |
| `RaidSettings` | Day and night length, raid days and waves, start window, delay between waves, spawn distance, `aggroRange`, `shelterRadius` |
| `ResourceDistributionSettings` | Clear-area radii and resource groups |
| `CameraConfig` | Pan, zoom and bounds values |

- The brief's values live in these assets: Barracks 100 HP, House 50 HP, soldiers 10 HP and 10 / 5 / 2 damage.
- Difficulty isn't a separate asset: its values sit in the settings of what they affect (`RaidSettings.ScaleCount`, `NormalModeSettings.ScaleStartingAmount`, `GetHunger`). This keeps all the numbers of a system in one asset. The session's `Difficulty` is read on every use.
- All definitions are collected in a `GameDatabase` asset and registered with VContainer through it.
- Every definition's `OnValidate` catches invalid values in the editor: size ≤ 0, HP ≤ 0, a null entry in `Producibles` and so on.

---

## 10. Rendering and SetPass (< 20)
- Every in-game sprite is packed into **Sprite Atlases**. Atlases reference parent folders, so new subfolders are included automatically.
- A single sprite material is used (URP `Sprite-Lit-Default`). Custom shaders are kept to a minimum.
- **Health bars:** SpriteRenderers (`HealthBarView`, bar sprites from the atlas) instead of a world-space Canvas, so they batch with the units. Shown when hurt or selected.
- **Depth sorting:** Transparency Sort Mode = Custom Axis (0, 1, 0) in the Renderer2D. Pivots are at the base. Building and unit roots have a Sorting Group.
- **Textures made in code:** The territory layer (`TerritoryOverlayView`) and coverage rings (`CoverageRingView`) are drawn once into a `Texture2D` at runtime; being outside the atlas, each adds a draw call, and they show only during placement or selection.
- **Minimap (planned):** No second camera. A single `Texture2D` built from grid data.
- **UI:** Static and frequently changing elements are split into separate Canvases (`StaticCanvas` / `DynamicCanvas`); that's why the FPS panel is on the `DynamicCanvas`. Popups have their own `PopupCanvas` (sort order 100).
- **Main menu background:** One PNG composed in advance from the game's sprites, drawn by a single Image. It stays out of the UI atlas (outside `Textures/UI`) so it doesn't bloat it.
- **Text materials:** The outline and shadow TMP presets share the same font atlas; no extra texture, only one draw per material.
- **Night effect:** A single semi-transparent UI image over the screen (`DayNightOverlayView`), its alpha from the calendar. No lighting is used.
- **Draw order (Order in Layer):** water -5, foam -4, ground -3, plateaus and stairs -2, territory layer and rings -1, buildings, units and resources 0, a villager inside a farm 1, placement preview 99-100, explosion effect 145, arrows and debug path dots 150, construction bar 155, health bar 158, status icon 160, hover frame 190, selection box 200.
- Measured with the Frame Debugger and Game view Stats: **10 SetPass** during a battle. After every feature, in the 150-unit test battle (+100 Units): **16 SetPass, 231 draw calls**. The increase comes from health bars, effects, status icons and the new UI panels.

---

## 11. Object Pooling and the Infinite Scroll
- There is no separate `PoolService`. Visuals that appear often use Unity's `UnityEngine.Pool.ObjectPool<T>` directly: arrows (`ProjectileSpawner`) and floating text (`FloatingTextSpawner`). The soldier cells of the Information Panel and the cells of the placement preview are pooled by hand: extras aren't destroyed, they are switched off.
- Unit and building views aren't pooled; deaths and destruction are rare enough for `Instantiate`/`Destroy`. The renderers of death and destruction effects are pooled in `EffectSpawner`.
- **Production Menu (infinite scroll):** `RecyclingScrollView`
- **Resource bar:** `ResourceBarView`, full width at the top. The panels below start under it.
- **Panel sliding in from the left edge:** `SlideOutPanel` (DOTween). The Production Menu starts closed; its button stays on screen while it is closed. While something is being placed, the menu closes and comes back afterwards (`ProductionMenuView`, through the placement events).
  - No ScrollRect; dragging, flinging and the wheel are in the component itself. Scrolling is a single offset value.
  - Only as many rows as are visible + 1 are created (half a row can show at the top and the bottom at the same time while scrolling).
  - A row that leaves the view while scrolling moves to the other end. The data index comes from a non-negative modulo: `(row * columns + column) mod dataCount`.
  - A cell is rebound only when the item it shows changes; while the list is still, no RectTransform changes (no canvas rebuild).
  - The offset wraps around the loop length at which the items repeat their positions, so float precision doesn't drift.
  - If the list is shorter than the visible area (data count < visible cells), the items repeat to fill it without gaps.

---

## 12. Input
- The default `InputSystem_Actions` template was removed in favor of `Settings/Input/GameInputActions.inputactions`, with a generated C# class. Maps: **Camera** (always on), **Gameplay** and **Placement** (one of the two is on). The Gameplay map also has `Delete`, `TogglePause`, `SpeedUp`, `SpeedDown`, `IdleVillager` (bound to the "." character, `#(.)`, plus the numpad; not to the physical key, since on a Turkish Q keyboard the US period key types "ç"), `ControlGroup` (1–9; which key it was is read from `activeControl`), `GroupModifier` (Ctrl), `AttackMove` (F), `Stop` (X), `HoldPosition` (H), `FocusHome` (Home), `ShowControls` (F1); the speed keys are bound by character (`<Keyboard>/#(+)`) and work on every keyboard layout. A Pause map would come with an in-game menu. When the asset changes, the generated class must change too; Unity regenerates it when it imports the asset.
- There is no single big input service; every feature has its own small handler:

| Class | Job |
|---|---|
| `InputModeController` | Mode switching: on `PlacementStartedEvent` it turns Gameplay off and Placement on, and the reverse on `PlacementEndedEvent`. One mode at a time |
| `PlacementInputHandler` | Placement: moves the footprint with the mouse, confirms, cancels |
| `SelectionInputHandler` | Left click and box selection |
| `CommandInputHandler` | Right click; the decision by context is in `UnitCommandService.CommandAt`: enemy → attack, resource → gather, ground → move |
| `HoverInputHandler` | What is under the mouse |
| `DeleteInputHandler` | Delete: removes the selection (the rules are in `DeletionService`) |
| `TimeControlInputHandler` | P pauses, + and - change the speed |
| `HotkeyInputHandler` | Shortcuts: F (Attack Move), X, H, ., Ctrl+1–9 / 1–9, Home. The buttons over the unit card call the same methods |
| `ControlsLauncher` (UI) | F1 opens and closes the Controls page |
| `EscapeInputHandler` (UI) | Each Esc closes the top layer (placement, an Attack Move waiting for its point, a popup, the selection, the menu); with nothing to close, it opens the settings |
| `CameraController` | Panning, dragging, zoom |

- Every handler ignores clicks into the world while the mouse is over UI (`EventSystem.IsPointerOverGameObject`).

---

## 13. Animation and the Color System
- **Decision: no Animator.** The first plan was an Animator Controller per unit and an override per color. For 4 units x 2 colors, sprite arrays were chosen instead: `TeamColorDefinition.UnitSpriteSet` holds a unit's idle, run, attack, axe and pickaxe frames.
- `UnitView` computes the frame from `GameClock.ElapsedTime`: units freeze when the game pauses and speed up with it. The phase comes from the unit id, so a group doesn't breathe in sync. Priority: attack > work > run > idle.
- Resources (trees, sheep) and the archer on the tower play their frames the same way; `ResourceNodeViewSpawner` updates all the trees in one loop.
- If a color is missing frames, a warning is logged and an empty set is returned. Buildings aren't animated. Color changes are made by swapping sprites through `TeamColorDefinition`.
- There is no separate death or destruction animation; `EffectSpawner` plays dust and explosions from the `Textures/VFX` frames (on the game clock).
- The code never derives the side from the color: `Faction` (Player/Enemy) and `TeamColor` (Red/Yellow) are independent.

---

## 14. Naming Standards

### C#
| Item | Rule | Example |
|---|---|---|
| Namespace | `BarracksAndBolts.<Module>.<Sub>` (same as the folders) | `BarracksAndBolts.Gameplay.Grid` |
| Class, struct, enum, method, property, event | PascalCase | `BuildingService`, `TryPlace()` |
| Interface | `I` prefix | `IEventBus` |
| Private field | `_camelCase` | `_gridMap` |
| `[SerializeField]` private field | `camelCase` | `popupContainer` |
| Constant, static readonly | PascalCase | `MaxSelection` |
| Bool | `Is` / `Has` / `Can` prefix | `IsAlive`, `CanProduce` |
| Async method | `Async` suffix, returns `UniTask` | `LoadSceneAsync()` |
| Event (C#) | Past tense | `Destroyed`, `SelectionChanged` |
| Event (bus) | `<Subject><Verb>Event` | `UnitDiedEvent` |

- One type per file, and the file name matches the type name.
- Public APIs get XML docs (`/// <summary>`). Comments explain **why**, not **what**.
- No magic numbers. Values live in a Definition/Config or in a named constant.
- Local variables always use `var` (including `foreach`, `for` and `out var`). The only exception is a variable initialized with `null`; C# requires its type to be written.
- No `GetComponent` or `Find` calls in code that runs often at runtime. References come from the Inspector or through Inject.

### Assets
| Kind | Pattern | Example |
|---|---|---|
| Texture | `Texture_<Folder path>_<Name>` | `Texture_Units_Red_Warrior_Idle` |
| Prefab | `Prefab_<Category>_<Name>` | `Prefab_Gameplay_Building_Barracks`, `Prefab_UI_Canvas_Main` |
| ScriptableObject | `Data_<Type>_<Name>` | `Data_Building_Barracks` |
| Animation Clip | `Anim_<Color>_<Unit>_<Action>` | `Anim_Red_Warrior_Run` |
| Animator | `Animator_<Unit>` / `AnimatorOverride_<Color>_<Unit>` | `AnimatorOverride_Yellow_Lancer` |
| Material | `Material_<Name>` | `Material_TerritoryOverlay` |
| Audio | `SFX_<Category>_<Name>`, `Music_<Name>` | `SFX_Combat_SwordHit` |
| Tile | `Tile_<Category>_<Name>` | `Tile_Terrain_Ground_Color_01_00` |
| Tile Palette | `Palette_<Name>` | `Palette_Terrain` |
| Scene | PascalCase | `Gameplay` |

### Scene Hierarchy (Gameplay)
```
Prefab_Core_GameplayLifetimeScope
----- CAMERAS -----
    Main Camera                (CinemachineBrain)
    CameraTarget               (CameraController)
    CinemachineCamera_Gameplay
----- GRIDS -----
    Grid
        Tilemap_Water      (order -5)
        Tilemap_Foam       (order -4, animated foam)
        Tilemap_Ground     (order -3, flat ground; a filled cell is walkable)
        Tilemap_Elevated   (order -2, plateaus and cliffs; a filled cell is high ground)
        Tilemap_Stairs     (order -2, stairs; a filled cell is a ramp)
        Tilemap_Blocking   (not drawn; a filled cell is unwalkable)
----- WORLD -----
    Prefab_Gameplay_Building_Spawner      (parent of what is made at runtime, BuildingViewSpawner)
    Prefab_Gameplay_Placement_Preview     (placement preview; in Normal mode it creates Prefab_Gameplay_Territory_Overlay next to itself)
    Prefab_Gameplay_Unit_Spawner          (parent of what is made at runtime, UnitViewSpawner)
    Prefab_Gameplay_ResourceNode_Spawner  (parent of the resources, ResourceNodeViewSpawner)
    Prefab_Gameplay_Selection_Box         (box selection frame, BoxSelectionView)
    Prefab_Gameplay_Move_Marker           (right click feedback, MoveMarkerView)
    Prefab_Gameplay_Projectile_Spawner    (pooled arrows, ProjectileSpawner)
    Prefab_Gameplay_Hover_Highlight       (frame around what is under the mouse, HoverHighlightView)
    Prefab_UI_FloatingText_Spawner        (pooled floating text, FloatingTextSpawner)
----- MANAGERS -----
    (empty for now; only for bridges that need a MonoBehaviour)
----- UI -----
    EventSystem
    Prefab_UI_Canvas_Gameplay_Variant (GameplayUiLifetimeScope, parent = GameplayLifetimeScope)
        StaticCanvas / DynamicCanvas (the Production Menu and Information Panel are here; their content changes during play)
```
- The scene has no lights; sprites are drawn unlit, and the night effect is a UI layer.
- Separator objects (`----- X -----`) are empty GameObjects. Their transforms stay at zero, with no components.
- Objects made at runtime always go under their parent. Nothing is added at the root of the hierarchy.
- Scopes find objects by component type, not by name; renaming an object doesn't change behavior.
- The scene follows this layout. `----- WORLD -----` used to be under `----- GRIDS -----` and was moved to the root (GRIDS is at the origin with no rotation, so nothing moved).

---

## 15. Tests (EditMode)
**521 tests** in total, 42 files (`TestDefinitions` and `TestMaps` are helpers). Each runs plain C# services without a Unity scene.

| Area | Files | Cases Covered |
|---|---|---|
| Infrastructure | `EventBusTests`, `GameClockTests`, `SimulationRunnerTests`, `CompositeDisposableTests`, `MinHeapTests`, `GameEntityId*Tests` | Subscriptions changing during a publish, a failing handler, pause, game speed, phase order |
| Grid and paths | `GridMapTests`, `AStarPathfinderTests`, `GridSearchTests` | No corner cutting, unwalkable target, no path, map bounds, group destinations, not crossing water |
| Buildings | `BuildingServiceTests`, `PlacementServiceTests`, `TerritoryTests`, `CoverageRingTests`, `ConstructionTests` | Overlaps, going off the map, placing over units, outside the territory, preview of the area to open, ring drawing, construction and tool cursors |
| Units | `UnitServiceTests`, `UnitMovementTests`, `UnitCommandServiceTests`, `UnitProductionServiceTests`, `UnitPlacementTests`, `SelectionServiceTests`, `HoverServiceTests`, `ShortcutTests` | Spawning, repathing, spreading out, refusing friendly fire, placing units in the Sandbox, box selection, Stop, Hold Position, Attack Move, idle villagers, control groups |
| Combat | `CombatTests`, `BuildingDefenseTests` | Cooldowns, deaths handled once, hitting a building from its edge, tower target choice |
| Normal mode | `NormalModeTests`, `VillagerBehaviourTests`, `GameLoopTests`, `ResourceDistributorTests`, `FarmHungerRallyTests` | Spending, the gathering loop, carried loads, depletion, sheltering, calendar, raids, difficulty, game over, seeds, farm, hunger, rally points |
| Other | `SandboxServiceTests`, `TeamServiceTests`, `CameraMathTests`, `GameSettingsTests`, UI tests (`EscapeInputHandler`, `PanelSizeMath`, `RecyclingScrollMath`, `Tooltip`, `OffScreenArrowMath`) | Color assignment, camera bounds, saving settings and a broken save, Esc opening and closing the settings, infinite scroll math, the raid arrow's place on the screen edge |

Tests run from *Window → General → Test Runner → EditMode → Run All*. The README explains this too.

---

## 16. Build and Project Settings
- [x] **Player Settings → Resizable Window: on** (the brief requires it)
- [x] Fullscreen Mode: **Windowed**, default resolution 1600x900 (*Default Is Native Resolution* off). `GameSettings` also starts Windowed on first launch; otherwise it switched to fullscreen at start.
- [x] Company Name: `Akin Can Hancer` (ASCII; it is used in the save path)
- [x] Build Settings scene order: Bootstrap, MainMenu, Gameplay
- [x] Camera: when the aspect ratio changes, it is clamped to the map again every frame
- [x] Canvas Scaler: Scale With Screen Size, reference 1920x1080, Match 0.5
- [ ] Build output goes to the `Builds/` folder (not in git). For delivery it is zipped on a GitHub Release; the `_BurstDebugInformation_DoNotShip` folder stays out of the zip.

---

## 17. Git Rules
- Only the `main` branch. Every commit compiles and runs.
- Commit messages are in English, in Conventional Commits format: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`
- The `v1.0-pdf` tag is set when P0 is done.
- The `.csproj` and `.sln` files Unity generates, and IDE folders, stay out of git.
