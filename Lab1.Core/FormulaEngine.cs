using System.Globalization;
using System.Numerics;
using System.Text.RegularExpressions;
using Lab1.Core.Models;

namespace Lab1.Core;

public class FormulaEngine
{
    public CalcRes Calculate(BaseNode expTree)
    {
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
        // TODO after making table
        return 1;
    }
}