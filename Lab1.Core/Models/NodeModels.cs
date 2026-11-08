using System.Numerics;

namespace Lab1.Core.Models;

public abstract class BaseNode
{
    public abstract BigInteger Calc(Func<string, BigInteger> refResolver);
}

public class NumNode : BaseNode
{
    private BigInteger Value { get; }

    public NumNode(BigInteger value) => Value = value;

    public override BigInteger Calc(Func<string, BigInteger> refResolver) => Value;

    public override string ToString() => $"NumNode({Value})";
}

public class UnaryOpNode : BaseNode
{
    private string Operator { get; }
    private BaseNode Child { get; }

    public UnaryOpNode(string op, BaseNode child)
    {
        Operator = op;
        Child = child;
    }

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Operator == "+") return Child.Calc(refResolver);
        if (Operator == "-") return -Child.Calc(refResolver);
        throw new CalcException($"Невідомий унарний оператор: {Operator}");
    }

    public override string ToString() => $"UnaryOpNode({Operator}, {Child})";
}

public class BinaryOpNode : BaseNode
{
    private string Operator { get; }
    private BaseNode Left { get; }
    private BaseNode Right { get; }

    public BinaryOpNode(string op, BaseNode left, BaseNode right)
    {
        Operator = op;
        Left = left;
        Right = right;
    }

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Operator != "^")
            throw new CalcException($"Невідомий бінарний оператор: {Operator}");

        BigInteger left = Left.Calc(refResolver);
        BigInteger right = Right.Calc(refResolver);
        if (right < 0)
            throw new CalcException("Відʼємний показник степені");
        if (right > 10000) 
            throw new CalcException("Показник степені не більше 10000");
        BigInteger result = BigInteger.Pow(left, (int)right);
        return result;
    }

    public override string ToString() => $"BinaryOpNode({Operator}, {Left}, {Right})";
}

public class FuncOpNode : BaseNode
{
    private string Func { get; }
    private BaseNode[] Args { get; }

    public FuncOpNode(string func, params BaseNode[] args)
    {
        Func = func;
        Args = args;
    }

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Func is "min" or "max")
        {
            if (Args.Length != 2)
                throw new CalcException($"{Func} очікувала 2 аргументи, отримала {Args.Length}");
            BigInteger a = Args[0].Calc(refResolver);
            BigInteger b = Args[1].Calc(refResolver);
            return Func == "min" ? BigInteger.Min(a, b) : BigInteger.Max(a, b);
        }
        if (Func is "inc" or "dec")
        {
            if (Args.Length != 1)
                throw new CalcException($"{Func} очікувала 1 аргумент, отримала {Args.Length}");
            BigInteger v = Args[0].Calc(refResolver);
            return Func == "dec" ? v - 1 : v + 1;
        }
        throw new CalcException($"Невідома функція: {Func}");
    }

    public override string ToString() => $"FuncOpNode({Func}, [{string.Join(", ", Args.Select(a => a.ToString()))}])";
}

public class RefNode : BaseNode
{
    private readonly string _address;
    private readonly int _position;

    public RefNode(string address, int position)
    {
        _address = address;
        _position = position;
    } 

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        try
        {
            return refResolver(_address);
        }
        catch (CalcException ex)
        {
            throw new CalcException(ex.Message, _position);
        }
    }

    public override string ToString() => $"RefNode({_address})";
    
}