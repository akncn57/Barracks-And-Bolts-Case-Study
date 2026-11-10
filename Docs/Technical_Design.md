# Barracks & Bolts – Teknik Tasarım Dokümanı

> Oyun tasarımı: [GDD_Decisions.md](GDD_Decisions.md) · Sistemlerin nasıl çalıştığı: [Systems.md](Systems.md) · Öncelikler: [Roadmap.md](Roadmap.md) · Assetler: [Missing_Assets.md](Missing_Assets.md)
> Unity 6000.3.16f1 (6.3 LTS) · URP 2D · Input System · VContainer · UniTask · DOTween · Cinemachine 3
>
> Bu doküman kuralları ve mimariyi anlatır. Sistem sistem "kod nasıl çalışıyor" için [Systems.md](Systems.md).

---

## 1. Temel Kararlar
| Konu | Karar |
|---|---|
| Mimari | Oyun mantığı **saf C# servislerinde**, MonoBehaviour'lar sadece **görünüm (View)** ve input'la ilgilenir. Bağlantılar VContainer ile kurulur. |
| İletişim | Sistemler birbirine **merkezi bir event bus** üzerinden tipli mesajlarla haber verir. |
| Güncelleme | Birimler ve sistemler **tek bir merkezi tick** üzerinden, belirli bir sırayla güncellenir. MonoBehaviour `Update` oyun mantığı için kullanılmaz. |
| Animasyon | **Animator yok.** Birimler, renk setindeki sprite karelerini oyun saatine göre kendisi çalar (bkz. Bölüm 13). |
| Sahneler | **Bootstrap → MainMenu → Gameplay** |
| Test | Kritik sistemler için **EditMode unit testleri** |
| Dil | Kod, yorumlar, commit mesajları ve README **İngilizce**. Tasarım dokümanları Türkçe. |
| Git | Sadece `main` branch'i. Her commit derlenebilir ve çalışır halde olur. |
| Kamera | **Cinemachine 3** gerçek kamerayı sürer. Input, kaydırma, zoom ve harita sınırları bizim `CameraController`'ımızda; kontrolcü takip hedefini ve lens boyutunu yönetir. |
| Async | Coroutine yerine **UniTask**. UI animasyonları ve geri bildirim efektleri için **DOTween**. |

---

## 2. Klasör Yapısı
```
Assets/
├── Animations/, Animators/     Şimdilik boş. Animator kullanılmıyor (bkz. Bölüm 13)
├── Atlases/                    Atlases_Gameplay, Atlases_UI, Atlases_Effects
├── Audio/
│   ├── Music/                  Music_<İsim>
│   └── SFX/                    SFX_<Kategori>_<İsim>
├── Fonts/
├── GameDatas/                  ScriptableObject verileri (UI/ altında popup verileri: Data_Popup_<İsim>)
│   ├── Buildings/              Data_Building_<İsim>.asset
│   ├── Units/                  Data_Unit_<İsim>.asset
│   ├── ResourceNodes/          Data_Resource_<İsim>.asset
│   ├── TeamColors/             Data_TeamColor_<Renk>.asset
│   └── Configs/                Data_Config_<İsim>.asset (harita, takvim, zorluk...)
├── Materials/                  Font/ altında TMP preset'leri (Outline, Shadow, Outline Shadow)
├── Editor/                     Tek seferlik editör araçları (iş bitince silinir)
├── Plugins/                    3. parti kütüphaneler (dokunulmaz)
├── Prefabs/
│   ├── Core/                   Prefab_Core_<İsim>LifetimeScope
│   ├── Gameplay/               Prefab_Gameplay_<İsim>
│   │   ├── Buildings/
│   │   ├── Units/
│   │   ├── ResourceNodes/
│   │   ├── Combat/
│   │   ├── Placement/
│   │   ├── Selection/
│   │   ├── Commands/
│   │   ├── Hover/
│   │   └── Territory/
│   └── UI/                     Prefab_UI_<Kategori>_<İsim>, Canvases/ (Main + MainMenu ve Gameplay varyantları), Popups/
├── Resources/                  Sadece kütüphanelerin zorunlu tuttuğu dosyalar (ör. DOTweenSettings). Bu isimdeki klasör build'e komple girer, başka yerde kullanılmaz
├── Scenes/                     Bootstrap, MainMenu, Gameplay
├── Scripts/                    (bkz. Bölüm 3)
├── Settings/                   URP, Renderer2D, Input Actions
├── Textures/                   Texture_<Klasör yolu>_<İsim>.png (renge göre: Red/, Yellow/)
│   ├── Backgrounds/            Menü arka planı; UI atlasına girmesin diye UI/ dışında
│   └── Markers/                Dünya üzerindeki işaretler (seçim halkası, yerleştirme hücresi, durum ve taşıma ikonları); renkliler Markers/<Renk>/
│   (Buildings/Neutral/ takım rengi olmayan yapılar, ör. tarla)
├── Tiles/
│   ├── Palettes/               Palette_<İsim>.prefab
│   └── <Kategori>/             Tile_<Kategori>_<İsim>.asset
└── TextMesh Pro/
```

