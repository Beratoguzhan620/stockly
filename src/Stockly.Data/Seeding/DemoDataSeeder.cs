using Microsoft.EntityFrameworkCore;
using Stockly.Data.Entities;

namespace Stockly.Data.Seeding;

/// <summary>
/// Fills an empty database with a small stationery shop: six categories, five suppliers, thirty products
/// and about six months of stock movements ending today, so the dashboard and history have real-looking data.
/// The data is generated from a fixed seed, so every install looks the same apart from the dates.
/// </summary>
public static class DemoDataSeeder
{
    public static async Task SeedAsync(AppDbContext context, TimeProvider timeProvider)
    {
        if (await context.Products.AnyAsync() || await context.Categories.AnyAsync())
            return;

        var today = timeProvider.GetUtcNow().UtcDateTime.Date;

        // Opening stock lands two weeks before the six months the dashboard chart shows,
        // so the chart starts with ordinary trading instead of one big delivery.
        var start = new DateTime(today.Year, today.Month, 1, 0, 0, 0, DateTimeKind.Utc).AddMonths(-5).AddDays(-14);

        var categories = CategoryNames.ToDictionary(c => c.Key, c => new Category { Name = c.Value, CreatedAt = start.AddHours(8) });
        var suppliers = Suppliers.Select(s => new Supplier
        {
            Name = s.Name,
            ContactName = s.Contact,
            Phone = s.Phone,
            Email = s.Email,
            Address = s.Address,
            IsActive = s.Active,
            CreatedAt = start.AddHours(8),
        }).ToList();

        var random = new Random(2026);
        var products = new List<Product>();

        foreach (var (spec, index) in Products.Select((p, i) => (p, i)))
        {
            var product = new Product
            {
                Sku = $"{spec.Category}-{index + 1:000}",
                Barcode = Ean13("869" + (4410000 + index * 37).ToString("0000000") + "00"),
                Name = spec.Name,
                Unit = spec.Unit,
                PurchasePrice = spec.Purchase,
                SalePrice = spec.Sale,
                CriticalLevel = spec.Critical,
                Category = categories[spec.Category],
                Supplier = suppliers[spec.Supplier],
                CreatedAt = start.AddHours(9),
            };

            AddHistory(product, spec, start, today, random);
            products.Add(product);
        }

        context.Categories.AddRange(categories.Values);
        context.Suppliers.AddRange(suppliers);
        context.Products.AddRange(products);
        await context.SaveChangesAsync();
    }

    private static void AddHistory(Product product, ProductSpec spec, DateTime start, DateTime today, Random random)
    {
        var stock = 0;
        DateTime? restockDue = null;

        void Move(MovementType type, int quantity, DateTime at, string note, decimal? unitCost = null)
        {
            stock += quantity;
            product.Movements.Add(new StockMovement
            {
                Type = type,
                Quantity = quantity,
                BalanceAfter = stock,
                UnitCost = unitCost,
                Note = note,
                OccurredAt = at,
                CreatedAt = at,
            });
        }

        var reorderQuantity = spec.Critical * 3 + (int)Math.Ceiling(spec.WeeklyOut * 4);
        Move(MovementType.In, spec.Critical * 2 + reorderQuantity, start.AddHours(10), "Açılış stoğu", spec.Purchase);

        for (var day = start.AddDays(1); day <= today; day = day.AddDays(1))
        {
            if (day.DayOfWeek == DayOfWeek.Sunday)
                continue;

            var stopRestocking = spec.RunsLow && (today - day).TotalDays < 45;

            if (restockDue is not null && day >= restockDue && !stopRestocking)
            {
                var cost = Math.Round(spec.Purchase * (decimal)(0.96 + random.NextDouble() * 0.08), 2);
                Move(MovementType.In, reorderQuantity, day.AddHours(10), "Tedarikçi teslimatı", cost);
                restockDue = null;
            }

            var expected = spec.WeeklyOut / 6;
            var sold = (int)Math.Floor(expected + random.NextDouble() * (expected + 0.6));
            sold = Math.Min(sold, stock);
            if (sold > 0)
                Move(MovementType.Out, -sold, day.AddHours(17).AddMinutes(random.Next(0, 50)), "Günlük satış");

            if (stock <= spec.Critical && restockDue is null)
                restockDue = day.AddDays(random.Next(2, 5));

            // The month-end count finds the odd missing item.
            if (day.Day == 28 && random.NextDouble() < 0.25 && stock > 2)
                Move(MovementType.Count, -1, day.AddHours(19), "Ay sonu sayımı");
        }

        product.StockQuantity = stock;
    }

    /// <summary>Adds the EAN-13 check digit to the first twelve digits.</summary>
    private static string Ean13(string twelveOrMore)
    {
        var digits = twelveOrMore[..12];
        var sum = digits.Select((c, i) => (c - '0') * (i % 2 == 0 ? 1 : 3)).Sum();
        return digits + ((10 - sum % 10) % 10);
    }

    private static readonly Dictionary<string, string> CategoryNames = new()
    {
        ["KAG"] = "Kâğıt ürünleri",
        ["YAZ"] = "Yazım gereçleri",
        ["DOS"] = "Dosyalama",
        ["OFS"] = "Ofis gereçleri",
        ["SAN"] = "Sanat malzemeleri",
        ["TEK"] = "Teknoloji aksesuarları",
    };

    private sealed record SupplierSpec(string Name, string Contact, string Phone, string Email, string Address, bool Active = true);

