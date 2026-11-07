# Barracks & Bolts – Teknik Tasarım Dokümanı

> Oyun tasarımı: [GDD_Decisions.md](GDD_Decisions.md) · Öncelikler: [Roadmap.md](Roadmap.md) · Assetler: [Missing_Assets.md](Missing_Assets.md)
> Unity 6000.3.16f1 (6.3 LTS) · URP 2D · Input System · VContainer · UniTask · DOTween

---

## 1. Temel Kararlar
| Konu | Karar |
|---|---|
| Mimari | Oyun mantığı **saf C# servislerinde**, MonoBehaviour'lar sadece **görünüm (View)** ve input'la ilgilenir. Bağlantılar VContainer ile kurulur. |
| İletişim | Sistemler birbirine **merkezi bir event bus** üzerinden tipli mesajlarla haber verir. |
| Güncelleme | Birimler ve sistemler **tek bir merkezi tick** üzerinden, belirli bir sırayla güncellenir. MonoBehaviour `Update` oyun mantığı için kullanılmaz. |
| Animasyon | Her birim tipi için bir **Animator Controller**, her renk için bir **AnimatorOverrideController**. |
| Sahneler | **Bootstrap → MainMenu → Gameplay** |
| Test | Kritik sistemler için **EditMode unit testleri** |
| Dil | Kod, yorumlar, commit mesajları ve README **İngilizce**. Tasarım dokümanları Türkçe. |
| Git | Sadece `main` branch'i. Her commit derlenebilir ve çalışır halde olur. |
| Async | Coroutine yerine **UniTask**. UI animasyonları ve geri bildirim efektleri için **DOTween**. |

---

## 2. Klasör Yapısı
```
Assets/
├── Animations/                 Anim_<Renk>_<Birim>_<Aksiyon>.anim
│   └── <Birim>/
├── Animators/                  Animator_<Birim>.controller, AnimatorOverride_<Renk>_<Birim>.overrideController
├── Atlases/                    Atlases_Gameplay, Atlases_UI, Atlases_Effects
├── Audio/
│   ├── Music/                  Music_<İsim>
│   └── SFX/                    SFX_<Kategori>_<İsim>
├── Fonts/
├── GameDatas/                  ScriptableObject verileri
│   ├── Buildings/              Data_Building_<İsim>.asset
│   ├── Units/                  Data_Unit_<İsim>.asset
│   ├── Resources/              Data_Resource_<İsim>.asset
│   ├── TeamColors/             Data_TeamColor_<Renk>.asset
│   └── Configs/                Data_Config_<İsim>.asset (harita, takvim, zorluk...)
├── Materials/
├── Plugins/                    3. parti kütüphaneler (dokunulmaz)
├── Prefabs/
│   ├── Gameplay/               Prefab_Gameplay_<İsim>
│   │   ├── Buildings/
│   │   ├── Units/
│   │   └── Resources/
│   └── UI/                     Prefab_UI_<Kategori>_<İsim>
├── Resources/                  Sadece kütüphanelerin zorunlu tuttuğu dosyalar (ör. DOTweenSettings)
├── Scenes/                     Bootstrap, MainMenu, Gameplay
├── Scripts/                    (bkz. Bölüm 3)
├── Settings/                   URP, Renderer2D, Input Actions
├── Textures/                   Texture_<Klasör yolu>_<İsim>.png (renge göre: Red/, Yellow/)
└── TextMesh Pro/
```

---

## 3. Kod Modülleri ve Assembly Definition'lar
Her modül kendi `.asmdef` dosyasına sahiptir. Bu, derleme süresini kısaltır ve **bağımlılık yönünü zorunlu kılar** (UI, Gameplay'i bilir; Gameplay, UI'ı bilemez).

```
Scripts/
├── Core/        BarracksAndBolts.Core       Altyapı: EventBus, tick, pool, sahne yükleme, kayıt, ayarlar
├── Gameplay/    BarracksAndBolts.Gameplay   Oyun mantığı ve dünya görünümleri
├── UI/          BarracksAndBolts.UI         Ekranlar, paneller, HUD
├── Popup/       BarracksAndBolts.Popup      Popup sistemi (mevcut)
├── Utils/       BarracksAndBolts.Utils      Bağımsız yardımcılar (MinHeap, uzantı metotları)
├── Editor/      BarracksAndBolts.Editor     Editor araçları (sadece Editor platformu)
└── Tests/
    └── EditMode/ BarracksAndBolts.Tests.EditMode
```

**Bağımlılık yönü** (oklar "bağımlıdır" anlamındadır):
```
UI ──► Gameplay ──► Core ──► Utils
 └──► Popup ──────► Core
Tests.EditMode ──► Gameplay, Core, Utils
```

