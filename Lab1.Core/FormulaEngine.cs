using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Lab1.Core.Models;

namespace Lab1.Core;

public class FormulaEngine(Table table)
{
    private Table _table = table;
    private HashSet<string> _visited = new HashSet<string>();

    public CalcRes Calculate(BaseNode expTree)
    {
        _visited.Clear();
        try
        {
            return CalcRes.Ok(expTree.Calc(RefResolver));
        }
        catch (CalcException e)
        {
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
                referencedCell = _table.GetCellByAddress(address);
            }
            catch (CellNotFoundException)
            {
                throw new CalcException("Reference not found");
            }

            if (referencedCell.Type == CellType.Error)
            {
                throw new CalcException("Error in reference");
            }

            if (referencedCell.Type is CellType.Str or null)
            {
                throw new CalcException("Wrong data in reference");
            }

            if (referencedCell.Type == CellType.Num)
            {
                if (BigInteger.TryParse(referencedCell.Value, out BigInteger value)) return value;
                throw new CalcException("Can't parse referenced cell");
            }

            ;

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