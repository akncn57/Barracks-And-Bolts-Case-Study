# Barracks & Bolts – GDD Kararları (Taslak)

> Kaynak: Panteon "Game Developer – Strategy Game Demo" case study PDF'i.
> Bu dosya GDD öncesi alınan kararların kaydıdır.

## Alınan Kararlar
- **Tema:** Ortaçağ (eldeki free asset paketi ortaçağ temalı).
- **Castle = Ana üs (HQ).** Kaybetme koşulları "Takvim ve Dalgalar" bölümünde.
- **Mevcut asset binaları:** Castle, House, okçu binası, kule, kilise benzeri bir yapı. İleride yenileri eklenebilir.
- **Bina boyutları ve değerleri** asset paketi incelenirken belirlenecek. PDF'teki değerler örnek niteliğinde, kendi değerlerimizi belirleyebiliriz.

### Oyun Modları
- **Normal Mod:** Kaynak, nüfus ve takvim sistemleri aktif.
- **Sandbox Modu (Creative):** Minecraft Creative moduna benzer. Oyuncu oyundaki her şeyi istediği yere koyabilir. PDF kurallarını birebir karşılar ve inceleyen kişi mekanikleri kolayca test edebilir.
  - Kaynaklar sınırsız, nüfus limiti yok, üretim ve inşa anlık.
  - Bir yerleştirme paleti var: tüm binalar, tüm birimler (oyuncu ve düşman tarafı), kaynaklar (ağaç, taş, maden, koyun).
  - Bölge (territory) sınırı yok, bina her yere konabilir.
  - Takvim ve baskınlar açılıp kapatılabilir. Baskın manuel olarak da başlatılabilir.
  - Silme aracı ve zaman kontrolü (durdur, hızlandır) var.
  - Oyuncu kendi savaşlarını kurabilir: kendi ordusu ile düşman birim ve binalarını karşı karşıya koyabilir.
  - **Şu anki hali (minimal):** Side düğmesi yeni bina ve birimlerin tarafını değiştirir. Düşman binası konur, düşman Barracks'ından düşman asker üretilir. Tam palet P2'de.
- **Zorluk Seviyesi (Normal Mod):** Kolay, Normal, Zor. Düşman sayısını, düşman gücünü ve başlangıç kaynaklarını etkiler. Değerler bir `DifficultySettings` ScriptableObject'inde tutulur.

### Nüfus (Power Plant karşılığı = House)
- Oyuncu 1 nüfusla başlar, bu da 1 köylüdür.
- House inşa etmek nüfus kapasitesini artırır.
- **Askerler ve köylüler nüfustan yer kaplar.**
- House: 50 HP, üretim menüsü yok. PDF'teki Power Plant'in birebir karşılığı.

### Ekonomi
- Kaynaklar: **Odun, Taş, Yemek, Altın** (ileride başkaları da eklenebilir).
- Kullanım alanları: bina inşası, nüfusun beslenmesi (yemek tüketimi), bina yükseltme.

| Kaynak | Haritadaki Kaynağı | Kullanım |
|---|---|---|
| Odun | Ağaçlar | İnşa, yükseltme |
| Taş | Taş kayalıkları | İnşa, yükseltme |
| Yemek | **Koyunlar** (köylü keser). İleride tarla eklenecek, asseti bulunacak ya da yapılacak. | Nüfusu besleme |
| Altın | **Altın madenleri** | Bina yükseltme (diğer kaynaklarla birlikte harcanır) |

- **Bina yükseltme:** Altın ve diğer kaynaklardan belirli miktarlar harcanarak yapılır.

### Yükseltme Etkileri
| Bina | Her Seviyede Kazandığı |
|---|---|
| Tüm binalar | Maksimum HP artar |
| House | Ek nüfus kapasitesi |
| Kule | Üzerindeki okçuların hasarı ve okçu sayısı artar |
| Kilise | Yetiştirilen Monk'un iyileştirme gücü artar |
| Depolar | Depolama kapasitesi artar |
| Barracks | Üretilen askerin seviyesi artar |

