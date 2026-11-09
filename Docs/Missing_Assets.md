# Barracks & Bolts – Eksik Asset Listesi

> Öncelikler [Roadmap.md](Roadmap.md) ile aynıdır.
> **Çözüm** sütunu: 👤 Kullanıcı ekleyecek · 🛠 Mevcut assetten üretilecek · 💻 Kodla veya basit bir sprite ile çözülür · 🔍 Bulunacak

## Mevcut UI Assetlerinin Eşleştirmesi (eksik olmayanlar)
| Asset | Kullanım |
|---|---|
| `Texture_UI_Icons_Hammer` | İnşa / tamir butonu |
| `Texture_UI_Icons_Wood`, `_Gold`, `_Meat` | Kaynak ikonları (odun, altın, yemek) |
| `Texture_UI_Icons_Sword`, `_Shield` | Saldırı / savunma (bilgi panelindeki istatistikler) |
| `Texture_UI_Icons_Arrow_Green`, `_Arrow_Orange` | İleri / geri butonları |
| `Texture_UI_Icons_Cancel` | İptal, kapat, geçersiz |
| `Texture_UI_Icons_Settings`, `_Info`, `_Music` | Ayarlar, bilgi, ses ayarı |
| `Texture_UI_Avatars_01–05` | Birim portreleri: Şövalye, Mızrakçı, Okçu, Rahip, Köylü (ilk üçü `Data_Unit_*` asset'lerine atandı) |
| `Texture_Cursors_Default`, `_Pointer`, `_Forbidden` | Varsayılan imleç, tıklanabilir (el), yasak |
| `Texture_Cursors_Selection` (köşe çerçevesi) | Seçim / hedef göstergesi |
| `Texture_UI_Tools_Hammer`, `_Axe`, `_Sword`, `_Pickaxe` | İmleç durumları: inşa/tamir, odun, saldırı, maden |
| `Texture_UI_Bars_Big_*`, `_Small_*` | Can barı, ilerleme barı |
| `Texture_UI_Ribbons_Small_<Renk>_<Fork/Point>`, `_Banners_*`, `_Papers_*`, `_WoodTable_*` | Panel arka planları, başlıklar, slotlar |

---

## P0 – PDF için gerekli
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 1 | ~~İkinci renk birimler~~ | ✅ Sarı set eklendi (Warrior, Lancer, Archer, Monk, Pawn) | – |
| 2 | ~~İkinci renk binalar~~ | ✅ Sarı set eklendi (Castle, Barracks, House, Monastery, Tower, Archery) | – |
| 2a | ~~Sarı `Pawn_Interact_Axe`~~ | ✅ Eklendi | – |
| 3 | Yerleştirme önizlemesi (yeşil/kırmızı hücre) | PDF: geçersiz alan kırmızı | 💻 Beyaz kare sprite + renk |
| 4 | ~~Seçim göstergesi (birimin altında halka/elips)~~ | ✅ Üretildi: `Texture_Markers_SelectionRing` | – |
| 5 | ~~Kutu seçimi dikdörtgeni~~ | ✅ Üretildi: `Texture_Markers_SelectionBox` (dünyada Sliced SpriteRenderer) | – |
| 6 | ~~Hareket/saldırı emri işareti~~ | ✅ `Texture_Markers_SelectionRing`: hareket yeşil, saldırı kırmızı | – |
| 7 | ~~Ölüm ve yıkılma efekti~~ | ✅ Paketin Particle FX klasöründen alındı: `Texture_VFX_Dust_01/02`, `_Explosion_01/02`, `_Fire_01/02/03`, `_Water_Splash` (kare kare kesildi) | – |

## P1 – Oyun döngüsü
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 8 | ~~Taş ikonu~~ | ✅ Paketteki kayalardan (gölgesiz, dış hatlı): `Texture_UI_Icons_Stone` | – |
| 9 | ~~Nüfus ikonu~~ | ✅ Gri köylü avatarından: `Texture_UI_Icons_Population` | – |
| 10 | Gün / gece / takvim ikonu | Takvim UI'ı | 🔍 / 🛠 |
| 11 | Baskın uyarı ikonu ve ekran kenarı yön oku | Baskın bildirimi | 🛠 Kılıç ikonu + ok ikonu |
| 12 | Koyun kesme imleci | Yemek toplama | 🛠 Et ikonu veya kılıç aracı |
| 13 | Kaynak taşıyan köylü (odun/altın/et taşırken) | Görsel geri bildirim (opsiyonel) | 🔍 Tiny Swords'ta var. Alternatif: köylünün üstünde küçük kaynak ikonu |
| 14 | Kaynak toplama ve hasar sayıları (ör. "+5", "-10") | Geri bildirim | 💻 TextMeshPro (`MedievalSharp` fontu var) |
| 15 | Ana menü arka planı | Ana menü | 🛠 Oyun sahnesinden bir görüntü veya Banner/Paper ile |
| 16 | Kazandın / Kaybettin ekranı görseli | Oyun sonu | 🛠 Banner + Ribbon |

## P2 – Derinlik
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 17 | ~~Depo binaları (4 adet)~~ | ✅ Üretildi: Archery sundurması (hedefler silindi) + avluda tek kaynak yığını. `Texture_Buildings_<Renk>_Storage_Wood/Food/Gold/Stone` | – |
| 18 | ~~Karakol~~ | ✅ Tower + takım renginde bayrak: `Texture_Buildings_<Renk>_Outpost` | – |
| 19 | İnşa halindeki bina (iskele) | Köylünün inşa etmesi | 🔍 Tiny Swords'ta "Construction" varyantları var. Alternatif: 💻 yarı saydam bina ve alttan dolan maske |
| 20 | ~~Hasarlı bina efekti~~ | ✅ `Texture_VFX_Fire_*` | – |
| 21 | Rütbe / seviye simgesi (asker ve bina seviyesi) | Yükseltme sistemi | 🛠 / 💻 Küçük yıldız veya çizgiler |
| 22 | ~~Bölge sınırı görseli~~ | ✅ Kodla: haritanın üstünde tek doku, `TerritoryOverlayView` | – |
| 23 | Mini harita çerçevesi | Mini harita | 🛠 `Papers_*` / `WoodTable_*` 9-slice |
| 24 | ~~Kuledeki okçu~~ | ✅ `Archer` kareleri kulenin platformunda, takım rengine göre | – |
| 25 | ~~Toplanma noktası bayrağı~~ | ✅ Kurdeleden üretildi: `Textures/Markers/<Renk>/Texture_Markers_<Renk>_RallyFlag` (atlas'a eklendi) | – |
| – | ~~Birim durum ikonları~~ | ✅ Birimlerin paletiyle çizildi: `Textures/Markers/Texture_Markers_Status_Idle`, `_EnemySpotted` (klasörle atlas'a giriyor) | – |

## P3 – Cila
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 26 | ~~Tarla~~ | ✅ Paketin çalı sprite'larından fideler, buğday sırası, Archery çitiyle çevrili: `Textures/Buildings/Neutral/Texture_Buildings_Neutral_Farm` | – |
| 27 | **Müzik:** ana menü, gündüz, baskın | Atmosfer | 🔍 (CC0: OpenGameArt, Pixabay) |
| 28 | **SFX:** UI tıklama, bina yerleştirme, inşa, odun kesme, maden kazma, kılıç vuruşu, ok atışı, ölüm, bina yıkılması, borozan, iyileştirme, kazanma, kaybetme, hata | Geri bildirim | 🔍 (Kenney CC0 paketleri) |
| 29 | Tutorial göstergeleri (vurgulama çerçevesi, ok) | Tutorial | 🛠 Seçim imleci + ok ikonu |
| 30 | Kayıt slotu ve ayarlar paneli görselleri | Kayıt / ayarlar | 🛠 `Banners_Slots`, `Papers_*` |

---

## En Önemli Eksikler (önce bunlar)
P0 ve P1 için kritik asset eksiği kalmadı. Açık kalanlar: gün/gece ve baskın ikonları (P1, mevcut ikonlardan türetilecek), inşa görseli (P2, kodla), müzik ve ses efektleri (P3).

## Projeye Kopyalanmamış Paket Dosyası
- `Shadow.png` (Tiny Swords, Terrain/Tileset): Yüksek zeminin altındaki gölge. Haritada gölge kullanılmamasına karar verildi, gerekli değil.

## Lisans Notu
Dışarıdan eklenen her asset için kaynak ve lisans bilgisi README'ye "Credits" bölümüne yazılacak.
