# Katkı ve Çalışma Kuralları

Bu dosya Stockly üzerinde yapılan her değişiklik için geçerlidir.

## İş akışı
- `main` her zaman çalışır durumdadır; doğrudan `main`'e commit atılmaz.
- Her iş `docs/ROADMAP.md`'deki bir aşamaya karşılık gelir ve `stage/<no>-<kisa-ad>` branch'inde yapılır.
- Her aşama bir Pull Request ile biter. PR açıklaması: **Ne yapıldı**, **Nasıl test edilir**, **Ekran görüntüleri**, varsa **Kalanlar**.

## Commit mesajları
- Conventional Commits, İngilizce, emir kipinde: `feat: add course assignment submission`, `fix: ...`, `refactor: ...`, `docs: ...`, `test: ...`, `chore: ...`, `style: ...`
- Küçük ve anlamlı commit'ler; bir commit tek bir mantıksal değişiklik.
- Commit mesajlarına ve PR açıklamalarına araç/otomasyon imzası, `Co-Authored-By` satırı veya "generated with" ibaresi eklenmez.

## Kod
- Değişken, sınıf ve metot adları İngilizce; arayüz metinleri Türkçe.
- Kod içinde gereksiz yorum yok; yorum sadece "neden"i açıklar. `TODO` bırakılmaz — ya yapılır ya da ROADMAP'e yazılır.
- Gizli bilgi (şifre, API key, connection string) asla repoya girmez: .NET'te `dotnet user-secrets` / ortam değişkeni, Expo'da `.env` + `.env.example`.
- Kontroller: `dotnet build` uyarısız, `dotnet test` yeşil.

## Bitti tanımı (her aşama için)
1. Aşamadaki tüm maddeler tamam.
2. Kontroller geçiyor, CI yeşil.
3. Arayüz değiştiyse `docs/screenshots/` güncel.
4. Yeni ayar/kurulum adımı varsa README'ye eklendi.
5. Tasarım `docs/DESIGN.md`'ye uygun (yasak listesine özellikle dikkat).

## README şablonu (son aşamada, TR + EN)
`README.md` İngilizce, `README.tr.md` Türkçe; en üstte iki dosya arasında dil bağlantısı.
1. Ad + tek cümle tanım + rozetler (CI, .NET/Expo sürümü, lisans)
2. Ekran görüntüsü / GIF (en üstte, büyük)
3. Canlı demo linki + demo hesapları
4. Özellikler (madde madde)
5. Teknolojiler
6. Mimari (Mermaid şeması, katmanlar)
7. Kurulum (adım adım, kopyala-yapıştır komutlar)
8. Test çalıştırma
9. Yol haritası / bilinen eksikler
10. Lisans (MIT) ve iletişim
