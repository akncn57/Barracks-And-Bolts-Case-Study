# Barracks & Bolts – Öncelik ve Takvim

> **Durum (8 Kasım):** Altyapı, grid, kamera, A* (algoritma), tüm P0/P1 assetleri, veri tanımları, bina yerleştirme, Production Menu, binaya sol tıkla seçim, Information Panel, birimler, Barracks'tan asker üretimi ve birim seçimi (sol tık ve kutu) hazır ve oyunda denendi. Sağ tıkla hareket kodu hazır. **Sıradaki adım:** hareketin sahne kurulumu → saldırı → yok olma → minimal Sandbox.

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
- [ ] **A\* pathfinding:** sağ tıkla hareket, binaların etrafından dolaşma, grup halinde gidince hedefin etrafına dağılma *(kod ve testler hazır; sahne kurulumu kaldı)*
- [ ] **Saldırı:** sağ tıkla birime ya da binaya saldırma
- [ ] HP 0 olunca yok olma: seçim ve panel temizlenir, grid hücreleri boşalır, saldıranlar hedefini bırakır
- [ ] **Minimal Sandbox:** düşman birimi ve binası yerleştirme. Dost ateşi olmadığı için saldırı maddesinin test edilebilmesi buna bağlı
- [x] Ana menü (Sandbox ile başlama ve çıkış yeterli)
- [ ] SetPass < 20 kontrolü (Sprite Atlas, Frame Debugger ile ölçüm)
- [ ] Resizable Windows build
- [ ] README: nasıl oynanır, kontroller, mimari özeti, PDF maddelerinin nasıl karşılandığı

## P1 – Oyun Döngüsü (bu seviyeyle bir "oyun" ortaya çıkar)
**Hedef: 11 Kasım Çarşamba akşamı.**

- [ ] Kaynaklar (odun, taş, yemek, altın) ve haritadaki kaynak noktaları (tükenme, koyunların çoğalması)
- [ ] Köylü: Castle'dan üretim, kaynak toplama, Castle'a taşıma
- [ ] Bina ve birim maliyetleri
- [ ] Nüfus sistemi (House ile kapasite artışı)
- [ ] Takvim: gündüz/gece döngüsü, saldırı günleri, baskın sırasında zamanın durması
- [ ] Düşman AI: Castle'a yürür, yolundakine saldırır, kapalı yolda engel yıkar
- [ ] Dalga sistemi ve baskın bildirimleri
- [ ] Kazanma ve kaybetme koşulları ve oyun sonu ekranı
- [ ] Ana menüde Normal / Sandbox mod seçimi

## P2 – Derinlik (zaman kaldıkça, aşağıdaki sırayla)
**Hedef: 12 Kasım Perşembe.**

1. [ ] Kule: üzerindeki okçular otomatik ateş eder
2. [ ] Bina yükseltme sistemi (HP, nüfus, kule gücü, asker seviyesi, depo kapasitesi)
3. [ ] Seed tabanlı rastgele harita üretimi ve yol doğrulaması
4. [ ] Mini harita
5. [ ] Kilise ve Monk (kendiliğinden iyileştirme)
6. [ ] Köylünün inşa etmesi ve manuel tamir
7. [ ] Depo binaları ve kapasite sistemi
8. [ ] Açlık
9. [ ] Bölge sistemi ve Karakol
10. [ ] Köylülerin kaçıp saklanması
11. [ ] Toplanma noktası (rally point)
12. [ ] Tam Creative Sandbox (her şeyi yerleştirme, silme aracı, zaman kontrolü)

## P3 – Cila (yetişirse; yetişmezse README'de "Gelecek Planları" olarak listelenir)
- [ ] Kayıt sistemi (manuel kayıt, birden fazla slot)
- [ ] Zorluk seviyeleri
- [ ] Sonsuz mod
- [ ] Ayarlar menüsü
- [ ] Ses ve müzik
- [ ] Tutorial
- [ ] Kontrol grupları, AttackMove, HoldPosition, oyun hızı
- [ ] Tarla
- [ ] Düşman yerleşkeleri

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
