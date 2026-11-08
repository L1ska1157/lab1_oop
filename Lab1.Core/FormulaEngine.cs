using System.Diagnostics;
using System.Numerics;
using Lab1.Core.Models;
using System.Globalization;

namespace Lab1.Core;

public class FormulaEngine(Table table)
{
    private readonly HashSet<string> _visited = [];

    public CalcRes Calculate(BaseNode expTree)
    {
        _visited.Clear();
        Debug.WriteLine($"[calc] tree: {expTree}");
        try
        {
            var result = expTree.Calc(RefResolver);
            Debug.WriteLine($"[calc] result: {result}");
            return CalcRes.Ok(result);
        }
        catch (CalcException e)
        {
            Debug.WriteLine($"[calc] error: {e.Message} (pos {e.Position})");
            return CalcRes.Fail(e.Message, e.Position);
        }
    }

    private BigInteger RefResolver(string address)
    {
        if (!_visited.Add(address)) throw new CalcException("Cycled refs");

        try
        {
            Cell referencedCell;
            try
            {
                referencedCell = table.GetCellByAddress(address);
            }
            catch (CellNotFoundException)
            {
                throw new CalcException("Reference not found");
            }

            if (referencedCell.Type == CellType.Error)
            {
                throw new CalcException("Error in reference");
            }

            if (referencedCell.Type is CellType.Str or CellType.Empty)
            {
                throw new CalcException("Wrong data in reference");
            }

            if (referencedCell.Type == CellType.Num)
            {
                if (BigInteger.TryParse(referencedCell.Value, out BigInteger value)) return value;
                throw new CalcException("Can't parse referenced cell");
            }

            if (referencedCell.Type == CellType.Exp)
            {
                if (referencedCell.ExpTree == null)
                    throw new CalcException("Empty expression tree");
                return referencedCell.ExpTree.Calc(RefResolver);
            }
        }
        finally
        {
            _visited.Remove(address);
        }
        return 0;
    }
}