# Barracks & Bolts

[English](README.md) | **Türkçe**

Unity 6 ile geliştirilmiş, ortaçağda geçen 2D bir gerçek zamanlı strateji oyunu. Üssünü kur, köylülerinle ekonomini çevir ve on gün boyunca gelen baskınlara dayan. İstersen her şeyi serbestçe kurabileceğin Sandbox modunda da oynayabilirsin.

Proje, bir oyun geliştirici pozisyonu için verilen bir haftalık "strateji oyunu demosu" case study'si olarak başladı. Brief'te istenen her madde yapıldı (bkz. [Brief'teki Maddeler](#briefteki-maddeler)). Kalan zamanda demo, temiz ve testlerle korunan bir mimari üzerinde küçük ama eksiksiz bir oyuna dönüştü.

<!-- Ekran görüntüleri / GIF: Docs/Images klasörüne ekleyip buradan bağlantı verin. -->

**Unity 6.3 LTS (6000.3.16f1)** · URP 2D · VContainer · UniTask · DOTween · Cinemachine 3 · Input System · **521 EditMode testi** · **150 birimlik savaşta 16 SetPass**

---

## Öne Çıkanlar
- **İki oyun modu:** Kaynakların, nüfusun, gece-gündüz döngüsünün ve baskın dalgalarının olduğu *Normal* mod; her türlü bina, birim ve kaynağın iki taraf için de istenen yere konabildiği *Sandbox* modu.
- **Alışıldık RTS kontrolleri:** Kutuyla seçim, tıklanan yere göre değişen sağ tık emirleri (yürü, saldır, kaynak topla, inşa et, tarlada çalış), Attack Move, Stop, Hold Position, kontrol grupları, boştaki köylüye giden kısayol ve toplanma noktaları.
- **Köylü ekonomisi:** Köylüler odun, taş, altın ve yemek toplayıp en yakın depoya taşır; binaları inşa eder, tarlada çalışır. Yemek yetmezse açlık başlar. Baskın gelince köylüler Castle'a sığınır.
- **Savaş:** Platoları ve merdivenleri olan bir grid üzerinde A\* ile yol bulma. Grup halinde gönderilen birimler hedefin etrafına dağılır. Saldırılar animasyonun vuruş anında, oklar hedefe ulaşınca hasar verir. Kuleler kendiliğinden ateş eder; düşman yapay zekâsı Castle'a yürür ve önünü kapatan duvarları yıkar.
- **Zorluk seviyeleri:** Kolay, Normal ve Zor; baskınların büyüklüğünü, başlangıç kaynaklarını ve açlığın şiddetini değiştirir. Bütün değerler veri dosyalarında (asset) tutulur.
- **Cila:** Can barları, ölüm ve yıkılma efektleri, birimlerin başında durum ikonları, ekran dışındaki baskını gösteren ok, kısayol tuşlarını yazan tooltip'ler, bütün tuşları listeleyen Controls sayfası (F1) ve anında kaydedilen ayarlar.

## Nasıl Oynanır?
- **Windows sürümü:** [Releases sayfasından](https://github.com/akncn57/Barracks-And-Bolts-Case-Study/releases) en son zip dosyasını indir, klasöre çıkar ve `Barracks & Bolts.exe`'yi çalıştır. Pencerenin boyutunu istediğin gibi değiştirebilirsin.
- **Kaynak koddan:** Projeyi **Unity 6000.3.16f1** ile aç. Gerekli paketler (Git adresinden gelen VContainer dahil) Package Manager tarafından kendiliğinden yüklenir. `Assets/Scenes/Bootstrap.unity` sahnesini açıp Play'e bas. `Gameplay.unity` sahnesini doğrudan da açabilirsin; bu durumda oyun varsayılan ayarlarla Sandbox modunda başlar.

### Oyun Modları
| Mod | Açıklama |
|---|---|
| **Play** (Normal) | Bir zorluk seç; bir Castle ve 3 köylüyle başla. Kaynak toplayıp üssünü büyüt ve 3., 6., 8. ve 10. günlerde gelen baskınlara dayan. Castle yıkılırsa kaybedersin; 10. günün baskınını püskürtürsen kazanırsın. |
| **Sandbox** | Kaynaklar sınırsız, binalar anında hazır. İki taraf için de istediğin bina, birim ve kaynağı haritaya koy; baskınları elle başlat, düşman yapay zekâsını ve debug görünümünü açıp kapat, 100 birimlik bir test savaşı başlat ya da haritayı temizle. |

## Kontroller
Bütün tuşları oyun içinde de **Controls** sayfasında görebilirsin (F1 ya da Settings → Controls).

| Tuş / Fare | İşlevi |
|---|---|
| W A S D / ok tuşları, fareyi ekran kenarına götürmek, orta tuşla sürüklemek | Kamerayı kaydırır |
| Fare tekerleği | Yakınlaştırır / uzaklaştırır |
| Sol tık / sol tıkla sürüklemek | Bir birimi ya da binayı seçer / kendi birimlerini kutu içine alarak seçer |
| Sağ tık | Tıklanan yere göre yürür, saldırır, kaynak toplar, inşa eder ya da tarlada çalışır. Üretim binası seçiliyken toplanma noktasını belirler |
| **F**, ardından sol tık | Attack Move: birimler tıklanan yere yürür ve yolda karşılaştıkları her düşmanla savaşır |
| **X** / **H** | Stop (dur) / Hold Position (yerini koru) |
| **Ctrl + 1–9** / **1–9** | Seçimi bir gruba kaydeder / o grubu seçer (iki kez basınca kamera gruba gider) |
| **.** (nokta) | Sıradaki boştaki köylüyü seçer |
| **Home** | Kamerayı Castle'a götürür |
| **Delete** | Seçili olanı siler |
| **Shift** + sol tık | Aynı binadan art arda yerleştirir |
| **P** / **+** **-** | Oyunu duraklatır / oyun hızını değiştirir (1x, 2x, 3x) |
| **Esc** | En üstteki katmanı kapatır (yerleştirme, açık pencere, seçim, menü); kapatılacak bir şey kalmayınca ayarları açar |
| **F1** | Controls sayfasını açar |

## Brief'teki Maddeler
| İstenen | Nasıl yapıldı? |
|---|---|
| Üretim menüsü: **object pooling kullanan sonsuz bir scroll view** | `RecyclingScrollView`: Sadece ekranda görünen satırlar ve bir yedek satır oluşturulur, kaydırdıkça bunlar yeniden kullanılır. Kaydırma değeri döngüsel tutulduğu için saatlerce kaydırılsa da kayma olmaz. Unity'nin `ScrollRect` bileşeni kullanılmadı. |
| **Geçerli alanı yeşil, geçersiz alanı kırmızı** gösteren bina yerleştirme | `PlacementService` ve `PlacementPreviewView`: Binanın kaplayacağı her hücre tek tek kontrol edilir (harita sınırı, dolu hücre, üstünde birim olması, bölge dışı, yetersiz kaynak). |
| **Barracks** (100 HP) ve **Power Plant** (50 HP) | Barracks ve Power Plant'in yerini alan **House** (nüfus kapasitesi verir, üretim yapmaz). Bunlara ek olarak Castle, Kule, Karakol ve Tarla. |
| Seçilen binanın resmini ve üretebildiği birimleri gösteren **bilgi paneli** | `InformationPanelView`: Binanın resmi, adı, anlık can değeri ve üretilebilen birimlerin listesi. |
| **3 tür asker**: 10 HP; 10, 5 ve 2 hasar; üretim süresi yok | Şövalye, Mızrakçı ve Okçu; değerler veri dosyalarında. Askerler binanın çıkış noktasında, orası doluysa en yakın boş hücrede doğar. |
| Birim seçimi ve **sağ tıkla A\* kullanarak hareket** | Projeye özel yazılmış `AStarPathfinder` (8 yönlü, köşeden geçmeyen, her aramada yeni bellek ayırmayan). Grup halinde gönderilen birimler hedefin etrafındaki ayrı hücrelere dağılır. |
| Birimlere ve binalara **sağ tıkla saldırı**, canı biten şeyin yok olması | `CombatService` ve `CombatSystem`. Bir birim ya da bina yok olunca grid, seçim, bilgi paneli ve ona saldıranlar event'ler üzerinden temizlenir. |
| Veriye dayalı tanımlar (ScriptableObject) | `BuildingDefinition`, `UnitDefinition`, sprite'lar için renk setleri ve hepsini toplayan bir `GameDatabase`. |
| **Boyutu değiştirilebilen pencere**, **SetPass < 20** | Pencere boyutu serbestçe değiştirilebiliyor; 150 birimlik bir savaşta 16 SetPass ve 231 draw call. |

## Mimari
```
Definition (ScriptableObject) ──okur──► Service / System (saf C#) ──yayınlar──► EventBus ──► View (MonoBehaviour)
                                              ▲                                                  │
                                              └────────── Input handler'lar / UI çağırır ◄───────┘
```
- **Oyun mantığı saf C# sınıflarında, MonoBehaviour'lar sadece görüntüden sorumlu.** Modeller (`UnitModel`, `BuildingModel`) ve servisler Unity bileşenlerine bağlı değil. 521 testin sahne açmadan çalışabilmesi bu sayede.
- **VContainer ile bağımlılık yönetimi (DI).** Uygulamanın tamamı için bir ana scope, her sahne için ayrı bir scope ve oyun arayüzü için bunlara bağlı bir alt scope var. Assembly Definition'lar bağımlılık yönünü `UI → Gameplay → Core → Utils` olarak sabitliyor; oyun mantığı arayüzü hiç göremiyor.
- **Her şey tek bir merkezi döngüde güncelleniyor.** `SimulationRunner`, bütün sistemleri her karede aynı sırayla çalıştırıyor: Zaman → Yapay zekâ → Hareket → Toplama → Savaş → Temizlik. Duraklatma ve oyun hızı yalnızca `GameClock` üzerinden yönetiliyor; `Time.timeScale`'e hiç dokunulmuyor. Bu sayede oyun duraklatıldığında da arayüz animasyonları çalışmaya devam ediyor. Ölen birimler de karenin sonunda toplu olarak temizleniyor.
- **Tip güvenli bir event sistemi.** Event'ler `readonly struct`; abonelikler `IDisposable` olarak dönüyor ve olay yayınlanırken yeni bellek ayrılmıyor. Event'ler sadece "ne oldu" bilgisini taşıyor; "şunu yap" demek için doğrudan servis metodu çağrılıyor.
- **Nesne referansı yerine kimlik numarası.** Birimler ve binalar birbirini `GameEntityId` ile tanıyor. Böylece ölmüş bir hedefe ait geçersiz bir referans kodda asılı kalmıyor.
- **Animator kullanılmadı.** Birimler, renk setlerindeki sprite karelerini oyun saatine göre kendileri oynatıyor. Taraf (`Faction`) ile renk (`TeamColor`) birbirinden bağımsız; düşman herhangi bir renkte olabilir.
- **Önce veri.** Can, hasar, maliyetler, inşa süreleri, baskın dalgaları, zorluk çarpanları ve açlık değerleri veri dosyalarında tutuluyor. Yeni bir bina ya da birim eklemek için kod yazmak gerekmiyor.

Ayrıntılar için: [Teknik Tasarım](Docs/tr/Technical_Design.md) · [Sistem Rehberi](Docs/tr/Systems.md)

## Performans
- Oyundaki bütün sprite'lar birkaç Sprite Atlas'ta toplandı ve tek bir materyalle çiziliyor. Can barları, ilerleme çubukları ve durum ikonları dünyaya yerleştirilmiş Canvas'lar yerine aynı atlastaki SpriteRenderer'larla çiziliyor.
- Arayüzün sabit ve sık değişen kısımları ayrı Canvas'larda duruyor. Yazılardaki kontur ve gölge efektleri, aynı font atlasını kullanan TMP materyal ayarlarıyla yapılıyor.
- Oklar, uçan yazılar ve efektler havuzdan (pool) geliyor. A\* ve BFS aramaları her seferinde aynı tamponları kullanıyor.
- **150 birim savaşırken: 16 SetPass, 231 draw call.** Bir A\* araması ortalama 0,01 ms'nin çok altında sürüyor. Oyundaki istatistik paneli (Settings → FPS Panel) FPS'i, A\* sürelerini ve SetPass'i anlık olarak gösteriyor.

## Testler
Grid, yol bulma, bina yerleştirme, bölge, inşaat, savaş, kaynak toplama, baskınlar, zorluk, açlık, kısayollar ve arayüz hesaplamaları için toplam **521 EditMode testi** var.
Testleri çalıştırmak için: *Window → General → Test Runner → EditMode → Run All*.

## Proje Yapısı
```
Assets/Scripts/
├── Core/       EventBus, simülasyon döngüsü, oyun saati, sahne geçişleri, oturum ve oyuncu ayarları
├── Gameplay/   grid, yol bulma, binalar, birimler, savaş, ekonomi, baskınlar, kamera, input
├── UI/         menüler, paneller, HUD, sonsuz scroll, tooltip'ler, ayarlar
├── Popup/      popup sistemi
├── Utils/      MinHeap, CompositeDisposable
└── Tests/      EditMode testleri
```

## Dokümanlar
Bütün dokümanlar aynı dosya adlarıyla hem İngilizce hem Türkçe olarak mevcut.

| Doküman | English | Türkçe |
|---|---|---|
| Oyun tasarımı kararları ("şu anki hali" notlarıyla) | [GDD_Decisions](Docs/en/GDD_Decisions.md) | [GDD_Decisions](Docs/tr/GDD_Decisions.md) |
| Teknik tasarım: mimari, kurallar, isimlendirme | [Technical_Design](Docs/en/Technical_Design.md) | [Technical_Design](Docs/tr/Technical_Design.md) |
| Sistem rehberi: her sistemin kodda nasıl çalıştığı | [Systems](Docs/en/Systems.md) | [Systems](Docs/tr/Systems.md) |
| Öncelikler ve bir haftalık çalışma planı | [Roadmap](Docs/en/Roadmap.md) | [Roadmap](Docs/tr/Roadmap.md) |
| Asset'ler: pakette neyin eksik olduğu ve nasıl tamamlandığı | [Missing_Assets](Docs/en/Missing_Assets.md) | [Missing_Assets](Docs/tr/Missing_Assets.md) |

Kodu incelemeye başlamak için en iyi yer [Sistem Rehberi](Docs/tr/Systems.md): Her bölümde o sistemin hangi dosyalarda olduğu ve hangi testlerle korunduğu yazıyor.

## Gelecek Planları
Aşağıdakiler tasarlandı ama henüz yapılmadı. Her birinin mevcut yapıya nasıl ekleneceği [Sistem Rehberi](Docs/tr/Systems.md#g-planlanan-sistemler)'nde anlatılıyor.
- Bina yükseltme ve asker rütbeleri
- Depo binaları ve kaynak kapasitesi
- Kilise ve yaralıları iyileştiren Monk
- Bina tamiri ve inşaatı iptal etme
- Köylülerin House'lara da sığınması ve bina başına sığınma kapasitesi
- Mini harita (ikinci bir kamera yerine grid verisinden üretilen tek bir doku ile)
- Seed'e göre rastgele harita üretimi
- Kaydetme ve kayıt yükleme
- Sonsuz mod
- Ses ve müzik
- Yürürken birimlerin birbirinin içinden geçmemesi
- Oyuna giriş eğitimi (tutorial)

## Kullanılan Kaynaklar
- **Görseller:** Pixel Frog'un [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) paketi. Depo binaları, Karakol, tarla, inşaat halindeki binalar, ikonlar, imleçler ve harita işaretleri yine bu paketin sprite'larından üretildi. Görseller paketin kendi lisansıyla kullanılıyor ve bu reponun herhangi bir lisansına dahil değil.
- **Font:** MedievalSharp (SIL Open Font License).
- **Kütüphaneler:** [VContainer](https://github.com/hadashiA/VContainer) (MIT), [UniTask](https://github.com/Cysharp/UniTask) (MIT), [DOTween](http://dotween.demigiant.com/) (Demigiant) ve Unity'nin Cinemachine, Input System, URP, Test Framework ve TextMesh Pro paketleri.

## Geliştirici
Akın Can Hançer
