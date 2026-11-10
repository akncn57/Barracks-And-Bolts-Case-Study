# Barracks & Bolts – Öncelik ve Takvim

> **Durum (10 Kasım):** P0 ve P0+ bitti, sadece README kaldı. P1 oyun döngüsü kuruldu ve oynandı. P2'den kule, bölge sistemi, köylünün inşaatı, tam Sandbox, toplanma noktası ve açlık yapıldı; P3'ten tarla ve efekt turu (vuruş karesi, ölüm ve yıkılma efekti, can barları). SetPass 10 (son özelliklerden önce ölçüldü), Windows build 8 Kasım'da denendi. **Sıradaki adım:** Test Runner sonucu, SetPass ölçümü, yeni build, denge, README en son.
> Sistemlerin kodda nasıl çalıştığı: [Systems.md](Systems.md)

> **Teslim tarihi:** 13 Kasım 2026, Cuma
> **Ana kural:** Her öncelik seviyesi bitmeden bir sonrakine geçilmez. `main` branch'i her zaman çalışan ve build alınabilen bir halde tutulur.
> Detaylı tasarım kararları için: [GDD_Decisions.md](GDD_Decisions.md)

---

## P0 – PDF Zorunlulukları (olmazsa olmaz)
Bu seviye bittiğinde PDF'teki her madde karşılanmış olur. **Hedef: 10 Kasım Salı akşamı.** Bu noktada `v1.0-pdf` tag'i ile bir build alınır.

