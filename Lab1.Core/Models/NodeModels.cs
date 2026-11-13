using System.Numerics;

namespace Lab1.Core.Models;

public abstract class BaseNode
{
    public abstract BigInteger Calc(Func<string, BigInteger> refResolver);
}

public class NumNode(BigInteger value) : BaseNode
{
    private BigInteger Value { get; } = value;

    public override BigInteger Calc(Func<string, BigInteger> refResolver) => Value;

    public override string ToString() => $"NumNode({Value})";
}

public class UnaryOpNode(string op, BaseNode child) : BaseNode
{
    private string Operator { get; } = op;
    private BaseNode Child { get; } = child;

    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        if (Operator == "+") return Child.Calc(refResolver);
        if (Operator == "-") return -Child.Calc(refResolver);
        throw new CalcException($"Невідомий унарний оператор: {Operator}");
    }

    public override string ToString() => $"UnaryOpNode({Operator}, {Child})";
}

public class BinaryOpNode(string op, BaseNode left, BaseNode right) : BaseNode
{
    private string Operator { get; } = op;
    private BaseNode Left { get; } = left;
    private BaseNode Right { get; } = right;

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

public class FuncOpNode(string func, params BaseNode[] args) : BaseNode
{
    private string Func { get; } = func;
    private BaseNode[] Args { get; } = args;

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

public class RefNode(string address, int position) : BaseNode
{
    public override BigInteger Calc(Func<string, BigInteger> refResolver)
    {
        try
        {
            return refResolver(address);
        }
        catch (CalcException e)
        {
            throw new CalcException(e.Message, position);
        }
    }

    public override string ToString() => $"RefNode({address})";
    
}