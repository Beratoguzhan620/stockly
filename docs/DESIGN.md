# Tasarım Sistemi

Hedef: **modern ama sade**. Göze batmayan, okunaklı, hazır şablon gibi durmayan, jenerik hissettirmeyen bir arayüz. Bütün projeler aynı temel sistemi kullanır; sadece vurgu rengi değişir.

## Renkler
| Token | Değer | Kullanım |
|---|---|---|
| `--bg` | `#F7F6F3` | Sayfa zemini (sıcak kâğıt tonu) |
| `--surface` | `#FFFFFF` | Kart, tablo, form zemini |
| `--surface-2` | `#F0EEE9` | İkincil zemin, hover |
| `--border` | `#E2DFD8` | Tüm çizgiler, 1px |
| `--text` | `#1A1A1A` | Ana metin |
| `--text-muted` | `#6B6862` | İkincil metin, etiketler |
| `--primary` | `#1F2A37` | Ana buton, aktif menü (mürekkep) |
| `--accent` | `#5B6B2E` | **Stockly vurgu rengi (olive)** — link, seçili durum, küçük vurgular |
| `--success` | `#2F7D4F` | |
| `--warning` | `#B7791F` | |
| `--danger` | `#B42318` | |

Koyu tema (isteğe bağlı): `--bg #141413`, `--surface #1C1C1A`, `--border #2E2D2A`, `--text #EDEBE6`, `--text-muted #A3A098`.

## Tipografi
- Arayüz: **IBM Plex Sans** (400, 500, 600). Sayılar/kod: **IBM Plex Mono**.
- Ölçek: 12 / 14 / 16 (gövde) / 20 / 24 / 32. Satır yüksekliği gövdede 1.5, başlıkta 1.2.
- Başlıklar 600 ağırlık; tamamı büyük harf başlık kullanılmaz.

## Biçim
- Boşluk ölçeği 4'ün katları (4, 8, 12, 16, 24, 32, 48).
- Köşe yarıçapı: input/buton 6px, kart 10px. Hap (pill) şekli sadece küçük etiketlerde.
- Gölge minimum: `0 1px 2px rgba(0,0,0,.06)`; ayrımı çoğunlukla çizgi (border) sağlar.
- İkonlar: tek set (web: Lucide veya Bootstrap Icons; mobil: lucide-react-native), çizgi kalınlığı tutarlı, 16/20px.
- Maksimum içerik genişliği 1200px; formlar 560px.

## Yasaklar
- Mor, eflatun, indigo, neon camgöbeği; **gradyan arka planlar**, parlama (glow), cam efekti (glassmorphism/blur).
- Arayüz metinlerinde emoji; "✨", "🚀" gibi süsler.
- Her şeyi kart içine koyma, gereksiz büyük gölgeler, her bölümde ortalanmış dev başlık.
- Stok illüstrasyon yığınları; hazır admin temalarının (SB Admin vb.) varsayılan görünümü.

## Bileşen kuralları
- Butonlar: birincil (dolu `--primary`), ikincil (çerçeveli), tehlikeli (`--danger`). Sayfada tek birincil buton.
- Tablolar: başlık `--text-muted` 12px, satır hover `--surface-2`, sayısal sütunlar sağa hizalı (Plex Mono).
- Formlar: etiket üstte, yardım metni altta, hata mesajı `--danger` ile alanın hemen altında.
- Her liste için **boş durum** (kısa açıklama + tek aksiyon), **yükleniyor** (iskelet) ve **hata** durumu.
- Bildirim: sağ altta küçük toast; onay gerektiren işlemler modal ile.
- Mobil: dokunma hedefleri en az 44px; alt sekme çubuğu varsa en fazla 4 öğe.
