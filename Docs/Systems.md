# Barracks & Bolts – Sistem Rehberi

> Oyundaki her sistemin kodda **nasıl** yapıldığını anlatır: yapılanlar ve planlananlar.
> Kurallar ve mimari: [Technical_Design.md](Technical_Design.md) · Oyun tasarımı: [GDD_Decisions.md](GDD_Decisions.md) · Öncelikler: [Roadmap.md](Roadmap.md) · Assetler: [Missing_Assets.md](Missing_Assets.md)
>
> **Son güncelleme:** 10 Kasım 2026 · vuruş zamanlaması, efektler, can barları, tarla (köylü çitin içinde), açlık, toplanma noktası · 479 EditMode testi
>
> **Durum işaretleri:** ✅ yapıldı · 🟡 kısmen · ⏳ planlandı

## Bu doküman nasıl güncellenir?
- Yeni bir sistem eklenince ya da mevcut biri değişince ilgili bölüm aynı işte güncellenir.
- Planlanan bir sistem yapılınca "Planlanan Sistemler"den çıkarılıp kendi bölümüne taşınır, durumu ✅ olur.
- Her bölümde aynı sıra: **ne yapıyor**, **akış**, **önemli kurallar**, **dosyalar, veri ve testler**.
- Sayılar (HP, süre, maliyet) burada sadece örnek olarak geçer. Gerçek değerler tanım ve config asset'lerindedir.

---