### Gameplay alt yapısı (namespace = klasör)
```
Gameplay/
├── Bootstrap/      GameplayLifetimeScope, GameplayEntryPoint
├── Data/           BuildingDefinition, UnitDefinition, TeamColorDefinition ... (ScriptableObject)
├── Grid/           GridMap, GridCell, GridCoordinates
├── Pathfinding/    AStarPathfinder, PathRequest, DestinationSpreader
├── Placement/      PlacementService, PlacementValidator, PlacementPreviewView
├── Buildings/      BuildingService, BuildingModel, BuildingView, Production/
├── Units/          UnitService, UnitModel, UnitView, States/
├── Combat/         CombatService, Damage, Targeting
├── Selection/      SelectionService, BoxSelectionView
├── Commands/       CommandService, IUnitCommand, MoveCommand, AttackCommand ...
├── Teams/          Faction, TeamService (TeamColor, menüden taşındığı için Core.Session içinde)
├── Economy/        ResourceService, PopulationService, StorageService     (P1)
├── Calendar/       CalendarService, DayNightView                          (P1)
├── Waves/          WaveService, EnemyAI                                    (P1)
├── Map/            MapGenerator, MapGenerationSettings                     (P2)
├── Input/          GameplayInputService, InputModeController
└── CameraControl/  CameraController
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
- **Model (saf C#):** Runtime durumu. Örnek: `BuildingModel` (Id, Definition, Team, Origin, CurrentHp). Unity'ye bağımlı değildir, test edilebilir.
- **Service (saf C#):** Kuralları uygular ve modelleri yönetir. Örnek: `BuildingService.TryPlace(...)`, `CombatService.ApplyDamage(...)`. Sonuçları event olarak yayınlar.
- **View (MonoBehaviour):** Modeli ekranda gösterir (sprite, animasyon, can barı). Mantık içermez. Event'leri dinler ya da modelden okur.
- **Input / UI:** Oyuncunun niyetini servislere iletir (ör. "şu birimleri şuraya gönder"). Sonucu kendisi uygulamaz.

**Kural:** Bir servis hiçbir zaman bir View'a doğrudan erişmez. View'lar servise erişebilir.

### Kimlikler
Her bina ve birimin bir `EntityId` değeri vardır (artan `int`). Servisler birbirine nesne referansı yerine `EntityId` ile başvurur. Böylece:
- Yok olan bir hedefe referans asılı kalmaz. Hedef, `TryGet` ile her seferinde kontrol edilir.
- Kayıt sistemi kimlikleri doğrudan yazabilir.

---

## 5. VContainer Scope'ları
| Scope | Sahne | Ömür | Kayıtlar |
|---|---|---|---|
| `ProjectLifetimeScope` | Bootstrap | Uygulama boyunca (DontDestroyOnLoad) | `SceneLoader`, `SettingsService`, `SaveService`, `AudioService`, `CursorService`, `GameSessionSettings` |
| `MainMenuLifetimeScope` | MainMenu | Sahne | Menü ekranları, `PopupSystem` |
| `GameplayLifetimeScope` | Gameplay | Sahne | `EventBus`, `SimulationRunner`, tüm gameplay servisleri, View'lar (`RegisterComponentInHierarchy`) |

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
- `SimulationRunner`, VContainer'ın `ITickable` arayüzünü uygular ve her karede sistemleri **sabit bir sırayla** çağırır:
  1. Input → komutlar
  2. Takvim / zaman
  3. AI (P1)
  4. Birim hareketi
  5. Savaş
  6. Ölümler ve temizlik
  7. View senkronizasyonu
- Sistemler `ISimulationSystem.Tick(float deltaTime)` arayüzünü uygular.
- `GameClock` oyun zamanını tutar: **duraklatma** (`IsPaused`) ve **oyun hızı** (`TimeScale`) yalnızca burada uygulanır. `Time.timeScale` kullanılmaz, böylece UI animasyonları duraklatmadan etkilenmez.
- Ölümler tick'in sonunda toplu işlenir. Böylece aynı karede hasar alan birimlerin işlem sırası sonucu değiştirmez ve bir koleksiyon üzerinde dolaşırken o koleksiyondan eleman silinmez.

---

## 8. Grid ve Pathfinding
- **1 hücre = 1 Unity birimi = 64px** (oyun içi sprite'larda PPU 64). Görsel taraf Unity `Grid` + `Tilemap`, mantık tarafı `GridMap` ile çalışır.
- `GridMap`, her hücre için şu bilgileri tutar: yürünebilir mi, üzerinde hangi bina veya kaynak var (`EntityId`), bölge içinde mi (P2).
- **Birimler hücre işgal etmez.** Sadece binalar ve kaynaklar engeldir. Bu sayede birimler birbirini kilitleyemez. Üst üste binmeyi `DestinationSpreader` ve hafif bir ayırma (separation) kuvveti önler.
- **A\***:
  - 8 yönlü hareket. Köşe kesmeye izin verilmez (iki yanı dolu çaprazdan geçilmez).
  - Octile mesafe sezgiseli kullanılır.
  - Açık liste için `MinHeap` kullanılır. .NET Standard 2.1'de `PriorityQueue` olmadığı için kendimiz yazacağız.
  - Arama bitince düğüm tamponları temizlenir ve sonraki aramada tekrar kullanılır, her aramada yeni bellek ayrılmaz.
  - Hedef yürünemezse en yakın yürünebilir hücreye gidilir. Hiç yol yoksa ulaşılabilen en yakın hücreye gidilir.
- **Grup hareketi:** `DestinationSpreader`, hedef hücreden BFS ile yayılarak her birime ayrı bir boş hücre atar.
- **Dinamik engel:** Bina konunca veya yıkılınca `GridChangedEvent` yayınlanır. Yolu bu değişiklikten etkilenen birimler yollarını yeniden hesaplar.

---

## 9. Veri Tanımları (ScriptableObject)
| Tip | Önemli Alanlar |
|---|---|
| `BuildingDefinition` | `Id`, `DisplayName`, `Icon`, `Size (Vector2Int)`, `MaxHp`, `Producibles (UnitDefinition[])`, `SpawnOffset`, `Cost` |
| `UnitDefinition` | `Id`, `DisplayName`, `Portrait`, `MaxHp`, `Damage`, `AttackRange`, `AttackCooldown`, `MoveSpeed`, `PopulationCost` |
| `TeamColorDefinition` | `Color`, bina sprite'ları ve her birim tipi için `AnimatorOverrideController` |
| `GameConfig` ve diğer config'ler | Takvim, harita, zorluk değerleri |

- PDF değerleri bu asset'lerde tutulur: Barracks 100 HP, House 50 HP, askerler 10 HP ve 10 / 5 / 2 hasar.
- Tüm tanımlar bir `GameDatabase` asset'inde toplanır ve VContainer'a bu şekilde kaydedilir.
- Her tanımın `OnValidate` metodu geçersiz değerleri editörde yakalar: boyut ≤ 0, HP ≤ 0, `Producibles` listesinde null eleman gibi.

---

## 10. Render ve SetPass (< 20)
- Tüm oyun içi sprite'lar **Sprite Atlas**'larda toplanır. Atlas'lar üst klasörleri referans alır, yeni eklenen alt klasörler otomatik olarak dahil olur.
- Tek bir sprite materyali kullanılır (URP `Sprite-Lit-Default`). Özel shader sayısı en az tutulur.
- **Can barları:** World-space Canvas yerine SpriteRenderer (atlas'taki bar sprite'ları) kullanılır, birimlerle aynı batch'e girer.
- **Derinlik sıralaması:** Renderer2D'de Transparency Sort Mode = Custom Axis (0, 1, 0). Pivotlar tabanda olur.
- **Mini harita:** İkinci bir kamera kullanılmaz. Grid verisinden üretilen tek bir `Texture2D` kullanılır.
- **UI:** Statik ve sık değişen elemanlar ayrı Canvas'lara bölünür (statik çerçeve / HUD sayaçları / popuplar).
- **Gece efekti:** Ekstra çizim maliyeti olmaması için tek bir Global Light 2D'nin rengi ve şiddeti değiştirilir.
- Ölçüm her özellikten sonra Frame Debugger ve Game view Stats ile yapılır. Sonuçlar README'ye yazılır.

---

## 11. Object Pooling ve Sonsuz Scroll
- `PoolService`, Unity'nin `UnityEngine.Pool.ObjectPool<T>` sınıfını prefab bazında sarar. Birimler, mermiler, efektler ve hasar sayıları pool'dan gelir. Pool'a geri dönen nesne `IPoolable.OnDespawn` ile durumunu sıfırlar.
- **Production Menu (sonsuz scroll):** `RecyclingScrollView`
  - Sadece görünen hücre sayısı + 2 kadar hücre oluşturulur.
  - Kaydırma sırasında görünümden çıkan hücre diğer uca taşınır ve veri indeksi `index % dataCount` ile döngüye girer.
  - Liste görünür alandan kısaysa (veri sayısı < görünen hücre sayısı), boşluk kalmaması için öğeler tekrarlanarak doldurulur.

---

## 12. Input
- Varsayılan `InputSystem_Actions` şablonu kaldırılır. Yerine GDD'deki action map'lerle (Camera, Gameplay, Placement, UI) yeni bir asset oluşturulur: `Settings/Input/GameInputActions.inputactions`. Bu asset'ten C# sınıfı otomatik üretilir.
- `GameplayInputService` şunlardan sorumludur:
  - Mod geçişleri (Gameplay ↔ Placement ↔ Pause). Aynı anda tek bir mod aktif olur ve bu tek bir yerden yönetilir.
  - Pointer UI üzerindeyse dünyaya tıklamayı engellemek.
  - Bağlama göre sağ tık: zemin → hareket, düşman → saldırı, kaynak → toplama.

---

## 13. Animasyon ve Renk Sistemi
- Her birim tipi için bir controller: `Animator_Warrior.controller` (Idle, Run, Attack, Death).
- Her renk için override: `AnimatorOverride_Red_Warrior`, `AnimatorOverride_Yellow_Warrior`. Bir renkte eksik olan animasyon, override'da varsayılan klibe bırakılır.
- Animator parametreleri tek bir sabit sınıfında tutulur: `AnimatorParams.IsMoving = Animator.StringToHash("IsMoving")`. Kodda string kullanılmaz.
- Binalar animasyonsuzdur. Renk değişimi `TeamColorDefinition` üzerinden sprite değiştirilerek yapılır.
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
| Sahne | PascalCase | `Gameplay` |

### Sahne Hiyerarşisi (Gameplay)
```
GameplayLifetimeScope
----- CAMERAS -----
    Main Camera