---

## 3. Kod Modülleri ve Assembly Definition'lar
Her modül kendi `.asmdef` dosyasına sahiptir. Bu, derleme süresini kısaltır ve **bağımlılık yönünü zorunlu kılar** (UI, Gameplay'i bilir; Gameplay, UI'ı bilemez).

```
Scripts/
├── Core/        BarracksAndBolts.Core       Altyapı: EventBus, simülasyon döngüsü, oyun saati, sahne yükleme, oturum ayarları, oyuncu ayarları, root scope
├── Gameplay/    BarracksAndBolts.Gameplay   Oyun mantığı ve dünya görünümleri
├── UI/          BarracksAndBolts.UI         Ana menü, paneller, HUD, ayarlar, sonsuz scroll, tooltip, uçan yazı
├── Popup/       BarracksAndBolts.Popup      Hazır popup sistemi (ayarlar penceresi bunu kullanıyor)
├── Utils/       BarracksAndBolts.Utils      Unity'den bağımsız yardımcılar (MinHeap, CompositeDisposable)
└── Tests/
    └── EditMode/ BarracksAndBolts.Tests.EditMode   494 test
```
- Oyuncu ayarları `Core/Settings`'te (`GameSettings`, PlayerPrefs). Kayıt (save) henüz yok; geldiğinde Core'a girer.
- Tek seferlik editör araçları `Assets/Editor/` altında asmdef'siz durur ve iş bitince silinir.

**Bağımlılık yönü** (oklar "bağımlıdır" anlamındadır):
```
UI ──► Gameplay ──► Core ──► Utils
 └──► Popup (bizim modüllerimize bağlı değil)
Tests.EditMode ──► Gameplay, UI, Core, Utils
```

