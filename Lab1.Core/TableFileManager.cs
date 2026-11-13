using System.Text.Encodings.Web;
using System.Text.Json;
using Lab1.Core.Models;

namespace Lab1.Core;

public static class TableFileManager
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Save(Table table, Stream stream)
    {
        string[][] data = new string[table.Rows][];
        for (int r = 1; r <= table.Rows; r++)
        {
            data[r - 1] = new string[table.Cols];
            for (int c = 1; c <= table.Cols; c++)
                data[r - 1][c - 1] = table.GetCellByRowCol(r, c).Exp ?? "";
        }
        JsonSerializer.Serialize(stream, data, Options);
    }

    public static Table Load(Stream stream)
    {
        string[][]? data;
        try { data = JsonSerializer.Deserialize<string[][]>(stream); }
        catch (JsonException) { throw new TableFileException("Файл має неправильний формат"); }

        if (data == null || data.Length == 0 || data[0] == null || data[0].Length == 0)
            throw new TableFileException("Таблиця у файлі порожня");

        int cols = data[0].Length;
        foreach (var row in data)
            if (row == null || row.Length != cols || row.Any(cell => cell == null))
                throw new TableFileException("Рядки таблиці у файлі мають різну довжину");

        var table = new Table(data.Length, cols);
        for (int r = 1; r <= data.Length; r++)
            for (int c = 1; c <= cols; c++)
                table.GetCellByRowCol(r, c).SetExpression(data[r - 1][c - 1]);
        return table;
    }
}