using System.Diagnostics;
using System.Numerics;
using Lab1.Core.Models;
using System.Globalization;

namespace Lab1.Core;

public class FormulaEngine(Table table)
{
    private HashSet<string> _visited = [];

    public BigInteger Calculate(BaseNode expTree)
    {
        _visited.Clear();
        return expTree.Calc(RefResolver);
    }

    private BigInteger RefResolver(string address)
    {
        if (!_visited.Add(address)) throw new CalcException("Зациклені посилання");

        try
        {
            Cell referencedCell;
            try
            {
                referencedCell = table.GetCellByAddress(address);
            }
            catch (CellNotFoundException)
            {
                throw new CalcException("Посилання не знайдено");
            }

            if (referencedCell.Type == CellType.Error)
            {
                throw new CalcException("Помилка в обчисленні посилання");
            }

            if (referencedCell.Type is CellType.Str or CellType.Empty)
            {
                throw new CalcException("Неправильні дані в посиланні");
            }

            if (referencedCell.Type == CellType.Num)
            {
                if (BigInteger.TryParse(referencedCell.Value, out BigInteger value)) return value;
                throw new CalcException("Неможливо прочитати значення за посиланням");
            }

            if (referencedCell.Type == CellType.Exp)
            {
                if (referencedCell.ExpTree == null)
                    throw new CalcException("Пусте дерево виразу");
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