    private static readonly SupplierSpec[] Suppliers =
    [
        new("Marmara Kırtasiye Toptan", "Hakan Yılmaz", "0212 555 41 20", "siparis@marmarakirtasiye.example", "Topkapı Mah. Matbaacılar Sit. No: 14, Zeytinburnu / İstanbul"),
        new("Ege Kâğıt ve Defter", "Selin Aydın", "0232 555 18 07", "satis@egekagit.example", "Atatürk OSB 10003 Sok. No: 5, Çiğli / İzmir"),
        new("Renk Sanat Malzemeleri", "Murat Koç", "0312 555 72 33", "info@renksanat.example", "Kazım Karabekir Cad. No: 41, Altındağ / Ankara"),
        new("Boyut Ofis Ürünleri", "Derya Şen", "0224 555 90 16", "bayi@boyutofis.example", "Nilüfer OSB Ahmet Taner Kışlalı Cad. No: 9, Nilüfer / Bursa"),
        new("Kuzey Ambalaj", "Okan Demir", "0216 555 03 44", "kuzey@kuzeyambalaj.example", "Esenşehir Mah. Kuyumcular Cad. No: 22, Ümraniye / İstanbul", Active: false),
    ];

    private sealed record ProductSpec(string Category, string Name, ProductUnit Unit, decimal Purchase, decimal Sale, int Critical, int Supplier, double WeeklyOut, bool RunsLow = false);

    private static readonly ProductSpec[] Products =
    [
        new("KAG", "A4 fotokopi kâğıdı 80 g, 500 yaprak", ProductUnit.Ream, 115m, 165m, 20, 0, 12),
        new("KAG", "A3 fotokopi kâğıdı 80 g, 500 yaprak", ProductUnit.Ream, 240m, 330m, 5, 0, 2),
        new("KAG", "Spiralli defter A4 kareli, 100 yaprak", ProductUnit.Piece, 38m, 65m, 15, 1, 10),
        new("KAG", "Spiralli defter A5 çizgili, 80 yaprak", ProductUnit.Piece, 26m, 45m, 15, 1, 9),
        new("KAG", "Yapışkanlı not kâğıdı 76x76 sarı", ProductUnit.Pack, 18m, 35m, 10, 2, 6, RunsLow: true),
        new("KAG", "Renkli fotokopi kâğıdı, 100 yaprak", ProductUnit.Pack, 55m, 90m, 6, 0, 2),
        new("YAZ", "Tükenmez kalem mavi, 50'li kutu", ProductUnit.Box, 210m, 320m, 4, 0, 1.5),
        new("YAZ", "Jel kalem 0.7 siyah", ProductUnit.Piece, 14m, 28m, 30, 2, 20, RunsLow: true),
        new("YAZ", "Kurşun kalem HB, 12'li", ProductUnit.Box, 42m, 70m, 8, 1, 4),
        new("YAZ", "Fosforlu kalem, 4 renk", ProductUnit.Set, 48m, 85m, 8, 2, 5),
        new("YAZ", "Beyaz tahta kalemi, 4'lü", ProductUnit.Set, 60m, 99m, 6, 2, 3),
        new("YAZ", "Silgi beyaz", ProductUnit.Piece, 6m, 12m, 25, 1, 15, RunsLow: true),
        new("DOS", "Klasör geniş mavi", ProductUnit.Piece, 34m, 58m, 12, 3, 6),
        new("DOS", "Şeffaf poşet dosya, 100'lü", ProductUnit.Pack, 95m, 150m, 5, 3, 2),
        new("DOS", "Telli dosya, 10'lu", ProductUnit.Pack, 40m, 70m, 6, 3, 3),
        new("DOS", "Plastik evrak sepeti", ProductUnit.Piece, 85m, 140m, 3, 3, 0.7),
        new("OFS", "Zımba makinesi no:10", ProductUnit.Piece, 120m, 195m, 4, 3, 1),
        new("OFS", "Zımba teli no:10", ProductUnit.Box, 9m, 18m, 20, 3, 8, RunsLow: true),
        new("OFS", "Delgeç 2 delikli", ProductUnit.Piece, 135m, 220m, 3, 3, 0.6),
        new("OFS", "Makas 21 cm", ProductUnit.Piece, 45m, 79m, 6, 1, 2),
        new("OFS", "Koli bandı şeffaf 45 mm", ProductUnit.Piece, 22m, 38m, 15, 0, 7, RunsLow: true),
        new("OFS", "Ataş no:3, 100'lü", ProductUnit.Box, 12m, 24m, 10, 3, 3),
        new("SAN", "Kuru boya, 24 renk", ProductUnit.Box, 95m, 160m, 6, 2, 3),
        new("SAN", "Sulu boya, 12 renk", ProductUnit.Set, 70m, 120m, 5, 2, 2),
        new("SAN", "Resim defteri 35x50", ProductUnit.Piece, 32m, 55m, 10, 1, 4),
        new("SAN", "Akrilik boya seti, 12'li", ProductUnit.Set, 180m, 290m, 3, 2, 0.8),
        new("TEK", "USB bellek 32 GB", ProductUnit.Piece, 120m, 199m, 5, 0, 1.5),
        new("TEK", "Hesap makinesi 12 hane", ProductUnit.Piece, 160m, 260m, 3, 3, 0.7),
        new("TEK", "Kablosuz mouse", ProductUnit.Piece, 210m, 349m, 3, 0, 0.5, RunsLow: true),
        new("TEK", "AA pil, 4'lü", ProductUnit.Pack, 55m, 95m, 10, 0, 5),
    ];
}