- **PDF uyumu:** Seviye 1 askerler PDF'teki değerlerde olacak (10 HP, 10 / 5 / 2 hasar). Daha yüksek seviyeler bu değerlerin üstüne çıkar.
- **Karar:** Barakanın ayrı bir asker kapasitesi yok. Birlik sayısını yalnızca nüfus limiti sınırlar.
- **Karar:** Baraka yükseltmesi sadece bundan sonra üretilen askerleri etkiler. Seviye her askerin kendi özelliğidir ve askerin üzerinde gösterilir (ör. rütbe simgesi). Farklı seviyelerdeki barakalar farklı seviyede asker üretir.
- **Şu anki hali:** Köylüler 3 kişi başlıyor, Castle sadece Normal modda köylü üretiyor. Sağ tıkla kaynağa gönderiliyor. Bırakma noktası sadece Castle. Castle ve House 5'er nüfus veriyor. Sayılar geçici: `Data_Config_NormalMode`, `Data_Config_Raids` ve tanım asset'lerinde.
- **Kaynak toplama (köylü):**
  1. Oyuncu köylüyü sol tıkla seçer.
  2. İmleç bir kaynağın (ağaç, taş, yemek) üstüne gelince toplama imlecine dönüşür.
  3. Oyuncu kaynağa tıklar. Köylü A* ile oraya gider ve kaynak toplamaya başlar.
  4. Topladığı kaynağı en yakın uygun depoya (veya Castle'a) taşır ve geri döner. Ayrıntılar "Depo Sistemi" bölümünde.

### Takvim ve Dalgalar (Geçici Değerler)
Bu değerler ScriptableObject üzerinden ayarlanabilir olacak, ileride değiştirilebilir.
- **1 gün = 3 dakika:** 2 dakika gündüz, 1 dakika gece.
- **Toplam süre:** 10 gün.
- **Saldırı günleri:** 3, 6, 8 ve 10. Baskın, o günün herhangi bir anında başlayabilir (gündüz ya da gece).
- **Baskın sırasında zaman durur:** Gündüz/gece döngüsü ilerlemez. Tüm dalgalar yenildiğinde döngü kaldığı yerden devam eder.
- Her saldırı 2–3 dalgadan oluşur. Sonraki saldırılarda düşmanlar daha kalabalık ve daha güçlü olur.
- **Kazanma:** 10. gündeki saldırı da püskürtülürse oyun kazanılır.
- **Sonsuz Mod:** Zafer ekranında "Devam Et" seçeneği çıkar. Oyuncu devam ederse belirli aralıklarla, giderek zorlaşan baskınlar gelmeye devam eder. Ekranda hayatta kalınan gün sayısı gösterilir.
- **Baskın Bildirimleri:**
  1. Saldırı gününün sabahında ekranda "Bugün saldırı günü!" bildirimi çıkar.
  2. Baskın başlarken ekranda "Baskın var!" uyarısı çıkar ve borozan sesi çalar. Düşmanın geleceği yön mini haritada ve ekran kenarında oklarla gösterilir.
- **Kaybetme:** Castle yıkılırsa ya da nüfus 0'a düşer ve Castle'ın yeni köylü üretecek kadar yemeği kalmazsa oyun kaybedilir.

### Birlikler
| Birim | HP | Hasar | Üretildiği Bina | Not |
|---|---|---|---|---|
| Köylü (Villager) | 10 | 1 | Castle | Kaynak toplar |
| Şövalye (Knight) | 10 | 10 | Barracks | PDF Soldier 1 |
| Mızrakçı (Spearman) | 10 | 5 | Barracks | PDF Soldier 2 |
| Okçu (Archer) | 10 | 2 | Barracks | PDF Soldier 3, menzilli |
| Rahip (Monk) | 10 | – | Kilise | Ekstra birlik, dostlarını iyileştirir |

### Bina Görevleri
| Bina | Görev |
|---|---|
| Castle | Ana üs. Köylü üretir, kaynaklar buraya taşınır. Yıkılırsa oyun kaybedilir. |
| Barracks | Şövalye, mızrakçı ve okçu üretir. Askerler barakanın spawn point'inde belirir. |
| House | Nüfus kapasitesini artırır. Üretim menüsü yoktur (Power Plant karşılığı). |
| Kule | Üzerindeki okçular belirli bir yarıçaptaki alanı korur ve menziline giren düşmana otomatik ateş eder. Kuledeki okçular binanın parçasıdır, nüfustan yer kaplamaz ve tek tek seçilemez. |
| Kilise | Monk yetiştirir. |
| Oduncu Deposu (Lumber Storage) | Odun depolar. Köylüler buraya odun bırakır. |
| Taş Deposu (Stone Storage) | Taş depolar. |
| Ambar (Granary) | Yemek depolar. |
| Hazine (Treasury) | Altın depolar. |
| Karakol (Outpost) | Bölgeyi genişletir. Yükseltilince bölge yarıçapı büyür. |

### Depo Sistemi
- Her kaynağın kendi depo binası vardır. Assetlerde karşılığı yoksa mevcut assetlerden üretilecek.
- **Kaynak kapasitesi:** Bir kaynaktan en fazla, o kaynağın depolarının toplam kapasitesi kadar tutulabilir. Kapasite doluysa köylü toplamayı bırakır ve bekler, UI'da "Depo dolu" uyarısı gösterilir.
- **Castle**, her kaynak için küçük bir başlangıç kapasitesi verir ve her kaynak için bırakma noktasıdır. Böylece oyunun başında depo olmadan da oynanabilir.
- Köylü, topladığı kaynağı o kaynağı kabul eden **en yakın** depoya (veya Castle'a) taşır. Bu, depoları kaynakların yakınına kurmayı stratejik hale getirir.
- Depo yıkılırsa kapasite düşer. Kapasiteyi aşan kaynak kaybolur.
- **Sandbox Modu:** Kapasite sınırsızdır.

## Asset Eşleştirmesi (Tiny Swords – 64px tile)
| Oyun Öğesi | Asset | Sprite (px) | Önerilen Taban (hücre) |
|---|---|---|---|
| Castle | `Texture_Buildings_Castle` | 320x256 | 5x3 |
| Barracks | `Texture_Buildings_Barracks` | 192x256 | 3x3 |
| Kilise | `Texture_Buildings_Monastery` | 192x320 | 3x3 |
| Kule | `Texture_Buildings_Tower` | 128x256 | 2x2 |
| House | `Texture_Buildings_House` | 128x192 | 2x2 |
| Depolar (Odun, Yemek, Altın, Taş) | `Texture_Buildings_<Renk>_Storage_*` (Archery sundurması + avluda kaynak yığını, üretildi) | 192x256 | 3x3 (kesinleşecek) |
| Köylü | `Pawn` (Idle, Run, Axe, Pickaxe, Hammer) | 192 kare | 1x1 |
| Şövalye | `Warrior` | 192 kare | 1x1 |
| Mızrakçı | `Lancer` | 320 kare | 1x1 |
| Okçu | `Archer` (+ Arrow) | 192 kare | 1x1 |
| Rahip | `Monk` (+ Heal VFX) | 192 kare | 1x1 |
| Koyun | `Sheep` | 128 kare | 1x1 |
| Odun | `Tree1–4` (animasyonlu) → biter, `Stump` kalır | 192x256 kare | 1x1 |
| Altın | `Gold Stone 5–6` (+ Highlight) | 128x128 | 2x1 |
| Taş | `Rock1–4`. Birkaç kaya bir araya gelip bir taş ocağı kümesi oluşturur | 64x64 | 1x1 (her kaya) |
| Zemin | `Tilemap_color1–5`, `Water Background`, `Water Foam`, `Water Rocks` | 64 tile | – |

- **Klasör yapısı renge göredir:** `Buildings/<Renk>/Texture_Buildings_<Renk>_<Bina>`, `Units/<Renk>/<Birim>/Texture_Units_<Renk>_<Birim>_<Animasyon>`. Renksiz assetler ayrı klasördedir: `Units/Sheep`, `Units/Projectiles` (ok), `VFX`.
- Tablodaki asset adları renk öneki olmadan yazılmıştır (ör. `Castle` → `Texture_Buildings_Red_Castle`).
- **Taban** binanın grid'de kapladığı alandır. Sprite'ın üst kısmı (çatı) tabanın dışına taşar ve üstteki hücreleri örter (pivot altta).
- Assette olmayanlar (üretilecek): Depolar (4 adet), Karakol, Tarla, ses ve müzik. Düşman renkli birimleri kullanıcı ekleyecek.
- Oyun içi sprite'ların PPU değeri 64'e çekildi (1 tile = 1 Unity birimi = 1 grid hücresi). UI ve imleç texture'ları 100'de kaldı.
- **Archery:** Asker üretmiyor; PDF'e uymak için şövalye, mızrakçı ve okçunun üçünü de Barracks üretir. Archery'nin sundurması, hedefleri silinip dört depo binasının ortak görseli oldu. Karakol için başka bir görsel bulunacak.
- Zeminde su olduğu kesin: su yürünemez alan olacak. Tileset yükseltilmiş zemin ve uçurum da içeriyorsa onlar da kullanılabilir.

## Gameplay Kuralları

### İnşa
- **Normal Mod:** Bina, yerleştirildiği anda iskele (yarım) halinde belirir ve kaynak o an harcanır. Seçili köylüler A* ile binaya gidip belirli bir sürede inşa eder. Birden fazla köylü çalışırsa inşa hızlanır.
- İnşa halindeki bina düşük HP ile başlar ve saldırıya açıktır. İnşa ilerledikçe HP'si dolar.
- İnşa tamamlanmadan bina üretim yapamaz ve nüfus kapasitesi vermez.
- İnşa iptal edilirse harcanan kaynağın bir kısmı (ör. %50) geri verilir.
- **Sandbox Modu:** İnşa anında tamamlanır. Bu, PDF'teki "üretim süresi yok" maddesini karşılar.

### Açlık
- Yemek her gün sabah nüfus başına tüketilir.
- Yemek yetmezse açlık başlar ve tüm birimler belirli aralıklarla yavaşça HP kaybeder.
- Yemek tekrar gelince HP kaybı durur.
- Açlık nüfusu 0'a düşürebilir. Kaybetme koşulu için "Takvim ve Dalgalar" bölümüne bakın.
- UI'da yemek yetersizse uyarı gösterilir.

> **Şu anki hali:** Düşmanlar kıyıdan çıkıyor. Sonraki baskınlar daha kalabalık ve daha çok şövalyeli; can ve hasar çarpanı yok, PDF değerleri korunuyor. Yemek tüketimi ve açlık, zorluk seçimi, sonsuz mod, ses ve mini harita yapılmadı.

### Düşman AI
- Düşmanlar baskın başladığında haritanın kenarındaki spawn noktalarında dalgalar halinde belirir.
- A* ile Castle'a doğru yürürler. Yolda menzillerine giren oyuncu birimine veya binasına saldırırlar.
- Hedefleri ölünce ya da menzillerinden çıkınca Castle'a yürümeye devam ederler.
- Duvarlar ve binalar yolu kapatırsa, en yakın engeli yıkarak yol açarlar (edge case: Castle tamamen kapatılırsa).

### Kaynak Tükenmesi
- Ağaç, taş kayalığı ve altın madeninin sınırlı miktarda kaynağı vardır. Biten kaynak haritadan kaldırılır ve grid'deki hücreleri yürünebilir hale gelir (A* güncellenir).
- Koyunlar her sabah belirli bir sayıda yeniden doğar. Haritada maksimum koyun sayısı sınırı vardır.
- Koyun kendi hücresinin içinde küçük adımlarla gezer. Tek kesimde ölür ve bütün yemeği (40) tek seferde taşınır, köylünün taşıma kapasitesine bakılmaz.
- Kaynağı biten köylü, yakındaki aynı tipte bir kaynağa otomatik geçer. Yakında yoksa boşta bekler.

## Harita (Procedural Generation)

### Genel
- Her oyun başında harita rastgele üretilir. Üretim **seed** tabanlıdır: aynı seed aynı haritayı verir. Bu, hata ayıklamayı ve testi kolaylaştırır.
- Ana menüde seed girilebilir veya "Rastgele" seçilebilir.
- Harita grid tabanlıdır ve A* ile bina yerleştirme sistemi aynı grid'i kullanır. Varsayılan boyut: 64x64 hücre (ayarlanabilir).
- Zemin Unity **Tilemap** ile çizilir. Tilemap tek materyal ve tek atlas kullanarak SetPass sayısını düşük tutar.

> **Şu anki hali:** Sahnede sabit bir harita var: 64x64 alanda ada, iki su birikintisi, üç düzensiz şekilli plato (biri denize bakan uçurumlu). Gölge kullanılmadı. Zemin sabit, rastgele zemin üretimi P2'de. **Kaynaklar ise her oyunda seed ile rastgele dağılıyor:** ortada Castle için boş alan, yakınında her kaynaktan birer tane var; merdiven önleri açık kalıyor, hiçbir kaynak haritayı ikiye bölemiyor.

### Üretim Adımları
1. **Zemin:** Perlin noise ile çimen, toprak ve (varsa) su/uçurum bölgeleri oluşturulur. Su ve uçurum yürünemez.
2. **Castle:** Haritanın ortasına yakın, düz ve açık bir alana yerleştirilir. Çevresinde garantili boş bir inşa alanı bırakılır.
3. **Başlangıç kaynakları (adil başlangıç):** Castle'ın belirli bir yarıçapı içinde mutlaka en az 1 orman kümesi, 1 taş kayalığı, 1 altın madeni ve birkaç koyun bulunur.
4. **Dış kaynaklar:** Ormanlar Perlin noise ile küme halinde dağıtılır. Taş ve altın, birbirinden ve Castle'dan minimum mesafede rastgele noktalara yerleştirilir (Poisson disk benzeri dağılım). Uzaktaki kaynaklar daha zengin olabilir, bu da oyuncuyu genişlemeye teşvik eder.
5. **Düşman spawn noktaları:** Harita kenarlarında 2–4 nokta seçilir. Her noktanın Castle'a minimum mesafede olması gerekir.
6. **Doğrulama:** Flood fill / A* ile her spawn noktasından Castle'a ve tüm başlangıç kaynaklarına yürünebilir bir yol olduğu kontrol edilir. Yol yoksa engel temizlenir veya harita yeni bir seed ile tekrar üretilir.

### Edge Case Notları
- Bina ve kaynak yerleştirmede tüm grid sınırları kontrol edilir (harita dışına taşma olmaz).
- Kaynaklar spawn noktalarını ve Castle'ın çevresini kapatamaz.
- Üretim deterministik olmalıdır: `System.Random(seed)` kullanılır, `UnityEngine.Random` global state'i kullanılmaz.
- Ayarlar (boyut, kaynak yoğunluğu, spawn sayısı) bir `MapGenerationSettings` ScriptableObject'inde tutulur.

### Düşman Tarafı ve Renk Seçimi
- Düşman birlikleri oyuncunun birlikleriyle aynıdır: Şövalye, Mızrakçı, Okçu ve Rahip. Sadece renkleri farklıdır.
- **Renk taraf belirtmez:** Oyuncu yeni oyun ayarlarında kendi rengini seçer, düşman kalan renklerden birini alır. Kod hiçbir yerde "kırmızı = düşman" gibi bir varsayım yapmaz. Taraf (`Faction`) ve renk (`TeamColor`) ayrı kavramlardır.
- Her renk için bir **renk seti** (ScriptableObject) tanımlanır: o rengin bina ve birim sprite'ları. Yeni renk eklemek için yeni bir klasör ve yeni bir set yeterlidir.
- Mevcut renkler: **Kırmızı** ve **Sarı** (ikisi de tam set).
- Oyunda baltayla koşma animasyonu kullanılmayacak. Köylü her durumda normal `Run` animasyonuyla koşar.
- Renk setleri arasında küçük bir fark var: Sarıda `Warrior_Guard` var, Kırmızıda yok. Kod, bir animasyon eksikse varsayılan animasyona (Idle / Run) düşmelidir.
- **Dost ateşi yok:** Oyuncu kendi birimlerine ve binalarına saldırma emri veremez.
- **Karşılık verme:** Boşta duran birim, kendisine vurana saldırır. Yürüyen ya da hedefi olan birim işine devam eder.
- **Düşman yerleşkeleri (ileride konuşulacak):** Haritada rastgele küçük düşman yerleşkeleri olabilir. Böylece PDF'teki "binaya saldırma" maddesi düşman binaları üzerinden de karşılanır.

### Birlikte Hareket
- Birden fazla birim aynı noktaya gönderilince üst üste binmezler. Hedef noktanın etrafındaki boş hücrelere dağıtılırlar.

### Spawn Point ve Toplanma Noktası
- Her üretim binasının (Castle, Barracks, Kilise) bir spawn point'i vardır.
- Spawn hücresi doluysa ya da kapalıysa birim en yakın boş hücrede doğar.
- Üzerinde birim duran alana bina konamaz; önizlemede o hücreler kırmızı görünür.
- Bina seçiliyken sağ tıklanırsa bir toplanma noktası (rally point) belirlenir. Yeni doğan birimler oraya kendiliğinden yürür.

### Köylü Kaçışı
> **Şu anki hali:** Köylü baskında işine devam ediyor, düşman 7 hücre yaklaşınca Castle'a koşup içeri giriyor. Taşıdığı yük o an sayılıyor. Haritada düşman kalmayınca çıkıp işine dönüyor. Oyuncunun verdiği emir 10 saniye kaçıştan önce geliyor. House'a sığınma ve bina kapasitesi yok; Castle yıkılınca oyun bittiği için içeridekilerin çıkması da gerekmiyor.

- Saldırıya uğrayan köylü işini bırakır ve en yakın House'a ya da Castle'a kaçar.
- İçeri giren köylü gizlenir ve saldırıya uğramaz. Tehlike geçince (baskın bittiğinde ya da yakında düşman kalmadığında) dışarı çıkar ve önceki işine kendiliğinden döner.
- Sığınılan bina yıkılırsa içindeki köylüler dışarı çıkar.
- Her binanın bir sığınma kapasitesi vardır. Bina doluysa köylü bir sonraki en yakın binaya gider.

### Bölge (Territory) Sistemi
- Castle'ın çevresinde belirli bir yarıçapta bir **bölge** vardır. Oyuncu bina sadece kendi bölgesinin içine koyabilir.
- **Karakol (Outpost):** Bölgeyi genişletmek için kullanılan yeni bir binadır. Bölgenin içine, sınıra yakın bir yere konur ve çevresinde yeni bir bölge açar. Karakol yükseltilince yarıçapı büyür. Görseli: Tower + takım renginde bayrak (`Texture_Buildings_<Renk>_Outpost`). Savunma kulesiyle karışmaması için bilgi panelinde adı ve bayrağı ayırt edici.
- **Kule** de küçük bir yarıçapta bölge açar. Bu sayede savunmayla genişleme birbirine bağlanır.
- Bina yerleştirme modundayken bölge sınırları zeminde gösterilir. Bölgenin dışı geçersiz alan olarak kırmızı işaretlenir (PDF'teki kırmızı alan maddesiyle uyumlu).
- Karakol ya da kule yıkılırsa bölge küçülür. Bölge dışında kalan binalar yerinde kalır ve çalışmaya devam eder, ama oraya yeni bina konamaz.
- Kaynak toplama ve birim hareketi bölgeyle sınırlı değildir, harita her yerde gezilebilir.
- Bölge hesabı grid üzerinde yapılır: her hücre için "bölge içinde mi?" bilgisi tutulur. Bu bilgi sadece bölge kaynağı (Castle, Karakol, Kule) eklenince ya da kaldırılınca yeniden hesaplanır.

### Mini Harita
- Ekranın bir köşesinde mini harita olur. Haritada zemin, kaynaklar, binalar, birimler (oyuncu ve düşman ayrı renkte), bölge sınırları ve kameranın gördüğü alan gösterilir.
- Mini haritaya tıklanınca kamera o noktaya gider. Sağ tıklanınca seçili birimler oraya gönderilir.
- **SetPass notu:** Mini harita ikinci bir kamerayla değil, grid verisinden üretilen tek bir `Texture2D` ile çizilir. İkinci kamera tüm sahneyi bir kez daha çizer ve draw call sayısını ikiye katlar. Birimler texture üzerinde nokta olarak gösterilir ve belirli aralıklarla güncellenir.

### Monk
- Menzilindeki yaralı dost birimleri **kendiliğinden** seçip iyileştirir. Oyuncunun hedef göstermesi gerekmez.

### Tamir
- Köylüler hasar almış ama yıkılmamış binaları tamir edebilir. Tamir, kaynak harcar.
- **Manuel:** Köylü seçilir ve hasarlı binaya sağ tıklanır.

### Gece
- Şimdilik saldırı dışında bir etkisi yok. Sadece görsel olarak ortam kararır.

### Oyun Akışı
1. **Ana Menü:** Yeni Oyun, Devam Et / Kayıt Yükle (kayıt varsa), Ayarlar, Çıkış.
2. **Yeni Oyun:** Önce mod seçilir (Normal veya Sandbox), ardından oyun ayarları yapılır (seed vb.). Son olarak dünya oluşturulur ve oyun başlar.
3. **Oyun içi:** Duraklatma menüsünden Devam Et, Kaydet, Ayarlar ve Ana Menü'ye erişilir.
4. **Oyun Sonu:** Kazandın veya Kaybettin ekranı çıkar. Ekranda istatistikler, Tekrar Oyna ve Ana Menü seçenekleri bulunur.

### Kayıt Sistemi
- Kayıt dosyasında seed, gün ve saat bilgisi, kaynaklar, binalar (tip, seviye, HP, konum), birimler (tip, seviye, HP, konum, mevcut emir) ve haritadaki kaynakların kalan miktarları tutulur.
- Harita seed'den yeniden üretilir, üstüne değişiklikler uygulanır. Bu, kayıt dosyasını küçük tutar.
- Format JSON olur ve `Application.persistentDataPath` altına yazılır.
- **Manuel kayıt** yapılır, otomatik kayıt yoktur. **Birden fazla kayıt slotu** olur.

### Ayarlar
- Klasik ayarlar: ses seviyeleri (genel, müzik, efekt), çözünürlük, pencere modu, VSync, kenar kaydırma açık/kapalı. Ekstralar sonra konuşulacak.

### Ses
- Şu an ses ve efekt yok. Sonradan eklenecek, ya bulunacak ya da üretilecek.

### Tutorial
- Kapsamlı bir tutorial yapılacak. Adımları oyun netleşince belirlenecek.

### Genel Notlar
- Tüm sayısal değerler (HP, hasar, saldırı hızı, menzil, maliyetler, süreler) ScriptableObject'lerde tutulur ve kod tarafına geçerken belirlenir.
- UI düzeni, kullanıcının vereceği çizime göre ayrıca konuşulacak.
- Klasör yapısı ve kod standartları ayrı bir teknik dokümana yazılacak.
- SetPass < 20, resizable pencere ve Canvas ayarları geliştirme boyunca kontrol edilecek.

## Input Map (Unity Input System)
Tuşlar Age of Empires, StarCraft ve Warcraft III gibi RTS oyunlarının alışılmış kısayolları baz alınarak seçildi.
**[PDF]** etiketli olanlar case study'de zorunlu istenen inputlardır.

### Action Map: `Camera`
| Action | Tuş | Tür | Not |
|---|---|---|---|
| `Pan` | WASD / Ok tuşları | Value (Vector2) | |
| `EdgePan` | Mouse ekran kenarında | Value (Vector2) | Varsayılan kapalı; ayarlardan açılabilir |
| `DragPan` | Orta tuş basılı + sürükle | Button + Delta | |
| `Zoom` | Mouse tekerleği | Value (float) | Min/max zoom sınırı olur |
| `FocusSelection` | Space | Button | Kamerayı seçili birime ortalar |
| `FocusCastle` | H | Button | Kamerayı Castle'a ortalar |

### Action Map: `Gameplay`
| Action | Tuş | Tür | Not |
|---|---|---|---|
| `Select` | Sol tık | Button | **[PDF]** Birim veya bina seçer |
| `BoxSelect` | Sol tık basılı + sürükle | Button + Position | Sadece kendi birimlerimiz seçilir |
| `AddToSelection` | Shift + Sol tık | Modifier | Seçime ekler veya seçimden çıkarır |
| `SelectAllOfType` | Çift sol tık / Ctrl + Sol tık | Button | Ekranda o tipteki tüm birimleri seçer |
| `Command` | Sağ tık | Button | **[PDF]** Bağlama göre değişir: zemine tıklanırsa hareket, düşmana tıklanırsa saldırı, kaynağa tıklanırsa (köylü seçiliyse) toplama |
| `QueueCommand` | Shift + Sağ tık | Modifier | Ara nokta (waypoint) ekler |
| `AttackMove` | A + Sol tık | Button | Yolda karşılaştığı düşmana saldırarak ilerler |
| `Stop` | S | Button | Mevcut emri iptal eder |
| `HoldPosition` | X | Button | Birim yerinde durur, sadece menziline gireni vurur |
| `ControlGroupAssign` | Ctrl + 1–9 | Button | Seçimi bir kontrol grubuna atar |
| `ControlGroupSelect` | 1–9 | Button | Grubu seçer. Çift basılırsa kamera gruba gider |
| `IdleVillager` | . (nokta) | Button | Boştaki köylüyü seçer ve kamerayı ona götürür (AoE'deki gibi) |
| `DeleteSelected` | Delete | Button | Seçili kendi binasını veya birimini yok eder (onay ister) |

### Action Map: `Placement` (bina yerleştirme modunda aktif olur)
| Action | Tuş | Tür | Not |
|---|---|---|---|
| `Confirm` | Sol tık | Button | **[PDF]** Bina geçerli alana yerleştirilir. Geçersiz alan kırmızı gösterilir |
| `ConfirmAndContinue` | Shift + Sol tık | Modifier | Yerleştirdikten sonra aynı binadan yerleştirmeye devam eder |
| `Cancel` | Sağ tık / Esc | Button | Yerleştirme modundan çıkar |

### Action Map: `UI` / Genel
| Action | Tuş | Tür | Not |
|---|---|---|---|
| `Pause` | Esc / P | Button | Duraklatma menüsünü açar. Önce aktif modu iptal eder |
| `GameSpeed` | + / − | Button | Oyun hızı 1x, 2x, 3x (opsiyonel) |
| `ToggleHealthBars` | Alt (basılı tutulduğunda) | Button | Tüm can barlarını gösterir |

### Edge Case Notları
- Mouse UI üzerindeyken (`EventSystem.IsPointerOverGameObject`) dünya inputları (Select, Command, Placement) çalışmaz.
- Placement modu açıkken `Gameplay` map'i devre dışı kalır. Modlar arası geçiş tek bir yerden yönetilir.
- Pause sırasında sadece `UI` map'i aktif kalır.
- Pencere boyutu değiştiğinde edge pan ve box select ekran koordinatlarına göre yeniden hesaplanır.
- Tüm tuşlar Input Actions asset'inde tanımlanır. İleride ayarlardan tuş değiştirme (rebinding) eklenebilir.

## Açık Sorular
1. Asset paketi incelenecek: Barracks hangi bina olacak, binaların boyutları, HP'leri ve maliyetleri ne olacak?
2. Tarla asseti (bulunacak ya da yapılacak).
3. Yükseltme maliyetleri ve maksimum seviye sayısı.
4. Takvim ve dalga değerleri test edildikten sonra kesinleştirilecek.
