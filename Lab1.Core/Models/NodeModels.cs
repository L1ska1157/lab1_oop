using System.Numerics;

namespace Lab1.Core.Models;

public abstract class BaseNode
{
    public abstract BigInteger Calc(Func<string, BigInteger> refResolver);
}

public class NumNode : BaseNode
{
    public BigInteger Value { get; }

    public NumNode(BigInteger value) => Value = value;

    public override BigInteger Calc(Func<string, BigInteger> refResolver) => Value;

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

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
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

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Operator != "^")
            throw new CalcException($"Unknown binary operator: {Operator}");

        BigInteger left = Left.Calc(refResolver);
        BigInteger right = Right.Calc(refResolver);
        if (right < 0)
            throw new CalcException("Negative exponent");
        if (right > 10000) 
            throw new CalcException("Exponent must be less than 10000");
        BigInteger result = BigInteger.Pow(left, (int)right);
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

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Func == "min" || Func == "max")
        {
            if (Args.Length != 2)
                throw new CalcException($"{Func} expects 2 arguments, got {Args.Length}");
            BigInteger a = Args[0].Calc(refResolver);
            BigInteger b = Args[1].Calc(refResolver);
            return Func == "min" ? BigInteger.Min(a, b) : BigInteger.Max(a, b);
        }
        if (Func == "inc" || Func == "dec")
        {
            if (Args.Length != 1)
                throw new CalcException($"{Func} expects 1 argument, got {Args.Length}");
            BigInteger v = Args[0].Calc(refResolver);
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

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
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