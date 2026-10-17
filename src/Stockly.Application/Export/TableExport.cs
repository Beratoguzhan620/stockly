using System.Globalization;
using System.IO.Compression;
using System.Security;
using System.Text;

namespace Stockly.Application.Export;

/// <summary>A column of an export: its header and how to read the value from a row.</summary>
public sealed record ExportColumn<T>(string Header, Func<T, object?> Value, ExportFormat Format = ExportFormat.Text);

public enum ExportFormat
{
    Text,
    Integer,
    Money,
    DateTime,
}

/// <summary>
/// Writes rows as CSV or as a minimal Excel workbook (.xlsx). The workbook is built by hand from the
/// Office Open XML parts it needs, so the app does not depend on a spreadsheet library.
/// </summary>
public static class TableExport
{
    private static readonly CultureInfo Turkish = CultureInfo.GetCultureInfo("tr-TR");

    /// <summary>
    /// CSV for Turkish Excel: UTF-8 with a byte order mark (so "ç, ğ, ş" open correctly) and semicolons,
    /// because Excel in a Turkish locale reads commas as decimal separators.
    /// </summary>
    public static byte[] ToCsv<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns)
    {
        var builder = new StringBuilder();
        builder.AppendJoin(';', columns.Select(c => Quote(c.Header))).Append("\r\n");

        foreach (var row in rows)
        {
            builder.AppendJoin(';', columns.Select(c => Quote(FormatCsv(c.Value(row), c.Format)))).Append("\r\n");
        }

        return [.. Encoding.UTF8.GetPreamble(), .. Encoding.UTF8.GetBytes(builder.ToString())];
    }

    public static byte[] ToXlsx<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns, string sheetName)
    {
        using var stream = new MemoryStream();
        using (var zip = new ZipArchive(stream, ZipArchiveMode.Create, leaveOpen: true))
        {
            Add(zip, "[Content_Types].xml", ContentTypes);
            Add(zip, "_rels/.rels", RootRels);
            Add(zip, "xl/workbook.xml", Workbook(sheetName));
            Add(zip, "xl/_rels/workbook.xml.rels", WorkbookRels);
            Add(zip, "xl/styles.xml", Styles);
            Add(zip, "xl/worksheets/sheet1.xml", Sheet(rows, columns));
        }

        return stream.ToArray();
    }

    private static string FormatCsv(object? value, ExportFormat format) => value switch
    {
        null => string.Empty,
        decimal d when format == ExportFormat.Money => d.ToString("0.00", Turkish),
        DateTime dt => dt.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture),
        IFormattable f => f.ToString(null, Turkish),
        _ => value.ToString() ?? string.Empty,
    };

    private static string Quote(string value)
    {
        // A leading =, +, - or @ would be run as a formula by spreadsheet apps (CSV injection).
        if (value.Length > 0 && value[0] is '=' or '+' or '-' or '@' && !double.TryParse(value, NumberStyles.Any, Turkish, out _))
            value = "'" + value;

        return value.IndexOfAny([';', '"', '\r', '\n']) >= 0 ? $"\"{value.Replace("\"", "\"\"")}\"" : value;
    }

    private static void Add(ZipArchive zip, string path, string content)
    {
        var entry = zip.CreateEntry(path, CompressionLevel.Optimal);
        using var writer = new StreamWriter(entry.Open(), new UTF8Encoding(false));
        writer.Write(content);
    }

    private static string Sheet<T>(IEnumerable<T> rows, IReadOnlyList<ExportColumn<T>> columns)
    {
        var xml = new StringBuilder();
        xml.Append("""<?xml version="1.0" encoding="UTF-8" standalone="yes"?>""");
        xml.Append("""<worksheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main">""");
        xml.Append("""<sheetViews><sheetView workbookViewId="0"><pane ySplit="1" topLeftCell="A2" activePane="bottomLeft" state="frozen"/></sheetView></sheetViews>""");
        xml.Append("<cols>");
        for (var i = 0; i < columns.Count; i++)
        {
            var width = columns[i].Format switch { ExportFormat.Text => 32, ExportFormat.DateTime => 18, _ => 14 };
            xml.Append($"""<col min="{i + 1}" max="{i + 1}" width="{width}" customWidth="1"/>""");
        }
        xml.Append("</cols><sheetData>");

        xml.Append("""<row r="1">""");
        for (var i = 0; i < columns.Count; i++)
            xml.Append($"""<c r="{Cell(i, 1)}" t="inlineStr" s="1"><is><t>{Escape(columns[i].Header)}</t></is></c>""");
        xml.Append("</row>");

        var rowNumber = 1;
        foreach (var row in rows)
        {
            rowNumber++;
            xml.Append($"""<row r="{rowNumber}">""");
            for (var i = 0; i < columns.Count; i++)
                xml.Append(CellXml(Cell(i, rowNumber), columns[i].Value(row), columns[i].Format));
            xml.Append("</row>");
        }

        xml.Append("</sheetData>");
        if (rowNumber > 1)
            xml.Append($"""<autoFilter ref="A1:{Cell(columns.Count - 1, rowNumber)}"/>""");
        xml.Append("</worksheet>");
        return xml.ToString();
    }

    private static string CellXml(string reference, object? value, ExportFormat format) => value switch
    {
        null => string.Empty,
        decimal d => $"""<c r="{reference}" s="{(format == ExportFormat.Money ? 2 : 0)}"><v>{d.ToString(CultureInfo.InvariantCulture)}</v></c>""",
        int or long or short or byte => $"""<c r="{reference}"><v>{Convert.ToString(value, CultureInfo.InvariantCulture)}</v></c>""",
        DateTime dt => $"""<c r="{reference}" s="3"><v>{dt.ToOADate().ToString(CultureInfo.InvariantCulture)}</v></c>""",
        _ => $"""<c r="{reference}" t="inlineStr"><is><t xml:space="preserve">{Escape(value.ToString() ?? string.Empty)}</t></is></c>""",
    };

    /// <summary>Zero-based column index and one-based row to an A1 reference (0, 1 → A1; 27, 3 → AB3).</summary>
    public static string Cell(int column, int row)
    {
        var name = string.Empty;
        for (var n = column + 1; n > 0; n = (n - 1) / 26)
            name = (char)('A' + (n - 1) % 26) + name;
        return name + row.ToString(CultureInfo.InvariantCulture);
    }

    private static string Escape(string value)
    {
        // XML 1.0 does not allow most control characters, even escaped.
        var clean = new string(value.Where(c => c is '\t' or '\n' or '\r' || c >= ' ').ToArray());
        return SecurityElement.Escape(clean) ?? string.Empty;
    }

    private static string Workbook(string sheetName) =>
        $"""<?xml version="1.0" encoding="UTF-8" standalone="yes"?><workbook xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main" xmlns:r="http://schemas.openxmlformats.org/officeDocument/2006/relationships"><sheets><sheet name="{Escape(SheetName(sheetName))}" sheetId="1" r:id="rId1"/></sheets></workbook>""";

    /// <summary>Excel limits sheet names to 31 characters without : \ / ? * [ ].</summary>
    private static string SheetName(string name)
    {
        var clean = new string(name.Where(c => c is not (':' or '\\' or '/' or '?' or '*' or '[' or ']')).ToArray()).Trim();
        return clean.Length == 0 ? "Sayfa1" : clean[..Math.Min(31, clean.Length)];
    }

    private const string ContentTypes = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Types xmlns="http://schemas.openxmlformats.org/package/2006/content-types"><Default Extension="rels" ContentType="application/vnd.openxmlformats-package.relationships+xml"/><Default Extension="xml" ContentType="application/xml"/><Override PartName="/xl/workbook.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml"/><Override PartName="/xl/worksheets/sheet1.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml"/><Override PartName="/xl/styles.xml" ContentType="application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml"/></Types>""";

    private const string RootRels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument" Target="xl/workbook.xml"/></Relationships>""";

    private const string WorkbookRels = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><Relationships xmlns="http://schemas.openxmlformats.org/package/2006/relationships"><Relationship Id="rId1" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet" Target="worksheets/sheet1.xml"/><Relationship Id="rId2" Type="http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles" Target="styles.xml"/></Relationships>""";

    // Styles: 0 default, 1 bold header, 2 money (#,##0.00), 3 date and time.
    private const string Styles = """<?xml version="1.0" encoding="UTF-8" standalone="yes"?><styleSheet xmlns="http://schemas.openxmlformats.org/spreadsheetml/2006/main"><numFmts count="1"><numFmt numFmtId="164" formatCode="dd.mm.yyyy hh:mm"/></numFmts><fonts count="2"><font><sz val="11"/><name val="Calibri"/></font><font><b/><sz val="11"/><name val="Calibri"/></font></fonts><fills count="2"><fill><patternFill patternType="none"/></fill><fill><patternFill patternType="gray125"/></fill></fills><borders count="1"><border><left/><right/><top/><bottom/><diagonal/></border></borders><cellStyleXfs count="1"><xf numFmtId="0" fontId="0" fillId="0" borderId="0"/></cellStyleXfs><cellXfs count="4"><xf numFmtId="0" fontId="0" fillId="0" borderId="0" xfId="0"/><xf numFmtId="0" fontId="1" fillId="0" borderId="0" xfId="0" applyFont="1"/><xf numFmtId="4" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/><xf numFmtId="164" fontId="0" fillId="0" borderId="0" xfId="0" applyNumberFormat="1"/></cellXfs></styleSheet>""";
}