- [x] Proje iskeleti: klasör yapısı, sahne hiyerarşisi, isimlendirme standartları, teknik doküman (+ DI scope'ları, EventBus, simülasyon döngüsü, Bootstrap/MainMenu/Gameplay sahneleri)
- [x] Grid sistemi (A* ve bina yerleştirme aynı grid'i kullanır). Bu aşamada sabit veya basit bir harita
- [x] Kamera: kaydırma ve zoom, harita sınırlarına göre kısıtlı, farklı ekran oranlarında çalışır (Cinemachine)
- [x] Veri odaklı bina ve birim tanımları (ScriptableObject: ad, görsel, boyut, HP, hasar). Görseller renk setlerinde (`TeamColorDefinition`)
- [x] **Production Menu:** sonsuz scroll view + object pooling
- [x] **Bina yerleştirme:** önizleme, geçerli alan yeşil, geçersiz alan kırmızı
- [x] Barracks (100 HP) ve House (Power Plant karşılığı, 50 HP). Ek olarak en az 1–2 bina daha (Castle, Kule; işlevleri P2'de gelir)
- [x] **Information Panel:** seçilen binanın görseli. Üretim yapan binada üretilebilir birimlerin listesi
- [x] Barracks'tan 3 asker üretimi (10 HP; 10 / 5 / 2 hasar), süre yok, sınırsız. Spawn point (doluysa en yakın boş hücre)
- [x] Seçim: sol tık ve kutu seçimi
- [x] **A\* pathfinding:** sağ tıkla hareket, binaların etrafından dolaşma, grup halinde gidince hedefin etrafına dağılma
- [x] **Saldırı:** sağ tıkla birime ya da binaya saldırma
- [x] HP 0 olunca yok olma: seçim ve panel temizlenir, grid hücreleri boşalır, saldıranlar hedefini bırakır
- [x] **Minimal Sandbox:** düşman birimi ve binası yerleştirme. Dost ateşi olmadığı için saldırı maddesinin test edilebilmesi buna bağlı
- [x] Ana menü (Sandbox ile başlama ve çıkış yeterli)
- [x] SetPass < 20 kontrolü: savaş sırasında Stats'ta 10
- [x] Resizable Windows build
- [ ] README: nasıl oynanır, kontroller, mimari özeti, PDF maddelerinin nasıl karşılandığı

## P0+ – Oyun İçi Test İstekleri (8 Kasım)
Oyunu deneyince çıkan kullanılabilirlik istekleri. Sandbox'ta da olmalı, build'den önce yapılacak.

- [x] **Uçan yazı (generic):** Dünyada beliren, yukarı süzülüp solan yazı. İlk kullanım: geçersiz alana bina koymaya çalışınca uyarı
- [x] **Üzerine gelince bina adı:** Mouse binanın üstündeyken ekranda adı yazar, binada hover efekti olur
- [x] **Information Panel boyutu:** Min-max arasında içeriğe göre boyutlanır. İçerik sığmazsa scroll olur, sağda sürüklenebilir scrollbar görünür. Üste anchor'landığı için geniş ekran taşması da çözüldü
- [x] **Production Menu tooltip:** Hücrenin üstünde durunca bina bilgisi açılır (boyut, HP, ürettikleri). Maliyet satırı P1'de maliyetler gelince eklenir
- [x] **Production Menu açılır kapanır:** Kapalı başlar, butona basınca soldan DOTween ile kayarak gelir. Bir şey yerleştirilirken kenara çekilir, bitince geri gelir. Haritayı kapatmasın diye
- [x] **Esc ve panel kapatma:** Esc her basışta en üstteki katmanı kapatır: yerleştirme, seçim, Production Menu. Information Panel'in köşesinde kapatma butonu var
- [x] **Kaynak barı:** Ekranın üstünde odun, taş, yemek, altın ve nüfus. Sandbox'ta hepsi ∞. Üstteki UI barın altına indi
- [x] **Harita:** 64x64 ada: su, köpük, düz zemin, üç plato, merdivenler. Askerler platoya sadece merdivenden çıkar; plato kenarına kadar bina konur. Ağaç ve kaynaklar P1'de
- [x] **Birim kartı:** Asker seçilince alt ortada küçük kart çıkar, mouse'u yakalamaz. Büyük panel sadece binada açılır

## P1 – Oyun Döngüsü (bu seviyeyle bir "oyun" ortaya çıkar)
**Hedef: 11 Kasım Çarşamba akşamı.**

- [x] **Haritada rastgele kaynak dağılımı:** seed'li; orman, taş ocağı, altın madeni, koyun sürüsü
- [x] **Normal modun ilk dilimi:** menüde Play; Castle, 3 köylü ve başlangıç kaynağıyla başlangıç; sayaçlar; bina ve asker maliyetleri, yetmezse uyarı; köylüyle toplama, Castle'a taşıma, kaynağın tükenmesi
- [x] **Oyun döngüsü (aşağıdaki maddeler):** kuruldu ve oynandı; dengesi henüz ayarlanmadı
- [x] **Durum ikonları:** boştaki köylüde "Zz" baloncuğu, düşman fark eden askerde kırmızı "!", yük taşıyan köylüde kaynağın küçük ikonu
- [x] Koyunların çoğalması: her sabah 3 yeni koyun, en fazla 30
- [x] **İlk oyun notları:** köylü kaynağın dibinde çalışıyor, düşman yaklaşınca Castle'a sığınıyor, boştayken ikon, koyunlar geziyor ve tek kesimde ölüyor, isim etiketi hedefin üstünde
- [x] Köylü: Castle'dan üretim, sadece Normal modda
- [ ] Bina ve birim maliyetlerinin Production Menu hücresinde görünmesi (tooltip'te var)
- [x] Nüfus sistemi: Castle ve House kapasite verir, barda `kullanılan/kapasite`
- [x] Takvim: gündüz/gece döngüsü, gece kararması, saldırı günleri, baskın sırasında zamanın durması
- [x] Düşman AI: Castle'a yürür, yolundakine saldırır, kapalı yolda binayı yıkar. Boştaki askerler de yakındaki düşmana saldırır
- [x] Dalga sistemi ve baskın bildirimleri (ses ve mini harita yok; yön yazıyla söyleniyor, dalga ekran dışındayken kenarda tıklanabilir ok)
- [x] Kazanma ve kaybetme koşulları ve oyun sonu ekranı
- [ ] Oyun döngüsünün dengesi (süreler, maliyetler, dalga büyüklükleri)
- [ ] Test Runner sonucu (494 test) ve son SetPass ölçümü

## P2 – Derinlik (zaman kaldıkça, aşağıdaki sırayla)
**Hedef: 12 Kasım Perşembe.**

1. [x] Kule: üzerindeki okçu otomatik ateş eder (okçu sayısı ve gücü yükseltme sistemiyle gelecek)
2. [ ] Bina yükseltme sistemi (HP, nüfus, kule gücü, asker seviyesi, depo kapasitesi)
3. [ ] Seed tabanlı rastgele harita üretimi ve yol doğrulaması (kaynaklar zaten seed'le dağılıyor; zemin sabit)
4. [ ] Mini harita
5. [ ] Kilise ve Monk (kendiliğinden iyileştirme)
6. [x] Köylünün inşa etmesi: Normal modda bina gri inşaat alanı olarak konur, köylüler bitirir, üstünde ilerleme çubuğu var. Kaynakta ve inşaatta araç imleci. (Tamir ve iptal sonra)
7. [ ] Depo binaları ve kapasite sistemi
8. [x] Açlık: her sabah birim başına yemek, yetmezse yavaş HP kaybı; değerler zorluğa göre (menüde zorluk seçimi henüz yok)
9. [x] Bölge sistemi ve Karakol (yarıçap yükseltmesi, yükseltme sistemiyle gelecek). Seçili binada menzil ve bölge halkası
10. [ ] Köylülerin House'a da sığınması ve bina başına sığınma kapasitesi (Castle'a sığınma yapıldı)
11. [x] Toplanma noktası (rally point): üretim binası seçiliyken sağ tık, yeni birimler oraya sağ tık emriyle gider
12. [x] Tam Creative Sandbox: bina, birim ve kaynak sekmeleri, Delete ile silme, zaman kontrolü, Enemy AI ve Debug (grid + A* yolu) düğmeleri, elle baskın, haritayı temizleme, +100 birimlik test savaşı ve istatistik paneli (FPS, A* süresi, SetPass)

## P3 – Cila (yetişirse; yetişmezse README'de "Gelecek Planları" olarak listelenir)
- [ ] Kayıt sistemi (manuel kayıt, birden fazla slot)
- [ ] Zorluk seviyeleri
- [ ] Sonsuz mod
- [x] Ayarlar menüsü (pencere modu, VSync, arayüz boyutu, kamera hızı, kenar kaydırma, FPS paneli; ses ayarları ses gelince)
- [x] Arayüz cilası: konturlu ve gölgeli yazı malzemeleri, buton hover/basma animasyonu, kaynak sayısı punch, ana menü arka planı ve giriş animasyonları, FPS paneli sağ altta
- [ ] Ses ve müzik
- [ ] Tutorial
- [x] Kısayollar: kontrol grupları, Attack Move (F), Stop (X), Hold Position (H), boştaki köylü (.), Castle'a kamera (Home); F1 Controls sayfası, birim kartı üstünde komut butonları, tooltip'lerde tuşlar
- [x] Tarla: 3x3, 40 odun, bir köylü, bitmez, 6 saniyede 5 yemek
- [ ] Düşman yerleşkeleri
- [x] Efekt turu: vuruş animasyon karesinde, ok varınca hasar, ölüm ve yıkılma efekti, can barları (hasarlı ya da seçiliyken)
- [ ] Ana menüde zorluk seçimi (açlık hazır, dalgalar ve başlangıç kaynağı da bağlanacak)

## Teslim Öncesi
- [ ] Sahne ayırıcıları: `----- CAMERAS-----` adı ve `----- WORLD -----`'ın yeri
- [ ] Company Name (`DefaultCompany`)
- [ ] `Assets/Editor` altındaki tek seferlik aracın silinmesi
- [ ] Yeni Windows build, GitHub Release'e zip (`_BurstDebugInformation_DoNotShip` hariç), `v1.0-pdf` tag'i
- [ ] README (en son)

---

## Günlük Plan
| Gün | Tarih | Hedef |
|---|---|---|
| Cumartesi | 7 Kasım | GDD'nin bitirilmesi, asset incelemesi, teknik doküman, proje iskeleti, grid |
| Pazar | 8 Kasım | Kamera, veri tanımları, Production Menu (sonsuz scroll + pooling), bina yerleştirme, Information Panel |
| Pazartesi | 9 Kasım | Seçim, A\*, hareket, spawn, saldırı, HP ve yok olma, minimal Sandbox |
| Salı | 10 Kasım | P0 cilası: edge case'ler, SetPass, çözünürlük, README. **`v1.0-pdf` build'i** |
| Çarşamba | 11 Kasım | P1: kaynaklar, köylüler, nüfus, takvim, düşman AI, dalgalar, kazanma ve kaybetme |
| Perşembe | 12 Kasım | P2 maddeleri sırayla. **Akşam feature freeze** |
| Cuma | 13 Kasım | Sadece bug fix, son build, README güncellemesi, test, **e-posta ile teslim** |

## Risk Kuralları
- P0 Salı akşamına kadar bitmezse P1'e geçilmez. Eksik kalan P0 maddesi tamamlanır.
- Yeni bir sistem yarım kalacaksa hiç başlanmaz. Yarım kalan sistem ya bir feature flag ile kapatılır ya da `main` branch'ine merge edilmez.
- Her gün sonunda çalışan bir Windows build alınır.
- Perşembe akşamından sonra yeni özellik eklenmez.
- Teslim edilmeyen özellikler README'de "Gelecek Planları" bölümünde listelenir. Tasarımın derinliği bu şekilde de gösterilebilir.
