# Stockly — Yol Haritası

Kategori, ürün, tedarikçi ve stok hareketlerini izleyen küçük işletme stok yönetimi uygulaması.

- **Önerilen repo adı:** `stockly`
- **Teknoloji:** ASP.NET Core MVC (.NET 10), EF Core, SQL Server, Bootstrap 5 + özel SCSS, xUnit
- **Kurallar:** `CONTRIBUTING.md` · **Tasarım:** `docs/DESIGN.md`

Her aşama ayrı bir branch ve PR olarak yapılır (`stage/<no>-<kisa-ad>`). Bir aşama ancak tüm maddeleri bitince, build/testler geçince ve ekran görüntüleri eklenince tamamlanmış sayılır. Biten maddeler `[x]` yapılır.

> Kapsam: Marketly'den farklılaşması için "Stockly" — küçük işletmeler için **stok yönetimi** (müşteri tarafı yok).

## Aşama 1 — Veri modeli ve tasarım
- [x] Ad/marka "Stockly"; `docs/DESIGN.md` token'larıyla yeni layout (sol menü)
- [x] Entity'ler: Product, Category (mevcut), Supplier, StockMovement (giriş/çıkış/sayım)
- [x] EF Core migrations + seed verisi

## Aşama 2 — Özellikler
- [x] Ürün CRUD (SKU, barkod, fiyat, kritik stok seviyesi, görsel)
- [x] Stok giriş/çıkış kaydı; anlık stok hesaplama; kritik stok uyarısı
- [x] Tedarikçi CRUD; ürün-tedarikçi ilişkisi
- [x] Dashboard: toplam stok değeri, kritik ürünler, son hareketler, aylık hareket grafiği
- [x] Mükerrer kategori kontrolü korunur; "TAMAMLANDI" TODO yorumları silinir

## Aşama 3 — Cila, test, yayın
- [ ] Arama/filtre/sayfalama, Excel/CSV dışa aktarma
- [ ] Servis katmanı + xUnit testleri + GitHub Actions
- [ ] Canlı demo, ekran görüntüleri, README (TR + EN)
