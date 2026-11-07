# Barracks & Bolts – Eksik Asset Listesi

> Öncelikler [Roadmap.md](Roadmap.md) ile aynıdır.
> **Çözüm** sütunu: 👤 Kullanıcı ekleyecek · 🛠 Mevcut assetten üretilecek · 💻 Kodla veya basit bir sprite ile çözülür · 🔍 Bulunacak

## Mevcut UI Assetlerinin Eşleştirmesi (eksik olmayanlar)
| Asset | Kullanım |
|---|---|
| `Icon_01` çekiç | İnşa / tamir butonu |
| `Icon_02` odun, `Icon_03` altın, `Icon_04` et | Kaynak ikonları (odun, altın, yemek) |
| `Icon_05` kılıç, `Icon_06` kalkan | Saldırı / savunma (bilgi panelindeki istatistikler) |
| `Icon_07` / `Icon_08` oklar | İleri / geri butonları |
| `Icon_09` çarpı | İptal, kapat, geçersiz |
| `Icon_10` dişli, `Icon_11` bilgi, `Icon_12` müzik | Ayarlar, bilgi, ses ayarı |
| `Avatars_06–10` | Birim portreleri: Şövalye, Mızrakçı, Okçu, Rahip, Köylü (eşleşme Unity'de kontrol edilecek) |
| `Cursor_01–03` | Varsayılan imleç, tıklanabilir (el), yasak |
| `Cursor_04` köşe çerçevesi | Seçim / hedef göstergesi |
| `Tool_01–04` çekiç, balta, kılıç, kazma | İmleç durumları: inşa/tamir, odun, saldırı, maden |
| `BigBar` / `SmallBar` | Can barı, ilerleme barı |
| `SmallRibbons`, `Banner`, `Banner_Slots`, `Papers`, `WoodTable` | Panel arka planları, başlıklar, slotlar |

---

## P0 – PDF için gerekli
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 1 | ~~İkinci renk birimler~~ | ✅ Mavi set eklendi (Warrior, Lancer, Archer, Monk) | – |
| 2 | ~~İkinci renk binalar~~ | ✅ Mavi set eklendi (Barracks, House, Monastery, Tower, Archery) | – |
| 2a | **Mavi Castle ve mavi Pawn** | Oyuncu maviyi seçerse ana üs ve köylü gerekir | 👤 |
| 3 | Yerleştirme önizlemesi (yeşil/kırmızı hücre) | PDF: geçersiz alan kırmızı | 💻 Beyaz kare sprite + renk |
| 4 | Seçim göstergesi (birimin altında halka/elips) | Neyin seçili olduğunu göstermek | 💻 Basit elips sprite. Alternatif: `Cursor_04` köşeleri |
| 5 | Kutu seçimi dikdörtgeni | Sürükleyerek seçim | 💻 UI Image (9-slice) |
| 6 | Hareket/saldırı emri işareti (tıklanan yerde kısa efekt) | Sağ tık geri bildirimi | 💻 / 🔍 |
| 7 | Ölüm efekti (birim) ve yıkılma efekti (bina) | HP 0 olduğunda yok olma | 🔍 Tiny Swords "Effects" klasöründe Dead, Explosion, Fire var. Alternatif: DOTween ile fade + sallanma |

## P1 – Oyun döngüsü
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 8 | **Taş ikonu** (UI) | Kaynak barı, maliyetler | 🛠 `Rock1` küçültülerek veya 🔍 |
| 9 | **Nüfus ikonu** | Nüfus göstergesi | 🛠 `Avatars_10` küçük hali veya 🔍 |
| 10 | Gün / gece / takvim ikonu | Takvim UI'ı | 🔍 / 🛠 |
| 11 | Baskın uyarı ikonu ve ekran kenarı yön oku | Baskın bildirimi | 🛠 `Icon_05` kılıç + `Icon_07` ok |
| 12 | Koyun kesme imleci | Yemek toplama | 🛠 `Icon_04` et veya `Tool_03` kılıç |
| 13 | Kaynak taşıyan köylü (odun/altın/et taşırken) | Görsel geri bildirim (opsiyonel) | 🔍 Tiny Swords'ta var. Alternatif: köylünün üstünde küçük kaynak ikonu |
| 14 | Kaynak toplama ve hasar sayıları (ör. "+5", "-10") | Geri bildirim | 💻 TextMeshPro (`MedievalSharp` fontu var) |
| 15 | Ana menü arka planı | Ana menü | 🛠 Oyun sahnesinden bir görüntü veya Banner/Paper ile |
| 16 | Kazandın / Kaybettin ekranı görseli | Oyun sonu | 🛠 Banner + Ribbon |

## P2 – Derinlik
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 17 | **Depo binaları (4 adet):** Oduncu Deposu, Taş Deposu, Ambar, Hazine | Depo sistemi | 🛠 House/Barracks + kaynak yığınları (`Wood Resource`, `Gold_Resource`, `Meat Resource`, `Rock`) ile kompozisyon ya da renk değişimi |
| 18 | **Karakol** | Bölge sistemi | 🛠 Kullanılmayan `Archery` veya `Tower` + bayrak |
| 19 | İnşa halindeki bina (iskele) | Köylünün inşa etmesi | 🔍 Tiny Swords'ta "Construction" varyantları var. Alternatif: 💻 yarı saydam bina ve alttan dolan maske |
| 20 | Hasarlı bina efekti (duman/ateş) | HP düşük geri bildirimi | 🔍 Tiny Swords Effects/Fire |
| 21 | Rütbe / seviye simgesi (asker ve bina seviyesi) | Yükseltme sistemi | 🛠 / 💻 Küçük yıldız veya çizgiler |
| 22 | Bölge sınırı görseli | Bölge sistemi | 💻 Tilemap overlay veya shader |
| 23 | Mini harita çerçevesi | Mini harita | 🛠 `Papers` / `WoodTable` 9-slice |
| 24 | Kuledeki okçu | Kule savunması | 🛠 `Archer` sprite'ı kulenin üstünde |
| 25 | Toplanma noktası bayrağı | Rally point | 🔍 / 🛠 |

## P3 – Cila
| # | Eksik | Neden Gerekli | Çözüm |
|---|---|---|---|
| 26 | **Tarla** | Yemek üretimi | 🔍 |
| 27 | **Müzik:** ana menü, gündüz, baskın | Atmosfer | 🔍 (CC0: OpenGameArt, Pixabay) |
| 28 | **SFX:** UI tıklama, bina yerleştirme, inşa, odun kesme, maden kazma, kılıç vuruşu, ok atışı, ölüm, bina yıkılması, borozan, iyileştirme, kazanma, kaybetme, hata | Geri bildirim | 🔍 (Kenney CC0 paketleri) |
| 29 | Tutorial göstergeleri (vurgulama çerçevesi, ok) | Tutorial | 🛠 `Cursor_04` + `Icon_07` |
| 30 | Kayıt slotu ve ayarlar paneli görselleri | Kayıt / ayarlar | 🛠 `Banner_Slots`, `Papers` |

---

## En Önemli Eksikler (önce bunlar)
1. **Mavi Castle ve mavi Pawn** (renk seçiminin tam çalışması için) 👤
2. **Ölüm ve yıkılma efektleri** (Tiny Swords Effects klasörü) 🔍
3. **Taş ve nüfus ikonları** (kaynak barı) 🛠 / 🔍
4. **Depo binaları ve Karakol** (P2, mevcut assetlerden üretilecek) 🛠

## Lisans Notu
Dışarıdan eklenen her asset için kaynak ve lisans bilgisi README'ye "Credits" bölümüne yazılacak.
