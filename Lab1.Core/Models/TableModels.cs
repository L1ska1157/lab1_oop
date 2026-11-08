using System.Diagnostics;
using System.Numerics;
using System.Globalization;

namespace Lab1.Core.Models;

public class Cell
{
    public string? Exp { get; private set; }
    public string Value { get; private set; } = "";
    public BaseNode? ExpTree { get; private set; }
    public CellType Type { get; private set; } = CellType.Empty;

    public void SetExpression(string exp)
    {
        exp = exp.Trim();
        Exp = exp.Length == 0 ? null : exp;
        ExpTree = null;

        if (exp.Length == 0)
        {
            Value = "";
            Type = CellType.Empty;
        }
        else if (exp[0] == '=')
        {
            try
            {
                ExpTree = FormulaParser.Parse(exp[1..]);
                Type = CellType.Exp;
                Value = "";
            }
            catch (ParsingException e)
            {
                Debug.WriteLine($"[parse] '{exp}' -> {e.Message} (pos {e.Position})");
                Type = CellType.Error;
                Value = e.Position is int p
                    ? $"Syntax error at {p + 2}\n{e.Message}"
                    : $"Syntax error\n{e.Message}";
            }
        }
        else if (BigInteger.TryParse(exp, NumberStyles.AllowLeadingSign,
                     CultureInfo.InvariantCulture, out BigInteger val))
        {
            Value = val.ToString(CultureInfo.InvariantCulture);
            Type = CellType.Num;
        }
        else
        {
            Value = exp;
            Type = CellType.Str;
        }
    }
}

public class Table
{
    private List<List<Cell>> Cells { get; } = [];
    public int Rows { get; private set; }
    public int Cols { get; private set; }

    public Table(int rows = 10, int cols = 10)
    {
        if (rows < 1 || cols < 1) throw new ArgumentOutOfRangeException();
        Cols = cols;
        for (int r = 0; r < rows; r++) AddRow();
    }

    public void AddRow()
    {
        var row = new List<Cell>(Cols);
        for (int c = 0; c < Cols; c++) row.Add(new Cell());
        Cells.Add(row);
        Rows++;
    }

    public void AddCol()
    {
        foreach (var row in Cells) row.Add(new Cell());
        Cols++;
    }

    public void DeleteRow(int row)
    {
        int i = row - 1;
        if (i < 0 || i >= Rows) throw new CellNotFoundException($"Row {row} doesn't exist");
        if (Rows == 1) throw new InvalidOperationException("Table must have at least one row");
        Cells.RemoveAt(i);
        Rows--;
    }

    public void DeleteCol(int col)
    {
        int i = col - 1;
        if (i < 0 || i >= Cols) throw new CellNotFoundException($"Column {col} doesn't exist");
        if (Cols == 1) throw new InvalidOperationException("Table must have at least one column");
        foreach (var row in Cells) row.RemoveAt(i);
        Cols--;
    }

    public Cell GetCellByRowCol(int row, int col)
    {
        int rowIndex = row - 1;
        int colIndex = col - 1;
        if (
            rowIndex >= Rows ||
            rowIndex < 0 ||
            colIndex >= Cols ||
            colIndex < 0
        ) throw new CellNotFoundException($"Cell at row {row}, column {col} doesn't exist");
        
        return Cells[rowIndex][colIndex];
    }

    public Cell GetCellByAddress(string address)
    {
        int i = 0;
        while (i < address.Length && char.IsAsciiLetter(address[i])) i++;
        string letters = address[..i];

        if (letters.Length is 0 or > 3 ||
            !int.TryParse(address[i..], NumberStyles.None, CultureInfo.InvariantCulture, out int row))
            throw new CellNotFoundException($"Cell with address '{address}' doesn't exist");

        int col = 0;
        foreach (char ch in letters.ToUpperInvariant())
            col = col * 26 + (ch - 'A' + 1);

        return GetCellByRowCol(row, col);
    }
}