## İçindekiler
**A. Temel Yapı**
1. [Katmanlar](#1-katmanlar)
2. [Açılış ve Scope'lar](#2-açılış-ve-scopelar)
3. [Bir Karede Ne Olur?](#3-bir-karede-ne-olur)
4. [EventBus ve Event Haritası](#4-eventbus-ve-event-haritası)
5. [Veri ve Oyun Modları](#5-veri-ve-oyun-modları)

**B. Dünya**
6. [Harita ve Grid](#6-harita-ve-grid)
7. [Kaynaklar](#7-kaynaklar)
8. [Kamera](#8-kamera)
9. [Yol Bulma](#9-yol-bulma)

**C. Binalar**
10. [Yerleştirme](#10-yerleştirme)
11. [Bina Modeli ve Görünümü](#11-bina-modeli-ve-görünümü)
12. [Bölge ve Karakol](#12-bölge-ve-karakol)
13. [Kule Savunması](#13-kule-savunması)
14. [Kapsama Halkaları](#14-kapsama-halkaları)

**D. Birimler**
15. [Birim Üretimi](#15-birim-üretimi)
16. [Seçim ve Hover](#16-seçim-ve-hover)
17. [Komutlar ve Hareket](#17-komutlar-ve-hareket)
18. [Savaş ve Yok Olma](#18-savaş-ve-yok-olma)
19. [Birim Görünümü ve Durum İkonları](#19-birim-görünümü-ve-durum-ikonları)

**E. Normal Mod**
20. [Kaynaklar ve Maliyet](#20-kaynaklar-ve-maliyet)
21. [Nüfus](#21-nüfus)
22. [Kaynak Toplama](#22-kaynak-toplama)
22b. [İnşaat](#22b-inşaat)
22c. [Tarla](#22c-tarla)
22d. [Açlık](#22d-açlık)
23. [Takvim ve Gece](#23-takvim-ve-gece)
24. [Baskınlar ve Savaş Yapay Zekası](#24-baskınlar-ve-savaş-yapay-zekası)
25. [Sığınma](#25-sığınma)
26. [Oyun Sonu](#26-oyun-sonu)

**F. Arayüz**
27. [Ana Menü](#27-ana-menü)
28. [Production Menu](#28-production-menu)
29. [Information Panel ve Birim Kartı](#29-information-panel-ve-birim-kartı)
30. [HUD](#30-hud)
31. [Geri Bildirim](#31-geri-bildirim)
32. [Esc Katmanları](#32-esc-katmanları)
33. [Sandbox](#33-sandbox)
33b. [Zaman Kontrolü ve Silme](#33b-zaman-kontrolü-ve-silme)

**G. [Planlanan Sistemler](#g-planlanan-sistemler)**

**H. [Tarifler: Yeni Bir Şey Eklemek](#h-tarifler-yeni-bir-şey-eklemek)**

**I. [Bilinen Sınırlar](#i-bilinen-sınırlar)**

---

# A. Temel Yapı

## 1. Katmanlar
**Durum:** ✅

```
Definition (ScriptableObject)  ──okur──►  Service / System (saf C#)  ──publish──►  EventBus  ──►  View (MonoBehaviour)
                                               ▲                                                  │
                                               └──────── Input handler / UI çağırır ◄──────────────┘ (View servisi okuyabilir)
```
- **Definition:** Değişmeyen tasarım verisi. Runtime'da hiç değişmez.
- **Model:** Runtime durumu (`BuildingModel`, `UnitModel`, `ResourceNodeModel`). Saf C#.
- **Service:** Kuralları uygular, modelleri tutar. Örnek: `BuildingService.TryPlace`.
- **System:** Her karede çalışan servis (`ISimulationSystem`). Örnek: `CombatSystem`.
- **View:** Modeli çizer. Kural içermez. Servis hiçbir zaman View'a seslenmez; View servisi okur ya da event dinler.
- **Input handler:** Oyuncunun niyetini servis çağrısına çevirir.
- Sistemler birbirine nesne değil `GameEntityId` ile başvurur. Ölen hedefin id'si `TryGet` ile boş döner; asılı referans kalmaz.
- Assembly yönü: `UI → Gameplay → Core → Utils`. Gameplay UI'ı göremez; bu kuralı derleyici korur.

## 2. Açılış ve Scope'lar
**Durum:** ✅ · **Dosyalar:** `Core/Bootstrap/*`, `Core/Scenes/*`, `Core/Session/*`, `Gameplay/Bootstrap/GameplayLifetimeScope.cs`, `UI/Bootstrap/GameplayUiLifetimeScope.cs`

```
Uygulama açılır ─► VContainerSettings root scope prefab'ını kurar (SceneLoader, GameSessionSettings)
Bootstrap sahnesi ─► BootstrapFlow.Start ─► MainMenu yüklenir
MainMenu ─► Play / Sandbox ─► GameSessionSettings'e mod ve yeni seed yazılır ─► Gameplay yüklenir
Gameplay ─► GameplayLifetimeScope (servisler, sistemler, dünya view'ları)
         └► GameplayUiLifetimeScope (UI view'ları), parent = GameplayLifetimeScope
```
- `GameSessionSettings` menüden oyuna mod, renk, zorluk ve seed taşır. Gameplay sahnesi editörde doğrudan açılırsa varsayılanlarla (Sandbox, kırmızı, rastgele seed) çalışır.
- `RegisterComponentInHierarchy<T>()` sahnede o bileşeni arar; bulamazsa scope kurulmaz ve bütün oyun durur. Yeni bir sahne view'ı eklemek = sahneye koymak + scope'a kaydetmek.
- `IStartable` scope kurulduktan sonra çalışır; `IInitializable` kurulum sırasında. Sahnedeki view'ların event'leri kaçırmaması için başlangıç işleri (`ResourceMapInitializer`, `NormalModeStarter`) `IStartable`.
- `EventBus` Gameplay scope'unda yaşar; sahne kapanınca bütün abonelikler de gider.

## 3. Bir Karede Ne Olur?
**Durum:** ✅ · **Dosyalar:** `Core/Simulation/*`

```
1. SimulationRunner.Tick        (ITickable)      GameClock.Advance → sistemler faz sırasıyla
       Time(100)        CalendarService
       AI(200)          RaidService, BattleAiSystem, ShelterSystem
       Movement(300)    UnitMovementSystem, ResourceRoamSystem
       Gathering(350)   GatheringSystem, ConstructionSystem
       Combat(400)      CombatSystem, BuildingDefenseSystem
       Cleanup(500)     DeathCleanupSystem
2. MonoBehaviour Update                          CameraController kamerayı taşır
3. IPostTickable                                 Placement, Selection, Command, Hover, Delete, TimeControl, Escape input handler'ları
4. LateUpdate                                    View'lar modeli okuyup çizer
```
- **`GameClock`** oyun zamanının tek sahibi: `IsPaused`, `TimeScale`, `ElapsedTime`. Kare süresi en fazla 0,1 sn sayılır. `Time.timeScale` hiç değişmez; UI ve DOTween oyun dururken de çalışır.
- **Input neden sonra?** Handler'lar kameranın bu karedeki yerini görmeli; yoksa fare bir kare geriden gelir. Emir simülasyona bir sonraki karede girer, bu fark edilmez.
- **Ölümler neden Cleanup'ta?** Aynı karede birbirine vuran iki birimin sırası sonucu değiştirmesin, sözlük dönerken eleman silinmesin.
- **Yeni sistem:** `ISimulationSystem` uygula, `Phase` seç, `builder.RegisterSimulationSystem<T>()` ile kaydet.

## 4. EventBus ve Event Haritası
**Durum:** ✅ · **Dosyalar:** `Core/Events/*`, `Utils/Disposables/*`

- Event `readonly struct`, adı geçmiş zaman (`UnitDiedEvent`). Event haberdir; "şunu yap" için servis metodu çağrılır.
- `Subscribe` bir `IDisposable` döner; abone `CompositeDisposable` ile toplayıp `OnDestroy`/`Dispose`'da bırakır.
- Yayın abone listesinin kopyası üzerinden yapılır; bir handler hata verirse diğerleri yine çalışır.

| Event | Yayınlayan | Dinleyen |
|---|---|---|
| `BuildingPlacedEvent` | `BuildingService` | `BuildingViewSpawner`, `TerritoryService`, `ResourceBarView` |
| `BuildingDestroyedEvent` | `BuildingService` | `BuildingViewSpawner`, `TerritoryService`, `SelectionService`, `GameOutcomeService`, `ResourceBarView` |
| `BuildingCompletedEvent` | `ConstructionSystem` | `TerritoryService`, `BuildingViewSpawner` (okçu çıkar), `InformationPanelView` |
| `BuildingFiredEvent` | `BuildingDefenseSystem` | `BuildingViewSpawner` (okçu karesi), `ProjectileSpawner` (ok) |
| `GridChangedEvent` | `BuildingService`, `ResourceNodeService` | `UnitMovementSystem` (yolu etkilenenler yeni yol bulur) |
| `UnitSpawnedEvent` | `UnitService` | `UnitViewSpawner`, `ResourceBarView` |
| `UnitDiedEvent` | `UnitService` | `UnitViewSpawner`, `SelectionService`, `GameOutcomeService`, `ResourceBarView` |
| `UnitShelteredEvent` / `UnitLeftShelterEvent` | `UnitService` | `UnitViewSpawner`, `SelectionService` (sadece girişte) |
| `UnitOrderedEvent` | `UnitCommandService` | `ShelterSystem` (oyuncu emri 10 sn kaçışı erteler) |
| `UnitAttackedEvent` | `CombatSystem` | `UnitViewSpawner` (saldırı karesi), `ProjectileSpawner` (ok, `ReleaseDelay` ve `FlightTime` ile) |
| `HungerChangedEvent` | `HungerSystem` | `ResourceBarView` (yemek kırmızı) |
| `EntityDamagedEvent` | `CombatService` | Bina ve birim spawner'ları (kırmızı flaş), `InformationPanelView`, `UnitSelectionCardView` |
| `MoveCommandIssuedEvent` / `AttackCommandIssuedEvent` | `UnitCommandService` | `MoveMarkerView` |
| `SelectionChangedEvent` | `SelectionService` | Bina ve birim spawner'ları, `InformationPanelView`, `UnitSelectionCardView` |
| `HoverChangedEvent` | `HoverService` | `HoverLabelView` |
| `PlacementStartedEvent` / `PlacementEndedEvent` | `PlacementService` | `InputModeController` |
| `ResourceNodePlacedEvent` / `ResourceNodeRemovedEvent` | `ResourceNodeService` | `ResourceNodeViewSpawner` |
| `ResourcesChangedEvent` | `ResourceService` | `ResourceBarView` |
| `DayStartedEvent` | `CalendarService` | `RaidService`, `ResourceRespawnService`, `CalendarLabelView` |
| `RaidEndedEvent` | `RaidService` | `GameOutcomeService`, `CalendarLabelView` |
| `GameNoticeEvent` | `RaidService`, `ShelterSystem`, `HungerSystem` | `NoticeBannerView` |
| `GameEndedEvent` | `GameOutcomeService` | `GameEndView` |
| `FloatingTextRequestedEvent` | `PlacementInputHandler`, `InformationPanelView`, `GatheringSystem` | `FloatingTextSpawner` |
| `SandboxFactionChangedEvent` | `SandboxService` | `ProductionMenuView`, `InformationPanelView` |
| `SandboxSwitchesChangedEvent` | `SandboxService` | `SandboxSwitchesView` (`DebugOverlayView` ve `BattleAiSystem` her karede okur) |
| `GameSpeedChangedEvent` | `TimeControlService` | `TimeControlView` |

## 5. Veri ve Oyun Modları
**Durum:** ✅ · **Dosyalar:** `Gameplay/Data/*` · **Veri:** `GameDatas/`

- Bütün tanımlar `Data_Database_Game`'de toplanır ve scope'a o kaydedilir. Alan boşsa scope hemen anlaşılır bir hata verir.
- Binanın sprite'ı tanımda değil, renk setindedir (`Data_TeamColor_Red`, `_Yellow`). Taraf (`Faction`: Player, Enemy) ve renk (`TeamColor`) ayrıdır. Oyuncu rengini seçer, düşman kalan ilk rengi alır (`TeamService`).
- Her tanımın `OnValidate`'i boyut ve HP gibi değerleri geçerli aralığa çeker, eksik alan için uyarı yazar.

**Sandbox ile Normal mod nerede ayrılıyor?** Sandbox PDF'in istediği oyundur ve ona dokunulmaz. Her yeni kural Normal moda özeldir; ayrım az ve belli yerlerde yapılır:

| Yer | Sandbox | Normal |
|---|---|---|
| `ResourceService.IsUnlimited` | Her fiyat geçer | Kaynak harcanır |
| `PopulationService` | Sınır yok | Kapasite kontrolü |
| `TerritoryService.IsEnabled` | Her hücre bölge içi | Bölge kuralı |
| `NormalModeStarter` | Hiçbir şey yapmaz | Castle, köylü, başlangıç kaynağı |
| `PlacementService.TryConfirm` | Bina anında hazır | `buildTime` > 0 ise inşaat alanı olarak konur |
| `ToolCursorView` | Normal imleç | Kaynakta ve inşaatta araç imleci |
| `BattleAiSystem` | Düğmeyle açılır (varsayılan kapalı) | Hep açık |
| `DeletionService` | Her şey silinir, fare altındaki kaynak da | Sadece oyuncunun birim ve binası, Castle hariç |
| Production Menu | Buildings, Units, Resources sekmeleri | Sadece binalar |
| `BuildingDefinition.GetProducibles` | `producibles` | `normalModeProducibles` (Castle köylü üretir) |
| `BuildingDefinition.IsNormalModeOnly` | Menüde yok (Karakol) | Menüde var |
| Takvim, baskın, AI, sığınma, oyun sonu | Çalışmaz | Çalışır |
| `SandboxService.ActiveFaction` | Side düğmesiyle değişir | Hep oyuncu |

---

# B. Dünya

## 6. Harita ve Grid
**Durum:** 🟡 (zemin sabit, kaynaklar rastgele) · **Dosyalar:** `Gameplay/Grid/*`, `Gameplay/Entities/*` · **Test:** `GridMapTests`

```
Tilemap'ler (görüntü) ─► TilemapGridSource.BuildGridMap() ─► GridMap (mantık)
```
- **1 hücre = 1 Unity birimi = 64 px.** Hücre (0, 0) sol alt köşedir.
- `GridMap` düz dizilerle hücre başına tutar: zemin yürünebilir mi, üzerinde kim var (bina ya da kaynak id'si), seviye (0 düz, 1 plato), rampa mı.
- `IsWalkable`: harita içinde, zemin yürünebilir ve boş. `CanStepBetween`: aynı seviye ya da rampa. `IsAreaFree`: bina için tek seviye, rampasız, boş alan.
- `TryOccupy` önce bütün hücreleri kontrol eder, sonra doldurur; başarısız yerleştirme haritayı yarım bırakmaz. `Release` sadece o id'ye ait hücreleri boşaltır.
- Hangi tilemap neyi söyler: `Tilemap_Ground` dolu = yürünebilir, `Tilemap_Elevated` = seviye 1, `Tilemap_Stairs` = rampa, `Tilemap_Blocking` (çizilmez) = engel.
- Grid sahne açılırken hemen kurulur; bozuk tilemap ilk saniyede hata verir.
- **Birimler grid'e yazılmaz.** Yazılsalardı birbirinin yolunu kapatırlardı. Birim konumu `UnitModel.Position`'da durur.

## 7. Kaynaklar
**Durum:** ✅ · **Dosyalar:** `Gameplay/ResourceNodes/*`, `Gameplay/Economy/ResourceRespawnService.cs` · **Veri:** `Data_Resource_*`, `Data_Config_ResourceDistribution` · **Test:** `ResourceDistributorTests`, `VillagerBehaviourTests`

```
ResourceMapInitializer (IStartable) ─► ResourceDistributor (seed) ─► ResourceNodeService.TryPlace ─► ResourceNodePlacedEvent ─► ResourceNodeViewSpawner
```
- **Dağılım (`ResourceDistributor`, saf C#):** `System.Random(seed)`; aynı seed aynı haritayı verir. Ortada başlangıç için boş alan, her türden bir küme başlangıca yakın, merdiven önleri açık. Her kaynaktan önce flood fill yapılır; haritayı ikiye bölen kaynak konmaz. Sıra azdan çoğa: altın, taş, koyun, orman, tek ağaç.
- **Model:** `ResourceNodeModel` kalan miktarı tutar. Biten kaynak `ResourceNodeService.Remove` ile kalkar, hücresi boşalır, `GridChangedEvent` gider.
- **Görünüm:** Bütün ağaç ve koyun karelerini `ResourceNodeViewSpawner` tek döngüde oynatır; 200 ağaca ayrı `Update` verilmez.
- **Koyun:** `ResourceRoamSystem` sadece görsel `Offset`'i değiştirir, grid hücresi aynı kalır. Tek kesimde ölür (`IsOneCut`).
- **Yeniden doğma:** `ResourceRespawnService` her sabah (`DayStartedEvent`, ilk sabah hariç) koyun ekler, üst sınırla. Yerini dağıtıcının kurallarıyla seçer.

## 8. Kamera
**Durum:** ✅ · **Dosyalar:** `Gameplay/CameraControl/*` · **Veri:** `Data_Config_Camera` · **Test:** `CameraMathTests`

- Cinemachine 3 gerçek kamerayı sürer; bizim `CameraController` `CameraTarget` objesini taşır ve lens boyutunu (zoom) yazar.
- WASD/ok tuşları ile kaydırma, orta tuşla sürükleme, tekerlekle fareye doğru zoom. Kenar kaydırma var ama ayarda kapalı.
- Sınırlar her karede `CameraMath.ClampCenter` ile uygulanır; görüş alanı ekran oranından hesaplanır. Pencere boyutu değişince kamera kendiliğinden sınıra döner.
- `Time.unscaledDeltaTime` kullanır; oyun dururken de etrafa bakılır.
- Fare→dünya dönüşümü `CameraController.ScreenToWorld` ile kendi merkezimizden yapılır; `Camera.ScreenToWorldPoint` bir önceki kareyi verebilir.

## 9. Yol Bulma
**Durum:** ✅ · **Dosyalar:** `Gameplay/Pathfinding/*`, `Utils/Collections/MinHeap.cs` · **Test:** `AStarPathfinderTests`, `GridSearchTests`, `MinHeapTests`

- **A\*:** 8 yön, köşe kesme yok, octile sezgisel, kendi `MinHeap`'imiz. Diziler bir kez ayrılır, her aramaya yeni bir damga (stamp) verilir; arama çöp üretmez.
- **Ulaşılamayan hedef:** Hedefe en yakın açılan hücreye yol verilir (`PathStatus.Partial`). Binaya saldırmak için yanına gitmek de böyle çalışır.
- **`GridSearch`:**
  - `TryFindNearestWalkable`: en yakın boş hücre (spawn noktası doluysa, binaya tıklanınca).
  - `CollectDestinations`: gruba ayrı hedef hücreleri; sadece yürünebilir hücreler üzerinden BFS, grup gölün iki yanına bölünmez.
  - `TryFindCellBeside`: bir alanın bitişiğinde, aynı seviyede en yakın boş hücre (köylü, saldıran asker).

---

# C. Binalar

## 10. Yerleştirme
**Durum:** ✅ · **Dosyalar:** `Gameplay/Placement/*`, `Gameplay/Input/InputModeController.cs` · **Test:** `PlacementServiceTests`, `TerritoryTests`, `UnitPlacementTests`

```
Production Menu tıklaması ─► PlacementService.Begin(bina, taraf) ─► PlacementStartedEvent ─► InputModeController: Placement map açılır
Her kare: PlacementInputHandler ─► MoveTo(fare) / HideArea (UI üstünde) / TryConfirm(Shift?) / Cancel
TryConfirm ─► BuildingService.TryPlace (Normal modda asSite) ─► fiyat alınır ─► BuildingPlacedEvent + GridChangedEvent
LateUpdate: PlacementPreviewView servisi okur, çizer
```
- `PlacementService` sadece durumu tutar: hangi bina, hangi taraf, footprint nerede. Input okumaz; testte `MoveTo(new Vector2(3, 3))` ile sürülür.
- **`Block`** alanın neden geçersiz olduğunu söyler: `NoArea`, `OutsideMap`, `Occupied`, `UnitInTheWay`, `OutsideTerritory`, `NotEnoughResources`. Uyarı metnini `PlacementInputHandler` seçer.
- **`IsCellFree(hücre)`** önizlemedeki her kareyi yeşil ya da kırmızı yapar: yürünebilir, üzerinde birim yok, bölge içinde.
- Footprint fareye ortalanır (`GridMap.GetAreaCenteredAt`); çift boyutta grid çizgisine oturur. `RoundToInt` yerine `Floor(x + 0.5)`, sınırda zıplamasın diye.
- Fiyat bina konduktan sonra alınır; konamazsa para gitmez. Shift basılıysa mod açık kalır.
- **Birim modu (Sandbox):** `BeginUnit(birim, taraf)` aynı modu birim için açar: footprint tek hücre, kural "hücre yürünebilir mi". Hücrede başka birim olması sorun değil, yeni birim en yakın boş hücreye çıkar (`UnitService.TrySpawn`). Bölge ve inşaat yok. Input haritaları, Esc, sağ tık iptali ve Shift ile arka arkaya koyma bina ile aynı. Önizlemede birimin ilk bekleme karesi ve tek hücrelik yeşil/kırmızı kare görünür. `Definition` ve `Unit`'ten aynı anda sadece biri dolu.
- Önizleme kareleri gizli bir şablondan kopyalanır; büyük bina için açılanlar silinmez, kapatılır.

## 11. Bina Modeli ve Görünümü
**Durum:** ✅ · **Dosyalar:** `Gameplay/Buildings/*` · **Test:** `BuildingServiceTests`

- `BuildingService` bütün binaların sahibi (`Dictionary<GameEntityId, BuildingModel>`). `TryPlace`, `Remove`, `TryGet`, `GetBuildingAt`.
- `BuildingModel`: `Id`, `Definition`, `Faction`, `Area`, `CurrentHp`, inşaat durumu (`BuildProgress`, `IsBuilt`), kule durumu (`DefenseTarget`, `ShotDelayLeft`).
- **Görünen alan:** Çatı hücrelerin üstüne taşar. `BuildingDefinition.VisualHeight` görünen yüksekliği verir; seçim, saldırı emri ve hover çatıya tıklamayı da kabul eder. Yol ve yerleştirme yine footprint'e bakar.
- **Görünüm:** Tek prefab (`Prefab_Gameplay_Building`) her binaya yeter; sprite renk setinden gelir. Kök footprint'in alt ortasında; `BuildingSpriteLayout.PlaceOnFootprint` sprite'ın alt ortasını köke oturtur.
- **Çizim sırası:** Kökteki Sorting Group binayı tabanının y'sine göre sıralar (Custom Axis 0, 1, 0). Binanın arkasındaki birim arkada kalır.
- Vurulan bina bir an kırmızıya döner (`DamageFlash`, birimlerle ortak). Flaş bitince bina kendi tonuna döner; inşaat alanında bu yarı saydam gridir.
- İnşaat alanı gri sprite'la çizilir (renk setinde `siteSprite`), bitince renkli sprite'a geçer (Bölüm 22b).

## 12. Bölge ve Karakol
**Durum:** ✅ · **Dosyalar:** `Gameplay/Territory/*` · **Veri:** `BuildingDefinition.TerritoryRadius` · **Test:** `TerritoryTests`

```
Oyuncunun bölge açan binası biter ya da yıkılır ─► TerritoryService.Rebuild ─► Version++
Yerleştirme: PlacementService.IsCellFree ─► TerritoryService.IsInside(hücre)
Görsel: TerritoryOverlayView, Version değişince ya da açılacak alan önizlemesi değişince yeniden çizer
```
- Tüm harita için tek `bool[]`. İnşaat alanı bölge açmaz; Karakol bitince açar. Yeniden hesap sadece kaynak bina bitince ya da yıkılınca yapılır; yerleştirmenin her karedeki sorgusu ucuzdur.
- Hücre, merkezi dairenin **içindeyse** bölgededir (`IsInCircle`, kesin `<`). Böylece dairenin yanlarında tek hücrelik çıkıntı olmaz.
- Binanın bütün hücreleri bölgede olmalı. Bölge küçülünce dışarıda kalan binalar yerinde kalır ve çalışır.
- Servis oluşurken mevcut binaları da tarar; Castle önce konmuş olsa da görür.
- **Açılacak alan:** `PlacementService.ShowsTerritoryPreview` bölge açan bina bölge içinde konacakken açık. `PlacementPreviewView` merkezi ve yarıçapı görsele verir; şekil, bina konunca açılacak alanla aynıdır.
- **Görsel:** Haritayı kaplayan tek doku, hücre başına 8 piksel: dışarısı kırmızı, sınır krem, açılacak alan yeşil. Sadece yerleştirme sırasında görünür. `PlacementPreviewView` bunu Normal modda kendi yanında oluşturur.
- **Karakol** kod değil veridir: `Data_Building_Outpost` (`territoryRadius`, `isNormalModeOnly`). Castle 12, Karakol 12, Kule 4.

## 13. Kule Savunması
**Durum:** ✅ · **Dosyalar:** `Gameplay/Combat/BuildingDefenseSystem.cs`, `Gameplay/Buildings/BuildingDefenderView.cs` · **Veri:** `BuildingDefinition` Defense başlığı · **Test:** `BuildingDefenseTests`

```
Combat fazı: BuildingDefenseSystem ─► HasDefense olan her bina
   bekleme bitti mi? ─► hedef hâlâ geçerli ve menzilde mi? ─► değilse CombatService.FindNearestHostileUnit
   ─► CombatService.ApplyDamage ─► BuildingFiredEvent ─► okçu karesi + ok
```
- İnşaat bitmeden kule ateş etmez ve üstünde okçu görünmez; okçu `BuildingCompletedEvent` ile çıkar.
- Okçu bir birim değil, binanın parçası: seçilmez, nüfus tutmaz, ölmez. Durum `BuildingModel`'de, sayılar tanımda. Yeni bir savunma binası sadece veriyle eklenir.
- Hedef ölene ya da menzilden çıkana kadar tutulur. Sürekli en yakına geçseydi hasar birimlere dağılırdı.
- Kimse yoksa saniyede 4 kez bakar (`ScanInterval`). Sadece birimleri vurur. Her modda ve iki tarafta da çalışır.
- Kuledeki okçunun zamanlaması `Defender` biriminin verisinden gelir: ok bırakış karesinde çıkar, hasar ok varınca işler (Bölüm 18).

## 14. Kapsama Halkaları
**Durum:** ✅ · **Dosyalar:** `Gameplay/Buildings/CoverageRingView.cs` · **Test:** `CoverageRingTests`

- Bina seçilince `BuildingViewSpawner` (`SelectionChangedEvent`) `BuildingView.SetSelected` çağırır: turuncu halka saldırı menzili, krem halka bölge. Kule yerleştirilirken de menzil görünür.
- Halka dokusu kodla bir kez çizilir; yarıçap değişmezse tekrar çizilmez, hiç görünmeyen binada hiç oluşmaz.
- Çizim sırası -1: zeminin üstünde, bina, birim ve ağaçların altında.
- Düşman binasında sadece menzil görünür; Sandbox'ta bölge halkası yok.

---

# D. Birimler

## 15. Birim Üretimi
**Durum:** ✅ · **Dosyalar:** `Gameplay/Units/UnitService.cs`, `Gameplay/Buildings/Production/UnitProductionService.cs` · **Test:** `UnitServiceTests`, `UnitProductionServiceTests`, `GameLoopTests`

```
Panelde asker butonu ─► InformationPanelView ─► UnitProductionService.TryProduce
    GetBlock: NotAvailable / UnderConstruction / NotEnoughResources / NoHousing  ─► uyarı yazısı
    spawn hücresi = Area.position + SpawnOffset ─► UnitService.TrySpawn (doluysa en yakın boş hücre)
    ─► UnitSpawnedEvent ─► UnitViewSpawner
```
- Süre yok, PDF öyle istiyor. Birim binanın tarafını alır.
- **Toplanma noktası (`RallyPointService`):** Sadece bir üretim binası seçiliyken sağ tık noktayı yazar (`CommandInputHandler`: birim emri geçmezse bina dener). Oyuncunun, bitmiş ve bu modda birim üreten binası olmalı. Üretilen her birim `UnitProductionService.UnitProduced` ile o noktaya, sağ tıkla aynı emirle gönderilir (`UnitCommandService.CommandAt`): zemin yürüme, düşman saldırı, kaynak toplama, inşaat yapma. Bina seçiliyken noktada takım renginde bayrak görünür (`BuildingView.rallyFlag`, kendi Sorting Group'u).

- "Bu hücrede birim var mı?" sorusu `UnitService.HasUnitAt` ile konumlardan cevaplanır (O(n), şu sayılar için yeterli).
- `UnitModel.Body`: ayakların çevresinde görünen karakter kadar bir dikdörtgen. Tıklama ve kutu seçimi buna bakar.

## 16. Seçim ve Hover
**Durum:** ✅ · **Dosyalar:** `Gameplay/Selection/*`, `Gameplay/Hover/*` · **Test:** `SelectionServiceTests`, `HoverServiceTests`

```
Sol tık bırakılınca ─► SelectionInputHandler
   8 px'ten az sürükleme ─► SelectionService.SelectAt   (önce öndeki birim, sonra bina, boşsa temizle)
   fazlası               ─► UpdateBox, bırakınca ConfirmBox (sadece oyuncunun birimleri)
─► SelectionChangedEvent ─► panel, kart, seçim halkaları, kapsama halkaları
```
- Seçim id listesi tutar; `Primary` listenin ilki. Ölen ya da yıkılan seçimden kendiliğinden çıkar.
- Aynı şey tekrar seçilince event gitmez; panel boşuna yenilenmez.
- **Hover** tıklamayla aynı sırayla seçer; tıklayınca seçilecek şey neyse çerçeve de onda. Her karede bakar, çünkü altından birim geçebilir. Yerleştirmede ve UI üstünde kapalı.
- Kutu, seçim halkası ve hover çerçevesi dünyada SpriteRenderer ile çizilir; Gameplay UI'a bağımlı olmaz.

## 17. Komutlar ve Hareket
**Durum:** ✅ · **Dosyalar:** `Gameplay/Commands/*`, `Gameplay/Units/UnitMovementSystem.cs`, `UnitModel.cs` · **Test:** `UnitCommandServiceTests`, `UnitMovementTests`

```
Sağ tık ─► CommandInputHandler ─► UnitCommandService.CommandAt(seçim, nokta)
   düşman birim ya da bina ─► Attack ─► UnitModel.SetAttackTarget          ─► AttackCommandIssuedEvent (kırmızı halka)
   kendi inşaat alanı      ─► Build  ─► köylüler ConstructionSystem.TryOrder, askerler yanına yürür
   kaynak                   ─► Gather ─► köylüler GatheringSystem.TryOrder, askerler yanına yürür
   zemin                    ─► MoveTo ─► her birime ayrı hücre ─► UnitMovementSystem.SendTo ─► MoveCommandIssuedEvent
   ─► UnitOrderedEvent
Movement fazı: UnitMovementSystem ─► UnitModel.MoveAlongPath(hız * dt)
```
- Sadece oyuncunun birimleri emir alır; dost ateşi `CombatService.GetHostileAt`'te engellenir. Kendi binana sağ tık hareket olur.
- **Dağılma:** `CollectDestinations` birim sayısı kadar hücre bulur; başka birimin durduğu ya da yürüdüğü hücre alınmaz. Hedefler yakından uzağa, her birine kalanlar içinde en yakın birim.
- **Yol değişince:** `GridChangedEvent` gelince sadece kalan yolu değişen alandan geçen birimler yeni yol bulur.
- Durağa varınca artan mesafe sonraki durağa devredilir; uzun karede birim durağı aşıp titremez.

## 18. Savaş ve Yok Olma
**Durum:** ✅ · **Dosyalar:** `Gameplay/Combat/*` · **Test:** `CombatTests`

```
Combat fazı: CombatSystem, hedefi olan her birim için
   hedef yok/öldü ─► bırak, dur
   menzilde       ─► dur, dön, bekleme bittiyse: UnitAttackedEvent (animasyon) + CombatService.ScheduleHit
                      vuruş karesinde (hitDelay) hasar; okçuda ok çıkar, hasar ok varınca (+ mesafe / projectileSpeed)
   değil          ─► hedefin yanına yürü (en fazla 0,25 sn'de bir yeni yol)
Cleanup fazı: DeathCleanupSystem ─► CombatService.RemoveDead ─► UnitService.Remove / BuildingService.Remove
              ─► UnitDiedEvent / BuildingDestroyedEvent + GridChangedEvent
```
- `CombatService.TryGetTarget` birim ve bina hedefini tek yerde toplar: mesafe, nişan noktası, yürünecek hücre. Binaya mesafe en yakın kenara ölçülür; Castle'a yandan vurulur.
- `ApplyDamage`: HP 0'ın altına inmez, ölüm tek kez kuyruğa girer. **Karşılık verme:** boşta duran birim ona vurana döner; işi olan devam eder.
- Bekleme süresi saldırı anında başlar, ilk vuruş beklemez; hedef değiştirerek hızlı vurulamaz.
- **Vuruş zamanlaması:** `UnitDefinition.hitDelay` saldırı animasyonunun başından vuruş karesine kadar geçen süre (Şövalye 0,2, Mızrakçı 0,1, Okçu 0,5, Köylü 0,2). `projectileSpeed` olan birimin hasarı ok uçup varınca işler (Okçu 12 hücre/sn). İkisi de 0 ise hasar hemen işler; eski davranış ve testler böyle kalır.
- `CombatService.ScheduleHit` zamanlanmış vuruşları tutar, `CombatSystem` her tick önce onları ilerletir. Bırakıştan önce saldıran ölürse vuruş iptal; havadaki ok yine de varır.
- Menzilli birim (`IsRanged`, menzil > 2) havuzdan ok atar. Ok oyun saatiyle uçar (`ProjectileView`), bırakışa kadar gizli bekler; duraklatma ve hız değişince hasarla eş zamanlı kalır.
- **Ölüm ve yıkılma efekti (`EffectSpawner`):** Birim ölünce (ya da silinince) küçük toz, bina yıkılınca boyuna göre patlama ve tabanında büyük toz. Kareler `Textures/VFX` (Effects atlas'ı), oyun saatiyle oynar, renderer'lar havuzdan. Sahnede obje yok: `GameplayLifetimeScope` `Prefab_Gameplay_Effect_Spawner`'ı `RegisterComponentInNewPrefab` ile kurar.
- **Can barı (`HealthBarView`):** İnce çerçeve ve tarafın renginde dolgu (oyuncu yeşil, düşman kırmızı), birimde başın üstünde, binada çatının üstünde. Sadece hasarlıyken ya da seçiliyken görünür; inşaat alanında onun yerine ilerleme çubuğu durur. Dolgu tam piksel, can kaldıkça en az bir piksel (`GetFillWidth`, testli). Sorting Group sırası 158.

- Yok olunca: grid boşalır, seçimden çıkar, saldıranlar hedefi bırakır, view silinir.

## 19. Birim Görünümü ve Durum İkonları
**Durum:** ✅ · **Dosyalar:** `Gameplay/Units/UnitView.cs`, `UnitViewSpawner.cs`

- **Animator yok.** Kareler renk setinden (`UnitSpriteSet`) gelir, oyun saatinden hesaplanır: `Floor(süre * fps + faz) % kareSayısı`. Oyun durunca donar. Faz birim id'sinden.
- Öncelik: saldırı (bir kez oynar) > inşaat (çekiç) > toplama (balta ya da kazma) > koşma > bekleme. Sprite sadece kare değişince atanır.
- Pivotlar ayakta; `flipX` karakteri kendi ekseninde çevirir.
- Seçim halkası ayakların altında, rengi tarafa göre.
- **Durum ikonları:** Tek `SpriteRenderer`, iki sprite. Boştaki köylüde krem "Zz" (`UnitModel.IsIdle`); düşmanı kendi fark eden ya da vurulup karşılık veren askerde 1,5 sn kırmızı "!" (`UnitModel.SpotEnemy`, `HasJustSpottedEnemy`). Oyuncunun emrinde çıkmaz. Kendi Sorting Group'u (`Sort At Root`, 160) ile ağaç ve binaların önünde.

---

# E. Normal Mod

## 20. Kaynaklar ve Maliyet
**Durum:** ✅ · **Dosyalar:** `Gameplay/Economy/ResourceService.cs`, `NormalModeStarter.cs`, `Gameplay/Data/ResourceCost.cs` · **Veri:** `Data_Config_NormalMode`, tanımlardaki `cost` · **Test:** `NormalModeTests`

- `ResourceService`: odun, taş, yemek, altın. `CanAfford`, `TrySpend`, `Add`; her değişimde `ResourcesChangedEvent`. Sandbox'ta `IsUnlimited`.
- `ResourceCost` bir kaynaktan bir miktar; bina, birim ve başlangıç kaynağı bununla yazılır.
- **Başlangıç (`NormalModeStarter`, `IStartable`):** Kaynaklar verilir, Castle haritanın ortasına konur, köylüler çıkar. Normal menüde Castle yok; tek Castle başlangıçta gelir.
- Maliyet tooltip'te `Cost:` satırında görünür (sadece Normal modda).

## 21. Nüfus
**Durum:** ✅ · **Dosyalar:** `Gameplay/Economy/PopulationService.cs` · **Test:** `GameLoopTests`

- `Used` = oyuncunun birimlerinin `PopulationCost` toplamı (sığınanlar dahil). `Capacity` = **bitmiş** binaların `PopulationCapacity` toplamı (Castle ve House 5).
- Sayaç tutulmaz, her sorguda hesaplanır; bina yıkılınca senkron kaçmaz.
- Barda `kullanılan/kapasite`. Yer yoksa üretim `NoHousing` ile durur.

## 22. Kaynak Toplama
**Durum:** ✅ · **Dosyalar:** `Gameplay/Gathering/GatheringSystem.cs` · **Test:** `NormalModeTests`, `VillagerBehaviourTests`

```
Köylüye kaynağa sağ tık ─► UnitCommandService.Gather ─► GatheringSystem.TryOrder
   WalkingToNode ─► Harvesting (harvestDuration) ─► WalkingToDropOff (en yakın IsDropOff bina) ─► ResourceService.Add ─► WalkingToNode ...
```
- **Nerede durur:** `TryPickWorkCell` kaynağın yanındaki hücrelere ceza ekler: yan 0, alt 3, üst 6, köşe 9, başka köylünün tuttuğu 20. Son adım hücre içinde kaynağa en yakın noktaya atılır (`workDistance`), balta kaynağa değsin diye.
- **Kaynak biterse:** 12 hücre içinde aynı türden yenisine geçer; yoksa elindekini bırakıp durur ve "Zz" çıkar.
- **Emir değişirse** (hareket, saldırı, karşılık verme) iş biter, yük kaybolur. **Kaçarken** iş `Suspend` ile saklanır, dönünce `Resume` ile devam eder.
- Koyun tek kesimde ölür, bütün yemeği tek seferde taşınır. Kesilirken `Hold()` onu durdurur.
- Çalışma kareleri: ağaç ve koyunda balta, taş ve altında kazma (`UnitModel.HarvestingKind`).

## 22b. İnşaat
**Durum:** ✅ (tamir yok) · **Dosyalar:** `Gameplay/Construction/ConstructionSystem.cs`, `Gameplay/Buildings/BuildingModel.cs`, `BuildingCompletedEvent.cs` · **Veri:** `BuildingDefinition.buildTime`, renk setlerinde `siteSprite` ve köylünün `buildFrames` · **Test:** `ConstructionTests`, `VillagerBehaviourTests`

```
Normal modda yerleştirme ─► BuildingService.TryPlace(asSite) ─► BuildingModel.StartAsSite: ilerleme 0, HP'nin %10'u
Köylü seçili, inşaata sağ tık ─► UnitCommandService.Build ─► ConstructionSystem.TryOrder
   Walking ─► binanın yanındaki hücre ─► son adım duvarın dibine ─► Working (çekiç kareleri)
   Working ─► her tick AddBuildProgress(dt / buildTime) ─► 4-7 sn sonra etraftaki başka bir hücreye geçer
   İlerleme 1 ─► BuildingCompletedEvent ─► köylü boşta kalır
```
- **Süre:** `buildTime` tek köylünün süresi (House 20, Barracks 35, Kule 30, Karakol 30 sn). Her köylü kendi payını ekler, yani iki köylü iki kat hızlı. Castle başlangıçta hazır gelir; `buildTime` 0 olan bina hep anında biter.
- **HP:** İnşaat %10 HP ile başlar, kalan HP işle birlikte dolar ve bitince tam olur (son kesir yuvarlanır, yoksa kayan nokta toplamı binayı 1 HP eksik bırakıp can barını gösteriyordu). Alınan hasar geri gelmez. İnşaat saldırıya açık; düşman normal bina gibi vurur.
- **Bitmeden çalışmayanlar:** Üretim (`ProductionBlock.UnderConstruction`), nüfus kapasitesi, kule atışı, bölge, bırakma noktası. Hepsi `BuildingModel.IsBuilt`'e bakar; kuralı her sistem kendisi uygular.
- **Nerede durur:** Yanlar ve alt tercih edilir (üstte çatı köylüyü kapatırdı). Başka köylünün tuttuğu hücre cezalı, iki köylü aynı yerde durmaz. Ayak duvardan 0,4 birim uzakta; çekiç binaya değer. Yer değiştirirken seçime küçük bir rastgelelik eklenir, köylü hep aynı iki hücre arasında gidip gelmez.
- **İş bölümü:** Toplama ve inşaat ayrı sistemler. Birini başlatan emir öbürünü iptal eder (`UnitCommandService.CancelJobs`). Sığınmada ikisi de askıya alınır, düşman gidince hangisi varsa ona dönülür.
- **Görünüm:** Gri sprite yarı saydam başlar, işlendikçe koyulaşır, bitince renkli sprite gelir (`BuildingView.UpdateLook`). Gri sprite'lar atlas'taki `Buildings` klasöründe, ayrı çizim maliyeti yok. Panel adın altında `Under construction 40%` yazar, HP'yi canlı gösterir.
- **İlerleme çubuğu (`BuildProgressBarView`):** İnşaatın çatısının 0,3 birim üstünde, paketin küçük bar çerçevesi (Sliced, binanın genişliğine göre 1-2 birim) ve altın dolgu. Dolgu tek piksel genişliğinde bir sütun, `localScale.x` ile uzuyor ve tam piksele yuvarlanıyor (`GetFillWidth`, testli). Dokular Gameplay atlas'ında (`Texture_Markers_ProgressBar_Frame/Fill`); UI atlas'ındaki orijinali dünyada kullanılsaydı ayrı çizim gerekirdi. Kendi Sorting Group'u var (`Sort At Root`, 155), ağaç ve birimlerin arkasında kalmıyor. Bina prefab'ında kapalı duruyor; sadece inşaatta açılıyor, bitince kapanıyor.
- **Sandbox:** İnşaat alanı yok, bina anında hazır (PDF'in "üretim süresi yok" maddesi).

## 22c. Tarla
**Durum:** ✅ · **Dosyalar:** `Gameplay/Gathering/FarmingSystem.cs` · **Veri:** `Data_Building_Farm` (`farmYield`, `farmHarvestDuration`, `farmWorkers`) · **Test:** `FarmHungerRallyTests`

```
Köylüyle kendi bitmiş tarlana sağ tık ─► UnitCommandService.Farm ─► FarmingSystem.TryOrder
   WalkingToFarm (yanındaki hücre, son adım çitin içine) ─► Working (balta kareleri, farmHarvestDuration)
   ─► WalkingToDropOff (en yakın bırakma noktası) ─► +farmYield yemek ─► WalkingToFarm ...
```
- **Kurallar:** 3x3, 40 odun, inşa 15 sn, 60 HP. Bir köylü çalışır, tarla hiç bitmez, her 6 saniyede 5 yemek (koyundan yavaş ama sürekli). Sadece Normal modda; menüde House'un yanında.
- Önce inşa edilir: inşaat alanına sağ tık köylüyü inşaata, bitmiş tarlaya sağ tık tarlaya gönderir. Dolu tarlaya gönderilen köylü yanına yürür ve "The farm has all the workers it can take" yazar.
- `GatheringSystem` kaynağın kendisine bağlı (miktar, tükenme, koyunun gezmesi); tarla bir bina olduğu için ayrı ve küçük bir sistem yazıldı. Yanındaki hücrenin seçimi `ConstructionSystem` ile aynı.
- **Çitin içinde çalışma:** Köylü yanındaki hücreden çitin üstünden tarlaya girer, bitişik kenar hücresinde rastgele bir noktada ekinlerin arasında durur (çitten yanlarda 0,45, altta 0,55 içeride). Her seferinde yeni bir nokta seçilir. Sadece kenar hücresi: çıkış tek adım, A* içeride sıkışmaz.
- Ayakları tarlanın alt kenarından yukarıda kaldığı için yükseklik sıralamasında ekinlerin arkasına düşerdi. Bu yüzden içerideyken `UnitModel.IsInField` açık ve `UnitView` kök Sorting Group'u 1 yapar; çitten çıkınca 0'a döner.
- Düşman çitin dışından vurur: `CombatService` içerideki köylüye mesafeyi en yakın çitten sayar ve kovalayanı o çitin dışındaki hücreye yollar. Şövalye (menzil 1) çitin dibinden yetişir.
- İş bölümü: toplama, inşaat ve tarla aynı anda olmaz; bir emir diğerlerini iptal eder. Sığınmada tarla işi de askıya alınır, taşınan yük Castle'da sayılır.
- İmleç kendi bitmiş tarlanda balta. Görsel renksiz (`Texture_Buildings_Neutral_Farm`), iki renk setinde aynı; gri inşaat kopyası `_Site`.

## 22d. Açlık
**Durum:** ✅ · **Dosyalar:** `Gameplay/Economy/HungerSystem.cs` · **Veri:** `Data_Config_NormalMode` > Hunger (Easy / Normal / Hard) · **Test:** `FarmHungerRallyTests`

- Her sabah (ilk sabah hariç) oyuncunun her birimi yemek yer: Kolay 1, Normal 2, Zor 4. Yetmeyen kısım borç olur; olan yemek yine yenir.
- Borç varken her birim belli aralıkla 1 HP kaybeder: Kolay 15, Normal 10, Zor 5 saniye. Gelen yemek önce borcu öder, ödenince kayıp durur.
- Başlarken "Not enough food! Your people are starving", bitince "There is food again" bildirimi; kaynak barında yemek sayısı kırmızı (`HungerChangedEvent`).
- Hasar saldırgansız verilir (`CombatService.ApplyDamage(..., GameEntityId.None)`); birim bu yüzden kimseye dönmez, "!" çıkmaz. Ölüm normal ölüm event'leriyle olur, yani son birim de ölürse mevcut kaybetme koşulu çalışır.
- Zorluk `GameSessionSettings.Difficulty`'den okunur. Ana menüde henüz zorluk seçimi yok, oyun Normal ile başlar. Sandbox'ta açlık yok.

## 23. Takvim ve Gece
**Durum:** ✅ · **Dosyalar:** `Gameplay/Calendar/CalendarService.cs`, `UI/Hud/DayNightOverlayView.cs`, `CalendarLabelView.cs` · **Veri:** `Data_Config_Raids`

- Gündüz 120, gece 60 saniye; Time fazında oyun saatiyle ilerler. Her gün başında `DayStartedEvent`.
- `Darkness` akşam ve şafakta 15 saniyede değişir; `DayNightOverlayView` onu ekranı kaplayan koyu tonun alfası yapar. Işık sistemi yok, ek çizim maliyeti tek bir görsel.
- Baskında `Freeze`, bitince `Resume`. Sandbox'ta çalışmaz.

## 24. Baskınlar ve Savaş Yapay Zekası
**Durum:** ✅ · **Dosyalar:** `Gameplay/Raids/RaidService.cs`, `BattleAiSystem.cs` · **Veri:** `Data_Config_Raids` · **Test:** `GameLoopTests`

```
Baskın günü sabahı: "Today is a raid day!" ─► günün %20-%80'i arasında başlar ─► takvim durur
Dalga doğar ─► hepsi ölür ─► 8 sn ─► sonraki dalga ... ─► "Raid beaten!" ─► RaidEndedEvent ─► takvim devam
```
- Baskın günleri 3, 6, 8 ve 10. Her baskın birkaç dalga; sonrakiler daha kalabalık ve daha çok şövalyeli. Can ve hasar çarpanı yok, PDF değerleri korunur.
- **Doğma noktası (`PickSpawnCell`):** Castle'dan yürünerek ulaşılan, kıyıda, en az 20 hücre uzak rastgele hücre. O anda hesaplanır, çünkü binalar haritayı değiştirmiş olabilir. Yön yazıyla söylenir ("from the north-east").
- Zamanlar seed'den; aynı seed aynı baskın zamanları.
- **`BattleAiSystem`** saniyede 4 kez hedef seçer; yürüme ve vurma `CombatSystem`'de:
  - Düşman: hedefi Castle. Yakınında (`aggroRange` 5) oyuncu birimi ya da binası varsa ona döner; 10 hücreden uzaklaşırsa bırakır. Binalar da hedef olduğu için yolu kapatan binayı yıkar; ayrı "engel yıkma" kodu yok.
  - Oyuncunun boştaki askeri yakındaki düşmana saldırır; hareket emri alanı rahatsız etmez.
- Sandbox'ta varsayılan kapalı; "Enemy AI" düğmesiyle açılır (Bölüm 33).

## 25. Sığınma
**Durum:** 🟡 (sadece Castle) · **Dosyalar:** `Gameplay/Raids/ShelterSystem.cs`, `UnitService.Shelter/BringOut` · **Test:** `VillagerBehaviourTests`

```
Düşman 7 hücre yakında ─► iş Suspend ─► Castle'ın yanına koşar ─► yük sayılır ─► içeri girer (UnitShelteredEvent)
Haritada düşman kalmadı ─► Castle'ın yanında belirir ─► işine döner (Resume)
```
- Toplama ya da inşaat işi `Suspend` ile saklanır, dönüşte `Resume` ile devam eder.
- İçerideki birim `_units`'ten `_sheltered`'a taşınır. Seçim, hover, savaş ve yerleştirme `_units`'e baktığı için başka kontrol gerekmez. Nüfus ikisini de sayar.
- Oyuncunun emri (`UnitOrderedEvent`) 10 saniye kaçıştan önce gelir.
- Yolu kapalıysa köylü işine döner, 10 saniye sonra yine dener.

## 26. Oyun Sonu
**Durum:** ✅ · **Dosyalar:** `Gameplay/Economy/GameOutcomeService.cs`, `UI/Hud/GameEndView.cs` · **Test:** `GameLoopTests`

- **Kaybetme:** Oyuncunun Castle'ı yıkılır, ya da son birim ölür ve yeni köylüye yetecek yemek yoktur.
- **Kazanma:** Son baskın yenilir (`RaidEndedEvent.IsLastRaid`).
- `GameClock.SetPaused(true)` ve `GameEndedEvent`. *Play Again* aynı modda yeni seed'le başlar, *Main Menu* menüye döner.

---

# F. Arayüz

## 27. Ana Menü
**Durum:** 🟡 (Play, Sandbox, Quit) · **Dosyalar:** `UI/MainMenu/*`

- Play Normal modu, Sandbox Sandbox'ı yeni bir seed'le başlatır. Basınca butonlar kilitlenir; `SceneLoader` da yükleme sürerken gelen ikinci isteği yok sayar.
- Quit editörde Play modunu kapatır, build'de uygulamayı.
- Planlanan: renk ve zorluk seçimi, seed girişi, kayıt yükleme, ayarlar (bkz. G).

## 28. Production Menu
**Durum:** ✅ · **Dosyalar:** `UI/Scroll/*`, `UI/Production/*`, `UI/Common/SlideOutPanel.cs` · **Test:** `RecyclingScrollMathTests`

- **Sonsuz scroll (`RecyclingScrollView`, generic):** Sadece görünen satır + 1 kadar hücre oluşur. Veri indeksi `PositiveModulo(satır * sütun + sütun, öğeSayısı)`; liste iki yöne de biter. Hücre sadece gösterdiği öğe değişince yeniden bağlanır, liste dururken hiçbir RectTransform değişmez.
- Offset döngü uzunluğuna sarılır; saatlerce kaydırılsa da float hassasiyeti bozulmaz. Sürükleme, fırlatma ve tekerlek bileşenin kendisinde, `ScrollRect` yok.
- `ProductionMenuView` binaları `GameDatabase` sırasıyla, aktif tarafın rengiyle gösterir; Normal modda Castle'ı, Sandbox'ta Karakolu gizler. Tıklama `PlacementService.Begin` çağırır.
- **Sekmeler (Sandbox):** Başlığın altında *Buildings*, *Units* ve *Resources*. Resources sekmesi ağaç, kaya, maden ve koyunları ilk kareleriyle listeler, tıklama `PlacementService.BeginNode`. Units sekmesi `GameDatabase.Units`'i portreleriyle listeler, tıklama `PlacementService.BeginUnit` çağırır. Seçili olmayan sekme koyulaşır. Normal modda sekmeler gizli ve liste onların yerine uzar (`tabsSpace`); orada birimler sadece binadan, parayla gelir. Hücre (`ProductionMenuCell`) ne gösterdiğini bilmez, sadece listedeki sırasını (`Index`) tutar; menü sırayı o anki sekmenin listesinde arar.
- `SlideOutPanel` menüyü soldan DOTween ile kaydırır; obje kapanmaz, sadece konumu değişir.

## 29. Information Panel ve Birim Kartı
**Durum:** ✅ · **Dosyalar:** `UI/Information/*`, `UI/Common/ContentFitPanel.cs`, `PanelSizeMath.cs` · **Test:** `PanelSizeMathTests`

- **Portreler renge göre:** `TeamColorDefinition.GetUnitPortrait(birim)` renk setindeki portreyi verir, yoksa birimin kendi `Portrait`'ına düşer. Paneldeki asker butonları binanın tarafının, birim kartı birimin tarafının, Units sekmesi aktif tarafın rengini gösterir; düşman askeri düşman renginde görünür. Dokular `Textures/UI/Avatars/<Renk>/Texture_UI_Avatars_<Renk>_<Birim>`.
- **Panel sadece bina gösterir:** resim (binanın kendi renginden), ad, HP yazısı ve çubuğu, üretim yapan ve aktif tarafa ait binada asker listesi. HP `EntityDamagedEvent` ile canlı.
- Boyutu içeriğe göre min ve max arasında; sığmazsa scroll. Ekrandan hiçbir zaman taşmaz.
- İnşaat alanında üretim listesi gizli, adın altında ilerleme yüzdesi var. Yüzde için event yok; panel gösterdiği inşaatın yüzdesini `LateUpdate`'te okur, sayı değişince yazar.
- Asker hücreleri elle havuzlanır. Köşedeki buton seçimi temizler; panel seçimi gösterdiği için kendiliğinden kapanır.
- **Birim kartı** alt ortada: portre, ad, HP; grupta `5 Units` ve toplam HP. Fareyi yakalamaz, sağ tık kartın içinden geçer.

## 30. HUD
**Durum:** ✅ · **Dosyalar:** `UI/Hud/*`

| View | Gösterdiği |
|---|---|
| `ResourceBarView` | Üstte odun, taş, yemek, altın ve nüfus. Sandbox'ta ∞ |
| `CalendarLabelView` | `Day 3 / 10` ve günün saati |
| `DayNightOverlayView` | Gece kararması |
| `NoticeBannerView` | `GameNoticeEvent` metinleri (baskın, sığınma) |
| `GameEndView` | Kazandın / kaybettin, iki buton |
| `TimeControlView` | Takvimin solunda duraklat ve 1x / 2x / 3x; seçili olan parlak (Bölüm 33b) |

## 31. Geri Bildirim
**Durum:** ✅ · **Dosyalar:** `UI/Feedback/*`, `UI/Tooltips/*`, `Gameplay/Feedback/*`, `Gameplay/Commands/MoveMarkerView.cs` · **Test:** `TooltipTests`

- **Uçan yazı:** Herhangi bir sistem `FloatingTextRequestedEvent` yayınlar, UI'daki `FloatingTextSpawner` havuzdan çizer. Gameplay TextMeshPro'yu bilmez. Tür (`Info`, `Warning`) verilir, renk Inspector'da.
- **Tooltip:** Tek `TooltipView`; metni `TooltipText`, yerini `TooltipPlacement` hesaplar (saf, testli). 0,25 sn gecikme, fareyi yakalamaz, ekrandan taşmaz.
- **Hover etiketi:** Hedefin üstünde adı (`Enemy Barracks`), her karede ekran pozisyonu güncellenir.
- **Araç imleci (`ToolCursorView`, `ToolCursorPicker`):** Normal modda fare kaynağın üstündeyse araç çıkar: ağaç ve koyunda balta, taş ve altında kazma; oyuncunun inşaat alanında çekiç. Karar saf bir fonksiyonda (testli), view sadece imleç değişince `Cursor.SetCursor` çağırır. Dokular `Textures/Cursors/Texture_Cursors_Axe/Pickaxe/Hammer`, 32x32, import tipi *Cursor*, tıklama noktası ortada. Bileşen `Prefab_Gameplay_Hover_Highlight` üzerinde. Sahne kapanırken normal imleç geri gelir.
- **Komut halkası:** `MoveMarkerView` tıklanan yerde küçülüp solar: hareket yeşil, saldırı kırmızı.

## 32. Esc Katmanları
**Durum:** ✅ · **Dosyalar:** `UI/Common/EscapeInputHandler.cs`, `ICloseablePanel.cs` · **Test:** `EscapeInputHandlerTests`

Her Esc en üstteki tek katmanı kapatır: yerleştirme (Placement map'inde, `PlacementInputHandler`) → seçim → Production Menu. Sıra tek yerde (`CloseTopLayer`) okunur. Oyun içi menü gelince en alt katman o olacak.

## 33. Sandbox
**Durum:** ✅ (baskını elle başlatma yok) · **Dosyalar:** `Gameplay/Sandbox/SandboxService.cs`, `UI/Sandbox/SandboxFactionToggleView.cs`, `SandboxSwitchesView.cs`, `Gameplay/DebugView/DebugOverlayView.cs` · **Test:** `SandboxServiceTests`, `SandboxToolsTests`

- `SandboxService.ActiveFaction`: yeni bina ve birimlerin tarafı. Side düğmesi değiştirir; menü o tarafın renginde gösterir. Düşman Barracks'ından düşman askeri üretmek için Side: Enemy olmalı.
- Kaynak sınırsız, nüfus ve bölge yok, takvim ve baskın kapalı, bina anında hazır.
- **Production Menu sekmeleri:** *Buildings*, *Units*, *Resources*. Birimler ve kaynaklar (ağaç, kaya, altın madeni, koyun) doğrudan haritaya konur (Bölüm 10 ve 28). Barracks'tan üretim (PDF yolu) aynen duruyor. Kaynak listesi `ResourceDistribution` gruplarından toplanır, her tanım bir kez.
- **Düğmeler** (`SandboxSwitchesView`, "Production >" butonunun altında, menü kapalıyken de görünür):
  - **Enemy AI:** `SandboxService.IsBattleAiOn`. Açıkken `BattleAiSystem` çalışır: düşman yakındaki oyuncu birim ve binasına, oyuncunun boştaki askeri yakındaki düşmana saldırır. Castle olmadığı için düşmanın yürüyeceği bir ana hedef yok. Varsayılan kapalı; test edenin emirleri olduğu gibi kalsın.
  - **Debug:** `SandboxService.IsDebugViewOn`. `DebugOverlayView` grid'i ve seçili birimlerin yolunu çizer (aşağıda).
- **Silme:** Delete tuşu seçili her şeyi siler (iki taraf). Hiçbir şey seçili değilse farenin altındaki kaynak silinir (Bölüm 33b).
- **Debug görünümü (`DebugOverlayView`):**
  - Haritanın tamamını kaplayan tek doku, hücre başına 8 piksel: hücre çizgileri, zemin yüzünden yürünemeyen hücre (su, uçurum) koyu, bina ya da kaynağın tuttuğu hücre kırmızı, merdiven mavi. `GridChangedEvent` gelince bir sonraki gösterimde yeniden çizilir.
  - Seçili ve yürüyen her birimin kalan yolu (`UnitModel.CopyRemainingPath`) 0,3 birim aralıklı noktalarla, hedefi büyük noktayla gösterilir. Oyuncu beyaz, düşman kırmızı. Noktalar havuzdan gelir, en fazla 800.
  - Prefab: `Prefab_Gameplay_Debug_Overlay`. Sahnede durmaz; `GameplayLifetimeScope` onu `RegisterComponentInNewPrefab` ile oluşturup inject eder. Kapalıyken hiçbir şey çizmez ve dokuyu hiç oluşturmaz.

## 33b. Zaman Kontrolü ve Silme
**Durum:** ✅ · **Dosyalar:** `Gameplay/TimeControl/*`, `UI/Hud/TimeControlView.cs`, `Gameplay/Selection/DeletionService.cs`, `DeleteInputHandler.cs` · **Test:** `SandboxToolsTests`

- **İki modda da var.** Kaynak barında takvimin solunda *II, 1x, 2x, 3x* butonları; tuşlar **P** (duraklat/devam), **+** ve **-** (hız; karakterle bağlı, her klavye düzeninde ve numpad'de çalışır).
- `TimeControlService` sadece `GameClock`'u değiştirir; takvim, baskın sayacı ve bütün sistemler onu izlediği için ek kod gerekmez. Hız seçmek duraklatmayı da bitirir. Oyun bitince (`GameEndedEvent`) saat bir daha başlatılamaz.
- **Silme (Delete tuşu):** `DeletionService.CanDelete` kuralı tek yerde tutar. Sandbox'ta her şey; Normal modda sadece oyuncunun birim ve binası, Castle asla (yanlışlıkla silinse oyun biterdi). Silmek ölümle aynı event'leri yayınlar (`UnitDiedEvent`, `BuildingDestroyedEvent`), yani seçim, panel, grid ve saldıranlar her zamanki gibi temizlenir. Geri ödeme yok. Silinemeyen bir şey seçiliyse "Can't delete this" yazar.
- Silinen kaynak önce boşaltılır, üstünde çalışan köylü tükenmiş kaynak gibi bir sonrakine geçer.

---

# G. Planlanan Sistemler
Her biri mevcut yapıya şöyle oturacak. Yapılınca kendi bölümüne taşınır.

### Depolar ve Kapasite ⏳
- **Nasıl:** `StorageService`, `PopulationService` gibi her sorguda kapasiteyi binalardan toplar (`BuildingDefinition.storage`: tür + miktar; Castle küçük başlangıç kapasitesi). `ResourceService.Add` kapasiteye kısar; doluysa `GatheringSystem` köylüyü bekletir ve "Storage full" yazar. Bırakma noktası "o türü kabul eden en yakın bina" olur. Yıkılınca fazla kaynak silinir.
- Görseller hazır: `Texture_Buildings_<Renk>_Storage_*`.

### Bina Yükseltme ⏳
- **Nasıl:** `BuildingModel.Level`. Seviye başına bonuslar ayrı bir veride (`UpgradeDefinition`: maliyet, HP, nüfus, kule hasarı ve okçu sayısı, bölge yarıçapı, depo kapasitesi). Tanım değişmez; değerleri okuyan yerler `BuildingStats` gibi tek bir yardımcıdan "tanım + seviye" okur. `UpgradeService.TryUpgrade` maliyeti alır, `TerritoryService.Rebuild` gerekirse çalışır. Barracks seviyesi üretilen askere `UnitModel.Level` olarak geçer.
- Panelde yükseltme butonu, birimde rütbe simgesi.

### Kilise ve Monk ⏳
- **Nasıl:** Yeni `UnitDefinition` (iyileştirme miktarı, menzil, bekleme). Combat fazında `HealSystem` menzildeki en yaralı dostu seçer, `CombatService.Heal`. Görsel `Monk` ve Heal VFX kareleri renk setine eklenir. Bina `Monastery` texture'larıyla, sadece veri.

### Tamir ve İnşaat İptali ⏳
- **Tamir:** `ConstructionSystem` aynı döngüyle hasarlı bitmiş binada çalışır: ilerleme yerine HP ekler, her puan için az kaynak harcar. Sağ tık kuralı aynı: kendi hasarlı binana köylüyle sağ tık, imleç yine çekiç.
- **İptal:** Panelde inşaat alanına iptal butonu; maliyetin yarısını geri verir, `BuildingService.Remove` çağırır.

### House'a Sığınma ve Kapasite ⏳
- **Nasıl:** `BuildingDefinition.shelterCapacity`. `ShelterSystem` doluluğu bina başına tutar ve en yakın boş binayı seçer. Bina yıkılınca içindekileri `BringOut` ile çıkarır.

### Mini Harita ⏳
- **Nasıl:** UI'da `MinimapView`: `GridMap` ve kaynaklardan tek `Texture2D` (zemin bir kez, binalar ve kaynaklar event'le), birimler 0,25 sn'de bir nokta. Kameranın gördüğü alan çerçeve. Tıklama `CameraController`'a odak verir, sağ tık `UnitCommandService.MoveTo`. İkinci kamera kullanılmaz (SetPass).

### Tam Sandbox 🟡
- **Yapıldı:** Bina, birim ve kaynak yerleştirme, iki taraf, silme, zaman kontrolü, yapay zekâ ve debug düğmeleri (Bölüm 33, 33b).
- **Kalan:** Baskını elle başlatma (`RaidService`'e bir metot ve Sandbox'ta takvimi açma).

### Rastgele Zemin Üretimi ⏳
- **Nasıl:** `Map/MapGenerator` seed'le tilemap'leri doldurur, `TilemapGridSource` ondan sonra grid'i kurar; geri kalan her şey değişmeden çalışır. Ada şekli, platolar ve merdivenler noise ile; yol doğrulaması `ResourceDistributor`'daki flood fill ile aynı yöntem. Ayarlar `MapGenerationSettings`'te.

### Kayıt ⏳
- **Nasıl:** Core'da `SaveService`, JSON, `Application.persistentDataPath`, birden fazla slot. Seed + değişiklikler: binalar, birimler (emirleriyle), kaynak miktarları, gün, kaynaklar. Tanımlar `Id` alanıyla yazılır. Yükleme: harita seed'den kurulur, üstüne kayıt uygulanır.

### Zorluk ve Sonsuz Mod ⏳
- **Zorluk:** Ana menüde Kolay / Normal / Zor seçimi, `GameSessionSettings.Difficulty`'ye yazar. Açlık bunu zaten okuyor (Bölüm 22d); dalga çarpanı ve başlangıç kaynağı `RaidService` ve `NormalModeStarter`'a eklenecek.
- **Sonsuz mod:** Son baskından sonra `RaidService` formülle yeni baskın üretir. `GameEndView`'e *Continue*; hayatta kalınan gün HUD'da.

### Oyun İçi Menü ve Ayarlar ⏳
- **Nasıl:** Pause action map'i, `GameClock.SetPaused`. Esc'in en alt katmanı menüyü açar. `SettingsService` (Core) ses, çözünürlük, pencere modu, VSync, kenar kaydırma; `PlayerPrefs`'e yazar.

### Ses ⏳
- **Nasıl:** Root scope'ta `AudioService` (müzik ve havuzlu `AudioSource`'lar). Gameplay'de bir köprü event'leri seslere çevirir (`UnitAttackedEvent` → kılıç, `BuildingDestroyedEvent` → yıkım, baskın başlangıcı → borozan). Oyun kodu ses bilmez.

### Komut Kısayolları ⏳
- Kontrol grupları (Ctrl+1-9), AttackMove (A), Stop (S), HoldPosition (X), boştaki köylü (.). (Oyun hızı, duraklatma ve Delete yapıldı, Bölüm 33b.) Hepsi Gameplay action map'ine eylem ve `UnitCommandService`'e metot. Duruş (`HoldPosition`) `UnitModel`'de bir alan; `BattleAiSystem` ona bakar.

### Diğerleri ⏳
- Yürürken ayırma (separation) kuvveti, tutorial, düşman yerleşkeleri, Production Menu hücresinde maliyet.

---

# H. Tarifler: Yeni Bir Şey Eklemek

### Yeni bina (kodsuz)
1. `GameDatas/Buildings/Data_Building_<İsim>` oluştur: boyut, `VisualHeight`, HP, maliyet, `buildTime`; gerekiyorsa üretim, savunma, bölge.
2. Her `Data_TeamColor_*` asset'ine binanın sprite'ını ve gri `siteSprite`'ını ekle (gri yoksa inşaat renkli sprite'la görünür).
3. `Data_Database_Game > Buildings` listesine ekle. Menüde bu sırada görünür.

### Yeni birim (kodsuz)
1. `Data_Unit_<İsim>`: HP, hasar, menzil, hız, nüfus, maliyet, portre.
2. Renk setlerinde birimin karelerini doldur (bekleme, koşma, saldırı; köylüyse balta ve kazma).
3. Üretecek binanın `producibles` (ya da `normalModeProducibles`) listesine ekle.

### Yeni simülasyon sistemi
1. `ISimulationSystem` uygula, doğru `Phase`'i seç.
2. `GameplayLifetimeScope`'ta `builder.RegisterSimulationSystem<T>()`.
3. Saf C# tut; testte `SimulationRunner.Step` ya da doğrudan `Tick` ile sür.

### Yeni event
1. `readonly struct XxxHappenedEvent : IGameEvent`, sadece okunur alanlar.
2. Yayınlayan `_eventBus.Publish(new ...)`. Dinleyen `Subscribe<...>(...).AddTo(_subscriptions)` ve yok olurken `Dispose`.
3. Bu dokümandaki event tablosuna ekle.

### Yeni dünya view'ı (sahnede)
1. MonoBehaviour, `[Inject] Construct(...)` ile servisleri alır, `LateUpdate`'te okur ya da event dinler.
2. Prefab yap (`Prefab_Gameplay_<Kategori>_<İsim>`), sahneye `----- WORLD -----` altına koy.
3. `GameplayLifetimeScope`'a `RegisterComponentInHierarchy<T>()`. Unutulursa `[Inject]` çalışmaz; sahnede yoksa scope kurulmaz.

### Yeni UI view
Aynısı, ama prefab `Prefab_UI_<Kategori>_<İsim>`, yeri Gameplay canvas'ı, kaydı `GameplayUiLifetimeScope`. Sık değişiyorsa `DynamicCanvas`'a koy.

### Normal moda özel kural
Kuralı servisin içinde `IsEnabled` / `IsUnlimited` gibi tek bir bayrakla kapat; çağıranlar modu bilmesin. Sandbox davranışı değişmemeli ve bunun testi olmalı.

---

# I. Bilinen Sınırlar
- Birimler yürürken birbirinin içinden geçer; sadece hedeflerde ayrılırlar.
- Yakın dövüşçü plato kenarının altından üstteki hedefe vurabilir; menzil seviyeye bakmaz.
- Ana menüde zorluk seçimi yok; açlık değerleri zorluğa göre hazır ama oyun Normal ile başlar.
- Hasar sayıları (ör. "-4") yok; vuruş kırmızı flaş ve can barıyla görünür.
- Bölge halkası gerçek daire, bölge katmanı hücre hücre; aralarında en fazla bir hücrelik fark olabilir.
- Bölge katmanı footprint her hücre değiştirdiğinde baştan çizilir; büyük haritada takılma olursa sadece değişen bölge çizilecek.
- En uzak zoom'da haritanın en üst sırası kaynak barının altında kalabilir.
- Kuledeki okçu sayısı ve Karakol yarıçapı yükseltme sistemi gelene kadar sabit.
- İnşaat iptal edilemez; tamir yok.
- Araç imleci 32x32 donanım imleci; çok yüksek DPI'da küçük görünür.
