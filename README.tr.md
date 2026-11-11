# Barracks & Bolts

[English](README.md) | **Türkçe**

Unity 6 ile yapılmış 2D, ortaçağ temalı bir gerçek zamanlı strateji oyunu: üs kur, köylülerle ekonomiyi döndür, on gün boyunca baskınlara dayan ya da yaratıcı sandbox modunda serbestçe oyna.

Bir oyun geliştirici pozisyonu için bir haftalık bir "strateji oyunu demosu" case study'si olarak başladı. Brief'in her maddesi karşılandı (bkz. [Brief Karşılığı](#brief-karşılığı)); haftanın geri kalanı demoyu temiz ve test edilmiş bir mimariyle küçük ama bütün bir oyuna çevirmeye gitti.

<!-- Ekran görüntüleri / GIF: Docs/Images altına ekleyip buraya bağlayın. -->

**Unity 6.3 LTS (6000.3.16f1)** · URP 2D · VContainer · UniTask · DOTween · Cinemachine 3 · Input System · **521 EditMode testi** · **150 birimlik savaşta 16 SetPass**

---

## Öne Çıkanlar
- **İki mod:** Kaynak, nüfus, gündüz/gece takvimi ve baskın dalgalarıyla *Normal* oyun; her şeyin, iki taraf için de, istenen yere konabildiği *Sandbox*.
- **RTS kontrolleri:** Kutu seçimi, bağlama göre sağ tık emirleri (yürü, saldır, topla, inşa et, tarlada çalış), Attack Move, Stop, Hold Position, kontrol grupları, boştaki köylü tuşu, toplanma noktaları.
- **Köylü ekonomisi:** Odun, taş, altın ve yemek toplama, yükü bırakma noktasına taşıma, köylülerin bitirdiği inşaat alanları, tarlalar, açlık, baskında Castle'a sığınma.
- **Savaş:** Platolu ve merdivenli grid üzerinde A\* yol bulma, hedefin etrafına dağılan gruplar, vuruş kareleri ve uçan oklar, kendiliğinden ateş eden kuleler, Castle'a yürüyüp duvarları yıkan düşman yapay zekâsı.
- **Zorluk:** Kolay, Normal ve Zor; baskın büyüklüğünü, başlangıç kaynaklarını ve açlığı değiştirir. Bütün değerler veri asset'lerinde.
- **Cila:** Can barları, ölüm ve yıkılma efektleri, birimlerin üstünde durum ikonları, ekran kenarında baskın oku, tuşları gösteren tooltip'ler, Controls sayfası (F1), anında kaydedilen ayarlar.

## Oynamak
- **Windows build:** [Releases sayfasından](https://github.com/akncn57/Barracks-And-Bolts-Case-Study/releases) son zip'i indir, aç ve `Barracks & Bolts.exe`'yi çalıştır. Pencere serbestçe boyutlandırılabilir.
- **Kaynak koddan:** Projeyi **Unity 6000.3.16f1** ile aç. Paketler, Git adresinden gelen VContainer dahil, Package Manager ile gelir. `Assets/Scenes/Bootstrap.unity`'yi açıp Play'e bas. `Gameplay.unity`'yi doğrudan açmak da çalışır; varsayılan ayarlarla bir Sandbox oyunu başlatır.

### Oyun modları
| Mod | Nedir |
|---|---|
| **Play** (Normal) | Zorluk seç, bir Castle ve 3 köylüyle başla, topla ve inşa et, 3., 6., 8. ve 10. günlerin baskınlarına dayan. Castle düşerse oyun biter; 10. günün baskınını yenersen kazanırsın. |
| **Sandbox** | Sınırsız kaynak, anında hazır binalar. İki taraf için de her bina, birim ve kaynağı koy, baskını elle başlat, düşman yapay zekâsını ve debug görünümünü aç kapa, 100 birimlik test savaşı kur, haritayı temizle. |

## Kontroller
Bütün tuşlar oyunun içinde de **Controls** sayfasında yazıyor (F1 ya da Settings → Controls).

| Girdi | Ne yapar |
|---|---|
| W A S D / ok tuşları, fare ekran kenarında, orta tuşla sürükleme | Kamerayı kaydırır |
| Fare tekerleği | Zoom |
| Sol tık / sol tıkla sürükleme | Birim ya da bina seçer / kendi birimlerini kutuyla seçer |
| Sağ tık | Farenin altındakine göre yürü, saldır, topla, inşa et ya da tarlada çalış. Üretim binası seçiliyken: toplanma noktasını belirler |
| **F**, sonra sol tık | Attack Move: oraya yürür ve yolda karşılaştığı her düşmanla savaşır |
| **X** / **H** | Stop / Hold Position |
| **Ctrl + 1–9** / **1–9** | Seçimi gruba kaydeder / grubu seçer (iki kez basınca kamera gruba gider) |
| **.** (nokta) | Sıradaki boştaki köylüyü seçer |
| **Home** | Kamerayı Castle'a götürür |
| **Delete** | Seçili olanı kaldırır |
| **Shift** + sol tık | Art arda bina koyar |
| **P** / **+** **-** | Duraklat / oyun hızı (1x, 2x, 3x) |
| **Esc** | En üstteki katmanı kapatır (yerleştirme, pencere, seçim, menü); kapatacak bir şey kalmayınca ayarları açar |
| **F1** | Controls sayfası |

## Brief Karşılığı
| Gereksinim | Projedeki karşılığı |
|---|---|
| **Object pooling'li sonsuz scroll view** olarak üretim menüsü | `RecyclingScrollView`: sadece görünen satırlar + 1 var; satırlar yeniden kullanılır, offset sarılır, float hassasiyeti hiç bozulmaz. `ScrollRect` yok. |
| **Yeşil / kırmızı önizlemeli** bina yerleştirme | `PlacementService` + `PlacementPreviewView`: footprint'in her hücresi kontrol edilir (harita sınırı, dolu hücre, yoldaki birim, bölge, maliyet). |
| **Barracks** (100 HP) ve **Power Plant** (50 HP) | Barracks ve Power Plant'in görevindeki **House** (nüfus verir, üretimi yok). Üstüne Castle, Kule, Karakol ve Tarla. |
| Binanın resmini ve birimlerini gösteren **bilgi paneli** | `InformationPanelView`: resim, ad, canlı HP, üretilecek birimlerin listesi. |
| **3 asker**, 10 HP, 10 / 5 / 2 hasar, üretim süresi yok | Şövalye, Mızrakçı, Okçu; değerler veri asset'lerinde. Binanın spawn noktasında, dolu ise en yakın boş hücrede doğarlar. |
| Seçim ve **A\* ile sağ tık hareketi** | Kendi `AStarPathfinder`'ımız (8 yön, köşe kesme yok, çöp üretmeyen); gruplar hedefin etrafında ayrı hücrelere dağılır. |
| Birim ve binaya **sağ tıkla saldırı**, 0 HP'de yok olma | `CombatService` / `CombatSystem`; ölünce grid, seçim, panel ve saldıranlar event'lerle temizlenir. |
| Veri odaklı tanımlar (ScriptableObject) | `BuildingDefinition`, `UnitDefinition`, sprite'lar için renk setleri, bir `GameDatabase`. |
| **Boyutlandırılabilir pencere**, **SetPass < 20** | Boyutlandırılabilir pencereli build; 150 birimlik savaşta 16 SetPass / 231 draw call. |

## Mimari
```
Definition (ScriptableObject) ──okur──► Service / System (saf C#) ──publish──► EventBus ──► View (MonoBehaviour)
                                              ▲                                                │
                                              └────────── Input handler / UI çağırır ◄─────────┘
```
- **Mantık saf C#'ta, Unity sadece görünüm için.** Modeller (`UnitModel`, `BuildingModel`) ve servisler MonoBehaviour'a bağlı değil; 521 EditMode testi sahnesiz çalışabiliyorsa bu sayede.
- **VContainer ile bağımlılık enjeksiyonu.** Bütün uygulama için bir kök scope, her sahne için bir scope ve gameplay canvas'ı için bir alt UI scope'u. Assembly definition'lar `UI → Gameplay → Core → Utils` yönünü zorunlu kılar; Gameplay UI'ı göremez.
- **Tek merkezi tick.** `SimulationRunner` bütün sistemleri sabit fazlarda çalıştırır (Time → AI → Movement → Gathering → Combat → Cleanup); duraklatmanın ve oyun hızının sahibi `GameClock`. `Time.timeScale`'e hiç dokunulmaz; oyun dururken de arayüz animasyonları çalışır, ölümler tick'in sonunda toplu işlenir.
- **Tipli event bus.** `readonly struct` event'ler, dispose edilebilen abonelikler; yayın sırasında allocation yok. Event'ler olanı haber verir, komutlar düz metot çağrısıdır.
- **Referans yerine id.** Varlıklar birbirine `GameEntityId` ile başvurur; ölen bir hedef asla asılı bir referans olarak kalmaz.
- **Animator yok.** Birimler renk setlerindeki sprite karelerini oyun saatine göre oynatır. Taraf (`Faction`) ve renk (`TeamColor`) birbirinden bağımsızdır; düşman herhangi bir renk olabilir.
- **Önce veri.** HP, hasar, maliyetler, inşa süreleri, baskın dalgaları, zorluk çarpanları ve açlık asset'lerde durur. Yeni bir bina ya da birim için kod gerekmez.

Daha fazlası: [Teknik Tasarım](Docs/tr/Technical_Design.md) · [Sistem Rehberi](Docs/tr/Systems.md)

## Performans
- Oyun içi bütün sprite'lar birkaç Sprite Atlas'ta ve tek bir sprite materyaliyle çizilir. Can barları, ilerleme çubukları ve durum ikonları world-space canvas değil, gameplay atlas'ındaki SpriteRenderer'lar.
- Statik ve dinamik arayüz ayrı canvas'larda; yazı efektleri tek font atlası üzerinde TMP materyal preset'leri.
- Oklar, uçan yazılar ve efektler havuzdan gelir. A\* ve BFS aramaları tamponlarını yeniden kullanır.
- **150 birim savaşırken: 16 SetPass, 231 draw call.** A\* aramaları ortalamada 0,01 ms'nin çok altında. Oyun içindeki istatistik paneli (Settings → FPS Panel) FPS, A\* süresi ve SetPass'i canlı gösterir.

## Testler
Grid, yol bulma, yerleştirme, bölge, inşaat, savaş, toplama döngüsü, baskınlar, zorluk, açlık, kısayollar ve arayüz matematiği üzerinde **521 EditMode testi**.
*Window → General → Test Runner → EditMode → Run All* ile çalışır.

## Proje Yapısı
```
Assets/Scripts/
├── Core/       EventBus, simülasyon döngüsü, oyun saati, sahneler, oturum ve oyuncu ayarları
├── Gameplay/   grid, yol bulma, binalar, birimler, savaş, ekonomi, baskınlar, kamera, input
├── UI/         menüler, paneller, HUD, sonsuz scroll, tooltip'ler, ayarlar
├── Popup/      popup sistemi
├── Utils/      MinHeap, CompositeDisposable
└── Tests/      EditMode testleri
```

## Dokümanlar
Her doküman aynı dosya adlarıyla hem İngilizce hem Türkçe var.

| Doküman | English | Türkçe |
|---|---|---|
| Oyun tasarımı kararları, "şu anki hali" notlarıyla | [GDD_Decisions](Docs/en/GDD_Decisions.md) | [GDD_Decisions](Docs/tr/GDD_Decisions.md) |
| Teknik tasarım: mimari, kurallar, isimlendirme | [Technical_Design](Docs/en/Technical_Design.md) | [Technical_Design](Docs/tr/Technical_Design.md) |
| Sistem rehberi: her sistemin kodda nasıl çalıştığı | [Systems](Docs/en/Systems.md) | [Systems](Docs/tr/Systems.md) |
| Öncelikler ve bir haftalık takvim | [Roadmap](Docs/en/Roadmap.md) | [Roadmap](Docs/tr/Roadmap.md) |
| Asset'ler: neyin eksik olduğu ve nasıl yapıldığı | [Missing_Assets](Docs/en/Missing_Assets.md) | [Missing_Assets](Docs/tr/Missing_Assets.md) |

Kodda yolunu bulmak için [Sistem Rehberi](Docs/tr/Systems.md) ile başla: her bölüm kendi dosyalarını ve testlerini yazar.

## Gelecek Planları
Tasarlandı ama henüz yapılmadı; her birinin mevcut yapıya nasıl oturacağı [Sistem Rehberi](Docs/tr/Systems.md#g-planlanan-sistemler)'nde.
- Bina yükseltme ve asker rütbeleri
- Depo binaları ve kaynak kapasitesi
- Kilise ve iyileştiren Monk
- Bina tamiri ve inşaat iptali
- Bina başına kapasiteyle House'lara sığınma
- Mini harita (ikinci kamera değil, grid verisinden tek bir doku)
- Seed'den rastgele zemin üretimi
- Kaydetme ve yükleme
- Sonsuz mod
- Ses ve müzik
- Yürüyen birimler arasında ayırma kuvveti
- Tutorial

## Emeği Geçenler
- **Görseller:** Pixel Frog'un [Tiny Swords](https://pixelfrog-assets.itch.io/tiny-swords) paketi. Depo binaları, Karakol, tarla, inşaat alanları, ikonlar, imleçler ve işaretler paketin kendi sprite'larından üretildi. Görseller paketin lisansıyla kullanılıyor; bu repodaki hiçbir şeyin kapsamında değil.
- **Font:** MedievalSharp, SIL Open Font License.
- **Kütüphaneler:** [VContainer](https://github.com/hadashiA/VContainer) (MIT), [UniTask](https://github.com/Cysharp/UniTask) (MIT), [DOTween](http://dotween.demigiant.com/) (Demigiant), Unity'nin Cinemachine, Input System, URP, Test Framework ve TextMesh Pro paketleri.

## Yazar
Akın Can Hançer
