namespace Lab1.Core.Models;

public abstract class BaseNode
{
    public abstract double Calc(Func<string, double> refResolver);
}

public class NumNode : BaseNode
{
    public double Value { get; }

    public NumNode(double value) => Value = value;

    public override double Calc(Func<string, double> refResolver) => Value;

    public override string ToString() => $"NumNode({Value})";
}

public class UnaryOpNode : BaseNode
{
    public string Operator { get; }
    public BaseNode Child { get; }

    public UnaryOpNode(string op, BaseNode child)
    {
        Operator = op;
        Child = child;
    }

    public override double Calc(Func<string, double> refResolver)
    {
        if (Operator == "+") return Child.Calc(refResolver);
        if (Operator == "-") return -Child.Calc(refResolver);
        throw new CalcException($"Unknown unary operator: {Operator}");
    }

    public override string ToString() => $"UnaryOpNode({Operator}, {Child})";
}

public class BinaryOpNode : BaseNode
{
    public string Operator { get; }
    public BaseNode Left { get; }
    public BaseNode Right { get; }

    public BinaryOpNode(string op, BaseNode left, BaseNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public override double Calc(Func<string, double> refResolver)
    {
        if (Operator != "^")
            throw new CalcException($"Unknown binary operator: {Operator}");

        double left = Left.Calc(refResolver);
        double right = Right.Calc(refResolver);
        if (left == 0 && right < 0)
            throw new CalcException("Division by zero in '^'");
        double result = Math.Pow(left, right);
        if (double.IsInfinity(result) || double.IsNaN(result))
            throw new CalcException("Overflow or undefined result in '^'");
        return result;
    }

    public override string ToString() => $"BinaryOpNode({Operator}, {Left}, {Right})";
}

public class FuncOpNode : BaseNode
{
    public string Func { get; }
    public BaseNode[] Args { get; }

    public FuncOpNode(string func, params BaseNode[] args)
    {
        Func = func;
        Args = args;
    }

    public override double Calc(Func<string, double> refResolver)
    {
        if (Func == "min" || Func == "max")
        {
            if (Args.Length != 2)
                throw new CalcException($"{Func} expects 2 arguments, got {Args.Length}");
            double a = Args[0].Calc(refResolver);
            double b = Args[1].Calc(refResolver);
            return Func == "min" ? Math.Min(a, b) : Math.Max(a, b);
        }
        if (Func == "inc" || Func == "dec")
        {
            if (Args.Length != 1)
                throw new CalcException($"{Func} expects 1 argument, got {Args.Length}");
            double v = Args[0].Calc(refResolver);
            return Func == "dec" ? v - 1 : v + 1;
        }
        throw new CalcException($"Unknown function: {Func}");
    }

    public override string ToString() => $"FuncOpNode({Func}, [{string.Join(", ", Args.Select(a => a.ToString()))}])";
}

public class RefNode : BaseNode
{
    public string Address { get; }
    public int Position { get; }

    public RefNode(string address, int position)
    {
        Address = address;
        Position = position;
    } 

    public override double Calc(Func<string, double> refResolver)
    {
        try
        {
            return refResolver(Address);
        }
        catch (CalcException ex)
        {
            throw new CalcException(ex.Message, Position);
        }
    }

    public override string ToString() => $"RefNode({Address})";
    
}