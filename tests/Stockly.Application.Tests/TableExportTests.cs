using System.IO.Compression;
using System.Text;
using System.Xml.Linq;
using Stockly.Application.Export;

namespace Stockly.Application.Tests;

public class TableExportTests
{
    private sealed record Row(string Name, int Quantity, decimal Price, DateTime When, string? Note);

    private static readonly IReadOnlyList<ExportColumn<Row>> Columns =
    [
        new("Ürün", r => r.Name),
        new("Miktar", r => r.Quantity, ExportFormat.Integer),
        new("Fiyat", r => r.Price, ExportFormat.Money),
        new("Tarih", r => r.When, ExportFormat.DateTime),
        new("Not", r => r.Note),
    ];

    private static readonly Row[] Rows =
    [
        new("Kâğıt; A4", 12, 1234.5m, new DateTime(2026, 10, 1, 14, 30, 0), "\"özel\" not"),
        new("=HYPERLINK(\"x\")", -3, 0m, new DateTime(2026, 10, 2, 9, 0, 0), null),
    ];

    [Fact]
    public void Csv_starts_with_a_byte_order_mark_and_uses_semicolons()
    {
        var bytes = TableExport.ToCsv(Rows, Columns);

        Assert.Equal(Encoding.UTF8.GetPreamble(), bytes[..3]);
        var lines = Encoding.UTF8.GetString(bytes[3..]).Split("\r\n");
        Assert.Equal("Ürün;Miktar;Fiyat;Tarih;Not", lines[0]);
        Assert.Equal("\"Kâğıt; A4\";12;1234,50;2026-10-01 14:30;\"\"\"özel\"\" not\"", lines[1]);
    }

    [Fact]
    public void Csv_neutralises_cells_that_would_run_as_formulas_but_keeps_negative_numbers()
    {
        var lines = Encoding.UTF8.GetString(TableExport.ToCsv(Rows, Columns)[3..]).Split("\r\n");

        Assert.StartsWith("\"'=HYPERLINK(\"\"x\"\")\";-3;", lines[2]);
    }

    [Fact]
    public void Xlsx_is_a_valid_package_with_typed_cells()
    {
        var bytes = TableExport.ToXlsx(Rows, Columns, "Ürünler: Ekim/2026");

        using var zip = new ZipArchive(new MemoryStream(bytes));
        Assert.NotNull(zip.GetEntry("[Content_Types].xml"));
        Assert.NotNull(zip.GetEntry("xl/styles.xml"));

        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var workbook = XDocument.Load(zip.GetEntry("xl/workbook.xml")!.Open());
        Assert.Equal("Ürünler Ekim2026", workbook.Descendants(ns + "sheet").Single().Attribute("name")!.Value);

        var sheet = XDocument.Load(zip.GetEntry("xl/worksheets/sheet1.xml")!.Open());
        var rows = sheet.Descendants(ns + "row").ToList();
        Assert.Equal(3, rows.Count);

        var cells = rows[1].Elements(ns + "c").ToList();
        Assert.Equal("Kâğıt; A4", cells[0].Descendants(ns + "t").Single().Value);
        Assert.Equal("12", cells[1].Element(ns + "v")!.Value);
        Assert.Equal("1234.5", cells[2].Element(ns + "v")!.Value);
        Assert.Equal("2", cells[2].Attribute("s")!.Value);
        Assert.Equal(new DateTime(2026, 10, 1, 14, 30, 0).ToOADate().ToString(System.Globalization.CultureInfo.InvariantCulture), cells[3].Element(ns + "v")!.Value);

        // An empty value leaves the cell out instead of writing an empty string.
        Assert.Equal(4, rows[2].Elements(ns + "c").Count());
        Assert.Equal("A1:E3", sheet.Descendants(ns + "autoFilter").Single().Attribute("ref")!.Value);
    }

    [Theory]
    [InlineData(0, 1, "A1")]
    [InlineData(25, 2, "Z2")]
    [InlineData(26, 3, "AA3")]
    [InlineData(27, 10, "AB10")]
    [InlineData(701, 1, "ZZ1")]
    public void Cell_references_follow_excel_column_names(int column, int row, string expected) =>
        Assert.Equal(expected, TableExport.Cell(column, row));
}