### Gameplay alt yapısı (namespace = klasör)
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
├── Combat/         CombatService, CombatSystem, BuildingDefenseSystem, DeathCleanupSystem, DamageFlash,
│                   ProjectileSpawner, ProjectileView, HealthBarView, CombatEvents
├── Effects/        EffectSpawner (ölüm ve yıkılma efekti, scope prefab'dan kurar)
├── Hover/          HoverService, HoverInputHandler, HoverHighlightView, ToolCursor, ToolCursorPicker, ToolCursorView
├── Feedback/       FloatingTextRequestedEvent (çizen: UI/Feedback/FloatingTextSpawner)
├── Selection/      SelectionService, SelectionInputHandler, BoxSelectionView, DeletionService, DeleteInputHandler
├── Commands/       UnitCommandService, CommandInputHandler, MoveMarkerView (AttackMove vb. gelince: IUnitCommand)
├── Sandbox/        SandboxService (taraf, Enemy AI ve Debug düğmeleri)
├── TimeControl/    TimeControlService, TimeControlInputHandler (iki modda)
├── DebugView/      DebugOverlayView (Sandbox: grid ve A* yolu)
├── Teams/          Faction, TeamService (TeamColor, menüden taşındığı için Core.Session içinde)
├── ResourceNodes/  ResourceNodeService, ResourceNodeModel, ResourceDistributor, ResourceMapInitializer,
│                   ResourceRoamSystem, ResourceNodeView, ResourceNodeViewSpawner
├── Economy/        ResourceService, PopulationService, NormalModeStarter, ResourceRespawnService, GameOutcomeService, HungerSystem
├── Gathering/      GatheringSystem, FarmingSystem
├── Construction/   ConstructionSystem (inşaat alanı; durum BuildingModel'de, BuildingCompletedEvent)
├── Calendar/       CalendarService (görünüm: UI/Hud/DayNightOverlayView, CalendarLabelView)
├── Raids/          RaidService, BattleAiSystem, ShelterSystem
├── Input/          GameInputActions (üretilen), InputModeController
└── CameraControl/  CameraController, CameraMath, CameraConfig
```
Planlanan klasörler: `Map/` (zemin üretimi), `Upgrades/`, `Storage/`, `Minimap/`, `Save/`. Ayrıntı: [Systems.md](Systems.md).

### UI alt yapısı
```
UI/
├── Bootstrap/      GameplayUiLifetimeScope
├── MainMenu/       MainMenuLifetimeScope, MainMenuView, MainMenuBackgroundView
├── Common/         SlideOutPanel, ContentFitPanel, PanelSizeMath, EscapeInputHandler, ICloseablePanel, IEscapeMenu,
│                   UiScaleApplier, ButtonFeedback, ButtonFeedbackInstaller
├── Settings/       SettingsPopup, SettingsButtonView, ISettingsContext (MainMenu / Gameplay)
├── Scroll/         RecyclingScrollView, RecyclingScrollCell, RecyclingScrollMath
├── Production/     ProductionMenuView, ProductionMenuCell
├── Information/    InformationPanelView, ProducibleUnitCell, UnitSelectionCardView
├── Hud/            ResourceBarView, CalendarLabelView, DayNightOverlayView, NoticeBannerView, GameEndView, TimeControlView, StatsPanelView
├── Feedback/       FloatingTextSpawner, FloatingTextView, HoverLabelView
├── Tooltips/       TooltipView, TooltipPlacement, TooltipText
└── Sandbox/        SandboxFactionToggleView, SandboxSwitchesView
```

---

## 4. Mimari Katmanlar
```
 ┌──────────────┐  okur    ┌──────────────────┐  publish   ┌───────────┐
 │  Definitions  │ ───────► │  Services (C#)   │ ─────────► │ EventBus  │
 │ (ScriptableO.)│          │  Model + Logic   │            └─────┬─────┘
 └──────────────┘          └────────▲─────────┘                  │ subscribe
                                    │ çağırır (intent)            ▼
                           ┌────────┴─────────┐         ┌──────────────────┐
                           │ Input / UI Views │         │  Views (Mono)    │
                           └──────────────────┘         │ Sprite, Animator │
                                                        └──────────────────┘
```
- **Definition (ScriptableObject):** Değişmeyen tasarım verisi. Ad, görsel, boyut, HP, hasar, maliyet. Runtime'da **asla değiştirilmez**.
- **Model (saf C#):** Runtime durumu. Örnek: `BuildingModel` (Id, Definition, Faction, Area, CurrentHp). Unity'ye bağımlı değildir, test edilebilir.
- **Service (saf C#):** Kuralları uygular ve modelleri yönetir. Örnek: `BuildingService.TryPlace(...)`, `CombatService.ApplyDamage(...)`. Sonuçları event olarak yayınlar.
- **View (MonoBehaviour):** Modeli ekranda gösterir (sprite, animasyon, can barı). Mantık içermez. Event'leri dinler ya da modelden okur.
- **Input / UI:** Oyuncunun niyetini servislere iletir (ör. "şu birimleri şuraya gönder"). Sonucu kendisi uygulamaz.

**Kural:** Bir servis hiçbir zaman bir View'a doğrudan erişmez. View'lar servise erişebilir.

### Kimlikler
Her bina ve birimin bir `GameEntityId` değeri vardır (artan `int`). Servisler birbirine nesne referansı yerine `GameEntityId` ile başvurur. Böylece:
- Yok olan bir hedefe referans asılı kalmaz. Hedef, `TryGet` ile her seferinde kontrol edilir.
- Kayıt sistemi kimlikleri doğrudan yazabilir.

---

## 5. VContainer Scope'ları
| Scope | Sahne | Ömür | Kayıtlar |
|---|---|---|---|
| `ProjectLifetimeScope` | Root prefab | Uygulama boyunca (DontDestroyOnLoad) | `SceneLoader`, `GameSessionSettings`, `GameSettings` + `PlayerPrefsSettingsStorage`, `DisplaySettingsApplier`. Planlanan: `SaveService`, `AudioService` |
| `BootstrapLifetimeScope` | Bootstrap | Sahne | `BootstrapFlow` (MainMenu'yü yükler) |
| `MainMenuLifetimeScope` | MainMenu | Sahne | `MainMenuView`, `UiScaleApplier`, `PopupSystem`, `SettingsButtonView`, `MainMenuSettingsContext` |
| `GameplayLifetimeScope` | Gameplay | Sahne | `EventBus`, `GameClock`, `SimulationRunner`, tüm gameplay servisleri ve sistemleri, dünya View'ları (`RegisterComponentInHierarchy`) |
| `GameplayUiLifetimeScope` | Gameplay (canvas) | Sahne, parent = Gameplay | Bütün UI view'ları, `PopupSystem`, `SettingsButtonView` (`IEscapeMenu`), `GameplaySettingsContext` ve `EscapeInputHandler`. UI tipleri Gameplay assembly'sinden görünmediği için ayrı scope |

- `EventBus` **Gameplay scope'una** kaydedilir. Sahne kapanınca tüm abonelikler de onunla birlikte yok olur, sahneler arası abonelik sızıntısı olmaz.
- `GameSessionSettings` yeni oyun ayarlarını (mod, renk, zorluk, seed) menüden Gameplay'e taşır.
- `ProjectLifetimeScope` bir prefab'dır ve `VContainerSettings` asset'inde **Root Lifetime Scope** olarak atanır. VContainer bu prefab'ı her sahne açılışında otomatik oluşturur. Böylece Gameplay sahnesi editörde doğrudan Play'e basılarak açıldığında da proje servisleri hazır olur ve varsayılan ayarlarla (Sandbox, Kırmızı, rastgele seed) çalışır.

---

## 6. Event Bus
```csharp
public interface IEventBus
{
    void Publish<TEvent>(in TEvent gameEvent) where TEvent : struct, IGameEvent;
    IDisposable Subscribe<TEvent>(Action<TEvent> handler) where TEvent : struct, IGameEvent;
}
```
- Event'ler `readonly struct` olur (GC üretmez). İsimlendirme: **`<Özne><GeçmişZamanFiil>Event`**, örnekler: `BuildingPlacedEvent`, `UnitDiedEvent`, `SelectionChangedEvent`.
- `Subscribe` bir `IDisposable` döndürür. Aboneler bunu `OnDestroy`/`Dispose` sırasında bırakır. Bunun için bir `CompositeDisposable` yardımcısı kullanılır.
- Bir event yayınlanırken abone listesi değişirse (ör. bir handler başka bir aboneliği kaldırırsa) sorun çıkmaması için, yayın sırasında abone listesinin bir kopyası üzerinde dolaşılır.
- **Event, komut değildir.** "Şunu yap" demek için servis metodu çağrılır. Event sadece "şu oldu" bilgisini taşır.

---

## 7. Simülasyon Döngüsü (Merkezi Tick)
- `SimulationRunner`, VContainer'ın `ITickable` arayüzünü uygular ve her karede sistemleri **sabit bir sırayla** çağırır (`SimulationPhase`):

| Faz | Değer | Sistemler |
|---|---|---|
| `Input` | 0 | Boş. Input handler'lar simülasyonun dışında, `IPostTickable` olarak çalışır |
| `Time` | 100 | `CalendarService` |
| `AI` | 200 | `RaidService`, `BattleAiSystem`, `ShelterSystem` |
| `Movement` | 300 | `UnitMovementSystem`, `ResourceRoamSystem` |
| `Gathering` | 350 | `GatheringSystem`, `ConstructionSystem` |
| `Combat` | 400 | `CombatSystem`, `BuildingDefenseSystem` |
| `Cleanup` | 500 | `DeathCleanupSystem` |
| `Presentation` | 600 | Boş. View'lar kendi `LateUpdate`'lerinde modeli okur |

- Sistemler `ISimulationSystem.Tick(float deltaTime)` arayüzünü uygular ve `builder.RegisterSimulationSystem<T>()` ile kaydedilir.
- **Kare sırası:** `SimulationRunner.Tick` (ITickable) → MonoBehaviour `Update` (kamera) → input handler'lar (`IPostTickable`, kameranın bu karedeki yerini görür) → View'ların `LateUpdate`'i. Oyuncunun bu karedeki emri simülasyona bir sonraki karede girer.
- `GameClock` oyun zamanını tutar: **duraklatma** (`IsPaused`) ve **oyun hızı** (`TimeScale`) yalnızca burada uygulanır. `Time.timeScale` kullanılmaz, böylece UI animasyonları duraklatmadan etkilenmez.
- Ölümler tick'in sonunda toplu işlenir. Böylece aynı karede hasar alan birimlerin işlem sırası sonucu değiştirmez ve bir koleksiyon üzerinde dolaşırken o koleksiyondan eleman silinmez.

---

## 8. Grid ve Pathfinding
- **1 hücre = 1 Unity birimi = 64px** (oyun içi sprite'larda PPU 64). Görsel taraf Unity `Grid` + `Tilemap`, mantık tarafı `GridMap` ile çalışır.
- `GridMap`, her hücre için şu bilgileri tutar: zemin yürünebilir mi, üzerinde hangi bina veya kaynak var (`GameEntityId`), seviyesi, rampa mı. Bölge bilgisi ayrı bir dizide, `TerritoryService`'te durur; grid'i değiştirmez.
- **Yükseklik:** Her hücrenin bir seviyesi var (0 düz zemin, 1 plato). Komşu iki hücre arasında sadece aynı seviyede ya da rampa üzerinden geçilir. Bina tek seviyede durmalı, rampaya konamaz.
- **Birimler hücre işgal etmez.** Sadece binalar ve kaynaklar engeldir. Bu sayede birimler birbirini kilitleyemez. Üst üste binmeyi `GridSearch.CollectDestinations` önler; hafif bir ayırma (separation) kuvveti henüz yok, birimler yürürken birbirinin içinden geçebilir. Üzerinde birim duran hücreye bina konamaz; yoksa birim binanın içinde kalır.
- **A\***:
  - 8 yönlü hareket. Köşe kesmeye izin verilmez (iki yanı dolu çaprazdan geçilmez).
  - Octile mesafe sezgiseli kullanılır.
  - Açık liste için kendi `MinHeap`'imiz kullanılır (.NET Standard 2.1'de `PriorityQueue` yok).
  - Arama bitince düğüm tamponları temizlenir ve sonraki aramada tekrar kullanılır, her aramada yeni bellek ayrılmaz.
  - Hedef yürünemezse en yakın yürünebilir hücreye gidilir. Hiç yol yoksa ulaşılabilen en yakın hücreye gidilir.
- **Grup hareketi:** `GridSearch.CollectDestinations`, hedefe en yakın boş hücreden başlayıp sadece yürünebilir hücreler üzerinden BFS ile yayılır ve her birime ayrı bir hücre verir.
- **Dinamik engel:** Bina konunca veya yıkılınca `GridChangedEvent` yayınlanır. Yolu bu değişiklikten etkilenen birimler yollarını yeniden hesaplar.

---

## 9. Veri Tanımları (ScriptableObject)
| Tip | Önemli Alanlar |
|---|---|
| `BuildingDefinition` | `Id`, `DisplayName`, `Size`, `VisualHeight`, `MaxHp`, `Cost`, `IsDropOff`, `PopulationCapacity`, `Producibles`, `NormalModeProducibles`, `SpawnOffset`; Defense: `Defender`, `DefenderPosition`, `DefenseRange`, `DefenseDamage`, `DefenseCooldown`; `TerritoryRadius`, `IsNormalModeOnly`. Sprite yok, renge bağlı olduğu için `TeamColorDefinition`'da |
| `UnitDefinition` | `Id`, `DisplayName`, `Portrait`, `MaxHp`, `Damage`, `AttackRange`, `AttackCooldown`, `MoveSpeed`, `PopulationCost`, `Cost`, `CanGather` |
| `ResourceNodeDefinition` | `Id`, `DisplayName`, `Kind` (Wood, Stone, Food, Gold), `Size`, `VisualHeight`, `Amount`, `WorkDistance`, `Frames`, `RoamRadius`, `WalkFrames`, `IsOneCut` |
| `TeamColorDefinition` | `Color`, bina → sprite ve gri inşaat sprite'ı, birim → portre ve kareler (bekleme, koşma, saldırı, balta, kazma, çekiç) |
| `GameDatabase` | `Buildings` (Production Menu sırası), `Units`, `TeamColors`, `ResourceDistribution`, `NormalMode`, `Raids` |
| `NormalModeSettings` | Castle, köylü, başlangıç köylüsü ve kaynağı, taşıma kapasitesi, çalışma süreleri, koyun sayıları |
| `RaidSettings` | Gündüz ve gece süresi, baskın günleri ve dalgaları, başlama aralığı, dalga arası, doğma mesafesi, `aggroRange`, `shelterRadius` |
| `ResourceDistributionSettings` | Boş alan yarıçapları ve kaynak grupları |
| `CameraConfig` | Kaydırma, zoom ve sınır değerleri |

- PDF değerleri bu asset'lerde tutulur: Barracks 100 HP, House 50 HP, askerler 10 HP ve 10 / 5 / 2 hasar.
- Zorluk (`DifficultySettings`) henüz yok; `Difficulty` enum'u oturum ayarlarında hazır bekliyor.
- Tüm tanımlar bir `GameDatabase` asset'inde toplanır ve VContainer'a bu şekilde kaydedilir.
- Her tanımın `OnValidate` metodu geçersiz değerleri editörde yakalar: boyut ≤ 0, HP ≤ 0, `Producibles` listesinde null eleman gibi.

---

## 10. Render ve SetPass (< 20)
- Tüm oyun içi sprite'lar **Sprite Atlas**'larda toplanır. Atlas'lar üst klasörleri referans alır, yeni eklenen alt klasörler otomatik olarak dahil olur.
- Tek bir sprite materyali kullanılır (URP `Sprite-Lit-Default`). Özel shader sayısı en az tutulur.
- **Can barları (planlandı):** World-space Canvas yerine SpriteRenderer (atlas'taki bar sprite'ları) kullanılacak, birimlerle aynı batch'e girer. Şu an HP panelde ve kartta.
- **Derinlik sıralaması:** Renderer2D'de Transparency Sort Mode = Custom Axis (0, 1, 0). Pivotlar tabanda olur. Bina ve birim köklerinde Sorting Group var.
- **Kodla üretilen dokular:** Bölge katmanı (`TerritoryOverlayView`) ve kapsama halkaları (`CoverageRingView`) çalışırken `Texture2D` olarak bir kez çizilir; atlas dışı oldukları için her biri bir çizim ekler, sadece yerleştirme ya da seçim sırasında görünürler.
- **Mini harita (planlandı):** İkinci bir kamera kullanılmaz. Grid verisinden üretilen tek bir `Texture2D` kullanılır.
- **UI:** Statik ve sık değişen elemanlar ayrı Canvas'lara bölünür (`StaticCanvas` / `DynamicCanvas`); FPS paneli de bu yüzden `DynamicCanvas`'ta. Popup'lar kendi `PopupCanvas`'ında (sıralama 100).
- **Ana menü arka planı:** Oyunun sprite'larından önceden birleştirilmiş tek bir PNG, tek Image ile çizilir. UI atlasına girmez; atlası büyütmesin diye `Textures/UI` dışında.
- **Yazı malzemeleri:** Kontur ve gölge TMP preset'leri aynı font atlasını kullanır; ek doku yok, sadece malzeme başına bir çizim.
- **Gece efekti:** Ekranı kaplayan tek bir yarı saydam UI görseli (`DayNightOverlayView`), alfası takvimden gelir. Işık sistemi kullanılmıyor.
- **Çizim sırası (Order in Layer):** su -5, köpük -4, zemin -3, plato ve merdiven -2, bölge katmanı ve halkalar -1, bina, birim ve kaynak 0, tarlanın içindeki köylü 1, yerleştirme önizlemesi 99-100, patlama efekti 145, ok ve debug yol noktaları 150, inşaat çubuğu 155, can barı 158, durum ikonu 160, hover çerçevesi 190, seçim kutusu 200.
- Ölçüm Frame Debugger ve Game view Stats ile yapılır: savaş sırasında **10 SetPass**. Son özelliklerden sonra tekrar ölçülecek, sonuç README'ye yazılacak.

---

## 11. Object Pooling ve Sonsuz Scroll
- Ayrı bir `PoolService` yok. Sık oluşan görseller Unity'nin `UnityEngine.Pool.ObjectPool<T>` sınıfını doğrudan kullanır: oklar (`ProjectileSpawner`) ve uçan yazılar (`FloatingTextSpawner`). Information Panel'deki asker hücreleri ve yerleştirme önizlemesinin kareleri elle havuzlanır: fazlası silinmez, kapatılır.
- Birim ve bina görünümleri havuzlanmıyor; ölüm ve yıkım seyrek olduğu için `Instantiate`/`Destroy` yeterli. Efekt turunda ölüm efektleri gelirse onlar havuzdan gelecek.
- **Production Menu (sonsuz scroll):** `RecyclingScrollView`
- **Kaynak barı:** `ResourceBarView`, ekranın üstünde tam genişlik. Üstteki paneller barın altından başlar.
- **Sol kenardan açılan panel:** `SlideOutPanel` (DOTween). Production Menu kapalı başlar; panel kapalıyken de kendi butonu ekranda kalır. Yerleştirme sürerken menü kapanır, bitince geri açılır (`ProductionMenuView`, yerleştirme event'leriyle).
  - ScrollRect kullanılmaz; sürükleme, fırlatma ve tekerlek bileşenin kendisindedir. Kaydırma tek bir offset değeridir.
  - Sadece görünen satır sayısı + 1 kadar satır oluşturulur (kaydırırken üstte ve altta aynı anda yarım satır görünebilir).
  - Kaydırma sırasında görünümden çıkan satır diğer uca taşınır. Veri indeksi negatif olmayan mod ile bulunur: `(row * columns + column) mod dataCount`.
  - Hücre sadece gösterdiği öğe değişince yeniden bağlanır; liste dururken RectTransform'lar değişmez (canvas rebuild yok).
  - Offset, öğelerin tekrar aynı yerlere geldiği döngü uzunluğuna sarılır; float hassasiyeti bozulmaz.
  - Liste görünür alandan kısaysa (veri sayısı < görünen hücre sayısı), boşluk kalmaması için öğeler tekrarlanarak doldurulur.

---

## 12. Input
- Varsayılan `InputSystem_Actions` şablonu kaldırıldı. Yerine `Settings/Input/GameInputActions.inputactions` geldi; C# sınıfı otomatik üretiliyor. Map'ler: **Camera** (her zaman açık), **Gameplay** ve **Placement** (ikisinden biri açık). Gameplay map'inde ayrıca `Delete`, `TogglePause`, `SpeedUp`, `SpeedDown` var; hız tuşları karakterle (`<Keyboard>/#(+)`) bağlı, her klavye düzeninde çalışır. Pause map'i oyun içi menüyle gelecek. Asset değişince üretilen sınıf da değişmeli; Unity asset'i import edince kendisi yeniden üretir.
- Tek bir büyük input servisi yok; her özelliğin kendi küçük handler'ı var:

| Sınıf | Görevi |
|---|---|
| `InputModeController` | Mod geçişi: `PlacementStartedEvent` gelince Gameplay'i kapatıp Placement'ı açar, `PlacementEndedEvent` ile tersini yapar. Aynı anda tek mod açık |
| `PlacementInputHandler` | Yerleştirme: footprint'i fareye taşır, onaylar, iptal eder |
| `SelectionInputHandler` | Sol tık ve kutu seçimi |
| `CommandInputHandler` | Sağ tık; bağlama göre karar `UnitCommandService.CommandAt`'te: düşman → saldırı, kaynak → toplama, zemin → hareket |
| `HoverInputHandler` | Farenin altındaki şey |
| `DeleteInputHandler` | Delete: seçimi siler (kural `DeletionService`'te) |
| `TimeControlInputHandler` | P duraklatır, + ve - hızı değiştirir |
| `EscapeInputHandler` (UI) | Esc her basışta en üstteki katmanı kapatır (yerleştirme, popup, seçim, menü); kapatacak bir şey yoksa ayarları açar |
| `CameraController` | Kaydırma, sürükleme, zoom |

- Her handler fare UI üzerindeyse (`EventSystem.IsPointerOverGameObject`) dünyaya tıklamayı yok sayar.

---

## 13. Animasyon ve Renk Sistemi
- **Karar: Animator yok.** İlk plan her birim için Animator Controller ve her renk için override idi. 4 birim x 2 renk için bunun yerine sprite dizileri seçildi: `TeamColorDefinition.UnitSpriteSet` birimin bekleme, koşma, saldırı, balta ve kazma karelerini tutar.
- `UnitView`, kareyi `GameClock.ElapsedTime`'dan hesaplar: oyun durunca birimler donar, hızlanınca hızlanır. Faz birim id'sinden gelir, grup aynı anda nefes almaz. Öncelik: saldırı > çalışma > koşma > bekleme.
- Kaynaklar (ağaç, koyun) ve kuledeki okçu da aynı şekilde kare çalar; ağaçların hepsini tek döngüde `ResourceNodeViewSpawner` günceller.
- Bir renkte eksik kare varsa uyarı yazılır ve boş set döner. Binalar animasyonsuzdur. Renk değişimi `TeamColorDefinition` üzerinden sprite değiştirilerek yapılır.
- Ölüm ve yıkılma animasyonu yok; efekt turunda `Textures/VFX` kareleriyle gelecek.
- Kod hiçbir yerde renkten taraf çıkarmaz: `Faction` (Player/Enemy) ile `TeamColor` (Red/Yellow) birbirinden bağımsızdır.

---

## 14. İsimlendirme Standartları

### C#
| Öğe | Kural | Örnek |
|---|---|---|
| Namespace | `BarracksAndBolts.<Modül>.<Alt>` (klasör yapısıyla aynı) | `BarracksAndBolts.Gameplay.Grid` |
| Sınıf, struct, enum, metot, property, event | PascalCase | `BuildingService`, `TryPlace()` |
| Interface | `I` öneki | `IEventBus` |
| Private alan | `_camelCase` | `_gridMap` |
| `[SerializeField]` private alan | `camelCase` | `popupContainer` |
| Sabit, static readonly | PascalCase | `MaxSelection` |
| Bool | `Is` / `Has` / `Can` öneki | `IsAlive`, `CanProduce` |
| Async metot | `Async` soneki, `UniTask` döner | `LoadSceneAsync()` |
| Event (C#) | Geçmiş zaman | `Destroyed`, `SelectionChanged` |
| Event (bus) | `<Özne><Fiil>Event` | `UnitDiedEvent` |

- Her dosyada tek tip olur ve dosya adı tip adıyla aynıdır.
- Public API'lerde XML doc (`/// <summary>`) yazılır. Yorumlar **neyi** değil, **neden** yapıldığını açıklar.
- Sihirli sayı kullanılmaz. Değerler ya Definition/Config'te ya da isimli bir sabitte durur.
- Yerel değişkenlerde her zaman `var` kullanılır (`foreach`, `for` ve `out var` dahil). Tek istisna `null` ile başlatılan değişkenlerdir; C# bunlarda tipin yazılmasını zorunlu kılar.
- `GetComponent` ve `Find` çağrıları runtime'da sık çalışan kodda yapılmaz. Referanslar Inspector'dan ya da Inject ile gelir.

### Asset
| Tür | Kalıp | Örnek |
|---|---|---|
| Texture | `Texture_<Klasör yolu>_<İsim>` | `Texture_Units_Red_Warrior_Idle` |
| Prefab | `Prefab_<Kategori>_<İsim>` | `Prefab_Gameplay_Building_Barracks`, `Prefab_UI_Canvas_Main` |
| ScriptableObject | `Data_<Tip>_<İsim>` | `Data_Building_Barracks` |
| Animation Clip | `Anim_<Renk>_<Birim>_<Aksiyon>` | `Anim_Red_Warrior_Run` |
| Animator | `Animator_<Birim>` / `AnimatorOverride_<Renk>_<Birim>` | `AnimatorOverride_Yellow_Lancer` |
| Material | `Material_<İsim>` | `Material_TerritoryOverlay` |
| Ses | `SFX_<Kategori>_<İsim>`, `Music_<İsim>` | `SFX_Combat_SwordHit` |
| Tile | `Tile_<Kategori>_<İsim>` | `Tile_Terrain_Ground_Color_01_00` |
| Tile Palette | `Palette_<İsim>` | `Palette_Terrain` |
| Sahne | PascalCase | `Gameplay` |

### Sahne Hiyerarşisi (Gameplay)
```
Prefab_Core_GameplayLifetimeScope
----- CAMERAS -----
    Main Camera                (CinemachineBrain)
    CameraTarget               (CameraController)
    CinemachineCamera_Gameplay
----- GRIDS -----
    Grid
        Tilemap_Water      (order -5)
        Tilemap_Foam       (order -4, animasyonlu köpük)
        Tilemap_Ground     (order -3, düz zemin; dolu hücre yürünebilir)
        Tilemap_Elevated   (order -2, plato ve uçurum; dolu hücre yüksek seviye)
        Tilemap_Stairs     (order -2, merdiven; dolu hücre rampa)
        Tilemap_Blocking   (çizilmez; dolu hücre yürünemez)
----- WORLD -----
    Prefab_Gameplay_Building_Spawner      (runtime'da üretilenlerin parent'ı, BuildingViewSpawner)
    Prefab_Gameplay_Placement_Preview     (yerleştirme önizlemesi; Normal modda yanına Prefab_Gameplay_Territory_Overlay'i kendisi oluşturur)
    Prefab_Gameplay_Unit_Spawner          (runtime'da üretilenlerin parent'ı, UnitViewSpawner)
    Prefab_Gameplay_ResourceNode_Spawner  (kaynakların parent'ı, ResourceNodeViewSpawner)
    Prefab_Gameplay_Selection_Box         (kutu seçimi çerçevesi, BoxSelectionView)
    Prefab_Gameplay_Move_Marker           (sağ tık geri bildirimi, MoveMarkerView)
    Prefab_Gameplay_Projectile_Spawner    (havuzdan gelen oklar, ProjectileSpawner)
    Prefab_Gameplay_Hover_Highlight       (mouse'un altındakinin çerçevesi, HoverHighlightView)
    Prefab_UI_FloatingText_Spawner        (havuzdan gelen uçan yazılar, FloatingTextSpawner)
----- MANAGERS -----
    (şu an boş; sadece MonoBehaviour gerektiren köprüler için)
----- UI -----
    EventSystem
    Prefab_UI_Canvas_Gameplay_Variant (GameplayUiLifetimeScope, parent = GameplayLifetimeScope)
        StaticCanvas / DynamicCanvas (Production Menu ve Information Panel burada; içerikleri oyun sırasında değişir)
```
- Sahnede ışık yok; sprite'lar ışıksız çiziliyor, gece efekti UI katmanı.
- Ayırıcı objeler (`----- X -----`) boş GameObject'tir. Transform'ları sıfırda durur, üzerlerinde component yoktur.
- Runtime'da üretilen nesneler her zaman ilgili parent'ın altına konur. Hiyerarşinin kök seviyesine nesne eklenmez.
- Scope'lar objeleri adla değil bileşen tipiyle bulur; obje adı değişikliği davranışı etkilemez.
- **Sahnede düzeltilecek:** `----- CAMERAS-----` ayırıcısında boşluk eksik ve `----- WORLD -----` şu an `----- GRIDS -----`'ın altında duruyor. Yukarıdaki düzen hedeflenen düzendir.

---

## 15. Testler (EditMode)
Toplam **494 test**, 39 dosya (`TestDefinitions` ve `TestMaps` yardımcı). Her biri saf C# servislerini Unity sahnesi olmadan çalıştırır.

| Alan | Dosyalar | Kapsadığı durumlar |
|---|---|---|
| Altyapı | `EventBusTests`, `GameClockTests`, `SimulationRunnerTests`, `CompositeDisposableTests`, `MinHeapTests`, `GameEntityId*Tests` | Yayın sırasında abonelik değişimi, hata veren handler, pause, oyun hızı, faz sırası |
| Grid ve yol | `GridMapTests`, `AStarPathfinderTests`, `GridSearchTests` | Köşe kesmeme, hedef yürünemez, yol yok, harita sınırı, grup hedefleri, suyu geçmeme |
| Bina | `BuildingServiceTests`, `PlacementServiceTests`, `TerritoryTests`, `CoverageRingTests`, `ConstructionTests` | Çakışma, taşma, birim altında yerleştirme, bölge dışı, açılacak alan önizlemesi, halka çizimi, inşaat ve araç imleci |
| Birim | `UnitServiceTests`, `UnitMovementTests`, `UnitCommandServiceTests`, `UnitProductionServiceTests`, `UnitPlacementTests`, `SelectionServiceTests`, `HoverServiceTests` | Spawn, yeniden yol, dağılma, dost ateşi reddi, Sandbox'ta birim yerleştirme, kutu seçimi |
| Savaş | `CombatTests`, `BuildingDefenseTests` | Bekleme süresi, ölümün tek kez işlenmesi, binaya kenardan vurma, kule hedef seçimi |
| Normal mod | `NormalModeTests`, `VillagerBehaviourTests`, `GameLoopTests`, `ResourceDistributorTests` | Harcama, toplama döngüsü, tükenme, sığınma, takvim, baskın, oyun sonu, seed |
| Diğer | `SandboxServiceTests`, `TeamServiceTests`, `CameraMathTests`, `GameSettingsTests`, UI testleri (`EscapeInputHandler`, `PanelSizeMath`, `RecyclingScrollMath`, `Tooltip`) | Renk ataması, kamera sınırı, ayarların kaydı ve bozuk kayıt, Esc'in ayarları açıp kapatması, sonsuz scroll matematiği |

Testler *Window → General → Test Runner → EditMode → Run All* ile çalışır. README'de anlatılacak.

---

## 16. Build ve Proje Ayarları
- [x] **Player Settings → Resizable Window: açık** (PDF zorunlu tutuyor)
- [x] Fullscreen Mode: **Windowed**, varsayılan çözünürlük 1600x900
- [ ] Company Name: `DefaultCompany` → gerçek bir isim
- [x] Build Settings sahne sırası: Bootstrap, MainMenu, Gameplay
- [x] Kamera: ekran oranı değişince harita sınırlarına göre her karede yeniden kısıtlanır
- [x] Canvas Scaler: Scale With Screen Size, referans 1920x1080, Match 0.5
- [ ] Build çıktısı `Builds/` klasörüne alınır (git'e girmez). Teslim için GitHub Release'e zip olarak eklenir; `_BurstDebugInformation_DoNotShip` klasörü zip'e girmez.

---

## 17. Git Kuralları
- Sadece `main` branch'i kullanılır. Her commit derlenebilir ve çalışır durumda olur.
- Commit mesajları İngilizce ve Conventional Commits formatındadır: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`
- P0 bittiğinde `v1.0-pdf` tag'i atılır.
- Unity'nin ürettiği `.csproj` ve `.sln` dosyaları ile IDE klasörleri git'e girmez.
