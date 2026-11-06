using System.Numerics;

namespace Lab1.Core.Models;

public class Cell
{
    public string? Exp { get; private set; }
    public string Value { get; private set; }
    public BaseNode? ExpTree { get; private set; }
    public CellType? Type { get; private set; }

    public Cell()
    {
        Exp = null;
        Value = "";
        ExpTree = null;
        Type = null;
        IsVisited = false;
    }

    public void SetExpression(string exp)
    {
        exp = exp.Trim();
        Exp = exp;
        if (exp.Length == 0)
        {
            Exp = null;
            Value = "";
            ExpTree = null;
            Type = null;
        }
        else if (exp.StartsWith("="))
        {
            try
            {
                ExpTree = FormulaParser.Parse(exp.Substring(1));
                Type = CellType.Exp;
                Value = "";
            }
            catch (ParsingException e)
            {
                Type = CellType.Error;
                ExpTree = null;
                Value = $"Syntax error at {e.Position}\n{e.Message}";
            }
        }
        else if (BigInteger.TryParse(exp, out BigInteger val))
        {
            Value = val.ToString();
            ExpTree = null;
            Type = CellType.Num;
        }
        else
        {
            Value = exp;
            ExpTree = null;
            Type = CellType.Str;
        }
    }
}

public class Table
{
    private List<List<Cell>>  Cells { get; set; }
    private int Rows { get; set; }
    private int Cols { get; set; }
    public string? Filepath { get; private set; }
    public bool IsSaved { get; set; }
    
    
    public Table()
    {
        Rows = 10;
        Cols = 10;
        Cells = new List<List<Cell>>(Rows);
        for (int r=0; r < Rows; r++)
        {
            List<Cell> row = new List<Cell>(Cols);
            for (int c = 0; c < Cols; c++)
            {
                row.Add(new Cell());
            }
            Cells.Add(row);
        }
        Filepath = null;
        IsSaved = false;
    }

    public void DeleteRow(int row)
    {
        int rowIndex = row - 1;
        if (rowIndex >= Rows || rowIndex < 0) throw new CellNotFoundException($"Row with position {row} doesn't exists");;

        Cells.RemoveAt(rowIndex);
        Rows -= 1;
    }

    public void DeleteCol(int col)
    {
        int colIndex = col - 1;
        if (colIndex >= Cols || colIndex < 0) throw new CellNotFoundException($"Col with position {col} doesn't exists");;

        foreach (List<Cell> row in Cells)
        {
            row.RemoveAt(colIndex);
        }

        Cols -= 1;
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
        ) throw new CellNotFoundException($"Cell with position {col}-{row} doesn't exists");
        
        return Cells[rowIndex][colIndex];
    }

    public void AddRow()
    {
        Cells.Add(new List<Cell>(Rows));
        Rows += 1;
    }

    public void AddCol()
    {
        foreach (List<Cell> row in Cells)
        {
            row.Add(new Cell());
        }
    }

    public Cell GetCellByAddress(string address)
    {
        int i = 0;
        while (i < address.Length && char.IsLetter(address[i]))
        {
            i++;
        }

        string letters = address[..i];
        string numbers = address[i..];
        
        if (string.IsNullOrEmpty(letters) || string.IsNullOrEmpty(numbers))
        {
            throw new CellNotFoundException($"Cell with address '{address}' doesn't exist");
        }

        if (!int.TryParse(numbers, out int row))
        {
            throw new CellNotFoundException($"Invalid row number in address '{address}'");
        }

        int col = 0;

        foreach (char ch in letters.ToUpper())
        {
            col = col * 26 + (ch - 'A' + 1);
        }

        try
        {
            return GetCellByRowCol(row, col);
        }
        catch (CellNotFoundException)
        {
            throw new CellNotFoundException($"Cell with address {address} doesn't exists");
        }
    }
}