----- GRIDS -----
    Grid
        Tilemap_Ground
        Tilemap_Water
----- WORLD -----
    Buildings          (runtime'da üretilenlerin parent'ı)
    Units
    Resources
    Effects
----- MANAGERS -----
    (sadece MonoBehaviour gerektiren köprüler, ör. PoolRoot)
----- UI -----
    EventSystem
    Canvas_HUD / Canvas_Panels / Canvas_Popups
----- LIGHTS -----
    Global Light 2D
```
- Ayırıcı objeler (`----- X -----`) boş GameObject'tir. Transform'ları sıfırda durur, üzerlerinde component yoktur.
- Runtime'da üretilen nesneler her zaman ilgili parent'ın altına konur. Hiyerarşinin kök seviyesine nesne eklenmez.

---

## 15. Testler (EditMode)
| Sistem | Test Edilecek Durumlar |
|---|---|
| A\* | Düz yol, engel etrafından dolaşma, köşe kesmeme, hedef yürünemez, yol yok, başlangıç = hedef, harita sınırı |
| GridMap / Yerleştirme | Geçerli alan, çakışma, harita dışına taşma, yıkılan binanın hücreleri boşaltması |
| DestinationSpreader | N birim → N farklı hücre, kalabalık / engelli alan |
| Combat | Hasar, HP'nin 0'ın altına inmemesi, ölümün tek kez tetiklenmesi, ölü hedefe saldırmama, dost ateşi engeli |
| EventBus | Yayın, abonelikten çıkma, yayın sırasında abonelikten çıkma |
| Ekonomi (P1) | Yetersiz kaynak, depo kapasitesi, nüfus sınırı |

Testler Test Runner'dan çalıştırılır. README'de nasıl çalıştırılacağı anlatılır.

---

## 16. Build ve Proje Ayarları
- [ ] **Player Settings → Resizable Window: açık** (şu an `resizableWindow: 0`, PDF bunu zorunlu tutuyor)
- [ ] Fullscreen Mode: **Windowed**, varsayılan çözünürlük 1600x900
- [ ] Company Name: `DefaultCompany` → gerçek bir isim
- [ ] Build Settings sahne sırası: Bootstrap, MainMenu, Gameplay
- [ ] Kamera: ortografik boyut, ekran oranı değişince harita sınırlarına göre yeniden hesaplanır
- [ ] Canvas Scaler: Scale With Screen Size, referans 1920x1080, Match 0.5
- [ ] Build çıktısı `Builds/` klasörüne alınır (git'e girmez). Teslim için GitHub Release'e zip olarak eklenir.

---

## 17. Git Kuralları
- Sadece `main` branch'i kullanılır. Her commit derlenebilir ve çalışır durumda olur.
- Commit mesajları İngilizce ve Conventional Commits formatındadır: `feat:`, `fix:`, `refactor:`, `chore:`, `docs:`, `test:`
- P0 bittiğinde `v1.0-pdf` tag'i atılır.
- Unity'nin ürettiği `.csproj` ve `.sln` dosyaları ile IDE klasörleri git'e girmez.
