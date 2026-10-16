using System.Globalization;
using Stockly.Data.Entities;

namespace Stockly.Web.Infrastructure;

/// <summary>Turkish formatting helpers shared by the views.</summary>
public static class Display
{
    public static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    public static string Money(decimal amount) => amount.ToString("C2", Turkish);

    public static string Number(int value) => value.ToString("N0", Turkish);

    public static string Date(DateTime utc) => ToLocal(utc).ToString("d MMM yyyy", Turkish);

    public static string DateTime(DateTime utc) => ToLocal(utc).ToString("d MMM yyyy, HH:mm", Turkish);

    public static string MonthShort(int year, int month) => new DateTime(year, month, 1).ToString("MMM", Turkish);

    public static string Unit(ProductUnit unit) => unit switch
    {
        ProductUnit.Pack => "paket",
        ProductUnit.Box => "kutu",
        ProductUnit.Set => "set",
        ProductUnit.Ream => "top",
        _ => "adet",
    };

    public static string Movement(MovementType type) => type switch
    {
        MovementType.In => "Giriş",
        MovementType.Out => "Çıkış",
        _ => "Sayım",
    };

    public static string MovementBadge(MovementType type) => type switch
    {
        MovementType.In => "badge badge-success",
        MovementType.Out => "badge badge-neutral",
        _ => "badge badge-warning",
    };

    public static string Signed(int value) => value > 0 ? $"+{value.ToString("N0", Turkish)}" : value.ToString("N0", Turkish);

    private static DateTime ToLocal(DateTime utc) =>
        TimeZoneInfo.ConvertTimeFromUtc(System.DateTime.SpecifyKind(utc, DateTimeKind.Utc), Istanbul);

    private static readonly TimeZoneInfo Istanbul = TimeZoneInfo.FindSystemTimeZoneById("Europe/Istanbul");
}
